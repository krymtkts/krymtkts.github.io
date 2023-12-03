---
title: "Fable でブログを再構築する pt.20"
tags: ["fsharp", "fable"]
---

[Fable](https://fable.io/) で作った [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の手直し。

細かすぎる修正は置いといて主だったものだけ記す。

---

もう 2 週前になるのだけど、 [highlight.js の Solarized dark の CSS](https://github.com/highlightjs/highlight.js/blob/6317acd780bfe448f75393ea42d53c0149013274/src/styles/base16/solarized-dark.css) を CDN から引っ張ってきてたけど、 blog-fable の生成物へ同梱するようにした。 [#112](https://github.com/krymtkts/blog-fable/pull/112)

CDN に分かれてたら `package.json` のバージョン更新に追随し忘れることもあるし、試した感じどうも GitHub Pages だとまとめて配信するほうが速いっぽいし。

`node_modules` 配下の `highlight.js` には minified な CSS が用意されてるのでそれをそのままコピった。
SCSS もあったので、試しに自前で `sass --style=compressed --no-source-map` 指定してビルドしてみたけど、ビミョーにデカくなったのでやめた。
これは highlight.js をチラ見した感じ `sass` は使ってないから。
スタイルを編集してること無いしそのまま利用で OK 。

これで外部への依存がリンクくらいしかなくなったはず。随分とスッキリしたな。

---

あと Markdown の脚注を有効にするの忘れてたので、 追加した。
過去の記事で数カ所脚注を使ってて、今後もたまに使うだろうしあった方がいいかなあと思ってのこと。

[marked](https://github.com/markedjs/marked) は Markdown の標準に脚注がないから対応してない。
[GitHub Flavored Markdown(gfm)](https://github.github.com/gfm/) みたいに脚注をサポートしたいなら Extension を使うのが筋らしい。

[Is "footnote" supported in marked.js? · Issue #714 · markedjs/marked](https://github.com/markedjs/marked/issues/714)

ちょうど最近 [`marked-footnote`](https://github.com/bent10/marked-extensions/tree/b82985fc0c2c71287d69a9063464a740396ad5f3/packages/footnote) といういいのを作った人がいて、それをそのまま採用させてもらった。
デフォだと脚注にでかいタイトルが付くのだけど、これは accessibility のためっぽい。でもあまりにもでかいし見出しは不要かなと思って水平ラインに置き換えた。スマン。

あと脚注に Markdown の link 記法 `[nanigashi](https://example.com)` を含むと正常に脚注を出力できないバグがあった。
対象の脚注が `[` を含む場合に正規表現で捕まえたい対象を逃してしまうというやつなのだけど、長めのパターンを直して PR するの面倒かもなと思って Issue を送ってみた。 [Issue #23 · bent10/marked-extensions](https://github.com/bent10/marked-extensions/issues/23)
そしたらすぐに対応してもらえて、とても助かった。

使う node module はそんな感じで、次は Fable でどう使うかってところ。
これに関しては、 Marked Extension に限定する限りは fable binding 書くメリットあんまないと感じてて、 `obj` をそのまま使った。

一点 [marked-highlight.js](https://github.com/markedjs/marked-highlight) と違って extension を作成する関数が `export default` になってた。
この場合は対応する `importDefault` で取り込んであげないと対象が見つからない。
(この点 marked-highlight.js は大量に export されてるうちの 1 つだったから `importMember`を使ってる)

以下のように `obj` をパラメータにして `MarkedExtension` を返す定義する。
option の指定は型ありのときと変わらず匿名レコードで行って、

```fsharp
        let markedFootnote: obj -> Marked.MarkedExtension = importDefault "marked-footnote"

        // 使うとき
        let footNote = markedFootnote !!{| description = "<hr />" |}
```

ただし間違ってても検査できないのが玉に瑕。でも局所的だしいちいち書くほどでもないかなと思って。その内気が変わるかもだけど

だいぶ npm module を Fable で使うときの取り回しに慣れてきた気がする～嬉しい。
