---
title: "F# でコマンドレットを書いてる pt.40"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

[はじめメモリ浪費に気付いたとき](/posts/2024-05-05-writing-cmdlet-in-fsharp-pt38.html)の使い方は pocof の起動までにめちゃくちゃ時間がかかる。

```powershell
    function global:Show-ReadLineHistory() {
        Get-Content -Path (Get-PSReadLineOption).HistorySavePath | Select-Object -Unique | Select-Pocof -CaseSensitive -Layout TopDown
    }
    Set-Alias pghy Show-ReadLineHistory -Option ReadOnly -Force -Scope Global
```

こういう使い方をしてるのだけど、 [`Select-Object -Unique`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/select-object?view=powershell-7.4#example-4-select-unique-characters-from-an-array) がめちゃくちゃ遅い。
どういう実装をしてるかまでおってないが、順不同の要素中の他の重複を取り除くから遅いっぽい。
ここを [`Get-Unique`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-unique?view=powershell-7.4) にしたら爆発的に速くなるのだけど、まず [`Sort-Object`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/sort-object?view=powershell-7.4) で並び替えておく必要があり、それは期待した動作じゃない。元の順そのままで重複だけなくしたい。

そこまできたらもう面倒なので pocof に `-Unique` オプションつけた方が楽やろなということで、そういう機能を実装した。 [#181](https://github.com/krymtkts/pocof/pull/181)

PowerShell で言うところのこういうコードにしたら、とても速く重複が取り除ける。

```powershell
Get-Content -Path (Get-PSReadLineOption).HistorySavePath | % -begin {$a=[ordered]@{}} -process {if ($a.Contains($_)) {} else {$a.Add($_, $null)}} -end {$a.Keys}
```

`[ordered]` は pocof のコードだと [`System.Collections.Specialized.OrderedDictionary`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.specialized.ordereddictionary?view=net-8.0) になる。
pocof の`InputObject` の並び順を維持しつつとなるとこいつを使うのが定石だろう。

`System.Collections.Specialized.OrderedDictionary` では key の等価性を判定するのに [`Equals`](https://learn.microsoft.com/en-us/dotnet/api/system.object.equals?view=net-8.0) と [`GetHashCode`](https://learn.microsoft.com/en-us/dotnet/api/system.object.gethashcode?view=net-8.0) が使われる。
なので `PSCustomObject` の場合だと重複が取り除けない。 `PSCustomObject` は `Object.Equals` の再定義とかしてないから参照等価性しかチェックしてないみたい。
ちょっと残念だが、でも PowerShell の環境では大体の object はそれらを実装してるし、いったん `PSCustomObject` 用の特別な比較関数を書かないでやった。

`-Unique` オプション対応してみてわかったのだけど、 `hashtable` の key-value の組み合わせが一致するエントリも除外できるし、こりゃなかなか便利な機能な気がしている。ぱぱっと思いついて作った割には。

pocof の設計思想的に、 pocof の呼び出し前後で他の Cmdlet でできることはあまり実装しないことにして。
それが PowerShell らしいかなと(勝手に)と考えて `-Unique` オプションは考えてこなかった。
でも今回の件は学びになった。
既存の Cmdlet で出来たとしても、長々とパイプラインを書かないといけないとかで複雑になったり、パフォーマンスが振るわないようであれば、機能に取り込んでも良さそうやなという感覚。

この `-Unique` は早速 [0.13.0](https://www.powershellgallery.com/packages/pocof/0.13.0) でリリースして、自分の [PowerShell profile](https://github.com/krymtkts/pwsh-profile/blob/main/Scripts/Pocof/Pocof.psm1) で新しいオプション使うように直した。
