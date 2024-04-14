---
title: "F# の Inline Functions のカバレッジを計測する"
tags: ["fsharp", "dotnet"]
---

最近あんま週末時間取れず開発できてないので小ネタ。

[CI test coverage of F# unit tests · Issue #3579 · dotnet/fsharp](https://github.com/dotnet/fsharp/issues/3579)

この中で触れられてるのだけど、 F# の [Inline Functions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/inline-functions) は code coverage を計測できない。
コンパイル時点で使われてる箇所に inline 展開され、展開前の情報を捨て去るので追えないみたい。
[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発でも Inline を使ってる箇所があって、そこは長らく code coverage 取れないので網羅率がちょっと低くなるという状態だった。

ただ計測のためのちょっとした hack はある。
[ここ](https://github.com/dotnet/fsharp/issues/3579#issuecomment-329348683)で触れられてる [`#if` ディレクティブ](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#conditional-compilation) を使って Debug モードのビルドのときだけ `inline` を外す方法だ。
静的にジェネリック型が解決されるようなパターンだと使えないようだが、 pocof では使えた。
[こんな感じ](https://github.com/krymtkts/pocof/blob/350cb39288f384d3123dd7f16275bb75bde276c9/src/pocof/Query.fs#L63-L74)。

```fsharp
    let
#if !DEBUG
        inline
#endif
        private (?->)
            (x: PSObject)
            (prop: string)
            =
        try
            Some (x.Properties.Item prop).Value
        with _ ->
            None
```

ただやっぱコード見た目意味わかんなくなるし Fantomas もフォーマットに困るみたい(ガタガタ)なので、あんま良くないなと。仕方なくやってる。

F# は coverlet とあまり相性いい感じしない。個人的には以下が相性良くないと感じてる点だ。他にもあるんかな。

- match expression は可読性高くてあまり複雑にならないのだけど、コンパイル後の中間言語の循環的複雑度が高い
- [`string` の slicing](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/strings#string-indexing-and-slicing) を使うと循環的複雑度が上がる
  - [Multiple branch points created for F# string slice · Issue #1208 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/issues/1208)
- 先述の Inline functions

ここまで来ると F# では code coverage や複雑度などのメトリクスはあまり意識しない方がいいのか？と思えてくる。

[FSharplint](https://github.com/fsprojects/FSharpLint) の issue でも「みんな使ってないし手続き型ほど重要じゃない」という意見で、過去に循環的複雑度の計測をなくしたことがあるみたいだし(復活したようだが)。
[cyclomatic complexity removed? · Issue #195 · fsprojects/FSharpLint](https://github.com/fsprojects/FSharpLint/issues/195)

code coverage も、テスト書くならどのコードがテストされたかを計測したいのは当然だと思ったが...

pocof の場合で考えたら、毎日連続的に開発してるわけじゃないし、品質を維持できる指標があると、すっぱり忘れてしまってても安心して取り組めるのであった方が良い。

興味は尽きませんな。
