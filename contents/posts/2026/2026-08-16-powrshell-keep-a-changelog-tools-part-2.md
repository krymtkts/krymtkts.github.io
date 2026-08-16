---
title: "PowerShell Keep a Changelog tools Part 2"
tags: ["powershell", "keepachangelog"]
---

[krymtkts/PSKeepAChangelogTools](https://github.com/krymtkts/PSKeepAChangelogTools) の [0.2.0](https://www.powershellgallery.com/packages/PSKeepAChangelogTools/0.2.0) を公開した。

0.1.0 は AI Agent とざっと作ったのもあって [`New-ModuleManifest`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/new-modulemanifest?view=powershell-7.6) で出力させる Module Manifest の構造に依存してた。
具体的にいうと、書き換え対象の `ReleaseNotes` の位置を特定するのにテンプレ出力のコメントに依存して限定的なケースでしか使えず、手書きの Module manifest だと使えなかった。
流石にこれは良くないので、今回真面目に AST から `ReleaseNotes` の位置を見つけて書き換えるように直した。

わたしは全然知らなかったので AI Agent に色々教えてもらったのだけど、 PowerShell に備え付けの [`Parser` class](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.language.parser?view=powershellsdk-7.6.0) を使うことで AST を取得できる。
今回は [`Parser.ParseInput`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.language.parser.parseinput?view=powershellsdk-7.6.0) という渡した文字列を解析する method を使った。
以下のように AST が取得できる。

```powershell
$content = '@{ PrivateData = @{ PSData = @{ ReleaseNotes = "note" } } }'
$tokens = $null
$parseErrors = $null
$manifestAst = [System.Management.Automation.Language.Parser]::ParseInput($content, [ref] $tokens, [ref] $parseErrors)
$manifestAst

# Attributes         : {}
# UsingStatements    : {}
# ParamBlock         :
# BeginBlock         :
# ProcessBlock       :
# EndBlock           : @{ PrivateData = @{ PSData = @{ ReleaseNotes = "note" } } }
# CleanBlock         :
# DynamicParamBlock  :
# ScriptRequirements :
# Extent             : @{ PrivateData = @{ PSData = @{ ReleaseNotes = "note" } } }
# Parent             :

$tokens | format-table

# Text                 TokenFlags             Kind HasError Extent
# ----                 ----------             ---- -------- ------
# @{           ParseModeInvariant          AtCurly    False @{
# PrivateData          MemberName       Identifier    False PrivateData
# =            AssignmentOperator           Equals    False =
# @{           ParseModeInvariant          AtCurly    False @{
# PSData               MemberName       Identifier    False PSData
# =            AssignmentOperator           Equals    False =
# @{           ParseModeInvariant          AtCurly    False @{
# ReleaseNotes         MemberName       Identifier    False ReleaseNotes
# =            AssignmentOperator           Equals    False =
# "note"       ParseModeInvariant StringExpandable    False "note"
# }            ParseModeInvariant           RCurly    False }
# }            ParseModeInvariant           RCurly    False }
# }            ParseModeInvariant           RCurly    False }
#              ParseModeInvariant       EndOfInput    False
```

これを使って Module manifest 中から `PrivateData.PSData.ReleaseNotes` を探し出し更新している。
万が一、要素が足りない場合は新規に作成される。
これでいったん機能的にはまともになったんではないかと思われる。

機能的にはもう自分の使いた機能の範囲では不足なさそう。
ただ一般的な用途における物足りなさはあるかも知れない。
またテストの漏れだとかコードの冗長さもあろうから、今後はその辺気が付いたら詰めていく感じかな。
