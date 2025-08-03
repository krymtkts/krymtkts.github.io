---
title: "F# で Cmdlet を書いてる pt.68"
subtitle: "WaitHandle を使う"
tags: ["fsharp", "powershell", "dotnet"]
---

今週はまるで開発の時間が取れなかったが、久しぶりに [krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。
[krymtkts/PSGameOfLife](https://github.com/krymtkts/pocof) でいくつか高速化に関わる Tips を手に入れたので、それを輸入しようという魂胆だ。
ただ今のところは、それらをすぐに pocof で実現するのは難しそうだ、という結論になった。

ひとつは、 [`Console.Out`](https://learn.microsoft.com/en-us/dotnet/api/system.console.out?view=net-9.0) を [`StreamWriter.AutoFlush`](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamwriter.autoflush?view=net-9.0) を無効にした [`StreamWriter`](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamwriter?view=net-9.0) で置き換える方法。
書き込み完了後に [`Flush`](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamwriter.flush?view=net-9.0) することで [`Console`](https://learn.microsoft.com/en-us/dotnet/api/system.console?view=net-9.0) の書き込み性能を高められる。
PSGameOfLife の CUI では非常に上手くいったが、 pocof の方は単純に導入できるものではなさそうだった。
[`Console.WriteLine`](https://learn.microsoft.com/en-us/dotnet/api/system.console.writeline?view=net-9.0) を使わないと 1 行の改行が適切に出力されない[^1]ため、狙った印字にならない。
pocof は初期カーソル位置下部に必要な数の行を出力して描画スクリーンを作っている関係で、 `Console.WriteLine` で自動で出力される改行と相性が悪いので、その調整が必要そう。
閾値の調整位で済めば導入も容易いのだけど、ややこしい部分なので億劫だ。

[^1]: 確か改行コードや ANSI escape sequence でもダメだったと記憶だが今度再確認する

もう 1 つは [async expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions) を [task expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) に置き換える方法。
置き換えるだけなのでとても簡単なのだが、 pocof では F#らしく async expression と末尾再帰による無限 loop をしているので、 task だと末尾再帰を最適化できない。
つまり再帰をやめて [`while`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/loops-while-do-expression) に変える必要がある。
それは性能のために F# らしさをやめるということで、 pocof ではどうするかというのが悩ましい。
速いは正義なのでそれでいいのだけど、もうちょっと悩んでもいいかなと思ってやらなかった。

---

そんな感じで打つ手がなくなってしまった。
仕方ないので、これらの代わりに pocof での main loop(UI Thread) に相当する箇所の効率化を試しているところ。
最早 PSGameOfLife から得たアイディアでもない。
外出中に pocof のコードを読んでて [`Thread.Sleep`](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.thread.sleep?view=net-9.0) があるのを思い出したので、これは良くないなと GitHub Copilot に相談して出てきたアイディア。
再帰による無限 loop で `Thread.Sleep` を使っていたところを [`WaitHandle`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.waithandle?view=net-9.0) で置き換えることで、 CPU の無駄使いを取り除く。
[#347](https://github.com/krymtkts/pocof/pull/347)

```diff
diff --git a/src/pocof/Pocof.fs b/src/pocof/Pocof.fs
index d31c9c9..b29993b 100644
--- a/src/pocof/Pocof.fs
+++ b/src/pocof/Pocof.fs
@@ -188,6 +188,8 @@ module Pocof =
         let renderStack: RenderEvent Concurrent.ConcurrentStack =
             Concurrent.ConcurrentStack()

+        let event = new AutoResetEvent(false)
+
         [<TailCall>]
         let rec getLatestEvent (h: RenderEvent) (es: RenderEvent list) =
             match h with
@@ -205,9 +207,14 @@ module Pocof =
             | [] -> RenderMessage.None
             | h :: es -> getLatestEvent h es |> RenderMessage.Received

-        member __.Publish = renderStack.Push
+        member __.Publish e =
+            renderStack.Push e
+            event.Set() |> ignore
+
+        member __.Receive(block: bool) =
+            if block then
+                event.WaitOne() |> ignore

-        member __.Receive() =
             let items =
                 match renderStack.Count with
                 // NOTE: case of 0 is required for .NET Framework forward compatibility.
@@ -222,9 +229,12 @@ module Pocof =

     [<TailCall>]
     let rec render (buff: Screen.Buff) (handler: RenderHandler) =
-        match handler.Receive() with
+        match handler.Receive(block = true) with
         | RenderMessage.None ->
-            Thread.Sleep 10
+            // NOTE: for backward compatibility.
+#if DEBUG
+            Logger.LogFile [ "render received RenderMessage.None." ]
+#endif
             render buff handler
         | RenderMessage.Received RenderEvent.Quit -> ()
         | RenderMessage.Received(RenderEvent.Render(state, entries, props)) ->
@@ -257,7 +267,7 @@ module Pocof =
         | StopUpstreamCommands

     let renderOnce (handler: RenderHandler) (buff: Screen.Buff) =
-        match handler.Receive() with
+        match handler.Receive(block = false) with
         | RenderMessage.None -> RenderProcess.Noop
         | RenderMessage.Received RenderEvent.Quit -> RenderProcess.StopUpstreamCommands
         | RenderMessage.Received(RenderEvent.Render(state, entries, props)) ->
```

元々 `Thread.Sleep` していた箇所は取り除き、 [`WaitOne`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.waithandle.waitone?view=net-9.0) で待ち受ける。
[`ProcessRecord`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.processrecord?view=powershellsdk-7.4.0) から呼び出される描画処理を詰まらせないために non-blocking な option も提供する。

いい感じなのだが、これらの差分はまだ実装途中で merge もしてない。
というのも pocof は結構 coverage が厳しいので、 engine 部分以外はゆるい PSGameOfLife と違い下手は変更が許容されないのを忘れてた。
`RenderMessage.None` は発生することがないので消したいがまだ消せてない。おかげで coverage が低下している。なんとかせな。

自分が書いてる F# の中では poof は legacy な部類なので、この様な形でちょいちょい更新していけたら良いなと考えている。

多分続く。
