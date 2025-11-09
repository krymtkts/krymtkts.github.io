---
title: "F# で Cmdlet を書いてる pt.80"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発を少し進めた。
必要最低限の [Multi targeting](https://learn.microsoft.com/en-us/visualstudio/msbuild/net-sdk-multitargeting) を実現する変更をした。
[#376](https://github.com/krymtkts/pocof/pull/376)

PowerShell module で Multi targeting で build した dll を配信するなら、 .NET の Multi targeting の情報だけでは準備が足りない。
platform に適切な dll を読み込めるように PowerShell の module 構成についても知る必要がある。
[前回](/posts/2025-11-02-writing-cmdlet-in-fsharp-pt79.html)、 PowerShell の loader script について書いてる文書がないと書いたが、見つけた。
ちゃんと準備されてたみたいで見つけられなかったのが恥ずかしい。
ここに [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) の例が載ってる。これが公式版といえそう。

[Modules with compatible PowerShell Editions - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/gallery/concepts/module-psedition-support?view=powershellget-3.x#targeting-multiple-editions)

PSScriptAnalyzer の repository のものはこちら。

[PSScriptAnalyzer/Engine/PSScriptAnalyzer.psm1 at main · PowerShell/PSScriptAnalyzer · GitHub](https://github.com/PowerShell/PSScriptAnalyzer/blob/main/Engine/PSScriptAnalyzer.psm1)

[`$PSEdition`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_powershell_editions?view=powershell-7.5#the-psedition-automatic-variable) を利用する方法と loader script で細かく制御する方法の 2 つがあると書いてある。
ただ loader script という熟語は出てこないので、これは俗語みたいなもんか。

`$PSEdition` を利用する方法は PowerShell 5.1 以上で利用できるらしいので、 Windows PowerShell 5.1 と PowerShell すべてで使えることになる[^1]。
因みに使える変数はいくつかあるが、使えないものを参照すると error になる。

```plaintext
PS C:\Users\takatoshi\dev\github.com\krymtkts\pocof> import-Module .\publish\pocof\pocof.psd1
Import-Module: The module manifest 'C:\Users\takatoshi\dev\github.com\krymtkts\pocof\publish\pocof\pocof.psd1' could not be processed because it is not a valid PowerShell module manifest file. Remove the elements that are not permitted: At C:\Users\takatoshi\dev\github.com\krymtkts\pocof\publish\pocof\pocof.psd1:12 char:22
+     RootModule = if ($PSVersionTable) {
+                      ~~~~~~~~~~~~~~~
A variable that cannot be referenced in restricted language mode or a Data section is being referenced. Variables that can be referenced include the following: $PSCulture, $PSUICulture, $true, $false, $null, $PSScriptRoot, $PSEdition, $EnabledExperimentalFeatures.
```

`$PSEdition` を使った分岐は記述が簡素になるが、今回は loader script を採用した。
これはなるべく広く PowerShell を最適化するために下限を .NET 6 としたため、 loader script でないと書けないからだ。
.NET 6 の PowerShell 7.2 はもう EOL してるので要らないといえばそうなので、そのうち .NET 8 を下限に変える可能性はある。

[^1]: [PowerShell Support Lifecycle - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/install/powershell-support-lifecycle?view=powershell-7.5#powershell-end-of-support-dates) の表を参照

pocof では以下のようにした。

[`RootModule`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_module_manifests?view=powershell-7.5#rootmodule) は loader script を指定。

```powershell
    # Script module or binary module file associated with this manifest.
    RootModule = 'pocof.psm1'
```

`pocof.psm1` で [`$PSVersionTable.PSVersion`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_automatic_variables?view=powershell-7.5#psversiontable) を検査し load し分ける。

```powershell
#
# Script module for module 'pocof'
# Based on https://learn.microsoft.com/en-us/powershell/gallery/concepts/module-psedition-support?view=powershellget-3.x#targeting-multiple-editions
#
Set-StrictMode -Version Latest

$PSModule = $ExecutionContext.SessionState.Module
$PSModuleRoot = $PSModule.ModuleBase

# NOTE: Import the appropriate nested binary module based on the current PowerShell version.
# https://learn.microsoft.com/en-us/powershell/scripting/install/powershell-support-lifecycle?view=powershell-7.5#powershell-end-of-support-dates
if ($PSVersionTable.PSVersion -lt [Version]'7.2') {
    $targetFramework = 'netstandard2.0'
}
else {
    $targetFramework = 'net6.0'
}
# NOTE: Build paths separately for Windows PowerShell compatibility.
$binaryModuleRoot = Join-Path -Path $PSModuleRoot -ChildPath $targetFramework
$binaryModulePath = Join-Path -Path $binaryModuleRoot -ChildPath 'pocof.dll'
$binaryModule = Import-Module -Name $binaryModulePath -PassThru

Write-Verbose "pocof: Loaded $targetFramework binary from $binaryModulePath" -Verbose

# NOTE: When the module is unloaded, remove the nested binary module that was loaded with it.
$PSModule.OnRemove = {
    Remove-Module -ModuleInfo $binaryModule
}
```

loader script は後方互換を意識しないと、対応したい古い PowerShell で利用できない可能性がある。
例えば `Join-Path` で [`AdditionalChildPath`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/join-path?view=powershell-7.5#-additionalchildpath) が使えるのは PowerShell 6.0 以降なので、後方互換のため複数回呼ぶ。

あと、わたしがまだ Multi targeting に慣れてないので [`Write-Verbose`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-verbose?view=powershell-7.5) で読み込む module の情報を出力するようにしている。

このようにして Multi targeting された project は明示的に [`framework` option](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build#options) で対象を指定しないと build できない。
`net6.0` と `netstandard2.0` を明示的に指定するよう build script を調整した。
build した dll を publish 用の directory 出力するよう project の調整も必要だ。

```xml
    <!-- Deploy the produced assembly -->
    <PublishDir>../../publish/pocof/$(TargetFramework)/</PublishDir>
```

これで publish 用の directory にあるファイルはすべて Publish-PSResource で publish されるはず。
module 構成は以下のようにした。

```plaintext
C:\USERS\TAKATOSHI\DEV\GITHUB.COM\KRYMTKTS\POCOF\PUBLISH\POCOF
|   .gitkeep
|   pocof-Help.xml
|   pocof.psd1
|   pocof.psm1
|
+---net6.0
|       FSharp.Core.dll
|       pocof.dll
|
\---netstandard2.0
        FSharp.Core.dll
        pocof.dll
```

配信ファイルが増えてサイズが大きくなるがそれは許容するしかないか。
あとほんとにこれで publish が上手くいくか試してないので、テスト用の module で挙動をチェックしたい感じではある。

Multi targeting で注意したい点がいくつかある。

まず `net6.0` を target して build した場合に [`Nullable`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values#null-values-starting-with-f-9) 有効化時の検査が `netstandard2.0` より厳しくなる。
これは `null` 避けすればいいだけだが、再現が難しい path は coverage を下げる原因にもなり得る。
また F# 開発を Visual Studio Code + Ionide で行っているが、 Multi targeting していると片方の build error しか検知できない。
Ionide は [`TargetFrameworks`](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#targetframeworks) の先頭に書いた framework で build するようなので、 よりチェックが厳しい `net6.0` を先頭にした。
開発時に `netstandard2.0` での build 結果を見ることができないのは少し不安だが、 build script と CI でその点をカバーするつもり。

あと、今後 framework の [conditional compilation symbols](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives#conditional-compilation) で分岐するので、 unit testing や coverage もそれぞれ実行する必要がある。
test project が pocof の project 参照しているため、 parameter を付与して指定の [`TargetFramework`](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#targetframework) で build できるよう変更した。

```xml
  <ItemGroup>
    <ProjectReference Include="..\pocof\pocof.fsproj"
                      Properties="TargetFramework=$(TestTargetFramework)"
                      Condition="'$(TestTargetFramework)' != ''" />
    <ProjectReference Include="..\pocof\pocof.fsproj"
                      Condition="'$(TestTargetFramework)' == ''" />
  </ItemGroup>
```

これで 2 つの framework で実行できるので、まとめて実行するのは build script で吸収する。
まだ benchmark や memory layout を確認する project ではこれに対応していないので、特に benchmark については必要になり次第調整したい。

2 つの `TargetFramework` で coverage 計測したら当然 report も 2 つになる。
pocof では CI の coverage report に [Codecov](https://about.codecov.io/) を使っているが、幸い[複数 report に対応している](https://github.com/codecov/codecov-action?tab=readme-ov-file#arguments)のでまとめて送りつけることにした。

```yml
- name: Execute All Tests
  id: run_tests
  shell: ${{ inputs.shell }}
  run: |
    Invoke-Psake -taskList TestAll
    if (-not $psake.build_success) { exit 1 }
    $reports = (Get-ChildItem ./src/pocof.Test/TestResults/coverage.*.cobertura.xml) -join ','
    "REPORT_PATHS=$reports" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
- name: Upload coverage reports to Codecov
  uses: codecov/codecov-action@v5
  if: runner.os == 'Linux'
  with:
    file: ${{ steps.run_tests.outputs.REPORT_PATHS }}
  env:
    CODECOV_TOKEN: ${{ inputs.codecov_token }}
```

この日記を書いてる途中に気付いたが、 `file` は非推奨なので `files` に変えるのが正しいな。あとで直す。

とりま build 構成と PowerShell module 構成についてのまとめはこんな感じかな。
ここまでやればこれまでの開発と同じように Multi targeting での開発もできるはずだ。
あとは conditional compilation symbols を使った実装で、 `net6.0` と `netstandard2.0` の両方でうまく開発する工夫が必要になってくる。
これは開発を進める中でよい落とし所を模索していきたい。
