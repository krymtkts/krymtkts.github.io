{:title "git で local repo が自分のものじゃないとなったやつ"
:layout :post
:tags ["windows","git"]}

今日はブログ書くつもり無かったけど、これまた起こったらなんかハマりそうやなーと思ったので、したためた。

何があったかわからんが、 git に local repo の所有権が自分にないと言われてエラーになった。

```powershell
PS> git status
fatal: detected dubious ownership in repository at 'C:/Users/takatoshi/dev/github.com/krymtkts/Get-GzipContent'
To add an exception for this directory, call:

        git config --global --add safe.directory C:\Users\takatoshi\dev\github.com\krymtkts\Get-GzipContent
Set the environment variable GIT_TEST_DEBUG_UNSAFE_DIRECTORIES=true and run
again for more information.
```

`safe.directory` が設定されているパスなのだけど。

```powershell
PS> git config --list | ? {$_ -like 'safe*'}
safe.directory=C:/Users/takatoshi/dev/*
```

`GIT_TEST_DEBUG_UNSAFE_DIRECTORIES` でデバッグメッセージを有効化できるそうなのでしてみたところ、以下の様なのが出た。
最近使ってた local repo では出なくて、~~放置してた~~ 最近ご無沙汰だった repo で出ている。

```powershell
PS> $env:GIT_TEST_DEBUG_UNSAFE_DIRECTORIES=$true
PS> git status
warning: 'C:/Users/takatoshi/dev/github.com/krymtkts/Get-GzipContent/.git' is owned by:
        'S-1-5-32-544'
but the current user is:
        'S-1-5-21-3808303910-2770483448-703627078-1001'
fatal: detected dubious ownership in repository at 'C:/Users/takatoshi/dev/github.com/krymtkts/Get-GzipContent'
To add an exception for this directory, call:

        git config --global --add safe.directory C:/Users/takatoshi/dev/github.com/krymtkts/Get-GzipContent
```

owner が違う...なんで owner 変わってしまってたんだろう。ユーザー 1 人しかいないのに。
あーハイハイ、と想像がついたので試したところ、管理者権限でならエラーが出ない。

どうも過去に管理者権限で作成した local repo の所有者が Administrator になってた模様。それが最近の Git for Windows の更新で検知されるようになったんだ。

Windows で所有者を変更する方法を調べてみて、それっぽい手順があったのでそれに倣う。
Windows のこの辺の昨日全然知らないので、どっかのタイミングで調べないといけないな。
[Using Takeown.exe Command to Take Ownership of a File or Folder – TheITBros](https://theitbros.com/using-takeown-exe-command-to-take-ownership-of-file-or-folder/#:~:text=You%20can%20change%20the%20owner,%3E%20Advanced%20%3E%20Owner%20%3E%20Change.)

```powershell
takeown /F C:\Users\takatoshi\dev\github.com\krymtkts\Get-GzipContent /R
# SUCCESS: The file (or folder):  ....
# えげつない量の印字...
```

```powershell
PS> git status
On branch master
Your branch is up to date with 'origin/master'.

nothing to commit, working tree clean
```

イイね。直せた。
他のディレクトリも所有者を変えた。

```powershell
takeown.exe /f . /r | Out-Null
# えげつない量の印字を無に帰す...
```

終。
