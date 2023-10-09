---
title: "Get-GzipContent のユースケース"
tags: ["powershell", "aws"]
---

以前 [krymtkts/Get-GzipContent: Get-Content for gzip files.](https://github.com/krymtkts/Get-GzipContent) というのを作って [PowerShell Gallery](https://www.powershellgallery.com/packages/Get-GzipContent/0.1.2) に公開した。

当時 gzip された JSON を扱う機会が多かった。
その時の職場は bash 文化だったのだけど、自分のローカルでは PowerShell を使うし...ということで、モジュールが欲しかった。
この PowerShell で gzip を展開するコード自体よく知られてるパターンらしかったけど、登録されてるモジュールがなかったので、自分で作った。ちゃんと探したら似たモジュールはあったのだろうけど、周辺モジュールもドカッと入るのは期待するところでないので、単機能のモジュールを作ったんだ確か。

最近になってまた gzip されたファイルを扱う機会が増えたので、改めて使う機会が来たのだけど...これちょっとイマイチやなーと思っている。

CloudFront のアクセスログで[標準ログ](https://docs.aws.amazon.com/ja_jp/AmazonCloudFront/latest/DeveloperGuide/AccessLogs.html#LogFileFormat)というのがあるのだけど、こいつは gzip された TSV で、これを PowerShell で取り扱うのに以下の関数を作った。

```powershell
function ConvertFrom-CloudFrontAccessLog {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true,
            HelpMessage = 'Path to one or more locations.')]
        [Alias('PSPath')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Path
    )
    begin {
        $header = 'date', 'time', 'x-edge-location', 'sc-bytes', 'c-ip', 'cs-method', 'cs(Host)', 'cs-uri-stem', 'sc-status', 'cs(Referer)', 'cs(User-Agent)', 'cs-uri-query', 'cs(Cookie)', 'x-edge-result-type', 'x-edge-request-id', 'x-host-header', 'cs-protocol', 'cs-bytes', 'time-taken', 'x-forwarded-for', 'ssl-protocol', 'ssl-cipher', 'x-edge-response-result-type', 'cs-protocol-version', 'fle-status', 'fle-encrypted-fields', 'c-port', 'time-to-first-byte', 'x-edge-detailed-result-type', 'sc-content-type', 'sc-content-len', 'sc-range-start', 'sc-range-end'
    }
    process {
        $Path | ForEach-Object { (zcat $_) -split "`n" } | ConvertFrom-Csv -Delimiter "`t" -Header $header
    }
}
```

`` ForEach-Object { (zcat $_) -split "`n" } `` ここがイケてない。まじで使いにくい。
改行コードずつ `Write-Object` するようにしてないから、自分で分割しないといけなくなってしまっている。当時は改行なし JSON ばかり取り扱ってたので、全く気づかなかったのかなと思っている(しらん)。

↓ こうできたらすごくイイ気がする。

```powershell
$Path | zcat | ConvertFrom-Csv -Delimiter "`t" -Header $header
```

breaking change なのもあるしバージョン 1 にするかーとか思っている。後方互換のスイッチ要るかな？とか分割単位を指定できるのが良いのかな？とか考え出すと、やる気が...
ま、 `Get-Content` と似た動きにしたいってのがあるので、 `-Delimiter` かなー。

でも、とりあえず数年後し？のセルフ使用レビューを経てツールを改善しよ...という気になったので、自分のために書いたモジュールでもあるし自分が必要な範囲で直そ。使ってる人がおこになったらちょっと考える。

さっさと直したらいいのだけど、動機づけとしてこの記事をしたためた。
あとはやるだけ... [Add the `-Delimiter` option like `Get-Content`. · Issue #3 · krymtkts/Get-GzipContent](https://github.com/krymtkts/Get-GzipContent/issues/3)
