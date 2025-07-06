---
title: "F# でミニゲームを書いてる Part 6"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発をした。なかなか大変だったが、終了処理の課題を解決できた。
[#6](https://github.com/krymtkts/PSGameOfLife/pull/6)

元々課題に思ってた終了処理が詰まるのも解決の手立てがなかった。
UI Thread が詰まってるから Window を閉じる操作をしても event に関連づいた処理が起動するのにどうしても時間がかかってしまうという感じか。
Avalonia 内部まで追ったわけではないがそういう挙動に見えた。

あとどうも [`WriteableBitmap`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_Imaging_WriteableBitmap) の生成コストが結構高いようだというのがわかってきた。
`WriteableBitmap` を使い回す実装に変えたら 1 frame 当たりの処理時間が 1ms くらいは縮む。
でも  [Avalonia.FuncUI](https://github.com/fsprojects/Avalonia.FuncUI) か [Avalonia.FuncUI.Elmish](https://github.com/fsprojects/Avalonia.FuncUI/tree/master/src/Avalonia.FuncUI.Elmish) の制約で、参照が同じままだと変更を検知できなくて再描画できないみたい。
あんま追えてないが [`Object.ReferenceEquals`](https://learn.microsoft.com/en-us/dotnet/api/system.object.referenceequals?view=net-9.0) を使ってそう。
Avalonia.FuncUI.Elmish での実現は断念した。

この通り Avalonia.FuncUI と Avalonia.FuncUI.Elmish の制約が乗り越えられない壁になってたので、いっそのこと利用をやめてみた。
代わりに描画処理を自前の event loop で実行する必要がある。末尾再帰で loop するようにした。
event loop 内で [`CancellationTokenSource`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource?view=net-9.0) に基づいて終了できるようにしたら、瞬時に終了処理が実行できるよう改善できた。
きびきび反応していい感じ。
Avalonia.FuncUI の宣言的な DSL とか [MVU](https://elmish.github.io/elmish/#dispatch-loop) の恩恵が受けられなくなったが、最低限の UX を提供するためには仕方ないのだ。
Avalonia.FuncUI と Avalonia.FuncUI.Elmish もパフォや機能的な制約がないのであれば問題ないし、共存の未知を探せるとよいが。

今回の変更による [UI 更新と event loop のコード](https://github.com/krymtkts/PSGameOfLife/blob/4a6da2bc415541cf9a45f47014b710ac587adf2c/src/PSGameOfLife/View.Avalonia.fs#L213-L273)は以下の通り。
宣言的 UI じゃなくなったので見通しは悪くなった。
event loop は一般的な再帰になってわかりやすいといえるが処理が増えたら煩雑化しそうではある。

```fsharp
    let stack, updateUI =
        let status1 =
            TextBlock(Background = Brushes.White, Foreground = Brushes.Black, Height = Main.statusRowHeight)

        let status2 =
            TextBlock(Background = Brushes.White, Foreground = Brushes.Black, Height = Main.statusRowHeight)

        let image = Image(Width = float width, Height = float height)

        let wb =
            new WriteableBitmap(PixelSize(width, height), Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque)

        image.Source <- wb

#if DEBUG || SHOW_FPS
        let fpsText =
            let tb =
                TextBlock(Foreground = Brushes.Yellow, Background = SolidColorBrush(Color.Parse("#80000000")))

            Canvas.SetTop(tb, 0.0)
            Canvas.SetRight(tb, 0.0)
            tb.SetValue(Canvas.ZIndexProperty, 100) |> ignore
            tb
#else
        let fpsText = null
#endif
        let canvas = Canvas(Width = float width, Height = float height)
        let stack = StackPanel()

        status1 |> stack.Children.Add
        status2 |> stack.Children.Add
        image |> canvas.Children.Add
        canvas |> stack.Children.Add
#if DEBUG || SHOW_FPS
        fpsText |> canvas.Children.Add
#endif

        let updateUI board =
            status1.Text <- $"#Press Q to quit. Board: {board.Column} x {board.Row}"
            status2.Text <- $"#Generation: {board.Generation, 10} Living: {board.Lives, 10}"
            renderBoard board wb
            image.InvalidateVisual()
#if DEBUG || SHOW_FPS
            fpsText.Text <- $"FPS: %.2f{FpsCounter.get ()}"
#endif
        stack, updateUI

    [<TailCall>]
    let rec loop board =
        async {
            if cts.IsCancellationRequested then
                return ()

            do!
                Dispatcher.UIThread.InvokeAsync(fun () -> updateUI board).GetTask()
                |> Async.AwaitTask

            do! Async.Sleep(int board.Interval)
            let currentBoard = nextGeneration board
            return! loop currentBoard
        }
```

これによって終了処理は改善されたが、同時に不可解な hangup が発生するようになった。 CPU ・ GPU が無風になる。
それも毎回 hangup するのでなくて、特にゲームボードのサイズが大きくて cell の数が多いほど起動時に詰まる確率が高い。
でもデフォのサイズでも確率が多少低いだけで発生することに変わりない。

原因不明で困ったが、なんとか原因らしき挙動は突き止めた。
どうも `WriteableBitmap` が thread safe じゃないことによるみたい。
[`Parallel.ForEach`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.foreach?view=net-9.0) を使って並列で重ならない address に書き込んでいても、内部で deadlock するっぽい。
早い話が `WriteableBitmap` への並列書き込みが原因ということになる。
つまり一時領域に並列で書きこんで、その結果を `WriteableBitmap` に一括コピーすれば回避可能なのがわかった。コピー効率は悪くなるが。
[コード](https://github.com/krymtkts/PSGameOfLife/blob/4a6da2bc415541cf9a45f47014b710ac587adf2c/src/PSGameOfLife/View.Avalonia.fs#L167-L211)は以下のようになった。

```fsharp
    let renderBoard (board: Board) (wb: WriteableBitmap) =
        let partitioner = Partitioner.Create(0, Array2D.length1 board.Cells)
        use tempPtr = fixed &tempBuffer.[0]
        let lenX = Array2D.length2 board.Cells - 1

        do
            Parallel.ForEach(
                partitioner,
                fun (startIdx, endIdx) ->
                    for y = startIdx to endIdx - 1 do
                        let yc = y * cellSize

                        for x = 0 to lenX do
                            let xc = x * cellSize

                            let vectors, bytes =
                                match board.Cells.[y, x] with
                                | Live -> templates.LiveVectors, templates.LiveBytes
                                | Dead -> templates.DeadVectors, templates.DeadBytes

                            for dy = 0 to cellSize - 1 do
                                let dstOffset = ((yc + dy) * width + xc) * 4

                                let dstLinePtr =
                                    NativePtr.add
                                        (NativePtr.ofNativeInt<byte> (NativePtr.toNativeInt tempPtr))
                                        dstOffset

                                Main.writeTemplateSIMD dstLinePtr vectors bytes
            )
            |> ignore

        // NOTE: Parallel write to WriteableBitmap cause a deadlock. so avoid it by using a temporary buffer.
        use fb = wb.Lock()
        System.Runtime.InteropServices.Marshal.Copy(tempBuffer, 0, fb.Address, bufferSize)
```

これで Razer Blade Stealth 2017 でも 50x50 で 700 FPS くらい、 100x80 だと 140 ~ 160 FPS くらい。
100x80 があまり高速化してないのだけど、 全画面に近い 160x80 でも 120 FPS は出るようになって割と良いのではないかと。

ひとまず終了処理の課題と高速化も実現できて満足。

---

あと気になるのは、 [SIMD 命令](https://learn.microsoft.com/en-us/dotnet/standard/simd)の効率。
現状 SIMD 命令は 1 cell 毎に SIMD 命令でのコピーと端数のコピーが実行されてるのだけど、理論的には SIMD 命令の効率をもっと上げられるはず。
でも実際に試してみたら、 SIMD 命令のために `Vector<byte>[]` を積み上げる箇所にどうにもコストがかかるみたいで大して速くならなかった。
なので現状だと先述の通り理論上の SIMD 命令の効率は悪いが、現状の定義済みメモリをこまめに SIMD と端数に分けてコピる方式が最も速くなってる。

でもこれは動作確認する際の cell 数だとそうなるというだけだと思われる。
cell が 10x10 の固定サイズじゃなくて指定できるようになってたらもっと SIMD 命令の効率を上げないと FPS 上がらないんじゃないかなと。
またちまちま試行錯誤して改善を繰り返したい。

描画効率が更に良くなったら全画面表示なんかも導入していい気がする。
GUI はやることが多くて勉強になるが、お陰で一向にゲームモードの追加とかに進まんのが難点やな。
