---
title: "F# でミニゲームを書いてる Part 4"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

最近週末お出かけばかりで時間が取れないので、今日代わりに休みを取って開発、日記とした。

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発をした。

まず PSGameOfLife に GUI の prototype を実装した。 [#1](https://github.com/krymtkts/PSGameOfLife/pull/1)

[`NativeLibrary.SetDllImportResolver`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.setdllimportresolver?view=net-9.0) で native library 読み込みを調整するやつの目処がついたためだ。
`NativeLibrary.SetDllImportResolver` は native library の既定の読み込みを上書きする。
なので既定の読み込みも活かすよう自分で実装する必要がある。
[`IntPtr.Zero`](https://learn.microsoft.com/en-us/dotnet/api/system.intptr.zero?view=net-9.0) を返すと library が見つからなかったことになるので、既定の読み込みに fallback する必要がある。
結果[このような形](https://github.com/krymtkts/PSGameOfLife/blob/026246251b79e9f21cc16f54f859b412cfa4ac6c/src/PSGameOfLife/View.Avalonia.fs#L36-L73)に落ち着いた。

```fsharp
    let resolver (ptrCache: Concurrent.ConcurrentDictionary<string, nativeint>) moduleDir extension =
        let tryLoadLibrary (moduleDir: string) (extension: string) (libraryName: string) =
            let libPath =
                let libPath =
                    if extension |> libraryName.EndsWith then
                        $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}"
                    else
                        $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/{libraryName}.{extension}"

                System.IO.Path.Combine(moduleDir, libPath)

            if libPath |> IO.File.Exists then

                libPath
                |> NativeLibrary.TryLoad
                |> function
                    | true, ptr -> ptr
                    | _ -> IntPtr.Zero
            else
                IntPtr.Zero

        DllImportResolver(fun libraryName assembly searchPath ->
            match libraryName |> ptrCache.TryGetValue with
            | true, ptr -> ptr
            | _ ->
                match tryLoadLibrary moduleDir extension libraryName with
                | ptr when ptr = IntPtr.Zero ->
                    // NOTE: fallback to the default behavior if the library is not found.
                    match NativeLibrary.TryLoad(libraryName, assembly, searchPath) with
                    | true, ptr ->
                        ptrCache.TryAdd(libraryName, ptr) |> ignore
                        ptr
                    | _ ->
                        // NOTE: Returning IntPtr.Zero means the library was not found. This will cause an error when P/Invoke is called.
                        IntPtr.Zero
                | ptr ->
                    ptrCache.TryAdd(libraryName, ptr) |> ignore
                    ptr)
```

これで Windows / Linux(Ubuntu on WSL) のいずれでもご機嫌な GUI が動く。

ただ [#1](https://github.com/krymtkts/PSGameOfLife/pull/1) の実装は描画処理に問題があったみたいで、極端に遅かった。
10ms 毎の interval で描画を繰り返すと、描画が stuck した。
なるべく CUI で出来てることを GUI でもやりたくて、 Cmdlet の parameter で指定できる `Interval` の範囲はなんとか動作保証したかったので、改善に着手した。
[#2](https://github.com/krymtkts/PSGameOfLife/pull/2)

(ほぼ AI にぶん投げて)調べたところ高速な描画を実装するのに対して、 2 つ良くない点があった。

1 つ目は[ここ](https://github.com/krymtkts/PSGameOfLife/blob/e3db7c59f7fc6154a578b9393dd8a2fc136fbace/src/PSGameOfLife/View.Avalonia.fs#L109-L152)で、 [`Canvas`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Controls_Canvas) に [`Rectangle`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Controls_Shapes_Rectangle) で描画しているので描画が重かった。
cell の数が多くなるととてつもなく重くなるみたいなので game of life には向かないと。そりゃそうか。

これは単純に高速な画像の描画処理が必要なら [`WriteableBitmap`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_Imaging_WriteableBitmap) を使えばいいみたい。
手続き的な書き方で埋め尽くされたが、これでかなり速くなって、 interval 1ms くらいならさばけるようになった。

それでも interval 0ms 、要は待ち時間なしにすると stuck していた。
これが 2 つ目の良くない点で、 [ここ](https://github.com/krymtkts/PSGameOfLife/blob/e3db7c59f7fc6154a578b9393dd8a2fc136fbace/src/PSGameOfLife/View.Avalonia.fs#L154-L162)。
描画処理の完了を待たずに [`DispatcherTimer`](https://reference.avaloniaui.net/api/Avalonia.Threading/DispatcherTimer/) が次々と message を送ってくるから処理が追いつかなくなって、 queue が詰まったという感じみたい。

なので `DispatcherTimer` を使うのをやめて、描画が終わったら次の処理のための message を配信するという感じに書き換えた。再帰的になってる感じ。
この書き換えには `Cmd.OfAsyncImmediate.either` を使った。
単に Elmish を使いこなせてないだけかも知れないが、最初 `Cmd.OfAsyncImmediate.perform` してたら GUI を閉じたときに再帰的な処理が停止できてなかったみたい。
そのせいで 2 回目以降の起動で描画処理にカクつきが見られた。
この解消のために `Cmd.OfAsyncImmediate.either` で cancellation token が cancel されたときは何もしないようにしてで呼び出しを止めさせた。
これで待ち時間 0ms(実際は計算直があるためベストエフォート)の描画ができるようになった。

よく F# の game programming は最適化のため手続きっぽい書き方が増えるというのをいろんな人の blog で見ていたが、2 つの対応を通して、身にしみてわかった気がする。

まだ Avalonia や Elmish に慣れてないのもあるが、 Avalonia の v11 を使ってるから AI の suggest も v10 以前の知識で動いたりするのが面倒だった。
過去の Avalonia を知らんので結構混乱する。
また Avalonia.FuncUI.Elmish に関しては情報がそもそも少ない(多分ないよな？)から、 AI が出たらめいってくる。
結局自分で関数の signature や直に実装を見て判断するしかない。
慣れかな。

これで Windows / Linux でも高速な描画ができるようになった GUI mode の土台が整った。
あとは画面のサイズの指定とか、 CUI モードとなるべくおなじ interface に汎化するとかを進めていければよさそう。
もうちょい週末が穏やかだと嬉しいのだが。続く。
