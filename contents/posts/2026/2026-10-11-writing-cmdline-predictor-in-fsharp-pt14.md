---
title: "F# で command-line predictor を書いてる Part 14"
subtitle: "0.10.0 でやりたいこと"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発をチマチマ進めている。

今のところ 0.10.0 を目標に含めたい機能がいくつかある。あと調べたいこと。
主に snippet の検索性能を高めるためのものだ。

- 予測候補の並びを関連度順にする
- 通常入力で snippet を予測しない設定を追加
- AST と token に基づく入力の文脈判定の検証

このうち 2 つまでできた。最後の AST と token を使った文脈判定はまだ検討中だ。

まず、予測候補の並びを関連度順にした。 [#179](https://github.com/krymtkts/SnippetPredictor/pull/179)
これまでは部分一致した snippet を設定の記述順に出していたので、より優先表示したい snippet があっても後ろに出ていた。
今回の改善では、関連度順を完全一致→前方一致→単語先頭の一致→それ以外の部分一致にした。
これで、例えば `git` と入力したら、 `git`、 `git status`、 `Write-Host git`、 `digit` の順になる。
同じ順位なら元の設定順を保つし、検索語がない場合も設定順のままにしている。
使ってみた感触はなかなかイイ。目的とする snippet を引き当てやすい感じはしてるが、個人差が大きいかも知れない。

単語先頭の判定の実装は少し細かくなった。
英数字と `_` は単語の一部として扱い、非 ASCII の文字は Unicode category を見て判定することにした。
文字や数字だけでなく結合文字も単語の一部にするので、例えばアクセント付き文字の途中を単語の先頭と扱わない。
ASCII を優先に扱うのは Unicode category を取ると速度が犠牲になるためでもある。

またコレまでの単純な中間一致からの速度と消費メモリの劣化を最小限にしたくて、 [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet) の benchmark も追加した。
ただ今はまだ benchmark test を CI に組込めてなくて、そこは良くないのでなんとかしたい。

もう 1 つ、通常入力では snippet を予測しない設定を追加した。 [#180](https://github.com/krymtkts/SnippetPredictor/pull/180)

有効にすると、普通に command を入力している間は snippet の予測を出さない。
`:snp` や group identifier、 `:tip` で明示的に検索するときは今までどおり予測する。
わたしは使わない設定だが、 snippet が欲しいときだけ呼び出したいという使い方も選べるようになった。

最後に計画しているのが AST と token に基づく入力の文脈判定の検証だ。
入力している文字列のカーソル位置に応じて予測の表示をするか判定できるようにしたい。

例えば以下のような入力をした場合、今の SnippetPredictor はこの入力から `git` の部分だけを使って snippet を検索できない。
以下の例では `|` をカーソル位置とする。

```powershell
<# xxx #> git|

Write-Host 'xxx'; git|
```

コメント中では補完されなくてもいいけど、コマンド中では補完されるみたいな。
ただこういう入力を terminal でまずやらないし、果たしてこれは有用なのかはまだよくわからん。
なんとなく、先頭の block comment を変えずに後ろだけ差し替えるとか、文字列中の一部を差し替えることができれば、使い道があるかも知れない。
snippet に token の種類を指定できるようにするとかして、その一致した token にのみ出すとか。

今の実装でも [`PredictionContext`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.subsystem.prediction.predictioncontext?view=powershellsdk-7.4.0) を受け取っているが、 AST が持つ構文の情報を予測条件に使ってない。
今は `context.InputAst.Extent.Text` を使って AST から入力全体の文字列を取り出して検索に渡しているだけ。
そこを変えたら多分機能的には実現できるねんよな。

意味ない機能だと判断できたら、現状の改善点だけで 0.10.0 をリリースするつもり。
