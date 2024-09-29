---
title: "F# で Cmdlet を書いてる pt.49"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。最近のまとめ。

以前[コードクォート](https://learn.microsoft.com/ja-jp/dotnet/fsharp/language-reference/code-quotations)[^1]でクエリの述語を実装してみるアイデアを記した。
多重で [lambda](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/lambda-expressions-the-fun-keyword) を入れ子にするよりもコードクォートで条件を平坦に繋いだ lambda にした方が速そうやなというやつ。

[^1]: プログラミング F# ではコード引用符と呼んでたがどうもコードクォートとカタカナ表記するのが一般的なようなのでこっちに寄せる。

趣味プロなのでパフォーマンスの計測とかせずそのまま実装に移してしまっても良かったが、一応サンプルとして lambda 入れ子版とコードクォート版を作って比較してみた。

[fsharp-cmdlet-sandbox/src/code-quote/Library.fs at main · krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox/blob/main/src/code-quote/Library.fs)

コードはこんな感じ。

```fsharp
    let buildExpr conditions op =
        conditions
        |> List.rev
        |> List.reduce (fun acc x -> fun e -> op (acc e) (x e))


    (*
        or condition
        if expr1 x then true else xxx
        if expr2 x then true else xxx
        if expr3 x then true else xxx
        if expr4 x then true else xxx
        if expr5 x then true else false

        and condition
        if expr1 x then xxx else false
        if expr2 x then xxx else false
        if expr3 x then xxx else false
        if expr4 x then xxx else false
        if expr5 x then true else false
    *)
    let generateExpr conditions op =
        let xVar = Var("x", typeof<string>)
        let x = xVar |> Expr.Var |> Expr.Cast<string>

        let combination =
            match op with
            | Operator.And -> fun c acc -> Expr.IfThenElse(<@ c %x @>, acc, <@ false @>)
            | Operator.Or -> fun c acc -> Expr.IfThenElse(<@ c %x @>, <@ true @>, acc)

        let rec recBody acc conditions =
            match conditions with
            | [] -> acc
            | condition :: conditions ->
                let acc = combination condition acc
                recBody acc conditions

        let body =
            match conditions |> List.rev with
            | [] -> <@@ true @@>
            | condition :: conditions ->
                let term = Expr.IfThenElse(<@ condition %x @>, <@ true @>, <@ false @>)
                recBody term conditions

        let lambda = Expr.Lambda(xVar, body)

        lambda
        |> LeafExpressionConverter.EvaluateQuotation
        :?> string -> bool
```

コードクォートを使って組み合わせて作った lambda の AST を評価することで関数を動的に生成する。
body の組み立てに必要な `(||)` や `(&&)` は [`F# の repo`](https://github.com/dotnet/fsharp/blob/0a5901fd9c02b4c3b066678a1f7b68ce5939b774/src/FSharp.Core/prim-types.fs#L640-L644) を見てわかるように `if ~ then ~ else` の構文糖なので、 AST 組み立てでも [ExprIfThenElse](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-quotations-fsharpexpr.html#IfThenElse) を利用して再現する。
コードクォート同士の組み合わせは [Splicing operator](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/code-quotations#splicing-operators) を使って、最後に [Expr.Lambda](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-quotations-fsharpexpr.html#Lambda) に食わせることで lambda が出来上がるという仕組み。
AST の組み立ては型のサポートあれど、不正な文法だと実行時のエラーでしか気付けないのでちょっと面倒だった。しかし面倒さを払う価値ある強力な機能。

以降は 2018 Razer Blade Stealth RAM 16 GB で計測した結果の話をする。

結果の数値をコピってなかったが、件数が多いとコードクォートの方が明らかに速かった。
デメリットとして、コードクォートから lambda を構築するのに 100ms 程度のオーバーヘッドが生じるようだった。
lambda の方は単純に組み合わせていくだけなので、構築するのに大して時間がかからない。

ただ lambda 版の方は入れ子の階層が深いほど lambda の呼び出し回数が増加してパフォ劣化が如実になるようだった。
そのオーバヘッドを回収するのはそんなに難しいことではないなと判断し、 [#224](https://github.com/krymtkts/pocof/pull/224) で pocof へ実装した。コードは[こんな感じ](https://github.com/krymtkts/pocof/blob/417b109280568cc2c258acb677a72cb27ba24957/src/pocof/Query.fs#L147-L173)でほぼサンプル実装と同じ。

pocof 実装の note にもあるが、条件を末尾から組み立てる必要があるので `(Entry -> bool) list` の並び順は事前に反転してある。
また pocof では `hashtable` をサポートする関係で評価の対象が [`DictionaryEntry`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.dictionaryentry?view=net-8.0) の場合の条件組み立てで key と value を評価した結果を `(||)` か `(&&)` で判定する必要があるのだけど、それは `Entry -> bool` の関数を構築する時点で解決しておりここには載ってない。
パフォに関しては、 pocof の場合は動的にプロパティの値にアクセスするコードがあるのもあってか、サンプル実装よりも条件の組み合わせが増えた場合の lambda 版の速度劣化が著しかった。
コードクォート版なら条件の数が多くてもさほど遅くならない。複数条件を入力することが多いし、大いにコードクォートを使う価値があったな～という感じ。

わずかでも pocof が速くなったし、コードクォートの実践もできたし、なかなか良かった。
コードクォートはまだ簡単な使い方しかできないし、型情報有り無しの取り扱いもこなれてないので、 pocof とは別の形でもいいので更に掘り下げたいな。
