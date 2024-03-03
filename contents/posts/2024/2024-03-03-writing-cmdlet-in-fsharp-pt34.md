---
title: "F# でコマンドレットを書いてる pt.34"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の 0.10.0 をリリースした。

pocof 史上初の全画面スタイルを廃止する Half モード(`TopDownHalf`, `BottomUpHalf`) の導入は、自分自身使ってみてやっぱいいなという感じがしてる。
挙動が [PSReadLine](https://github.com/PowerShell/PSReadLine) チック過ぎて、わたしがやりたかったのは Feedback Providers[^1] やったんかも...というのはあるかも。

[^1]: [What are Feedback Providers? - PowerShell Team](https://devblogs.microsoft.com/powershell/what-are-feedback-providers/)

あと [FSharpLint](https://fsprojects.github.io/FSharpLint/), [Fantomas](https://fsprojects.github.io/fantomas/), [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) 諸々チェックするようにしたので開発の安心感も高まった。

次に着手している開発は [#44 Support query string selection](https://github.com/krymtkts/pocof/issues/44) だ。
わたしのタイピングだと、変えたい箇所を選択して backspace, delete, あるいはそのままタイプして変えることが多い(あと Ctrl + Arrow で移動するのも多いけどこれは保留)。
いま pocof には文字列を選択する機能がないから、通常のエディタと pocof で編集の仕方が違うのでめんどい。
これも個人的に期待度の高い機能となっている(放置してたけど)。

この機能を作るに当たり、 CUI の前景色と背景色を反転する必要がある。
そして PowerShell で CUI の色を操作する方法が 2 つあるのは理解している。
具体的なコードとかは雑な理解だったので今回見てみた。あと他の方法があるかも知れないが、それは調べてない。

1 つは [`Console.ForegroundColor`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.foregroundcolor?view=net-8.0#system-console-foregroundcolor) [`Console.BackgroundColor`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.backgroundcolor?view=net-8.0#system-console-backgroundcolor) を直にいじる方法。
変えた色を戻すのに [`Console.ResetColor`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.resetcolor?view=net-8.0) を使う。

```powershell
# Powerline を使ってると動作確認困難なので繋げて実行する
$tmp = [Console]::BackgroundColor; `
[Console]::BackgroundColor = [Console]::ForegroundColor; `
[Console]::ForegroundColor = $tmp; `
[Console]::Write("こんちはーっ!!`n"); `
[Console]::ResetColor(); `
[Console]::Write('ザバーッ');
```

もう 1 つは [ANSI escape sequences](https://en.wikipedia.org/wiki/ANSI_escape_code) を直に使う方法。
PowerShell 直で使うと `` `e `` が ANSI escape sequence 。
[Escape (`e) | about Special Characters - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_special_characters?view=powershell-7.4#escape-e)

`` `e7m `` で反転、 `` `e[27m `` で元に戻すらしい。
[Using ANSI Escape Sequences in PowerShell :: — duffney.io](https://duffney.io/usingansiescapesequencespowershell/)

1 つ目と完全に同じではないが、よりできることが多い。その代わり先に挙げた個々の ANSI Escape Sequence を理解して使う必要があり、~~めんどさ~~制御の煩雑さが伴う。

```powershell
[Console]::Write("`e[7mこんちはーっ!!`e[27m`nザバーッ")
```

2 つ目の利点は ANSI Escape Sequence を組み込んだ文字列さえ組み立ててしまえば、一発で印字できるところ。
pocof での用途を鑑みると、クエリ文字列の部分選択をした箇所だけ前景背景を反転させる必要があるので、そのためだけに印字を分割しなければならない前者は採用できないかなーというのがでかい。

(
現に今の pocof のもっさりレンダリングは書き込み回数が小分けされてることに因る...
PSReadLine パイセンは一気に印字スタイルぽいので、ふつーそっちに寄せるのが一般的なんやろな [PSReadLine/PSReadLine/Render.cs](https://github.com/PowerShell/PSReadLine/blob/5efe2ef55f85bbac9c8a8f39825ad62b3049b0a5/PSReadLine/Render.cs#L722)
)

ちょいと前例というか PSReadLine パイセンを調べたところ、こちらも ANSI Escape Sequence を使っていた。なので方向性は合ってる。
PSReadLine は [VSColorUtils](https://github.com/PowerShell/PSReadLine/blob/5efe2ef55f85bbac9c8a8f39825ad62b3049b0a5/PSReadLine/Cmdlets.cs#L1048) というクラスにその辺こ ANSI escape sequences をまとめて使ってる。
pocof でもそんな多機能はいらんにしてもまとめる必要はあるかな。

F# で書く場合、 ANSI Escape Sequence は PowerShell と違って C# と同じように `\x1b` を送るのが一般的ぽい。
C# では `\e` を ANSI escape sequence に使える機能が増えてる様子。
だが機能が新し過ぎるのもあってか、昔からやってる PSReadLine では直に ANSI escape sequence を送る `\x1b` を使ってるようだ。

[[Proposal]: String/Character escape sequence `\e` as a short-hand for `\u001b` (`<ESCAPE>`) · Issue #7400 · dotnet/csharplang](https://github.com/dotnet/csharplang/issues/7400)

これらを踏まえて F# でやるなら以下の形式の出力を、なんかの抽象化層を通してやる感じか。

```fsharp
Console.Write("\x1b[7mこんちはーっ!!\x1b[27m\nザバーッ");;
```

あとはどっからどこまでを反転するかの選択範囲を pocof 内部の状態に追加して、 [`Handle.fs`](https://github.com/krymtkts/pocof/blob/6383b3efcd812593f5110ea7fbf18ac834fabc67/src/pocof/Handle.fs) でキーに応じてよしなに更新操作するってところかー。

とりま方針は定まったな。
ついでにレンダリング効率悪いのも解決できたらなんか使い勝手良さそう(今クソ遅いし)。

続く。
