---
title: "F# で Cmdlet を書いてる pt.65"
subtitle: ".NET SDK のエラーに引っかかった"
tags: ["fsharp", "powershell", "dotnet"]
---

多分次の release で解消されるであろう .NET SDK の issue を備忘のため記録しておく。

[#329](https://github.com/krymtkts/pocof/pull/329) の過程で発見した。

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発を始めて [krymtkts/pocof](https://github.com/krymtkts/pocof) の開発は鈍化してるが、依存関係の更新は [Dependabot version updates](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/about-dependabot-version-updates) を使ってるのもあって、大体毎週何かしらの更新があるから追随している。
ただ [FsUnit.xUnit](https://www.nuget.org/packages/FsUnit.xUnit/) が v7 で [xunit.v3](https://www.nuget.org/packages/xunit.v3/) を使う様になり、 pocof で合わせて利用している [FsCheck.XUnit](https://www.nuget.org/packages/FsCheck.Xunit/) が未だ xUnit.v3 に対応してないため、更新は据え置きの状態になってる。
その結果 Dependabot の pull request をそのまま受け入れられない状態が続いたので、昨日そのへんの調整を施した。
ただ、その施しはうまくいってるはずなのだけど [GitHub Actions](https://docs.github.com/en/actions) の macOS の job だけ絶対に成功しなくなった。

こういうとき、 GitHub Actions の失敗した job の log の末尾だけを見がちなのだけど、それだけだと何が原因かわからなかった。
気を取り直して log 全体を見渡すと、先頭の方で他の platform と macOS で出力の違いを見つけた。

Windows と Ubuntu ではこう ↓。

```plaintext
psake version 4.9.1
Copyright (c) 2010-2018 James Kovacs & Contributors

Module: pocof ver0.19.0 root=/home/runner/work/pocof/pocof/src/pocof/ publish=/home/runner/work/pocof/pocof/publish/pocof/
Executing Init
Init is running!
Tool 'dotnet-fsharplint' (version '0.21.2') was restored. Available commands: dotnet-fsharplint
Tool 'fantomas' (version '7.0.1') was restored. Available commands: fantomas
```

macOS ではこう ↓。

```plaintext
psake version 4.9.1
Copyright (c) 2010-2018 James Kovacs & Contributors

Module: pocof ver0.19.0 root=/Users/runner/work/pocof/pocof/src/pocof/ publish=/Users/runner/work/pocof/pocof/publish/pocof/
Executing Init
Init is running!

Welcome to .NET 9.0!
---------------------
SDK Version: 9.0.200

Telemetry
---------
The .NET tools collect usage data in order to help us improve your experience. It is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.

Read more about .NET CLI Tools telemetry: https://aka.ms/dotnet-cli-telemetry

----------------
Installed an ASP.NET Core HTTPS development certificate.
To trust the certificate, run 'dotnet dev-certs https --trust'
Learn about HTTPS: https://aka.ms/dotnet-https

----------------
Write your first app: https://aka.ms/dotnet-hello-world
Find out what's new: https://aka.ms/dotnet-whats-new
Explore documentation: https://aka.ms/dotnet-docs
Report issues and find source on GitHub: https://github.com/dotnet/core
Use 'dotnet --help' to see available commands or visit: https://aka.ms/dotnet-cli
--------------------------------------------------------------------------------------
Failed to validate package signing.

Verifying dotnet-fsharplint.0.21.2

Signature type: Repository
  Subject Name: CN=NuGet.org Repository by Microsoft, O=NuGet.org Repository by Microsoft, L=Redmond, S=Washington, C=US
  SHA256 hash: 5A2901D6ADA3D18260B9C6DFE2133C95D74B9EEF6AE0E5DC334C8454D1477DF4
  Valid from: 2/16/2021 12:00:00 AM to 5/15/2024 11:59:59 PM

warn : NU3018: The repository primary signature found a chain building issue: RevocationStatusUnknown: An incomplete certificate revocation check occurred.
error: NU3037: The repository primary signature validity period has expired.
error: NU3028: The repository primary signature's timestamp found a chain building issue: ExplicitDistrust: The trust setting for this policy was set to Deny.

Package signature validation failed.
```

まず Windows & Ubuntu と macOS では dotnet CLI の version が 9.0.101 と 9.0.200 で違ってる。
ただし当時の `global.json` が以下なので、この dotnet CLI の version の差異は想定内。

```json
{
  "sdk": {
    "rollForward": "latestMinor",
    "version": "9.0.101"
  }
}
```

次に macOS では `NU3018` `NU3037` `NU3028` が発生しているのがわかるので、こいつで調べると以下にたどり着いた。

[dotnet tool restore started failing on macOS with NU3037 and NU3028 errors after 11th February · Issue #46857 · dotnet/sdk](https://github.com/dotnet/sdk/issues/46857)

[これ](https://github.com/dotnet/sdk/issues/46857#issuecomment-2683545963) と [これ](https://github.com/dotnet/sdk/issues/46857#issuecomment-2683556715) を見ればわかるが、 macOS では NuGet package の署名検証がコケるから default で無効らしい。
公式の文書は [NuGet signed-package verification - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification#macos) か。

でも 9.0.200 でデフォルト有効になってしまう変更があったらしい。これは [`DOTNET_NUGET_SIGNATURE_VERIFICATION = false` でも回避できない](https://github.com/dotnet/sdk/issues/46857#issuecomment-2683555678)とか。
issue の Milestone は 9.0.3xx なのですぐには直って落ちてこないかな。

まとめると、 9.0.3xx で直す予定みたいだが、現状の 9.0.200 だと回避できない。
つまり workaround として有効な方法は、 `9.0.103` に固定することのみと思われるので、今回はそのようにして回避した。

```json
{
  // TODO: pin sdk version to 9.0.103 to prevent `dotnet tool restore` failure on MacOS due to `NU3018`, `NU3037` and `NU3028`.
  "sdk": {
    "rollForward": "disable",
    "version": "9.0.103"
  }
}
```

こういうトラシューは AI さんにも未だ解けないっぽいので当てにならん。
AI さんにこれをとかそうとしたらどこをどう調べるべきかをレクチュアせねばならず、自分でやるのより面倒だった。

とりあえず、その場しのぎだが CI を動かせるように直せた。
調べてみて、現象の報告から 2 週間程度仕方ってないというのもあるかも知れないが、大して知られてない？ような感じ。
これは macOS で dotnet 使う人がめちゃくちゃ少ないってことなのでは...

---

因みに、この過程で `global.json` に comment を書けることを初めて知った。要は JSON with Comments 。
[global.json overview - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json#comments-in-globaljson)
多分時代の流れで `System.Text.Json` を使ってるので deserialize のときは comment が skip されてる。
なので serialize するのは()`dotnet new globaljson` のときくらいしかないと思うが)消えるだろう。

tool manifest `dotnet-tools.json` でも同様かと思ったが、こちらはこちらで理由があって対応してないみたい。
JSON はコメントの振る舞いに関して定義がないからやらないって感じか。

- [Enable comments for tool manifest json · Issue #10384 · dotnet/sdk](https://github.com/dotnet/sdk/issues/10384)
- [Support of comments in jsondocument · Issue #30316 · dotnet/runtime](https://github.com/dotnet/runtime/issues/30316)
- [[Feature Request] Support jsonc for dotnet tools manifest · Issue #16043 · dotnet/sdk](https://github.com/dotnet/sdk/issues/16043)
