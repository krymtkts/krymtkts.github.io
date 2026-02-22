---
title: "Windows の Smart App Control を無効にする"
tags: ["windows"]
---

ある日 [`ghq`](https://github.com/x-motemen/ghq) がブロックされた。

```plaintext
> ghq
ResourceUnavailable: Program 'ghq.exe' failed to run: An error occurred trying to start process 'C:\Users\takatoshi\go\bin\ghq.exe' with working directory 'C:\Users\takatoshi'. An Application Control policy has blocked this file.At line:1 char:1
+ ghq
+ ~~~.
```

event log 全文。

```plaintext
> Get-WinEvent -LogName "Microsoft-Windows-CodeIntegrity/Operational" | ? -Property Message -like '*ghq.exe*' | Select-Object -First 1 | % Message
Code Integrity determined that a process (\Device\HarddiskVolume3\Program Files\PowerShell\7-preview\pwsh.exe) attempted to load \Device\HarddiskVolume3\Users\takatoshi\go\bin\ghq.exe that did not meet the Enterprise signing level requirements or violated code integrity policy (Policy ID:{0283ac0f-fff1-49ae-ada1-8a933130cad6}).
```

さっぱりわからんかったが、 Windows 11 の [Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/overview) という機能らしい。
署名されてないアプリを全殺しするみたい。わたしは Go 系の tool は GitHub 直で local build しているのでそのせい。
新しい laptop を使い始めて、最初の期間は評価モードで、その後 Windows Updates かなんかを機に強制モードとなり、ブロックされるようになったようだ。怖。

Go のバイナリ以外にも local で build してるものは全て使えなくなってると想像され面倒極まりないので、開発者には向いてないと判断した。
決まった directory を除外するとかの細かいルールを定めるのも億劫なので、やることは定まってくる。
除外ルール設定せず Privacy & security > Windows Security > App & browser control で Smart App Control を無効化する。
これまでは一度 Off にすると clean install するまで再度 On にできなかったそうだが、 [2026-02 の更新プログラムで再度有効できるよう修正された](https://windowsreport.com/windows-11-kb5074105-removes-major-limitation-from-smart-app-control/)様子。
そうならば一旦 Off にするのに何の躊躇もなく行えるので、無効にした。

久し振りに困った。非エンジニア向けの機能らしいが、めちゃくちゃ面倒な機能だな。
いまでは Windows にも開発者向け機能の設定画面があるのだし、そこで設定できれば楽なのに。
