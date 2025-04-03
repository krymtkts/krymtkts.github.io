---
title: "Chocolatey の OpenSSH をやめて WinGet の OpenSSH にする 2025"
tags: ["openssh", "chocolatey", "winget"]
---

最近の活動は、 pocof の benchmark に基づいた細かい改善とか、 Command-line predictor を作り始めたのとか。
でも今日は最早わたしの中で恒例となった [Chocolatey](https://github.com/chocolatey/choco) での OpenSSH 話を記す。

---

随分長い間気づいてなかったのだけど OpenSSH の Chocolatey の 最新 prerelease 壊れとるね。

[openssh 9.5.0-beta20240403 seems to contain OpenSSH_for_Windows_8.6p1, (#105) · Issues · DarwinJS / ChocoPackages · GitLab](https://gitlab.com/DarwinJS/ChocoPackages/-/issues/105)

気づかず使ってた。
repo 見ても最新は上記のママだし、変なバイナリ使ってたんかと思ってゾッとしたわ...
一応以下のコマンドで 1 つ前の正しいやつに戻せる。

```powershell
choco install openssh --version=9.5.0-beta1 -params '"/SSHAgentFeature"' -y -pre --force
choco pin add --name='openssh'
```

念の為 9.5.0-beta1 に固定したけどこれからどうしよかね。
Windows 11 の標準の SSH が更新によって 9.5.2.1 になってる[^1]ので、 Chocolatey のやつを使う意味がないという。

[^1]: 多分これか → [OpenSSH for Windows の 2024 年 10 月の更新について | Microsoft Japan Windows Technology Support Blog](https://jpwinsup.github.io/blog/2024/11/12/OpenSSH/OpenSSH_update_oct_2024/)

Windows の OpenSSH の install に Universal Installer を使うのはもう長らく deprecated で、ただ chocolatey で管理するの楽なので依存し続けてた。
[[Deprecated] Win32 OpenSSH Automated Install and Upgrade using Chocolatey · PowerShell/Win32-OpenSSH Wiki](https://github.com/PowerShell/Win32-OpenSSH/wiki/%5BDeprecated%5D-Win32-OpenSSH-Automated-Install-and-Upgrade-using-Chocolatey)

今の公式な install 手段としては [MSI 直](https://github.com/PowerShell/Win32-OpenSSH/wiki/Install-Win32-OpenSSH-Using-MSI)か [WinGet](https://github.com/PowerShell/Win32-OpenSSH/wiki/Install-Win32-OpenSSH) な感じはする。
この手段でなら最新の beta も取れるし。
[WinGet](https://github.com/microsoft/winget-cli) にはまだあんま踏み込んでないけど、この機会に始めるべきかな～ そんで [DSC](https://learn.microsoft.com/en-us/powershell/dsc/overview?view=dsc-3.0) も嗜む？みたいな。

何度か WinGet への移行は考えてたけど、ビミョーに痒いところに手が届かないのよな。 [Maven](https://maven.apache.org/) がないとか(`mvn` はもう殆ど使わないのだけど [krymtkts/MavenAutoCompletion](https://github.com/krymtkts/MavenAutoCompletion) がある関係で一応必要)。
Maven がないのは [`Scripted-Application`](https://github.com/microsoft/winget-pkgs/labels/Scripted-Application) って label が付いてる `*.bat` とか `*.cmd` とかの script として提供されてる application の install を WinGet の security policy で禁じてるからなので...どうしようもない。

でもずっと許可しないとか有り得んよな～と思って調べたら、やっぱ issue に挙がってきてた(それにしては随分最近でびっくりした)。

[Support other portable application formats · Issue #5083 · microsoft/winget-cli](https://github.com/microsoft/winget-cli/issues/5083)

というわけで WinGet の今後に期待し、とりあえず新しいのを使いたいから一旦 OpenSSH だけ winget に移行した。

```powershell
winget install --id Microsoft.OpenSSH.Preview --scope machine --override ADDLOCAL=Client
```

Chocolatey から WinGet へのスムースな移行とかもできるかわからんので当面は二刀流か。
続く。
