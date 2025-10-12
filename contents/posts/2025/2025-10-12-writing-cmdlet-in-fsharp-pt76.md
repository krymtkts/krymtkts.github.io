---
title: "F# で Cmdlet を書いてる pt.76"
tags: ["fsharp", "powershell", "dotnet"]
---

漸くまとまった時間が取れるようになった。とりあえず毎週何もできないとかは当面ないと思われる。

早速 [krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。
主に `String` 操作の効率化だ。 hot path ではなるべく F# の `String` module も使わないようにしてみている。 [#368](https://github.com/krymtkts/pocof/pull/368)

benchmark をとってもみて極端に良くできたところもあれば、何故速くなった・遅くなったかわからないところもある。
効果が顕著そうなところを benchmark ピックアップする。

修正前。

| Method                        |     Mean |    Error |   StdDev |   Gen0 | Allocated |
| ----------------------------- | -------: | -------: | -------: | -----: | --------: |
| QueryState_getCurrentProperty | 89.43 ns | 2.249 ns | 6.595 ns | 0.0267 |     112 B |

| Method                  |     Mean |    Error |   StdDev |   Gen0 | Allocated |
| ----------------------- | -------: | -------: | -------: | -----: | --------: |
| QueryCondition_toString | 26.93 us | 0.862 us | 2.486 us | 2.0142 |   8.38 KB |

| Method                |     Mean |     Error |    StdDev |   Gen0 | Allocated |
| --------------------- | -------: | --------: | --------: | -----: | --------: |
| invokeAction_AddQuery | 1.144 us | 0.0228 us | 0.0577 us | 0.4005 |   1.64 KB |

| Method                    |     Mean |    Error |   StdDev |   Gen0 | Allocated |
| ------------------------- | -------: | -------: | -------: | -----: | --------: |
| invokeAction_BackwardChar | 590.3 ns | 11.59 ns | 24.44 ns | 0.0248 |     104 B |
| invokeAction_BackwardWord | 681.4 ns | 12.35 ns | 14.70 ns | 0.0477 |     200 B |

修正後。

| Method                        |     Mean |    Error |   StdDev |   Gen0 | Allocated |
| ----------------------------- | -------: | -------: | -------: | -----: | --------: |
| QueryState_getCurrentProperty | 51.49 ns | 1.229 ns | 3.547 ns | 0.0153 |      64 B |

| Method                  |     Mean |     Error |    StdDev | Allocated |
| ----------------------- | -------: | --------: | --------: | --------: |
| QueryCondition_toString | 4.406 ns | 0.0873 ns | 0.2361 ns |         - |

| Method                |     Mean |     Error |    StdDev |   Gen0 | Allocated |
| --------------------- | -------: | --------: | --------: | -----: | --------: |
| invokeAction_AddQuery | 1.093 us | 0.0217 us | 0.0434 us | 0.4005 |   1.64 KB |

| Method                    |     Mean |    Error |   StdDev |   Gen0 | Allocated |
| ------------------------- | -------: | -------: | -------: | -----: | --------: |
| invokeAction_BackwardChar | 535.8 ns | 10.61 ns | 18.57 ns | 0.0248 |     104 B |
| invokeAction_BackwardWord | 689.1 ns | 12.58 ns | 22.68 ns | 0.0477 |     200 B |

[`String.length`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-stringmodule.html#length) が不要な箇所は直に `String.Length` を使ってみるのは、実際に `StringModule.Length` の呼び出しがなくなる分僅かに速くなる。
`invokeAction` の `AddQuery` や `BackwardChar` はそれに当たるとだろう。
何故か `BackwardWord` は遅くなってしまったが軽微な差なので誤差かな。

`QueryState.getCurrentProperty` は中間文字列を生成していたのをやめて 4 割ほど高速化した。

```patch
         let getCurrentProperty (state: QueryState) =
             let s =
-                state.Query
-                |> String.upToIndex state.Cursor
-                |> fun x -> String.fromIndex <| x.LastIndexOf " " + 1 <| x
+                let q, c = state.Query, state.Cursor
+                let start = if c > 0 then q.LastIndexOf(' ', c - 1) + 1 else 0
+                q.Substring(start, c - start)
```

また `QueryCondition.toString` に関しては [`list`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-list-1.html) の生成と中間文字列の生成を一切なくすことで、比較にならないほど劇的に速くメモリも少なくできた。
代わりに愚直な match expression を採用したので、全てのケースがベタ書きされているので、一見するとギョッとする。
でもある意味 match expression で経路も網羅できてるし「正しい」使い方ではあるかな。
速さと省メモリは正義なのだ。

今回修正を試みた中で、完全に読みが外れたのは String interpolation だ。
以下に例を示す。

```fsharp
    module InternalState =
        let queryInfo (state: InternalState) (count: int) =
            $" %s{state.QueryCondition |> QueryCondition.toString} [%d{count}]"
```

```fsharp
    module InternalState =
        let queryInfo (state: InternalState) (count: int) =
            String.Concat(" ", QueryCondition.toString state.QueryCondition, " [", string count, "]")
```

これらは C# での表現だと以下に compile されていた。

```csharp
[CompilationArgumentCounts(new int[] { 1, 1 })]
public static string queryInfo(InternalState state, int count)
{
    return PrintfModule.PrintFormatToStringThen(new PrintfFormat<string, Unit, string, string, Tuple<string, int>>(" %s%P() [%d%P()]", new object[2]
    {
        QueryConditionModule.toString(state.QueryCondition@),
        count
    }, null));
}
```

```csharp
[CompilationArgumentCounts(new int[] { 1, 1 })]
public static string queryInfo(InternalState state, int count)
{
    return " " + QueryConditionModule.toString(state.QueryCondition@) + " [" + ((IFormattable)(object)count).ToString(null, CultureInfo.InvariantCulture) + "]";
}
```

[`String.Concat`](https://learn.microsoft.com/en-us/dotnet/api/system.string.concat?view=net-9.0) の方が期待しない形になっているのがよく分かる。
なので断然 String interpolation の方が速い。

| Method                  |      Mean |     Error |    StdDev | Median | Allocated |
| ----------------------- | --------: | --------: | --------: | -----: | --------: |
| InternalState_queryInfo | 0.0462 ns | 0.0330 ns | 0.0968 ns | 0.0 ns |         - |

| Method                  |      Mean |     Error |    StdDev |    Median | Allocated |
| ----------------------- | --------: | --------: | --------: | --------: | --------: |
| InternalState_queryInfo | 0.1410 ns | 0.0528 ns | 0.1516 ns | 0.0935 ns |         - |

このような細かいチューニングで、しかも最も遅いところから着手するわけでないという進め方は、仕事だと中々できない。
やはり趣味のプログラミングだからこそ、そのような重箱の隅をドリルしまくれる。

当然、これらの分析と改善は今 pocof が使ってる .NET 9 でのみこうなるという可能性が十分にある。
それでもこういう挙動を少しずつ頭の中に積んでいくことで、一般的に F# のチューニングで言われているようなパターンの原理を理解できて、良い感じがする。
