---
title: PowerShell Preview を使い始めた
subtitle: PowerShell 7.6.0-preview.6
tags: ["powershell"]
date: 2026-01-11
---

刺激を求めて PowerShell の Preview([v7.6.0-preview.6](https://github.com/PowerShell/PowerShell/releases/tag/v7.6.0-preview.6)) を使うように変えた。
CLI での install には [Chocolatey](https://community.chocolatey.org/packages/powershell-core/7.6.0-preview06) か [WinGet](https://learn.microsoft.com/ja-jp/powershell/scripting/install/install-powershell-on-windows?view=powershell-7.6#install-powershell-using-winget-recommended) が使える。
ただ Chocolatey では prerelease で提供されてるので [`choco pin`](https://docs.chocolatey.org/en-us/choco/commands/pin/) してないと [`choco upgrade all`](https://docs.chocolatey.org/en-us/choco/commands/upgrade/) すると stable に落ちてしまう。
これは面倒なので今回は winget を使うようにした。
install したときは PowerShell 7.6.0-preview.5 までしか winget の community repo に存在しなかった。
でもタイミングよくすぐ更新されて PowerShell 7.6.0-preview.6 が降ってきた。
以下のコマンドを実行すればよい。

```powershell
winget install --id Microsoft.PowerShell.Preview
```

あと [Windows Terminal](https://github.com/microsoft/terminal) でも [GitHub Copilot Chat](https://docs.github.com/en/copilot/how-tos/chat-with-copilot/chat-in-windows-terminal) が使いたくて [Canary](https://github.com/microsoft/terminal?tab=readme-ov-file#installing-windows-terminal-canary) に変えた。
これは仕事用の PC では既に切り替えてあるので、そちらと合わせる意味もある。
変えてからあんまり積極的に Windows Terminal で GitHub Copilot Chat を使ってるわけではないけど、いつも質問できる安心感的なのを得るためだ。

Windows Terminal Canary は chocolatey や winget では入れられない。
なので自前で installer を download して起動するよう関数を profile に用意してる。
いま単に installer を起動するだけにしてるが、どうも [`Add-AppxPackage`](https://learn.microsoft.com/en-us/powershell/module/appx/add-appxpackage?view=windowsserver2025-ps) が使えるみたい。
それができたら installer の削除まで一貫して行えるので、次回試してみたい。コメントアウトしてるところがそれだ。

```powershell
function Install-WindowsTerminalCanary {
    $installer = 'Microsoft.WindowsTerminalCanary.appinstaller'
    Invoke-WebRequest 'https://aka.ms/terminal-canary-installer' -OutFile 'Microsoft.WindowsTerminalCanary.appinstaller'
    Start-Process $installer
    # try {
    #     Add-AppxPackage -AppInstallerFile $installer
    # }
    # finally {
    #     Remove-Item $installer -Force -ErrorAction SilentlyContinue
    # }
}
```

PowerShell Preview に入れ替えたら、 Windows Terminal の profile にちょっと工夫が必要だった。
PowerShell の stable を入れてたときは `"source": "Windows.Terminal.PowershellCore"` だけで Preview を使えてた。
けど stable を消したら認識しなくなってしまった。なので個別に指定する必要があるみたい。
以下は GUI から操作して生成させたものなので、ひょっとしたら公式かどっかに落ちてる情報なのかな。

```json
            {
                "colorScheme": "Solarized Dark - Patched",
                "guid": "{a3a2e83a-884a-5379-baa8-16f193a13b21}",
                "hidden": false,
                "name": "PowerShell 7 Preview",
                // "source": "Windows.Terminal.PowershellCore",
                "commandline": "\"C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe\"",
                "icon": "ms-appx:///ProfileIcons/pwsh-preview.png",
                "startingDirectory": "%USERPROFILE%",
                "tabColor": "#001E27"
            },
```

Preview に入れ替えたせいか知らんが [`Get-ExperimentalFeature`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/get-experimentalfeature?view=powershell-7.6) の出力が変わったような。最近見てなかったから定かではない。
これまで更新してきた分の残骸かな？

```powershell
> Get-ExperimentalFeature | Format-List

Name        : PSLoadAssemblyFromNativeCode
Enabled     : True
Source      : PSEngine
Description : Expose an API to allow assembly loading from native code

Name        : PSProfileDSCResource
Enabled     : False
Source      : PSEngine
Description : DSC v3 resources for managing PowerShell profile.

Name        : PSSerializeJSONLongEnumAsNumber
Enabled     : True
Source      : PSEngine
Description : Serialize enums based on long or ulong as an numeric value rather than the string
              representation when using ConvertTo-Json.
```

いま [`powershell.config.json`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_powershell_config?view=powershell-7.6) に入ってるものは以下(整形済み)だった。随分多いな。

```json
{
  "ExperimentalFeatures": [
    "PSFeedbackProvider",
    "PSCommandNotFoundSuggestion",
    "PSSubsystemPluginModel",
    "PSLoadAssemblyFromNativeCode",
    "PSNativeWindowsTildeExpansion",
    "PSSerializeJSONLongEnumAsNumber"
  ]
}
```

以下を見るに 7.5, 7.6 で mainstream になった feature が残ってたみたい。

[Using Experimental Features in PowerShell - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/learn/experimental-features?view=powershell-7.6#available-features)

experimental からなくなった feature は取り除いておいた。

最後に winget で管理してる application が増えたので更新のタイミングがわかりやすいよう profile に仕込んでみた。
以下がこれまでに winget で入れるようになったもの。

- `Microsoft.OpenSSH.Preview`
- `Microsoft.PowerShell.Preview`
- `Microsoft.VisualStudioCode.Insiders`

```powershell
if (Get-Command Get-WinGetPackage -ErrorAction SilentlyContinue) {
    @(
        'Microsoft.VisualStudioCode.Insiders'
        'Microsoft.OpenSSH.Preview'
        'Microsoft.PowerShell.Preview'
    ) | ForEach-Object {
        $pkg = Get-WinGetPackage -Id $_
        if (($pkg -and $pkg.IsUpdateAvailable)) {
            Write-Warning "💡 Newer '${_}' is available. $($pkg.AvailableVersions | Where-Object {
                [version]$_ -gt [version]$pkg.InstalledVersion
            } | Sort-Object -Descending | Select-Object -First 1)"
        }
    }
}
```

[Microsoft.WinGet.Client](https://www.powershellgallery.com/packages/Microsoft.WinGet.Client/1.11.460) で簡単に操作できるのがありがたい。
パッと見 `AvailableVersions` は降順に並ぶようだけど並び替えと絞り込みを入れておいた。
`Get-WinGetPackage` が使えるかもチェックしている。

これで更新があれば PowerShell 起動時にログが出て多分気付ける。
その後の更新は command prompt から手で実行することになる想定。未だに PowerShell 内から PowerShell を更新する良い手順を知らない。

特に違いも感じずまだ刺激になってないけど、これからは preview を使っていこう。
VS Code も GitHub Copilot の機能をいち早く使うため Insider にしてるし、なんか unstable だれけになりつつある。

---

2026-01-11 追記。

VS Code で PowerShell Extension が利かなくなった。
どうも Preview だけだと `pwsh` バイナリが見つからないみたい。
なので設定で直に `pwsh` への path を指定してやる必要があった。

```json
  "powershell.powerShellAdditionalExePaths": {
    "pwsh-preview": "C:\\Program Files\\PowerShell\\7-preview\\pwsh.exe"
  },
  "powershell.powerShellDefaultVersion": "pwsh-preview",
```
