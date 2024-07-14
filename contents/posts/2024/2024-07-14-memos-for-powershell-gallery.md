---
title: "PowerShell Gallery に関する備忘録"
tags: ["powershell"]
---

先日 [krymtkts/pocof](https://github.com/krymtkts/pocof) のメンテをしてるときに、 [PSResourceGet](https://github.com/PowerShell/PSResourceGet) で response status code 999 を受け取ってびっくりした。
おおよその原因がわかれば大した話ではないが、また忘れてびっくりしないように備忘のメモを残す。

2024-07-12 06:00 JST 過ぎに Dependable version updates で自動作成された PR を処理しようとした。

[Merge pull request #201 from krymtkts/dependabot/nuget/test-lib-8e312… · krymtkts/pocof@95be938](https://github.com/krymtkts/pocof/actions/runs/9899237786/job/27349382236)

特に問題なかったので自動作成された PR を merge したらなんと GitHub Actions workflow がコケた。んなあほな。
ログを見てみたら [`Install-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x) で response status code 999 を受け取ってるようだった。
銀河鉄道かよ。
わたしの浅い経験では 999 を見たのはこのときが初めてだ。

```plaintext
Prepare all required actions
Getting action download info
Download action repository 'actions/setup-dotnet@v4' (SHA:6bd8b7f7774af54e05809fcc5431931b3eb1ddee)
Download action repository 'codecov/codecov-action@v4' (SHA:e28ff129e5465c2c0dcc6f003fc735cb6ae0c673)
Run ./.github/actions/test
Run actions/setup-dotnet@v4
/home/runner/work/_actions/actions/setup-dotnet/v4/externals/install-dotnet.sh --skip-non-versioned-files --runtime dotnet --channel LTS
dotnet-install: Attempting to download using aka.ms link https://dotnetcli.azureedge.net/dotnet/Runtime/8.0.7/dotnet-runtime-8.0.7-linux-x64.tar.gz
dotnet-install: Remote file https://dotnetcli.azureedge.net/dotnet/Runtime/8.0.7/dotnet-runtime-8.0.7-linux-x64.tar.gz size is 31272597 bytes.
dotnet-install: Extracting archive from https://dotnetcli.azureedge.net/dotnet/Runtime/8.0.7/dotnet-runtime-8.0.7-linux-x64.tar.gz
dotnet-install: Downloaded file size is 31272597 bytes.
dotnet-install: The remote and local file sizes are equal.
dotnet-install: Installed version is 8.0.7
dotnet-install: Adding to current process PATH: `/usr/share/dotnet`. Note: This change will be visible only when sourcing script.
dotnet-install: Note that the script does not resolve dependencies during installation.
dotnet-install: To check the list of dependencies, go to https://learn.microsoft.com/dotnet/core/install, select your operating system and check the "Dependencies" section.
dotnet-install: Installation finished successfully.
/home/runner/work/_actions/actions/setup-dotnet/v4/externals/install-dotnet.sh --skip-non-versioned-files --version 8.0.100
dotnet-install: Attempting to download using primary link https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100/dotnet-sdk-8.0.100-linux-x64.tar.gz
dotnet-install: Remote file https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100/dotnet-sdk-8.0.100-linux-x64.tar.gz size is 214395068 bytes.
dotnet-install: Extracting archive from https://dotnetcli.azureedge.net/dotnet/Sdk/8.0.100/dotnet-sdk-8.0.100-linux-x64.tar.gz
dotnet-install: Downloaded file size is 214395068 bytes.
dotnet-install: The remote and local file sizes are equal.
dotnet-install: Installed version is 8.0.100
dotnet-install: Adding to current process PATH: `/usr/share/dotnet`. Note: This change will be visible only when sourcing script.
dotnet-install: Note that the script does not resolve dependencies during installation.
dotnet-install: To check the list of dependencies, go to https://learn.microsoft.com/dotnet/core/install, select your operating system and check the "Dependencies" section.
dotnet-install: Installation finished successfully.
Run Set-PSResourceRepository PSGallery -Trusted
Install-PSResource: /home/runner/work/_temp/03b13c40-34d5-4152-b471-63d55ca095a9.ps1:3
Line |
   3 |  Install-PSResource Psake,Pester,PSScriptAnalyzer -Quiet -Reinstall -S …
     |  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | 'Response status code does not indicate success: 999.' Request sent:
     | 'https://www.powershellgallery.com/api/v2/FindPackagesById()?id='Psake'&$inlinecount=allpages&$filter=IsLatestVersion%20and%20Id%20eq%20'Psake''
Error: Process completed with exit code 1.
```

999 について調べてみても「一般的じゃない response code」くらいしか情報が見つからない(当然)。
999 は未定義のため、一般的には内部 status code が漏れ出てるパターンぽい。
過去に LinkedIn をログインせず crawling しようとしたらが 999 返ったことがあるらしい。
[python - 999 response when trying to crawl LinkedIn with Scrapy - Stack Overflow](https://stackoverflow.com/questions/42910269/999-response-when-trying-to-crawl-linkedin-with-scrapy/45407138#45407138)
これは GitHub Actions だけじゃなさそうと思い、ローカルで試してみたら同じだった。

```powershell
> Install-PSResource Psake
Install-PSResource: 'Response status code does not indicate success: 999.' Request sent: 'https://www.powershellgallery.com/api/v2/FindPackagesById()?id='Psake'&$inlinecount=allpages&$filter=IsLatestVersion%20and%20Id%20eq%20'Psake''
Install-PSResource: Package(s) 'Psake' could not be installed from repository 'PSGallery'.
```

`Install-PSResource` だけでなく [`Find-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/find-psresource?view=powershellget-3.x) も挙動が変だった。
`Find-PSResource -Name pocof` で検索しても結果を取得できないけど `Find-PSResource -Name poco*` だと取得できたり。なんでだよ。

これ障害かなんかやなと漸くここで気づき [PowerShell Gallery](https://www.powershellgallery.com/) を見に行ったら、ページトップに ↓ の通りの記載があった。

> Individual package statistics are temporarily unavailable. More info: https://aka.ms/psgallerystatus

https://aka.ms/psgallerystatus が ↓ に繋がってる

[PowerShellGallery/psgallery_status.md at master · PowerShell/PowerShellGallery](https://github.com/PowerShell/PowerShellGallery/blob/master/psgallery_status.md#july-11th-2024-individual-package-statistics-will-be-temporarily-unavailable)

> ### July 11th, 2024 Individual package statistics will be temporarily unavailable.
>
> Individual package statistics will be temporarily unavailable while we are making infrastructure changes. There will be no loss of information, however statistics numbers will temporarily not update.
>
> **Status: Ongoing**

この文面からは PSResourceGet が影響を受けるような雰囲気は全く読み取れなかったのだけど、絶対これやろ。
2024-07-12 08:00 JST 時点でも 999 解消できなかったので、 PowerShell Gallery のインフラ変更中の間無理なんじゃないかなと不安になった。

2024-07-12 09:10 JST にはローカルで `Install-PSResource` が 999 することもなくなり、 `Find-PSResource` もちゃんと結果を返すようになった。
その後 GitHub Actions を再実行したらうまくいった。
きょうこの記事をしたためてる最中も、↓ ままやけど。

> July 11th, 2024 Individual package statistics will be temporarily unavailable.
> Status: Ongoing

いつだったか PowerShell Gallery の更新計画を [PowerShell の devblogs](https://devblogs.microsoft.com/) でみた気がするけど覚え違いかな？
調べてみても、最近の [PowerShell and OpenSSH team investments for 2024 - PowerShell Team](https://devblogs.microsoft.com/powershell/powershell-and-openssh-team-investments-for-2024/) で新しいタイプの PowerShell の repo に触れてる程度だった。
ただこう、なんやろ。もうちょっと計画的なメンテとかは開発者がわかりやすく把握できるようにならんのかなという気はする。
前から Markdown 直書きなのは知ってるけど、この更新を自動で受け取るのって PowerFighter(勝手に作った PowerShell ユーザの呼び名)のみんなは自前でシステム組んでるんかな？
気になる所あるが、次回に多様な記事を書いたときまでには進展があるといいな。
