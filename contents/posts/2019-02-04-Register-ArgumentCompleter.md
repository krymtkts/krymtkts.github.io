---
title: "Register-ArgumentCompleter"
tags:  ["powershell"]
---

まだoutputが習慣化してなくて随分空いてしまった🤔

### [Register-ArgumentCompleter](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/register-argumentcompleter?view=powershell-6)

PowerShell5から、従来の`TabExpansion`に代わる`Register-ArgumentCompleter`なるモノが現れたのは、PowerShellerなら知っているだろう(わたしは去年まで知らんかったのでPowerShellerではないのだ)。

### サンプル書いてみた

Mavenのよく使うコマンドでお試し。

```powershell
Register-ArgumentCompleter -Native -CommandName mvn -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    echo -- clean install eclipse:eclipse |
        Where-Object { $_ -like "$wordToComplete*" } |
        Sort-Object |
        ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
}
```

めちゃんこ簡単。

### 作ったもの

当時仕事で巨大なMavenプロジェクトを使っていて、コマンド打つのも億劫だったのでPowerShellで自動補完がほしいなと思っていたのだが、`TabExpantion`はちょっと自分には使いこなせなかった。関数のオーバーライドをしたりのおまじないが必要だし。

`Register-ArgumentCompleter`なら簡単に作れたので、APIが進化してる感をひしと感じたのであった。以下成果物↓

[krymtkts/MavenAutoCompletion: MavenAutoCompletion provides a simple auto completion of Maven 3 to PowerShell.](https://github.com/krymtkts/MavenAutoCompletion)

### 既知のバグ

PowerShell5だと`-Native`オプションありの場合に`-`を利用した補完ができないバグがあって、前述の自動補完がうまく使えなくて辛い...

[Native ArgumentCompleter not invoked for inputs that begin with hyphen (-) · Issue #2912 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/2912)

PowerShell5.xでも使えないものかと試してみたところ、Trickyな回避策として<code>&#x60;</code>で`-`をエスケープしたらイケるというのを見つけたが...posh-gitもchocolateyもそれで従来からの`TabExpantion`を使ってたのかーという気付きは得られた。

ちなみにわたしはPowerShell6を使ってるのでかんけーないのだ😜

### 残

`Register-ArgumentCompleter`のScriptBlockの引数をちまちま調べたのがあるけど、長いから別に書こう。
