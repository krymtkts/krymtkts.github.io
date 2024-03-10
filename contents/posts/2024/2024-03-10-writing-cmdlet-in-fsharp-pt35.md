---
title: "F# でコマンドレットを書いてる pt.35"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

ちょっと今週末は忙しかったので[クエリ文字列選択](https://github.com/krymtkts/pocof/issues/44)の開発をあまりできてない。

時間が取れなかったので車の後部座席に乗りながら pocof 開発を 2 時間ほどやってみたのだけど、それなりに準備しないといけないが、思ったよりできるなという感触を得た。
助手席のヘッドレストにリュックの背中の部分が上になるようリュックを片方かける。そこに laptop を置き、下から膝でサポートすれば割りと安定する。
ケチってテザリングしなかったので GitHub Copilot の支援は受けれなかったが、支援なくなったてもまだ割と書けるなという感じも得た。

ただし高速に乗ってるときくらいじゃないと揺れでとてもタイピングできないし、
あと夜間はわたしの目が悪いのもあって、結構しんどい感じだった。

---

という感じで進捗あまりないが、 [ANSI escape sequences](https://en.wikipedia.org/wiki/ANSI_escape_code) 対応の前準備として、手軽にできるレンダリングの改善だけしたのでそれに触れる。 [#149](https://github.com/krymtkts/pocof/pull/149)

[PSReadLine/PSReadLine/ReadLine.cs](https://github.com/PowerShell/PSReadLine/blob/5efe2ef55f85bbac9c8a8f39825ad62b3049b0a5/PSReadLine/ReadLine.cs#L1004-L1041) を見てて気づいたのだけど、描画中は [`Console.CursorVisible`](https://learn.microsoft.com/ja-jp/dotnet/api/system.console.cursorvisible?view=net-8.0) を `false` にしてる。

```csharp
            var console = _singleton._console;
            console.CursorVisible = false;

            // 略
            _singleton.Render();
            console.CursorVisible = true;
        }
```

GUI アプリケーションなんかでもよくある手法で、描画の状態が決定するまで描画停止するアレと同じ手法だこれ。
CUI アプリケーションの場合だと、カーソルのチラつき(flicking と表現するぽい)を抑えるために同じ手法が使えるみたい。
CPU 負荷が高い時にノロノロとカーソルが移動することでチラつき(flicking)が目に付きやすくなるので、カーソルを見えなくすることでチラつき(flicking)をなくす効果があると。
なるほどなー、すっかり忘れてたわ。

実際に pocof の CPU 高負荷時のもっさり挙動でチラつきが目立つので、 [`IDisposable`](https://learn.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-7.0) 実装を使って以下の様に対処した。目立つチラつき(flicking)は抑えれそう。

```fsharp
    type RawUI(rui) =
        // Console.CursorVisible を一時的に無効にする
        interface IRawUI with
            member __.HideCursorWhileRendering() =
                Console.CursorVisible <- false

                { new IDisposable with
                    member _.Dispose() = Console.CursorVisible <- true }

    type Buff(r, i, layout) =
        let rui: IRawUI = r

        // 使う方はこうする
        member __.WriteScreen
            (layout: Data.Layout)
            (state: Data.InternalState)
            (entries: Data.Entry list)
            (props: Result<string list, string>)
            =
            use _ = rui.HideCursorWhileRendering()

```

ちなみに実際のパフォに違いがあるのかというと、調べてみた感じだと微妙だった。
以下のようなテスト関数を作成しまして、 1 行ずつ書くパターン、 1 文字ずつ書くパターンを比較した。

```powershell
function test-rendering {
    param (
        [switch]
        $HideCursor,
        [switch]
        $HardMode
    )
    if ($HideCursor) {
        [Console]::CursorVisible = $false
    }
    [Console]::SetCursorPosition(0, 0)
    $start = Get-Date
    0..30 | % {
        [Console]::SetCursorPosition($_, $_)
        if ($HardMode) {
            # 1 文字ずつ描画する激遅モード
            $_ .. ([Console]::WindowWidth - 1) | % { [Console]::Write('x') }
        }
        else {
            # 1 行ずつ描画する普通モード
            [Console]::Write(('x' * ([Console]::WindowWidth - $_)))
        }
    }
    ((Get-Date) - $start).Milliseconds
    if ($HideCursor) {
        [Console]::CursorVisible = $true
    }
}

# それぞれを無風状態のときに実行
$a1 = 1 .. 100 | % { test-rendering } | measure -AllStats
$a2 = 1 .. 100 | % { test-rendering -HideCursor} | measure -AllStats
$b1 = 1 .. 100 | % { test-rendering -HardMode } | measure -AllStats
$b2 = 1 .. 100 | % { test-rendering -HardMode -HideCursor} | measure -AllStats

$a1;$a2;$b1;$b2;

# Count             : 100
# Average           : 5.05
# Sum               : 505
# Maximum           : 10
# Minimum           : 3
# StandardDeviation : 1.50671895859912
# Property          :

# Count             : 100
# Average           : 4.96
# Sum               : 496
# Maximum           : 10
# Minimum           : 3
# StandardDeviation : 1.43491808093351
# Property          :

# Count             : 100
# Average           : 216.31
# Sum               : 21631
# Maximum           : 283
# Minimum           : 196
# StandardDeviation : 19.5596446023361
# Property          :

# Count             : 100
# Average           : 221.2
# Sum               : 22120
# Maximum           : 297
# Minimum           : 196
# StandardDeviation : 20.8810967628667
# Property          :
```

結果、カーソル非表示の有無で誤差レベルの差しかない。
実行したときの CPU 利用状況の影響のほうがでかい。
なので体感の改善って面が大きい。

おわり。
