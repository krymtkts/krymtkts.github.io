---
title: "F# の PowerShell Module の Template を作りたい Part 2"
tags: ["fsharp", "powershell", "dotnet"]
---

ようやく重い腰を上げて PowerShell Module を F# で書くときの template を作り始めた。
repository name は [krymtkts/FsPowerShellTemplate](https://github.com/krymtkts/FsPowerShellTemplate) にした。
まだ完成してないから template repository にはしてない。

命名はちょっと気を使った。
先頭に `FSharp` とか `PowerSHell` は持ってくるべきではないだろうし、個人の識別子が入るのも微妙だ。
それでいて将来的に PowerShell Module 以外が含まれる可能性も考慮したうえで、いつか NuGet で template 配布するというのも考えて、今の形に。
略称の `Fs` なら多少カジュアル感出るし、多分いい落とし所なはず。
`Scaffold` も考えたが、 NuGet では `Template` が一般的なのを考えたらそっちの方が見つけやすくなる。
NuGet の慣習的な dot 区切りだけはやめたが、それは repository name と合わせたくての妥協点だ。でも気が変わったら変える可能性ある。

ひとまず template の設計としては、 Cmdlet, Command-line predictor, Feedback provider を 1 つの project に同梱する。
そしてこれらの連携が真骨頂なので、それの sample として十分な例を示しておけたらなと考えている。まだ作ってないけど。
これらを無効にする術としては、ひとまずはコードを消すことで実現したらいいかなと考えている。
そのため Cmdlet, Command-line predictor, Feedback provider が別ファイルで実装される。
今はやってないけど連携のコアの部分も別ファイルで作る。
そうすれば Feedback provider や Command-line predictor が不要ならファイルごと消せる感じということになる。
これで自分が新しく作るときもそのテンプレに従って書くだけなのでかなり楽なのでは？と期待している。

と作り始めたもののなんだか花粉症かなと思ってたのが風邪っぽい？不調に変わってきたそんなに進んでない。
でも Cmdlet, Command-line predictor, Feedback provider の作成まではやった。
例にしてるのは何度も読んでる以下の文書。

- [How to create a command-line predictor - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor)
- [How to create a feedback provider - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-feedback-provider)

Feedback provider は初めて作ったのだが、作ってみてこいつ単体の interface は simple なもんだというのに気づいた。
上記文書の sample だと 1 つの class で Command-line predictor と Feedback provider の両方を実装してるのでゴチャついて見えるだけ。
まだ Feedback provider の真髄である Command-line predictor や Cmdlet との連携を全くやってないので simple になっていると言うのもけど。
いま template に実装したのは何とも連携してない Feedback provider で、それを参考として貼ると以下の通り。

[FsPowerShellTemplate/src/SampleModule/FeedbackProvider.fs at 39cb16631a6cbc63812ba66ebff1a5b55290ed8e · krymtkts/FsPowerShellTemplate](https://github.com/krymtkts/FsPowerShellTemplate/blob/39cb16631a6cbc63812ba66ebff1a5b55290ed8e/src/SampleModule/FeedbackProvider.fs)

```fsharp
namespace SampleModule

open System
open System.Collections
open System.Management.Automation.Subsystem.Feedback

type GreetingFeedbackProvider(guid: string) =
    let id = Guid.Parse(guid)

    [<Literal>]
    let name = "Greeting"

    [<Literal>]
    let description =
        "A feedback provider that handles feedback for the greeting predictor."

    interface IFeedbackProvider with
        member __.Id = id
        member __.Name = name
        member __.Description = description
        member __.FunctionsToDefine = null
        member __.Trigger: FeedbackTrigger = FeedbackTrigger.Success

        member __.GetFeedback(context: FeedbackContext, token: Threading.CancellationToken) : FeedbackItem | null =
            let header = "Greeting Feedback"

            FeedbackItem(
                header,
                [ "Was the greeting helpful?"; "Did you like the style of the greeting?" ]
                |> Generic.List<string>,
                "Feedback for the Greeting Predictor",
                FeedbackDisplayLayout.Portrait
            )
```

文書に載ってる sample でもわかるように、 [`GetFeedback`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.feedback.ifeedbackprovider.getfeedback) が Feedback provider のコア。
この中で [`FeedbackContext`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.feedback.feedbackcontext) の結果に応じて処理を分ける等するのが期待されるので、とっ散らからないよう別の型に処理をまとめるなどが必要だろうな。

とりままだ連携機能を作ったワケではないので完全ではないけど、 Feedback provider を作った。
これで作ったことがなかったものは 1 つクリアしたわけだ。
作ってみていて、こういう Feedback provider の使い方はアリかもしれんな、とかアイデアの片鱗も掴めてた気がする。
実際に実装してみるというのはやっぱり重要やな。

とりま連携部分とかの残りのタスクを進めるつもり。
そんで今の一番の悩みは task runner を何にするか。
長らく [psake/psake](https://github.com/psake/psake) を使ってるが [nightroman/Invoke-Build](https://github.com/nightroman/Invoke-Build) も使ってみたい。
psake は名前が最高だが parameter を渡すのはちょっと面倒なのがなあ。
あと [fsprojects/FAKE](https://github.com/fsprojects/FAKE) を使うという手もある。
でも fsx から PowerShell 呼ぶってのもどうなんかなあという気がするよな。
楽しい悩みや。
