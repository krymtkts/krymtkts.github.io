---
title: "F# でミニゲームを書いてる Part 3"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発。
次は GUI でミニゲームを実行するようにしたいというやつだ。

[krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox) で [Avalonia.FuncUI](https://github.com/fsprojects/Avalonia.FuncUI) の練習をしてみた。
Avalonia.FuncUI 自体を利用するのは難なくできそうだが、 PowerShell Cmdlet 内から呼び出すのが極めて難しいなこれ。

`dotnet run ~` のように実行形式で起動すれば Windows でも Linux(Ubuntu on WSL で確認した)でも問題ない。
しかし PowerShell Cmdlet 内から起動すると DLL 等 native library が読み込まれない。

Windows 側は以下の hack を使うことで、事前に load した assembly がキャッシュされてる状態で GUI を起動できた。
`runtimes/*/native/*` を動的に load することで課題に対処した具合だ。

[fsharp-cmdlet-sandbox/src/avalonia-funcui/Library.fs at dbc637c82935da48d14c0affc69d869a5e43751f · krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox/blob/dbc637c82935da48d14c0affc69d869a5e43751f/src/avalonia-funcui/Library.fs#L99-L149)

検証コードなので汚いが、まんまコピる。

```fsharp
        printfn
            $"OSArchitecture: {RuntimeInformation.OSArchitecture} OSDescription: {RuntimeInformation.OSDescription} FrameworkDescription: {RuntimeInformation.FrameworkDescription} ProcessArchitecture: {RuntimeInformation.ProcessArchitecture} RuntimeIdentifier: {RuntimeInformation.RuntimeIdentifier}"

        printfn "EndProcessing called"

        let moduleDir =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)

        printfn "Module directory: %s" moduleDir

        if RuntimeInformation.IsOSPlatform(OSPlatform.Windows) then
            [ $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/av_libglesv2.dll"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libHarfBuzzSharp.dll"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libSkiaSharp.dll" ]
        elif RuntimeInformation.IsOSPlatform(OSPlatform.OSX) then
            [ $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libAvaloniaNative.dylib"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libHarfBuzzSharp.dylib"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libSkiaSharp.dylib" ]
        elif RuntimeInformation.IsOSPlatform(OSPlatform.Linux) then
            [ $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libHarfBuzzSharp.so"
              $"runtimes/{RuntimeInformation.RuntimeIdentifier}/native/libSkiaSharp.so" ]
        else
            List.empty
        |> List.iter (fun skiaDll ->
            let skiaPath = System.IO.Path.Combine(moduleDir, skiaDll)

            try
                printfn "Loading SkiaSharp library from: %s" skiaPath

                if System.IO.File.Exists(skiaPath) then
                    printfn "SkiaSharp library found."
                    NativeLibrary.Load(skiaPath) |> ignore
            with e ->
                printfn "Failed to load SkiaSharp library: %s" e.Message
                ())

        let app =
            let lt =
                new Avalonia.Controls.ApplicationLifetimes.ClassicDesktopStyleApplicationLifetime()

            AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseSkia()
                .LogToTextWriter(Console.Out, LogEventLevel.Verbose)
                .SetupWithLifetime(lt)
        // .SetupWithoutStarting()

        printfn "Avalonia FuncUI application configured."

        app
```

でも Linux では効果なかったから同じ方法を使えなかった。 library 解決の手法が Windows と違うそうなので、これは別途対応する必要がある。
また Mac での挙動は端末がなくてチェックできない。
cross platform 対応のハードルは実に高いと実感している。

まるで詳しくないが、 [`NativeLibrary.SetDllImportResolver`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.nativelibrary.setdllimportresolver?view=net-9.0) で native library の読み込み path を調整できるようなので、それを使うのが良さそう。
Linux での native library の読み込み問題さえ解決したら PSGameOfLife に組み込めるなーと思ってる。

別の課題もある。
PowerShell Cmdlet 内から Avalonia の普通の使い方をすると初回起動しか成功しない。
2 回目以降は `Setup was already called on one of AppBuilder instances` になる。
どうも同一 process 内で Avalonia の [`AppBuilder`](https://reference.avaloniaui.net/api/Avalonia/AppBuilder/) の setup は一度限りというのが仕様みたい。
先述の [`SetupWithLifetime`](https://reference.avaloniaui.net/api/Avalonia.Controls/AppBuilderBase_1/F3B58741) もその対象。
また [`Start`](https://reference.avaloniaui.net/api/Avalonia.Controls/AppBuilderBase_1/07E2926E) 等も内部的に setup を行うので、 2 回目以降同様のエラーが発生する。
Avalonia の event loop は [`Avalonia.Threading.Dispatcher`](https://reference.avaloniaui.net/api/Avalonia.Threading/Dispatcher/) が担っているようだが、こいつも process で一度限りしか利用できないみたい。
2 回目以降の実行では `Dispatcher` が破棄されて使えなかった。
PowerShell Cmdlet はその session 内であればずっと同じ process なので、大変だこれ。

たどり着いた回避方法が、 process 内での `AppBuilder` の setup を 1 度限りにし、 [`Window`](https://reference.avaloniaui.net/api/Avalonia.Controls/Window/) を cmdlet 実行毎に生成することだ。
setup については先述の通りだが、 `Window` も同様で一度 `Close` すると再利用できなくなる。
だから `Window` を毎回使い捨てる必要がある。
いずれも GUI は概ね単一の process で実行されるから、 process 内で使い回せるようにしてないのが仕様なのかな。
何にせよ、これで PowerShell の同一 session 内で何度も GUI を起動できるようになった。

[fsharp-cmdlet-sandbox/src/avalonia-funcui/Library.fs at dbc637c82935da48d14c0affc69d869a5e43751f · krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox/blob/dbc637c82935da48d14c0affc69d869a5e43751f/src/avalonia-funcui/Library.fs#L53-L178)

既に引用した一部を省略してまま検証コードをコピったのが以下。

```fsharp
type MainWindow() as this =
    inherit HostWindow()

    do
        base.Title <- "Example"
        base.Height <- 300.0
        base.Width <- 300.0

        Elmish.Program.mkProgram Main.init Main.update Main.view
        |> Program.withHost this
        |> Program.run

    override this.OnClosed(e: System.EventArgs) : unit = base.OnClosed(e: System.EventArgs)


type App() =
    inherit Application()

    member val mainWindow: MainWindow | null = null with get, set
    member val desktopLifetime: IClassicDesktopStyleApplicationLifetime | null = null with get, set

    override __.Initialize() =
        __.Styles.Add(FluentTheme())
        __.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        printfn "Application initialized with FluentTheme and Dark variant."

    override __.OnFrameworkInitializationCompleted() =
        match __.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as (desktopLifetime: IClassicDesktopStyleApplicationLifetime) ->
            __.desktopLifetime <- desktopLifetime
            // __.mainWindow <- new MainWindow()
            // desktopLifetime.MainWindow <- __.mainWindow
            // desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose
            printfn "MainWindow set as the main window."
        | _ -> ()

open System
open System.Diagnostics
open Avalonia.Logging

[<Cmdlet(VerbsDiagnostic.Test, "AvaloniaFuncUI")>]
[<OutputType(typeof<PSObject>)>]
type SelectPocofCommand() =
    inherit PSCmdlet()

    static let app =
        // 先述した通り。略

        app

    override __.BeginProcessing() = printfn "BeginProcessing called."

    override __.ProcessRecord() = printfn "Hello from AvaloniaFuncUI"

    override __.EndProcessing() =

        printfn "Starting Avalonia FuncUI application..."

        let app = (app.Instance :?> App)
        app.mainWindow <- new MainWindow()
        app.mainWindow.WindowStartupLocation <- WindowStartupLocation.CenterScreen
        app.desktopLifetime.MainWindow <- app.mainWindow
        app.desktopLifetime.ShutdownMode <- ShutdownMode.OnMainWindowClose

        let cts = new Threading.CancellationTokenSource()

        app.mainWindow.Closed.Add(fun _ ->
            printfn "MainWindow closed, shutting down application."
            cts.Cancel())

        app.mainWindow.Show()
        let ret = app.Run(cts.Token)
        printfn $"Avalonia FuncUI application started successfully. {ret}"

        app.mainWindow.Close()
        cts.Cancel()

        Console.WriteLine("\n\n\n\n\n\n\n\n\n\n")
```

この 2 つの課題は、 Avalonia の基礎を抑えてたら困らないのかも知れないが、文書量も多いし example から体当たりで対処してしまった。
でも issue や discussion でも似たような話題があったから、わかりにくいところなんじゃないかなという気がせんでもない。
もっと良い方法があれば知りたい。
またこの調査をするに当たり、前例として他に PowerShell の中から GUI を起動する狂った事例ないか探してみたら、安定の Ironman Software さんが随分前にやってた。
他にもそこから派生した project もあった。
実現方法は違うが、 PowerShell でもできるということがわかり支えになった。

- [ironmansoftware/psavalonia: Avalonia bindings for PowerShell](https://github.com/ironmansoftware/psavalonia)
- [pinuke/PWSH-AGUI: Asynchronous PowerShell GUI tool](https://github.com/pinuke/PWSH-AGUI)

今回わたしがやろうとしてる F# で PowerShell Cmdlet を書いて Avalonia で GUI を表示するってのも、彼らに続く例がない狂った事例のはずだ。
うまく実現できたらこれは胸を張って良いはだろう。誰も興味ないやろうけど。

まだ krymtkts/PSGameOfLife に反映してないし、 GUI 版の実装まで随分と時間がかかりそうだ。
