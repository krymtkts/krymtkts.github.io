{:title "Fable でブログを再構築する pt.13"
:layout :post
:tags ["fsharp", "fable"]}

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

開発サーバが重いので、諸々調整をしている。

既に対応済みのものとしては、ビルドの軽量化。 [#52](https://github.com/krymtkts/blog-fable/pull/52) [#53](https://github.com/krymtkts/blog-fable/pull/53)

毎回全部ビルドし直す必要はないので、 `*.fs`, `*.md` と `*.scss` どれが変更されたかを判別して、実行するビルドタスクを切り替えるような実装にした。

特に今後ブログのコードをいじることはあまりないだろうから、 Markdown を変更したときのビルド待ちは極力抑えたかった。
その場合 `dotnet fable` でビルド済みの JavaScript があるからそれを実行する形を採用した。

[FAKE](https://fake.build/index.html) には npm コマンドを実行するモジュールがある。

[Npm (Fake)](https://fake.build/reference/fake-javascript-npm.html)

これを利用して直接 `node` を呼び出すことはできないが、 `package.json` に書いておいて `npm run` すれば FAKE から呼び出せる。
こんな感じ ↓ で `build-md` と `build-css` を用意して FAKE から呼び出すだけ。 (ちょっとタスク名は整理した方がいいか)

```json
  "scripts": {
    "postinstall": "dotnet tool restore",
    "build-css": "sass --style=compressed --no-source-map ./sass/style.scss ./docs/blog-fable/css/style.css",
    "serve": "dotnet fsi ./dev-server.fsx",
    "build-fable": "dotnet fable src --runScript",
    "build": "npm run build-css && npm run build-fable",
    "dev": "npm run build dev && npm run serve",
    "build-md": "node ./src/App.fs.js"
  },
```

```fsharp
// 実行は非常に簡素なインタフェース
Npm.run "build-md" id
```

Npm(FAKE) があれば、 `dotnet fable` に関しても `npm run` 経由で呼べばよいのだけど、 1 つ難点がありそのまま DotNet モジュールを利用している。
それは実行結果を得られないところだ。
コマンドの成否を以て何かしている訳では無いが、わざわざ情報を劣化させる理由もないのでそのままステイだ。

毎回 `dotnet fable` することがなくなったから、 Markdown の編集だけならかなり快適になったはず。

---

次に、適切に処理していなかった WebSocket 接続の手当をしている話。 [#53](https://github.com/krymtkts/blog-fable/pull/53)

これちょっと難しくて、 WebSocket が難しいのでなくて Suave でクライアントからメッセージを受けつつサーバ起点で何かするのが厄介で、手こずってる。

ひとまずやりたかった「Suave でクライアントからメッセージを受けつつサーバ起点で何かする」に関しては 2 つのループで実現できるのがわかったのでそれを使った。
これを参照した。 → [How to implement server-push over websocket in suave? · Issue #307 · SuaveIO/suave](https://github.com/SuaveIO/suave/issues/307)

2 つのループを設けることで、クライアントが切断したときにサーバ側の WebSocket ハンドラを終了するのと、サーバ側の変更イベントでクライアントにプッシュする、が同時に実現できる。
ただまだ駄目なところがあって完成ではない。現時点の Suave の WebSocket のハンドラは以下の通り。

```fsharp
    let socketHandler (ws: WebSocket) _ =
        let mutable loop = true

        Async.Start(
            async {
                while loop do // ループ①
                    do! refreshEvent.Publish |> Async.AwaitEvent

                    printfn "fire event."

                    let seg = ASCII.bytes "refreshed" |> ByteSegment
                    do! ws.send Text seg true |> Async.Ignore
            }
        )

        socket {
            while loop do // ループ②
                let! msg = ws.read ()

                match msg with
                | (Close, _, _) ->
                    let emptyResponse = [||] |> ByteSegment
                    do! ws.send Close emptyResponse true
                    printfn "WebSocket connection closed gracefully."
                    loop <- false
                | _ -> ()
        }
```

何が駄目かって、 `Async.AwaitEvent` するところで非同期ブロックが入るのだけど、その後クライアントから切断を受信して `loop` 変数の値を変更・ループ ② を止めても、ループ ① は非同期ブロックされてて次の変更イベント発火まで生き続けるのよね。
なので画面遷移やリロードのたびに変更イベントの待ちが溜まっていって、変更イベントで一気にドバっと流れるというのになってる。

因みに `Async.AwaitEvent` のあとで `if` なり仕込んで抜けられるかなと試したけど、同じ周回だと `loop` 変数への変更が伝播してなくて止まらなかった。 `ref` をあえて使ったらまた違うのかな。

F# の async/await ちゃんと触ってないから手こずってるけど、これ上手く手なづけたら [pocof](https://github.com/krymtkts/pocof) へも良い影響出せそうなので、これを気にちゃんとやってこう。
