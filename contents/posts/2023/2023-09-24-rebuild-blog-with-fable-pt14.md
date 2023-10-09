---
title: "Fable でブログを再構築する pt.14"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。
もういい加減ブログ移行作業着手したいけど、ちまちま直してたら修正スべき箇所見つかってきてちまちまちした更新を続けている。

[前のやつ](/posts/2023-09-17-rebuild-blog-with-fable-pt13.html) ↓。

> 何が駄目かって、 `Async.AwaitEvent` するところで非同期ブロックが入るのだけど、その後クライアントから切断を受信して `loop` 変数の値を変更・ループ ② を止めても、ループ ① は非同期ブロックされてて次の変更イベント発火まで生き続けるのよね。
> なので画面遷移やリロードのたびに変更イベントの待ちが溜まっていって、変更イベントで一気にドバっと流れるというのになってる。

どう解決するかわかりまして、 `CancellationToken` でもって破棄してやれば良いとのことだったのでそうした。 [#54](https://github.com/krymtkts/blog-fable/pull/54)

[Async and Task Programming - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/async#asyncstart)

ようやっと F# で非同期計算する入り口に立った気。

他に細々した更新やバグ修正してたのだけど、手間がかかったのだと依存関係の更新をしてた。
特に Marked と Highlight.js の更新はちょっと気が重かった。
ずっと古い ver に依存しておったことで型定義は `@types/highlightjs` `@types/marked` に依存してたのだけど、それを取り除いて純正の型を使う切り替えが必要だった。 Marked も Highlight.js もえらい前から型提供してたみたい。
コピペ元が古いから自分での確認を怠っており全く気づかなかったか。

- [Release v6.0.0 · markedjs/marked](https://github.com/markedjs/marked/releases/tag/v6.0.0)
- [(parser/docs) Add jsdoc annotations and TypeScript type file (#2517) · highlightjs/highlight.js@c836e3a](https://github.com/highlightjs/highlight.js/commit/c836e3aae667ce8c8f7b28534a25d280587f7d3f)

個別の型定義を取り除いたあとは [ts2fable](https://github.com/fable-compiler/ts2fable) で Fable binding を生成する。

```powershell
npx ts2fable node_modules\highlight.js\types\index.d.ts src/bindings/HighlightJs.fs
npx ts2fable node_modules\marked\lib\marked.d.ts src/bindings/Marked.fs
```

ぐいっと一気に引き上げたので極力型を fable binding を再生成したかったが、結構負担大きかった。
結局生成した binding を手で補正する必要があった。

Highlight.js に関しては再生成した型定義をエラーが出ない範囲で削った。自分が使う極小範囲に留めた binding を作ったわけだ。

Marked はちょっとそう簡単にはいかなかった。びっくりするくらい上手くいかない。
ts2fable って `Omit` とか型の交差を理解しなくて、そのまま F# のコードに出力される。

[Combined and Omit<> types · Issue #474 · fable-compiler/ts2fable](https://github.com/fable-compiler/ts2fable/issues/474)

あとは `Record` とかも理解しない。これらに対する対処を手でやりつつ、非公開な型を削るのしんどかった。
ので、 Marked は元々使ってた `@types/marked` で生成した型から、不要な部分や削除された部分を最小限で削ったものにした。
恐らく利用してない範囲に非互換な型定義があるんじゃないかと思うが、利用範囲だけは手直しして使えるようにした感じ。

Fable の binding を用意する方法、結局 ts2fable を使うよりも自分で更地から書いた方がいいのでは...という気配薄っすら感じる。
が、まったく ts2fable 開発されてないわけじゃないし今のところコミュニティの方向はどこに向いてんだろ。
追々調べておく必要がありそう。そうそう binding て書く機会もないしな。
とはいえよ、 module で API には快適変更があるときに binding 再生成だけで住むのが理想よな。ちょっと遠そう。

他、 Marked で使ってた `headerIds` という Extension が v8.0.0 からなくなってた。 [Using Advanced - Marked Documentation](https://marked.js.org/using_advanced)

> Removed in v8.0.0 use marked-gfm-heading-id to add a string to prefix the id attribute when emitting headings (h1, h2, h3, etc).

けどこれは `h*` 要素に anchor をつけるように作ってたから元々いらなくて、なくてもいけたのでコレを機に消した。
[Using Advanced - Marked Documentation](https://marked.js.org/using_advanced)

---

あとこれは現在進行系で解決できてない困ってること。
Bulma が document で書かれてるような部分的な利用ができなくなってるってのがわかった。
非推奨となった `@import` を使ってたら起こらないけど `@use` だと起こる。

[With node-sass | Bulma: Free, open source, and modern CSS framework based on Flexbox](https://bulma.io/documentation/customize/with-node-sass/)

SCSS ビルド時にこういうエラーが出る。

```plaintext
Error: The target selector was not found.
Use "@extend %overlay !optional" to avoid this error.
```

[Webpack @extend !optional error · Issue #3391 · jgthms/bulma](https://github.com/jgthms/bulma/issues/3391)

同じようなことに困ってる人いる。
単純に Bulma 側が SCSS 対応がちゃんとできてないってことなんやろな。 `@import` ならいけるからな。

Dart 製の `sass` モジュールを使ってるせいか未だ調べてないけど、解決する予定もなさそうなので一旦全部入りでやり過ごす。
できれば利用してるスタイルだけにとどめて高速・軽量化したいのやけどなー。
Bulma 自体今年はメンテ止まってるし、最悪自分でなんとかする感じかな。
