{:title "古のはてなダイアリー XML を Markdwon に変換する(いい感じに)"
:layout :post
:tags ["powershell"]}

先日 Google ドライブの中に眠っていた XML を発掘した。古のはてなダイアリーからエクスポートされたやつだこれ。

昔、プレーンテキストで日記をつけ始める前に、誰に見せるでもないブログを書いていた。何度かブログサービスに書いては止めるを繰り返していた。
そのうち最も最近までやってたのがはてなダイアリーで、そのバックアップがこれっぽい。
178 記事、2009 ~ 2011 の 3 年間もあるのに、すっかり忘れていた。

これはぜひ今の日記に統合したい。ということではてなダイアリーの XML を Markdown に変換して取り込むことにした。

### 変換する

今回は Pandoc を使わず純粋に PowerShell だけで処理する。
Pandoc で[はてな記法](https://help.hatenablog.com/entry/text-hatena-list)から Markdown に変換できるが、ごく一部の記事は既に日記があって追記しないといけないこともあり、PowerShell だけでやる方が融通が利く。

XML なら PowerShell の主戦場なので、何をするにも楽だ。
使っているはてな記法もおおよそパターンマッチで置換できる(鬼の連続置換)。

引用だけ正規表現での変換がわからんけど、一桁件数しかなかったので、手で書き換える。

以下変換スクリプト。 `hatena2md.ps1`

````````powershell
[CmdletBinding()]
param (
    [Parameter(
        Mandatory,
        HelpMessage = "XML Path of hatena diary.")]
    [ValidateScript({
            if (-Not ($_ | Test-Path) -or ($_ -notmatch "\.xml")) {
                throw "The file specified in the XmlPath argument must be XML."
            }
            return $True
        })]
    [System.IO.FileInfo]
    $XmlPath,
    [Parameter(
        Mandatory,
        HelpMessage = "Output Path of Markdown diaries.")]
    [ValidateScript({
            if (-Not (Test-Path $_ ) -or -not (Test-Path $_ -PathType Container)) {
                throw "The BlogPath argument must be a folder."
            }
            return $True
        })]
    [System.IO.FileInfo]
    $BlogPath
)

$xml = [XML](Get-Content $XmlPath)
Write-Host "Convert $($xml.diary.day.Length) hatena XML to Markdown."

$xml.diary.day | ForEach-Object {
    $date = Get-Date $_.date -ErrorAction SilentlyContinue
    if (-not $?) {
        Write-Error "Cannot convert diary of $($_.date)"
        return
    }
    $diaryPath = "$BlogPath/$($date.Year)/$($date.ToString('yyyy-MM'))/$($date.toString('dd')).md"
    Get-ChildItem $diaryPath -ErrorAction SilentlyContinue | Out-Null
    if ($?) {
        Write-Host "Add Content to exists diary of $($_.date)"
        $InvokeContent = 'Add-Content'
        $content = @"

## はてなの日記

"@
    }
    else {
        Write-Host "Create new diary of $($_.date)"
        $InvokeContent = 'Set-Content'
        mkdir -Force (Split-Path $diaryPath -Parent) | Out-Null
        $content = @"
# $($date.ToString('yyyy-MM/dd (ddd)'))

## はてなの日記

"@
    }
    $content += (
        $_.body -split "`n" | ForEach-Object {
            $_ `
                -replace '^\*\d+\*(.+)$', "### `$1`n" `
                -replace '^=+$', "`n---`n" `
                -replace '^--(.+)', '    - $1' `
                -replace '^-(.+)', '- $1' `
                -replace '^\+\+(.+)', '    1. $1' `
                -replace '^\+(.+)', '1. $1' `
                -replace '\>\|(\w+)\|', "`n```````$1" `
                -replace '(\>\|\||\>\|)', "`n``````" `
                -replace '(\|\|\<|\|\<)', "```````n" `
                -replace '\[(.+?)\:title=(.+?)]', '[$2]($1)'
        }) -join "`n"
    $content | & $InvokeContent -Path $diaryPath -Encoding utf8 -NoNewline | Out-Null
}
````````

はてなダイアリーの XML から、自分の日記の構造に変換している。
はてなダイアリーの記事にはわかりやすく「はてなの日記」というセクションを設ける。

基本ファイルを作成するが、既存のファイルが存在する場合は、追記する。
先述の通りセクションがあるので追記しても自然だ。

ディレクトリ構造は[前回の Textile → Markdown 変換](/posts/2022-04-02-convert-textile-to-md) で記した通り ↓ 。 1 つの XML からこの形にファイルを出力する。

```plaintext
+---2013
|   +---2013-01
|   |       17.textile
|   ︙      ︙
︙
\---2022
   +---2022-01
   ︙
   \---2022-03
            01.md
            ︙
            31.md
```

実行する。

```powershell
. .\hatena2md.ps1 -XmlPath $XmlPath -BlogPath $BlogPath
```

### 変換後の処理

変換後のファイルから`>>` で検索して該当するものを一覧し、書き換える。

```powershell
ls -File -Recurse | ? {cat $_ | % {$_ -split "`n"} | ? {$_ -match '^>>'}} | select -Property FullName
```

ココに来て順序付きリストの数字をやっぱりインクリメントしたい(前のステップでは`1.`固定でいいかと思ってた)...ということで一部書き直した。

最後に全体を [Prettier](https://prettier.io/) で清書する。

```powershell
prettier --write .
```

最終チェックをして完了。

### まとめ

Textile からの変換よりも、量が 1/10 程度と少なかったこともあり、簡単にできた。

コンテンツそれ自体は中々に青臭く、読んでると「わー！！！」と声を上げたくなるところもある。
ただ人生の転機が集中した期間だったこともあって、興味深い内容だった。貴重なログなので、これまた自己分析に使いたい。
