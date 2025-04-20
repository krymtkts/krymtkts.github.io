---
title: "F# で command-line predictor を書いてる Part 7"
subtitle: "GitHub Code Scanning"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発をした。

[Fsharp.Analyzers.SDK](https://github.com/ionide/FSharp.Analyzers.SDK/) で作成した Code Scanning Alerts の解消をした。 [#48](https://github.com/krymtkts/SnippetPredictor/pull/48)

ただ Code Scanning Alerts が解消されたにも関わらず Open なままだった。
よくわからんなーと思っていたのだが、途中で Code Scanning の configuration を変えたらそうなるという仕様のようだった。なんてこったい。
解消するには以下の通り、古いのと新しい configuration とが競合してることによるようなので、古い方を消したら良いようだ。

- [Fixed code scanning alerts still show up as open · community · Discussion #23403](https://github.com/orgs/community/discussions/23403)
- [Delete stale code scanning configurations to close outdated alerts - GitHub Changelog](https://github.blog/changelog/2023-03-10-delete-stale-code-scanning-configurations-to-close-outdated-alerts/)

消してみたら全部の alerts が解消されてスッキリした。

因みに alert の内容は妥当な感じのやつだったが 1 つ困ったのがあった。
このルール。

[StructDiscriminatedUnionAnalyzer | ionide-analyzers](https://ionide.io/ionide-analyzers/suggestion/012.html)

```fsharp
    [<RequireQualifiedAccess>]
    [<NoEquality>]
    [<NoComparison>]
    type SearchCaseSensitive =
        | CaseSensitive
        | CaseInsensitive
```

こういう DU を作って、それに [`Interlocked`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.interlocked?view=net-9.0) を使って atomic な操作をしてるところがあった。
この DU を [Struct Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions#struct-discriminated-unions) にしてもいいよというヒント。
メモリが無駄なのでって文脈だろう。
構造体にすると `Interlocked` を使えなくなるし(参照型で wrap してもまじで意味ない)、どうしたもんかと。
個別の alert を無効化する方法がわからなかったし、ヒントなので対応しなくても良いのだけど、 alert は 0 の方が安心感ある。
この際なので `Interlocked` で DU を扱うのをやめて `int` に変換する形で [`mutable` な変数](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/#mutable-variables)に格納するように変えた。
こんなんを使う。

```fsharp
   module CaseSensitivity =
        [<Literal>]
        let sensitive = 1

        [<Literal>]
        let insensitive = 0

    module SearchCaseSensitivity =
        let ofBool =
            function
            | true -> CaseSensitivity.sensitive
            | false -> CaseSensitivity.insensitive

        let stringComparison =
            function
            | CaseSensitivity.sensitive -> StringComparison.Ordinal
            | _ -> StringComparison.OrdinalIgnoreCase
```

因みにずっと知らずに生きてきたのだが、変数名が upper case で始まる場合 [`match` expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/match-expressions) の pattern に使えるみたい。
そうなんや...よく今まで生きてこれたな。
でも F# like な命名に即すると Upper camel case は違和感ある。
この場合上記のように module で修飾すれば良い。

やっぱこういう気付きがあることからも、 F# のような強力な言語でも linter の類はあった方がいいよな。
[FSharpLint](https://github.com/fsprojects/FSharpLint) を使いこなせなくなって久しかったので助かった。

FSharpLint のメンテに関しては、 2 週間ほど前に追加のメンテナをコミュニティで募集しよか？と Don さんが issue 立ててた。なんか変化起こるかなー。
[Checking on on maintainers status · Issue #721 · fsprojects/FSharpLint](https://github.com/fsprojects/FSharpLint/issues/721)
