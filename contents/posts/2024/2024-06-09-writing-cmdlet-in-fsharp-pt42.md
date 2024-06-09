---
title: "F# でコマンドレットを書いてる pt.42"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[前回](/posts/2024-06-02-writing-cmdlet-in-fsharp-pt41.html) 考えた方針で基本実装できたので、諸々の TODO は残しているけど一旦 pull request を merge して区切りをつけた。
とはいえ結構直さないといけないところがいっぱいなので、 [#177](https://github.com/krymtkts/pocof/issues/177) の issue に関連付ける形でいくつか pull request を積んでいっているところ。

実装に関しては思ったよりも制御が難しかったが、以下に示すような延々と `WriteObject` してくるような Cmdlet が上流にいても、即座に起動してユーザ入力を受け付けられるようになった。
これはつまりとんでもなくデータ量が多くても即座に起動できることを意味する。

```powershell
function Invoke-InfiniteLoop {
    [CmdletBinding()]
    param (
        [Parameter()]
        [scriptblock]
        $ScriptBlock
    )
    end {
        while ($true) {
            if ($ScriptBlock) {
                & $ScriptBlock
            }
        }
    }
}

# 100 ms 毎に WriteObject するので Select-Object 以外の通常の Cmdlet は止まれない
# pocof は Cancel action を使えば終了できる
Invoke-InfiniteLoop -ScriptBlock {$global:a += 1; $a; Start-Sleep -Milliseconds 100} | pocof
```

個人的に、ちょっとしたデータ量の通常利用時でも初回のレスポンス向上になった気がして、良いかもと感じている。
ただ現状ユーザ入力がないと再描画しないので、ユーザ入力 ≒ 操作がないと定期的に再描画する仕組みが必要で、それは TODO として積んでいる。

---

最近の pocof 実装はそんなに悩むことなのだけど、今回の非同期レンダリングは比較的悩みごとが多かった。腕が不足してるんやろなと感じる。

非同期コレクションを生で [PSCmdlet](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.pscmdlet?view=powershellsdk-7.4.0) 継承クラスから使うのはいまいちかなと思って wrapped type を作ったのだけど、 F# 的にこのアプローチで良かったのかなと感じながらやってた。
でも [FSharp.Core/mailbox](https://github.com/dotnet/fsharp/blob/main/src/FSharp.Core/mailbox.fs#L361-361) みたいな非同期処理 utility も同じアプローチでカプセル化してるし、多分そんなに変ではないはず。

他にも、 [`Cmdlet.EndProcessing`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.endprocessing?view=powershellsdk-7.4.0) を [`Option.iter`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-optionmodule#iter) や [Pattern matching function](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/match-expressions) の中で使おうとしてスコープが解決出来ないのを知ったり。 method 呼び出しの場合暗黙で第一引数に instance が渡されるから、スコープが違うと protected method は呼べないらしいな。初めて知った。
仕方ないので `EndProcessing` を呼ぶだけの member method を作ってそれを `Option.iter` の引数の関数に渡した。

また先述の TODO 以外にもなんか怪しいところがチラホラある。
まだあまりテストできていないが、もともと直列で動くことが前提だったこともありユーザ入力と描画がかち合ってたまにカーソル位置がおかしくなるような。
内部状態の更新は直列で実行されるから問題ないと踏んでたけど、描画部分でなんかやらかしてるのか。
他にも開発中はたまに入力が hang up することもあったし、アレがまだ発生しうるのかもチェックしないと。

あと、この変更によって Pester による E2E テストが flaky になってしまったぽいところ。今のところ開発環境では落ちたことがないが、 GitHub Actions の macOS の workflow がたまに落ちるのがわかっている。

これらは [FsUnit](https://github.com/fsprojects/FsUnit) や [Pester](https://github.com/pester/Pester) でのテストでは拾いきれない部分なので、マニュアルテストを日常的に実施するために久しぶりの Prerelease をしてもいいかもなーと考えている。
ただしバグが多いうちから出すのは流石に自身の日常使用に耐えないだろうから、 TODO はなるべく解消したうえで v0.14.0-alpha を出すのがいいかな。

Cmdlet の機能を使う非同期プログラミング、けっこう大変だったけどいい経験になった。
まだ全くイケてるとは言い難い構造だし、もっとマシな実装ないんかとか、もう一度やりたいものではないなというのが本音だけど。
やりようによってはできるなというのがわかっただけでも良い。
