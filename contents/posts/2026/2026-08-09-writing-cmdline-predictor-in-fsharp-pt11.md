---
title: "F# で command-line predictor を書いてる Part 11"
subtitle: "0.7.0"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.7.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.7.0) をリリースした。
まだ GitHub Actions で release するようになってから熟れてないのもあって release task がコケたりしたけ [#133](https://github.com/krymtkts/SnippetPredictor/pull/133) ど、なんとか公開できた。

[前回の日記](/posts/2026-08-02-writing-cmdline-predictor-in-fsharp-pt10.html)で書いた tab で候補が補完できるようにしたやつ。
今回の release から F# binary module と PowerShell module の hybrid だ。
[PSReadLine](https://learn.microsoft.com/en-us/powershell/module/psreadline/?view=powershell-7.6) 用の custom key binding を設定する PowerShell function を提供する。
PSReadLine の custom bindings を作成するあたりはちょっと過激だが、それしか妥当な手段がないから仕方ない。
もし今後の PSReadLine の更新で壊れるようなら、 script module の方で細々とした対応が必要となるだろう。

あとこの PSReadLine の key bindings とか handler とか action とかの言葉の使い分けがまだいまいちわかってない。
今回の機能地下でも PSReadLine の文書見つつ整理して changelog 等の文書を書いた。はっきりと書いてない気がするのは気の所為か。
でもひょっとしたら言葉の使い方間違ってる所あるやもしれん。後で探す。

何にせよ `:` 打鍵後の tab で snippet group が補完されるようになったのほんとに良いので、毎日使いに貢献するだろう。
ひょっとしたらその過程で予期せぬ bug が見つかることもあろうけど、それは dogfooding ってことでいこう。
