---
title: "F# で command-line predictor を書いてる Part 13"
subtitle: "0.9.0"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.9.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.9.0) をリリースした。

[前回の日記](/posts/2026-09-13-writing-cmdline-predictor-in-fsharp-pt12.html)では、存在しない group identifier のまま Enter してコマンド実行エラーにしてしまう話を書いた。
Enter での補完機能をつけてこの事故の頻度は減らしたが、本当はこういう入力は [`AcceptLine`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.powershell.psconsolereadline.acceptline?view=powershellsdk-1.1.0) で処理せず、 history へ残さないようにしたかった。
ただ、ユーザへの通知方法を決めてなかったので、前回リリースではそこまで実装しなかった。
今回は `-AcceptChord` の handler が未登録の group identifier を検出すると accept 処理を止め、 bell を鳴らすようにした。
誤入力がそのまま command として実行されるのを防げる。

bell の出し方には少し工夫が必要だった。
[PSReadLine](https://learn.microsoft.com/en-us/powershell/module/psreadline/?view=powershell-7.6) で [`Get-PSReadLineOption`](https://learn.microsoft.com/en-us/powershell/module/psreadline/get-psreadlineoption?view=powershell-7.6) の [`BellStyle`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.powershell.bellstyle?view=powershellsdk-1.1.0) が `Visual` の場合だけ、 `[Console]::Write([char]7)` で直接 [BEL](https://ghostty.org/docs/vt/control/bel) を送る。
何故このような周りくどいことをするのかというと、 PSReadLine の `Visual` bell は[現状空の実装](https://github.com/PowerShell/PSReadLine/blob/2984546f62da9f63c31aba965ec3008fcb107024/PSReadLine/Render.cs#L1843-L1845)になっており、フィードバックを表示しないからだ。

[Set-PSReadLineOption BellStyle should have an option for an actual ASCII 7 bell · Issue #4766 · PowerShell/PSReadLine](https://github.com/PowerShell/PSReadLine/issues/4766)

2025 年の起票から今までどうするか進んでないみたいなので、自前で workaround を使うのが妥当と判断した。
それ以外の `BellStyle` は PSReadLine でも正しく動くようなので `[Microsoft.PowerShell.PSConsoleReadLine]::Ding()` を呼ぶ。
多分一般的な terminal で期待される実装は、 terminal へ `[Console]::Write([char]7)` を送って、 terminal の振る舞いに任せることだろう。
ただ PSReadLine の流儀に沿うため、このような形にした。

他にも細かい修正を色々入れている。
例えば group identifier の候補順は ordinal order にした。
前は dictionary の列挙順だったので、表示順に決まりがなかった。
補完候補では `:snp` が先頭で、その後に group identifier が並ぶ。snippet 自体の候補順は変えていない。

設定まわりも少し整えた。
`SNIPPET_PREDICTOR_CONFIG` が空白だけなら未設定として扱う。
元々テスト用途だったが undocumented な正規機能として残ってるのだけど、折角なので丁寧に扱うことにした。

設定の Snippet が欠落、null、空、空白だけの場合や、要素自体が null の場合は JSON path を含む診断を出す。

Tooltip は省略や null を空文字として扱い、設定ファイルを読めない場合も parse error とは別に診断する。
`NextChord` と `PreviousChord` に null、空、空白だけの値を指定した場合も拒否する。

changelog に載っていない細かい修正だと、 suggestion cache の状態を 1 つの `Snapshot` にまとめたのが大きい変更か。
前は snippets や group 一覧をそれぞれ更新していたので、 refresh 中に一部だけ新しい状態になる可能性があった。
今は新しい状態を組み立ててから一括で差し替えるので、候補を作る側が更新途中の状態を見ることがない。
原子性が保たれるようになったということ。

今回は細かい修正が中心で、比較的小さい release になった。普段使いで困るところを少しずつ潰せている。
当面は他にもやりたい細かい修正があるから、少しずつ進めて好みの感じに仕上げていくつもり。
