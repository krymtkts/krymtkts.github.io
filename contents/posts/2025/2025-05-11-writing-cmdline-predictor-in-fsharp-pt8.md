---
title: "F# で command-line predictor を書いてる Part 8"
subtitle: "0.4.0"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.4.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.4.0) をリリースした。

v0.4.0 は主に [FSharp.Analyzer.SDK](https://github.com/ionide/FSharp.Analyzers.SDK) とか [Dependabot の `reviewers` の置き換え](https://github.blog/changelog/2025-04-29-dependabot-reviewers-configuration-option-being-replaced-by-code-owners/)ととか、内部的なリソース管理の更新とか、機能に関係ない更新が多かった。
その中で唯一 snippet に設定した group を部分一致で suggestion に表示する機能を追加した。
この機能は、例えば個人の環境と仕事の環境で snippet が違うことはよくあって、その場合に何の group があるかインタラクティブに一覧できるといいかなという動機でつけた。
自分が実際そういう使い方なので、あると記憶を引き出すのが楽かな的な。
仮に利用者が自分でつけた group を覚えてたら使う機会もないだろう。

元々は [`Register-ArgumentCompleter`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/register-argumentcompleter?view=powershell-7.5) みたいな補完候補を出してもらえると良いのではないかと考えた。
でもその場合 `:` を擬似的な native command にして、そこから `commandAst` を分解して～みたいな手順になり、なんか違うなと。コマンドじゃないし。
なので落とし所としては command-line predictor の仕組みに閉じるのが良いかなと判断した。

現状は入力似合わせて `:group` が suggestion に表示されるのみなので、もうちょっとリッチな表現にできんかな―という気もする。
が、その場合 `.snippet-predictor.json` に group に関する付加情報を足すことになりそうだし、一旦やめた。
snippet の先頭数文字を切り出して tooltip に足してもいいかな。幅が全然足りなそうだけど。
さらに言えば suggestion の表示優先順位を設定したいが、 command-line predictor にその仕組みがないから現状できない。
そこはキーワード入力して絞り込んでいくことで代替するイメージ。

[v0.2.0 を出したとき](/posts/2025-03-23-writing-cmdline-predictor-in-fsharp-pt5.html)に SnippetPredictor の開発は一段落したかといってそうならなかったが、今回で流石に一段落しただろう。
またなんか新しいアイデア探すか、いま `README.md` が質素なのでその辺を AI に拡充させるとか、そんな感じかな。
