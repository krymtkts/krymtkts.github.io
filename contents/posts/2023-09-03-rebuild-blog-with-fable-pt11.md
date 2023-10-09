---
title: "Fable でブログを再構築する pt.11"
tags: ["fsharp", "fable", "bulma"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

ここ 1 ヶ月ほどでやりたかったことを 3 つ進められた。エライ(低ハードル)。

- ややこしくなってた入出力パスの構築 [#38](https://github.com/krymtkts/blog-fable/pull/38) [#39](https://github.com/krymtkts/blog-fable/pull/39)
- [PageSpeed Insights](https://pagespeed.web.dev/analysis/https-krymtkts-github-io-blog-fable/vfo1picz8u?form_factor=mobile) のスコア改善 [#42](https://github.com/krymtkts/blog-fable/pull/42)
- カラースキームの導入 [#43](https://github.com/krymtkts/blog-fable/pull/43)

以下のとおり結果的に結構良いスコア叩き出すようになった。

| platform | Performance | Accessibility | Best Practices | SEO |
| -------- | ----------: | ------------: | -------------: | --: |
| Mobile   |          99 |            93 |            100 | 100 |
| Desktop  |         100 |            93 |            100 | 100 |

[Solarized](https://ethanschoonover.com/solarized/) が好きなのだけど今回は Dark にしてみた。エディタやターミナルなんでも Solarized Dark にして 10 年近いのでついにブログも Dark に。
まだいい感じに色調整できてないから Accessibility をちょい下げてしまってるが、後ほど調整する。

今回やった中には、初体験でちょっと調べる必要があるものもあった。
これらはメモがてら以下に記しておく。全然 F# ネタではないけど。

いずれも上手く拡張できる様になっており楽だった。感心するわ。

---

### Marked の Renderer を拡張する

[The Renderer - Marked Documentation](https://marked.js.org/using_pro#renderer) に示されるように `Renderer` オブジェクトにわたすオプションで拡張できる。

`listitem(string text, boolean task, boolean checked)` と `checkbox(boolean checked)` の組み合わせにちょっと癖があった。
どっちも定義しているとどっちも呼ばれてチェックボックスが 2 個になってしまう。
`checkbox(boolean checked)` 側を無効化するような書き方をして `checkbox` 要素を `label` 要素で包み込んだ。
シグネチャが `checkbox(boolean checked)` なもんで、こっち側は `label` 要素書こうにもテキスト不明でできないねんよな。
ブログの場合 `checkbox` 要素が単独で使用されることまずないからこうしたけど、なんかもっと良い方法がないのかは気になるところ。

```fsharp
            let listitem =
                fun text task check ->
                    let checkState =
                        match check with
                        | true -> "checked"
                        | false -> ""

                    // NOTE: input を label で包んで関連を持たせる
                    match task with
                    | true ->
                        $"""<li><label class="checkbox"><input type="checkbox" class="checkbox" disabled {checkState} />{text}</label></li>"""
                    | false -> $"""<li>{text}</li>"""

            let checkbox =
                fun _ ->
                    // NOTE: checkbox は list と一緒に使われる前提と考えて何も返さない
                    ""

            let mops =
                !!{| heading = heading
                     link = link
                     listitem = listitem
                     checkbox = checkbox |}


            jsOptions<Marked.MarkedExtension> (fun o ->
                o.renderer <- Some <| U2.Case2 mops
                o.gfm <- Some true
                o.headerIds <- Some true)
```

### Bulma の自前 style をビルドする

[With node-sass | Bulma: Free, open source, and modern CSS framework based on Flexbox](https://bulma.io/documentation/customize/with-node-sass/)

概ねこの手順の通りにやればいい。

ただし以下の非推奨があって、それらは自前で最新のものに置き換える必要があった。

- [`node-sass`](https://www.npmjs.com/package/node-sass) は非推奨、 Dart 製の [`sass`](https://www.npmjs.com/package/sass) に変える
- Sass では `@import` は非推奨、 `@use` に書き換える
  - 余談だが commit message には backquote で囲わずにこれらのキーワードを書いてたら GitHub 上でホバーしたときにその ID の方々がポップアップされて笑ってしまった

これらは Sass の最新仕様に追随するよう自前でこしらえた。

### `node-sass` を `sass` に

変えるのは大したことないけど、 cli インタフェースのオプションも変わってるので、そこはケアする必要がある。
ついでに出力される CSS を圧縮するオプション `--style=compressed` もつける。今回のケースでは空白が消えたことで 3KB くらい節約できた。

```patch
   "scripts": {
     "postinstall": "dotnet tool restore",
-    "css-build": "node-sass --omit-source-map-url ./sass/style.scss ./docs/blog-fable/css/style.css",
+    "css-build": "sass --style=compressed --no-source-map ./sass/style.scss ./docs/blog-fable/css/style.css",
     "serve": "dotnet fsi ./dev-server.fsx",
     "build": "dotnet fable src --runScript",
     "dev": "npm run build dev && npm run serve"
```

### `@import` を `@use` に

`@use` を使う場合は、変数のデフォルト値を書き換えるには先述の手順のとおりにはできない。
[Default Values | Sass: Variables](https://sass-lang.com/documentation/variables/#default-values) の示す通り `@use <url> with (<variable>: <value>, <variable>: <value>)` を使う。以下書き換えイメージ。

```patch
 @charset "utf-8";

 // NOTE: based on https://ethanschoonover.com/solarized/
 $base03 :#002b36;
 $base02 :#073642;
 $base01 :#586e75;
 $base00 :#657b83;
 $base0 :#839496;
 $base1 :#93a1a1;
 $base2 :#eee8d5;
 $base3 :#fdf6e3;
 $yellow :#b58900;
 $orange :#cb4b16;
 $red :#dc322f;
 $magenta :#d33682;
 $violet :#6c71c4;
 $blue :#268bd2;
 $cyan :#2aa198;
 $green :#859900;

-@import "../node_modules/bulma/bulma.sass";
-// NOTE: Solarized Dark.
-$black: $base3;
-$black-bis: $base2;
-$black-ter: $base2;
-$grey-darker: $base1;
-$grey-dark: $base1;
-$grey: $base0;
-$grey-light: $base00;
-$grey-lighter: $base01;
-$grey-lightest: $base01;
-$white-ter: $base02;
-$white-bis: $base02;
-$white: $base03;
-// NOTE: set alternative colors.
-$turquoise: $cyan;
-$purple: $violet;
+@use "../node_modules/bulma/bulma.sass" with (
+    // NOTE: Solarized Dark.
+    $black: $base3,
+    $black-bis: $base2,
+    $black-ter: $base2,
+    $grey-darker: $base1,
+    $grey-dark: $base1,
+    $grey: $base0,
+    $grey-light: $base00,
+    $grey-lighter: $base01,
+    $grey-lightest: $base01,
+    $white-ter: $base02,
+    $white-bis: $base02,
+    $white: $base03,
+    // NOTE: set alternative colors.
+    $turquoise: $cyan,
+    $purple: $violet,
+);
```

---

もうちょっと PageSpeed Insights のスコア上げられる雰囲気はあるので、そこだけ粘ってみる。
結構 Fable 製ブログも出来上がってきたし、いよいよ自ブログ移行に向けた試行をはじめられそう。
