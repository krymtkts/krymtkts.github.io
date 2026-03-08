---
title: "PowerShell Local Resource Manager Part 1"
tags: ["powershell"]
---

[前回触れた](/posts/2026-03-01-fsharp-powrshell-module-template-part-4.html) PowerShell の project-local な依存管理を提供する薄い wrapper module を作り始めた。
仮に名前は PowerShell Local Resource Manager ということで pslrm としている。

[krymtkts/pslrm](https://github.com/krymtkts/pslrm)

まだ未完成だが、 project- local で動かせるんだなといのがわかったので、ひとまず repository 公開した。
でもまだ [PowerShell Gallery](https://www.powershellgallery.com/) に公開するには使えなさ過ぎるので、やってない。

読みは acronym なので "P-S-L-R-M" でもいいし、親しみを込めて "ps-lurm"("Pee Slurm") と疑似的に読んでもいい。
AI 曰く、擬似的に読むなら "ps-lrm" -> "Pee Slurm" が自然な読み方で、日本語的に「ピーエスラーム」とはならんそうだ。

pslrm では [PSResourceGet](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/?view=powershellget-3.x) に PowerShell からの取得や依存関係の解決を任せている。
だから pslrm がやる仕事は少ない。

- project-local を指定して [`Save-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/save-psresource?view=powershellget-3.x) すること。
- project-local な module を global 汚染せず別 session で動かすこと。

しかしこれだけでもまだできない機能があるくらいなので、随分と面倒だ。
特に「project-local な module を別 session」のは process か runspace を分けることになる。
今のところ pslrm 0.0.1 では runspace を分けるだけにしている。 process を分けると起動が流石に遅いかと考えた。
現状でもかなり遅いのだが、 runspace を分けて最低限 project-local な resource を動かせるところまでは確認できた。

以下は [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) を動かしてみた例だ。呼び出しが極めて面倒だが、動くのは動く。

```plaintext
> Import-Module .\pslrm.psd1 -Force
> get-command -Module pslrm

CommandType     Name                                               Version    Source
-----------     ----                                               -------    ------
Function        Get-InstalledPSLResource                           0.0.1      pslrm
Function        Install-PSLResource                                0.0.1      pslrm
Function        Invoke-PSLResource                                 0.0.1      pslrm
Function        Restore-PSLResource                                0.0.1      pslrm
Function        Uninstall-PSLResource                              0.0.1      pslrm
Function        Update-PSLResource                                 0.0.1      pslrm

> cat .\psreq.psd1
@{
    Pester = @{
        Repository = 'PSGallery'
        Prerelease = $true
    }
    PSScriptAnalyzer = @{
        Repository = 'PSGallery'
        Prerelease = $true
    }
}
> Install-PSLResource -Path . -Confirm:$false

Name        : PSScriptAnalyzer
Version     : 1.24.0
Repository  : PSGallery
IsDirect    : True
ProjectRoot : C:\Users\takatoshi\dev\krymtkts.github.com\krymtkts\pslrm

Name        : Pester
Version     : 6.0.0-alpha5
Repository  : PSGallery
IsDirect    : True
ProjectRoot : C:\Users\takatoshi\dev\krymtkts.github.com\krymtkts\pslrm

> Get-InstalledPSLResource -Path . -IncludeDependencies

Name        : PSScriptAnalyzer
Version     : 1.24.0
Repository  : PSGallery
IsDirect    : True
ProjectRoot : C:\Users\takatoshi\dev\krymtkts.github.com\krymtkts\pslrm

Name        : Pester
Version     : 6.0.0-alpha5
Repository  : PSGallery
IsDirect    : True
ProjectRoot : C:\Users\takatoshi\dev\krymtkts.github.com\krymtkts\pslrm

> Invoke-PSLResource -Path . -CommandName Invoke-ScriptAnalyzer -Arguments @('-Path', '.\src', '-Recurse')

RuleName : PSUseShouldProcessForStateChangingFunctions
Severity : Warning
Line     :
Column   :
Message  : Function 'New-PSLRMResourceObject' has verb that could change system state. Therefore, the
           function has to support 'ShouldProcess'.

RuleName : PSUseShouldProcessForStateChangingFunctions
Severity : Warning
Line     :
Column   :
Message  : Function 'New-Resource' has verb that could change system state. Therefore, the function has
           to support 'ShouldProcess'.

RuleName : PSUseSingularNouns
Severity : Warning
Line     :
Column   :
Message  : The cmdlet 'Get-LockfileResourceNames' uses a plural noun. A singular noun should be used
           instead.

RuleName : PSUseSingularNouns
Severity : Warning
Line     :
Column   :
Message  : The cmdlet 'ConvertTo-PSLResourceInvocationArguments' uses a plural noun. A singular noun
           should be used instead.

RuleName : PSUseOutputTypeCorrectly
Severity : Information
Line     :
Column   :
Message  : The cmdlet 'Uninstall-PSLResource' returns an object of type 'System.Object[]' but this type
           is not declared in the OutputType attribute.
```

コードも不格好でかなり遅いが、今はとりあえず project-local な依存関係の管理ができるというのがわかった。
この後本当に使いやすさであるとか例外的に支えない PowerShell module があるかとかを検証していく必要がある。
でも GitHub Actions 等での CI で個別に module を install することなく local と同じ version を使えるというのは、いいんじゃないかな。
PowerShell でも deterministic な module 管理をしたかったので気に入っている。

F# の PowerShell module template 作成からは寄り道しているが、当面 pslrm の機能を拡充していくつもり。
[Pester](https://pester.dev/) のように設定が独自 object だと型の解決に問題があるので、それは cmdlet 直の実行でなく script を実行できるようにしたい。
template で依存する PowerShell module 管理を簡単にするため pslrm のアイデアが生まれたので、どん詰まりまではやってみてもよかろう。
