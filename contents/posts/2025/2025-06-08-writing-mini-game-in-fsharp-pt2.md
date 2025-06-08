---
title: "F# でミニゲームを書いてる Part 2"
tags: ["fsharp", "game", "dotnet"]
---

[前回](/posts/2025-05-18-i-want-to-write-mini-game-in-fsharp.html)から始めた Game of Life の実装がようやく一歩前進した。
キャプチャは以下の通り。

![game capture](https://raw.githubusercontent.com/krymtkts/PSGameOfLife/0e4d2c04a578572c3e060e3dc93416b31967d9b0/docs/images/psgameoflife.gif)

[PowerShell Gallery](https://www.powershellgallery.com/) に [v0.0.1](https://www.powershellgallery.com/packages/PSGameOfLife/0.0.1) として publish したので、 [repository krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) も public にした。

結局ゲームの提供方法は PowerShell Gallery を利用することにした。
つまり Cmdlet で実行できるゲームということだ。
仕事をサボってプレイする PowerFighter(勝手に作った PowerShell ユーザの呼称) が出てくること間違いなし。
自分の terminal で Game of Life を実行できるのはなんか良い。
まだ単に眺めるだけのゲームだが、気に入っている。

実装力のショボさに故に CUI 版の実装自体に 1 週間ほどかかった。
その後時間が確保できなくて publish の準備に取りかかれず 2 週間ほど放置、久しぶりに暇を確保したので、今日公開にこぎつけられた。

開発を通して Game of Life が極めて興味深い題材であり、作っていてのめり込んでしまう、いわば沼であることもわかった。
John Conway のルール以外にも様々なルールが有り、また初期配置もランダム・特定のパターン・アルゴリズムを利用するものがあるのを知った。
[LifeWiki](https://conwaylife.com/wiki/) が極めて勉強になるし、読んでて楽しい。

今回作ったのはスタンダードな [B3/S23 Conway's Life](https://conwaylife.com/wiki/Conway%27s_Game_of_Life) という rule pattern みたい。
rule pattern は指定できるようにしてみたいな。
あと指定のパターンを読み込んで再生できるのが一般的らしい。

このように、掘り下げていくと永久に GUI へたどり着かなさそうなので、とりあえず簡単に指定できる仕組みだけ作り GUI へ移ることにした。
GUI の実装ができたらまた戻ってくればいい。

CUI 版の開発は [pocof](https://github.com/krymtkts/pocof) と同じように [PSReadLine](https://github.com/PowerShell/PSReadLine) スタイルを採用した。
カーソル下に行追加して screen を作り、処理が終わったら掃除をする方法だ。
はじめはゲーム盤のサイズを指定できるようにしていたが処理が面倒すぎるのでコンソール全面を利用するように変えた。
世代交代を直列で計算してるし、描画も全面的な書き直しをするようになってるので、処理効率は良くない。
でも Windows Terminal では全画面にしても比較的 CPU load も低く、快適な動作になってた。
Visual Studio Code の terminal で試すとちょっと描画が重過ぎるみたいで、 Windows Terminal のように CPU load は低くならない既知の制約がある。

PSGameOfLife は pocof と違って [`TargetFramework`](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) を `net8.0` にしている。
[サポート中の PowerShell](https://learn.microsoft.com/ja-jp/powershell/scripting/install/PowerShell-Support-Lifecycle?view=powershell-7.5#powershell-end-of-support-dates) を対象にしつつ、この先の GUI 化を見据えると `net8.0` を下限にしておくのが無難そうだったからだ。
まだ使うの確定ではないが [Avalonia.FuncUI](https://github.com/fsprojects/Avalonia.FuncUI) は [`net8.0` が下限](https://github.com/fsprojects/Avalonia.FuncUI/blob/213d336c0654b66afe4ff430e05f48cd1f667c08/src/Avalonia.FuncUI/Avalonia.FuncUI.fsproj)みたい。
結果、 Windows PowerShell では使えなくなったので至極残念。なんか回避方法あれば提供できる可能性ある。
PowerShell 7.4 以降に対応していて、一応 Windows と Ubuntu on WSL2 では動作確認できている。

因みにこの `net8.0` に対応するのは思ったより手間取った。
初めはゲーム本体の project だけ `TargetFramework` `net8.0` にして、 test project は `net9.0` にしてた。
これだと何故か `System.Runtime` version 9.0.0.0 への参照が残ってしまい、 PowerShell 7.4 で `Import-Module` するときエラーになってしまった。
結局どちらも `net8.0` にすることで事なきを得たが、 test project の設定が何故影響したのか不明。

これで遂に GUI programming に着手する準備が整ってしまったワケだ。
やれない理由がなくなってしまったし、そろそろ始めるかーという感じ。
test coverage はゲームのコア機能しかカバーしてなくて UI 部分をやってないので低いし、 UI 部分の interface 整理も含めて GUI 版の実装を進めるイメージでいる。
pocof でもそうだったが [`Console`](https://learn.microsoft.com/en-us/dotnet/api/system.console?view=net-9.0) を使う部分をいい感じの testable にするの、未だにいい感じの方法がわかってないから、そこも模索する。
いきなり GUI を PSGameOfLife に組み込み始めるのか、なんか sample 的な repo で練習してみるかは、後で考えよう。

続く。
