---
title: "F# で Cmdlet を書いてる pt.73"
subtitle: "pocof 0.20.0"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。

[0.20.0](https://www.powershellgallery.com/packages/pocof/0.20.0) をリリースをした。

benchmark 上は 100 万件とかの追加だともとより遅くなってそうだが、メモリ効率は良くなっているし全ての件数に於いて走査は速くなって。
あとは実際に使ってみて変な bug が無いかを見たい。
次はこれまた細かい効率化の修正を入れていこうと考えている。

今回のリリースで更新を忘れていた Command help(`pocof-Help.xml`) に word 単位の操作の shortcut が反映された。
その作業自体はたいしたことなかったが、最新の [PlatyPS](https://github.com/PowerShell/platyPS) を使うのが結構面倒だった。
[#361](https://github.com/krymtkts/pocof/pull/361)

今の最新は [Microsoft.PowerShell.PlatyPS v1.0.1](https://www.powershellgallery.com/packages/Microsoft.PowerShell.PlatyPS/1.0.1) 。
多分前回は [`1.0.0-preview1`](https://www.powershellgallery.com/packages/Microsoft.PowerShell.PlatyPS/1.0.0-preview1) を使ってるのかな？随分変わってる。
この手の作業はいつもわからなくなるので [`psake`](https://github.com/psake/psake) の task にまとめているが、今まで通りのやり方だと出力結果がめちゃくちゃになるので、1 つずつ手で行った。
余談として、いつものことだが PowerShell Gallery の検索が機能してないときは、 `/packages/` の後ろに直に package 名を打ち込んで飛ぶのが良い。

まず最初に [`Update-MarkdownCommandHelp`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/update-markdowncommandhelp?view=ps-modules) で既存の Markdown help を更新してみた。
この手順でセクション内の要素が何故か 2 重 3 重に出力されてしまうのでもうダメだなと判断。
既存ファイルを削除して [`New-MarkdownCommandHelp`](https://learn.microsoft.com/ja-jp/powershell/module/microsoft.powershell.platyps/new-markdowncommandhelp?view=ps-modules) を実行後、差分を反映した。
こんな感じ。

```powershell
New-MarkdownCommandHelp -ModuleInfo (Get-Module pocof) -OutputFolder .\docs\ -HelpUri 'https://github.com/krymtkts/pocof/blob/main/docs/pocof/Select-Pocof.md' -Locale en-US
```

前回リリース時にどの version を使ったか忘れてしまったが、 parameter に余分な `ParameterValue: []` が生えて問題だった。
それが v1.0.1 だと解消されていた。
これは version up によるいいところだが、個人的には悪いところの方が多いと感じた。

他、 Markdown の表の中に `.` があると余計な改行が挟まれて表が崩れるようだった。
幸い実装予定から削った shortcut の除去により `.` が消えて正しく生成されるようになった。

そして最後の問題が、 [`Export-MamlCommandHelp`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/export-mamlcommandhelp?view=ps-modules) 。
MAML help export 後に example の各 code block 直後に `<maml:para>&#x80;</maml:para>` が差し込まれてしまう。なんじゃこりゃ。
以下の issue と同じ問題に悩まされた。

[Export-MamlCommandHelp doesn't handle fenced code blocks in examples · Issue #799 · PowerShell/platyPS](https://github.com/PowerShell/platyPS/issues/799)

上記の issue を読むに、この挙動は PlatyPS の仕様であり、 code block による sample code の例示が非推奨であるような印象を受けた。
ただし「bug ではありません。仕様です。」で issue が閉じられて以降は特に回答もなく、推奨される example の書き方はわからないままだ。
結局自動化の policy に反するが、出力結果に含まれる余計な `<maml:para>&#x80;</maml:para>` を手で削って MAML help を仕上げた。
実に残念や。

今回はこれで乗り切ったが、次回もこれだとちょっと嫌だな。
挙動が意図したものか否かがわからないってのは結構きつい。
なのでもう少し PlatyPS の設計([`/docs`](https://github.com/PowerShell/platyPS/tree/main/docs) にあるみたい)を読んで理解を深めたい。
分量的にまありえられるものがなさそうだけど。

パッと見 PlatyPS 自体は PlatyPS を使ってない。
[`PowerShell Gallery`](https://www.powershellgallery.com/packages/Microsoft.PowerShell.PlatyPS/) を見ても downloads が他の PowerShell の package に比べてかなり低い。
これらから考えるとあまり使われてなくて機能が磨かれてないと見るのが妥当か。
こういう状況なので、自力でなんとかして調べていくための動機づけが中々難しいが、 AI サンに手伝ってもらったりで誤魔化しつつ進めていくかー。
