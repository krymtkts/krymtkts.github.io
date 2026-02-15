---
title: "F# の PowerShell Module の Template を作りたい Part 3"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/FsPowerShellTemplate](https://github.com/krymtkts/FsPowerShellTemplate) の開発をした。
Cmdlet, Command-line predictor, Feedback Provider の相互の連携を作り、 task runner の整備をした。

[`ICommandPredictor`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.prediction.icommandpredictor?view=powershellsdk-7.4.0) と [`IFeedbackProvider`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.feedback.ifeedbackprovider?view=powershellsdk-7.4.0) を別 class で実装してるので、状態の共有を別の class を経由する必要がある。
この場合は、コードの中枢を module に抽出するのが良い。 [`Core`](https://github.com/krymtkts/FsPowerShellTemplate/blob/d35ee403e1dd54f1aeaf03105f1536c0cb7b97ec/src/SampleModule/Core.fs) とした。
Cmdlet, Command-line predictor, Feedback Provider はそれを参照する薄い実装のみに留めるのが良いだろう。

`Core` がこんな感じ。

```fsharp
namespace SampleModule

open System.Threading
open System.Collections

module Core =
    type GreetingStore() =
        [<Literal>]
        let dirtyFlag = 1

        [<Literal>]
        let cleanFlag = 0

        let gate = obj ()
        let names = Generic.List<string>()
        let mutable dirty = cleanFlag

        member __.Add(name: string) =
            lock gate (fun () ->
                name |> names.Add
                dirty <- dirtyFlag)

        member __.Get() : seq<string> =
            lock gate (fun () ->
                // NOTE: Return a snapshot to avoid enumeration issues with concurrent updates.
                names |> Seq.toArray :> seq<string>)

        member __.Count() : int = lock gate (fun () -> names.Count)

        member __.Remove(name: string) =
            lock gate (fun () ->
                if name |> names.Remove then
                    dirty <- dirtyFlag)

        member __.ConsumeUpdated() : bool =
            Interlocked.Exchange(&dirty, cleanFlag) = dirtyFlag

    let greetingStore = GreetingStore()
```

Command-line predictor (長いので一部端折る)
なんか今見てみたら `GetSuggestion` の実装がイマイチなのでより simple に直した方が良さそう。
`OnSuggestionAccepted` を呼ばせるには `CanAcceptFeedback` で `true` を返す必要がある。
また suggestion に mini session ID も含ませていないと呼ばれなかった。
これらの情報は多分 sample には載ってないのよね。 interface の説明[^1][^2]等からこの挙動を拾ったのだけど、他の文書に書いてないのかな。

[^1]: [ICommandPredictor.OnSuggestionDisplayed Method (System.Management.Automation.Subsystem.Prediction) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.prediction.icommandpredictor.onsuggestiondisplayed?view=powershellsdk-7.4.0)
[^2]: [SuggestionPackage Constructor (System.Management.Automation.Subsystem.Prediction) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.prediction.suggestionpackage.-ctor?view=powershellsdk-7.4.0#system-management-automation-subsystem-prediction-suggestionpackage-ctor(system-uint32-system-collections-generic-list((system-management-automation-subsystem-prediction-predictivesuggestion))))

```fsharp
// 略

type GreetingPredictor(guid: string) =
    let id = guid |> Guid.Parse

    let mutable miniSessionId = 0
    // 略

    interface ICommandPredictor with
        // 略

        member __.GetSuggestion
            (client: PredictionClient, context: PredictionContext, cancellationToken: CancellationToken)
            : SuggestionPackage =

            let suggestions =
                context.InputAst.Extent.Text
                |> function
                    // NOTE: suggestionEntries requires non-empty by Requires.NotNullOrEmpty.
                    // https://github.com/PowerShell/PowerShell/blob/eef334de1b0f648512859bd032356f9c8df7cb91/src/System.Management.Automation/engine/Subsystem/PredictionSubsystem/ICommandPredictor.cs#L278
                    | input when input |> String.IsNullOrWhiteSpace -> Seq.empty
                    | input ->
                        greetingStore.Get()
                        |> Seq.choose (fun name ->
                            if name.Contains(input, StringComparison.OrdinalIgnoreCase) then
                                PredictiveSuggestion(
                                    $"{suggestionPart1}{name}{suggestionPart2}",
                                    "A friendly greeting from F#!"
                                )
                                |> Some
                            else
                                None)
                |> Linq.Enumerable.ToList

            // NOTE: empty suggestionEntries is rejected by PowerShell's internal validation.
            if suggestions.Count = 0 then
                Unchecked.defaultof<SuggestionPackage>
            else
                // NOTE: SuggestionPackage must include a mini-session id; PowerShell uses it when calling OnSuggestionDisplayed/OnSuggestionAccepted.
                let session = Threading.Interlocked.Increment(&miniSessionId) |> uint32
                SuggestionPackage(session, suggestions)

        member __.CanAcceptFeedback(client: PredictionClient, feedback: PredictorFeedbackKind) : bool =
            DebugLogger.WriteLine $"CanAcceptFeedback: Feedback kind: {feedback}"

            // NOTE: to capture events, must be return true for expected feedback kinds.
            feedback = PredictorFeedbackKind.SuggestionAccepted

        // 略

        member __.OnSuggestionAccepted(client: PredictionClient, session: uint32, acceptedSuggestion: string) : unit =
            DebugLogger.WriteLine $"OnSuggestionAccepted: Accepted suggestion: {acceptedSuggestion}"

            let matches = acceptedSuggestion |> greetingPattern.Match

            if matches.Captures.Count = 1 then
                let removal = matches.Groups.["removal"].Value
                removal |> greetingStore.Remove
                DebugLogger.WriteLine $"OnSuggestionAccepted: Removed greeting for: {removal}"

        // 略
```

Feedback Provider は、多分既定では [`GetFeedback`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.feedback.ifeedbackprovider.getfeedback?view=powershellsdk-7.4.0#system-management-automation-subsystem-feedback-ifeedbackprovider-getfeedback(system-management-automation-subsystem-feedback-feedbackcontext-system-threading-cancellationtoken)) の引数 `context`[^3] をこねくり回して前の実行の文脈から処理対象を選び出すとかを想定されてる。
ただ Cmdlet, Command-line predictor, Feedback Provider の連携が必要な場合はそれだと役に立たない。
なので状態の共有が結果的に一番 simple になる。
template 用の sample 実装では global な状態を持たせてるから、同じ PowerShell session の中でだけ状態が共有される。
session をまたいで共有させたい場合は [`krymtkts/SnippetPredictor`](https://github.com/krymtkts/SnippetPredictor) のように PowerShell の外の世界に永続化しないといけない。

[^3]: 型は [FeedbackContext Class (System.Management.Automation.Subsystem.Feedback)](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.feedback.feedbackcontext?view=powershellsdk-7.4.0)

```fsharp
// 略

type GreetingFeedbackProvider(guid: string) =
    // 略

    interface IFeedbackProvider with
        // 略
        member __.Trigger: FeedbackTrigger = FeedbackTrigger.Success

        member __.GetFeedback(context: FeedbackContext, token: Threading.CancellationToken) : FeedbackItem | null =
            DebugLogger.WriteLine "GetFeedback: Checking for feedback provision."

            // NOTE: Provide feedback only when there is an update.
            // NOTE: Using the greeting store directly here for simplicity and context does not provide suggestion acceptance status.
            if greetingStore.ConsumeUpdated() then
                let header = "Greeting Feedback"

                FeedbackItem(
                    header,
                    [ $"You have {greetingStore.Count()} greetings stored."
                      "Thank you for using the Greeting Predictor!" ]
                    |> Generic.List<string>,
                    "Feedback for the Greeting Predictor",
                    FeedbackDisplayLayout.Portrait
                )
            else
                null
```

改めて [Command-line predictor](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor) が中枢だと思った。
[Feedback Provider](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-feedback-provider) は、言葉は悪いが Command-line predictor に添えるアクセントのようなものというのが現状だろう。
Feedback Provider が登場したのは 2023 年の PowerShell 7.4 だが、いまのところ今後も残り続けるのかがよくわからん機能でもある。
コミュニティもなんとなく使い所がよくわからない機能として扱ってる気もする(それは Command-line predictor も同じ話だが)。
Feedback Provider の GitHub Project はあるようだが 2026-02-15 時点で大きな動きはなさそう。 community が feedback しても先に進まん的な雰囲気があるのかも知れない。
[Main · Feedback Provider Roadmap](https://github.com/orgs/PowerShell/projects/11/views/1)

もしかしたら [community call](https://github.com/PowerShell/PowerShell-RFC/blob/master/CommunityCall/README.md) に参加してたらなんかわかるのかな？
そんな感じには思えないのだけど。

Command-line predictor, Feedback Provider は binary module なので、試した感じでは PowerShell だけで完結してサクッと書けない。
C# なり F# なりで書く必要があるから、それもあって feedback が集まりにくい状況になっているのかもな。
実際、実装自体は `ICommandPredictor` と `IFeedbackProvider` を 1 つの class に実装すれば状態共有も単純だ。
なので PowerFighter(PowerShell 使いの自作の造語) の言語をまたぐ心理的なハードルだけが普及の課題なんやろう。

ハードルを超えて Command-line predictor を書けば、結構いい感じに PowerShell 統合された追加機能を作れる。
でも Feedback Provider はコマンド実行後の表示を賑やかすだけなので、あえて付ける必要があるかというのがこれまた難しい。

何にせよ実際に書いてみないと使い所や問題点もイメージできなかったし、今回書いてよかった。
まだ廃止にもなってないし、自作 PowerShell module にも使ってみようと思っている。
採用した後で廃止となれば、撤退作業のような普段経験しにくい作業も体験できるかも知れないし、やっておく経験が損になることはないかな。

---

あと task runner 。今回から psake じゃなく [nightroman/Invoke-Build](https://github.com/nightroman/Invoke-Build) を使うようにした。
[psake/psake](https://github.com/psake/psake) は名前が最高だが parameter を渡すのはちょっと面倒だし、 Invoke-Build でならそのへん simple に改善できそうだったためだ。

build script の名は Invoke-Build の流儀に合わせ [`build.ps1`](https://github.com/krymtkts/FsPowerShellTemplate/blob/d35ee403e1dd54f1aeaf03105f1536c0cb7b97ec/build.ps1) とした。
build script 直で呼んだ場合に Invoke-Build で起動し直す。
build script で呼べるので parameter 補完を PowerShell の仕組みに寄せた感じ。
parameter の一部を `$Task` としてたら予約されておりエラーになったので、そこだけ別名にしたため詰め直しが必要だった。
使用感はなかなか良い。

```powershell
<#
.Synopsis
    Invoke-Build tasks
#>

# Build script parameters
[CmdletBinding()]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', '', Justification = 'Variables are used in script blocks and argument completers')]
param(
    [Parameter(Position = 0)]
    [ValidateSet('Init', 'Clean', 'Lint', 'Build', 'Import')]
    [string[]] $Tasks = @('Build'),

    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

# If invoked directly (not dot-sourced by Invoke-Build), hand off execution to Invoke-Build.
if ($MyInvocation.InvocationName -ne '.') {
    $forward = $PSBoundParameters.GetEnumerator() | ForEach-Object -Begin { $acc = @{} } -Process {
        Write-Host "Processing parameter: ${_}" -ForegroundColor Yellow
        if ($_.Key -ne 'Tasks') {
            $acc[$_.Key] = $_.Value
        }
    } -End { $acc }
    Invoke-Build -File $PSCommandPath -Task $Tasks @forward
    exit $LASTEXITCODE
}

# 略
```

まだ Invoke-Build 準拠の help とか書いてない。けどなくてもいいかもしれんな。

あとは sample module の unit test や end-to-end test 、 documentation あたりを整備すれば一通りそろうかな。
そしたらまず GitHub の template repository 化しよう。
