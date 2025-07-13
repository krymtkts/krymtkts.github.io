---
title: "F# でミニゲームを書いてる Part 7"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発をした。

[#7](https://github.com/krymtkts/PSGameOfLife/pull/7) で cell のサイズ指定に対応した。
[#10](https://github.com/krymtkts/PSGameOfLife/pull/10) で CUI にも FPS の debug 機能を追加した。
CUI の FPS が見られるようになったことで、 GUI で同等の FPS が出せてるのもわかっていい感じ。
あと [#9](https://github.com/krymtkts/PSGameOfLife/pull/9) [#11](https://github.com/krymtkts/PSGameOfLife/pull/11) でちょっとした描画処理の改善した。

前回、 cell のサイズが小さいと [SIMD 命令](https://learn.microsoft.com/en-us/dotnet/standard/simd)の恩恵を受けられなくなる話を書いた。
[現状の実装](https://github.com/krymtkts/PSGameOfLife/blob/2616f36ffe851ff0b302fd4956fda600a099037f/src/PSGameOfLife/View.Avalonia.fs#L100-L149)は以下の通り。
`Vector<byte>.Count` に満たないデータはエラーになって使えないから、使わないようにしている。
(cell の byte 数が小さいと `createCellTemplate` で [`Vector`](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector?view=net-9.0) が作られないから書き込み時も `Vector` を使わない)

```fsharp
    let createCellTemplate (cellSize: int) (color: byte * byte * byte * byte) : byte array * Vector<byte> array =
        let b, g, r, a = color
        let byteLength = cellSize <<< 2
        let bytes = Array.zeroCreate<byte> byteLength

        for x in 0 .. cellSize - 1 do
            let idx = x <<< 2
            bytes.[idx] <- b
            bytes.[idx + 1] <- g
            bytes.[idx + 2] <- r
            bytes.[idx + 3] <- a

        let nvec = byteLength / vectorSize
        let vectors = Array.init nvec (fun i -> Vector<byte>(bytes, i * vectorSize))
        let offset = nvec * vectorSize
        let rem = if byteLength > offset then bytes.[offset..] else [||]
        rem, vectors

    // 略

    let writeTemplateSIMD (dst: nativeptr<byte>) (vectors: Vector<byte> array) (rem: byte array) =
        let baseAddr = NativePtr.toNativeInt dst

        for i = 0 to vectors.Length - 1 do
            Unsafe.WriteUnaligned((baseAddr + nativeint (i * vectorSize)).ToPointer(), vectors.[i])

        let offset = vectors.Length * vectorSize

        if rem.Length > 0 then
            let dstRemPtr = NativePtr.add dst offset
            // NOTE: pinning the template array to avoid GC moving it.
            use ptr = fixed &rem.[0]

            Unsafe.CopyBlockUnaligned(NativePtr.toVoidPtr dstRemPtr, NativePtr.toVoidPtr ptr, uint32 rem.Length)
```

このため、 1x1 みたいな極端に dot の数が小さい cell の場合に SIMD を使わないので書き込みメモリ効率が落ちる。
これを改善したかったので、試しに毎 frame [stackalloc](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-45#span-and-byref-like-structs) で確保した buffer へデータを溜め込んで行データをまとめて SMID で書き込む方法を試してみたのだけど、大差なかった。
現状の定義済み `Vector` と余りの byte を書き込む方法だと、毎 frame で BGRA のための loop や `Vector` の生成が不要なので同等に速いみたい。
なんか直感的にはまだまだできそうな感じはするけどな～、今はまだ技量が足りない。
他に思いつく高速化は [byref](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs) で破壊的操作をして copy を減らすとかかな。効果あるかな...

あと、現状 GUI で可能な最大サイズ 1000x1000 にすると、 2 FPS とかで極めて重い。
cell の数を減らして、代わりに size を増やして window が大きくしても描画が劣化しないので、多分 cell が多いことに起因してる。
ゲームの core な部分はまだ改善できそう。
array を使うようにはしてるけど、 array の再生成を最小限にするとかの最適化はそれほどやってないし。
ただそこに手をいれるには現状の simple な盤面の管理を二重にしてやる必要があって、気が進まない。
game of life は cell の生死を判断するのに周りの cell の状態に依存するから、逐次書き換えができないのよね。
都度盤面を copy するとそれだけ時間がかかるし、だとしたら二重 buffer しかないと。
速さのためには多少の不満も受け入れないといけないか。
当面大きい盤面での処理効率が向上しない間は全画面機能を追加せずいこう。

他にも Linux でのみ shortcut key で終了すると window が残る bug もあったりして、 cross platform は改めて難しいな～というのを感じている。
現状はいちいち動作確認してるから、 GUI の end-to-end testing が要るのかなと思ってる。
一応それに使えそうな [Avalonia の Headless platform](https://docs.avaloniaui.net/docs/concepts/headless/) というのがあるらしい。
実際に使えるのかは試してみないとちょっとわからんな。

何にせよもうそろそろ PowerShell Gallery へ公開してもいいなという気持ちになってきてる。
GUI 版のための document 拡充とかも始める頃合いか。
自分の仕事 PC で game of life を流しながら休憩する未来はすぐそこにある。
