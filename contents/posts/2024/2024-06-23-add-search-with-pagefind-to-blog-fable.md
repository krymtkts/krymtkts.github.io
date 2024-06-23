---
title: "blog-fable に pagefind による検索を追加する"
tags: ["pagefind", "fable", "fsharp"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 放置気味だが、今日も [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の話を書く。

[このブログ](https://github.com/krymtkts/krymtkts.github.io)は結構自身の備忘のために役立っている。
例えば [chocolatey](https://chocolatey.org/) で OpenSSH をインストールするオプションを確認するときのような、たまにしかやらないことを探すのにとても役立つ。
ただ書くときは探しやすさに注意を払わずタイトルを書いてることもあって、どこに書いたか忘れて、あとからそういった情報を探すのにいちいち数記事読み返すときもある。
そんなときは検索機能が欲しくなるのだけど、あんまり日本語がいい感じで、軽量な静的コンテンツの検索ってなさそうだったので放置していた。

でも最近 [pagefind](https://pagefind.app/) の日本語が良くなってきていると知り、また導入も簡単だということなのでやってみた。

---

blog-fable に pagefind を導入するのに以下を行った。

- task runner に `pagefind` の実行を追加する
- 検索を最適化するための設定を作成し、 metadata を追加する
- pagefind UI の初期化コードを仕込む
- pagefind UI のスタイル調整する

### task runner に `pagefind` の実行を追加する

まず `pagefind` の実行だが、やり方は至極簡単。
概ね pagefind の文書に書いてあるとおりに index を構築して GitHub Pages に deploy したらいいだけ。

[Running Pagefind | Pagefind — Static low-bandwidth search at scale](https://pagefind.app/docs/running-pagefind/)

当然静的コンテンツの作成後出ないといけないので、コマンドの実行順にだけ気をつけたらいい。
[こんなふう](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/package.json#L4-L14)にした。
元々 `build` task に引数を渡して開発と本番を切り分けてたが、直接引数を渡すと最後尾の `build-index` に適用されてしまうため、仕方なく分割した。

```json
  "scripts": {
    "postinstall": "dotnet tool restore",
    "build-fable": "dotnet fable src --test:MSBuildCracker --runScript",
    "build-md": "node ./src/App.fs.js",
    "build-css": "sass --style=compressed --no-source-map ./sass/style.scss ./docs/blog-fable/css/style.css",
    "build-index": "pagefind",
    "build": "npm run build-css && npm run build-fable && npm run build-index",
    "build-dev": "npm run build-css && npm run build-fable dev && npm run build-index",
    "serve": "dotnet fsi ./dev-server.fsx /blog-fable",
    "dev": "npm run build-dev && npm run serve"
  },
```

### 検索を最適化するための設定を作成し、 metadata を追加する

次は重要な点で、静的コンテンツからどう index 構築するかの設定する必要があった。
デフォルトだと何もかも indexing してしまうので、どの要素で構築すべきか、どのパスで構築すべきかの設定しないと検索のノイズがすごかった。
幸い [pagefind は設定ファイルをサポートしている](https://pagefind.app/docs/config-sources/)ので、このような面倒な設定はすべてファイルにまとめられる。
[こんなふう](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/pagefind.yaml#L1-L5)にした。

```yaml
site: ./docs/blog-fable
root_selector: .content
keep_index_url: true
glob: "{posts,pages}/*.html"
```

[`site`](https://pagefind.app/docs/config-options/#site) は対象となる静的コンテンツのディレクトリ。
[`glob`](https://pagefind.app/docs/config-options/#glob) で指定するパターンと組み合わせて対象の静的コンテンツを指定できる。
blog-fable は `posts/` `pages/` 以外を対象にしたくない(404 だったり archives だったりは不要)のでこのオプションがなかったら終わってた。

blog-fable では [`root_selector`](https://pagefind.app/docs/config-options/#root-selector) を使ったが、文書では `data-pagefind-body` を使うべきで `root_selector` は余り使う必要ないと書いてあった。
ただ blog-fable は Markdown から翻訳された HTML はすべて `.content` 配下に出力されるので、むしろこのオプションの方がハマっていた。
一箇所だけ front matter なし page だと title 要素を拾えなかったので、しゃーなしで metadata [`data-pagefind-meta="title"`](https://pagefind.app/docs/metadata/) を追加した。
[こんなふうに](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/src/Common.fs#L49-L52)。

```fsharp
                let meta = if l = 2 then " data-pagefind-meta=\"title\" " else ""

                $"""<h%d{l} %s{meta}><a name="%s{escapedText}" href="#%s{escapedText}">%s{text}</a></h%d{l}>"""
```

[`keep_index_url`](https://pagefind.app/docs/config-options/#keep-index-url) は `index.html` を取り除くかどうか。 blog-fable では top page 以外にないので不要だが、万が一当てはまるパターンが増えたときに加工されるのはかなんのでつけた。

### pagefind UI の初期化コードを仕込む

次はサイトで検索を使うための初期化コードを仕込む。
オプションの指定で検索に表示される文言を変えたりするので、指定項目は多いが一通り指定するのが無難だろう。

ここで注意すべき設定項目は [`baseUrl`](https://pagefind.app/docs/search-config/#base-url) の指定だ。
blog-fable のような GitHub アカウントの page じゃない場合 URL が `https://${account}.github.io/${repo-name}/` となる。
そのためそれに合わせて `baseUrl` を設定する必要がある。
このブログなら GitHub アカウントの page なので `"/"` で、 blog-fable は `"/blog-fable/"` だ。

あと blog-fable 固有の対応として、 初期化の JavaScript を F# に翻訳する必要がある。
[Sample のコード](https://pagefind.app/docs/ui-usage/#adding-the-pagefind-ui-to-a-page)を[こんなふう](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/src/Handler.fs#L36-L66) にした。

```fsharp
type PagefindUI =
    [<Emit "new $0($1, $2)">]
    abstract Create: obj -> unit

[<Global>]
let PagefindUI: PagefindUI = jsNative

window.addEventListener (
    "DOMContentLoaded",
    (fun _ ->
        let elm = document.querySelector "#search"

        if isNull elm then
            ()
        else
            PagefindUI.Create(
                !!{| element = "#search"
                     baseUrl = "/blog-fable/"
                     pageSize = 5
                     translations =
                      !!{| placeholder = "Search"
                           clear_search = "Clear"
                           load_more = "More"
                           search_label = ""
                           zero_results = "\"[SEARCH_TERM]\" now found."
                           many_results = "\"[SEARCH_TERM]\" ([COUNT])"
                           one_result = "\"[SEARCH_TERM]\" ([COUNT])"
                           searching = "Searching \"[SEARCH_TERM]\"..." |} |}
            ))
)
```

`new PagefindUI` は `pagefind-ui.js` を読み込んでいれば global に展開されているので、 Fable の [Emit](https://fable.io/docs/javascript/features.html#emit-when-f-is-not-enough) で呼び出すのが多分いちばん楽。
`new PagefindUI` の引数は本来なら型をちゃんと書いた方が良いが、どのオプションを使うべきか定かでなかったのもあり横着して匿名レコードをぶち込んでいる。
あと pagefind UI の埋め込み対象の要素は Archives ページにだけ作るようにしたので、その要素がない場合のエラー避けをした(これも横着)。

### pagefind UI のスタイル調整する

最後に pagefind UI のスタイルを調整する。
[CSS custom parameter が提供されている](https://pagefind.app/docs/ui-usage/#customising-the-styles)ので基本それでスタイル調整する。
light と dark の 2 テーマあるので[ここ](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/sass/style.scss#L92-L101)と[ここ](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/sass/style.scss#L145-L154)にで CSS custom parameter を上書きしている。
当然、 `pagefind-ui.css` の読み込み後じゃないと上書きできないので、読み込み順も注意する。

ただ CSS custom parameter でできることが限定的で、あまり納得行く出来にならない。
なので必要に応じて独自に CSS を書くのが妥当だろう。いまはまだやってないけど。
検索結果の highlight が `mark` 要素で囲まれたり、検索フィールドにフォーカスすると枠が太くなるのとか、デザインの調和が取れてないと個人的に感じる点は今後いじっておきたい。

### その他

あとこれ以外にも blog-fable 固有の問題があった。
それは開発サーバとして使ってる [Suave](https://github.com/SuaveIO/suave) がデフォルトで未知の拡張子のファイルを配信できず 404 になるというものだ。
一度知ってしまったら簡単だけど、これは結構調べるのに時間がかかった。

デフォの MIME type → [suave/src/Suave/Combinators.fs](https://github.com/SuaveIO/suave/blob/d9deb5f4f973fd21d15bdd7e85ac9c0bee05baab/src/Suave/Combinators.fs#L87-L112)

拡張はこうやる。
[suave/examples/Example/Program.fs](https://github.com/SuaveIO/suave/blob/d9deb5f4f973fd21d15bdd7e85ac9c0bee05baab/examples/Example/Program.fs#L79-L80)
拡張した MIME type の設定を Suave の [`startWebServer`](https://github.com/SuaveIO/suave/blob/d9deb5f4f973fd21d15bdd7e85ac9c0bee05baab/src/Suave/Web.fs#L71) に渡したらいい。

pagefind は生成した Wasm に付与した 4 つ独自の拡張子を持っているので、それらを `application/octet-stream` で配信するものだと明示してやらないといけない。
[こんなふう](https://github.com/krymtkts/blog-fable/blob/5a82f1283aefdde9d8a98823a060a050ce1efef3/dev-server.fsx#L143-L158)にした。

```fsharp
let cfg =
    { defaultConfig with
        homeFolder = Some(home)
        compressedFilesFolder = Some(home)
        bindings = [ HttpBinding.create HTTP IPAddress.Loopback port ]
        listenTimeout = TimeSpan.FromMilliseconds 3000.
        mimeTypesMap =
            Writers.defaultMimeTypesMap
            // NOTE: Add custom mime types for pagefind to prevent 404 error.
            @@ ((function
            | ".pagefind"
            | ".pf_fragment"
            | ".pf_index"
            | ".pf_meta" -> Writers.createMimeType "application/octet-stream" false
            | _ -> None)) }
```

(ちなみに Suave のページが不安定みたい)

---

これで pagefind の設定は第 1 回戦終了って感じ。
まだ気に入らない点あるし検索の使い勝手も検証してないけど、ないよりいいやろ。
ちょくちょく改善入れていきたい。
