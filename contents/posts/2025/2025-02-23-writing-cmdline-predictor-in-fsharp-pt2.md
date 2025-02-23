---
title: "F# で command-line predictor を書いてる Part 2"
tags: ["fsharp", "powershell", "dotnet"]
---

[前](https://krymtkts.github.io/posts/2024-11-10-writing-cmdline-predictor-in-fsharp-pt1.html)は sample を作っただけだが、最近 F# で command-line predictor を書くのを本格化した。

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) という。
まだ prerelease だが PowerShell Gallery に [SnippetPredictor](https://www.powershellgallery.com/packages/SnippetPredictor) で公開してある。
事前に用意しておいた snippet を入力内容に応じて suggestion に表示するという単純な command-line predictor だ。
snippet を表示・追加・削除するための Cmdlet も一緒のバイナリで提供している。

![SnippetPredictor のキャプチャ](/img/2025-02-23-capture/snippet-predictor.gif "SnippetPredictor のキャプチャ")

因みに PowerShell の command-line predictor は [PowerShell](https://github.com/PowerShell/PowerShell) 7.2 ＆ [PSReadLine](https://github.com/PowerShell/PSReadLine) 2.2.2 以降に導入された plugin 機能の 1 つだ。以下の Microsoft の記事が詳しい(当たり前だが)。

[How to create a command-line predictor - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.4)

PSReadLine の experimental feature の `PSSubsystemPluginModel` を手動で有効化すると使えるようになる。
また PSReadLine の option で `PredictionSource=HistoryAndPlugin` `PredictionViewStyle=ListView` の設定をする必要がある。

[PSSubsystemPluginModel Using Experimental Features in PowerShell - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.5#pssubsystempluginmodel)

前にも触れたが command-line predictor の作り方は先に挙げた [How to create a command-line predictor - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.4) を参照している。

多分このドキュメントと PowerShell team 謹製の [PowerShell/CompletionPredictor](https://github.com/PowerShell/CompletionPredictor) とか、個人が作ってるようなのしか参考がないのが現状だろう。
今回作ったやつが「個人が作ってるようなの」に仲間入りすることになれば最高やな。

今のところ command-line predictor の一部の機能しか使ってないので、大して困ったことは発生していない。
ただいくつか気になる・工夫すべき点があるのは事実で、いかにそのいくつかを記す。

まず、実装して使ってみないと気付けなかった点として、 `Name` 属性の指定がある。
[`ICommandPredictor`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.subsystem.icommandpredictor?view=powershellsdk-7.2.0) 実装 class に `Name` 属性で subsystem 実装の名前を定義するのだけど、これが suggestion 右端に `[subsystem name]` という感じで表示される。
なので多少長い名前をつけているだけで幅の制限に引っかかって省略表記になってしまう。
実際 `SnippetPredictor` だと長過ぎて省略されてしまったので `Snippet` に短縮した。
PowerShell が流行ることはないと思うが、万が一 subsystem が沢山増えた場合、名前の奪い合いみたいになってしまうであろうこと必至。

そして command-line predictor の実装の制約として、① suggestion を 1 行を収める必要があるのと、② 20ms 以内に表示する必要がある。これらは先述の作り方の記事にも書いてある。

① に違反すると PSReadLine が suggestion の ListView 用に確保しているスペースの再描画が崩れる。早い話が suggestion が消費する行の高さまで PSReadLine が計算してないためそうなってる。今のところはそれだけっぽいので気合で使おうと思えばどうにかなるが、見た目にもきれいじゃなく見にくくなるだけなので、やらないのが無難だろう。これが原因でエラーになるとかではないので、比較的ゆるい制約と言える。
② に違反すると、 suggestion 荷表示されない。これは command-line predictor として役に立たないので非常に気に掛ける必要がある。

SnippetPredictor の場合、 snippet は JSON ファイルに保持している[^1]ので、読み込みに時間がかかるようなことを避けるべく SnippetPredictor の import 後にファイルの snippet をメモリ上に読み込んで suggestion の絞り込みに利用している。
また、ファイルの変更を監視して変更があったら比較的リアルタイムに反映できるようにした。
① に関しては今のところ機能的に制限していないので、 JSON ファイルに改行文字 `\n` が含まれたらダメな感じ。なので登録時か出力時に `\n` を読み替える必要があるかも。検討中だ。

最後に PowerShell で対話型のインタフェースを作ったときに困るのがエラーの表示だ。 [pocof](https://github.com/krymtkts/pocof) でも散々苦労したが、 command-line predictor も例に漏れず、専用のエラー表示 UI は持ってなさそうだ。
現状 command-line predictor 自体は suggestion でユーザと対話するしかないので、仮にエラーがあればそこに表示するようにした。
SnippetPredictor の場合は snippet を出せない理由、例えば JSON ファイルのフォーマットが壊れてるとかが表示されることになる。

[^1]: .NET で依存関係なしで楽に実装できてユーザも比較的楽に編集できるのがこれくらいしかなかった。より良い方法は模索中。

とりあえず PowerShell module として必要な最低限の要素(機能と help くらい)をざーっと作ったのみなので発見してない bug などあろうが、今のところ気に入っている。
テストを全然書いてなかったり、開発を継続するのを楽にする CI とか作ってないので、今後充実していけたら良いなと思う。

他にも pocof で変えたいと思っている点も、 SnippetPredictor のようなまだ出来上がってない project だと導入しやすい。
pocof ではちょうど [xUnit](https://github.com/xunit/xunit) をやめて [expecto](https://github.com/haf/expecto) に移行したい気持ちがあるので、 SnippetPredictor では先行して expecto をセットアップしてみたい。
