{:title "psake の Task 名を自動補完する"
:layout :post
:tags ["psake","powershell"]}

ほぼ趣味レベルなのだが、所謂タスクランナーとして[psake](https://github.com/psake/psake)を使っている。趣味レベルなのは、Go とか Python とかでは `make` を使うので `psake` を製品コードでは使ったことなくて、自分の細々とした面倒な処理をスクリプト化してまとめるのに `psake` を使ってるからだ。

そんな訳で利用頻度も高くなかったのだが、なんか最近は AWS のリソースを操作するニッチなスクリプト(例えば開発環境とかステージング環境だけに使うようなやつ)が大量にあって、それをまとめるのに使い出した。
その御蔭で利用頻度が高まり、いやーよくできたツールやな～などと改めて思っていたが、今まで不満に感じなかった自動補完がないことがストレスになってきた。タスクが増え過ぎて名前が覚えられないのだ。

ｷﾞｯﾊﾌﾞの repo を確認すると、古の`TabExpansion`版はあれど、今どきの`Register-ArgumentCompleter`版がない。
[psake/PsakeTabExpansion.ps1 at master · psake/psake](https://github.com/psake/psake/blob/master/tabexpansion/PsakeTabExpansion.ps1)

今更`TabExpansion`使いたくないので、`Register-ArgumentCompleter`用に合わせてこしらえた。

[This is Register-ArgumentCompleter version of https://github.com/psake/psake/blob/master/tabexpansion/PsakeTabExpansion.ps1.](https://gist.github.com/krymtkts/b2e6742691fdca6ca09567ca146063df)

使ってみていまんとこ良さそうな感じ。問題なさそうなら本家に PRO ぶん投げてみてもいいかもね。

---

以下は`Register-ArgumentCompleter`のスクリプトブロックをデバッグするときの個人的メモ。

その時の入力でトリガーされたスクリプトブロックの引数を確認するのに `Write-Host` とか使うと厄介だと思うので、ログファイル的なものをこしらえておき、別窓で`tail`してあげると見易くなる(と思っている)。

```powershell
Register-ArgumentCompleter -CommandName Invoke-Psake -ParameterName taskList -ScriptBlock {
    param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters)
    "$commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameters" >> test.log
    if ($commandAst -match '(?<file>[^\.]*\.ps1)') {
        $file = $Matches.file
        "YEAH" >> test.log
    }
    else {
        $file = 'psakefile.ps1'
        "DEFAULT" >> test.log
    }
    & $commandName -buildFile $file -docs -nologo | Out-String -Stream | ForEach-Object { if ($_ -match "^[^ ]*") { $matches[0] } } | `
        Where-Object { $_ -notin ('Name', '----', '') } | Where-Object { !$wordToComplete -or $_ -like "$wordToComplete*" }
}
```

```powershell
Get-Content .\test.log -Wait -Tail 10
# Invoke-psake, taskList, I, invoke-psake -buildFile .\psakefile.ps1 -taskList I, System.Collections.Hashtable
# Invoke-psake, taskList, In, invoke-psake -taskList In, System.Collections.Hashtable
# DEFAULT
# Invoke-psake, taskList, I, invoke-psake -buildFile .\psakefile.ps1 -taskList I, System.Collections.Hashtable
# YEAH
```

どーでもいーけどこの日記の deploy をｷﾞｯﾊﾌﾞｱｸｼｮﾝ化したい。

---

追記。

デバッグ中に気づいたのだが、プロファイル内で `$psake` という変数を作ると `Invoke-psake` が壊れるという事に気づいた。

```powershell
🤖 takatoshi  invoke-psake -nologo
Test-Path: C:\Program Files\PowerShell\Modules\psake\4.9.0\private\Get-DefaultBuildFile.ps1:9
Line |
   9 |      if (test-path $psake.config_default.buildFileName -pathType Leaf) …
     |          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | Value cannot be null. (Parameter 'The provided Path argument was null or an empty collection.')

Test-Path: C:\Program Files\PowerShell\Modules\psake\4.9.0\private\Get-DefaultBuildFile.ps1:11
Line |
  11 |  …   } elseif (test-path $psake.config_default.legacyBuildFileName -path …
     |                ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | Value cannot be null. (Parameter 'The provided Path argument was null or an empty collection.')

InvalidOperation: C:\Program Files\PowerShell\Modules\psake\4.9.0\public\Invoke-psake.ps1:327
Line |
 327 |          $psake.build_success = $false
     |          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
     | The property 'build_success' cannot be found on this object. Verify that the property exists and can be set.
```

罠すぎる...
