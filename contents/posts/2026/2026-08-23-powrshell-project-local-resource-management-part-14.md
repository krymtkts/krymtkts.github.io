---
title: "PowerShell Local Resource Manager Part 14"
tags: ["powershell"]
---

[krymtkts/pslrm](https://github.com/krymtkts/pslrm) の開発をした。

pslrm 周辺のツール([krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) , [krymtkts/PSKeepAChangelogTools](https://github.com/krymtkts/PSKeepAChangelogTools))の開発は少しずつやってた。
でも、肝心の本体を置いてきぼりだったので、重い腰を上げて着手した。

ちょうど最近 [PowerShell の profile](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_profiles?view=powershell-7.6) 内の関数に [`ValueFromRemainingArguments`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions_advanced_parameters?view=powershell-7.6#valuefromremainingarguments-argument) を使った改善をした。

```pwsh
        function Set-SelectedRepository {
            param(
                [Parameter(
                    Position = 0,
                    ValueFromPipeline,
                    ValueFromRemainingArguments)]
                [string]
                $Query
            )
            $repos = ghq list
            $repo = $repos | Select-Pocof $Query -NonInteractive
            if ($repo.Count -ne 1) {
                $repo = $repos | Select-Pocof $Query | Select-Object -First 1
            }
            Set-Location "$(ghq root)/$repo"
        }
        Set-Alias gcd Set-SelectedRepository -Option ReadOnly -Force -Scope Global
```

[pwsh-profile/Scripts/Pocof/Pocof.psm1 at b2625f4bc7417928e395bcea2735d9ff24fda115 · krymtkts/pwsh-profile](https://github.com/krymtkts/pwsh-profile/blob/b2625f4bc7417928e395bcea2735d9ff24fda115/Scripts/Pocof/Pocof.psm1#L162-L178)

こういうやつ。
元は `gcd 'poc cof'` みたいにしないと複数パラメータの検索が出来なかった。
今では、 `ValueFromRemainingArguments` で `gcd poc cof` とそのまま打てるようになる。便利。
これと同様の改善を pslrm にも入れてみた。 [#26](https://github.com/krymtkts/pslrm/pull/26)

何故これが pslrm に有用かというと、現状の pslrm を対話的に使うには、以下のように面倒な入力形式に沿う必要があるからだ。

```pwsh
Invoke-PSLResource Invoke-Psake '-taskList', 'Lint', '-parameters', @{'Stage'='Release'}
```

pslrm はまだ初版であり、初版では対話的な利用は捨ててる。
それもあって、 `Invoke-PSLResource` で実行したい Cmdlet/Function の parameter を配列形式で書かないといけない。

これは流石に面倒だ。
現に自分ではなるべく直に pslrm を使わず [Invoke-Build](https://github.com/nightroman/Invoke-Build) 等 build script で wrap して使うようにしてる。
対話的に使うには、せめてこうできないかなと考えた。

```pwsh
Invoke-PSLResource Invoke-Psake -taskList Lint -parameters @{'Stage'='Release'}
```

これだと `-taskList Lint -parameters @{'Stage'='Release'}` の部分が `ValueFromRemainingArguments` で処理できるので、可能だ。

ただ `ValueFromRemainingArguments` をそのまま使えば解決できるってわけではなく、以下の調整をした。
1 つは PowerShell の互換性の問題、残りは `Invoke-PSLResource` と実行したい Cmdlet/Function の parameter を分離するためのものだ。
後者は使い方によっては必要ないが、安定させるには使った方がいい。

- [`ValueFromRemainingArguments`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions_advanced_parameters?view=powershell-7.6#valuefromremainingarguments-argument) が PowerShell 6.2 未満では余分な配列で入力を wrap するため、 flatten する必要がある
  - > Beginning in PowerShell 6.2, collections are handled differently when passed to **ValueFromRemainingArguments**. If you only pass a collection, then each value in the collection is treated as a separate item.
    文書上は明示されてないけど、元々 collection 直で渡しても collection が維持されたのは wrapping してたから、という挙動を確認した
- positional parameter の曖昧さを解消するため、 `PositionalBinding` を無効にして位置指定を無効にする
  [PositionalBinding | about_Functions_CmdletBindingAttribute - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions_cmdletbindingattribute?view=powershell-7.6#positionalbinding)
- `Invoke-PSLResource` の parameter name や [common parameters](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_commonparameters?view=powershell-7.6) と衝突しなよう `--` の利用を推奨する
  [The end-of-parameters token | about_Parsing - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_parsing?view=powershell-7.5#the-end-of-parameters-token)

これで対話式でもちょっとだけ使いやすくなったはず。
ただしまだ足りない部分もある。

まず実行したい Cmdlet/Function 自体の補完や parameter 補完はできない。
これは [dynamic parameter](https://learn.microsoft.com/en-us/powershell/scripting/developer/cmdlet/cmdlet-dynamic-parameters?view=powershell-7.6) で実現できないか考えている。

また `ValueFromRemainingArguments` 自体にも制約がある。
`-Switch:$false` の形式だと、 token が `-Switch:` と `$false` 分割されてしまう問題がある。
これは PowerShell 自体の問題と思われるが、現状は以下のように修正の見込みがないっぽい。

- [Parameter parsing/passing: an unquoted argument that looks like a named parameter/value pair separated with ":" (colon) is broken in two in positional binding · Issue #6292 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/6292)
- [Parameter parsing/passing: unquoted tokens that look like named arguments with colon as the separator are broken in two when passed indirectly via $Args / @Args · Issue #6360 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/6360)

よって実装を急ぐなら workaround 実装を探る必要がある。

ちょっと難易度高いけど、ひとまず dynamic parameter を実装してみる。
できたら次の版を出すのも良さそうだが、自分で使ってみた感触で判断するつもり。
