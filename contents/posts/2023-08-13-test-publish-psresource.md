---
title: "Publish-PSResource を試す"
tags: ["powershell"]
---

[以前から気になってた](/posts/2023-07-23-psresourceget.html) PSResourceGet の `Publish-PSResource` を試す機会があったので試した。
試したと書いてるけど、自分の利用ケースで問題なく使えるか見ただけ。

[krymtkts/PSJobCanAttendance](https://github.com/krymtkts/PSJobCanAttendance/) を修正する機会があったので、ついでに `Publish-PSResource` へ切り替えた。上手くいってた。
[#9](https://github.com/krymtkts/PSJobCanAttendance/pull/9)

(追記 2023-08-15)
試したバージョンは以下の通り。

```powershell
PS> Get-Module *PSResourceGet; $PSVersionTable | Format-Table

ModuleType Version    PreRelease Name                                ExportedCommands
---------- -------    ---------- ----                                ----------------
Binary     0.5.23     beta23     Microsoft.PowerShell.PSResourceGet  {Find-PSResource, Get-InstalledPSResource, Get-PS…


Name                           Value
----                           -----
PSVersion                      7.3.6
PSEdition                      Core
GitCommitId                    7.3.6
OS                             Microsoft Windows 10.0.22621
Platform                       Win32NT
PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0…}
PSRemotingProtocolVersion      2.3
SerializationVersion           1.1.0.1
WSManStackVersion              3.0
```

[Publish-PSResource (Microsoft.PowerShell.PSResourceGet) - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/publish-psresource?view=powershellget-3.x) を参照して、
`Path`, `ApiKey`, `Verbose`, `WhatIf` を使った。
`Verbose`, `WhatIf` は確認用なのでなくても良い。
`Repository` も省略できて、省略した場合は優先度が高い repository に公開される。自分の場合は PowerShell Gallery 。

PSJobCanAttendance の場合は公開するファイルが少ないからか、実行したら一瞬で公開された。 PSResourceGet になったら公開まで高速化されるんや。

これでいけそうな感触を得たので、 pocof でも試す。 [krymtkts/pocof#54](https://github.com/krymtkts/pocof/pull/54)

`AllowPrerelease` とか `RequiredVersion` とかのオプションで指定していたところがなくなって、 `*.psd1` から読み取るように変わってるぽい。
ただ `ModuleName` とかの指定どうなるんやと思ったけど、ディレクトリ名ぽいな。

ということなので、従来の pocof の公開方法である `(Get-Module).Path` を渡す方法は無理だってこと。これだと ddl のパスが得られるのだけどこれは弾かれたし、 `*.psd1` のパスを渡す方法だと親ディレクトリがバージョン番号になってモジュール名にならない。 API Key にモジュール名の制限をかけてたから権限で弾かれて変な数字の名前をしたモジュールの公開を免れた。
そこんところを公開用のディレクトリにモジュール類をコピーして、その中の `pocof.psd1` を `Publish-PSResource` するようにしてみた。

```
+---.github
+---coverage
+---docs
+---publish
|   \---pocof <- ここへこぴる
|       \---pocof.psd1
+---src
|   \---pocof
        \---bin
            \---Release
                \---*
                    \---pocof.psd1 <- コピー元
\---tests
```

が、以下のようなエラーでまだ上手くいってない。

```
Error: 2023-08-13 18:59:25:
At C:\Users\takatoshi\dev\github.com\krymtkts\pocof\psakefile.ps1:108 char:5 +     Publish-PSResource @Params +     ~~~~~~~~~~~~~~~~~~~~~~~~~~ [<<==>>] Exception: Repository 'PSGallery': Response status code does not indicate success: 403 (The specified API key is invalid, has expired, or does not have permission to access the specified package.).
```

公開対象のディレクトリ名がモジュール名になるって判断が間違ってるのか...いけそうな感触を持ったけどあかんのかな、挑戦は続く。

### 追記 2023-08-14

その後、 PowerShell Gallery の API Key を作り直したり、 pocof のみ更新可能に絞っていた package を `*` にして全権与えてみたりしたがダメだった。
DDL の Module に対応してないような旨はどこにもなかった気がするけど。

Issue で気になるものとしては、 prerelease の依存関係を持つケースに対応してないというのがある。
[PSResourceGet module prerelease version scheme issues · Issue #1251 · PowerShell/PSResourceGet](https://github.com/PowerShell/PSResourceGet/issues/1251)
けど pocof 自身はそういう依存関係を持ってないので該当しないはず。
~~他の可能性があるとしたら、 prerelease のみのバージョン履歴を持つ場合に未知のバグがあるとかかな。~~
普通に自家製バグ、見当外れ。

いかんせん条件が定かでないので、一通り他の PSResourceGet の Cmdlet も試してみるとかが妥当だろうけど、めんどくせえええ...
~~でも解決しないと pocof のプルリク永久に生き続けるし、事象を調べるか誰かが解決するのを待つか、面倒な選択しかない。~~
自家製バグが原因なので時間かけてでも調査して当たり前。

~~なんかいい感じにサクッとできるつもりだったがそうならなかったのは、我ながら「持ってる」な。しらんけど。~~
単に休みボケのケアレスミス発動しただけ。

### 追記 2023-08-16

昨日この件を調べるためのテストモジュールを書いた。
F# で空のモジュールをサクッと書いて PSResourceGet と PowerShellGet の両方で公開できるようなのを。
でも PowerShellGet でも同じような権限のエラーになった。

それもそのはず普通に自分が書いた psake タスクのバグだったわ...恥ずかし。
API Key を `string` で取り出さなあかんとこ `PSCredential` がそのまま渡ってた...(`SecureString` じゃないんやというのは置いといて)

[pocof/psakefile.ps1 at 83e3bf2691a8485a6b2595934845204d21b885a2 · krymtkts/pocof](https://github.com/krymtkts/pocof/blob/83e3bf2691a8485a6b2595934845204d21b885a2/psakefile.ps1#L101-L108)

間違いそのママの差分はこんな感じ。

```diff
     $Params = @{
-        Name = $ModuleName
-        NugetAPIKey = (Get-Credential API-key -Message 'Enter your API key as the password').GetNetworkCredential().Password
+        Path = $p.FullName
+        Repository = 'PSGallery'
+        ApiKey = (Get-Credential API-key -Message 'Enter your API key as the password') # ここが終わってる
         WhatIf = $DryRun
         Verbose = $true
-        AllowPrerelease = $true
-        RequiredVersion = $RequiredVersion
     }
-    Publish-Module @Params
+    Publish-PSResource @Params
```

正しいのはこう。

```diff
-        ApiKey = (Get-Credential API-key -Message 'Enter your API key as the password')
+        ApiKey = (Get-Credential API-key -Message 'Enter your API key as the password').GetNetworkCredential().Password
```

2 日浪費したけど解決してよかったわ。
