---
title: "F# で Cmdlet を書いてる pt.69"
subtitle: "Task.Run と task expression"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

keyboard 入力を読み取り描画イベントを起こす main loop を async + tail recursion から task + while loop に変えた。
はじめ単純に [async expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions) を [task expression](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions) に変えてみたが、処理が始まらなくなってしまった。
これは current thread で即座に task が実行されるから期待しない blocking が発生していたことによるらしい。
task expression は [`do!`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/computation-expressions#do) のような非同期操作を切り替わりのタイミングとして、別の thread に移るらしい。
なので初めから別 thread で動く [`Task.Run`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run?view=net-9.0) で実行するようにし、さらに結果を待ち受けるタイミングも変える必要があった。
考えたこともなかったが、 `Task.Run` と task expression は実行が初めから別 thread に乗るか乗らないかの違いがあるらしい。
これは async expression で開始方法が様々提供されてるのと違う点なのかな。
以下の引用辺りか。

- [Task expressions - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/task-expressions#syntax)
  - > The task is started immediately after this code is executed and runs on the current thread until its first asynchronous operation is performed (for example, an asynchronous sleep, asynchronous I/O, or other primitive asynchronous operation).
- [Task.Run Method (System.Threading.Tasks) | Microsoft Learn](<https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run?view=net-9.0#system-threading-tasks-task-run(system-func((system-threading-tasks-task)))>)
  - > Queues the specified work to run on the thread pool and returns a proxy for the task returned by function.

今回の変更により、元の async + tail recursion から多少の overhead は減ったろうが、 tail recursion 最適化があるし速度への寄与は微々たるものと思われる。
でもこの基礎的な挙動をちゃんと理解できたという点で収穫ありかな。
.NET 力の低さを多少は改善できただろう。

これからやりたいこととしては、やっぱり console output の最適化はやりたいなと考えている。
標準出力の [`StreamWriter.AutoFlush`](https://learn.microsoft.com/en-us/dotnet/api/system.io.streamwriter.autoflush?view=net-9.0) を無効にして、手動で flush するやつ。
[`Console.WriteLine`](https://learn.microsoft.com/en-us/dotnet/api/system.console.writeline?view=net-9.0) を利用するための行数の調整が必要になるので多少難しかろうが。
100 万件を pocof で操作するような自機(Razer blade stealth 2017)の CPU 負荷が高いときだと顕著に描画が遅くモッサリ感あるので、速くしたい。

クエリ構築部分も、正規表現で文字列を分割するようなのでなく、自前で parser を書いて構築するやつにしたい。

あと全体的な高速化を一番やらないといけない。
だが、 state の受け渡し箇所を [`byref`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/byrefs) を使って heap 割当を避けコピー削減するようなパターンしか思いつかない。
この部類の最適化は効果がさほどでないことも多いしあまり期待はできない。
一番遅いであろう `ConcurrentQueue` の絞り込み部分をどうにかせんと極限のパフォーマンスに迫らないのだけど、まだ解決策を考えられてない。
非同期で描画する対応のとき手っ取り早く使える方法として `ConcurrentQueue` を使ったが、読みと書きの thread が別れてるだけなのでより良い方法があるような。
より最適なデータ構造を選択するなり作るのが良さそうな気配はしてる。

今後は、とりま抜本的な改善を GitHub Copilot なんかと相談しつつ、まず実装イメージが掴めている console output の最適化とクエリの parser 実装かな。
