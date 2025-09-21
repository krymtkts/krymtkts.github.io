---
title: "Chocolatey で install した pyenv-win を uninstall する"
tags: ["chocolatey", "windows"]
---

休みの日が家族とのお出かけで潰れがちで開発ネタが無くなってきたので、今日は小ネタを書く。

[uv](https://github.com/astral-sh/uv) を使うまでは長らく [pyenv-win](https://github.com/pyenv-win/pyenv-win) を使ってた。
でも uv 導入後は使わなくなって以降もそのまま放置していたので、思い立って pyenv-win を uninstall してみた。

pyenv-win を [Chocolatey](https://chocolatey.org/) で install してたのでまずは `chocolatey` で uninstall する。
わたしはいつも `chocolatey` を使うときは `cmd` でやる。 PowerShell を更新するときとかに面倒だからだ。

[Chocolatey Software | pyenv-win 3.1.1](https://community.chocolatey.org/packages/pyenv-win)

管理者権限で以下を実行した。
([Sudo for Windows](https://learn.microsoft.com/en-us/windows/advanced-settings/sudo/) が使えるようになってるがまだ余り試していないので古き良き手法で)

```plaintext
C:\Users\takatoshi>choco uninstall pyenv-win -y
Chocolatey v2.5.1
Uninstalling the following packages:
pyenv-win

pyenv-win v3.1.1
 Skipping auto uninstaller - No registry snapshot.
 pyenv-win has been successfully uninstalled.
Environment Vars (like PATH) have changed. Close/reopen your shell to
 see the changes (or in powershell/cmd.exe just type `refreshenv`).

Chocolatey uninstalled 1/1 packages.
 See the log for details (C:\ProgramData\chocolatey\logs\chocolatey.log).
```

Chocolatey の package によくある uninstall は何もしないやつだった。
なのでバイナリの除去とばらまかれた環境変数の掃除は自分でやる必要がある。

まず、ばらまかれた環境変数に何があるかは以下の文書が参考になる。

[Add System Settings | pyenv-win/docs/installation.md at master · pyenv-win/pyenv-win](https://github.com/pyenv-win/pyenv-win/blob/a22a0e2415ef0f9e7a95ce6e2aede468b18658ec/docs/installation.md#add-system-settings)

この辺を消せば良さそうだとわかる。

```powershell
[System.Environment]::SetEnvironmentVariable('PYENV',$env:USERPROFILE + "\.pyenv\pyenv-win\","User")
[System.Environment]::SetEnvironmentVariable('PYENV_ROOT',$env:USERPROFILE + "\.pyenv\pyenv-win\","User")
[System.Environment]::SetEnvironmentVariable('PYENV_HOME',$env:USERPROFILE + "\.pyenv\pyenv-win\","User")
[System.Environment]::SetEnvironmentVariable('path', $env:USERPROFILE + "\.pyenv\pyenv-win\bin;" + $env:USERPROFILE + "\.pyenv\pyenv-win\shims;" + [System.Environment]::GetEnvironmentVariable('path', "User"),"User")
```

しかしわたしの環境では上記とは違った。 Chocolatey で入れてるからかな。
User variable の `path` に含まれる pyenv-win の値は文書通りだった。

```powershell
[System.Environment]::GetEnvironmentVariables("User").GetEnumerator() | ? Name -Like pyenv
# 出力なし
[System.Environment]::GetEnvironmentVariables("Machine").GetEnumerator() | ? Name -Like pyenv
#
# Name                           Value
# ----                           -----
# PYENV                          %USERPROFILE%\.pyenv\pyenv-win\
#
[System.Environment]::GetEnvironmentVariable('Path', 'User') -split ';' | ? {$_ -match 'pyenv'}
#
# C:\Users\takatoshi\.pyenv\pyenv-win\bin
# C:\Users\takatoshi\.pyenv\pyenv-win\shims
#
```

以下を管理者権限で実行して消す。
[`SetEnvironmentVariable`](https://learn.microsoft.com/en-us/dotnet/api/system.environment.setenvironmentvariable?view=net-9.0) の第 2 引数を `''` と `$null` にしても環境変数を消せなかったため reg 直に消した。

```powershell
$pyenv = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' -Name 'PYENV'
Remove-ItemProperty -Path $pyenv.PSPath -Name 'PYENV'

$filtered = [System.Environment]::GetEnvironmentVariable('path', 'User') -split ';' | ? {$_ -notmatch 'pyenv'}
[Environment]::SetEnvironmentVariable('Path', $filtered -join ';', 'User')
```

pyenv-win のバイナリがどこにあるかを確認し、消す。

```powershell
Get-Command pyenv
#
# CommandType     Name                                               Version    Source
# -----------     ----                                               -------    ------
# ExternalScript  pyenv.ps1                                                     C:\Users\takatoshi\.pyenv\pyenv-win\bin\pyenv.ps1
#
rm -Recurse -Force ~/.pyenv
```

ファイル数が多くて古い自機だと削除に十数秒かかった。

多分これできれいになったはず。
他にも使わなくなったものが溜まってるだろうけど、目についたら消すって感じで。
今年買い替えるつもりだったがなんか時期を逃してしまって、いつまでこの古い Razer blade stealth 2017 を使うかわからない。
新しいのに買い替えるなら、ガッツリ使えるタイミングで買い替えたいもんな。
