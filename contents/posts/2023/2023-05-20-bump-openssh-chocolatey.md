---
title: "Chocolatey で Portable OpenSSH を更新する 2023"
tags: ["powershell", "openssh"]
---

久しぶりに、 Chocolatey に OpenSSH の β が降ってきてた。

前回は 2019 年だった。 [Windows10 の更新で OpenSSH が逝った](/posts/2019-03-31-win-openssh-is-gone.html) [また OpenSSH が動かなくなる季節がやってきた](/posts/2019-06-29-season-of-openssh-error.html)

[Chocolatey Software | Win32 OpenSSH (Universal Installer) 9.2.2-beta1](https://community.chocolatey.org/packages/openssh#versionhistory)

ので更新してみたところ、以下のエラーログが。

```plaintext
ERROR: There is a configured instance of the SSHD service, please specify the /SSHServerFeature to confirm it is OK to shutdown and upgrade the SSHD service at this time.
The upgrade of openssh was NOT successful.
Error while running 'C:\ProgramData\chocolatey\lib\openssh\tools\chocolateyinstall.ps1'.
 See log for details.
```

はて？ `sshd` 入れてたっけ？と思ったが見たら入ってた。 chocolatey で自動で入ったんやっけ？

```powershell
PS> get-service *ssh*

Status   Name               DisplayName
------   ----               -----------
Running  ssh-agent          ssh-agent
Running  sshd               sshd
```

いる！しかも実行中なのがなんかキモい。

こいつを停止させるのはあとにするとして、ひとまず `choco upgrade` を成功させるにはどうしたら良いか。
こういうときはちゃんとマニュアルを読む。 Chocolatey の Package Source のリンクを辿れば良い。

[openssh · master · DarwinJS / ChocoPackages · GitLab](https://gitlab.com/DarwinJS/ChocoPackages/-/tree/master/openssh#-params-sshserverfeature-install-and-uninstall)

> ## -params '"/SSHServerFeature"' (Install and Uninstall)
>
> Also install sshd Windows Service - including opening port 22.
> If this parameter is not included on an upgrade or uninstall and
> the sshd server is installed - an error is generated. You must
> use this switch to indicate you have made preparations for the
> sshd service to be interrupted or removed.

どう見てもこれ。以下を実行する。

```powershell
choco upgrade openssh -params '"/SSHServerFeature"' -y
```

この後再起動をして Windows Terminal で PowerShell を開くと作業が完了した。
すっかり忘れていが、わたしの `$PROFILE` は「 `ssh-agent` のサービスがいない ≒ Windows の OpenSSH が更新された」と判定し、インストールスクリプトを実行するのだった。
[ここ](https://gist.github.com/krymtkts/f8af667c32b16fc28a815243b316c5be#file-microsoft-powershell_profile-ps1-L910-L923)

```powershell
# install ssh-agent service if not exists.
# it will be triggered after updating Windows OpenSSH.
if (! ($SshAgent = (Get-Service -Name 'ssh-agent' -ErrorAction SilentlyContinue))) {
    install-sshd.ps1
    Set-Service -Name 'ssh-agent' -StartupType Automatic
    Start-Service ssh-agent
}
elseif ($SshAgent.StartType -eq 'Disabled') {
    Set-Service -Name 'ssh-agent' -StartupType Automatic
    Start-Service ssh-agent
}
else {
    Start-Service ssh-agent
}
```

インストールスクリプトは Portable OpenSSH に提供されるもので `ssh-agent` と `sshd` が一緒くたにインストールされてたのね...なんか他にやりようないか考えても良いのかな。

とりあえずこれで事なきを得た。 `sshd` の起動タイプも手動になっておりヨシ。

```powershell
PS> get-service *ssh* | select -Property Name,Status,StartType

Name       Status StartType
----       ------ ---------
ssh-agent Running Automatic
sshd      Stopped    Manual
```
