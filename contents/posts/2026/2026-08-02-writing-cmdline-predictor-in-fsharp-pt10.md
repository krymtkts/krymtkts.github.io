---
title: "F# で command-line predictor を書いてる Part 10"
subtitle: "PSReadLine custom key bindings"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発をした。

tab で候補が補完できるようにした。
厳密には [PSReadLine](https://learn.microsoft.com/en-us/powershell/module/psreadline/?view=powershell-7.6) 用の custom key binding を作った。

SnippetPredictor は毎日便利に利用させてもらってるが、 [command-line predictor](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.6) 単体では補完周りがないのでイマイチな点だ。
毎日 `:snp` を手で打たなければならないし、 `:{group id}` なんかいちいち覚えてない。
[ListView](https://learn.microsoft.com/en-us/powershell/scripting/learn/shell/using-predictors?view=powershell-7.6) で候補が表示されるとしても、その中から目当てのものを手動で type するなり ListView から選択しなければならない。

そこで `:` を type したら SnippetPredictor で管理している `:snp` とか `:{group id}` を補完できれば便利そうだと思いついた。
しかし [`Register-ArgumentCompleter`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/register-argumentcompleter?view=powershell-7.6) は決まった command に続けて space 区切りした場合のみ文字を補完できるため、目的にそぐわない。
この要求を満たすには [`Set-PSReadLineKeyHandler` の `ScriptBlock`](https://learn.microsoft.com/en-us/powershell/module/psreadline/set-psreadlinekeyhandler?view=powershell-7.6#scriptblock) を使った custom key binding を登録するしかないみたいだった。
よってその方向で実現することを考えた。

この方法だと custom key binding の中から [`PSConsoleReadLine.GetBufferState`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.powershell.psconsolereadline.getbufferstate) とかの PSReadLine の操作が必要になる。
当然 F# binary module にその依存関係を参照させて操作するのは、重いし嫌だなと考えた。
なので PowerShell でこの補助機能を実装することにした。
SnippetPredictor の F# binary module へは PSReadLine 依存を追加しない。
代わりに PowerShell の script module を作成し、 Module Manifest で [`NestedModules`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_module_manifests?view=powershell-7.6#nestedmodules) として組み込む hybrid module 構成にした。

script module は PSReadLine の `TabCompleteNext` `TabCompletePrevious` key bindings 用の script block を提供する。
これらを `Enable-SnippetPredictorKeyHandler` を実行して、既定で `Tab`, `Shift+Tab` に割り当てる。
snippet や group ID の取得はコアである F# binary module で行うが、その得た値を PSReadLine へ受け渡すのは script module の役割だ。
custom key bindings にまつわる状態の管理も script 内に閉じている。
PSReadLine の `Set-PSReadLineKeyHandler` も session の間しか有効じゃないし、揮発性がある script scoped な状態管理で十分だった。

cross-platform 対応において少し厄介だった点がある。
動作上の問題でなくて単に Pester での end-to-end testing が面倒だっただけだが。
どうも PSReadLine では enum 形式の `D0`～`D9` chord が Unix 上で null `KeyChar` から `@` へ正規化されるらしい。
それらの異なる digit key が同じ binding へ alias され、期待の結果が得られなくなるため test が失敗しまくった。
PSReadLine のコード的には以下のあたりっぽい。

- [ConsoleKeyChordConverter.cs](https://github.com/PowerShell/PSReadLine/blob/v2.4.5/PSReadLine/ConsoleKeyChordConverter.cs#L183-L221) に独自の mapping がなくて `\0` になって
- [Keys.cs](https://github.com/PowerShell/PSReadLine/blob/v2.4.5/PSReadLine/Keys.cs#L298-L305) で `\0` が `@` になってそう

よって実 binding を使う test では、名前が維持される `F1`～`F24` を使用することで問題を回避している。
一応テスト的には cross-platform 対応できてるけど、手動での動作確認は Windows でしかできてない。

りょっと触ってみるだけでも、この小さな改善で前よりいい感じがしてる。
特に group ID を補完できるのがすごくいい。
もう少し文書とかを小綺麗にしたら、次のリリースで公開したい。
