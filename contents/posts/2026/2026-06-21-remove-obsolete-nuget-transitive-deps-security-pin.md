---
title: "不要になった NuGet の推移的依存関係のピン留めを消す"
tags: ["dotnet", "powershell"]
---

過去に [krymtkts/pocof](https://github.com/krymtkts/pocof) で依存してる [Microsoft.PowerShell.SDK](https://www.nuget.org/packages/microsoft.powershell.sdk/) だかなんだかの推移的依存関係(transitive dependency)に脆弱性があった。
回避方法として [`System.Security.Cryptography.Xml`](https://www.nuget.org/packages/System.Security.Cryptography.Xml/) をピン留めしてたのだけど、それを project から取り除くのを忘れてたので今回実施した。

正しいと思われる手順を知らなかったので、今回やり方を整理した。
使うのは以下のコマンド達だ。

- [dotnet package list command - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-list)
- [dotnet restore command - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore)
- [dotnet nuget why command - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-nuget-why)

まず解消しているかは以下で確認できる。
脆弱性を含んだ package が一覧されなければ OK 。

```powershell
dotnet package list  --vulnerable --include-transitive
# Restore complete (0.9s)
#
# Build succeeded in 1.0s
#
# The following sources were used:
#    https://api.nuget.org/v3/index.json
#    https://api.nuget.org/v3/index.json
#
# The given project `pocof.Benchmark` has no vulnerable packages given the current sources.
# The given project `pocof.Inspector` has no vulnerable packages given the current sources.
# The given project `pocof.Test` has no vulnerable packages given the current sources.
# The given project `pocof` has no vulnerable packages given the current sources.
```

次に project から推移的依存関係のピン留めを削除する。
今回のケースだと `Directory.Packages.props` から `System.Security.Cryptography.Xml` を取り除いた。

```diff
     <!-- NOTE: for creating mock PSObject. Pin the LTS version of PowerShell SDK. -->
     <PackageVersion Include="Microsoft.PowerShell.SDK" Version="7.4.17" />
-    <!-- NOTE: to address security vulnerabilities. -->
-    <PackageVersion Include="System.Security.Cryptography.Xml" Version="8.0.3" />

     <!-- NOTE: Benchmark dependencies. -->
     <PackageVersion Include="BenchmarkDotNet" Version="0.15.8" />
```

取り除いた後、依存関係を復元して再度脆弱性が検知されないか確認する。

```powershell
dotnet restore
# Restore complete (0.8s)
#
# Build succeeded in 1.0s
dotnet package list  --vulnerable --include-transitive
# Restore complete (0.6s)

# Build succeeded in 0.7s

# The following sources were used:
#    https://api.nuget.org/v3/index.json
#    https://api.nuget.org/v3/index.json

# The given project `pocof.Benchmark` has no vulnerable packages given the current sources.
# The given project `pocof.Inspector` has no vulnerable packages given the current sources.
# The given project `pocof.Test` has no vulnerable packages given the current sources.
# The given project `pocof` has no vulnerable packages given the current sources.
dotnet package list --include-transitive
# 長いので一部省略
# top-level package に が一覧されなければよい
#
# Project 'pocof.Test' has the following package references
#    [net9.0]:
#    Top-level Package               Requested       Resolved
#    > coverlet.collector            10.0.1          10.0.1
#    > Expecto                       11.0.0-alpha9   11.0.0-alpha9
#    > Expecto.FsCheck               11.0.0-alpha9   11.0.0-alpha9
#    > FsCheck                       3.3.3           3.3.3
#    > FSharp.Core                   10.1.301        10.1.301
#    > Ionide.Analyzers              0.15.0          0.15.0
#    > Microsoft.NET.Test.Sdk        18.6.0          18.6.0
#    > Microsoft.PowerShell.SDK      7.4.17          7.4.17
#    > YoloDev.Expecto.TestSdk       0.15.6          0.15.6
#
#    Transitive Package                                                Resolved
#    ...
#    > System.Security.Cryptography.Xml                                8.0.3
#    ...
```

最後に対象となった package 、今回で言うと `System.Security.Cryptography.Xml` の出どころを確認しておく。

```powershell
dotnet nuget why pocof.Test.fsproj System.Security.Cryptography.Xml
# Project 'pocof.Test' has the following dependency graph(s) for 'System.Security.Cryptography.Xml':

#   [net9.0]
#   └── Microsoft.PowerShell.SDK (v7.4.17)
#       ├── Microsoft.Windows.Compatibility (v8.0.28)
#       │   ├── System.Security.Cryptography.Xml (v8.0.3)
#       │   ├── System.ServiceModel.Duplex (v4.10.3)
#       │   │   ├── System.Private.ServiceModel (v4.10.3)
#       │   │   │   └── System.Security.Cryptography.Xml (v8.0.3)
#       │   │   └── System.ServiceModel.Primitives (v4.10.3)
#       │   │       └── System.Private.ServiceModel (v4.10.3)
#       │   │           └── System.Security.Cryptography.Xml (v8.0.3)
# 長いので省略
```

これで必要なくなった推移的依存関係のピン留め削除は完了。
ただコレだと手動で芸が無いのと、必要なときこそ毎度忘れてるだろうから、半自動化してみた。

pocof は [Central Package Management (CPM)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) なので `Directory.Packages.props` に直接的に必要な package が書かれている。
今回のような脆弱性回避のピン留めも含まれるので、コレを使えば良い。
なのでその内容と `dotnet package list` の結果を比較し、 top-level に出現しない package を削除可能とみなす。
ただそれだと万が一後で使う予定の package があっても検知されると面倒だ。
よって削除可能と判定する対象を絞る独自の metadata `SecurityPin` を `PackageVersion` に追加しておく。

こんな感じで `Directory.Packages.props` にピン留めしたい推移的依存関係を含める。

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
  <ItemGroup>
    <!-- 略 -->

    <!-- NOTE: to address security vulnerabilities. -->
    <PackageVersion Include="System.Security.Cryptography.Xml" Version="8.0.3" SecurityPin="true" />

    <!-- 略 -->
  </ItemGroup>
</Project>
```

そんで [psake](https://github.com/psake/psake) の task に以下を仕込んで [GitHub Actions workflow](https://docs.github.com/en/actions) で PR した時に不要なやつがいたら落とすという感じ。

```powershell
Task CheckUnusedSecurityPins {
    $securityPins = Get-Content Directory.Packages.props -Raw |
        ForEach-Object { ([xml]$_).Project.ItemGroup.PackageVersion } |
        Where-Object { $_.SecurityPin }
    if (-not $securityPins) {
        Write-Output 'No security pins found in Directory.Packages.props.'
        return
    }

    $topLevelPackageNames = dotnet package list --format json |
        ConvertFrom-Json -Depth 10 |
        ForEach-Object { $_.projects.frameworks.topLevelPackages.id } |
        ForEach-Object -Begin { $set = [System.Collections.Generic.HashSet[string]]::new() } -Process {
            $set.Add($_) | Out-Null
        } -End { , $set }

    $unused = $securityPins | Where-Object { -not $topLevelPackageNames.Contains($_.Include) } |
        ForEach-Object {
            [PSCustomObject]@{
                Package = $_.Include
                Version = $_.Version
                Status = 'PossiblyUnused'
            }
        }
    if ($unused) {
        $unused | Format-Table
        throw 'Found possibly unused security pins in Directory.Packages.props. Please check the above list and remove the pins if the packages are no longer used.'
    }
}
```

とりま今はもう使う対象がないので実運用でのテストができないが、当面これでやってみる。
F# の他の自分の project に入れてみても良いかも知れんな。
ならまた action を作ればいいか、という気もするが CPM 以外のことも考えるとなんか面倒で億劫だ。
なので、一旦放置かやるとしても CPM 専用の最小限のやつかな。
