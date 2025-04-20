---
title: "F# で Cmdlet を書いてる pt.54"
subtitle: ".NET 9"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

.NET 9 に更新してみた。
結論から言うと 2024-11-24 時点ではなかなか厳しいなという感触だった。
ということで一旦寝かせて状況を見る。

---

詰まってるのは以下の課題。

- `dotnet fsharplint lint` が遂にどうにも動かなくなった。
- .NET 9 SDK の bug の影響で `fantomas` がコケる
  - [dotnet list package --vulnerable throws ArgumentException: '' is not a valid version string [.NET 9] · Issue #44838 · dotnet/sdk](https://github.com/dotnet/sdk/issues/44838)
- Ionide は .NET 9 未対応
  - [Update FSAC used in Ionide to the 9.0.100 version · Issue #2048 · ionide/ionide-vscode-fsharp](https://github.com/ionide/ionide-vscode-fsharp/issues/2048)

もともと fsharplint のやつは [Execution fails when using dotnet 7.x or 8.x to target net6.0 project · Issue #687 · fsprojects/FSharpLint](https://github.com/fsprojects/FSharpLint/issues/687) という課題にぶち当たってて、 0.24.2 を使えば回避できるのでそうしてた。
ただ .NET 9 になってエラーが出るようになって遂にどうにもならなくなった。

```powershell
> dotnet fsharplint lint pocof.sln
Lint failed to parse files. Failed with: Aborted type check.
Aborted type check.
Aborted type check.
(略)
```

最新の [0.24.2](https://www.nuget.org/packages/dotnet-fsharplint/0.24.2) とか [0.24.3 の preview](https://www.nuget.org/packages/dotnet-fsharplint/0.24.3--date20240609-0921.git-873d145) に変えてみても issue と同じ既知のエラーが出るのみ。
このエラーは fsharplint が .NET 6 を target framework にしてて、利用してるプロジェクトがそれより高い .NET 8 とかを利用してることに起因するらしいのだけど、よくわかってない。
プロジェクトに dotnet tool を入れてるとそのプロジェクトの .NET SDK で動く仕組みに問題があるんじゃないのみたいな話もあるようだけど、恐らくその仕組み自体は変わらんだろうしツールの提供側で前方・後方互換性を維持するしかないような感じはするな。

.NET SDK と Ionide ののやつは時が経てばコミュニティが解決してくれそうやけど、 fsharplint のはどうやろな。 .NET 6 は EOL したし target framework が変わればなにか改善できるのかもしらんけど。他力本願じゃなくコントリして直せよって話かも知れんが。

話は変わって言語機能的なところでいうと、 pocof は以下を使えたので利用してみた。
良さげ。

- [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9#nullable-reference-types)
- [Partial active patterns can return `bool` instead of `unit option`](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9#partial-active-patterns-can-return-bool-instead-of-unit-option)

ただ先述の通り、まだ Ionide が対応してない。ビルドは成功するけど VS Code 上ではエラーになる状態で、これはちょっと開発しにくい。

続く。
