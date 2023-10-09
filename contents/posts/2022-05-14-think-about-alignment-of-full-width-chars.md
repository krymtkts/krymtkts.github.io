{:title "East Asian な全角文字をキーに使った場合のアラインメントについて考える"
:layout :post
:tags ["powershell"]}

掲題のとおりである。
ここでいうアラインメントはメモリの話じゃなくて、字面上の整列のことを指す。

自分の中でもどうあるべきかまだ結論が出せてないので、考えをまとめるために書く。

### 問題

いま、 [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) で全角文字をキーに持つ hashtable を整形したとき期待どおりにならなのだけど、どう解決するのがいいのだろう？と悩んでる。

一応弁解しておくと、わたしの気持ちとしては「そんなところに全角文字使うとか危なげなことやめようや」だ。

ところが、今やってる仕事でスプレッドシートのデータをシステムに取り込むにあたり、それらをいくつかの CSV に分解・再構築する必要があって、その中でこのテーマに直面した。 PSCustomObject を CSV に変換する形でスクリプトを作ってるので、全角文字が識別子になるのだ。

普通に自分が書く範囲だとこんなの書かないので、最近まで気づかなかった。

VS Code で PowerShell を書くと、自動整形には [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) の `Invoke-Formatter` が使われる。この戻り値が整形後のコードになるのだけど、ここで今回のテーマに直面する。

問題は以下のコードで再現できる。

```powershell
$settings = @{
    IncludeRules = @(
        'PSAlignAssignmentStatement'
    )

    Rules = @{
        PSAlignAssignmentStatement = @{
            Enable = $true
            # PSAlignAssignmentStatement.CheckHashtable が真だと、
            # hashtable の要素の並びを整形してくれる。
            CheckHashtable = $true
        }
    }
}

$script = @'
$test = @{
    A = 0
    ABC = 0
    ABCDE = 0
    Ａ = 0
    ＡＢＣ = 0
    ＡＢＣＤＥ = 0
}
'@
Invoke-Formatter -ScriptDefinition $script -Settings $settings
```

出力はこうなる。これは期待のとおりではない。

```powershell
$test = @{
    A     = 0
    ABC   = 0
    ABCDE = 0
    Ａ     = 0
    ＡＢＣ   = 0
    ＡＢＣＤＥ = 0
}
```

わたしはこうなってほしいと考える。

```powershell
$test = @{
    A          = 0
    ABC        = 0
    ABCDE      = 0
    Ａ         = 0
    ＡＢＣ     = 0
    ＡＢＣＤＥ = 0
}
```

PSScriptAnalyzer のコードを調べて、 PSScriptAnalyzer 的には PowerShell のパーサが返した列番号(`EndColumnNumber`)の通りに整形してるのがわかった。
[PSScriptAnalyzer/AlignAssignmentStatement.cs GetHashtableCorrections の L194](https://github.com/PowerShell/PSScriptAnalyzer/blob/58c44234d44dfd0db35bb532906963e08fde8621/Rules/AlignAssignmentStatement.cs#L194)

次に PowerShell のパーサを直接調べる。

```powershell
$script = @'
$test = @{
    A = 0
    ABC = 0
    ABCDE = 0
    Ａ = 0
    ＡＢＣ = 0
    ＡＢＣＤＥ = 0
}
'@

function getColumnNumberString {
    param (
        $Extent
    )
    "start $($Extent.StartColumnNumber.ToString().PadLeft(2)) end $($Extent.EndColumnNumber.ToString().PadLeft(2))"
}

$ast = [System.Management.Automation.Language.Parser]::ParseInput($script, [ref]$null, [ref]$null)
$hashAst = $ast.FindAll({ $args[0] -is [System.Management.Automation.Language.HashtableAst] }, $true)
$hashAst.KeyValuePairs | ForEach-Object {
    # Item1 がキーの情報、 Item2 は '=' の情報
    $e1, $e2 = $_.Item1.Extent, $_.Item2.Extent
    "key $(getColumnNumberString($e1)) | '=' $(getColumnNumberString($e2))"
}
```

出力はこうなって、全角文字の表示幅は考慮されてないのがわかる。

```txt
key start  5 end  6 | '=' start  9 end 10
key start  5 end  8 | '=' start 11 end 12
key start  5 end 10 | '=' start 13 end 14
key start  5 end  6 | '=' start  9 end 10
key start  5 end  8 | '=' start 11 end 12
key start  5 end 10 | '=' start 13 end 14
```

じゃあこれは PowerShell のバグなのか？とまで考えを至らせると、いやパーサは表示する文字の幅を意識する必要あるか？と思えるので、これは誰が解決する問題なんや...と思っているのが、今の状況。

### 他の言語を調べる

他の事例を調べて、全角文字を含むキーや識別子をアラインメントするとどうなるか比べてみる。

とはいえ、他の言語でアラインメントするようなフォーマットかける言語何があったっけ？
少なくとも Python は違ったし、思いついたのは Go だけ。
Go 以外にも思いついたら追加したい。

#### Go

こんな中身の `test.go` があるとする。

```go
package main

type person struct {
	A int
	ABC int
	ABCDE int
	Ａ int
	ＡＢＣ int
	ＡＢＣＤＥ int
}
```

これに `gofmt ./test.go` すると次の通り。

```go
package main

type person struct {
        A     int
        ABC   int
        ABCDE int
        Ａ     int
        ＡＢＣ   int
        ＡＢＣＤＥ int
}
```

やっぱり！ PowerShell の結果と同じ。

### 現時点の理解

現実的には、パーサやフォーマッタがフォントの文字幅を考慮しないし、アラインメントしないのが妥当な落とし所なのかな。
気持ちとしては、全角文字を含んだとしても綺麗にアラインメントしてほしいが、それもエッジケースなのでそんなに困ることがない。

とはいえ Unicode 文字の演算子とかポツポツあるし、幅の規定が厳格であればフォーマッタあたりで解消したいテーマではある気がする。

なんか締まらない締めになった。

とりあえず自分用の覚書としては、 PSScriptAnalyzer は `PSAlignAssignmentStatement.CheckHashtable=$false` で利用すればこの問題に悩まされることもないので、オススメする。
