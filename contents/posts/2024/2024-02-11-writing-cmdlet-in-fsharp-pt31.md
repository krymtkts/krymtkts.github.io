---
title: "F# でコマンドレットを書いてる pt.31"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

[前回](/posts/2024-01-21-writing-cmdlet-in-fsharp-pt30.html)触れた [PSReadLine](https://github.com/PowerShell/PSReadLine) スタイルのコンソールウィンドウ操作を実装したり、 query window の時間計算量を対数時間 O(logn) にしたり。
それらを含む [pocof version 0.9](https://www.powershellgallery.com/packages/pocof/0.9.0) をリリースした。

いまは高さ半分サイズの UI を実装して、その後の調整や coverage を改善したりしてる。
高さ半分サイズの UI は `TopDownHalf`, `BottomUpHalf` という `Layout` オプションで使えるようにした。
なかなか良いが、端末の高さが変わった場合のレンダリング崩れをどうするかが悩ましく、まだ 0.10.0 リリースはしてない。

---

端末の高さが変わった場合のレンダリング崩れは半ば諦めてる。
PSReadLine でも似たようなレンダリング崩れは起こるしな。
ただ個人的に `TopDownHalf`, `BottomUpHalf` の実装してみて顕著に見えるようになったので気になりはじめた...

`Layout` オプションが `TopDownHalf` か `BottomUpHalf` の場合、コンソールウィンドウの高さの半分に相当する空行をカーソル位置から挿入し、 pocof 用の UI を用意する。
この高さの半分というのが難しくて、初期表示から終了までにコンソールウィンドウのサイズが変更されて高さが違うこともある。
そのためいずれも現在のコンソールウィンドウの高さを基準に操作する必要がある。
例えば元々高さ 100 だった場合、コンソールウィンドウの高さを縮めて 50 にしたあと終了処理をしたら、残り 50 にはコンソールウィンドウ を縮める前に描画したコンテンツが残っている可能性がある。

例えば Windows Terminal の場合だと、概ねカーソル位置を上端にして下方向に描画されたコンテンツが取り残される。
また pane を上限分割してサイズ変更するなどで下端からカーソル位置までの高さより縮めた場合にカーソル位置が移動されることがあって、その場合はカーソル位置より前(コンソールウィンドウの外)にコンテンツが取り残されることもある。
そうなると pocof は何も手出しできなくなる。
下方向の残骸は終了処理で画面下端までを初期化すればよいのでなんとかカバーできるが、カーソル位置が強制移動された場合はもうどうにもならん。

長々と書いたが操作中にコンソールウィンドウのサイズ変更をそんなにするか？というのもあるし、いったんこのままかなあ。
閃きが訪れるまで座して待つか。

---

coverage 改善についてはアプリケーションそれ自体の品質には寄与しないが、開発側の安心感やコンプ率欲求のような気持ちみたいなもんをアゲてくれる。
なのでひとまず 90 % 超えときたいなと言うのが最近の目標だった。
pocof は branch coverage についてはそれほど高くないけど line coverage に関してはなるべく網羅したくて取り組んできた。
でも [`PSCmdlet`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.pscmdlet?view=powershellsdk-7.4.0) の実装部分は難しくて放置してきた。
今回そこに手を入れようと考えた。

[Cmdlet Class](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet?view=powershellsdk-7.4.0) 実装の場合は、 自前の [ICommandRuntime](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.icommandruntime?view=powershellsdk-7.4.0) 実装を渡せさえしたら手軽に実行できるようだった。

[Unit Testing Powershell Cmdlets in C# - Fotsies Technology Blog](https://fgimian.github.io/unit-testing-powershell-cmdlets-in-c-sharp/)

でも `PSCmdlet` はそう簡単にいかず、 `Invoke` すれば `Cmdlets derived from PSCmdlet cannot be invoked directly.` を返す。 PowerShell は一筋縄ではいかないんだ。

[Invoking Cmdlets and Scripts Within a Cmdlet - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/invoking-cmdlets-and-scripts-within-a-cmdlet?view=powershell-7.4)

> All cmdlets can invoke an existing cmdlet by calling the System.Management.Automation.Cmdlet.Invoke method from within an input processing method, such as System.Management.Automation.Cmdlet.BeginProcessing, that is overridden by the cmdlet. However, you can invoke only those cmdlets that derive directly from the System.Management.Automation.Cmdlet class. You cannot invoke a cmdlet that derives from the System.Management.Automation.PSCmdlet class.

であれば自前でテスト機構を作るしかないということで、考えてみた。

まず以下を継承・実装して mock を作る。これらを `PSCmdlet` 実装に差し込めばいい。
これらの実装は利用する箇所以外重要でないので雑でいい。コードが長いし退屈なので省略する。

- [ICommandRuntime Interface (System.Management.Automation) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.icommandruntime?view=powershellsdk-7.4.0)
- [PSHost Class (System.Management.Automation.Host) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.host.pshost?view=powershellsdk-7.4.0)
- [PSHostUserInterface Class (System.Management.Automation.Host) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.host.pshostuserinterface?view=powershellsdk-7.4.0)
- [PSHostRawUserInterface Class (System.Management.Automation.Host) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.host.pshostrawuserinterface?view=powershellsdk-7.4.0)

次に `PSCmdlet` 実装を継承してテスト用のメソッドを生やす。
Cmdlet の肝となる `BeginProcessing`, `ProcessRecord`, `EndProcessing` は public にアクセスできないので、テスト用のメソッドでまとめて呼び出すようにする。
このやり方の場合 `ICommandRuntime` 実装から `PSHost` を `PSCmdlet` 実装の [PSCmdlet.Host Property](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.pscmdlet.host?view=powershellsdk-7.4.0#system-management-automation-pscmdlet-host) に渡せない。
`PSCmdlet.Host` は get のみのプロパティなので、あとから set もできない。
なので自前でどうにかする術が必要になる。

pocof の場合は `PSCmdlet.Host` を `PSCmdlet` の実装箇所で参照しているのと、あと PowerShell の処理を呼び出している箇所もある。
それらを mock できるようにするため [abstract と default](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/members/methods) で再定義可能なメソッドに落とし込んでみた。

以下のようになる。

```fsharp
module SelectPocofCommand =
    open Xunit
    open FsUnitTyped
    open pocof
    open System.Management.Automation
    open System.Management.Automation.Host

    type SelectPocofCommandForTest() =
        inherit SelectPocofCommand()
        // SelectPocofCommand で以下を定義しておく
        // abstract member invoke: 'a list -> string seq
        // abstract member host: unit -> PSHost

        member val Host: PSHost = new Mock.MyHost()
        override __.invoke(input: 'a list) = input |> Seq.map string
        override __.host() = __.Host

        member __.InvokeForTest() =
            __.BeginProcessing()
            __.ProcessRecord()
            __.EndProcessing()

    [<Fact>]
    let ``invoke.`` () =
        let runtime = new Mock.CommandRuntime()
        let cmdlet = new SelectPocofCommandForTest()

        let a = PowerShell.Create()

        cmdlet.CommandRuntime <- runtime // ないと WriteObject 未実装のエラーになる
        cmdlet.InputObject <- [| PSObject.AsPSObject "a" |]
        cmdlet.NonInteractive <- true
        cmdlet.InvokeForTest()

        runtime.Output |> shouldEqual [ "a" ]
```

なんやややこしい感じの実装をして、なおかつ暴力的な解決方法ではあるが、これでなかなかうまくテストできる。
結構手こずったが、 `PSCmdlet` 実装の理解も深まった。
ずっと面倒で放置していた `PSHostRawUserInterface` の mock 実装もやればできなくないなとわかり、結構手応えのある学びになった。

そろそろ 0.10.0 リリースするかな。
