---
title: "F# で Cmdlet を書いてる pt.72"
subtitle: "Single-producer/single-consumer append-only buffer 改善"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。

[`ConcurrentQueue`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=net-9.0) に代わる collection の実装をまだやってた。
[#358](https://github.com/krymtkts/pocof/pull/358) [#359](https://github.com/krymtkts/pocof/pull/359) で実施。

分岐の改善、 property access の削減等ちまちました改善と、あと bug が混入してたので coverage 100% にするためのテストを AI に追加させてみた。
大幅に性能が改善された感じではない。
でも個人的には勉強なったものもある。例えば [Happened-before](https://en.wikipedia.org/wiki/Happened-before) や [`MethodImplOptions.AggressiveInlining`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.methodimploptions?view=net-9.0) 。

元々 SPSC append-only buffer では件数の読み書きと次の segment の更新に [`Volatile`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile?view=net-9.0) を利用していた。
次の segment の読みに [`Volatile.Read`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile.read?view=net-9.0) が不要なのは happens-before 関係があるためだった。
実はこの次の segment の更新も同様みたいで、結局件数の読み書きによって次の segment の更新は保証されてるようだった。
以下にざっくり変更箇所を示す。

```diff
type SpscSegment<'T>(capacity: int) =
     let mutable next: SpscSegment<'T> | null = null
     member __.Items = items
     member __.Capacity = items.Length
-    // NOTE: Non-volatile read: safe after reader has observed published count.
-    member __.Next = next
-    // NOTE: Writer-side publish with volatile write to establish happens-before with count increment.
-    member __.PublishNext(seg: SpscSegment<'T>) = Volatile.Write(&next, seg)
+    // NOTE: Non-volatile reads/writes are safe once the reader has observed the published SpscAppendOnlyBuffer.count.
+    member __.Next
+        with get () = next
+        and set v = next <- v

...

type SpscAppendOnlyBuffer<'T>() =
     let readCount () = Volatile.Read(&count)
     let writeCount (v: int) = Volatile.Write(&count, v)

+    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
     member __.Add(item: 'T) : unit =
         // NOTE: Acquire local view of the current tail segment.
         let t = tail
         let idx = tailIndex
+        let cap = t.Capacity

-        if idx >= t.Capacity then
+        if idx >= cap then
             // NOTE: Current segment is full: create a new one, link it, and advance tail.
-            let newSeg = SpscSegment<'T>(min (t.Capacity <<< 1) segSizeMax)
+            let newSeg = SpscSegment<'T>(min (cap <<< 1) segSizeMax)
             // NOTE: Publish linkage before the element becomes observable via count.
-            t.PublishNext newSeg
+            t.Next <- newSeg
             tail <- newSeg
             // NOTE: Write into the new tail
             newSeg.Items[0] <- item
             tailIndex <- 1
         else
             t.Items[idx] <- item
             tailIndex <- idx + 1

         // NOTE: Publish new count after the element write (happens-before for reader).
         // NOTE: A volatile read is not required because SPSC guarantees a single writer.
         writeCount (count + 1)
```

AI に説明させたり順を追って注意深く読むとわかるのだけど、一目で「あ～ happens-before 関係やな(ｷﾘｯ」といえるくらいには理解したいな。

あと [`MethodImplOptions.AggressiveInlining`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.methodimploptions?view=net-9.0) を初めて知った。
.NET の JIT に対して(少しは AOT に対しても) inlining したいというヒントを強く与える属性みたい。
あくまでヒントで必ずではない。例えばクソデカメソッドなんかだと inlining されないらしい。
SPSC append-only buffer の場合は元々付与したいメソッドが小さいので付けてなくても inlining されることが多いようだが、 hot path では付与する価値がある。
F# の inline は IL を複製するパターンで、こちらは JIT の実行時に複製を判断するという違いがある。

今振り返ると `MoveNext` と `Add` にはつける意味あるが `GetEnumerator` は呼び出し回数がさほど多くないだろうし意味なさそうだ。
まとめて属性を付けて bench 結果が改善したようだったのでそのままにしてたけど、削っていいな。

最終的な benchmark はこんな感じだった。

Add

| Method               | EntryCount | Mean            | Error         | StdDev        | Ratio | RatioSD | Gen0      | Gen1     | Gen2    | Allocated   | Alloc Ratio |
|--------------------- |----------- |----------------:|--------------:|--------------:|------:|--------:|----------:|---------:|--------:|------------:|------------:|
| ConcurrentQueue      | 100        |      1,853.6 ns |      49.35 ns |     141.60 ns |  1.01 |    0.11 |    1.0529 |        - |       - |     4.31 KB |        1.00 |
| SpscAppendOnlyBuffer | 100        |        829.8 ns |      16.53 ns |      33.40 ns |  0.45 |    0.04 |    0.2575 |        - |       - |     1.05 KB |        0.24 |
|                      |            |                 |               |               |       |         |           |          |         |             |             |
| ConcurrentQueue      | 10000      |    278,770.7 ns |   5,498.68 ns |   9,773.89 ns |  1.00 |    0.05 |   41.5039 |  41.5039 | 41.5039 |   257.83 KB |        1.00 |
| SpscAppendOnlyBuffer | 10000      |     88,778.9 ns |   1,691.91 ns |   3,177.83 ns |  0.32 |    0.02 |   19.4092 |        - |       - |    79.66 KB |        0.31 |
|                      |            |                 |               |               |       |         |           |          |         |             |             |
| ConcurrentQueue      | 1000000    | 23,523,948.8 ns | 438,956.52 ns | 505,502.98 ns |  1.00 |    0.03 |  125.0000 |  93.7500 | 93.7500 | 16387.36 KB |        1.00 |
| SpscAppendOnlyBuffer | 1000000    | 27,094,229.8 ns | 427,652.16 ns | 357,108.92 ns |  1.15 |    0.03 | 1281.2500 | 937.5000 |       - |  7868.55 KB |        0.48 |

Iterate

| Method                       | EntryCount | Mean            | Error         | StdDev          | Median          | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |----------------:|--------------:|----------------:|----------------:|------:|--------:|-------:|----------:|------------:|
| ConcurrentQueue_iterate      | 100        |        950.3 ns |      18.94 ns |        54.04 ns |        950.3 ns |  1.00 |    0.08 | 0.0172 |      72 B |        1.00 |
| SpscAppendOnlyBuffer_iterate | 100        |        733.9 ns |      14.60 ns |        22.29 ns |        737.6 ns |  0.77 |    0.05 | 0.0134 |      56 B |        0.78 |
|                              |            |                 |               |                 |                 |       |         |        |           |             |
| ConcurrentQueue_iterate      | 10000      |     79,012.7 ns |   1,571.08 ns |     2,989.13 ns |     78,779.6 ns |  1.00 |    0.05 |      - |      72 B |        1.00 |
| SpscAppendOnlyBuffer_iterate | 10000      |     65,541.9 ns |   1,287.66 ns |     2,541.71 ns |     65,775.4 ns |  0.83 |    0.04 |      - |      56 B |        0.78 |
|                              |            |                 |               |                 |                 |       |         |        |           |             |
| ConcurrentQueue_iterate      | 1000000    | 12,770,833.4 ns | 727,602.69 ns | 2,099,300.27 ns | 12,829,000.0 ns |  1.03 |    0.25 |      - |      74 B |        1.00 |
| SpscAppendOnlyBuffer_iterate | 1000000    |  8,072,519.3 ns | 442,006.93 ns | 1,232,138.74 ns |  7,454,282.8 ns |  0.65 |    0.15 |      - |      62 B |        0.84 |

大量件数の追加以外は `ConcurrentQueue` より良さそう。
`ConcurrentQueue` は 32 ~ 1,048,576(1024 * 1024) の範囲で segment の要素数を決めてる。
これが大量件数の場合に利いてくるっぽい。
[runtime/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentQueue.cs](https://github.com/dotnet/runtime/blob/4076cf5ddd119af41bc1122be8e079ff23003095/src/libraries/System.Private.CoreLib/src/System/Collections/Concurrent/ConcurrentQueue.cs#L42-L49)

代わりに bench で gen2 が発生してるし、多分大量の要素があるケースで [LOH](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/large-object-heap) が発生することは諦めてる設計と思われる。
まあ確かに queue にそんな件数溜まるのは、データが適切に捌けてないケースだけか。

PowerShell の場合だと、 Cmdlet は呼び出しが終われば破棄されるが、そのときに LOH があると破棄のときに必ず回収されるものではない。
つまり同じ PowerShell の session で Cmdlet の実行を繰り返すと LOH が溜まり続けることになる。
この認識で合ってるかな。
こうしてみると、 pocof はなるべく LOH を発生させないようにして session を clean に保った方が無難に思えるな。
いま LOH の発生を嫌って segment 内の配列サイズを最大 1024 にしてる。
もし `ConcurrentQueue` と同じようなクソデカ segment 方式にすれば多分速くなるけど LOH が発生しメモリが肥大化し続けそう。
.NET や PowerShell Cmdlet のメモリの仕組みに詳しくないので、もっと研究しておく必要があるな。

とりあえず今のままが一旦良さそうに思えてきたな。
一応この現状で良さそうな踏ん切りはついてきたので、いい加減 pocof を新しい version で公開する方向で動くか。
