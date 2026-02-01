---
title: "F# で Cmdlet を書いてる pt.46"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[前回に引き続き](/posts/2024-07-28-writing-cmdlet-in-fsharp-pt45.html) `Query.run` 自体の最適化をしている。

[ちまちまと変更を加えて](https://github.com/krymtkts/pocof/pull/208/files)、もとよりは多少速くなってそう。
わたしのしょぼい 2017 年製 laptop で 1 ~ 100000 までの数字から 100000 を探し出すのに 5 秒前後かかっていたのが、 4.5 秒前後になったぽい。
ほんまかー？という感じではあるが、実際に数を増やして 1 ~ 500000 までの数字から 500000 を探し出してみても修正後の方が 2 ~ 2.5 秒速いので、効果は確かなようだ。

前回の修正も含めて行った最適化は、 `inline` 化や `List` を末尾再帰に置き換えるといった簡単なものだけだ。
それでも noob F# ninja のわたしには多くの気付きがあった。
`inline` をつけた関数であっても関数合成を使うと `inline` 化されないので pipeline を使う必要があるとか。

また特定のケースでは末尾再帰が `List` より速いようで、少し驚いた。 `List` は結構速いモノの認識だったが、更に突き詰めた局所最適化だと末尾再帰という primitive な形に落ち着くねんな...という気づきを獲た。
まーでもどの言語でも配列やリスト構造をそれ用の関数でグルグル回すよりは単純なループのほうが速いこともあるし、そういうことなんやろなという認識(末尾再帰は loop に最適化されるため)。

[判別共用体](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) を [構造体](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/structs) に置き換えるとかのより踏み込んだ最適化はまだやってない。
まだただの直感だが、 [`Data.Entry`](https://github.com/krymtkts/pocof/blob/main/src/pocof/Data.fs#L100-L104) を構造体にしたら、処理するデータ件数が大きいほど効果があるのかもなと思っている。
この手法は確か [bcarruthers/garnet](https://github.com/bcarruthers/garnet) を見つけたときに知ったのだったが、 pros. cons. あり検証を要するのでインスタントにできるものでは
そこに踏み込むにはまだやっていないことがあるので、計測・検証して順次試していくつもり。

他にも F# の最適化の手段は色々あるようで、 F# performance tips でググった上位 3 つを読むだけでもお腹いっぱいになってしまった。
全部は消化しきれないので地道に .NET のプロファイリングから学んでいくか。
ここ 1 月くらいで ILSpy を使い始めたのだけど、更にツールが増えるのかな...

因みに GitHub Copilot や ChatGPT に聞いても似たような回答をくれるが、(文脈理解してるはずなのに)一般的な話をされて pocof の文脈にそぐわないような回答をいただくばかり。
なので、この件に関してはあまり使い物になってない(プロンプトが悪いのかも知れんが)。
リスト処理を末尾再帰に変えてくれとか具体的な指示になったら元気に回答くれるけど(微妙に間違ってるけど)。

- [Writing high performance F# code](https://www.bartoszsypytkowski.com/writing-high-performance-f-code/)
  - これはレイヤが低めで .NET の理解度が求められ難しめ
- [Real life performance optimizations in F# - DEV Community](https://dev.to/t4rzsan/real-life-performance-optimizations-in-f-3nep)
  - これはかなり簡単な部類
- [performance - How to optimize F# programs generally - Stack Overflow](https://stackoverflow.com/questions/9252660/how-to-optimize-f-programs-generally)
  - まとめみたいなやつ。辿るのがダルい

まだやっていないことの 1 つは今対処中で、 `Query.run` が実行されるときに行われていたリストを取り除くことだ。
これは検索条件を `List` に詰め込んでいたところを残していたので、そこを判別共用体で作った自作の連結リストに変えるものだ。これも多少速くなったようだ。
教科書的な木構造の実装なんかで見た判別共用体の連鎖を普通に実用レベルで使えるねんなというのは気付きやな。

[#209](https://github.com/krymtkts/pocof/pull/209)

```diff
     [<RequireQualifiedAccess>]
     [<NoComparison>]
+    [<NoEquality>]
     type QueryPart =
-        | Normal of value: string
-        | Property of lowerCaseName: string * value: string
+        | Normal of is: (string -> bool)
+        | Property of lowerCaseName: string * is: (string -> bool)
+
+    [<RequireQualifiedAccess>]
+    [<NoComparison>]
+    [<NoEquality>]
+    type QueryNode =
+        | Part of head: QueryPart * tail: QueryNode
+        | End
```

今はこんな風に変えてて、まだちょっとイマイチ。 `QueryPart` と `QueryNode` は分ける必要ないかなと思っているところ。
元は検索条件の値だけを持った `List` を取り回して、 `Query.processQueries` 自体が引数に持つテスト関数で条件に一致しているか見ていた。
これを検索条件の関数を持った連結リストにして、 `Query.processQueries` 自体はテスト関数を持たなくさせる。
これは、今 `Regex.IsMatch` を使ってキャッシュされることを期待している部分を、事前にコンパイル済みのパターンを使うようにしてるための前準備でもある。
これも効果あるかは試さないとわからない。現時点でキャッシュが効いてるのであれば多分変わらないのじゃないかなと思っている。

これまでの改善同様に爆発的な効果は出てないけど、地道に試行錯誤してく予定。
