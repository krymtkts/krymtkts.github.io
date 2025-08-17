---
title: "F# で Cmdlet を書いてる pt.70"
subtitle: "Single-producer/single-consumer lock-free buffer(仮)"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[`StreamWriter.AutoFlush`](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamwriter.autoflush?view=net-9.0) を無効にした console output の最適化は失敗した。
カーソル位置の計算が難しく、期待通りの挙動をさせるには、書き込みの都度 [`StreamWriter.Flush`](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamwriter.flush?view=net-9.0) する必要があり、意味ないなと諦めた。
代わりに [`Console.Write`](https://learn.microsoft.com/en-us/dotnet/api/system.console.write?view=net-9.0) の呼び出し回数を減らすための改善をした。 [#351](https://github.com/krymtkts/pocof/pull/351)
I/O が減った分、 CPU が忙しいときも多少キビキビするようになった気はするかな。
`StreamWriter.AutoFlush` 作戦は、経験を積んだあと再挑戦してもいいか。

他にも hot path での list 生成をなくすとか struct を利用することで heap 割り当てをなくすとか。
[#352](https://github.com/krymtkts/pocof/pull/352)
[#354](https://github.com/krymtkts/pocof/pull/354)
[#356](https://github.com/krymtkts/pocof/pull/356)
劇的に性能に影響のある修正ではないので、コツコツ積み重ねるようなものばかり。

list 生成を減らすパターンは極端に hot な箇所なら劇的な効果があるが、今回特に顕著な差はなかった。
これ系の最適化を進めていると、やはり pocof の状態管理を担っている record が気になってくるところ。
小さい record に分割するか、 [`byref`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs) を組み合わせるか。
この話はもう何度も書いてるが着手してないのはひとえに先述のようにそこまで劇的な変化がないからやる気が出ないのかもな。

他方、今気になってるのは絞り込み対象のデータを保管するのに [ConcurrentQueue](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=net-9.0) を利用している点だ。
書き込みのみの thread と読み込みのみの thread が分かれているので、安全に読み書きするためにと思って .NET にすでにある順序を保持するデータ構造を採用したらこうなった。
このケースはいわゆる Single-producer/single-consumer (SPSC) というパターンみたいで、 lock-free なデータ構造でも捌けるみたい。
内部的には分割された [`array`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/arrays) を持っているだけで、参照は head の、追加は tail の pointer を参照する。
例えば .NET の [`ArrayList`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.arraylist?view=net-9.0) 実装はサイズ拡張時に内部配列を swap するので、そのときに競合しうる。
SPSC lock-free buffer(仮) だと参照で決まったサイズまでしか読まないので書き込みと競合しない。

`ConcurrentQueue` は複数 thread からの書き込みに対応してる点からも書き込み時点で順序保持するため何らかの lock をしていそうと考えた。
であれば lock-free なデータ構造よりも追加時の overhead がある。
なので、もしかしたらデータ構造の変更で速くなるかもなと検証を始めた。

ところが試しに実装してみたところ成果が出なかった。
[BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) で追加だけ 100 件・ 10,000 件・ 1,000,000 件を計測してみた。
10,000 件はなぜか順調だが、 100 件は差がなく、 1,000,000 件のような大量件数だとむしろ遅い。[#357](https://github.com/krymtkts/pocof/pull/357)
segment のサイズを固定にしてるので、件数が少ないとメモリが過剰だが、多いときは良さそう。

```plaintext
// Benchmark Process Environment Information:
// BenchmarkDotNet v0.14.0
// Runtime=.NET 9.0.8 (9.0.825.36511), X64 RyuJIT AVX2
// GC=Concurrent Workstation
// HardwareIntrinsics=AVX2,AES,BMI1,BMI2,FMA,LZCNT,PCLMUL,POPCNT VectorSize=256
// Job: DefaultJob
```

| Method               | EntryCount |          Mean |         Error |        StdDev | Ratio | RatioSD |      Gen0 |      Gen1 |     Gen2 |   Allocated | Alloc Ratio |
| -------------------- | ---------- | ------------: | ------------: | ------------: | ----: | ------: | --------: | --------: | -------: | ----------: | ----------: |
| ConcurrentQueue      | 100        |      5.413 us |     0.1075 us |     0.2492 us |  1.00 |    0.06 |    5.2948 |    0.0076 |        - |    21.63 KB |        1.00 |
| SpscAppendOnlyBuffer | 100        |      5.507 us |     0.1088 us |     0.2069 us |  1.02 |    0.06 |    6.2103 |         - |        - |    25.41 KB |        1.17 |
|                      |            |               |               |               |       |         |           |           |          |             |             |
| ConcurrentQueue      | 10000      |  2,019.421 us |    40.3491 us |    76.7684 us |  1.00 |    0.05 |  351.5625 |  230.4688 |  82.0313 |  2279.87 KB |        1.00 |
| SpscAppendOnlyBuffer | 10000      |  1,071.990 us |    20.8937 us |    35.4790 us |  0.53 |    0.03 |  353.5156 |  335.9375 |        - |  2102.62 KB |        0.92 |
|                      |            |               |               |               |       |         |           |           |          |             |             |
| ConcurrentQueue      | 100000     | 54,099.111 us | 1,072.9361 us | 1,235.5948 us |  1.00 |    0.03 | 4100.0000 | 2400.0000 | 800.0000 | 22354.19 KB |        1.00 |
| SpscAppendOnlyBuffer | 100000     | 56,827.114 us | 2,239.2215 us | 6,167.4668 us |  1.05 |    0.12 | 4181.8182 | 2454.5455 | 818.1818 | 21093.03 KB |        0.94 |

当然だが `ConcurrentQueue` は相当速いな。
実装見てなかったので調べてみたら、同じ原理を利用して極力 lock-free で動くようになってた。
実装をみたらわかるが、 Compare And Swap (CAS)というパターンを使っているみたい。
なので lock が必要になるような競合が発生しない限りは十分に速いわ。振り出しに戻ってしまった。

- [runtime/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentQueue.cs](https://github.com/dotnet/runtime/blob/58d1c2e3e9bc76a4a9e02af75eeba210800f54eb/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentQueue.cs)
- [FAQ :: Are all of the new concurrent collections lock-free? - .NET Blog](https://devblogs.microsoft.com/dotnet/faq-are-all-of-the-new-concurrent-collections-lock-free/)
  - > ConcurrentQueue<T> and ConcurrentStack<T> are completely lock-free in this way. They will never take a lock, but they may end up spinning and retrying an operation when faced with contention (when the CAS operations fail).

薄々気づいてはいたが、もうデータ保持の形式に関しては trie を採用しない状態でできることはなさそうな感じしてきた。
でも pocof での SPSC append-only な用途の方が明らかにやること少ないし、このまま諦めるのはなんか違う気がするな。
少しでも何かできることないんかな～。悪あがきしたい。
特に今回計測した追加のときよりも絞り込みのときの速度改善が肝(なんで今やらないかな)なので、そこも含めて見極めたいな。

続く。
