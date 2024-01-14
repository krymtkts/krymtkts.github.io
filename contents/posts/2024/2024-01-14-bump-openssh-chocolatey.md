---
title: "Chocolatey で Portable OpenSSH を更新する 2024"
tags: ["powershell", "openssh"]
---

少し前の話。
2024 を目前にして、久しぶりに Chocolatey に OpenSSH の β が降ってきてた。
[Chocolatey Software | Win32 OpenSSH (Universal Installer) 9.5.0-beta1](https://community.chocolatey.org/packages/openssh/9.5.0-beta1)

前回は 2023 年だった。 [2023-05-20 - Chocolatey で Portable OpenSSH を更新する 2023](/posts/2023-05-20-bump-openssh-chocolatey.html)

2023 年に自分で bump したとき、 `/SSHServerFeature` を使うって書いてた。
でも、これ `uninstall-sshd.ps1` で sshd service を消しておいたら `/SSHAgentFeature` でもエラーせずに更新できるわ、というのに今更気づいた。
そも sshd が立つのも、自分の PowerShell profile で `ssh-agent` のサービスがいないときに `install-sshd.ps1` してるからなので。

[openssh · master · DarwinJS / ChocoPackages · GitLab](https://gitlab.com/DarwinJS/ChocoPackages/-/tree/master/openssh#-params-sshagentfeature)

> -params '"/SSHAgentFeature"'
>
> Installs SSH Agent Service even if SSHD Server is not being installed.
> Requires admin rights to configure service.
> IMPORTANT: ssh-agent is no longer required for sshd after version openssh 1.0.0.0

ちゃんとドキュメント読んでいこうぜ。

```powershell
# 一度 uninstall しておく
uninstall-sshd.ps1
choco uninstall openssh -y
# 消えたの確認
Get-Service ssh-agent | select -Property Name,ServiceHandle,DisplayName,ServiceType,StartType

# 入れ直す
choco install openssh -params '"/SSHAgentFeature"' -y
Get-Service ssh-agent | select -Property Name,ServiceHandle,DisplayName,ServiceType,StartType

# Name          : ssh-agent
# ServiceHandle : Microsoft.Win32.SafeHandles.SafeServiceHandle
# DisplayName   : ssh-agent
# ServiceType   : Win32OwnProcess
# StartType     : Automatic
```

`install-sshd.ps1` で `ssh-agent` 入れると、 `StartType: Manual` になるから別途 `Automatic` に変更する必要があった。
正しい手順ならそのひと手間もいらないぽい。

[2019-03-31 - Windows10 の更新で OpenSSH が逝った](https://krymtkts.github.io/posts/2019-03-31-win-openssh-is-gone.html) ではちゃんと `/SSHAgentFeature` 使ってた。
なのにその後 [2019-06-29 - また OpenSSH が動かなくなる季節がやってきた](https://krymtkts.github.io/posts/2019-06-29-season-of-openssh-error.html) でそれを忘れて `install-sshd.ps1` でサービス起動するように変えてた。これがことの発端ぽいな。

その後 [Chocolatey で Portable OpenSSH を更新する 2023](/posts/2023-05-20-bump-openssh-chocolatey.html) で判断を誤る。
`/SSHAgentFeature` じゃなく `/SSHServerFeature` を使うようになってしまった。
期間が開くと色々ノウハウが失われていく教科書的なパターン。

今後はちゃんと `/SSHAgentService` 使ってやるようにせなあかんな。
ひとまず profile で `ssh-agent` サービス立ち上がってなかったら `install-sshd.ps1` 流すような記述してのを消した。

---

にしてもわたしの profile も継ぎ足し守られた秘伝のタレ化して、管理が難しくなってきてる。
もう利用してないやつは消したり、必要な初期化は関数小分けにするとかして改善せなあかん時がきたか...

Gist で 1 ファイルだけ管理してるのも download のやりやすさや更新のしやすさがあっていいけど、なんか他の方法考えなあかんかもな。
古くは VS Code の [Gist](https://marketplace.visualstudio.com/items?itemName=kenhowardpdx.vscode-gist) 、最近は [GistPad](https://marketplace.visualstudio.com/items?itemName=vsls-contrib.gistfs) で編集してる。
楽に編集できて便利やからなー、他の方法としたら git repo があるけどやっぱ手軽さは劣るよな。
