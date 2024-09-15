---
title: "F# で Cmdlet を書いてる pt.48"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。
日記にするのは約 1 ヵ月ぶりか。

この 1 ヶ月の間、 [#209](https://github.com/krymtkts/pocof/pull/209) 以降はちょうど盆、日本の夏休みということで、移動の車中や山に囲まれて細々とした修正を重ねてきた。
`None` operator を消して、それに伴うドキュメント修正し、プロパティ候補が case insensitive になってないバグ直したり、気になるコードをキレイにしてみたり...ほんと細々と。
そういうのの対処をしたあと、 [blog に booklog 機能を実装する](/posts/2024-09-01-start-booklog.html) のに注力してる間は、完全に放置してた。
先週になって booklog が 100% ではないが概ね満足いくものになったので、全部ひっくるめて [v0.15.0](https://www.powershellgallery.com/packages/pocof/0.15.0) としてリリース。
めでたしめでたしさて次何やるかなというところだった。

pocof のクエリ改善にコード引用符が使えそうやなという閃きが頭に降りてきた。その頃ちょうど [dmmf 本](/booklogs/dmmf.html) を読み終えたところで、本から特別な何かを得たわけじゃなかったが F# の本なんてそう日本語で読めるもんでもないので、刺激になったみたい。
dmmf 本には全然コード引用符書いてないけどｗ
母国語で興味のあるトピックに触れたことでシナプスが発火したのかな。いい経験したわ。

そこで [プログラミング F# のコード引用符の章](/booklogs/programming-fsharp.html) と[Code Quotations - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/code-quotations) を読んでイメージを膨らませ、 [#224](https://github.com/krymtkts/pocof/pull/224) で実装はじめてみた。

クエリの箇所は元が気合の再帰で実装してるのもあって(それより前は `List` 処理でもっと遅かったけど)、軽く見た感じ結構速くなりそう。ところがうまくいかんのよね。昔のわたしが書いたテストをクリアできない。なんでかなーとなるわけやけど調査してみたら、ちょっと前に非同期レンダリングを入れたときに non-interactive mode のプロパティ検索がバグってたみたい。
処理が遅いうちは気付かないが、速くなるとプロパティ読み替え用の `Map` の構築が追いつかなくて、プロパティ検索できなくなってるみたい。
[#223](https://github.com/krymtkts/pocof/issues/223)
どうにもならんので準備がてらきょう直してみたらテスト通る様になったので、やっぱコイツが原因だったか... まあ直ってよかったわ。

いまは未だ、入力されたクエリを個別の条件式の lambda にして `&&` か `||` でつなぐというところまでしかやってない。
これだと個別の条件式の数だけ lambda が増殖する。

[Implementation to replace with a lightweight predicate. by krymtkts · Pull Request #224 · krymtkts/pocof](https://github.com/krymtkts/pocof/pull/224/files#diff-7243da75219fb90456ee38cde35e5d411c463aa6ea393727bf526c89cd032587R232-R236)

```fsharp
        | QueryPart.End ->
            match acc with
            | [] -> alwaysTrue
            // TODO: should i use code quotation to remove redundant lambda?
            | _ -> acc |> List.rev |> List.reduce (fun acc x -> fun e -> combination (acc e) (x e))
```

この余分な lambda を取り除いて平坦な式にするのにコード引用符を使えたらなって考えている。 fsi で試す感じ `<@ %acc && x e @>` って感じにできそうやねんよな。

この対応で気になってるのはコード引用符の評価にどんくらいのオーバヘッドがあるかよくわかってないところ。
デフォで使える [`LeafExpressionConverter.EvaluateQuotation`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-linq-runtimehelpers-leafexpressionconverter.html#EvaluateQuotation) を使うつもりなんやが、こいつのオーバヘッドが lambda を入れ子にしまくるオーバヘッドに勝れば、いい感じよな。

他にも難しく考えすぎてややこしく遅くなってる pocof のコードって意外にありそうなので、まだまだ楽しませてくれそうな気がする。
続く。
