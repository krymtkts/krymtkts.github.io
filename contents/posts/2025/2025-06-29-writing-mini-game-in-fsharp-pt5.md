---
title: "F# でミニゲームを書いてる Part 5"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発をした。あんまりやれてないが。

終了のためのショートカットを追加したり、世代とかのステータスを表示できるようにしてみたらぐっと使いやすくなった。
[#4](https://github.com/krymtkts/PSGameOfLife/pull/4)

そこで改めて気付いたが `Interval` 0ms にしていると描画は極めて高速でも終了まで時間がかかるようになった。処理が多すぎて UI(main) thread が詰まるみたい。
0ms の利用を推したいわけじゃないが、あまり気持ち良くないのでもう少し最適化できないか調べてみている。

とりあえず、 cell の数が多いほど計算量が増える状態なのは GitHub Copilot の指摘でもあったので、 1 frame 毎の演算量を減らしてみるとかその他の最適化を試みた。
GitHub Copilot の提案も含め以下を施した。

- cell の生死を template 化して copy する
- 描画処理の並列化(描画反映が重ならないので可能)
- 描画処理の SIMD 命令利用

これらを [#5](https://github.com/krymtkts/PSGameOfLife/pull/5) で実装してみた。
GitHub Copilot は並列化をやたら推すが、他のアイデアについては提案してくれなかったのでこちらからあれこれどうかと聞いてみた感じ。
なんとなーく、段階的に高速化するような感じで 1 つずつ提案してくれるつもりだったのかも知れないが、そんな悠長なことはしてられないので。

F# で low layer は書いたことなかったので GitHub Copilot のレクチュアのもと行った。
1ms 以下の演算となると CPU キャッシュがとかメモリの転送量がとか、GC で address が変わるからコピーする間はピン留めする必要があるとか色々興味深く、中々いい経験になる。
これらの要素を何となく理解できることから、改めて [プログラマーのための CPU 入門](/booklogs/what-a-programmer-should-know-about-the-cpu.html) を読んでて良かったなと身にしみて感じた。
ちょっと残念だったのが、これは [Avalonia](https://avaloniaui.net/) の話だが [`WriteableBitmap`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_Imaging_WriteableBitmap) の描画が 1 行単位にしか行えないらしいこと。
お陰で 1 cell 10x10 を 10 回に書き分ける必要があった。
まだ `WriteableBitmap` のことはいまいちわかってないからこれが本当に最善手かわからないし、もっと効率的にできる方法があるかもな。

これらの対応によって、 Razer Blade Stealth 2017 でも随分速くなった。
ゲームのセルの数が デフォ 50x50 だと 120 FPS だったのが 400 FPS ~ に、 100x80 だと ~ 60 FPS が 120 FPS ~ に高速化されてる。

最適化に際して、描画処理の速度を測るため FPS を計測・表示する仕組みを作った。
昔 Avalonia の `Windows` には `VisualRoot.VisualRoot.Renderer.DrawFps` が生えてたらしい。
これは [Avalonia.FuncUI](https://github.com/fsprojects/Avalonia.FuncUI/blob/f86eea3f4b14c72ebb2d27f0143c4a21c896baf1/src/Examples/Elmish%20Examples/Examples.Elmish.GameOfLife/Program.fs#L34) の sample でもコメントの状態で見られる。
でも今の Avalonia 11 にはそれがなくなってるのか見つけられなかったので、自前で簡単なのを作った。
FPS の計算と表示による負荷が気になったのだけど、 GitHub Copilot 曰く微々たるものとのことなので許容した。

あと初めて SIMD 命令を使ったので初めて知ったのだけど、 .NET は Release build じゃないと JIT で SIMD の最適化しないみたい。
これについて触れられた文書はまだ探せてない。
Debug build だと SIMD の最適化がないのか、デフォのセル数で 250 FPS くらいになってた。かなり違う。
実際に利用する場合の性能を測りたい。
ということで、最初は `#if DEBUG` だけ FPS を表示してたのだけど、独自の symbol を指定した場合も表示できるようにした。

```sh
dotnet build -p:DefineConstants=SHOW_FPS
```

こういう command で独自の symbol を指定できる。
ただ PSGameOfLife では [psake](https://github.com/psake/psake) に build task を一任してる。
そのため `dotnet` 直じゃなく parameter を追加した場合のみ `DefineConstants` を追加できるようにしている。

現時点でも結構いい感じではなかろうか。
でも元々課題に思ってた終了処理が詰まるのは多少軽くなったけどまだ詰まったまま。
それと [Ionide.Analyzers](https://github.com/ionide/ionide-analyzers) の警告に対応してなかったりするので、その辺直せたら PR を merge しようかな。
週末時間取れない日が続くが少しずつ進められてるところも、中々良い。
