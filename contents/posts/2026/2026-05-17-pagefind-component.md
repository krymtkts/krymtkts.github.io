---
title: "Pagefind Component に切り替えた"
tags: ["pagefind", "fsharp", "fable"]
---

ちょっと前に、この blog の全文検索で [Pagefind の Component](https://pagefind.app/docs/components/) を使うように切り替えた。

きっかけは以下のログが出力されてたから。
box が日本語 font だとサイズが崩れるので、上辺底辺を省いている。

```plaintext
│  Pagefind found references to the Default UI (pagefind-ui.js)           │
│  on your site. The Default UI is supported and will continue            │
│  to work.                                                               │
│                                                                         │
│  As of 1.5.0, if you are setting up a new integration, use the          │
│  Component UI instead. It includes a search modal, better               │
│  accessibility and customization: https://pagefind.app/docs/search-ui/  │
```

2026-04 に しばらく無視してたのだけど [pslrm](https://github.com/krymtkts/pslrm) の開発が落ち着いたタイミングで処理した。
Pagefind 1.5.0 から [Web Component](https://developer.mozilla.org/en-US/docs/Web/API/Web_components) を利用した UI が提供される用になったみたい。

[Release v1.5.0 · Pagefind/pagefind · GitHub](https://github.com/Pagefind/pagefind/releases/tag/v1.5.0)

はじめは既存の UI を尊重して [`<pagefind-searchbox>`](https://pagefind.app/docs/components/searchbox/) にした。 [#481](https://github.com/krymtkts/blog-fable/pull/481)
でも折角だしより検索非利用時の UI が simple な [`<pagefind-modal>`](https://pagefind.app/docs/components/modal/) に寄せた。 [#482](https://github.com/krymtkts/blog-fable/pull/482)

[Feliz](https://github.com/fable-hub/Feliz) 的には Web Component のような [custom element](https://developer.mozilla.org/en-US/docs/Web/API/Web_components/Using_custom_elements) は `HtmlHelper.createElement` を使う必要がある。
現状だと `nav` に組み込んだので `li` に内包する形で以下のようになった。

```fsharp
                Html.li [
                    HtmlHelper.createElement "pagefind-config" [
                        prop.custom ("bundle-path", $"%s{pathRoot}/pagefind/")
                        prop.custom ("base-url", $"%s{pathRoot}/")
                        prop.custom ("lang", "en")
                    ]
                    HtmlHelper.createElement "pagefind-modal-trigger" [
                        prop.custom ("shortcut", "/")
                        prop.custom ("compact", "true")
                    ]
                    HtmlHelper.createElement "pagefind-modal" []
                ]),
```

`nav` に [Bulma](https://bulma.io/) の [`tabs`](https://bulma.io/documentation/components/tabs/) class を付与して tab として扱ってる。
custom element はその style の適用外で見た目が整わないので、個別に調整を入れた。

```scss
.tabs li:has(> pagefind-modal-trigger) {
  display: flex;
  align-items: center;
}

.tabs li > pagefind-modal-trigger {
  display: flex;
}
```

あとは posts, pages に加えて booklogs も全文検索対象にすることで、 `nav` に検索がついてても自然な感じにした。

```diff
 site: ./docs/
 root_selector: .content
 keep_index_url: true
-glob: "{posts,pages}/*.html"
+glob: "{posts,pages,booklogs}/*.html"
```

コレで見た目が simple で結構便利な全文検索 modal を blog に生やすことができた。
元々の Pagefind だと modal に必要な修正が多くて面倒で採用してなかったのもあり、 1.5.0 から非常に使いやすくなったなという印象。
特に体感はしてないけど CJK の indexing も改善してるらしいし。
GitHub Pages(Netlify) だと検索に必要なファイル取得も含め結構遅いのだけが難点だが、そこは仕方ない。

最後にこの local での [krymtkts.github.io](https://github.com/krymtkts/krymtkts.github.io) のログには `pagefind-ui.js` に依存してるってログが出たままになってた。
`pagefind-ui.js` への参照はもうないけどなんでかと思ってたが、過去に間違ってつくったタイトルの post の生成物が残ってたからだった。
これは blog-fable の方で直した方がいいかも知れん。
