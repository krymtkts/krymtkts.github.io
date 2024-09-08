---
title: "booklog はじめた 2"
tags: ["diary", "fsharp", "fable"]
---

[先日はじめた booklog](/posts/2024-09-01-start-booklog.html) がきょうで 3 週間になった。
仕組みを構築したことで明らかにい強制力働いてるあたり自身の単純さに笑える。

[krymtkts/pocof](https://github.com/krymtkts/pocof) も再開したが、次やりたいことは多少練習しないとできなそうなので、今日はまた当ブログの booklog 機能について記す。

---

booklog 機能のバグ修正と機能追加をちまちま行ってきた。
雑多なリファクタリング [#232](https://github.com/krymtkts/blog-fable/pull/232) と
Light テーマの対応漏れ [#233](https://github.com/krymtkts/blog-fable/pull/233) 。

既読日のセルからログにジャンプするリンクは、元々手抜きでその日の最初のログ(`${date}-1`)に飛ぶようになってた。でも登録降順でログ表示しているから 1 番目に飛べても嬉しくない。なのでその日の最後のログに飛ぶようにした。これなら描画域先頭が最後になりその日のログをある程度一覧できて良い。

あと忘れがちというか忘れてたのだけど light & dark theme 対応しているので、 Solarized Light っぽいカレンダーの色も考えた。ゆーても未読の日のセルの色を調整しただけなので、多少 cyan が目立つかなーというのはある。
また既読のセルの色を GitHub の Activity みたいに回数でグラデーションしたようなのは考えてないから、配色はコレで固まったかなという感じ。

あと足したい機能として考えてるのが、サマリ系の機能。
current streak & longest streak を表示しておきたいのと、あと書籍ごとにログをまとめたページを作りたい。

tag は要るかなと思って属性のみ設けていたけど、要らない気がしてきた。 tag のページから書籍のサマリの一覧があるといいかもなというのはあるが、そんなに既読の書籍が貯まるのは未だ先の話だと思う。
その時になって tag で書籍を管理できたらいいなという感覚があれば対処するものとしたい。
streak はそんなに難しくないけど、書籍ごとまとめはちょっと難しいなと考えてる。何が難しいかというと URL どうするかというところ。なんかいい感じに ASCII だけの URL にしたいけど日本語の書籍ばっか読むのでどーにもならん。 Punycode だと URL を一見して何のページかわからなくなるし。

となるとやで、手動で書籍ごとに識別子を考えて付与してやるのがいいと思うが、 booklog の YAML に毎回書くのはちょっとなあという感じがする。
なので現実的な解としては書籍の名前から識別子を引くための YAML を個別に作って、そこに登録されてたら書籍のサマリを生成するってのがいいかな。ちょっとめんどいけど。
そうするとなればいま booklog の YAML に直に書いてる著者名や前読んだかの属性も本に寄せた方がログ追記も楽やろなとなる。

[blog-fable/src/Booklog.fs at f4ee5cc9c3198596539d34588570619d7ba6e07a · krymtkts/blog-fable](https://github.com/krymtkts/blog-fable/blob/f4ee5cc9c3198596539d34588570619d7ba6e07a/src/Booklog.fs#L8-L16) は以下のようになるはず。

```fsharp
    // これが
    type Booklog =
        abstract date: string
        abstract bookTitle: string
        abstract bookAuthor: string
        abstract readCount: int option
        abstract previouslyRead: bool option
        abstract pages: string
        abstract notes: string option

    // こうなるはず
    type Booklog =
        abstract date: string
        abstract bookTitle: string
        abstract readCount: int option
        abstract pages: string
        abstract notes: string option

    // NEW!
    type Book =
        abstract bookTitle: string
        abstract bookAuthor: string
        abstract previouslyRead: bool option
```

当初 1 ファイルにまとめたかったからそうしていたけど、結局ファイルを分けて正規化することになろうとはな。

ファーストクソコードは依然そのままだが、今やりたいことが実現できれば機能的に満足いくようになって、そこで一段落かなと考えている。あとは習慣を継続することのみに注力できそう。
