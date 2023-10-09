{:title "師走に向けて PowerShell モジュールの大掃除"
:layout :post
:tags ["powershell"]}

PowerShell あるあるなのだけど、過去のモジュールが溜まってしまって自分で掃除しないといけない。
開発中でもない限りモジュールを切り戻することもないので、一気に消す。これが PowerFighter(勝手に作った PowerShell 使いの呼び名)の嗜み。

ゆーてもこしらえてあるスクリプトを流すだけ。以下の通り。個人的に `-Scope AllUsers` でモジュールを入れてるからそれに合わせた形となっている。
簡単のため改行を入れている。

```powershell
# $PSGetPath.AllUsersModules をロードするために一度流す。
Get-InstalledModule
# Uninstall 対象の取り方。 AllUser 向けに入れてる場合。
Get-Module -ListAvailable | Where-Object -Property Path -Like "$($PSGetPath.AllUsersModules)*" `
| Group-Object -Property Name | Where-Object -Property Count -GT 1 `
| ForEach-Object { $_.Group | Sort-Object -Descending -Property Version | Select-Object -Skip 1 } `
| ForEach-Object { [pscustomobject]@{
        Name = $_.Name;
        RequiredVersion = "$($_.Version)$(if ($_.PrivateData.Values.PreRelease) {'-'+$_.PrivateData.Values.PreRelease} else {''})"
    } }

# 心配なときは以下の出力の最新バージョンが Hit してないことを目検する。
# Get-Module -ListAvailable | ? -Property Path -like "$($PSGetPath.AllUsersModules)*"

# エグゼキューション。
# はじめは -WhatIf アリで流し、処理する時心配なら Confirm で一個ずつ確認する。
Get-Module -ListAvailable | Where-Object -Property Path -Like "$($PSGetPath.AllUsersModules)*" `
| Group-Object -Property Name | Where-Object -Property Count -GT 1 `
| ForEach-Object { $_.Group | Sort-Object -Descending -Property Version | Select-Object -Skip 1 } `
| ForEach-Object { [pscustomobject]@{
        Name = $_.Name
        RequiredVersion = "$($_.Version)$(if ($_.PrivateData.Values.PreRelease) {'-'+$_.PrivateData.Values.PreRelease} else {''})"
    } } | Uninstall-Module -AllowPrerelease
```

prerelease なモジュールの削除も一緒に含めるため、ごちゃごちゃしている。
`-Version` の指定を楽にする場合は `Get-InstalledModule` を使ったらいいけど、モジュールごとに実行すると遅いし...と思って使ってない。

これで消しきれるかなと思ったけど、依存関係を忘れてたことでいくつかエラーをもらった。 2 周したらきれいになるかなと思いきや、以下のモジュールに関しては消しきれなかった。

- `Configuration`
- `Metadata`
- `PackageManagement`
- `PowerShellGet`

これらは `AWS.Tools.Installer` と `PowerShellGet` が依存しており消せないとのこと。
先述のスクリプトでは最新版は確保するようになってあるし、壊れても入れ直したらいから、古いものに関してはやや強引に `-Force` を付与して消し去る。

```powershell
Get-Module -ListAvailable | Where-Object -Property Path -Like "$($PSGetPath.AllUsersModules)*" `
| Group-Object -Property Name | Where-Object -Property Count -GT 1 `
| ForEach-Object { $_.Group | Sort-Object -Descending -Property Version | Select-Object -Skip 1 } `
| ForEach-Object { [pscustomobject]@{
        Name = $_.Name
        RequiredVersion = "$($_.Version)$(if ($_.PrivateData.Values.PreRelease) {'-'+$_.PrivateData.Values.PreRelease} else {''})"
    } } | Uninstall-Module -AllowPrerelease -Force
```

この如何にも PowerFighter な近距離パワー型アプローチ、まさに PowerShell って感じ(いいたいだけ)。
穢れたモジュールフォルダを綺麗にできた。
