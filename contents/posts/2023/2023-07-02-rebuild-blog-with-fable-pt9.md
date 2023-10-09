---
title: "Fable でブログを再構築する pt.9"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

細々とした修正だけやった。 [#23](https://github.com/krymtkts/blog-fable/pull/23)

O'reilly の F# 本で勉強してたからか最近まで [Interpolated strings - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/interpolated-strings) わかってなかったのでそれに書き換えたり。あと [Fable.Node](https://github.com/fable-compiler/fable-node) の process 予約語で警告が出るやつも自前でかいた binding を使うことで解消した。
利用する範囲そんなに多くなければ最小の範囲に絞り込んで Node.js の binding を書き Fable.Node 排除するのもよいが、なんとなく先延ばしにした。

RSS Feed に取り組み始めた。
まずは RSS feed の構造を知らんかったので、自ブログの出力した [feed.xml](https://krymtkts.github.io/feed.xml) と [RSS 2.0 specification](https://validator.w3.org/feed/docs/rss2.html) [RFC 4287 - The Atom Syndication Format](https://datatracker.ietf.org/doc/html/rfc4287) を学んでる。
自ブログは RSS 2.0 で書かれていて `atom:link` セクションを使うことで追加の機能を入れてるらしい。

[RSS Validator](https://www.rssboard.org/rss-validator/) なんてのも初めて知ったわ。試しに feed.xml を読ませてみたところ valid な RSS Feed と判定されてるが推奨事項がチラホラあった。
Fable でやるにあたってこういうのも丁寧に対応できると良いな。

feed.xml を作るにあたって sitemap.xml を書いたのと同じく自前で [Fable.SimpleXml](https://github.com/Zaid-Ajaj/Fable.SimpleXml) を使って書くのか、楽に Node.js のモジュールを使うのかがちょっと悩ましいところ。

- [jpmonette/feed: A RSS, Atom and JSON Feed generator for Node.js, making content syndication simple and intuitive! 🚀](https://github.com/jpmonette/feed)
- [dylang/node-rss: RSS feed generator for Node.](https://github.com/dylang/node-rss)

この辺りが有名なモジュールのようだけど最後のコミットがちょっと古い。
[feed](https://github.com/jpmonette/feed) の場合以下みたいにオブジェクトをもりもり書いていくだけっぽくて、 Fable.SimpleXml 使って自分で構築するのと変わらんのでは...
という懸念があり、なんか自分でやったほうが良さそうに思っている。 RSS/Atom の知識乏しいけど、既存の焼き直しであればまあできるやろ的な。

```ts
// 以下はあくまでサンプルだがこれは書いてて楽しくなさそうな気配
// feed
const feed = new Feed({
  title: "krymtkts",
  description: "krymtktss blog",
  id: "http://krymtkts.github.io/",
  link: "http://krymtkts.github.io/",
  image: "http://krymtkts.github.io/image.png",
  favicon: "http://krymtkts.github.io/favicon.ico",
  copyright: "Copyright © 2019-2023 krymtkts",
  updated: new Date(2023, 7, 02),
  feedLinks: {
    json: "https://krymtkts.github.io/json",
    atom: "https://krymtkts.github.io/atom",
  },
});

// node-rss
var feed = new RSS({
  title: "krymtkts",
  description: "krymtktss blog",
  site_url: "http://krymtkts.github.io",
  image_url: "http://krymtkts.github.io/icon.png",
  copyright: "Copyright © 2019-2023 krymtkts",
  pubDate: "May 20, 2012 04:00:00 GMT",
  feed_url: "http://krymtkts.github.io/rss.xml",
});
```

```fsharp
let generateRssFeed (conf: RssFeed) =
    let items = ...

    let rss =
        {
            title = conf.title
            description = conf.description
            link = conf.link
            lastBuildDate = now.ToString("yyyy-MM-dd")
            generator = "blog-fable"
            items = items
        } |> convertToSimpleXml

    rss
    |> serializeXml
    |> (+) @"<?xml version=""1.0"" encoding=""UTF-8""?>"
```

イメージ雑いけどこんな感じにならんかな？
自前でやることによって進捗が鈍化しそうやけど、しゃーないかという感じ。
