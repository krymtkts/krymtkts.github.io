---
title: "F# でコマンドレットを書いてる pt.22"
tags: ["fsharp", "powershell"]
---

[pocof](https://github.com/krymtkts/pocof) の開発をした。

- [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) で素振りした [dependabot](/posts/2023-10-29-rebuild-blog-with-fable-pt19.html) の導入 [#62](https://github.com/krymtkts/pocof/pull/62) [#63](https://github.com/krymtkts/pocof/pull/63) [#64](https://github.com/krymtkts/pocof/pull/64) [#69](https://github.com/krymtkts/pocof/pull/69)
- キー入力に async expression を利用する [#67](https://github.com/krymtkts/pocof/pull/67)

キー入力に async expression を利用する、がメイン。
[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の開発で F# の async expression を触ったので、ちゃんと F# らしく pocof を改善してこうという気になった。

元々はキー入力されるまで [`Console.KeyAvailable`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.keyavailable?view=net-7.0) でチェックして無限に再帰する形で実装してたのだけど、このコメントによれば、どうも継続を使ったら非同期にできるらしい。
[asynchronous - Async to wait for a key in F#? - Stack Overflow](https://stackoverflow.com/questions/56398388/async-to-wait-for-a-key-in-f)

回答者が [Invoke-Build の nightroman サン](https://github.com/nightroman/Invoke-Build)というのも渋い。ぜひ使いたい。

[`Async.FromContinuations`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync.html#FromContinuations) でキー入力待ちを非同期にして [`Async.RunSynchronously`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-control-fsharpasync.html#RunSynchronously) で結果を待ち受ける。
これによって無限ループで無駄に CPU 回していたやつが回避される、と。
ひとまずこうした。

```fsharp
            let getKey () =
                Async.FromContinuations(fun (cont, _, _) -> Console.ReadKey true |> cont)
                |> Async.RunSynchronously
```

これでキー入力してないときの CPU 使用率はかなり改善された。

元々コピペした文字列を [`Console.ReadKey`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.readkey?view=net-7.0) で 1 文字ずつ読み取って表示の処理まで回してるからパフォ良くない課題がある。
今回の非同期化で幾分シンプルになったし、 [`Console.KeyAvailable`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.keyavailable?view=net-7.0) と再帰を組み合わせて文字列丸ごと読み取ることで改善できそうな雰囲気も感じてきた。

因みに [Async expressions - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions) にはこういう定石パターンみたいなの載ってなかったので、今回のように実例から学ぶのがいいのかな。

今回のキー入力を非同期にしたやつは、先にググったらそのまま良さそうな snippet が見つかったから GitHub Copilot には相談しなかった。
でも次は聞いてみたらなんか良いコード出してもらえるかもな。

他に [Lazy Expressions - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lazy-expressions) なんかも試したいけど pocof に活用できることないかもな...という直感。まずコードを漁って実例から学ぶとしよう。
