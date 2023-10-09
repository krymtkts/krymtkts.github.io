{:title "pocof の 1 年間"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

明日 2023-04-24 で pocof の開発を始めてから 1 年になるらしい。
開発しない週があったり、そもそも進捗は亀だが、続けてきたのは我ながらエライ。
折角なので Git の commit history を基に活動を振り返ってみる。

---

書き捨てのスクリプトは以下。案外役に立ったのでなんか小綺麗にして使える感じにしてもいいな。

```powershell
# merge commit 除く。
$logs = git log --pretty=format:"%cd,%s" --date=iso-strict | ConvertFrom-Csv -Header CommitDate, Message | Where-Object -Property Message -NotLike 'Merge pull request*' | ForEach-Object { [PSCustomObject]@{
        CommitDate = Get-Date $_.CommitDate
        Message = $_.Message
    } }

# 年月ごとのコミット数。
$logs | ForEach-Object { $_.CommitDate.ToString('yyyy-MM') } | Group-Object | Select-Object -Property Name, Count

# 追加・削除行数。
$ret = @{}
$stats = git log --numstat --pretty="%cd" --date=iso-strict | ForEach-Object {
    switch ($_) {
        { $_ -match '\d{4}-\d{2}-\d{2}' } {
            $key = Get-Date $_
        }
        default {
            $values = $_ -split "`t"
            if ($values.Count -eq 3) {
                if ($ret.ContainsKey($key)) {
                    $entry = $ret[$key]
                    $ret[$key] = [PSCustomObject]@{
                        Add = [int]$values[0] + $entry.Add
                        Remove = [int]$values[1] + $entry.Remove
                    }
                }
                else {
                    $ret[$key] = [PSCustomObject]@{
                        Add = [int]$values[0]
                        Remove = [int]$values[1]
                    }
                }
            }
        }
    }
} -End { $ret }
$stats.GetEnumerator() | ForEach-Object { [PSCustomObject]@{Add = $_.Value.Add; Remove = $_.value.remove } } | Measure-Object -Property add, remove -Sum | Format-Table

# 年月ごとの追加・削除行数。
$ret = @{}
$monthly = $stats.GetEnumerator() | ForEach-Object { [PSCustomObject]@{YM = $_.Key.ToString('yyyy-MM'); Add = $_.Value.Add; remove = $_.value.Remove } } | ForEach-Object {
    if ($ret.ContainsKey($_.YM)) {
        $entry = $ret[$_.YM]
        $ret[$_.YM] = [PSCustomObject]@{
            Add = [int]$_.Add + $entry.Add
            Remove = [int]$_.Remove + $entry.Remove
        }
    }
    else {
        $ret[$_.YM] = [PSCustomObject]@{
            Add = [int]$_.Add
            Remove = [int]$_.Remove
        }
    }
} -End { $ret }
$monthly.GetEnumerator() | Sort-Object -Property Key | ForEach-Object { [PSCustomObject]@{YM = $_.Key; Add = $_.Value.Add; Remove = $_.value.remove } }

# ファイルごとの追加・削除行数。
$ret = @{}
$fileStats = git log --numstat --pretty="%cd" --date=iso-strict | ForEach-Object {
    $values = $_ -split "`t"
    if ($values.Count -eq 3) {
        $key = $values[2]
        if ($ret.ContainsKey($key)) {
            $entry = $ret[$key]
            $ret[$key] = [PSCustomObject]@{
                Add = [int]$values[0] + $entry.Add
                Remove = [int]$values[1] + $entry.Remove
            }
        }
        else {
            $ret[$key] = [PSCustomObject]@{
                Add = [int]$values[0]
                Remove = [int]$values[1]
            }
        }
    }
} -End { $ret }
$fileStats.GetEnumerator() | ForEach-Object { [PSCustomObject]@{File = $_.Key; Add = $_.Value.Add; Remove = $_.Value.Remove } } | Sort-Object -Property Add, Remove -Descending

# 年月・ファイルごとの追加・削除行数。
$ret = @{}
$fileStats = git log --numstat --pretty="%cd" --date=iso-strict | ForEach-Object {
    switch ($_) {
        { $_ -match '\d{4}-\d{2}-\d{2}' } {
            $date = Get-Date $_
        }
        default {
            $values = $_ -split "`t"
            if ($values.Count -eq 3) {
                $key = "$($date.ToString('yyyy-MM')) $($values[2])"
                if ($ret.ContainsKey($key)) {
                    $entry = $ret[$key]
                    $ret[$key] = [PSCustomObject]@{
                        Add = [int]$values[0] + $entry.Add
                        Remove = [int]$values[1] + $entry.Remove
                    }
                }
                else {
                    $ret[$key] = [PSCustomObject]@{
                        Add = [int]$values[0]
                        Remove = [int]$values[1]
                    }
                }
            }
        }
    }
} -End { $ret }
$fileStats.GetEnumerator() | ForEach-Object { [PSCustomObject]@{File = $_.Key; Add = $_.Value.Add; Remove = $_.Value.Remove } } | Sort-Object -Property File, Add, Remove
```

Merge commit を除いたコミット数が 113 。 2023 年に入ってからは前年より多少活発。

以下は年月ごとのコミット数。
開発してない月あるんじゃないかって気がしてたが、ギリギリ毎月開発してた様子。

| YM      | Commit |
| ------- | -----: |
| 2022-04 |      4 |
| 2022-05 |     16 |
| 2022-06 |      4 |
| 2022-07 |      2 |
| 2022-08 |      1 |
| 2022-09 |     11 |
| 2022-10 |      4 |
| 2022-11 |      7 |
| 2022-12 |      2 |
| 2023-01 |      7 |
| 2023-02 |     15 |
| 2023-03 |     24 |
| 2023-04 |     16 |

コードの変更を伴うコミットが今まで 109 あって、
追加された行が 6312 、削除された行が 2821 。44% 位を書き直してるのが意外だった。時期的にはテストコード拡充とそれによるリファクタが大きい要因か。

以下が年月ごとの追加・削除行数。

| YM      |  Add | Remove |
| ------- | ---: | -----: |
| 2022-04 |  717 |     83 |
| 2022-05 | 1425 |    386 |
| 2022-06 |   83 |     26 |
| 2022-07 |   61 |     56 |
| 2022-08 |   30 |     17 |
| 2022-09 |  233 |    145 |
| 2022-10 |   69 |     27 |
| 2022-11 |  365 |    170 |
| 2022-12 |   57 |     19 |
| 2023-01 |  358 |    245 |
| 2023-02 |  214 |    143 |
| 2023-03 | 2163 |   1134 |
| 2023-04 |  537 |    370 |

ファイル別の分析はちょっと面倒で、 rename したものやドキュメント類も含まれてしまってる。
が、 目 grep した感じだと最近テストを頑張って書いてた `Action.fs`, `Data.fs` あたりの新陳代謝ができており、それにつられて依存関係の `Library.fs` の変更も多いと。
`Query.fs` はコア部分なので削除少ないのはおっかなびっくりいじってるからかな？
`UI.fs` なんかは全然いじれてないので削除行数少なめ。

以下がファイルごとの追加・削除行数。

| File                             | Add | Remove |
| -------------------------------- | --: | -----: |
| src/pocof.Test/Tests.fs          | 798 |    798 |
| src/pocof.Test/PocofData.fs      | 705 |    145 |
| src/pocof/Action.fs              | 569 |    179 |
| src/pocof/Data.fs                | 517 |    451 |
| src/pocof/Library.fs             | 470 |    282 |
| src/pocof.Test/PocofQuery.fs     | 459 |    131 |
| src/pocof/Query.fs               | 430 |    261 |
| .gitignore                       | 399 |      0 |
| src/pocof/pocof.dll-Help.xml     | 382 |     60 |
| tests/pocof.Tests.ps1            | 263 |    113 |
| docs/Select-Pocof.md             | 260 |     31 |
| src/pocof/UI.fs                  | 238 |    112 |
| src/pocof/pocof.psd1             | 231 |     99 |
| src/pocof.Test/PocofAction.fs    | 171 |     23 |
| psakefile.ps1                    | 134 |     31 |
| src/pocof/{Action.fs => Data.fs} |  71 |     82 |
| src/pocof/pocof.fsproj           |  38 |     17 |
| pocof.sln                        |  36 |      2 |
| src/pocof.Test/pocof.Test.fsproj |  36 |      1 |
| README.md                        |  33 |      2 |
| .github/workflows/pr.yml         |  30 |      1 |
| LICENSE                          |  21 |      0 |
| test/pocof.Tests.ps1             |  17 |      0 |
| src/pocof.Test/Program.fs        |   4 |      0 |
| {test => tests}/pocof.Tests.ps1  |   0 |      0 |

以下が年月・ファイルごとの追加・削除行数。

| File                                     | Add | Remove |
| ---------------------------------------- | --: | -----: |
| 2022-04 .gitignore                       | 398 |      0 |
| 2022-04 pocof.sln                        |  27 |      0 |
| 2022-04 src/pocof/Library.fs             |  57 |      3 |
| 2022-04 src/pocof/pocof.dll-Help.xml     |   1 |      0 |
| 2022-04 src/pocof/pocof.fsproj           |  22 |      0 |
| 2022-04 src/pocof/pocof.psd1             | 212 |     80 |
| 2022-05 {test => tests}/pocof.Tests.ps1  |   0 |      0 |
| 2022-05 docs/Select-Pocof.md             | 181 |      0 |
| 2022-05 LICENSE                          |  21 |      0 |
| 2022-05 pocof.sln                        |   1 |      1 |
| 2022-05 psakefile.ps1                    |  90 |     13 |
| 2022-05 README.md                        |  15 |      0 |
| 2022-05 src/pocof/{Action.fs => Data.fs} |  71 |     82 |
| 2022-05 src/pocof/Action.fs              | 346 |     46 |
| 2022-05 src/pocof/Data.fs                |  89 |    107 |
| 2022-05 src/pocof/Library.fs             | 150 |    112 |
| 2022-05 src/pocof/pocof.dll-Help.xml     | 261 |      3 |
| 2022-05 src/pocof/pocof.fsproj           |   8 |      9 |
| 2022-05 src/pocof/pocof.psd1             |   1 |      1 |
| 2022-05 src/pocof/Query.fs               |  39 |      2 |
| 2022-05 src/pocof/UI.fs                  | 128 |     10 |
| 2022-05 test/pocof.Tests.ps1             |  17 |      0 |
| 2022-05 tests/pocof.Tests.ps1            |   7 |      0 |
| 2022-06 src/pocof/Library.fs             |   3 |      3 |
| 2022-06 src/pocof/pocof.fsproj           |   3 |      3 |
| 2022-06 src/pocof/Query.fs               |  43 |     11 |
| 2022-06 src/pocof/UI.fs                  |  34 |      9 |
| 2022-07 src/pocof/Data.fs                |   7 |      4 |
| 2022-07 src/pocof/Library.fs             |   4 |     11 |
| 2022-07 src/pocof/Query.fs               |  41 |     38 |
| 2022-07 src/pocof/UI.fs                  |   9 |      3 |
| 2022-08 psakefile.ps1                    |   3 |      3 |
| 2022-08 src/pocof/Library.fs             |  27 |     14 |
| 2022-09 psakefile.ps1                    |  17 |      8 |
| 2022-09 README.md                        |  18 |      2 |
| 2022-09 src/pocof/Action.fs              |  11 |      8 |
| 2022-09 src/pocof/Data.fs                |  18 |     18 |
| 2022-09 src/pocof/Library.fs             |  88 |     51 |
| 2022-09 src/pocof/pocof.fsproj           |   2 |      2 |
| 2022-09 src/pocof/pocof.psd1             |  15 |     15 |
| 2022-09 src/pocof/Query.fs               |  18 |     17 |
| 2022-09 src/pocof/UI.fs                  |   8 |     12 |
| 2022-09 tests/pocof.Tests.ps1            |  38 |     12 |
| 2022-10 src/pocof/Library.fs             |  19 |     12 |
| 2022-10 src/pocof/pocof.fsproj           |   1 |      1 |
| 2022-10 src/pocof/pocof.psd1             |   1 |      1 |
| 2022-10 src/pocof/Query.fs               |  16 |      8 |
| 2022-10 src/pocof/UI.fs                  |   6 |      4 |
| 2022-10 tests/pocof.Tests.ps1            |  26 |      1 |
| 2022-11 docs/Select-Pocof.md             |  58 |     26 |
| 2022-11 src/pocof/Action.fs              |   1 |      0 |
| 2022-11 src/pocof/Data.fs                |  29 |      0 |
| 2022-11 src/pocof/Library.fs             |   6 |      1 |
| 2022-11 src/pocof/pocof.dll-Help.xml     |  97 |     57 |
| 2022-11 src/pocof/pocof.fsproj           |   1 |      1 |
| 2022-11 src/pocof/pocof.psd1             |   1 |      1 |
| 2022-11 src/pocof/Query.fs               |  66 |     53 |
| 2022-11 src/pocof/UI.fs                  |   0 |      1 |
| 2022-11 tests/pocof.Tests.ps1            | 106 |     30 |
| 2022-12 src/pocof/Data.fs                |  13 |      0 |
| 2022-12 src/pocof/Library.fs             |  23 |      9 |
| 2022-12 src/pocof/Query.fs               |   5 |      5 |
| 2022-12 src/pocof/UI.fs                  |  16 |      5 |
| 2023-01 src/pocof/Action.fs              |   3 |      3 |
| 2023-01 src/pocof/Data.fs                | 204 |    154 |
| 2023-01 src/pocof/Library.fs             |  14 |      7 |
| 2023-01 src/pocof/Query.fs               | 124 |     50 |
| 2023-01 src/pocof/UI.fs                  |  13 |     31 |
| 2023-02 docs/Select-Pocof.md             |  21 |      5 |
| 2023-02 src/pocof/Action.fs              |   9 |      4 |
| 2023-02 src/pocof/Data.fs                |  21 |     14 |
| 2023-02 src/pocof/Library.fs             |  22 |     11 |
| 2023-02 src/pocof/pocof.dll-Help.xml     |  23 |      0 |
| 2023-02 src/pocof/pocof.fsproj           |   1 |      1 |
| 2023-02 src/pocof/pocof.psd1             |   1 |      1 |
| 2023-02 src/pocof/Query.fs               |  18 |     12 |
| 2023-02 src/pocof/UI.fs                  |  17 |     25 |
| 2023-02 tests/pocof.Tests.ps1            |  81 |     70 |
| 2023-03 .gitignore                       |   1 |      0 |
| 2023-03 pocof.sln                        |   8 |      1 |
| 2023-03 psakefile.ps1                    |  17 |      7 |
| 2023-03 src/pocof.Test/pocof.Test.fsproj |  36 |      1 |
| 2023-03 src/pocof.Test/PocofAction.fs    | 113 |      0 |
| 2023-03 src/pocof.Test/PocofData.fs      | 530 |      2 |
| 2023-03 src/pocof.Test/PocofQuery.fs     | 404 |    118 |
| 2023-03 src/pocof.Test/Program.fs        |   4 |      0 |
| 2023-03 src/pocof.Test/Tests.fs          | 798 |    798 |
| 2023-03 src/pocof/Action.fs              | 161 |     91 |
| 2023-03 src/pocof/Data.fs                |  81 |    112 |
| 2023-03 src/pocof/Library.fs             |   2 |      2 |
| 2023-03 src/pocof/Query.fs               |   7 |      2 |
| 2023-03 tests/pocof.Tests.ps1            |   1 |      0 |
| 2023-04 .github/workflows/pr.yml         |  30 |      1 |
| 2023-04 psakefile.ps1                    |   7 |      0 |
| 2023-04 src/pocof.Test/PocofAction.fs    |  58 |     23 |
| 2023-04 src/pocof.Test/PocofData.fs      | 175 |    143 |
| 2023-04 src/pocof.Test/PocofQuery.fs     |  55 |     13 |
| 2023-04 src/pocof/Action.fs              |  38 |     27 |
| 2023-04 src/pocof/Data.fs                |  55 |     42 |
| 2023-04 src/pocof/Library.fs             |  55 |     46 |
| 2023-04 src/pocof/Query.fs               |  53 |     63 |
| 2023-04 src/pocof/UI.fs                  |   7 |     12 |
| 2023-04 tests/pocof.Tests.ps1            |   4 |      0 |

---

2 年目も細々と開発を続けていく。他にやることで Fable でブログ再構築する際の調べ物あるけど、直感ではそんなに失速しないんじゃないかなという気がしている(気だけかも)。
