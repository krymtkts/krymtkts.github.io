---
title: "F# で command-line predictor を書きたい"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) はちょこちょこいじってるだけ、また[前回発見した .NET の挙動が文書と違うやつ](/posts/2024-10-06-writing-cmdlet-in-fsharp-pt50.html)も意図した挙動で長くドキュメント不備だっただけらしいので、特に進捗なし。 [dotnet/dotnet-api-docs#10508](https://github.com/dotnet/dotnet-api-docs/issues/10508#issuecomment-2397449030)

なので今日は今後やりたいなと思ってることをメモしておく。

ちょっと前にに Windows Terminal に Snippets Pane ていう機能が来てた。

[Snippets Pane | Windows Terminal Preview 1.22 Release - Windows Command Line](https://devblogs.microsoft.com/commandline/windows-terminal-preview-1-22-release/#snippets-pane)

> Have you ever woken up at 3:00 AM after getting paged for a SEV1 and frantically struggled to remember what commands you need to revive a production cluster? Well, maybe the Snippets Pane can help with that…

意訳するとこう。

> 深夜 3:00 に緊急対応で起こされて本番クラスタを復活するためのコマンドを思い出すのに必死になったことはあらへんか？ Snippets Pane が助けてくれるかも知れへんで...

要はタダのスニペット挿入機能なんやけど、 Windows Terminal の複数プロファイルを束ねる特性と組み合わさることで、様々な環境に対して同じワンライナー(ここでいう snippets)を挿入できる素敵な機能になってる。
登場以来わたしもよく使ってる。

ただ満足してるわけではないねんよな。何故かというとこの機能は個々のシェルの外側にいるので、そのシェルの中で滑らかにつながってない。
郷に入っては郷に従えではないが、そのシェルの中、ここでは PowerShell のお作法に従って滑らかに繋がってるスニペット挿入機能があってほしい。
日常使いする PowerShell は自分のローカル開発環境でしかほぼ使わないから、 PowerShell のスニペットを Windows Terminal に保存しても特に嬉しいことないねんよな。
PowerShell のような人少なめコミュニティでもスニペットみたいなみんなが欲しい機能は当然昔からあるようで、調べたらなんぼでも出てくるのよね。例えば新し目のだと [mdowst/PSNotes](https://github.com/mdowst/PSNotes) とか良さそうよな。

ただ、わたしの場合スニペットに名前をつけて覚えといて、その名前で利用するときに呼び出すなんてことは面倒そうな気がしてる。
Windows Terminal の Snippets Pane を使うまでは履歴から目当てのコマンドを拾って実行したりしてたので、その利用感でスニペットを優先的に表示できたらなんか良さそうよな。
だからやりたいのは [PSReadLine](https://github.com/PowerShell/PSReadLine) の補完候補のところにスニペットを出したい。

少し調べたところ、 PowerShell 7.2 & PSReadLine 2.2.2 から使える command-line predictor という機能を使えばやりたいことができそう。

[How to create a command-line predictor - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.4)

Microsoft 様公式ドキュメントもパッと見充実しており、というかコレ以外はあまりまともそうな資料が見つからないのだけど、これを使って意中の機能を実装できないかなーと検討している。 F# で。
command-line predictor と、追加・削除のための Cmdlet を提供できたら、いい感じに日常使いが回るかなと考えている。
ひょっとしたら世界の何処かで誰かが既に実装してるかも知れないが、見つけられなかったので自作する。

それならまずサンプル実装をしてみようと思ってたのだけど、時間取れず着手できないまま 2 ヶ月近く経過してしまった。
このままではまずい。
ということで、とりあえず今日は日記に書くネタがなかったし、代わりにこの話を書くことで自身に対する実行の強制力を高めようという目論見だ。
日記に書く暇あったら sample だけでもはよ作れよという感じではあるが。

続く。
