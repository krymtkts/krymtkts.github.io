{:title "今更 ForEach-Object -Parallel"
:layout :post
:tags ["powershell"]}

今更だが、直列だと長時間かかる処理を分散させるために `ForEach-Object -Parallel` を使う必要があった。
困ったというほどでもないけど、真面目に使ったことなかったので、今回学んだ気をつけポイントをまとめる。
(毎度の如く PowerShell でそれをやる必要は全くなかったが、ぱっと手を動かしたくてついやってしまった)

- 参照
  - [PowerShell ForEach-Object Parallel Feature - PowerShell Team](https://devblogs.microsoft.com/powershell/powershell-foreach-object-parallel-feature/)
  - [about Foreach-Parallel - PowerShell | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/module/psworkflow/about/about_foreach-parallel?view=powershell-5.1)
  - [powershell - How to pass a custom function inside a ForEach-Object -Parallel - Stack Overflow](https://stackoverflow.com/questions/61273189/how-to-pass-a-custom-function-inside-a-foreach-object-parallel/61273544#61273544)

---

### 並列数の制御

`-Parallel` はとにかく遅い。異なる runspace が作成されそこで実行されるからだ。
なのでとにかく計算時間がかかるんやというような処理だけ渡すようにした方がいい。

今回は、あらかじめ `ThrottleLimit` と同じ数の `InputObject` に調整して重いコマンドを打つ方針を使った。
消えては立ち上がり x2 するような書き方をした方ではやはり runspace 作成のオーバーヘッドが、処理時間という形で顕著に見られた。
スクリプトブロックが消えては立ち上がり x2 しないように、スクリプトブロック内で重い 1 処理を実行する方が良かった(AWS のリソースを一括操作するやつだからできたことだけど)。

1 ヵ月分のデータを加工する必要があって、対象日毎に 1 処理にすることができたので、それを約 30 並列でやった。
イメージ ↓

```powershell
$begin = (Get-Date -Day 1 -Hour 0 -Minute 0 -Second 0 -Millisecond 0)
$end = $begin.AddMonths(1).AddDays(-1)
$dateRange = @()
while ($begin -le $end) {
    $dateRange += $begin
    $begin = $begin.AddDays(1)
}
$dateRange | ForEach-Object -ThrottleLimit $dateRange.Length -Parallel {
    # 長時間かかる処理.
}
```

### スクリプトブロック外のリソース参照

`$using:` 修飾子をつけたら変数を参照できる。
ただしスクリプトブロックや関数には使えない。

関数をどうしても使いまわしたいときは、文字列に変換した上で取り込む技もある。 [powershell - How to pass a custom function inside a ForEach-Object -Parallel - Stack Overflow](https://stackoverflow.com/questions/61273189/how-to-pass-a-custom-function-inside-a-foreach-object-parallel/61273544#61273544)

が、スクリプトブロック内でしか使わないのであればその中に関数を定義してしまったほうが楽か。
[最もクリーンな方法でオススメらしいし](https://devblogs.microsoft.com/powershell/powershell-foreach-object-parallel-feature/#comment-171)、実際にそうした。

(`Import-Module` すればいいのだけどいちいちモジュールを作らないこともあろう)

技を使うとこうなる。

```powershell
function Get-Identity {
    param (
        [Parameter(Mandatory,
            Position = 0,
            ValueFromPipeline
        )]
        [PSObject]
        $Value
    )
    process {
        $Value
    }
}

$funcDef = ${function:Get-Identity}.ToString()

function Test-UsingFuncInParallel {
    [CmdletBinding()]
    param ()

    1..30 | ForEach-Object -Parallel {
        ${function:Get-Identity} = $using:funcDef
        $_ | Get-Identity | Write-Host
    } -ThrottleLimit 30
}
```

技を使わない版。当然ながらスクリプトブロックは間延びする。

```powershell
function Test-UsingFuncInParallel {
    [CmdletBinding()]
    param ()

    1..30 | ForEach-Object -Parallel {
        function Get-Identity {
            param (
                [Parameter(Mandatory,
                    Position = 0,
                    ValueFromPipeline
                )]
                [PSObject]
                $Value
            )
            process {
                $Value
            }
        }

        $_ | Get-Identity | Write-Host
    } -ThrottleLimit 30
}
```

使う関数が多いとスクリプトブロックも伸びがちなので、使い分けを検討した方が良かろう。
用途次第だが、わたしの場合は先述の通り並列数 MAX ピッタリに調整した `InputObject` を使て長時間の処理を流すだろうから、 runspace の作成のコストはそれほど気にならない。スクリプトブロックの中がごちゃごちゃしないことのメリットがあるかも知れん。

---

あと微妙にハマったのが `$PSCmdlet` の参照。

`$using:` つけ忘れてると単に `$null` なだけなのでエラーメッセージ見てもピンときにくい。

```plaintext
Line |
   2 |          if ($PSCmdlet.ShouldProcess($_, 'Write-Host')) {
     |              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | You cannot call a method on a null-valued expression.
```

さらに関数の呼び出し時はカッコで囲む必要がある。カッコがないと Parse Error。

```diff
 function Test-ShouldProcessInParallel {
     [CmdletBinding(SupportsShouldProcess)]
     param ()

     1..30 | ForEach-Object -Parallel {
-        if ($using:PSCmdlet.ShouldProcess($_, 'Write-Host')) {
+        if (($using:PSCmdlet).ShouldProcess($_, 'Write-Host')) {
             $_ | Write-Host
         }
     } -ThrottleLimit 30
 }
```

```plaintext
ParserError:
Line |
   6 |          if ($using:PSCmdlet.ShouldProcess($_, 'Write-Host')) {
     |              ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | Expression is not allowed in a Using expression.
```

あとなんか見慣れぬエラーになる時があったが忘れた。再現できたら追記したい。
