---
title: "F# で Cmdlet を書いてる pt.53"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

---

その前に、毎年恒例 .NET の新しい major が出ましたな。

[What's new in F# 9 - F# Guide - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9)

Nullable reference がでかいのかな。 リリース後ブログになってたし。 [Nullable Reference Types in F# 9 - .NET Blog](https://devblogs.microsoft.com/dotnet/nullable-reference-types-in-fsharp-9/)

個人的には [Enforce attribute targets](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9#enforce-attribute-targets) はよく間違うのでありがたそう。

pocof 的には [Optimized equality checks](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9#optimized-equality-checks) [Field sharing for struct discriminated unions](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9#field-sharing-for-struct-discriminated-unions) あたりの IL の改善が効果でたらうれしいな。
equality の最適化は半年ほど前ブログにもなってて、すご...と思ったやつだ。 [F# developer stories: how we've finally fixed a 9-year-old performance issue - .NET Blog](https://devblogs.microsoft.com/dotnet/fsharp-developer-stories-how-weve-finally-fixed-a-9yearold-performance-issue/)

[Enhancing #help in F# Interactive - .NET Blog](https://devblogs.microsoft.com/dotnet/enhancing-help-in-fsi/) もブログになってたやつ。
[Extended #help directive in fsi to show documentation in the REPL](https://learn.microsoft.com/en-us/dotnet/fsharp/whats-new/fsharp-9#extended-help-directive-in-fsi-to-show-documentation-in-the-repl)
早速使ってみてる。

---

先に .NET の最新リリースに触れたが、 pocof でも毎年恒例の .NET 更新をやりたいので、それに合わせて今着手中の開発を一区切りして [0.17.0](https://www.powershellgallery.com/packages/pocof/0.17.0) でリリースした。

含まれるのは、 word 操作。これは自分の日常使いで早速使い込む予定。
あとはキーに対応づいた操作の、内部コードのリファクタリングをしてた。
例えば、対称な処理が個別の実装になってるのを 1 つにまとめたり。
クエリ文字列を操作するあたりはまだゴチャついてて、もっとよくできる直感があるけど、リリースのために後回し。

リリース自体は問題なくいったのだけど、リリース前に既存バグを見つけたので、リリース後にその対処をした。
ずっと気づいてなかったが、 pocof のいくつかの option に `$null` を渡すと validation されてなくて `NullReferenceError` が出たりしてたので、カッコ悪かった。
とりあえずこいつは直した。 [#261](https://github.com/krymtkts/pocof/pull/261)

その過程で知ったのだけど、 `pocof` の switch parameter に `$null` を渡したら truthy な動作をするのよね。
なんとなく `$false` が渡されたときみたいな挙動になるのかと思いきや、 `$true` を渡されたような挙動になる。

あんまり switch parameter に `$null` を渡すような変なことしないようなので、以下の辺りがわたしが探してることに近そうかな...という感じはしてるが、まだよくわかってない。

- [Reconcile -Switch:$false parameter invocation with parameter set resolution · Issue #16852 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/16852)
- [PowerShell Switch type never $null | Keith's Consulting Blog](https://keithga.wordpress.com/2017/10/09/powershell-switch-type-never-null/)

この件に関しては大した情報もなさそうやと思ってコード見に行ったのだけど、まだ追いきれなくてよくわからんかった。この辺は宿題としたい。
一応 PowerShell で簡易的に switch parameter の挙動をチェックする PowerShell の関数を作ったのだけど、こいつだと falsy な挙動になる。なんでや...ひょっとしたら pocof のバグなんかも。
あるいはバイナリ Cmdlet だと違うとか？めんど...宿題。

```powershell
function test-switch ([switch]$Switch) {
    "is null=$($Switch -eq $null)"
    "is false=$($Switch -eq $false)"
    "is true=$($switch -eq $true)"
    "isPresent=$($Switch.IsPresent)"
}

# 狂気のワンライナー
Write-Host 'plain'; test-switch -Switch; Write-Host '$false'; test-switch -Switch:$false; Write-Host '$true'; test-switch -Switch:$true; Write-Host 'null'; test-switch -Switch:$null
# plain
# is null=False
# is false=False
# is true=True
# isPresent=True
# $false
# is null=False
# is false=True
# is true=False
# isPresent=False
# $true
# is null=False
# is false=False
# is true=True
# isPresent=True
# null
# is null=False
# is false=True
# is true=False
# isPresent=False
```

続く。
