---
title: "F# でミニゲームを書いてる Part 9"
tags: ["fsharp", "game", "dotnet", "avalonia"]
---

[krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) の開発をした。

世代交代の並列化と main loop を [async expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions) から [task expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) に置き換えた。
[#17](https://github.com/krymtkts/PSGameOfLife/pull/17)
これで瞬間最大風速 3000 FPS 超えまできた(1 cell 10x10 dot の 50 x 50)。 2017 Razer blade stealth で。
速いなー。つまり理論上 300 ~ 400ns で 1 frame 描いてるのか。
これまで F# で書いてきたプログラムは `async` と `task` を使わけなくても `async` で十分速いケースが多かった。
また `async` の方が機能のサポートが手厚いし中々 `task` を使うことがない。
でも実際に 1ms 以下の処理時間が必要なケースだと `task` の方が .NET の最適化や overhead の少なさで速いみたい。
もっと積極的に `task` を使ってもいいなと。いい経験になった。

ただ、この改善により [UIThread](https://docs.avaloniaui.net/docs/guides/development-guides/accessing-the-ui-thread) が詰まるという現象が発生、 window を終了できなくなる問題が再発した。
今回のは何のことはない、 [`Dispatcher`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Threading_Dispatcher) で実行する優先順位が [`Render`](https://api-docs.avaloniaui.net/docs/F_Avalonia_Threading_DispatcherPriority_Render) では高すぎて、 closed 等の event を block してしまうみたい。
代わりに何を選ぶべきか、提供されてる [`DispatcherPriority`](https://api-docs.avaloniaui.net/docs/T_Avalonia_Threading_DispatcherPriority#fields) の文書を見てみたが、順位がよくわからなかった。書いてないようだけど。

結局 [Avalonia の `DispatcherPriority` のコード](https://github.com/AvaloniaUI/Avalonia/blob/a15eae6833b934f2470d1af1c78fec896a19dc72/src/Avalonia.Base/Threading/DispatcherPriority.cs)を直に参照した。
読み間違いがなければ、優先順位は以下の通りだった。

- 9: `MaxValue`, `Send`
- 8: `Normal`
- 7: (obsolete) `DataBind`
- 6: (private) `AsyncRenderTargetResize`
- 5: (private) `BeforeRender`
- 4: `Render`
- 3: (internal) `AfterRender`
- 2: (private) `UiThreadRender`
- 1: `Loaded`
- 0: `Default`, (internal) `MinimumForegroundPriority`
- -1: `Input`
- -2: `Background`
- -3: `ContextIdle`
- -4: `ApplicationIdle`
- -5: `SystemIdle`, (internal) `MinimumActiveValue`
- -6: `Inactive`, (internal) `MinValue`
- -7: `Invalid`

これに基づいて、強すぎず弱すぎずな [`Input`](https://api-docs.avaloniaui.net/docs/F_Avalonia_Threading_DispatcherPriority_Input) にしておいた。
正直 [`Default`](https://api-docs.avaloniaui.net/docs/F_Avalonia_Threading_DispatcherPriority_Default) でも問題なかろうが、 `Background` 寄りは優先されて他の割り込みを許容するよう、調整してみたつもり。
GUI mode を作ってみて結構 Avalonia は文書に書いてないから、コードを見た方が理解しやすいこともわかってきた。

GUI mode 追加に伴って MAML ヘルプファイル等の文書も整えた。 [#18](https://github.com/krymtkts/PSGameOfLife/pull/18)
あとで PowerShell Gallery に公開しよう。
心配なのは大量の依存関係がある Cmdlet を公開したことがないので、上手くいくのかというところかな。
それも経験しないとわからんので、やってみるしかないな。

今後は、PSGameOfLife には他にも追加していきたい機能があるので、それに着手しようか。
あるいは PSGameOfLife で得た知見を他のツールに反映するか。
