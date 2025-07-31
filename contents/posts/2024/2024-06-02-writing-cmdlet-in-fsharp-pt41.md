---
title: "F# でコマンドレットを書いてる pt.41"
tags: ["fsharp", "powershell", "dotnet"]
---

最近の [krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をまとめておく。

大量データを処理する場合、描画までめちゃくちゃ遅くなる課題 [#177](https://github.com/krymtkts/pocof/issues/177) の対処を試している。
対処法としては単純に、① 描画、② ユーザによるクエリ入力、③ `ProcessRecord` からのデータ入力の 3 つが切り分けられて別々に動いたら良いだけ。
issue 起票したときから非同期にしたら余裕よな～という感触はあった。
①、② を非同期にし、③ が終了した後 `EndProcessing` で ①② の終了を待ち受けるイメージ。
これが実装できない場合、 3 つに切り分ける数を 2 つに減らすとか、最悪のケースは全部直列で処理する。
最悪 ③ で特定の件数毎に ①、② は諦めて `EndProcessing` で起動する、というイメージだった。

ただこのアイデアを実装に移すにあたり、わたし自身事前に勉強しておく必要があった。
.NET や F# で並行/並列のような非同期プログラミングをほぼやったことなかった(意外とそういう機会なかった)し、 Cmdlet 内での非同期処理をどう扱えるか知らないのもあり。
アイデアの実現可能性を探る必要があったということ。

[krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox) で、お試しコードを書きつつドキュメントを見つつで try & error して、漸く感触を掴んできた。

わかってきたのは以下の感じ。
[`PSCmdlet`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.pscmdlet?view=powershellsdk-7.4.0) 内で普通の処理を非同期にするのは .NET と F# の POWER もあり簡単。でも PowerShell 由来の機能を使うのは出来ないという印象だった。なかなかハードル高い。

- F# には [`async`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions) [`task`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) computation 等の非同期のための準備が整っている
- .NET には [`SynchronizationContext`](https://learn.microsoft.com/en-us/archive/msdn-magazine/2011/february/msdn-magazine-parallel-computing-it-s-all-about-the-synchronizationcontext) で特定の thread で処理を実行する下地があり、 `task` や `async` でそれが使える([`Async.SwitchToContext`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync.html#SwitchToContext))
  - ただし `SynchronizationContext.Current` は Windows.Form や WPF 等では提供されていても CLI Application ひいては Cmdlet には提供されてない
- PowerShell の Cmdlet 実装は single thread を前提にしている
  - これちょっと裏付けのドキュメントが見つけれてないから探したい
  - multi threading で操作したらこんな感じのエラーが出た
    - > Select-Pocof: The WriteObject and WriteError methods cannot be called from outside the overrides of the BeginProcessing, ProcessRecord, and EndProcessing methods, and they can only be called from within the same thread. Validate that the cmdlet makes these calls correctly, or contact Microsoft Customer Support Services.
- (余談だが) PowerShell では無限リストのような終わらないデータ構造をサポートしている Cmdlet はそんなにない。 [`Select-Object -First`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/select-object?view=powershell-7.4) が代表格
  - 基本 [`BeginProcessing`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.cmdlet.beginprocessing?view=powershellsdk-7.4.0) → [`ProcessRecord`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.cmdlet.processrecord?view=powershellsdk-7.4.0) → [`EndProcessing`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.cmdlet.endprocessing?view=powershellsdk-7.4.0) のフローを抜けることはできない
  - `Select-Object` は `StopUpstreamCommandsException` という秘密の内部例外で以てフローを中断できる。 `ProcessRecord` を中断させる方法は公式に提供されないので、みんな reflection でこの方法を流用して中断している
    - [Allow user code to stop a pipeline on demand / to terminate upstream cmdlets. · Issue #3821 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/3821)
  - pocof で非同期的に描画を始める場合、全部読み込み終わる前にキャンセルするケースを考えたらこれが必要だったので調べた

pocof では [`WriteObject`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.writeobject?view=powershellsdk-7.4.0) 以外に PowerShell 由来の機能を使っている。
[`PSCmdlet.InvokeCommand.InvokeScript`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.commandinvocationintrinsics.invokescript?view=powershellsdk-7.4.0) で描画のフォーマットをしているのがそれだ。
multi threading でも `int` とか `PSCustomObject` を流す程度なら問題なかったのだけど、 `Get-ChildItem` の結果を流すと描画後に処理が詰まるとわかった。
深く追えてないけど `FileInfo` あたりがダメなんかな...？という感じ。回避できない。
マジか。

`WriteObject` を main thread に置くのは造作もない。 pocof の場合は最後の `EndProcessing` で実行したらいいだけだからだ。
けど、 `PSCmdlet.InvokeCommand.InvokeScript` を main thread に限定するのが難しい。常に描画する事があるためだ。

これを解消する実装パターン的にまさに UI で求められてるのと同じなので `SynchronizationContext` にハマるはずだった。
ただし CLI Application には提供されていない。
また自前でそれを作るとしても CLI Application に独自の `SynchronizationContext` 実装を組み込むのは困難が伴う。
他の回避方法を探るために、例えば [`System.Collections.ObjectModel.ObservableCollection`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1?view=net-8.0) なんかを使ってイベント駆動で描画しようとも試した。
けどコレクションに put した thread でイベントハンドラが実行されるので、先述の詰まりを回避できない。

結局のところ、 main thread に自前の event loop もどきを構築する形で実現することにした。 [#186](https://github.com/krymtkts/pocof/pull/186)

ユーザ入力とクエリ結果生成を `async` に逃して、 PowerShell 由来の機能を使う描画は main thread で地道に event loop を組む。
これには非同期コレクションを 2 つ用いた。 1 つは検索対象を保持する非同期コレクション ①、もう １ つはクエリ結果を格納する描画イベントと終了イベントを保持する非同期コレクション ②。実装イメージはこうだ。

- `BeginProcessing`
  - ユーザ入力待受やユーザ入力によるクエリ更新を `async` で起動、ユーザ入力によるクエリ更新があれば非同期コレクション ② に描画イベントを発行する
  - 画面の初期化はここで実行する
- `ProcessRecord`
  - 逐次受け渡される `InputObject` を非同期コレクション ① に追加する
  - 非同期コレクション ② にから描画イベントを受け取ればそれで描画する
- `EndProcessing`
  - 描画の event loop を開始し、非同期コレクション ② から描画イベントを受け取れば描画、終了イベントを受け取れば event loop を終了する
  - ユーザ入力の `async` が完了するのを待つ
  - `async` から結果を受け取ったら `WriteObject` して終了する

もっとマシな方法ないんかコラという感じではあるが...少なくともうまく実装出来そうな気配はしてきた。
まだ完成してなくて、上記の切り分けをしたうえで従来の処理フローを再現できた状態まできた。この後また詰まることあったら路線変更するかも。

もしうまくいったら、大規模にリファクタリングしたい。
いま、先述の非同期コレクション ② に使ってる [`ConcurrentStack`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentstack-1?view=net-8.0) が生で露出してるので、そこは抽象化出来たらかっこいいよねという気持ち。
それに既存のテストも結構モジュール構造に合わなくなってしまったので、新しい構造に寄り添わせないとカオスなテストがより手懐けられなくなる。
カバレッジもなんか落としてしまってるので改善できないか見る。

あと今回 [`lazy`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lazy-expressions) は使わなかったが、クエリ結果とかまさにこのパターンにハマるような気配もするけど試せてない。

Cmdlet に独自の処理フローを実装しようとしたら出来なくはないけどけっこう大変なんやなというのを身を以て知れて良かった。うまく非同期レンダリング実装できたら pocof 良くなりそう。
(そんなクソデカデータや無限リストを PowerShell で扱うやつがどこにおるねんというオーバースペック感)

続く。

(なんか自ブログ執筆中の dev-server で websocket 通らなくなってるなんで)
