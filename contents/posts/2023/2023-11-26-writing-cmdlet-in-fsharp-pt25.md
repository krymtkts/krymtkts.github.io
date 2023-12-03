---
title: "F# でコマンドレットを書いてる pt.25"
tags: ["fsharp", "powershell", "docker"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

- デバッグモードがほしいなと言っていたやつ [#81](https://github.com/krymtkts/pocof/pull/81)
- あと Linux で pocof を動作確認するための container image を作るやつ [#84](https://github.com/krymtkts/pocof/pull/84)

---

デバッグモードに関しては、バグレポートをもらうときとかに `-Verbose` スイッチみたいに有効化したいシーンが想定される(pocof の場合 console に情報を出力できないからログファイル出力)。
けど今のところ開発時に確認できる程度で良くて、 compiler directive で良さそうかなと思ってそうした。

[Compiler Directives - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/compiler-directives)

これでサクッとログのファイル出力がされるようにしたのだけど、ちょっと弊害もあった。
`dotnet test` で実行されるテストってマルチプロセスかマルチスレッドで実行されるみたいで、単一のファイルへの書き込みだと `System.IO.Exception` する。
回避のために、パフォ影響考えずにロックを掛けることで複数プロセスでの書き込みのエラーを発生しなくした。
MSBuild でプロセス数を絞れるみたいなので、それ使ったらいいのかも知れんけど。 [MSBuild Command-Line Reference - MSBuild | Microsoft Learn](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-command-line-reference?view=vs-2022#switches)

あくまで手軽にデバッグビルドで有効にするだけのログなのでこういう回避もアリかなと。

あと [Caller information](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/caller-information) を使ってログを賑やかした。
初めて試したみたけど [`DefaultParameterValue`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.defaultparametervalueattribute?view=net-7.0) などは関数には使えなくてメソッドしかダメみたいね。 C# との相互運用が目的ならそらそうか。
[Parameters and Arguments - F#](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/parameters-and-arguments#optional-parameters) 見る限り関数のサンプルないしな。

先述のドキュメントだけでわからない点は fslang-design を見るのが良さそう。
今のところ IL の話まで理解する必要はないかも知れんけど、期待されるパターンが書いてあり、理解が深まる。
[fslang-design/FSharp-4.1/FS-1027-complete-optional-defaultparametervalue.md at main · fsharp/fslang-design](https://github.com/fsharp/fslang-design/blob/main/FSharp-4.1/FS-1027-complete-optional-defaultparametervalue.md)

Caller information 出してみた効果は使ってみて判断する。 ただ RFC3339 形式の日時出力も足したしそれだけは使えそう。

---

次は Linux の container image を作るやつ。
初めは以下の MS 謹製 PowerShell image に dotnet 足したらいいかなと思ってたが、結構面倒だった。

[PowerShell by Microsoft | Docker Hub](https://hub.docker.com/_/microsoft-powershell)

何が面倒って dotnet 7 の最新バージョン(7.0.404 とか)が distro の repository にあるとは限らないこと。
最新を使いたければ Microsoft の repository を引き込む必要がある。
試しに Alpine Linux, Ubuntu で見てみたけど 7.0.1xx なので pocof の `global.json` の条件満たしてねえ。

自力で追加するなら Linux でのインストール方法 ([.NET and Ubuntu overview - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#register-the-microsoft-package-repository))を Dockerfile に焼き直すだけ。
なのだけど、めんどしこんなん世界で誰かが既にやってるやろと思って container image を探した。

結局 dotnet の Linux container image が dotnet 完備で PowerShell も新しくて楽なので採用した。これサイコー。
[.NET SDK by Microsoft | Docker Hub](https://hub.docker.com/_/microsoft-dotnet-sdk/)

ただサイズがデカイ... 1.5GB もあった。

ひとまず Linux の PowerShell 環境を手に入れたので、 pocof を対話モードで動かしてみたところ、以下のエラーを得た。

```plaintext
PS /src> Get-ChildItem | Select-Pocof
Select-Pocof: The method or operation is not implemented.
```

前から unit test と [Pester](https://github.com/pester/Pester) による非対話モードのテストは Windows, Linux, Mac いずれも成功してる。
cross platform の対話モード依存のエラーってことかな。
対応できてないやろなと想像はしてたけど、やっぱ動いてなかったので Issue を作成。[#85](https://github.com/krymtkts/pocof/issues/85)

エラーの詳細を見たら `Microsoft.PowerShell.ConsoleHostRawUserInterface.GetBufferContents` がダメみたい。まじか。

```plaintext
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

似てそうな Issue みつけたけど transcript してないしな。

[Unix system executables terminate with error if transcription enabled · Issue #1920 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/1920)

今ん所は `NotImplementedException` を検知した場合の workaround として pocof 開く前のバッファの復元をしないようにして、最小限の Linux 対応とした。 [#86](https://github.com/krymtkts/pocof/pull/86)

Linux でもバッファの復元したいし、もうちょっと調査する。
[PowerShell/src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleHostRawUserInterface.cs のこの箇所](https://github.com/PowerShell/PowerShell/blob/811efa46df822bf7be6179b6219f9f9d160eb7d5/src/Microsoft.PowerShell.ConsoleHost/host/msh/ConsoleHostRawUserInterface.cs#L1560)見るに単純に実装されてなかったりして...

---

この container iamge 試行錯誤の影響で、自機の 256 GB しかない記憶領域の残が 1 GB を切ってしまった。やば。
直ちに container image の掃除と VHD を圧縮することにした。
Windows 11 Home だと [`Optimize-VHD`](https://learn.microsoft.com/en-us/powershell/module/hyper-v/optimize-vhd?view=windowsserver2022-ps) が使えないので、以下のように `diskpart` というコマンドを使うらしい。

[WSL 2 should automatically release disk space back to the host OS · Issue #4699 · microsoft/WSL](https://github.com/microsoft/WSL/issues/4699#issuecomment-627133168)

ここで `attach`, `detach` なくてもできるみたい。動的に拡張できる VHD なので readonly いらんのかな。

[How to Shrink a WSL2 Virtual Disk – Stephen Rees-Carter](https://stephenreescarter.net/how-to-shrink-a-wsl2-virtual-disk/)

[compact vdisk | Microsoft Learn](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/compact-vdisk)

> Remarks
>
> - A dynamically expanding VHD must be selected for this operation to succeed. Use the select vdisk command to select a VHD and shift the focus to it.
> - You can only use compact dynamically expanding VHDs that are detached or attached as read-only.

単純な手順を示すとこんな感じ。

```powershell
docker system prune --all --force # 掃除しきる
wsl --shutdown
Resolve-Path  ~\AppData\Local\Docker\wsl\data\ext4.vhdx | Set-Clipboard
diskpart # 以降 diskpart との対話
# select vdisk file="paste here"
# compact vdisk
# exit
```

このように diskpart の窓の中でコマンド打たないといけないのは非常にだるい。ここは例に倣ってスクリプト化しておきたい。

[diskpart scripts and examples | Microsoft Learn](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/diskpart-scripts-and-examples)

以下に試作した手順を記す。

```powershell
# Docker Desktop も dockerd も立ち上がってない前提
Start-Process "C:\Program Files\Docker\Docker\resources\dockerd.exe" -WindowStyle Hidden
# dockerd がいれば docker cli は動かせる
docker system prune --all --force
Get-Process "*dockerd*" | % {
    $_.Kill();
    $_.WaitForExit()
}
wsl --shutdown
$vdisk = Resolve-Path  ~\AppData\Local\Docker\wsl\data\ext4.vhdx
$tmp = "${env:Temp}/diskpart.txt"
@"
select vdisk file="$vdisk"
compact vdisk
"@ | Set-Content -Path $tmp
diskpart /s $tmp > ./log.txt
cat ./log.txt | Write-Host
Remove-Item $tmp,./log.txt
```

少なくとも `docker ~` ってコマンドを打つだけなら `dockerd.exe` が立ち上がってさえいれば OK 。
なので VHD の圧縮だけならこんな感じでいけそうなのだけど、 Docker Desktop の起動・終了の制御だけ全くわからんくて調査中。
同時に Docker Desktop が立ち上がってるとどのみちこいつを graceful に殺さねば `wsl --shutdown` で Docker Desktop がエラーダイアログ出してくる。
`com.docker.backend.exe` が全てを司ってるのはわかったけど、こいつをどのように御せばいいのかわからん。

---

次、積み残し Issue やりたいなと思いつつも、 .NET 8 来たし更新するのが先かな。
F# 8 に新しく来た `_.Property` shorthand と nested record field copy and update は pocof でもすぐ取り入れたいしな。
[Announcing F# 8 - .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-fsharp-8/)
