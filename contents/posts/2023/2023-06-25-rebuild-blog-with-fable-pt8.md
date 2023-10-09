---
title: "Fable でブログを再構築する pt.8"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

先日の開発サーバ作成、 404 に生成済み HTML 表示するのもわかったので完了した。[#20](https://github.com/krymtkts/blog-fable/pull/20)

再ビルドのパフォ改善は、今やらないと言うかできないと判断した。
指定のファイル一覧だけを対象に HTML や関連する XML を再生成するって感じの機構を取り入れる必要があって、根本的に変えないとどうにもならんので、一通り必要と考える機能が揃ってから着手したい。

次に XML の類に取り掛かった。まずはデータ量少ない sitemap.xml のほう。 [#22](https://github.com/krymtkts/blog-fable/pull/22)

sitemap.xml に関しては何の XML か知らなかったが検索エンジンのクローラにより正確な情報を与えるためのデータらしいのは理解した。
今の Cryogen で生成されてる sitemap.xml がめちゃくちゃテキトーに作られてるのも含め。
こちらを参考に知識を補った。
[Build and Submit a Sitemap | Google Search Central  |  Documentation  |  Google for Developers](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap)

我がブログの運用に限れば、計測タグも設置してないし何 1 つ SEO 効果を期待してないけど、 wev.dev の PageSpeed Insights とかで良いスコアは出したい。
ハイスコア狙いのゲーム感覚なのだ。
だからこそいま全部同じ `loc` の全部同じ `lastmod` が出てるので、 `loc` を URL 毎にするとか `lastmod` に最終更新日を反映させるとかちょっとはマシにしたい。

折角なので `priority` も定めてみる。とりま index は `1.0` として Archives, Tags はカテゴリ系なので生成の度更新するし `0.9` としてみた。
新しめのページは高い方が良いんか？
結局全てが生成ファイルなので単純に post は投稿日付降順くらいしかできなそうやけど、ちょっとめんどいなあと思ったので今は控えて全部同じ `0.8` とした。
代わりに投稿日付とは別に front matter に日付を持たせて、更新日付ぽい扱いができるよう悪あがきだけしておいた。

Fable で XML を操作するのには [Zaid-Ajaj/Fable.SimpleXml](https://github.com/Zaid-Ajaj/Fable.SimpleXml) を使う。 [Feliz](https://github.com/Zaid-Ajaj/Feliz) 作者の作品。
使いたい機能は大体誰かが用意してくれていてありがたい限りである。

femto で足したらエラーになった。でも `fsproj` はちゃんと変更されてたから大丈夫ぽい。 npm モジュールが無いからエラーになる。
README には `paket` 使って足せと書いてあった(先に読め)。うちは使ってないから単純に `dotnet add` で足す。これでも十分シンプルに使えるし。

```powershell
dotnet add .\src\App.fsproj package Fable.SimpleXml
```

Fable.SimpleXml は XML 宣言の読み込みは対応してるけど出力は対応してないみたいなので、手書き文字列を添えて出力してやる必要があった。
それ以外に関しては Feliz で HTML を書くのと同じ感覚で書ける。
参考として、以下に Fable.SimpleXml を使って sitemap.xml の文字列を作成する箇所を抜粋する。

```fsharp
    let generateSitemap root locs =
        let urls =
            locs
            |> Seq.map (fun loc ->
                node
                    "url"
                    []
                    [ node "loc" [] [ text $"{root}{loc.loc}" ]
                      node "lastmod" [] [ text loc.lastmod ]
                      //   node "changefreq" [] [ text "monthly" ]
                      node "priority" [] [ text loc.priority ] ])
            |> List.ofSeq

        let urlSet =
            node
                "urlset"
                [ attr.value ("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9")
                  attr.value ("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance") ]
                urls

        urlSet
        |> serializeXml
        |> (+) @"<?xml version=""1.0"" encoding=""UTF-8""?>"
```

XML 宣言も文字列なんぞ使わずにサクッと書けるとカッコよいのだけど。

あとコメントアウトがあってダサいが、これは `changefreq` も足したいなーと思ってやらなかった残穢だ。
Index, Archives, Tags は post の投稿間隔から自動算出できるが、いま面倒でやらなかったので戒めとして残している。

これで次は RSS Feed の XML を作ったら最低限必要なものは揃う。スタイルや前後の post へのリンクみたいな細かな調整は必要だろうけど。
RSS Feed の仕組み、例えば既存の投稿に変更がある場合にどうなんの？とかもあんま知らんので、これを機に知識を補いたい。
結構 post 周りのコード手を入れないと Markdown から抽出・生成したタイトル・公開日・ HTML 後続処理に回せんな...
というのがあるので結構ダルそうではあるが、いい感じの手間がかかる点は最後に越える山としてふさわしい(と自分に言い聞かせる)。

場当たり的なコードも積もってきたし、細々とした気になってる点を補正する等も着手していこう。
