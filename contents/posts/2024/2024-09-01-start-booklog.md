---
title: "booklog はじめた"
tags: ["diary", "fsharp", "fable"]
---

[先日 GitHub の Current Streak が 2 年に達した](/posts/2024-08-18-github-streak-2-years.html)のを機に、新しい習慣 booklog をはじめた。

こちらが実装したモノで、

[Merge pull request #220 from krymtkts:feature/booklogs · krymtkts/blog-fable@1e3c577](https://github.com/krymtkts/blog-fable/commit/1e3c577a087a89e8de5cfea729b6e1ee4db994ef)
(この他にもちまちま修正を進めている)

それを使って出力したのが以下の Booklog ページだ。

[krymtkts - Booklogs](/booklogs.html)

正直なところ休みや空き時間でドバーッと書いたコードなので、まさにファーストクソコードと呼ぶに相応しき醜さだ。
未だバグあるし、ない機能もあるし、 module 構成なんか後付感すごく元の奴らと調和してない。
ただブツさえ生成できるようになればあとは改善やバグ修正は容易いので、まずはモノができたことを祝いたい。

わたしの booklog は今のところカレンダーの生成と 1 年毎のページに booklog がまとめられるのみだが、今後は tag やタイトルでまとめたページを作るのも良さそうだなと考えている。
特に書籍ごとのグルーピングは工夫すれば書評になるよなと思っている。
小言の羅列で連続性がないかも知れないけど、そこはまとめてみてのお楽しみ。

---

あといくつか実装前に想像していたことに対して実際どうなったのか書いていく。

> あと自分に習慣を刻み込むにあたり、 GitHub の activity の heatmap みたいなの(calendar chart とかいうらしい)、日本で例えるなら夏休みに体操の判子押してくれるカレンダーみたいなのがあればめちゃくちゃ強制力があるのは、 Current streak の経験からわかっている。
> なのでそういう形式の book log を自分でこしらえようと思いついた。

この通りなので booklog のカレンダーを作るにあたって見た目 GitHub の Activity を参考にした。
やはりアノ見た目は継続中毒者の心を鷲掴みにするので取り入れたかった。
とはいえこちらは静的ページなので、あちらのような click した日のデータを動的に読み込む機能はない。
代わりに 1 年間の booklog が日付降順で一覧されているので、その日付に飛べる。

このカレンダーの実装は GitHub Copilot や ChatGPT に投げつけたらすぐできるやろと高をくくっていたのだが、コイツラ全然満足なコードを出せなかった。
細かい箇所では彼彼女らのコードが使われた箇所もあるが、結局大部分で自ら書いた。
おかげで？愛着のあるものになった。早くゴチャついた部分のコードもキレイにしてあげたい。

この booklog のカレンダーだが、 [Archives](/archives.html) のページで post と page の記録日見れてもいいかもなと思ったが、週末しか書かないと悲しいカレンダーになるので今んところはナシかな。

> そういう SaaS を使うのも良いが、やはりデータが自分の手元にないのは良くない。
> plaintext に保存したデータから calendar chart が描けて、ついでに memo や読んだ回数、回数がわからなくても昔読んだことがあるかとかの属性も残せるやつ。

booklog のデータの保存形式は以下のような [YAML](https://yaml.org/) 形式にした。いくつかの省略可能な属性を省いている。

```yaml
- date: 2024-08-19
  bookTitle: Domain Modeling Made Functional 関数型ドメインモデリング ドメイン駆動設計と F# でソフトウェアの複雑さに立ち向かおう
  bookAuthor: Scott Wlaschin
  pages: vi ~ 5
  notes: |
    冒頭を読んだだけ。なんとなく丁寧な印象を受けた。
```

F# 側の厳密な型は以下のようにしている。
[blog-fable/src/Booklog.fs at ea7bec057edd5f9ec1420318e4627bf42ecd965d · krymtkts/blog-fable](https://github.com/krymtkts/blog-fable/blob/ea7bec057edd5f9ec1420318e4627bf42ecd965d/src/Booklog.fs#L8C1-L16C38)

```fsharp
    type Booklog =
        abstract date: string
        abstract bookTitle: string
        abstract bookAuthor: string
        abstract readCount: int option
        abstract previouslyRead: bool option
        abstract pages: string
        abstract tags: string ResizeArray option
        abstract notes: string option
```

`readCount` を記録できるようにしてみて、かつ回数を覚えてないようなものは `previouslyRead` で前読んだことがあるのを意思表示する。
`readCount` はそのままの数字が印字されるが、 `previouslyRead` を入れてると `n+1` のような印字になる。
`notes` は無駄に Markdown で書けるようにしてある。技術書を読んでると感想にも `code` を書きたくなるので、ほぼそのためだけ。
あとあんまりにもおもんない本であれば `notes` を書くこともないかなと思い `option` とした。
`tag` は先述の通り未だ実装してないが事前に定義しておいた。

> book log ができるまでは素朴に Markdown にでも記録していって、モノができたら変換して取り込むようにしてみようかな。

毎日更新するので気軽にかける形式が良いし、 [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) には既に front matter のデータを取り込むための YAML パーサ [eemeli/yaml](https://github.com/eemeli/yaml) があったので、 YAML にした。
TOML も好きなのだけど、[連続した要素の記述でカッコを書く](https://toml.io/en/v1.0.0#array)のだるそうやなとか、[複数行の文字列](https://toml.io/en/v1.0.0#string)も面倒そうかなとか思い、やめた。
実際 2 週間ほど booklog をつけてきたが YAML ならかなり楽に更新できる。
まだ [GistPad](https://github.com/lostintangent/gistpad/tree/master) からの更新はしてない(commit sign がつかないのが潔癖症に引っかかる)けど、いつかやる日が来るはず。
そうなればほぼメモ更新間隔で booklog をつけれるようになる。

理想的には `bookTitle` と `bookAuthor` は別のデータで管理してそれに参照するのが良いが、 1 ファイルに booklog を追記するだけで良いという利点を損なうので正規化していない。
書くときは愚直に前の booklog をコピペし展開していく運用にしている。
でもコピペと言っても毎回同じデータを書くのもダルいので、いつか省略できる仕組みは導入したい。

あと [Fable](https://fable.io/) のプログラミングが久しぶりだったので地味に Fable から Node.js の eemeli/yaml 使うのでハマった。 `ResizeArray` じゃないと array がマッピングされない点。
これで無駄に時間を溶かしたが次忘れないように note を残せたからヨシとする。

```fsharp
    let parseBooklogs (str: string) =
        // NOTE: requires to define as ResizeArray to convert from raw JavaScript array.
        let bs: Booklog ResizeArray = Yaml.parse str

        bs |> List.ofSeq
```

> モノがないとあんまり続けられる気がしないけど、習慣化の道筋はできたのでなんかいけそうな気がしてきた。

これについては booklog 機能をブログに実装すると決めたときから、書いておかないと booklog カレンダーを華やかすことができないという強制力が働いて、機能が出来上がるまでの間も習慣を続けられた。
機能なくても良かったのかも知れんが、長期になったら続かなかっただろうし多分必要だった。これは必然やな。

---

ちまちま booklog の実装してた間は [krymtkts/pocof](https://github.com/krymtkts/pocof) を放置してたけど、今後はそんな集中していじることもないと思うので、まずリリースから再開しよう。
と言いつつこの日記をしたためている間にバグを見つけた。実装してない機能もあるし、 pocof にはもう少し待ってもらうのが良いか、あるいは行ったり来たりが良いか。
気分と相談してやること決めていく。
