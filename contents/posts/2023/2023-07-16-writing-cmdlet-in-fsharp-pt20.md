---
title: "F# でコマンドレットを書いてる pt.20"
tags: ["fsharp","powershell"]
---

久しぶりに [pocof](https://github.com/krymtkts/pocof) の開発をした。
ゆーても .NET 6 → .NET 7 と、 PowerShellGet → PSResourceGet だけ。

### .NET 6 → .NET 7 [#53](https://github.com/krymtkts/pocof/pull/53)

ASP.NET じゃないけどこれがやることわかりやすくまとまってたので参考にした。
[Migrate from ASP.NET Core 6.0 to 7.0 | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/migration/60-70?view=aspnetcore-7.0&tabs=visual-studio-code)

SDK を `global.json` で固定するようにした。

```powershell
dotnet new globaljson --sdk-version 7.0.306 --roll-forward latestFeature
```

その後 `*.fsproj` の `TargetFramework` を net6.0 → net7.0 にする。
`global.json` の作成に先んじて `TargetFramework` を変更してビルドするとうまくいかなかったが、変えるとすんなりいった。キャッシュの影響？

[NuGet Gallery | Microsoft.PowerShell.SDK 7.3.6](https://www.nuget.org/packages/Microsoft.PowerShell.SDK/7.3.6)
を見て .NET 7 と互換性がある Microsoft.PowerShell.SDK に変える。
`7.2.4` → `7.3.6` にした。

これでビルドが通るようになる。

ビルド後に `Regex` 周りでエラーが出るようになった。
`Regex.IsMatch`メソッドで型推論できなくなった箇所があったので、型注釈して通るようにする。
`IsMatch(ReadOnlySpan<Char>)` が .NET 7 から増えたっぽいのでこの影響かなあ。
[Regex.IsMatch Method (System.Text.RegularExpressions) | Microsoft Learn](<https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex.ismatch?view=net-7.0#system-text-regularexpressions-regex-ismatch(system-readonlyspan((system-char)))>)

また例外の文面も変わったらしくて、 1 つのテストケースで期待値を新しいものに合わせた。

GitHub Actions の job で `actions/setup-dotnet@v3` に指定してるバージョンも `6.0.x` → `7.0.x` に変える。

この際 act 使った GitHub Actions の workflow テスト中に Docker が死んでしまった。
最近 disk 容量少なくなってて、 Docker image が pull されたタイミングで枯渇したの原因(一時的に残 100KB くらいになってた。やば)。

解消するために以下を参考にしたが、 dockerd の再起動だけでは解決しなかった。
[dockerfile - Docker error with read-only file system unknown - Stack Overflow](https://stackoverflow.com/questions/68218291/docker-error-with-read-only-file-system-unknown/70071216#70071216)

結果的に空き容量確保後に PC 再起動したら直った。

これにて .NET 7 化は完了。

### PowerShellGet → PSResourceGet [#54](https://github.com/krymtkts/pocof/pull/54)

変えたのは `Publish-Module` → `Publish-PSResource` だけ。
[前回の日記](/posts/2023-07-09-migrate-dev-environment.html) にも追記したが、 `Get-Module` は `Microsoft.PowerShell.Core` の持ち物だったので変える必要なかった。

`WhatIf` までの確認に留めている。
というのも、結構パラメータが変わっておりホンマにうまく動くんかこれ...というのがあるため。
[PSResourceGet の Issues](https://github.com/PowerShell/PSResourceGet/issues?q=is%3Aissue+is%3Aopen+Publish-PSResource) 見て `Publish-PSResource` の状況をつかもうとしてるがまだまだあんまわからない。
それに `Publish-PSResource` の前例を検索してもほぼない。

既存モジュールでいきなりやるのはちょい不安。やるならまずテスト用の module 作ってそれで試すが吉とみた。
が、作るのめんどくせええええ、というのは否めない。

### おわり

[昔の日記](/posts/2022-11-27-writing-cmdlet-in-fsharp-pt8.html) で書いてた platyPS の prerelease 使うとエラーになる件忘れていてまた引っかかったが、久しぶりの pocof 開発なんとかできてよかった。
