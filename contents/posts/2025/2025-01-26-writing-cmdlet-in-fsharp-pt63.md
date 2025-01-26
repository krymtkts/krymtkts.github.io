---
title: "F# で Cmdlet を書いてる pt.63"
tags: ["fsharp", "powershell", "dotnet", "benchmark"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

修正したい箇所の benchmark がなかったら作って、直して、修正後にまた benchmark を取って最適化の効果が出ているかを実践してみた。 [#315](https://github.com/krymtkts/pocof/pull/315)

今回は単純に [`array`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-array-1.html) に対して [`Seq` module](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seqmodule.html) を使ってた箇所を [`Array` module](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-arraymodule.html) を使うようにしてみたのだが、要素数が少なく遅延評価が必要ない箇所ばかりでやはり `array` のまま扱った方が処理速度が良い。

[`Seq.filter`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seqmodule.html#filter) を [`Array.filter`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-arraymodule.html#filter) にする前。

```plaintext
| Method              |      Mean |     Error |    StdDev |    Median |    Gen0 |   Gen1 | Allocated |
| ------------------- | --------: | --------: | --------: | --------: | ------: | -----: | --------: |
| Action_fromString   | 642.77 us | 12.622 us | 27.171 us | 635.54 us | 89.8438 | 1.9531 | 369.12 KB |
| Operator_fromString |  12.78 us |  0.430 us |  1.241 us |  13.18 us |  0.9766 |      - |   4.11 KB |
```

修正後の方が良い。 `Operator_fromString` の方は共通化の影響で使わない [`Array.filter`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-arraymodule.html#filter) が 1 箇所噛んでるから余計に `array` を作ってしまったのかメモリ増えたけど。
こういうのも今後は削って切り詰めていくのがやっぱ妥当かな。

```plaintext
| Method              |      Mean |     Error |    StdDev |    Gen0 | Allocated |
| ------------------- | --------: | --------: | --------: | ------: | --------: |
| Action_fromString   | 635.84 us | 12.393 us | 15.220 us | 89.8438 | 368.22 KB |
| Operator_fromString |  12.10 us |  0.215 us |  0.328 us |  1.0986 |   4.56 KB |
```

あとこれは元のコードが拙かっただけだが、不必要に [`seq`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seq-1.html) を [`list`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-list-1.html) にしてたところを `seq` のママ取り扱ったり、 `seq` に index access してたところを infinite sequence を実装して次の要素を取るだけにしてみたり。

修正前。

```plaintext
| Method                                 |       Mean |    Error |    StdDev |     Median |   Gen0 | Allocated |
| -------------------------------------- | ---------: | -------: | --------: | ---------: | -----: | --------: |
| invokeAction_CompleteProperty_NoSearch |   131.2 ns |  2.64 ns |   2.82 ns |   130.8 ns | 0.0286 |     120 B |
| invokeAction_CompleteProperty_Search   | 1,173.7 ns | 51.83 ns | 152.82 ns | 1,099.7 ns | 0.3319 |    1392 B |
| invokeAction_CompleteProperty_Rotate   |   535.5 ns | 10.69 ns |  15.68 ns |   534.7 ns | 0.1869 |     784 B |
```

[`List.ofSeq`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-listmodule.html#ofSeq) 取り除きで高速化＆メモリ減。
(`invokeAction_CompleteProperty_NoSearch` は影響受けないはずだが何でかメモリ消費が減ってる)

```plaintext
| Method                                 |     Mean |    Error |   StdDev |   Median |   Gen0 | Allocated |
| -------------------------------------- | -------: | -------: | -------: | -------: | -----: | --------: |
| invokeAction_CompleteProperty_NoSearch | 131.5 ns |  2.53 ns |  3.63 ns | 130.2 ns | 0.0229 |      96 B |
| invokeAction_CompleteProperty_Search   | 714.5 ns | 28.08 ns | 82.79 ns | 684.4 ns | 0.2041 |     856 B |
| invokeAction_CompleteProperty_Rotate   | 476.3 ns |  9.31 ns | 13.05 ns | 470.9 ns | 0.1564 |     656 B |
```

infinite sequence にした版。 [infinite sequence の実装](https://github.com/krymtkts/pocof/blob/2764077e96ad8b4a9e0a12daa79ce5d28325a151/src/pocof/Data.fs#L73-L79)自体は再帰的な `seq` 。

```fsharp
    module Seq =
        let rec cycle source =
            seq {
                yield! source
                yield! cycle source
            }
```

`PropertySearch.Rotate` という DU が元々 `Rotate of keyword: string * candidates: string seq` だったのが `Rotate of keyword: string * candidates: string seq` になって tuple が 4 byte 縮んでると思うのだけどそれよりも infinite sequence によるオーバヘッドの方がメモリに乗ってるのか？
処理速度もメモリもパフォは良くないけど、次の index を算出するあたりが煩雑だったしわかりやすさとのトレードオフということで。
再帰的な `seq` を作る際に `Seq.cache` で計算済みにしたら速くなるかなと思ったけどむしろ遅くメモリ使用量も多くなったのでやめた。

```plaintext
| Method                                 |     Mean |    Error |   StdDev |   Median |   Gen0 | Allocated |
| -------------------------------------- | -------: | -------: | -------: | -------: | -----: | --------: |
| invokeAction_CompleteProperty_NoSearch | 124.0 ns |  2.57 ns |  3.77 ns | 123.1 ns | 0.0210 |      88 B |
| invokeAction_CompleteProperty_Search   | 717.5 ns | 26.57 ns | 77.50 ns | 749.7 ns | 0.2232 |     936 B |
| invokeAction_CompleteProperty_Rotate   | 534.7 ns | 10.66 ns | 17.21 ns | 529.6 ns | 0.2060 |     864 B |
```

こうやって、速くなっているのかメモリ消費が減っているのか、特に速度に関しては実行事のブレが大きいのもあれど、定量的に測れるというのは良い。
この手でダメなら次の手で、の試行錯誤をするのに明確な理由付けができる。
今は開発時に local で走らせてるだけだが、理想は CI で benchmark testing して時間・空間計算量が悪化してたら job をこけさせれたら良いけど、 GitHub Actions だと中々ブレが大きいようなので当面は様子見か。

良いな～と思っているけど良くないところも当然ある。いちいち benchmark を取るから開発に時間がかかる。
今はまだ慣れてないから枝葉の部分でうまく使えるか練習してるような状態だが、ほんとはもっと中核の部分だけ開発時に benchmark を組み込むとかにした方が作業効率はいいんやろな。

今月ちまちまいじった分をそろそろリリースするか。

---

おまけで、 benchmark の改善とは関係ないけど [`Seq.last`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seqmodule.html#last) を [`Array.last`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-arraymodule.html#last) に変えた箇所の coverage が悪化することがあった。
理由は [`Array.last` が inline 展開されると要素数 0 の場合にエラーを発生させる分岐が埋め込まれる](https://github.com/dotnet/fsharp/blob/c4d36d699c52121cf2933314dac02b84a1a5a87a/src/FSharp.Core/array.fs#L33-L40)から([`Seq.last` は inline じゃない](https://github.com/dotnet/fsharp/blob/c4d36d699c52121cf2933314dac02b84a1a5a87a/src/FSharp.Core/seq.fs#L1714-L1720))。
要素数が 1 以上であることを保証できるケースであれば単純に `array[array.Length - 1]` して不要な分岐の発生を回避するしかない感じ。
