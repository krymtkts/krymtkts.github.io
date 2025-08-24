---
title: "F# で Cmdlet を書いてる pt.71"
subtitle: "Single-producer/single-consumer append-only buffer"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。

[`ConcurrentQueue`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=net-9.0) に代わる collection の実装 [#357](https://github.com/krymtkts/pocof/pull/357) に関して。
書き込み / 読み込み thread が 1 つずつで追加しかしないという限定用途から Single-producer/single-consumer append-only buffer と呼ぶことにした。
前回は、 MPMC の `ConcurrentQueue` より機能が少ない SPSC append-only buffer の方が遅くて残念な結果だったので、色々直した。
その結果、 `ConcurrentQueue` を置き換える価値があるものにできた。
以下に比較の benchmark を記す。

書き込みは以下の通り。

| Method               | EntryCount |            Mean |           Error |          StdDev | Ratio | RatioSD |      Gen0 |      Gen1 |    Gen2 |   Allocated | Alloc Ratio |
| -------------------- | ---------- | --------------: | --------------: | --------------: | ----: | ------: | --------: | --------: | ------: | ----------: | ----------: |
| ConcurrentQueue      | 100        |      1,803.8 ns |        43.06 ns |       124.25 ns |  1.00 |    0.10 |    1.0529 |         - |       - |     4.31 KB |        1.00 |
| SpscAppendOnlyBuffer | 100        |        794.2 ns |        15.84 ns |        30.14 ns |  0.44 |    0.03 |    0.2575 |         - |       - |     1.05 KB |        0.24 |
|                      |            |                 |                 |                 |       |         |           |           |         |             |             |
| ConcurrentQueue      | 10000      |    283,355.0 ns |     5,653.88 ns |    13,326.85 ns |  1.00 |    0.07 |   41.5039 |   41.5039 | 41.5039 |   257.83 KB |        1.00 |
| SpscAppendOnlyBuffer | 10000      |     89,695.3 ns |     1,787.21 ns |     2,886.01 ns |  0.32 |    0.02 |   31.0059 |    7.6904 |       - |   127.38 KB |        0.49 |
|                      |            |                 |                 |                 |       |         |           |           |         |             |             |
| ConcurrentQueue      | 1000000    | 29,405,508.3 ns | 1,432,592.75 ns | 4,178,940.60 ns |  1.02 |    0.20 |  125.0000 |   93.7500 | 93.7500 | 16387.36 KB |        1.00 |
| SpscAppendOnlyBuffer | 1000000    | 26,947,905.9 ns |   522,309.93 ns |   436,152.45 ns |  0.93 |    0.13 | 1281.2500 | 1187.5000 |       - |     7878 KB |        0.48 |

読み込みは以下の通り。

| Method                       | EntryCount |           Mean |         Error |        StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| ---------------------------- | ---------- | -------------: | ------------: | ------------: | ----: | ------: | -----: | --------: | ----------: |
| ConcurrentQueue_iterate      | 100        |       948.1 ns |      19.01 ns |      51.73 ns |  1.00 |    0.08 | 0.0172 |      72 B |        1.00 |
| SpscAppendOnlyBuffer_iterate | 100        |       858.9 ns |      17.18 ns |      30.09 ns |  0.91 |    0.06 | 0.0134 |      56 B |        0.78 |
|                              |            |                |               |               |       |         |        |           |             |
| ConcurrentQueue_iterate      | 10000      |    81,575.8 ns |   1,601.53 ns |   2,445.71 ns |  1.00 |    0.04 |      - |      72 B |        1.00 |
| SpscAppendOnlyBuffer_iterate | 10000      |    74,501.3 ns |   1,406.56 ns |   1,674.41 ns |  0.91 |    0.03 |      - |      56 B |        0.78 |
|                              |            |                |               |               |       |         |        |           |             |
| ConcurrentQueue_iterate      | 1000000    | 9,323,906.6 ns | 184,295.18 ns | 270,137.50 ns |  1.00 |    0.04 |      - |      78 B |        1.00 |
| SpscAppendOnlyBuffer_iterate | 1000000    | 7,588,917.4 ns | 123,090.41 ns | 155,669.97 ns |  0.81 |    0.03 |      - |      59 B |        0.76 |

書き込みで中規模まで大幅優位で、大規模でもわずかながら優位な感じ。
大規模でも性能を落としてきてるのは、 segment の拡張戦略があまり良くないことによるみたい。
ここはもっと改善ができたら良さそう。

読み込みは全サイズで優位そう。大規模(1M)での効果が高く、全体的にメモリ確保量が半分以下なので、これも良さそう。
segment がでかすぎて長寿命化(Gen1 昇格)してるみたいなので、ここはもっと改善ができそうかな。
ただし segment のサイズを縮めた場合は segment の数が増えて列挙の遅さに繋がる可能性がある。

性能改善に大きく寄与した具体的な変更は以下の通り。備忘のため GitHub Copilot と分析した結果も添えておく。

- [`Volatile.Read`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile.read?view=net-9.0) [`Volatile.Write`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile.write?view=net-9.0) を極力少なくする
  - `volatile` 利用により、 .NET の JIT 最適化の制限とメモリアクセス増加で遅くなる
- (読みのみ)[`IEnumerator`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.ienumerator?view=net-9.0) 実装に [`seq`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/sequences) を使わず専用の struct enumerator を実装する
  - `seq` の汎用コードを使わないことで JIT 最適化したのと、 struct 化によるヒープ除去

[`Volatile`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile?view=net-9.0) を少なくした改善はちょっとややこしかった。

```diff
 type SpscSegment<'T>(capacity: int) =
     let items: 'T array = Array.zeroCreate capacity
     let mutable next: SpscSegment<'T> | null = null
-    member _.Items = items
-    member _.Capacity = items.Length
-
-    member _.Next
-        with get () = Volatile.Read(&next)
-        and set (v) = Volatile.Write(&next, v)
+    member __.Items = items
+    member __.Capacity = items.Length
+    // NOTE: Non-volatile read: safe after reader has observed published count.
+    member __.Next = next
+    // NOTE: Writer-side publish with volatile write to establish happens-before with count increment.
+    member __.PublishNext(seg: SpscSegment<'T>) = Volatile.Write(&next, seg)
```

```diff
[<Sealed>]
 type SpscAppendOnlyBuffer<'T>() =
     // 略

     // NOTE: Volatile helpers for count.
     let readCount () = Volatile.Read(&count)
     let writeCount (v: int) = Volatile.Write(&count, v)

     member __.Add(item: 'T) : unit =
         // NOTE: Acquire local view of the current tail segment.
         let t = tail
         let idx = tailIndex

         if idx >= t.Capacity then
             // NOTE: Current segment is full: create a new one, link it, and advance tail.
             let newSeg = SpscSegment<'T>(min (t.Capacity <<< 1) segSizeMax)
             // NOTE: Publish linkage before the element becomes observable via count.
-            t.Next <- newSeg
+            t.PublishNext newSeg
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

この seq を使った `IEnumerator` 実装は遅かった。

```diff
     interface IEnumerable<'T> with
-        member __.GetEnumerator() : IEnumerator<'T> =
-            // NOTE: Snapshot the count once; traverse segments accordingly.
-            let snapshotCount = readCount ()
-            let mutable remaining = snapshotCount
-            let mutable seg = head
-            let mutable idx = 0
-
-            (seq {
-                while remaining > 0 do
-                    if idx >= seg.Capacity then
-                        seg <- seg.Next
-                        idx <- 0
-
-                    yield seg.Items[idx]
-                    idx <- idx + 1
-                    remaining <- remaining - 1
-            })
-                .GetEnumerator()
```

struct enumerator を実装することでコードは増えたが断然速くなった。

```diff
+[<Struct>]
+type SpscSegmentEnumerator<'T> =
+    val mutable private remaining: int
+    val mutable private seg: SpscSegment<'T>
+    val mutable private items: 'T array
+    val mutable private cap: int
+    val mutable private idx: int
+    val mutable private current: 'T
+
+    new(head: SpscSegment<'T>, total: int) =
+        let items = head.Items
+
+        { remaining = total
+          seg = head
+          items = items
+          cap = items.Length
+          idx = 0
+          current = Unchecked.defaultof<'T> }
+
+    // NOTE: for F# pattern enumeration optimization (zero allocation via struct enumerator).
+    member __.Current = __.current
+
+    // NOTE: for F# pattern enumeration optimization (zero allocation via struct enumerator).
+    member __.MoveNext() =
+        if __.remaining <= 0 then
+            false
+        else
+            if __.idx >= __.cap then
+                match __.seg.Next with
+                | null -> __.remaining <- 0
+                | next ->
+                    __.seg <- next
+                    let items = next.Items
+                    __.items <- items
+                    __.cap <- items.Length
+                    __.idx <- 0
+
+            if __.remaining > 0 then
+                let i = __.idx
+                __.current <- __.items[i]
+                __.idx <- i + 1
+                __.remaining <- __.remaining - 1
+                true
+            else
+                false
+
+    // NOTE: No resources to release.
+    member __.Dispose() = ()
+
+    interface IEnumerator<'T> with
+        member __.Current = __.current
+
+    interface IEnumerator with
+        member __.Current = box __.current
+        member __.MoveNext() = __.MoveNext()
+        member _.Reset() = raise (NotSupportedException())
+
+    interface IDisposable with
+        member __.Dispose() = __.Dispose()
```

`SpscSegmentEnumerator` で `count` 書き側の release (`Volatile.Write`) と読み側の acquire (`Volatile.Read`) が噛む publish 境界([happens-before](https://en.wikipedia.org/wiki/Happened-before) の境界)になっている。
なので単調に一方向へ連結された segment の、読み込みに必要な範囲については余計な `volatile` を省ける。
読み込み時は最初に snapshot した `count` の要素数分は書き終えていることが保証され、残り要素数が 0 なら `next` にアクセスすることもない。
だから `next` の読みは `volatile` なしでも安全に読める。
実は書き側も理論上 `next` の `Volatile.Write` を外せるようなのだが、まだちゃんと確認しきれていない。
release/acquire 、 happens-before といったキーワードをもっと理解したら削ってみるか。

またこのデータ型は、大前提として SPSC append-only という限定用途のみにしか適さないので、他の用途が出てきたら使い物にならなくなる。
でも pocof であれば、今のところ他の用途の可能性がないので、多分当分このままでいける。

改善できそうなところがまだ少し残ってるし、もう少し改善できたら良さげ。
`ConcurrentQueue` が pocof の性能の支配的な部分ではないからここばかり改善しても大差ないのだが、趣味なのでヨシッ！
