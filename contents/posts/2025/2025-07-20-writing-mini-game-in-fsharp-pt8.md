---
title: "F# でミニゲームを書いてる Part 8"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発をした。

[前回](/posts/2025-07-13-writing-mini-game-in-fsharp-pt7.html) こう書いた。

> ゲームの core な部分はまだ改善できそう。
> array を使うようにはしてるけど、 array の再生成を最小限にするとかの最適化はそれほどやってないし。
> ただそこに手をいれるには現状の simple な盤面の管理を二重にしてやる必要があって、気が進まない。

面倒だったのだけど試しに array の生成をやめてみたら FPS が 2 倍ほど高速化された。流石に笑える。
なので気が進まないとかそっちのけで対応した。
描画範囲外の隣接 cell を除外するところで [`Array.choose`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-arraymodule.html#choose) を使って array を生成していたので、単純な [`for`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/loops-for-in-expression) loop に置き換え。
FP ぽくないが性能のためには仕方ないのだ。

またこれも前回触れた二重 buffer 方式を一番単純な関数の引数にとる形で実装した。
世代交代の度に 2 つの buffer を入れ替えて使う。
そのため [`outref`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs#outref-semantics) を用いて mutable な操作を使うように変えている。
[コード](https://github.com/krymtkts/PSGameOfLife/blob/0140b2104bbb72558f462042134d85dc31ffbd20/src/PSGameOfLife/Core.fs#L49-L70)は以下のようにした。
処理を関数に小分けして分かりやすくしていたのもやめて全部統合したので、ちょっと見通しは悪いかも知れない。

```fsharp
let nextGeneration (buffer: outref<Cell[,]>) (board: outref<Board>) =
    let columns = int board.Column
    let rows = int board.Row
    let tmp = buffer

    for y in 0 .. rows - 1 do
        for x in 0 .. columns - 1 do
            let mutable lives = 0

            for dx, dy in neighborOffsets do
                let nx, ny = x + dx, y + dy

                if nx >= 0 && nx < columns && ny >= 0 && ny < rows && board.Cells.[ny, nx].IsLive then
                    lives <- lives + 1

            tmp.[y, x] <- nextCellState board.Cells.[y, x] lives

    buffer <- board.Cells
    board.Generation <- board.Generation + 1
    board.Lives <- tmp |> countLiveCells
    board.Cells <- tmp
```

これらの対応により、愚直な実装にしては結構速くなったんじゃないかな。
今のところ GUI は CUI より速いのだけど、瞬間最高風速 1800 FPS 台だったので、理論上 0.5ms くらいで 1 frame 描画してる。速いなー。
でも game of life ガチ勢は専用の algorithm を採用し、極めて大きな盤面でももの凄い高速の simulation ができるらしい。
盤面が大きいとこちらは数十 FPS なのでまだまだやな。
まだ並列化を施してないから改善の余地は残されてる。

---

他に対応したいものは、これも前回触れた Linux でのみ shortcut key で終了すると window が残る bug だ。
Windows では発生しなくて、 Ubuntu on WSL2 でのみこの現象を確認している。 純 Linux は持ってないのでわからん。
まだ原因がわかっておらず対処できてない。

調査の過程で 1 つわかったのは、 Avalonia の [`UIThread`](https://api-docs.avaloniaui.net/docs/P_Avalonia_Threading_Dispatcher_UIThread) に [`InvokeAsync`](https://api-docs.avaloniaui.net/docs/M_Avalonia_Threading_Dispatcher_InvokeAsync_2) したところで何か処理が詰まっていそうということだ。
一応 `InvokeAsync` に cancellation token を渡せる overload があったし、ワンチャン願ってお作法的に渡すようには変えた。
でも結果は変わらず。
また `InvokeAsync` で得られた Avalonia の [`DispatcherOperation`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Threading_DispatcherOperation) を [`Abort`](https://api-docs.avaloniaui.net/docs/M_Avalonia_Threading_DispatcherOperation_Abort) を実行してもこの詰まりは解消されなかった。
log を仕込んで動きを追ってみても、 cancellation token による中断で例外が発生するでもなく、単に詰まっているようにみえる。
この見解があってるのかもよくわからん。

原因がさっぱりなので、この現象の再現のために個別の project を作って細かく見るしかないなという感じ。
イつ解決できるかもわからないし、 PSGameOfLife の暫定対応として Linux では shortcut key による終了を無効化した。
Avalonia 単体で使えば問題ないのか、 PowerShell module として Application を破棄せず持ってることに問題があるのか、色々調べないとわからん。
因みに [`ClassicDesktopStyleApplicationLifetime`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Controls_ApplicationLifetimes_ClassicDesktopStyleApplicationLifetime) を [`Shutdown`](https://api-docs.avaloniaui.net/docs/M_Avalonia_Controls_ApplicationLifetimes_ClassicDesktopStyleApplicationLifetime_Shutdown) しても消えない。どうやったら消えるんだよ。
現状把握している唯一 window を消すのが可能な方法は、 process 自体を止めること。

取り敢えず現状臭い物に蓋をしたので、世代交代の並列化をして高速化し、 PowerShell Gallery に公開したあと、腰を据えて取り組んでみるかー。
