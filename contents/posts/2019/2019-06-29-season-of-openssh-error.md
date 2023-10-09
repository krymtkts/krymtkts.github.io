---
title: "またOpenSSHが動かなくなる季節がやってきた"
tags:  ["powershell", "openssh"]
---

アップデートの度に何かあるので、もはや風物詩と化したOpenSSHのエラー。

[前回のエラー](/posts/2019-03-31-win-openssh-is-gone.html)

きょうChocolateyでパッケージ更新したらWindowsのOpenSSHがアップデートされた様子。[Release v8.0.0.0p1-Beta · PowerShell/Win32-OpenSSH](https://github.com/PowerShell/Win32-OpenSSH/releases/tag/v8.0.0.0p1-Beta)かな？

それに伴い`ssh-agent`サービスが消え去ってしまった。

```powershell
$ Get-Service -Name 'ssh-agent'
Get-Service : Cannot find any service with service name 'ssh-agent'.
At line:1 char:1
+ Get-Service -Name 'ssh-agent'
+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
+ CategoryInfo          : ObjectNotFound: (ssh-agent:String) [Get-Service], ServiceCommandException
+ FullyQualifiedErrorId : NoServiceFoundForGivenName,Microsoft.PowerShell.Commands.GetServiceCommand
```

単純に再インストールしても自動でサービス登録はされなかったのだけど、同梱されているファイルを見てみたところそれらしいスクリプト`install-sshd.ps1`を発見した。Wikiにも記載がある。

[Install Win32 OpenSSH · PowerShell/Win32-OpenSSH Wiki](https://github.com/PowerShell/Win32-OpenSSH/wiki/Install-Win32-OpenSSH#install-win32-openssh-test-release)

実行してみたところ無事にサービスが作成されたので一安心。サービス自動起動の設定をしておいて完了した。

```powershell
$ .\install-sshd.ps1
[SC] SetServiceObjectSecurity SUCCESS
[SC] ChangeServiceConfig2 SUCCESS
[SC] ChangeServiceConfig2 SUCCESS
sshd and ssh-agent services successfully installed
$ Get-Service 'ssh-agent'

Status   Name               DisplayName
------   ----               -----------
Stopped  ssh-agent          OpenSSH Authentication Agent

$ Set-Service -name "ssh-agent" -startuptype "automatic"
```

ふう🙃
