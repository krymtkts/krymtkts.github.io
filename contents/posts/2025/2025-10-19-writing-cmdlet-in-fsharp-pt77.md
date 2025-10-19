---
title: "F# で Cmdlet を書いてる pt.77"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。

とりあえず query を正規表現で分割してから組み立てるのでなくて自前の parser で組み立てるのに書き直し始めたところ。
Parser Combinator にするっつってたが、アレはやめた。
どうしても抽象度が上がって関数の数が増えることによる overhead で、さほど速くならないのがわかったからだ。
結局 pocof のような小規模な構文だと、小さな関数にまとめた方が圧倒的に速いのがわかってそうした。速さは正義だ。

まず既存のコードに不足しているテストを足して benchmark を足して、そんでからまず単純に書き換えた版を作った。 [#372](https://github.com/krymtkts/pocof/pull/372)

benchmark は以下の通り。

before(regular expression)

| Method               | QueryCount |     Mean |     Error |    StdDev |   Median | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------- | ---------- | -------: | --------: | --------: | -------: | ----: | ------: | -----: | --------: | ----------: |
| prepareNormalQuery   | 1          | 1.455 us | 0.0291 us | 0.0638 us | 1.429 us |  1.00 |    0.06 | 0.6828 |   2.79 KB |        1.00 |
| preparePropertyQuery | 1          | 1.658 us | 0.0327 us | 0.0573 us | 1.652 us |  1.14 |    0.06 | 0.7210 |   2.95 KB |        1.06 |
|                      |            |          |           |           |          |       |         |        |           |             |
| prepareNormalQuery   | 3          | 2.872 us | 0.0574 us | 0.1353 us | 2.830 us |  1.00 |    0.07 | 1.3275 |   5.45 KB |        1.00 |
| preparePropertyQuery | 3          | 3.310 us | 0.0650 us | 0.1067 us | 3.330 us |  1.16 |    0.06 | 1.4267 |   5.84 KB |        1.07 |
|                      |            |          |           |           |          |       |         |        |           |             |
| prepareNormalQuery   | 5          | 4.249 us | 0.0847 us | 0.1484 us | 4.224 us |  1.00 |    0.05 | 1.9989 |   8.19 KB |        1.00 |
| preparePropertyQuery | 5          | 5.064 us | 0.0920 us | 0.2187 us | 5.011 us |  1.19 |    0.07 | 2.1439 |   8.78 KB |        1.07 |
|                      |            |          |           |           |          |       |         |        |           |             |
| prepareNormalQuery   | 7          | 5.615 us | 0.1081 us | 0.1585 us | 5.626 us |  1.00 |    0.04 | 2.6550 |  10.84 KB |        1.00 |
| preparePropertyQuery | 7          | 6.571 us | 0.1270 us | 0.1901 us | 6.597 us |  1.17 |    0.05 | 2.8381 |   11.6 KB |        1.07 |

after(tokenizer + parser)

| Method               | QueryCount |     Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------- | ---------- | -------: | --------: | --------: | ----: | ------: | -----: | --------: | ----------: |
| prepareNormalQuery   | 1          | 1.273 us | 0.0241 us | 0.0237 us |  1.00 |    0.03 | 0.6828 |   2.79 KB |        1.00 |
| preparePropertyQuery | 1          | 1.334 us | 0.0261 us | 0.0428 us |  1.05 |    0.04 | 0.7210 |   2.95 KB |        1.06 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 3          | 2.446 us | 0.0485 us | 0.0539 us |  1.00 |    0.03 | 1.3313 |   5.45 KB |        1.00 |
| preparePropertyQuery | 3          | 2.640 us | 0.0527 us | 0.0881 us |  1.08 |    0.04 | 1.4267 |   5.84 KB |        1.07 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 5          | 3.735 us | 0.0730 us | 0.1179 us |  1.00 |    0.04 | 1.9989 |   8.19 KB |        1.00 |
| preparePropertyQuery | 5          | 3.968 us | 0.0778 us | 0.1115 us |  1.06 |    0.04 | 2.1439 |   8.78 KB |        1.07 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 7          | 4.959 us | 0.0979 us | 0.1005 us |  1.00 |    0.03 | 2.6550 |  10.84 KB |        1.00 |
| preparePropertyQuery | 7          | 5.168 us | 0.0983 us | 0.1244 us |  1.04 |    0.03 | 2.8381 |   11.6 KB |        1.07 |

after(parser index)

| Method               | QueryCount |     Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------- | ---------- | -------: | --------: | --------: | ----: | ------: | -----: | --------: | ----------: |
| prepareNormalQuery   | 1          | 1.208 us | 0.0238 us | 0.0491 us |  1.00 |    0.06 | 0.6523 |   2.66 KB |        1.00 |
| preparePropertyQuery | 1          | 1.239 us | 0.0245 us | 0.0490 us |  1.03 |    0.06 | 0.6866 |    2.8 KB |        1.05 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 3          | 2.358 us | 0.0467 us | 0.1054 us |  1.00 |    0.06 | 1.2970 |    5.3 KB |        1.00 |
| preparePropertyQuery | 3          | 2.517 us | 0.0492 us | 0.0875 us |  1.07 |    0.06 | 1.3657 |   5.58 KB |        1.05 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 5          | 3.524 us | 0.0691 us | 0.1155 us |  1.00 |    0.05 | 1.9455 |   7.95 KB |        1.00 |
| preparePropertyQuery | 5          | 3.753 us | 0.0741 us | 0.1530 us |  1.07 |    0.06 | 2.0370 |   8.34 KB |        1.05 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 7          | 4.680 us | 0.0932 us | 0.1861 us |  1.00 |    0.06 | 2.5864 |  10.59 KB |        1.00 |
| preparePropertyQuery | 7          | 4.999 us | 0.0990 us | 0.1810 us |  1.07 |    0.06 | 2.7237 |  11.13 KB |        1.05 |

before(regular expression) が元で、 after(parser index) が最終形。
after(tokenizer + parser) は中間の状態で、元の tokenizer と parser が別れてたやつ。これも統合したほうが速くなった。
結果的に、普通の query と property query 共に元の 2 割ほど実行時間を短縮できた。

さらにここから、差分 compile を導入するだとか、コードクォートで filtering の predicate を構築するのが別れてるのを統合するだとかができたら、更に良さそう。

今回書いてみて思ったのが、効率が良い自前の parser を書くのは、実際のところ [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) を target framework にしてると厳しい。
benchmark を取った訳では無いが、多分 [ReadOnlySpan](https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1?view=net-9.0) を使えばまだ追求できるんじゃないかな。
やっぱ文字列操作が重いからな。
でも .NET Standard 2.0 では使えないから仕方がないね。

将来的に multi target で build して配布するというのもアリかも知れないが、ややこしいしまだ本気では考えてない。
でも Windows PowerShell がいつなくなるかも決まってないから、 multi target が今後性能を追求していくとしたら一番現実的かな。

query の parser を自前で書くのに着手する前は、気持ちを整えるため、ムダに割当されていた List の除去だったり諸々の改善と、その過程で見つけた旧来からの bug を修正していた。
結構しょぼい bug が残ってたので、前にも触れたがテストケースを更新するのも重要になってきたな～。
[#370](https://github.com/krymtkts/pocof/pull/370) [#371](https://github.com/krymtkts/pocof/pull/371)

結構書き換わってる気がするので、もうそろそろ新しいの出して自分の日々の仕事でも使おうかな。
