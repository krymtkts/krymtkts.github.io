---
title: "F#でコマンドレットを書いてる pt.11"
tags: ["fsharp","powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

年末年始思ったほど時間取れなかったが、 pocof のプロパティ指定検索はちょっとずつ進んでいる。

[前](/posts/2022-12-18-writing-cmdlet-in-fsharp-pt10.html)は入力できるプロパティの候補が表示されるだけだったが、最近ようやく入力できるプロパティの絞り込みと、プロパティを指定した検索ができるようになった。

プロパティ名は case insensitive に指定できる。まだ複数条件入れたときが buggy でうまく動いてないけど、あとプロパティ入力候補の表示非表示と Ctrl+space での補完ができたら、
ほしいものが出揃うかなーというところだ。

F# でオブジェクトのプロパティを動的に取得する方法としては、リフレクションを利用した。
これが妥当な実装なのかわからないが、現時点では一番シンプルだ。
オブジェクトに `GetType().GetProperty()` してプロパティを特定、 `PropertyInfo` を介してクエリで指定されたプロパティ名の文字列を使ってプロパティの値を取得している。

元々 F# のシンボルには動的にプロパティを参照する `?` が書いてある[^1]けど実装は提供されてない。
そういうモジュール([fsprojects/FSharp.Interop.Dynamic](https://github.com/fsprojects/FSharp.Interop.Dynamic))はある。
けど、依存を増やしたくないのと、利用する範囲のコードはすぐ書けるレベルなので自分で実装することにした(使えるか検証してないのもあるし)。

簡単なはずなんだが、結構自身の F# 力不足に起因して困る場面が多かったので、その内容を記す。

---

結論からいうと、自前の `?` 演算子を定義しようとしたところ、 `?` 演算子を中置演算子として使ったときに想定外の挙動があったので、結局 `/?` みたいな謎の組み合わせの演算子を自作した。

こういうコードがあるとする。

```fsharp
let inline (?) (x: 'a) (prop: string) =
    try
        let propInfo = x.GetType().GetProperty(prop)
        Some(propInfo.GetValue(x, null) :?> 'b)
    with
    | _ -> None
```

先述のコードを利用した場合、プロパティ名の変数を中置演算子と共に利用すると、変数の名前が引き渡されたパラメータとして認識されてしまい、変数の値が展開されなかった。
通常の関数として呼び出すとこのようなことはないのだけど...
なんか言語仕様を読み落としてる気がする。もうちょっと真面目に追い込んでみる必要があるか。以下は REPL(`dotnet fsi`) で試してみた結果。

```fsharp
> let str = "hello";;
> let p = "Length";;

> let a: int option = str ? p;; // プロパティ p を探しに逝って無いから None
val a: int option = None

> let a: int option = (?) str p;; // ちゃんと変数 p の値である Length の値を取れる
val a: int option = Some 5
```

結局これの解消ができないので `?` を諦めて `/?` としたのだが、その前に `!?` とか使ってみても駄目だったので妥協してこうなった。
この場合はシンボルの組み合わせに何らかの制限があるようだが、確証を持てるドキュメントが見つけられず。

```fsharp
> let inline (!?) (x: 'a) (prop: string) =
-     try
-         let propInfo = x.GetType().GetProperty(prop)
-         Some(propInfo.GetValue(x, null) :?> 'b)
-     with
-     | _ -> None
- ;;
val inline (!?) : x: 'a -> prop: string -> 'b option

> let a: int option = (!?) str p;;
val a: int option = Some 5

> let a: int option = str !? p;; // なんでエラーになるのかわかってない

  let a: int option = str !? p;;
  --------------------^^^

stdin(51,21): error FS0003: This value is not a function and cannot be applied.

> let inline (/?) (x: 'a) (prop: string) =
-     try
-         let propInfo = x.GetType().GetProperty(prop)
-         Some(propInfo.GetValue(x, null) :?> 'b)
-     with
-     | _ -> None
-
- ;;
val inline (/?) : x: 'a -> prop: string -> 'b option

> let a: int option = str /? p;; // こっちはいける
val a: int option = Some 5
```

この辺を理解していってもうちょい pocof の開発をスムースに行いたいところやなー。

[^1]: [シンボルと演算子のリファレンス - F# | Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/fsharp/language-reference/symbol-and-operator-reference/#dynamic-lookup-operators)
