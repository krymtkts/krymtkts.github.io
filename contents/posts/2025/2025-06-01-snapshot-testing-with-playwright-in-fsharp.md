---
title: "F# で Playwright の Snapshot testing"
tags: ["playwright", "fable", "fsharp"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) のための snapshot testing を [Playwright for .NET](https://playwright.dev/dotnet/) で作った。

krymtkts/blog-fable は[このブログ](https://github.com/krymtkts/krymtkts.github.io)の基盤となる repo だ。
最近はブログに機能を足すこともまあないので、もっぱら依存関係の更新が主たる修正となってる。
krymtkts/blog-fable に対して [Dependabot Version Updates](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/about-dependabot-version-updates) が作った PR を取り込む。
[krymtkts/krymtkts.github.io](https://github.com/krymtkts/krymtkts.github.io) はその歴史を取り込んで、依存関係が更新される。

この更新される依存関係は [Fable](https://github.com/fable-compiler/Fable) 、 [Feliz](https://github.com/fable-hub/Feliz) 、 [marked](https://github.com/markedjs/marked) 、 [Bulma](https://github.com/jgthms/bulma) 等。
大きな変更があったら、出力される HTML や style 等の内容が壊れるタイプのものなのだ。
ごく稀に描画の結果が変わったりして(確か過去に)、気づかずそのまま出して慌てて週セ、みたいなのがあった。
それで依存関係の更新時は自動で PR を merge せずに、手動で動かしてみて目視チェックして OK なら merge している。
大した手間ではないが、それが面倒なのでどうにかしたいなと考えていた。

こういった HTML の出力結果の違いを検知するには Snapshot testing が最適だ。
Playwright は使いやすさがイケてるとずっと聞いてたが触ったことなかったし、いい機会なので実装してみた。
動機が先述の通りなので、今のところ krymtkts.github.io では使わないつもり。日々の booklog や post を作るのが面倒になるだろうし。

[Snapshot testing by krymtkts · Pull Request #350 · krymtkts/blog-fable](https://github.com/krymtkts/blog-fable/pull/350)

---

今回は snapshot testing にしたが、スタイルも含めた描画結果を visual comparison をするなら screenshot が一番いいだろう。
GitHub では履歴の画像の比較もできるし、そういう意味でも向いてるなと思った。

ただ screenshot にすると描画される端末の影響が少なからずある。
これは自分の開発機で出力した結果と GitHub Actions に作った CI で出力した結果は必ずしも完全一致しない可能性があるということだ。
問題が発生したときいちいち調べるのは極めて面倒なので、今回描画された HTML の snapshot を保存し、それで比較することにした。
残念なことに Playwright for .NET の snapshot は style を含まない。
もし Bulma の出力する CSS が変わるような破壊的な変更だと気付けないことになる。
片手落ち感があるが仕方ないと一旦割り切る。

ちなみに snapshot を取る機能は Playwright for .NET に組み込まれているが、どうも npm の Playwright のように高機能ではないらしい。
[[Question]: Visual comparisons Feature Parity · Issue #1854 · microsoft/playwright-dotnet](https://github.com/microsoft/playwright-dotnet/issues/1854)

ただそれ以前の話 krymtkts/blog-fable では [Expecto](https://github.com/haf/expecto) と Playwright の組み合わせでやったので、提供されてる unit test の base class がない。
[Installation | Playwright .NET](https://playwright.dev/dotnet/docs/intro) に記載があるのは [MSTest](https://github.com/microsoft/testfx) 、 [NUnit](https://github.com/nunit/nunit) 、 [xUnit](https://github.com/xunit/xunit) だけある。
なので [`ToMatchAriaSnapshotAsync`](https://playwright.dev/dotnet/docs/api/class-locatorassertions#locator-assertions-to-match-aria-snapshot) のような便利な asserter が使えない。
仕方ないので、 snapshot の保存と比較は愚直に自前で実装した。
以下にほぼコピペママのコードを示す。
snapshot 毎に test を分けても良かったがこれも考えることが増えそうなので直列で実行する 1 ケースにした。

```fsharp
module Tests

open System
open Expecto
open Expecto.Flip

open Microsoft.Playwright

open DevServer
open Suave
open System.Threading

(*
This tests requires the Playwright CLI to be installed.
ex) PS> ./test/bin/Debug/*/playwright.ps1 install
*)

type DevServer() =
    let home = IO.Path.Join [| __SOURCE_DIRECTORY__; ".."; "docs" |]
    let port = port
    let root = "/blog-fable"
    let webServer = startWebServerAsync (suaveConfig home) (webpart root) |> snd
    let cancellationTokenSource = new CancellationTokenSource()

    do
        Async.Start(webServer, cancellationTokenSource.Token)
        printfn $"Dev server started at http://localhost:%d{port}%s{root}"

    member __.Port = port
    member __.Root = root

    interface IDisposable with
        member __.Dispose() =
            printfn "Stopping dev server..."
            cancellationTokenSource.Cancel()

type IPlaywright with
    member __.NewChromiumPage() =
        task {
            let! browser = __.Chromium.LaunchAsync()
            return! browser.NewPageAsync()
        }

type IPage with
    member __.GotoAndCheck(url: string) =
        task {
            let! response = __.GotoAsync(url)

            match response with
            | null -> return Error "Failed to load page: %s{url}"
            | r when not r.Ok -> return Error "Failed to load page: %s{url}"
            | r -> return Ok r
        }

let snapshotDir = IO.Path.Combine(__SOURCE_DIRECTORY__, "snapshots")

let ensureSnapshotDir () =
    if snapshotDir |> IO.Directory.Exists |> not then
        snapshotDir |> IO.Directory.CreateDirectory |> ignore

let getSnapshotPath (path: string) =
    if "http" |> path.StartsWith then
        failwith "Path should not start with 'http'. Use relative paths instead."

    let fileName = path.Replace("/", "_")
    IO.Path.Combine(snapshotDir, fileName + ".snapshot")

let saveSnapshot (path: string) (content: string) =
    IO.File.WriteAllTextAsync(path, content, Text.Encoding.UTF8)

let loadSnapshot (path: string) =
    task {
        if path |> IO.File.Exists then
            let! content = IO.File.ReadAllTextAsync(path, Text.Encoding.UTF8)
            return content |> Some
        else
            return None
    }


[<Tests>]
let tests =
    testList "snapshot testing" [

        testAsync "comparison" {

            let paths =
                [

                  "/index.html"
                  // ...直に URL を羅列している

                  ]

            use server = new DevServer()
            let baseUrl: string = $"http://localhost:%d{server.Port}%s{server.Root}"
            ensureSnapshotDir ()

            return!
                // TODO: i want to use testTask here, but i don't know how to convert it.
                task {
                    use! playwright = Playwright.CreateAsync()
                    let! page = playwright.NewChromiumPage()

                    for path in paths do
                        let url = baseUrl + path
                        let snapshotPath = getSnapshotPath path

                        printfn "Loading %s..." url

                        let! response = url |> page.GotoAndCheck

                        match response with
                        | Error msg -> failwithf "%s" msg
                        | Ok _ -> ()

                        let locator = "html" |> page.Locator
                        let! content = locator.AriaSnapshotAsync()
                        let! expectedContent = snapshotPath |> loadSnapshot

                        match expectedContent with
                        | Some expectedContent -> content |> Expect.equal $"Content mismatch for {url}" expectedContent
                        | None ->
                            do! saveSnapshot snapshotPath content
                            printfn $"Saved new snapshot for %s{url} to %s{snapshotPath}"

                }
                |> Async.AwaitTask
        }

    ]
```

Fable によって生成された HTML を `file://` scheme で読み込んでも良かったが、 local server 経由にしている。
元々開発用 server も [Suave](https://github.com/SuaveIO/suave) で作ってあるし、それを今回共通化する形で test project に取り入れた。
つまり従来の開発用 server は起動のみを行い、 server の定義本体は test project を参照するようになった。

この開発用 server の流れで、当初は [F# Interactive の script](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/) で snapshot testing も実装しようとしていた。
しかし Playwright の初期設定に実行する `./test/bin/Debug/*/playwright.ps1 install` が `dotnet build` されてる前提で、できなかった。
数年前は回避法があった ↓ ようだが、今では配置が変わったようで使えなかった。
ひょっとしたら別の回避方法があるかも知れないが。
[Playwright in F# scripts · Issue #1590 · microsoft/playwright-dotnet](https://github.com/microsoft/playwright-dotnet/issues/1590)

Playwright for .NET を F# で使うことに関しては、それほど困ることはなかった。
ただ Expecto と Playwright for .NET の組み合わせをあまり御しきれなかった。
Playwright for .NET の機能は概ね [`Task`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task?view=net-8.0) を返すので、それをうまく使うには `testTask` が良いだろうと思ったのだが、ダメだった。
`testTask` は [`ValueTask`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1?view=net-9.0) を期待してたり `use!` で [`IAsyncDisposable`](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync) を期待してたりで、つなぐための glue code が多くなりもうええか、と。
結局 `testAsync` の中で `task` CE を [`Async.AwaitTask`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync.html#AwaitTask) するというイケてなさそうな書き方が最もシンプルになった。
これももっとうまく取り回せる方法があれば変えたい。

---

以上で最低限の簡単な snapshot testing 実装を終えた。
今後これを洗練してく動機はあまりないけど、 `testTask` と Playwright の協調はもうちょっと頑張りたい気もするな。
snapshot の撮り直しも、今は作成済み snapshot を消して実行、という形だけなのでなんかあってもいいかも。でも今のも十分
簡素でよいが。
続くかも。
