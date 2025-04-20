---
title: "F# で command-line predictor を書いてる Part 1"
subtitle: sample 実装を F# に翻訳する
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

ちょっと前触れた [command-line predictor を書きたい](/posts/2024-10-13-i-want-to-write-predictor-in-fsharp.html)というやつを試し始めた。

[How to create a command-line predictor - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.4)

これ ↑ を参考にお試しする。
記事によると command-line predictor は [PSReadLine](https://github.com/PowerShell/PSReadLine) の 7.2 のときに登場した機能らしい。
7.4 でアナウンスされた [feedback provider](https://devblogs.microsoft.com/powershell/what-are-feedback-providers/) はなんか印象強かったが、 command-line predictor の方が先輩機能だった。
日本語の方のドキュメントを見たら「コマンドライン予測器」というある意味カッコいいがなんとも伝わらなそうな名前になっているので、英語の command-line predictor の方を使うことにする。
略記は記事の URL に含まれてる cmdline predictor に倣うとする。

command-line predictor の記事によれば、

> A predictor is a PowerShell binary module.

とのことでバイナリモジュールじゃないとダメなようだ。
確かに始め簡単に試したかったので sample code を AI と一緒に PowerShell に翻訳したものを作ってみたが、ダメだった。

諦めて真面目に F# で翻訳したやつを作ってみた。

[krymtkts/SamplePredictor: A sample command-line predictor written in F#.](https://github.com/krymtkts/SamplePredictor)

今回は素振りなので、次に本ちゃんを作る時用に手順を残した。

```powershell
dotnet new classlib --name SamplePredictor --language F#
cd SamplePredictor
git init
git commit -m 'Initial commit.' --allow-empty
# translate code.
git add .\Library.fs .\SamplePredictor.fsproj
git commit -m 'Initialize F# project.'
touch .gitignore
git add .\.gitignore
git commit -m 'Add `.gitignore`.'

dotnet build

# confirm that PredictionSource is set to HistoryAndPlugin.
Get-PSReadLineOption | Select-Object PredictionSource

# PredictionSource
# ----------------
# HistoryAndPlugin

Import-Module .\bin\Debug\net6.0\SamplePredictor.dll

# confirm that SamplePredictor is added to Implementations.
Get-PSSubsystem -Kind CommandPredictor

# Kind              SubsystemType      IsRegistered Implementations
# ----              -------------      ------------ ---------------
# CommandPredictor  ICommandPredictor          True {Windows Package Manager - WinGet, SamplePredictor}

New-GitHubRepository -Name $(pwd | Split-Path -Leaf) -Description 'A sample command-line predictor written in F#.'
git remote add origin $(Get-GitHubRepository -OwnerName krymtkts -RepositoryName SamplePredictor | select -ExpandProperty ssh_url)
git push -u origin main
```

雑にこんな感じで作れる。

実装は元記事の sample を丸ごとコピペ翻訳しても良かったが、 .NET 素人のためプロジェクト構成の `GenerateDependencyFile` `PublishDir` `ExcludeAssets` `PrivateAssets` 辺りが何を意図して設定したものか理解してなかったので、部分ごとにちまちま確認しつつコピペ翻訳した。
お陰で [krymtkts/pocof](https://github.com/krymtkts/pocof) でも試せそうやなというのが理解できた。
あと挙動がわからんのでファイルにログ出力する雑実装を入れて追跡を容易にし、念の為 sample から GUID も変えてる。

使い方は README.md にも書いてるが、これによって `Import-Module ~` 後に PSReadLine の suggestion に追加した command-line predictor のエントリが現れる。

```powershell
> sss
<-/1>                                                       <SamplePredictor(1)>
> sss HELLO WORLD                                              [SamplePredictor]
```

この実装だと、 [`ICommandPredictor`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.prediction.icommandpredictor?view=powershellsdk-7.4.0) の `CanAcceptFeedback` `GetSuggestion` が呼ばれるタイミングはわかったが、他のメソッドが何するとき動くかは理解できてない。
ドキュメント見てもぱっとせんので、手で試行錯誤するのが良さそう。
あと既にコミュニティで作成されてる predictor の困りごとを探るとか、
[`PSReadLine` の issue](https://github.com/PowerShell/PSReadLine/issues?q=is%3Aissue+is%3Aopen+predictor) を漁って、今出来ないことを探ってくのが現実的な理解方法かも知れんな。
ざっと見た感じ `ICommandPredictor` は 20ms 以内に response しないといけないやつが短過ぎるって意見があるから、重めの処理ができそうにないのは理解した。
にしてもやで、 `ICommandPredictor` で検索してもあんま情報ないな command-line predictor 。調べ方が悪いんかな。或いはどんだけ人気ないねん。 GitHub で `ICommandPredictor` 検索して直に見ていくしかないか。

何も内容がない日記になったが、個人的な理解は進んでるはずなのでヨシッ！
