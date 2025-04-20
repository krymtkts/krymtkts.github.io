---
title: "F# で Cmdlet を書いてる pt.62"
subtitle: BenchmarkDotnet と ObjectLayoutInspector
tags: ["fsharp", "powershell", "dotnet", "benchmark"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[BenchmarkDotnet](https://github.com/dotnet/BenchmarkDotNet) や [ObjectLayoutInspector](https://github.com/SergeyTeplyakov/ObjectLayoutInspector) で pocof の benchmark や memory layout を可視化できるようになったので、局所的ではあるが定量化された改善をはじめた。
何故局所的かというと、結局一部の module で改善されたとて全体的に見れば負荷を他所に避けているだけのこともあり、そういう意味で局所的だからだ。

ひとまず着手したのは内部状態を管理する型 `InternalState` のダイエットだ。 [#305](https://github.com/krymtkts/pocof/pull/305)
元々可変状態のみを保存していくつもりの型だったが、幾度の機能開発を経て起動後変わらんだろうという field まで含まれるようになっていた。
理想的には更新頻度も高めで取り回されることも多いし GC を避ける目的で struct にしたいのだが、サイズが大きくなり struct にしようものなら爆発的にコピーコストが嵩むという状態だった。
本当に struct にするかは benchmark との相談だが、やるやらない関係なくとも fat なので、ひとまず減量する必要があるなと考えた。

まず `InternalState` でプログラム実行中に変わらない fields を整理して半分ほど取り除いたりしてみた。
結果 memory layout は以下のようになった。
削られた fields はカリー化された関数の引数だったりの起動時以降変更されない値に移行した。

減量前 ([15b6fe4](https://github.com/krymtkts/pocof/commit/15b6fe40738a7c1a72e4125cb87137e945d04c3f))

```plaintext
Type layout for 'InternalState'
Size: 88 bytes. Paddings: 7 bytes (%7 of empty space)
|=====================================================|
| Object Header (8 bytes)                             |
|-----------------------------------------------------|
| Method Table Ptr (8 bytes)                          |
|=====================================================|
|   0-7: QueryState QueryState@ (8 bytes)             |
|-----------------------------------------------------|
|  8-15: QueryCondition QueryCondition@ (8 bytes)     |
|-----------------------------------------------------|
| 16-23: PropertySearch PropertySearch@ (8 bytes)     |
|-----------------------------------------------------|
| 24-31: FSharpOption`1 Notification@ (8 bytes)       |
|-----------------------------------------------------|
| 32-39: IReadOnlyCollection`1 Properties@ (8 bytes)  |
|-----------------------------------------------------|
| 40-47: IReadOnlyDictionary`2 PropertyMap@ (8 bytes) |
|-----------------------------------------------------|
| 48-55: String Prompt@ (8 bytes)                     |
|-----------------------------------------------------|
| 56-63: String WordDelimiters@ (8 bytes)             |
|-----------------------------------------------------|
| 64-71: Refresh Refresh@ (8 bytes)                   |
|-----------------------------------------------------|
| 72-75: Int32 PromptLength@ (4 bytes)                |
|-----------------------------------------------------|
| 76-79: Int32 ConsoleWidth@ (4 bytes)                |
|-----------------------------------------------------|
|    80: Boolean SuppressProperties@ (1 byte)         |
|-----------------------------------------------------|
| 81-87: padding (7 bytes)                            |
|=====================================================|
```

減量後 ([9ab37b0](https://github.com/krymtkts/pocof/commit/9ab37b0460a7473955b9b61b44ac34f3fd77008c))

```plaintext
Type layout for 'InternalState'
Size: 40 bytes. Paddings: 7 bytes (%17 of empty space)
|=================================================|
| Object Header (8 bytes)                         |
|-------------------------------------------------|
| Method Table Ptr (8 bytes)                      |
|=================================================|
|   0-7: QueryState QueryState@ (8 bytes)         |
|-------------------------------------------------|
|  8-15: QueryCondition QueryCondition@ (8 bytes) |
|-------------------------------------------------|
| 16-23: PropertySearch PropertySearch@ (8 bytes) |
|-------------------------------------------------|
| 24-31: Refresh Refresh@ (8 bytes)               |
|-------------------------------------------------|
|    32: Boolean SuppressProperties@ (1 byte)     |
|-------------------------------------------------|
| 33-39: padding (7 bytes)                        |
|=================================================|
```

これにより、キー入力で内部状態を書き換える処理の benchmark は良い方向に差分が出た。そこそこ速くメモリも軽くなった。
多分取り除いた fields より、それらを更新する関数が取り除かれたことによる影響が大きそう。

減量前 ([15b6fe4](https://github.com/krymtkts/pocof/commit/15b6fe40738a7c1a72e4125cb87137e945d04c3f))

```plaintext
| Method                          | Mean       | Error    | StdDev   | Gen0   | Allocated |
|-------------------------------- |-----------:|---------:|---------:|-------:|----------:|
| invokeAction_Noop               |   131.6 ns |  2.39 ns |  2.35 ns | 0.0401 |     168 B |
| invokeAction_AddQuery           | 1,690.6 ns | 32.05 ns | 63.26 ns | 0.8106 |    3392 B |
| invokeAction_BackwardChar       |   530.5 ns | 10.64 ns | 19.98 ns | 0.1030 |     432 B |
| invokeAction_DeleteBackwardChar |   220.1 ns |  4.31 ns |  3.36 ns | 0.0401 |     168 B |
| invokeAction_SelectBackwardChar |   444.8 ns |  8.78 ns | 13.41 ns | 0.1183 |     496 B |
| invokeAction_RotateMatcher      |   202.4 ns |  4.44 ns | 11.86 ns | 0.1070 |     448 B |
| invokeAction_CompleteProperty   |   249.8 ns | 21.63 ns | 63.77 ns | 0.0496 |     208 B |
```

減量後 ([9ab37b0](https://github.com/krymtkts/pocof/commit/9ab37b0460a7473955b9b61b44ac34f3fd77008c))

```plaintext
| Method                          | Mean     | Error    | StdDev   | Gen0   | Allocated |
|-------------------------------- |---------:|---------:|---------:|-------:|----------:|
| invokeAction_Noop               | 113.9 ns |  2.29 ns |  3.90 ns | 0.0210 |      88 B |
| invokeAction_AddQuery           | 964.6 ns | 19.28 ns | 20.63 ns | 0.4406 |    1848 B |
| invokeAction_BackwardChar       | 285.4 ns |  5.79 ns | 10.58 ns | 0.0591 |     248 B |
| invokeAction_DeleteBackwardChar | 125.3 ns |  2.58 ns |  3.70 ns | 0.0210 |      88 B |
| invokeAction_SelectBackwardChar | 308.2 ns |  6.22 ns | 11.06 ns | 0.0744 |     312 B |
| invokeAction_RotateMatcher      | 131.5 ns |  2.67 ns |  2.74 ns | 0.0381 |     160 B |
| invokeAction_CompleteProperty   | 125.3 ns |  2.57 ns |  3.43 ns | 0.0286 |     120 B |
```

が、これは局所最適なだけで、全体的に見れば他所に寄せられた負荷(特に正規表現パターンのエラー表示)があるので、全体で見てどこまで効果があるかはちょっと微妙。
そんで全体的な benchmark を取る術がないのが現状。
pocof は event loop を末尾再帰で実装してて、そこで `InternalState` の受け渡しが延々と発生してるので、そのへんの benchmark も見ておきたいのだけどまだ作れてない。

とりあえず `InternalState` の減量の第 1 ステップは完了、次のステップを進めるのに先述の通り今の benchmark では心許ないなと感じている。
たまにメチャクチャ簡単に部分的な高速化が全体に好影響をもたらすケースもあるが、他に影響ないのを確認するにはやはり全体を見るものがないと確証を得にくい。
必要は感じているが全体を見る benchmark 作るのちょっとしんどいな...と思ってしまいまだ着手できておらず、結果的に局所的な改善な改善からちまちまやってしまっているのは essential ではないなーと感じる。
はよ autopilot のアイデアを実現しろという自身からの圧。
