---
title: "F# で Cmdlet を書いてる pt.57"
tags: ["fsharp", "powershell", "dotnet", "platyps"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

PlatyPS についていくつか覚え書きしておくのと、 .NET 9 及び F# 9 対応に進展がありひとまず PR を merge できた。

---

[PlatyPS の v1 の preview](https://www.powershellgallery.com/packages/Microsoft.PowerShell.PlatyPS/1.0.0-preview1) の挙動に関してメモしておく。

> あと地味に Markdown の table が MAML 翻訳されない bug も直ってるようなので、コレを気に pocof の help も多少綺麗にできるかな。
> [Markdown tables not rendered properly in MAML · Issue #577 · PowerShell/platyPS](https://github.com/PowerShell/platyPS/issues/577)

[前回](/posts/2024-12-08-writing-cmdlet-in-fsharp-pt56.html)触れたこれ、直ってるっぽいけどその確認の過程で変なエラーに遭遇した。
再現条件がイマイチ分からなかったが、`Export-MamlCommandHelp` で必ず壊れた MAML が出力されるようだった。
壊れた MAML を `Show-HelpPreview` するとわかりやすくエラーになる。

```powershell
> Show-HelpPreview -Path .\src\pocof\pocof-Help.xml
InvalidArgument: Cannot convert value "<?xml version="1.0" encoding="utf-8"?> <helpItems xmlns:maml="http://schemas.microsoft.com/maml
... </helpItems>" to type "System.Xml.XmlDocument". Error: "The specified node cannot be inserted as the valid child of this node, because the specified node is the wrong type."
```

pocof でこのエラーが発生した場面は、元々 Markdown の table が使えなかったので code block (` ```md ` ~ ` ``` `)で囲んでいた箇所を通常の Markdown に戻したあとだった。
pocof の Markdown command help は scheme 更新した関係で `Locale: en-US` や `title: ...` が生えてなかったのだけど、これらの不足した属性がエラーとは関係ないのは確認した(あってもなくてもエラーになる)。
このとき MAML 最後尾の tag が重複して出力されていた。以下のような感じ。

```diff
   </command:command>
 </helpItems>
+  </command:command>
+</helpItems>
```

MAML command help を出力するコマンドは以下の通り。

```powershell
Measure-PlatyPSMarkdown -Path ./docs/pocof/*.md | Where-Object Filetype -match CommandHelp | Import-MarkdownCommandHelp -Path {$_.FilePath} | Export-MamlCommandHelp -OutputFolder .\src\ -Force
```

1 つわかったのは、エラーを回避するには出力済みの MAML command help を物理的に削除して、再度出力し直す必要があったことだ。
つまり `-Force` option 付きでの上書きの問題があるっぽい？何にせよ workaround があってよかった。
どうにもよくわからんが `Export-MamlCommandHelp` が不正な XML を出力する issue もあるしその関連かなあ。 [OPS13 Export-MamlCommandHelp creates invalid XML · Issue #692 · PowerShell/platyPS](https://github.com/PowerShell/platyPS/issues/692)

他に、 `INPUT` `OUTPUT` の section は code を使ってると `Markdig.Syntax.Inlines.CodeInline` が出力されてしまう。
そうなると繰り返し `Update-MarkdownCommandHelp` を実行したとき冪等性がないっぽくて、 section が無限増殖してしまう。
workaround としては code を section に使わなければ良い。

最後に、 pipeline で使いにくい件は `Path` に Alias なりついてりゃいいよなと思ってるのだけど、 `Path` parameter 周りでなんか改善の予定あるみたいだしそん時に前提的に見直されたりするのかも？様子見。
[Make Path and LiteralPath parameters work like they do for Item cmdlets · Issue #696 · PowerShell/platyPS](https://github.com/PowerShell/platyPS/issues/696)

---

あと .NET 9 及び F# 9 対応に進展があり、ついに PR を merge できた。やったね。

取り敢えず残された課題のうち、最低限 [Fantomas](https://github.com/fsprojects/fantomas) が対応できたら merge しようと考えてた。
でも format できないわ～参ったね... と思って [issue を立てた](https://github.com/fsprojects/fantomas/issues/3142)のだけど、実は [preview](https://www.nuget.org/packages/fantomas/7.0.0-alpha-003) リリースで対応していましたという話だった。
メンテナの人達は忙しかろうから一瞬 Issue を立てて迷惑やったな...と思ったが、立てるまでその preview リリースに気づいてなかったのもあり、結果的に解決に至れたのでヨシッ！とした。
副次的に Fantomas の online で[最新の preview が使える](https://fsprojects.github.io/fantomas-tools/#/fantomas/preview)(Setting で変えれる)のも知ったし、 .NET 9 対応の流れも知れた。

もうそろそろ pocof の今年最後のリリースしても良さそうな感じになってきた。

続く。
