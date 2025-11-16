---
title: "F# で Cmdlet を書いてる pt.81"
subtitle: "Central Package Management と .NET 10"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。
開発というかメンテ。

まずは [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) を導入した話。

ちょっと前に .NET の [Dependabot version updates](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/about-dependabot-version-updates) の挙動が変わって壊れたとき、 `dependabot.yml` を書き換えた内容が間違ってたみたい。
それが原因で一部の package が更新されない状態が続いてたのに気付いた。
それは pocof に限った話じゃないのだけで、自作の PowerShell cmdlet の repository 全体的に直した。 [#377](https://github.com/krymtkts/pocof/pull/377)

それで期待通り Dependabot が働き始めたのだけど、 pocof のような 1 solution に複数(pocof は 4) project ある package 管理が面倒に感じた。
いい機会なので Central Package Management(CPM) を導入してみた。 [#382](https://github.com/krymtkts/pocof/pull/382)

もともと [`Directory.Build.props`](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=vs-2022#directorybuildprops-and-directorybuildtargets) は [Ionide.Analyzer](https://ionide.io/ionide-analyzers/) を全 project へ導入するのに使ってた。
CPM ではこれとは別に `Directory.Packages.props` を置く。
そこで `<PackageVersion>` を定義して package ごとの version を集中管理する。
配下の各 project では [`PackageReference`](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files) に `Version` 属性を指定せず、 `Directory.Packages.props` で集中管理する。
これにより複数 project で参照される同一 package も同じ version を使うので手間が省けるってことみたい。

CPM を使うと、これまで project の管理に入れてなかった F# の package [`FSharp.Core`](https://www.nuget.org/packages/fsharp.core) も明示的に入れる必要があり、管理が厳密化された。
また [`CentralPackageTransitivePinningEnabled=true`](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#transitive-pinning) にしてる。
これは transitive dependencies に同じ package がいた場合も強制するための設定。
他に [`CentralPackageVersionOverrideEnabled`](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#overriding-package-versions) というのもあって、これは中央管理してる version の上書きを許可するかを指定できる。
pocof は指定忘れたので既定値で動いてるはず(既定値が `true` `false` どちらかは文書に書いてなかった)。
ガチガチ管理を進めるなら明示的に `false` が良さそう。
これは後ほど変更しておきたい。

CPM だと package 更新は手で直すしかないのかなと気になって試してみた。
以下の通り dotnet CLI で CPM でもうまく `Directory.Packages.props` を更新できた。
あと地味に .NET 10 から sub command 体系が変わってるのをここで知った。
[What's new in the SDK and tooling for .NET 10 | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/sdk#more-consistent-command-order)

```powershell
# .NET 9 まで(.NET 10 でも動く)
dotnet add ./src/pocof package FSharp.Core --version 10.0.100
# .NET 10 から
dotnet package add FSharp.Core --version 10.0.100 --project ./src/pocof
```

ただ `Directory.Packages.props` に体裁を整えるための改行とか入れてると全部吹っ飛ぶので、まあ考えどころやな。

CPM 導入により package version がひとつのファイルにまとまっただけだが、結構 package の見通しが良くなったと気がするので、気に入っている。

次は .NET 10 に上げた話。

毎年恒例の最新 [.NET 10 が出た](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)ので、 pocof は [#389](https://github.com/krymtkts/pocof/pull/389) で対応した。
やることは決まりきってるのでそんなに困ることもないが、いつもと少し手順を変えている。
今回はいつも使ってる [Chocolatey](https://chocolatey.org/install) では [.NET 10 SDK](https://community.chocolatey.org/packages/dotnet-sdk/10.0.100) がまだ承認されてなかったので、 [winget](https://github.com/microsoft/winget-cli) を使うことにした。
いま自分の PC の package 管理で winget を使ってるのは [OpenSSH](https://github.com/microsoft/winget-pkgs/tree/452a15ba1a4d61db861fa381487d97a22127d164/manifests/m/Microsoft/OpenSSH/Preview) だけなので、使い方を忘れるしメモっておく。
以下のコマンドで .NET 10 SDK を install した。

```powershell
# これだと確認 prompt が面倒
winget install --id Microsoft.DotNet.SDK.10
# これで完全自動化できる(管理者権限で実行すると)
winget install --id Microsoft.DotNet.SDK.10 --silent --accept-package-agreements --accept-source-agreements --exact
```

コメントのとおりだが winget はデフォで確認プロンプトが面倒なのでゴテゴテと options をつけないといけないのが少し残念。
Chocolatey なら `choco upgrade dotnet -y` やからな。

あとは手で SDK version を書き換える感じだけど、今回は PowerShell で簡単に [`global.json`](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) を変更できる関数を作った(初版 AI に作らせて)。

```powershell
function Set-DotnetGlobalJson {
    [CmdletBinding()]
    param(
        # Path to global.json (default: ./global.json)
        [Parameter(Position = 0)]
        [Alias('PSPath')]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path = 'global.json',
        # Target major version (e.g. 10, 9)
        [int]
        $Major = 10,
        # rollForward value to write
        [ValidateSet('disable', 'latestPatch', 'minor', 'latestMinor', 'major', 'latestMajor', 'latestFeature')]
        [string]
        $RollForward = 'latestFeature'
    )

    function Get-LatestDotnetSdkVersionForMajor {
        param(
            [int]$Major
        )

        $sdks = dotnet --list-sdks 2>$null
        if (-not $sdks) {
            throw 'No dotnet SDKs found. Make sure dotnet is on PATH.'
        }

        $versions = $sdks |
            ForEach-Object {
                # "10.0.100 [C:\...]" → "10.0.100"
                ($_ -split '\s+')[0]
            } |
            Where-Object {
                $_ -match '^\d+\.\d+\.\d+' -and $_.StartsWith("$Major.")
            } |
            ForEach-Object {
                [version]$_
            }

        if (-not $versions) {
            throw "No .NET $Major SDKs found. Please install .NET $Major SDK first."
        }

        ($versions | Sort-Object -Descending | Select-Object -First 1).ToString()
    }

    try {
        $version = Get-LatestDotnetSdkVersionForMajor -Major $Major
        Write-Host "Detected latest .NET $Major SDK: $version"

        $jsonObject = @{
            sdk = @{
                version = $version
                rollForward = $RollForward
            }
        }

        $json = $jsonObject | ConvertTo-Json -Depth 3

        $resolved = Resolve-Path -Path $Path -ErrorAction SilentlyContinue
        if ($null -eq $resolved) {
            $targetPath = Join-Path (Get-Location) $Path
            Write-Host "Creating new global.json at: $targetPath"
            $json | Set-Content -Encoding UTF8 $targetPath
        }
        else {
            Write-Host "Updating existing global.json at: $($resolved.Path)"
            $json | Set-Content -Encoding UTF8 $resolved.Path
        }

        Write-Host "global.json updated to use SDK $version (rollForward=$RollForward)." -ForegroundColor Green
    }
    catch {
        Write-Error $_
    }
}
```

この関数を作った動機は dotnet CLI に指定する full version をいちいち覚えてないから自動で設定したいというものだ。
`dotnet new globaljson --force --sdk-version 10` でも full version が入ってほしいがそうはいかない。
あと `dotnet new globaljson --force` で `global.json` の末尾改行が削れるのも好きじゃない。
ちょっと安全じゃないかもなという気はすれど、自前でやるのがいいかなと考えた。
これで `Set-DotnetGlobalJson -Major 10` のようにして簡単に `10.0.100` に変更できる。楽だ。
(SDK を事前に install 済みである必要があるが)

まだ pocof しか .NET SDK を更新してないから、他の repository でもちまちま更新していく予定。
その間 pocof は放置気味になるかも知れんが、できる限りぼちぼち進めたい。
Multi targeting にしたことで `netstandard2.0` と `net6.0` でパフォに差があるのかとか benchmark を見てないから測定するとか。
速くなってるなら、ここらで次の version として公開するのもいいしね。
Multi targeting した初回は怖いので alpha が妥当かも知れん。
