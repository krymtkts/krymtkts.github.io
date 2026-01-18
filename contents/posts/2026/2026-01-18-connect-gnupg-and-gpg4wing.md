---
title: Windows の GnuPG と Gpg4Win の連携
tags: [gnupg]
---

つい先日 `git commit` したとき [GnuPG](https://www.gnupg.org/) での commit sign が通らなくなったので、その解消方法をログに残す。

まず前提を整理する。
実際のところ commit sign するだけなら GnuPG だけでも良い。
けど、 passphrase を入力する window が味気なく masking 解除できないのも好かんので、 [Gpg4Win](https://www.gpg4win.org/) の `pinentry.exe` を使ってた。
理想は `pinentry-mode loopback` で CLI 入力するのが好きだ。
ただ [Visual Studio Code](https://code.visualstudio.com/) と一緒に使うと支障があり、 Gpg4Win の `pinentry.exe` に落ち着いてた。

GnuPG と Gpg4Win はそれぞれ [Chocolatey](https://chocolatey.org/) で入れてる。
初回 install は以下のような感じだ。

```powershell
choco install gnupg gpg4win
```

この構成で使っていて、まず年末年始頃に `choco upgrade all -y` したあと commit sign できなくなった。
細かく調べてないが、どうも GnuPG 2.5 系になったため GPG の path が変わったからっぽい。

そのため `~/.gitconfig` で指定する `gpg` の path を変更する必要があった。

```ini
[gpg]
	# program = C:\\Program Files (x86)\\gnupg\\bin\\gpg.exe
	program = C:/Program Files/GnuPG/bin/gpg.exe
```

それでも以下のように sign できなかった。 `FC536D8DE73F2424` は GitHub で使ってるわたしの GPG Key ID だ。

```powershell
> git commit -m 'Add a new post'
error: gpg failed to sign the data:
gpg: skipped "FC536D8DE73F2424": No secret key
[GNUPG:] INV_SGNR 9 FC536D8DE73F2424
[GNUPG:] FAILURE sign 17
gpg: signing failed: No secret key
fatal: failed to write commit object
```

今思えば Gpg4Win が GnuPG 2.5 系に追いついてなかったからなのだろうけど、 `pinentry.exe` が見つからなくなってるってのまでは理解できた。
なので `gpg-agent.conf` で `pinentry.exe` が見つかるように指定してやれば解消できる。

```powershell
cat $env:APPDATA/gnupg/gpg-agent.conf
```

```plaintext
default-cache-ttl 86400
max-cache-ttl 86400

pinentry-program C:\Program Files (x86)\Gpg4win\bin\pinentry.exe
```

これで `pinentry.exe` が見つかるようになり、 commit sign できるようになった。

ついでに、今までのように commit 時に passphrase を入力するようにしてたら、 GnuPG と Gpg4Win が繋がらなくなってた時の検知が遅れる。
それは気になるので、 terminal 起動時に passphrase を cache するようにしてみた。

```powershell
if (Get-Command -Name 'gpgconf' -ErrorAction SilentlyContinue) {
    gpgconf --launch gpg-agent | Out-Null
    # NOTE: open pinentry for caching passphrase.
    'warmup' | gpg --clearsign *> $null
}
```

これで一見落着したかに見えたが、後日 chocolatey で更新したらまた commit sign できなくなった。
今度は Gpg4Win が [5 系](https://gpg4win.org/version-5.0.0.html)に更新されて `pinentry.exe` の path が変わったからだ。
`Program Files(x86)` から `Program Files` に移ったようなので、一旦以下のように変えればいい。

```plaintext
default-cache-ttl 86400
max-cache-ttl 86400

pinentry-program C:\Program Files\Gpg4win\bin\pinentry.exe
```

`gpg-agent` を再起動して passphrase を cache し直せばいつも通りに戻る。

```powershell
gpgconf --kill gpg-agent
gpgconf --launch gpg-agent | Out-Null
'warmup' | gpg --clearsign *> $null
```

ただこれだと `pinentry.exe` がどこに存在するかいちいちチェックしないといけない。
なので PC 乗り換えのときに備えて `gpg-agent.conf` を更新するときに使う自前関数を更新し、 x86 でも x64 でも見つかるように修正した。

```powershell
function global:Set-GPGConfig {
    $pinentryPath = ${env:ProgramFiles}, ${env:ProgramFiles(x86)} | ForEach-Object {
        $path = "${_}\Gpg4win\bin\pinentry.exe"
        if (Test-Path $path) {
            "pinentry-program ${path}"
        }
    } | Where-Object { $_ -ne $null } | Select-Object -First 1
    @"
default-cache-ttl 86400
max-cache-ttl 86400

$pinentryPath
"@ | Set-Content "$env:APPDATA/gnupg/gpg-agent.conf"

    # currently unused.
    @'
# loopback is not work with VS Code.
# VS Code hang up if you commit with signing.
# pinentry-mode loopback
'@ | Set-Content "$env:APPDATA/gnupg/gpg.conf"
}
```

これで一通り整った。

そもそも GnuPG と Gpg4Win が別々に入ってる構成が変な気もするので、そこを見直した方がいいのかもな。
なんでこんな構成になってるのかも忘れてしまってるし。
