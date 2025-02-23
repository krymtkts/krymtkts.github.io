---
title: "F# でコマンドレットを書いてる pt.30"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

[前回](/posts/2024-01-07-writing-cmdlet-in-fsharp-pt29.html)触れた `PSHostRawUserInterface.LengthInBufferCells` を使った full-width character の表示に対応した。
なので、[pocof version 0.8](https://www.powershellgallery.com/packages/pocof/0.8.0) をリリースした。

`PSHostRawUserInterface.LengthInBufferCells` で query window の表示域に収まる長さを求めるのに再帰させた。
一発で長さを求められないあたり効率は良くない。
時間計算量が線形時間 O(n) なので、今後せめて二分探索で対数時間 O(logn) にできないか考える。

---

pocof の積み Issues は今 4 つある。

- [#85 Linux support](https://github.com/krymtkts/pocof/issues/85)
- [#44 Support query string selection](https://github.com/krymtkts/pocof/issues/44)
- [#17 Selection support](https://github.com/krymtkts/pocof/issues/17)
- [#16 Page up/down support](https://github.com/krymtkts/pocof/issues/16)
  - こいつはいらんかなと思いだした。 paging が必要なほど件数多いなら query 足して絞るし、なんか余計かなーと

今は [#85 Linux support](https://github.com/krymtkts/pocof/issues/85) を進めてる。

pocof は [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) をターゲットにしてるので、 Windows PowerShell でも PowerShell (Core 6 以降) でも動くはず。現在の dotnet はほんとすごいな。
でも PowerShell の `ConsoleHost` の実装で Windows 以外では `NotImplementedException` を投げる箇所がある。
以下に、 Ubuntu on Docker で確認したエラーを Issue から転記する。

```powershell
PS /src> Get-ChildItem | Select-Pocof
Select-Pocof: The method or operation is not implemented.

PS /src> $ErrorView = 'DetailedView'
PS /src> $Error

Exception             :
    Type       : System.NotImplementedException
    TargetSite :
        Name          : GetBufferContents
        DeclaringType : Microsoft.PowerShell.ConsoleHostRawUserInterface, Microsoft.PowerShell.ConsoleHost, Version=7.3.10.500, Culture=neutral, PublicKeyToken=31bf3856ad364e35
        MemberType    : Method
        Module        : Microsoft.PowerShell.ConsoleHost.dll
    Message    : The method or operation is not implemented.
    Source     : Microsoft.PowerShell.ConsoleHost
    HResult    : -2147467263
    StackTrace :
   at Microsoft.PowerShell.ConsoleHostRawUserInterface.GetBufferContents(Rectangle rectangle)
   at System.Management.Automation.Internal.Host.InternalHostRawUserInterface.GetBufferContents(Rectangle r)
   at pocof.PocofScreen.RawUI..ctor(PSHostRawUserInterface rui) in /src/src/pocof/UI.fs:line 55
   at pocof.PocofScreen.init(PSHostRawUserInterface rui, String prompt, FSharpFunc`2 invoke) in /src/src/pocof/UI.fs:line 173
   at pocof.SelectPocofCommand.interact(InternalConfig conf, InternalState state, Position pos, PSHostRawUserInterface rui, FSharpFunc`2 invoke) in /src/src/pocof/Library.fs:line 44
   at pocof.SelectPocofCommand.EndProcessing() in /src/src/pocof/Library.fs:line 191
CategoryInfo          : NotImplemented: (:) [Select-Pocof], NotImplementedException
FullyQualifiedErrorId : NotImplementedException,pocof.SelectPocofCommand
InvocationInfo        :
    MyCommand        : Select-Pocof
    ScriptLineNumber : 1
    OffsetInLine     : 17
    HistoryId        : 2
    Line             : Get-ChildItem | pocof
    PositionMessage  : At line:1 char:17
                       + Get-ChildItem | pocof
                       +                 ~~~~~
    InvocationName   : pocof
    CommandOrigin    : Internal
ScriptStackTrace      : at <ScriptBlock>, <No file>: line 1
PipelineIterationInfo :
```

`NotImplementedException` の発生源はここかなと思ってる。
[PowerShell/src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleHostRawUserInterface.cs](https://github.com/PowerShell/PowerShell/blob/f0076b9d883aa0ed07fb2a5c2be5f38f5d945e1e/src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleHostRawUserInterface.cs#L1553-L1563)

```csharp
        /// <summary>
        /// This API returns a rectangular region of the screen buffer. In
        /// this example this functionality is not needed so the method throws
        /// a NotImplementException exception.
        /// </summary>
        /// <param name="rectangle">Defines the size of the rectangle.</param>
        /// <returns>Throws a NotImplementedException exception.</returns>
        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }
```

`ConsoleHostRawUserInterface.cs` の先頭に `#if !UNIX` てプリプロセッサディレクティブがあって、前半 Windows 後半その他になってる。これはその他の方。

そのため、 pocof では Linux の場合(厳密には Mac も含むが確認できない)に work around が必要だった。
今は `NotImplementedException` を捕捉した場合は `GetBufferContents` 未対応の platform とみなし、エラーを回避する。
この場合、コンソールに表示されているバッファのバックアップと復元はしない。

```fsharp
    type RawUI(rui) =
        let rui: PSHostRawUserInterface = rui

        // TODO: replace backup/restore buffer contents with scrolling contents for Linux support.
        let buf: BufferCell [,] option =
            try
                rui.GetBufferContents(Rectangle(0, 0, rui.WindowSize.Width, rui.CursorPosition.Y))
                |> Some
            with // NOTE: when running on Linux, this exception is thrown.
            | :? NotImplementedException -> None
```

元々 Windows で動いてるバッファのバックアップと復元も、可視範囲しか対応しないのと復元結果が微妙に壊れるので、イマイチではあるけど。

この課題を見つけて以降、 dotnet で cross-platform なコンソール操作どうなってんのやと調査を進めていた。
PowerShell で Windows 以外が未実装な機能もあれば、 dotnet 自体で Windows 以外の platform をサポートしないケースもある。
[Support for Console.MoveBufferArea() on Linux & Mac · Issue #23073 · dotnet/runtime](https://github.com/dotnet/runtime/issues/23073)

コンソール周りの cross-platform は結構難しくて、 Windows でできることをそのまま他所ではできないのがわかった。

---

じゃあ pocof では描画域を確保したりバッファを復元したりの操作をどう変えていくべきか、悩ましいところだった。
どう解消すべきかなと調査していたところ、先達の重要なコメントを見つけた。

[PS-GuiCompletion does not work in PowerShell Core 7.0 on Linux · Issue #13 · cspotcode/PS-GuiCompletion](https://github.com/cspotcode/PS-GuiCompletion/issues/13#issuecomment-620084134)

> Similar functionality, such as the Menucomplete feature of PSReadLine, work around this limitation by writing several blank lines to the terminal, scolling everything upward and creating an empty region at the bottom of the screen. They draw their "gui" into this empty region, knowing they can erase it entirely when they're done.

[PSReadLine](https://github.com/PowerShell/PSReadLine) の補完機能で表示されるアレをパクれば良いと。なるほどーって感じ。
でも実装が大変なんでしょ？と思いコードを見に行ったところ、めちゃくちゃシンプルな仕組みだった。

[PSReadLine/PSReadLine/DisplayBlockBase.cs at e57f7d691d8df8c1121fddf47084f96aea74a688 · PowerShell/PSReadLine](https://github.com/PowerShell/PSReadLine/blob/e57f7d691d8df8c1121fddf47084f96aea74a688/PSReadLine/DisplayBlockBase.cs#L17-L24)

```csharp
            protected void MoveCursorDown(int cnt)
            {
                IConsole console = Singleton._console;
                while (cnt-- > 0)
                {
                    console.Write("\n");
                }
            }
```

改行を印字したらカーソルが 1 行下に進むので、それを必要な高さ分行うだけで描画域を確保できると。
この方法であれば、コンソールのバッファを破壊することもない。

一点マウススクロールを制限できないのがどうかなーと、はじめは思った。
が、それは今までの pocof が「全画面モーダル」的なアプリだったからそう感じたわけで、これからは「全画面ウィンドウ」的なアプリと捉えればいいだけなのではと思えてきた。
従来であれば可視範囲だけバッファをバックアップするのでスクロールを制限できる代わりに、見えない範囲のバッファを吹っ飛ばす制限があったし。
これが全画面ウィンドウであれば、全画面をやめてバッファと共存するような下半分だけの UI もアリやなというアイデアにつなげることもできる。これは良さそうや。
何ならタイリングレイアウト派なので、自分の用途だと半分 UI の方が向いてそう。

ということで、 PSReadLine スタイルの描画をプロトタイプしてみた。
まず [PowerShell で雑に書いてみて](https://github.com/krymtkts/pocof/issues/85#issuecomment-1890919078)、次に [F# で雑に書いてみた](https://github.com/krymtkts/pocof/issues/85#issuecomment-1901918776)。これらは [Gist](https://gist.github.com/krymtkts/52de8e9d4b864db7919d892795486978) に置いた。

以下は F# 版。

```fsharp
open System

[<TailCall>]
let rec readAndDisplay h w arr =
    let k = Console.ReadKey(true)
    let arr = k :: arr

    if k.Key = ConsoleKey.Enter then
        ()
    else
        let take =
            match List.length arr < h with
            | true -> List.length arr
            | _ -> h

        arr
        |> List.take take
        |> List.map (fun k -> $"[{k.Key} - {k.Modifiers}]")
        |> List.iteri (fun i s ->
            Console.SetCursorPosition(0, i)
            let s = $"{i} {s}"

            Console.Write(s + String.replicate (w - String.length s) " "))

        readAndDisplay h w arr

// test script for interactive console window.
// currently cannot prevent mouse scrolling.
let testConsoleWindowWithoutBufferCleaning () =
    // backup cursor x position.
    let x = Console.CursorLeft

    // add lines to the end of the screen for scrolling using the PSReadLine method.
    let h = Console.WindowHeight
    let w = Console.WindowWidth

    String.replicate (h - 1) "\n" |> Console.Write

    // write contents.
    let yy = Console.CursorTop

    Console.SetCursorPosition(0, 0)

    [ 0..yy ]
    |> List.map (fun i ->
        let s = $"Line: %d{i}"
        s + String.replicate (w - s.Length) " ")
    |> String.concat "\n"
    |> Console.Write
    // read and display.
    readAndDisplay h w []

    // clear contests.
    [ 0 .. (h - 1) ]
    |> List.iter (fun i ->
        Console.SetCursorPosition(0, i)
        Console.Write(String.replicate w " "))

    // restore cursor position.
    Console.SetCursorPosition(x, 0)

testConsoleWindowWithoutBufferCleaning ()
```

こんな感じに動く。

![F# での PoC キャプチャ](/img/2024-01-21-poc\poc-fsharp.gif "F# での PoC キャプチャ")

Windows の他に Ubuntu on WSL でも動かしてみた(disk カツカツで Docker やめた)が、良さそう。
前方スクロールしてたとしても、入力をはじめたらカーソル位置の操作が行われて全画面位置に戻る。いい。
これを pocof に組み込んでみて、まずは使ってみる。
違和感なくいけそうなら 0.9 で出すかな。
