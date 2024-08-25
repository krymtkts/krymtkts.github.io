---
title: "Terminal Icons のよくわからないエラーを解決する"
tags: ["powershell", "terminal-icons"]
---

ちょっと前から、 pwsh 起動時の profile 読み込みでエラーが出るようになった。

どうも出どころは [Terminal-Icons](https://github.com/devblackops/Terminal-Icons/) みたい。 [`$ErrorView = 'DetailedView'`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_preference_variables?view=powershell-7.4#errorview) にして `$Error` で表示した内容は以下の通り。

```plaintext
Exception             :
    Type        : System.Management.Automation.RuntimeException
    ErrorRecord :
        Exception             :
            Type    : System.Management.Automation.ParentContainsErrorRecordException
            Message : Index operation failed; the array index evaluated to null.
            HResult : -2146233087
        CategoryInfo          : InvalidOperation: (:) [], ParentContainsErrorRecordException
        FullyQualifiedErrorId : NullArrayIndex
        InvocationInfo        :
            ScriptLineNumber : 3416
            OffsetInLine     : 5
            HistoryId        : 1
            ScriptName       : C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1
            Line             :     $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme

            Statement        : $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme
            PositionMessage  : At C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1:3416 char:5
                               +     $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme
                               +     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            PSScriptRoot     : C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0
            PSCommandPath    : C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1
            CommandOrigin    : Internal
        ScriptStackTrace      : at <ScriptBlock>, C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1: line 3416
                                at <ScriptBlock>, C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1: line 3414
                                at <ScriptBlock>, C:\Users\takatoshi\OneDrive\Documents\PowerShell\Scripts\PSResource\PSResource.psm1: line 27
                                at <ScriptBlock>, C:\Users\takatoshi\OneDrive\Documents\PowerShell\Microsoft.PowerShell_profile.ps1: line 18
                                at <ScriptBlock>, <No file>: line 1
    TargetSite  : System.Object CallSite.Target(System.Runtime.CompilerServices.Closure, System.Runtime.CompilerServices.CallSite, System.Object, System.Object, System.Object)
    Message     : Index operation failed; the array index evaluated to null.
    Data        : System.Collections.ListDictionaryInternal
    Source      : Anonymously Hosted DynamicMethods Assembly
    HResult     : -2146233087
    StackTrace  :
   at CallSite.Target(Closure, CallSite, Object, Object, Object)
   at System.Dynamic.UpdateDelegates.UpdateAndExecute3[T0,T1,T2,TRet](CallSite site, T0 arg0, T1 arg1, T2 arg2)
   at System.Management.Automation.Interpreter.DynamicInstruction`4.Run(InterpretedFrame frame)
   at System.Management.Automation.Interpreter.EnterTryCatchFinallyInstruction.Run(InterpretedFrame frame)
CategoryInfo          : InvalidOperation: (:) [], RuntimeException
FullyQualifiedErrorId : NullArrayIndex
InvocationInfo        :
    ScriptLineNumber : 3416
    OffsetInLine     : 5
    HistoryId        : 1
    ScriptName       : C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1
    Line             :     $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme

    Statement        : $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme
    PositionMessage  : At C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1:3416 char:5
                       +     $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme
                       +     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    PSScriptRoot     : C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0
    PSCommandPath    : C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1
    CommandOrigin    : Internal
ScriptStackTrace      : at <ScriptBlock>, C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1: line 3416
                        at <ScriptBlock>, C:\Program Files\PowerShell\Modules\Terminal-Icons\0.11.0\Terminal-Icons.psm1: line 3414
                        at <ScriptBlock>, C:\Users\takatoshi\OneDrive\Documents\PowerShell\Scripts\PSResource\PSResource.psm1: line 27
                        at <ScriptBlock>, C:\Users\takatoshi\OneDrive\Documents\PowerShell\Microsoft.PowerShell_profile.ps1: line 18
                        at <ScriptBlock>, <No file>: line 1

```

[`Terminal-Icons.psm1` のここ](https://github.com/devblackops/Terminal-Icons/blob/46866e45a602566bb8a52af5a04dac1d69482c29/Terminal-Icons/Terminal-Icons.psm1#L43-L48) でエラーが出てる。
`the array index evaluated to null.` とのことなので `$userIconTheme.Name` が `$null` であると。んなアホな。

```powershell
# Load user icon and color themes
# We're ignoring the old 'theme.xml' from Terimal-Icons v0.3.1 and earlier
(Get-ChildItem $userThemePath -Filter '*_icon.xml').ForEach({
    $userIconTheme = Import-CliXml -Path $_.FullName
    $userThemeData.Themes.Icon[$userIconTheme.Name] = $userIconTheme
})
```

なんじゃー？と思ったら自分の icon テーマ `krymtkts` の名称が取れてないっぽい。なんでよ。
更に追うと `krymtkts.psd1` から生成される `$env:APPDATA\powershell\Community\Terminal-Icons\krymtkts_icon.xml` で `Name` 要素がダブってた。
`krymtkts_icon.xml` の場合は先頭と末尾にふたつ `Name` 要素のセクションができてた。以下再現イメージ。

```xml
<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
  <Obj RefId="0">
    <TN RefId="0">
      <T>Deserialized.System.Collections.Hashtable</T>
      <T>Deserialized.System.Object</T>
    </TN>
    <DCT>
      <En>
        <S N="Key">Name</S>
        <S N="Value">krymtkts</S>
      </En>
      <En>
        <!-- 省略 -->
      </En>
      <En>
        <S N="Key">Name</S>
        <S N="Value">krymtkts</S>
      </En>
    </DCT>
  </Obj>
</Objs>
```

このように `Name` 要素がダブってると値が `$null` になってだめみたい。

```powershell
$x = Import-CliXml $env:APPDATA\powershell\Community\Terminal-Icons\krymtkts_icon.xml
$x

# Name                           Value
# ----                           -----
# Name                           krymtkts
# Name                           krymtkts
# Types                          {[Directories, System.Collections.Hashtable], [Files, System.Collections.Hashtable], [Directories, Sy…

$x.Name -eq $null
# True
```

なんでこれが発生して解消の仕方わからんので、とりあえず `krymtkts_icon.xml` 末尾の `Name` をコメントアウトし profile 再読み込みで `Terminal-Icons` を再度 import したら、ダブらずに生成されるようになった。
起動時に正常な theme は [`Export-CliXml`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/export-clixml?view=powershell-7.4) されてて、そこでコメントが消えてきれいになったみたい。

流れとしては、 [`Import-Preferences`](https://github.com/devblackops/Terminal-Icons/blob/46866e45a602566bb8a52af5a04dac1d69482c29/Terminal-Icons/Terminal-Icons.psm1#L37) が `prefs.xml` に設定された `CurrentIconTheme` を読み込み、そのあと問題なければ [`Export-CliXml`](https://github.com/devblackops/Terminal-Icons/blob/46866e45a602566bb8a52af5a04dac1d69482c29/Terminal-Icons/Terminal-Icons.psm1#L68-L71) で保存される。
はじめは途中で `Terminal-Icons.psm1` がコケるため保存までいかなかったが、コメントアウトしてエラーが無くなり `krymtkts_icon.xml` が補正された結果が保存されたと。

解せんのは、何故このエラー原因となった `Name` の重複が発生したか、というところ。
元ファイルが変わってないのと Module 自体も変わってないのを考えたら、壊したのは `Export-CliXml` よなー。
`*_icon.xml` が壊れるという意味で似たような Issue はあれど、今回のケースでは XML の syntax がが壊れることはなかったしなんかよくわからん。

[Getting error on importing the terminal icons module · Issue #121 · devblackops/Terminal-Icons](https://github.com/devblackops/Terminal-Icons/issues/121)

ちょっとよくわからんので追いきれない感じがしたが、暫定的でも対処法がわかってよかった。
