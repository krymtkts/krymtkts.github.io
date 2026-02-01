---
title: "F# の PowerShell Module の Template を作りたい"
tags: ["fsharp", "powershell", "dotnet"]
---

今年やったことないものに取り組むのを、何にしようかなと考えていた。
結局 PowerShell の Feedback provider 辺りがいいかなと決定した。

ただ Feedback provider の本当の魅力は Command-line predictor との連携なんやが、何を連携させるかのアイデアがない。
とはいえ F# で Feedback provider を書く変わり者もそういないし、それだけでも作る価値あるような気もしている。
なもんで何を実装するのかのアイデアを決めるまでの場繋ぎが必要になった。

Feedback provider を作るだし、せっかくなので Template を作ってみよう思っている。
F# で作る PowerShell の Cmdlet, Command-line predictor, Feedback provider の Template だ。
これまでコピペで作ってたしちょうどいいわ。

これを GitHub の Template にするのか NuGet の Template にするのかが悩ましかったので、 AI に聞いた。
ChatGPT に相談した感じだと、はじめは GitHub Template repository で型を作る。
その後に NuGet の Template で簡単に展開できる版にできるぞということなので、それで行く。
対応言語は F# のみ。自分が使うからな。
参照するのは以下の文書かな。

- [How to create a Standard Library Binary Module - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-standard-library-binary-module?view=powershell-7.5)
- [How to create a command-line predictor - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.5)
- [How to create a feedback provider - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-feedback-provider?view=powershell-7.5)
- [Custom templates for dotnet new - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)

Feedback provider と NuGet の Template は作ったことないが、他は作ったことある。
始めやすく Cmdlet -> Command-line predictor -> Feedback provider -> NuGet Template の流れで作ったらいいかな。
その内に Feedback provider が必要な機能のヒントを得られているかも知れない。
