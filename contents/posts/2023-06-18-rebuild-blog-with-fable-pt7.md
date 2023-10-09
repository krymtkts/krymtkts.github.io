{:title "Fable でブログを再構築する pt.7"
:layout :post
:tags ["fsharp", "fable"]}

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

当初開発サーバに使ってた [tapio/live-server](https://github.com/tapio/live-server) は npm の vulnerability 報告がずっと出てた。
開発用なので構わないといえばそうだがやっぱいい気分ではないので、自力で開発サーバを立てるように変えていく(現在作業中)。
[#20](https://github.com/krymtkts/blog-fable/pull/20)

F# Weekly の Sergey Tihon 氏がちょうどよい F# script を書いてるのを見つけたので、それをベースにする。
Web サーバに F# 製の [SuaveIO/suave](https://github.com/SuaveIO/suave) を使うようになる。

[f# - Suave in watch mode (during development) - Stack Overflow](https://stackoverflow.com/questions/34603913/suave-in-watch-mode-during-development)

これに従うと、変更イベントで WebSocket のメッセージをページに送って、ページはメッセージを受け取ったらリロードする、という流れで live reload をすることになる。

1. ファイルの変更検知して変更イベントを発火する
2. 変更イベントで WebSocket のメッセージ送信する
3. ページに差し込んだ JavaScript で WebSocket のメッセージを受け取ったら reload する

この開発サーバ用 F# スクリプトを開発するにあたり、
F# Interactive 特有の挙動とかあまりわかってなかったので、以下を参照に勉強した。
[F# Interactive (dotnet) Reference | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/)

今回ファイルの変更を監視するのと、 `dotnet` コマンド実行するのに [FAKE](https://fake.build/index.html) のモジュールがいくつか必要だったので NuGet から取得する形にした。

- [NuGet Gallery | Fake.Core.Trace 6.0.0](https://www.nuget.org/packages/Fake.Core.Trace)
- [NuGet Gallery | Fake.DotNet.Cli 6.0.0](https://www.nuget.org/packages/Fake.DotNet.Cli)
- [NuGet Gallery | Fake.IO.FileSystem 6.0.0](https://www.nuget.org/packages/Fake.IO.FileSystem)

FAKE はいつか勉強せなあかんなと思っていたが、タイミング良く使う機会が来た。その良さもなんとなく感じており、なんでも F# で書きたくなる気持ちがわかってくる。

先述の WebSocket でやり取りする JavaScript は、デプロイするブツには不要だ。
なので今回開発モードをこしらえて、その場合のみ WebSocket 用の JavaScript をロードする `script` タグを差し込むようにした。
開発モード自体は SSG 実行時のオプションで渡す形にしている。設定ファイルなしでスクリプトだけで完結する世界観にしたいので。

この開発用 JavaScript も、そのうち [Fable.Browser.WebSocket](https://github.com/fable-compiler/fable-browser/tree/master/src/WebSocket) で書いてトランスパイルしたものを使うようにしたい。

一通り作ってみて、いい感じに動いてるっぽい。
ただし強烈に遅いのと、 Suave で 404 ページを表示する方法がわからないので、今後の課題かな。差分ビルドとかできるとかっこよい。

以下に今回のハマりどころを記載しておく。

### ハマりどころ

Suave 、 Fable 共にそうだけどドキュメントに書いてないことがチラホラあるので、分からなくてコード読む必要があった。備忘のためいくつかメモを記録しておく。

#### Suave

Suave で以下のエラーが出たときは、権限じゃなくてサーバの 設定が足りない。

```log
System.UnauthorizedAccessException: Access to the path 'C:\Program Files\dotnet\sdk\7.0.302\FSharp\_temporary_compressed_files' is denied.
```

これを解消するには権限を与えるのではなく、 Sauve がデフォルトで使ってる圧縮ファイルのパスを config に指定しないといけない(デフォでなんかやってくれよめんどくせえ)。
[suave/Web.fs at 8efe4b32ea0dc52f36c10c8d8fec8191c6ae901c · SuaveIO/suave](https://github.com/SuaveIO/suave/blob/8efe4b32ea0dc52f36c10c8d8fec8191c6ae901c/src/Suave/Web.fs#L42)

```fs
compressedFilesFolder = Some(home)
```

#### Fake.DotNet

`Fake.DotNet` の使い方は [SAFE-Stack のテンプレ](https://github.com/SAFE-Stack/SAFE-template) が参考になる。
[SAFE-template/Build.fs at db892ace1ecd1589a9a03a484e388a97f3b71718 · SAFE-Stack/SAFE-template](https://github.com/SAFE-Stack/SAFE-template/blob/db892ace1ecd1589a9a03a484e388a97f3b71718/Build.fs#L70-L75)

`command` と `args` どう分けるか謎だが、あんまきにしなくていいっぽい。
現に ↓ のように `cmd` に引数が混ざっていても動くという。

```fsharp
let cmd = "fable src"
let args = "--runScript dev" // NOTE: run script with development mode.
let result = DotNet.exec (fun x -> { x with DotNetCliPath = "dotnet" }) cmd args
```

#### Fable

Fable に `--runScript` をつけて呼び出したときに引数を与えられるのかについては、ドキュメントの記載は見つけられなかったがソースコードを見る限り与えることができる。
[Fable/src/Fable.Cli/Entry.fs at ac4d44997f69d5b1b7109730ccb45e354a4ec368 · fable-compiler/Fable](https://github.com/fable-compiler/Fable/blob/ac4d44997f69d5b1b7109730ccb45e354a4ec368/src/Fable.Cli/Entry.fs#L390)

この形式なので、以下のようにコマンドを打てばよい。

```sh
dotnet fable src --runScript dev
```

`--runScript` で実行するスクリプトの方では、 Node.js の引数の取り方を使えば良い。つまり `process.argv` を使う。

[Fable.Node](https://github.com/fable-compiler/fable-node) を使ってたら `process.argv` が簡単に使える。
でも `process` は F# の予約語で警告が出るので、気持ち悪いからそのうち自前で binding するように直すかも。
