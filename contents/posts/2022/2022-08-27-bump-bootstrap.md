---
title: "Bootstrap のバージョンを上げる"
tags: ["cryogen"]
---

先週の記事を書いたあとで、何の気なしに [Measure page quality - web.dev](https://web.dev/measure/) で当ブログの測定をした。
そこで既知の脆弱性があるライブラリ (`Bootstrap@3.3.0` と `jQuery@1.11.0`)使うなよ！みたいなレポートがでたので、 Bootstrap の更新を思い立った。

変更した内容 → [Upgrade bootstrap from 3 to 5. · krymtkts/krymtkts.github.io@c4adeac](https://github.com/krymtkts/krymtkts.github.io/commit/c4adeacb06fe759b787646bba5fb698c1f688c94)

ブログ作成当時の記事([Clojure でブログを作った](/posts/2019-01-10-make-blog-with-clojure.html))を見ると、このブログのテーマは Cryogen 備え付けのテーマである `blue_centered` をコピって作ったものだった。
そのままずっと使ってるので、当然の如くテンプレで利用しているライブラリも古いままだったという訳だ。

さて、 Bootstrap のマイグレーションは当然ドキュメントがあるわけだが、

- [Migrating to v4 · Bootstrap](https://getbootstrap.com/docs/4.0/migration/)
- [Migrating to v5 · Bootstrap v5.0](https://getbootstrap.com/docs/5.0/migration/)

今回は~~手抜きによる~~ v3 → v5 という飛び級であるし、わたし自身は Bootstrap に慣れてないので、とりあえずマイグレーションで対応するのは目で見て影響がある範囲のみとした。マイグレーションとしては正攻法な感じじゃなくて、割りと雑な感じだ。

それらの変更は例えば CSS のセレクタの変更だったり、 `class` 属性の変更だったりだ。一応レスポンシブなデザインなので PC とモバイル(ブラウザのエミュレータだけど)も見ている。ちょいちょい以前と違うデザインにした・或いは意図せず変わった箇所もある。

途中コンテンツを空で更新したままデプロイしてしまうしょーもないミスがあったが、再度コンテンツを生成して事なきを得た。
これ Cryogen の差分ビルドの影響でちょいちょいやらかすのだけど、普段は人間力でカバーしていたところを今回はできず、デプロイしてしまった次第。この記事の投稿時点で設定を見直した。

今回の VerUp により Best Practices は 92 -> 100 に、 あと Performance も 93 -> 94 と意図せず微妙な好影響があった。

- before
  - ![変更前は Performance 93 Accessibility 97 Best Practices 92 SEO 92](/img/2022-08-27-capture/before.png)
- after
  - ![変更後は Performance 94 Accessibility 97 Best Practices 100 SEO 92](/img/2022-08-27-capture/after.png)

Bootstrap 5 では CSS カスタムプロパティでフォントサイズとか色とか変えられるみたいなので、このブログ用に再定義してるスタイルのいくつかは不要になるんじゃないかな。
これを機にちょっと見直すのもありかも知れない。あるいは別の静的サイトジェネレータに乗り換えるとか。他にやることなくなったらそれも一興か。

終。
