---
title: "F# で command-line predictor を書いてる Part 16"
subtitle: "0.11.0 と .NET 11"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

いろいろリリースした。

- [krymtkts/pocof](https://github.com/krymtkts/pocof) の [v0.25.0](https://www.powershellgallery.com/packages/pocof/0.25.0)
- [krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.11.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.11.0)
- [krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の [v0.0.4](https://www.powershellgallery.com/packages/PSGameOfLife/0.0.4)

.NET 11 が出たので test や benchmark project を .NET 11 にした。
配布物としては F# 11 の `FSharp.Core` dll が同梱されるようになってる。
PowerShell の同じ session で古い `FSharp.Core` を先に使っていると、新しい assembly の要求を満たせず module の読み込みに失敗することがある。
更新後は PowerShell を再起動して使う必要がある。

全体的に単に bump しただけなのだけど、 SnippetPredictor では面白い気づきを得たのでついでに書いておく。

`FunctionsToDefine` をずっと実装してたけど、 SnippetPredictor には要らなかったみたい。
具体的な使い方は分からないまま、 `ICommandPredictor` の実装の中で以下のように空の Dictionary を返していた。

```fsharp
member __.FunctionsToDefine: Dictionary<string, string> = Dictionary<string, string>()
```

SDK 11 で build したら、この値が空の Dictionary であることを確認していた 2 つのテストが失敗した。
interface 経由では `null` が返っていた。

そもそも [`ICommandPredictor`](https://github.com/PowerShell/PowerShell/blob/v7.7.0/src/System.Management.Automation/engine/Subsystem/PredictionSubsystem/ICommandPredictor.cs) には以下の default implementation があった。

```csharp
Dictionary<string, string>? ISubsystem.FunctionsToDefine => null;
```

`FunctionsToDefine` の宣言元は親の [`ISubsystem`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.isubsystem?view=powershellsdk-7.4.0) で、 `ICommandPredictor` がその既定実装を提供している。
F# 11 では、このように派生 interface が親の member に提供する [inherited default implementations](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/interfaces#inherited-default-implementations) を使えるようになった。
今回の書き方では、この変更で interface 経由の呼び出し先も変わっていたみたい。

旧 compiler は、上の独自実装を [`ISubsystem.FunctionsToDefine`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.isubsystem.functionstodefine?view=powershellsdk-7.4.0#system-management-automation-subsystem-isubsystem-functionstodefine) の実装として出力していた。
なので、テストから呼ぶと独自実装の空 Dictionary が返っていた。
新 compiler では、独自実装は `ICommandPredictor.FunctionsToDefine` という別の member として出力されていた。
`ISubsystem.FunctionsToDefine` の呼び出し先は `ICommandPredictor` が提供する既定実装になり、 `null` が返る。
そのため、空 Dictionary が返る前提のテストが失敗したという流れみたい。

どっからこの余計な実装が入ったのか調べたら、 Microsoft Learn の Feedback Provider の sample に同じ member があった。
[command-line predictor の sample](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor) になはく、 [Feedback Provider の sample](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-feedback-provider#step-4---add-class-members-and-define-the-constructor) にはある。
Feedback Provider の sample は [`IFeedbackProvider`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.feedback.ifeedbackprovider?view=powershellsdk-7.4.0) と `ICommandPredictor` を両方実装してる。
両方の interface が `ISubsystem.FunctionsToDefine` の既定実装を持つ場合、 C# では [競合を明示的に解消する必要がある](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/interfaces#most-specific-implementation)。
sample の明示的な実装はその競合を解消する形になっているけど、 `ICommandPredictor` だけを実装する SnippetPredictor には全く不要なのがわかった。
今回余計な実装とその値を確認するだけの assertion を削除した。 [#219](https://github.com/krymtkts/SnippetPredictor/pull/219)

そもそも `FunctionsToDefine` は何なのか知りたくなったので、調べた。
[`ISubsystem`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.isubsystem?view=powershellsdk-7.4.0) の member で global scope に定義する関数の一覧を返すと書かれている。
Dictionary の key が関数名で、 value が関数の script という説明もある。
ただ、どんな場面で何を入れて使うのか、具体的な利用手順や例は見つけられなかった。
Feedback Provider の sample も `null` を返しているだけで、関数を実際に定義する例ではない。
[subsystem 導入時の PR](https://github.com/PowerShell/PowerShell/pull/13186) の公開議論も確認したが、 predictor での具体的な利用方法には言及されてない様子。

PowerShell 7.7 の[登録処理を辿った](https://github.com/PowerShell/PowerShell/blob/5c95ed61735c05eb99543aabf196529b5f267602/src/System.Management.Automation/engine/Subsystem/SubsystemManager.cs#L240-L253)ところ、関数を global scope に追加するという設計のコメントが残っている。
ただコメントがあるだけで、実際に関数を登録するコードはない。
新しい runspace にも関数を追加する想定まで書いてあるけど、処理自体はまだ何もない。
最新の PowerShell でも、 subsystem 本体の登録処理は未実装で、想定用途の説明はあるけど何も機能を提供していないのがわかった。

.NET 11/F# 11 への更新が目的だったけど、その過程でおまじないコードが消せて、その正体もわかったのは良い収穫だった。
