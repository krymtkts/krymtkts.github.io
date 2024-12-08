---
title: "F# で Cmdlet を書いてる pt.56"
tags: ["fsharp", "powershell", "dotnet", "platyps"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

まだ [FsCheck](https://fscheck.github.io/FsCheck/index.html) を使った既存の事例テストを PBT で置き換えてる最中。
なるべくシンプルなプロパティにして、ジェネレータでいろんなパターンを書くような感じで今は上手くハマっている。
今はまだ簡単な関数に対するプロパティしか書いてないので、もうそろそろ難易度上げても良い頃合いかも。

.NET 9 のやつも放置状態。 FSharpLint の .NET 9 の問題は issue ができてた。 [Latest version incompatible with .NET 9 · Issue #718 · fsprojects/FSharpLint](https://github.com/fsprojects/FSharpLint/issues/718)

他に新しいネタとしては、 2024-10-30 に dev blog に出てた [PlatyPS の v1 の preview](https://www.powershellgallery.com/packages/Microsoft.PowerShell.PlatyPS/1.0.0-preview1) へ移行始めてみた。
調べた内容を整理する意味も込めてまとめておく。

[Announcing Microsoft.PowerShell.PlatyPS 1.0.0-Preview1 - PowerShell Team](https://devblogs.microsoft.com/powershell/announcing-platyps-100-preview1/)

まだ preview なので PlatyPS のチュートリアルは更新されてない。 [Create XML-based help using PlatyPS - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/utility-modules/platyps/create-help-using-platyps?view=ps-modules)
install は先に挙げた dev blog に従ってやる。

```powershell
Install-PSResource -Name Microsoft.PowerShell.PlatyPS -Prerelease -Scope AllUsers
```

```powershell
> Get-PSResource -Name platyPS,Microsoft.PowerShell.PlatyPS -Scope AllUsers | select Name,Version,PublishedDate

Name                         Version PublishedDate
----                         ------- -------------
platyPS                      0.14.2  2021-07-02 22:53:28
Microsoft.PowerShell.PlatyPS 1.0.0   2024-10-29 22:38:53
```

実に 3 年ぶりの更新になる様子。 [pocof を作り始めたとき](/posts/2022-05-07-start-to-write-cmdlet-by-fsharp.html)から v0.14.2 を使ってた。
module name も変わった。 `PowerShellGet` が `PSResourceGet` になったのと同じ命名規則なので、 PowerShell Team の module は将来皆こうなるんかな。

module に含まれる Function, Cmdlet は随分様変わりした。コレはなるほど使い方がわからん。

```powershell
> Get-Command -Module platyPS

CommandType     Name                                               Version    Source
-----------     ----                                               -------    ------
Function        Get-HelpPreview                                    0.14.2     platyPS
Function        Get-MarkdownMetadata                               0.14.2     platyPS
Function        Merge-MarkdownHelp                                 0.14.2     platyPS
Function        New-ExternalHelp                                   0.14.2     platyPS
Function        New-ExternalHelpCab                                0.14.2     platyPS
Function        New-MarkdownAboutHelp                              0.14.2     platyPS
Function        New-MarkdownHelp                                   0.14.2     platyPS
Function        New-YamlHelp                                       0.14.2     platyPS
Function        Update-MarkdownHelp                                0.14.2     platyPS
Function        Update-MarkdownHelpModule                          0.14.2     platyPS
```

```powershell
> Get-Command -Module Microsoft.PowerShell.PlatyPS

CommandType     Name                                               Version    Source
-----------     ----                                               -------    ------
Function        New-HelpCabinetFile                                1.0.0      Microsoft.PowerShell.PlatyPS
Function        Show-HelpPreview                                   1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Compare-CommandHelp                                1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Export-MamlCommandHelp                             1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Export-MarkdownCommandHelp                         1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Export-MarkdownModuleFile                          1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Export-YamlCommandHelp                             1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Export-YamlModuleFile                              1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Import-MamlHelp                                    1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Import-MarkdownCommandHelp                         1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Import-MarkdownModuleFile                          1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Import-YamlCommandHelp                             1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Import-YamlModuleFile                              1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Measure-PlatyPSMarkdown                            1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          New-CommandHelp                                    1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          New-MarkdownCommandHelp                            1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          New-MarkdownModuleFile                             1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Test-MarkdownCommandHelp                           1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Update-CommandHelp                                 1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Update-MarkdownCommandHelp                         1.0.0      Microsoft.PowerShell.PlatyPS
Cmdlet          Update-MarkdownModuleFile                          1.0.0      Microsoft.PowerShell.PlatyPS
```

PlatyPS の reference は更新されてるので、そこを参考に手で試しながら理解するしかないか。
[Microsoft.PowerShell.PlatyPS Module - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/?view=ps-modules)

pocof の用途だと「既存の help をイイ感じに更新したい」ところなので、その周りの機能を確認する。

```powershell
# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/measure-platypsmarkdown?view=ps-modules
# Markdown help が含む content type と schema version を検証する
# Filetype が enum なので stringify された値に対して match しないと filter できない
# Output の path 属性が Filepath なので pipeline でそのまま渡しにくい
# 気が利いた機能だが出力が微妙に行けてなくて PlatyPS 内での取り回しが良くない
Measure-PlatyPSMarkdown .\docs\*.md
#
# Title   Filetype              Filepath
# -----   --------              --------
# unknown CommandHelp, V1Schema C:\Users\takatoshi\dev\github.com\krymtkts\pocof\docs\Select-Pocof.md

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/test-markdowncommandhelp?view=ps-modules
# Markdown command help の内容を検査する
Measure-PlatyPSMarkdown .\docs\*.md | ? Filetype -match CommandHelp | select -ExpandProperty FilePath | Test-MarkdownCommandHelp -DetailView
#
# Test-MarkdownCommandHelp
#   Valid: True
#   File: C:\Users\takatoshi\dev\github.com\krymtkts\pocof\docs\Select-Pocof.md
#
# Messages:
#   PASS: First element is a thematic break
#   PASS: SYNOPSIS found.
#   PASS: SYNOPSIS is in order.
#   PASS: SYNTAX found.
#   PASS: SYNTAX is in order.
#   PASS: DESCRIPTION found.
#   PASS: DESCRIPTION is in order.
#   PASS: EXAMPLES found.
#   PASS: EXAMPLES is in order.
#   PASS: PARAMETERS found.
#   PASS: PARAMETERS is in order.
#   PASS: INPUTS found.
#   PASS: INPUTS is in order.
#   PASS: OUTPUTS found.
#   PASS: OUTPUTS is in order.
#   PASS: NOTES found.
#   PASS: NOTES is in order.
#   PASS: RELATED LINKS found.
#   PASS: RELATED LINKS is in order.

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/import-markdowncommandhelp?view=ps-modules
# Markdown command help を読み込んで command help の object を作る
$before = Measure-PlatyPSMarkdown .\docs\*.md | ? Filetype -match CommandHelp | select -ExpandProperty FilePath | Import-MarkdownCommandHelp

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/update-commandhelp?view=ps-modules
# command help object を session に存在する同名 cmdlet の情報で更新する
$after = Measure-PlatyPSMarkdown .\docs\*.md | ? Filetype -match CommandHelp | select -ExpandProperty FilePath | Update-CommandHelp

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/compare-commandhelp?view=ps-modules
# command help object 同士を比較した text を出力する
# 出力される text が D=delete, M=modified, S=same ぽいので適宜 filter しないといけない
Compare-CommandHelp $before $after | ? {$_ -notlike 'S*'}
# M Inspecting dictionary CommandHelp.Metadata
# D   CommandHelp.Metadata: ms.date does not exist in reference
# M Inspecting list CommandHelp.Syntax
# D CommandHelp.Syntax lists are different sizes (1 vs 2)
# 省略

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/export-markdowncommandhelp?view=ps-modules
# command help object を Markdown command help に出力する
# OutputFolder 配下に module name で directory が作成されてその中に出力される
# module name の directory 作らず平置きしてる場合は注意
$after | Export-MarkdownCommandHelp -OutputFolder .\docs

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/update-markdowncommandhelp?view=ps-modules
# Markdown command help を session に存在する同名 cmdlet の情報で更新する
# デフォ *.bak ファイルを作ってくるので、 -NoBackup を指定すると作らなくなる
# description とか example に手で書いた内容は消えないので気軽に実行できる
Measure-PlatyPSMarkdown .\docs\*.md | ? Filetype -match CommandHelp | select -ExpandProperty FilePath | Update-MarkdownCommandHelp -NoBackup

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/export-mamlcommandhelp?view=ps-modules
# OutputFolder 配下に module name で directory が作成されてその中に `${moduleName}-Help.xml` 出力される
Measure-PlatyPSMarkdown .\docs\*.md | ? Filetype -match CommandHelp | select -ExpandProperty FilePath | Import-MarkdownCommandHelp | Export-MamlCommandHelp -OutputFolder ./src/

# https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.platyps/show-helppreview?view=ps-modules
# 出力した MAML を Markdown preview する。何故かこいつは Path が Position 0 じゃなく Named なので注意
Show-HelpPreview -Path .\src\pocof\pocof.dll-Help.xml
# 省略
```

`Measure-PlatyPSMarkdown` の output の object が pipeline で使いにくいのだけ気に入らんが、他は良好な使用感そう。

pocof の古いドキュメントを更新するにあたってやるべきことも見えてきた。
`Update-MarkdownCommandHelp` を 0.14.2 で作った Markdown command help に対して実行すると scheme の version が 1 -> 2 になって結構変わるので、初回は scheme の更新のみで実行する方が良さそう。
`OutputFolder` 配下に module name の directory ができるのもあるし、出力されるファイル名が変わるのもあるし、まずは今の構造を変えるのが良さげやな。

あと地味に Markdown の table が MAML 翻訳されない bug も直ってるようなので、コレを気に pocof の help も多少綺麗にできるかな。
[Markdown tables not rendered properly in MAML · Issue #577 · PowerShell/platyPS](https://github.com/PowerShell/platyPS/issues/577)

続く。
