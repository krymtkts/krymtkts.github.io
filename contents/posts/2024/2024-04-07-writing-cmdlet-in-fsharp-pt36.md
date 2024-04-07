---
title: "F# でコマンドレットを書いてる pt.36"
tags: ["fsharp", "powershell", "dotnet"]
---

最近の[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をまとめる。

主だったものでいうと、[クエリ文字列選択](https://github.com/krymtkts/pocof/issues/44)のプロトタイプを実装した。
それと Windows Terminal で `Ctrl+S` して出力を停止しているときに type すると pocof がエラーで落ちる様になってたので、その修正をした。 [#160](https://github.com/krymtkts/pocof/pull/160)

これらとカバレッジの改善などを含む 0.11.0 をリリースした。

---

クエリ文字列選択は、以下の新しいショートカットで実装している。

- `Ctrl+A` 全選択
- `Shift+Home` カーソル以前全ての文字列を選択
- `Shift+End` カーソル以降全ての文字列を選択
- `Shift+LeftArrow` カーソル前方の 1 文字を選択
- `Shift+RightArrow` カーソル後方の 1 文字を選択

あと削除系の操作に選択範囲がある場合の振る舞いを追加した。

- `Backspace` 選択範囲を削除
- `Delete` 選択範囲を削除
- `Alt+K` 選択範囲とカーソル以前全ての文字列を削除
- `Alt+U` 選択範囲とカーソル以降全ての文字列を削除

選択範囲がある場合のカーソル前後すべてを消すやつは、 [PSReadLine](https://github.com/PowerShell/PSReadLine) でも見ない挙動だろうからちょっと変わり種だ。
(PSReadLine の `BackwardDeleteInput` `ForwardDeleteInput` は選択範囲を消さない)
ただ自分の感覚だと削除系の挙動が統一されてないと感じるので、選択範囲を消すような実装とした。
(余談だがショートカット割当を雑に決めてしまってるので、そこもそのうちデフォ設定を変えたい)

あとプロトタイプと書いてるのは、選択モードでもプロパティ補完候補の振る舞いを決めてないからだ。
いまのところ通常のカーソル位置にあるプロパティ補完候補の表示が働いている。

選択範囲があるときに Tab 補完したら、選択範囲を消して補完候補を出したらそれなりに便利な気がするが...そうしたら補完候補は常に表示しておきたいか。
逆に選択中で補完候補を出したくないときってどんなとき？まだちょっと利用想定を詰めるのが甘いか。もうちょっと煮詰まったら実装したい。

---

あと [#160](https://github.com/krymtkts/pocof/pull/160) のバグ修正は、 Windows Terminal で `Ctrl+S` したあとで何か type したときに発生してたエラーを防ぐものだ。
この年まで知らなかったのだけど、昔は標準出力の印字を紙にしてたから、それを一時停止するための `Ctrl+S` と再開するための `Ctrl+Q` があったそうだ。
そのため伝統的に terminal emulator でも同機能を実装しており、しかも割と現代でもそれを使う人もいるということだった。
(なんか `tail -f` で流しているようなとき気になる箇所で止めたりする使い方らしい)

- [terminal - What is the point of Ctrl-S? - Unix & Linux Stack Exchange](https://unix.stackexchange.com/questions/137842/what-is-the-point-of-ctrl-s)
- [terminal - History of Ctrl-S and Ctrl-Q for flow control - Retrocomputing Stack Exchange](https://retrocomputing.stackexchange.com/questions/7263/history-of-ctrl-s-and-ctrl-q-for-flow-control)

Windows Terminal でもその実装があるみたいで、 `Ctrl+S` で出力停止、何らかのキー押下で再開するらしい。
この振る舞いに詳しく触れている文書を見つけられなくて、以下で触れられてるのくらいしかなかった。
Windows Terminal の repo に PR があるからどっかで触れられてていいと思うが。
[Re-investigate Ctrl+S pausing · Issue #809 · microsoft/terminal](https://github.com/microsoft/terminal/issues/809)

何にせよ `Ctrl+S` 押下したあと `Console.ReadKey` が呼ばれると `InvalidOperationException` が発生する。ドキュメントにも書いてある。

[Console.ReadKey Method (System) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.console.readkey?view=net-8.0)

> InvalidOperationException
> The In property is redirected from some stream other than the console.

これどうやったら防げるんや？と調べてたら、単純に pocof の実装がバグってるだけだったんがわかった ↓ [Fix Screen.read function to prevent an unhandled error when output is… · krymtkts/pocof@60843b9](https://github.com/krymtkts/pocof/commit/60843b9dc7f44ee68cc845c235be8b321b1ca35e)

```diff
        [<TailCall>]
         let rec read (acc: ConsoleKeyInfo list) =
-            let acc = rui.ReadKey true :: acc
-
-            match rui.KeyAvailable() with
-            | true -> read acc
-            | _ -> List.rev acc
+            rui.KeyAvailable()
+            |> function
+                | true ->
+                    let acc = rui.ReadKey true :: acc
+                    read acc
+                | _ -> List.rev acc
```

`rui.ReadKey` は `Console.ReadKey` 、 `rui.KeyAvailable()` は内部で `Console.KeyAvailable` を呼んでる。
元のコードだと、初回は必ず `Console.KeyAvailable` を見ずに `Console.ReadKey` を呼んでしまうので、そこでエラーになってた。
修正したコードでは必ず `Console.KeyAvailable` のチェックのあとで呼ばれるので、エラーにならない。

`Console.KeyAvailable` を改めてみてみたら、ちゃんと標準入力でキーが有効かチェックしてて、そらそうやなという感じ...ただの実装ミスやったと。
[Console.KeyAvailable Property (System) | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.console.keyavailable?view=net-8.0)

> Gets a value indicating whether a key press is available in the input stream.

しょぼバグを直せて良かった。

---

次にやるのは、ひとまず選択中のプロパティ補完の振る舞いを詰める。
その次は起票済みの機能よりも、 PSReadLine の Word 単位の移動や削除を模してみるか。
自分で起票したけど行選択やページ移動はあまり気乗りしない。
