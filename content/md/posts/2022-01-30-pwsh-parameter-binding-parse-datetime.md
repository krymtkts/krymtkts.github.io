{:title "PowerShell のパラメータバインディングは \"3 時\" を Datetime 型にパースする"
:layout :post
:tags ["powershell"]}

### いきなりまとめ

PowerShell の関数なりコマンドレットがパラメータを受け取る場合、パラメータバインディングの仕組みで型ごとの変換処理をしており、日付型のパラメータでは`DateTime.Parse` しているのがわかった。

---

### 経緯

先日 [ScheduledTask を設定した](/posts/2022-01-23-scheduled-task-in-powershell)くだりで初めて知った。

[New-ScheduledTaskTrigger (ScheduledTasks) | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/module/scheduledtasks/new-scheduledtasktrigger?view=windowsserver2022-ps#example-1--register-a-scheduled-task-that-starts-a-task-once)

を見ていて、 `-At 3pm` て何これ？と思って日本語も試した。
使い所がわかりかねるが、以下のようなジャパナイズされた入力でも OK!
ただし漢数字、曜日や午前/午後は `Parse` できないので無理な。

参照: [Convert strings to DateTime | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/standard/base-types/parsing-datetime)

```powershell
PS> Get-Date 15時

Wednesday, January 16, 2022 03:00:00

PS> Get-Date 6年6月6日

Monday, June 6, 2006 00:00:00

PS> Get-Date 3じ
Get-Date: Cannot bind parameter 'Date'. Cannot convert value "3じ" to type "System.DateTime". Error: "The string '3じ' was not recognized as a valid DateTime. There is an unknown word starting at index '1'."
```

どういう仕組や。

#### 大まかな予測

コマンドレットや関数に渡す前、引数の型 `DateTime` の時点で捏ねくっている様子。

```powershell
function hiduke() {
  param(
    [datetime]
    $d
  )
  $d
}
```

```powershell
PS> hiduke -d 3時

Wednesday, January 16, 2022 03:00:00
```

キャストとパースの違いを見る。

```powershell
PS> [DateTime]::Parse('3時')

Wednesday, January 16, 2022 03:00:00

PS> [DateTime]::Parse('3じ')
MethodInvocationException: Exception calling "Parse" with "1" argument(s): "The string '3じ' was not recognized as a valid DateTime. There is an unknown word starting at index '1'."

PS> [DateTime]'3時'

Wednesday, January 16, 2022 03:00:00

PS> [DateTime]'3じ'
InvalidArgument: Cannot convert value "3じ" to type "System.DateTime". Error: "The string '3じ' was not recognized as a valid DateTime. There is an unknown word starting at index '1'."
```

同じエラーメッセージ出てるので、 PowerShell が指定の型に評価するのに `DateTime.Parse` を呼んでる。
真面目に考えたことなかったが、PowerShell 自体の機能でパラメーターバインディングというらしい。
[about Parameters - PowerShell | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_parameters?view=powershell-7.2)

(今更ながら)また一つ学んでしまったようだな...

パラメーターバインディングの処理内容を知るには `Trace-Command` が使える。

```powershell
PS> Trace-Command -PSHost -Name ParameterBinding {Get-Date 3じ}
DEBUG: 2022-01-30 14:47:05.5488 ParameterBinding Information: 0 : BIND NAMED cmd line args [Get-Date]
DEBUG: 2022-01-30 14:47:05.5491 ParameterBinding Information: 0 : BIND POSITIONAL cmd line args [Get-Date]
DEBUG: 2022-01-30 14:47:05.5493 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 14:47:05.5495 ParameterBinding Information: 0 :         BIND arg [3じ] to param [Date] SKIPPED
DEBUG: 2022-01-30 14:47:05.5496 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 14:47:05.5497 ParameterBinding Information: 0 :         BIND arg [3じ] to param [Date] SKIPPED
DEBUG: 2022-01-30 14:47:05.5499 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 14:47:05.5501 ParameterBinding Information: 0 :         BIND arg [3じ] to param [Date] SKIPPED
DEBUG: 2022-01-30 14:47:05.5503 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 14:47:05.5504 ParameterBinding Information: 0 :         COERCE arg to [System.DateTime]
DEBUG: 2022-01-30 14:47:05.5506 ParameterBinding Information: 0 :             Trying to convert argument value from System.String to System.DateTime
DEBUG: 2022-01-30 14:47:05.5509 ParameterBinding Information: 0 :             CONVERT arg type to param type using LanguagePrimitives.ConvertTo
DEBUG: 2022-01-30 14:47:05.5519 ParameterBinding Information: 0 :             ERROR: ERROR: COERCE FAILED: arg [3じ] could not be converted to the parameter type [System.DateTime]
(...端折る...)
Get-Date: Cannot bind parameter 'Date'. Cannot convert value "3じ" to type "System.DateTime". Error: "The string '3じ' was not recognized as a valid DateTime. There is an unknown word starting at index '1'."
```

パラメータバインディング時によしなにしてるのがわかってきたところで、力尽きた。
PowerShell のパラメータバインディングの仕組みに関しては宿題やな。気長に見ていくしかない。

#### 潜る

ここまで来れたので、次は PowerShell のパラメータバインディングのコードに潜り込む。

`Trace-Command` の ParameterBinding Information に出てた

- `"COERCE FAILED: arg .+ could not be converted to the parameter type "`
- `"CONVERT arg type to param type using LanguagePrimitives.ConvertTo"`

から、 [`ParameterBinderBase.cs`](https://github.com/PowerShell/PowerShell/blob/master/src/System.Management.Automation/engine/ParameterBinderBase.cs#L1262)にたどり着いた。

例外がスローされたのがどこか `LanguagePrimitives.ConvertTo` から先を探るのにはちょっと情報が足りなかったので、`Trace-Command` に TypeConversion を足して出力した。

TypeConversion が有用なのがわかったのは、[`LanguagePrimitives.cs` 内の `ConvertTo`](https://github.com/PowerShell/PowerShell/blob/0ef30e54c70b9d5d69a35d1aeecdf2820cc1ab3b/src/System.Management.Automation/engine/LanguagePrimitives.cs#L4837)を追ってたらトレース情報にまんまその名前が出てきたから。

```powershell
PS> Trace-Command -PSHost -Name ParameterBinding,TypeConversion {Get-Date 3じ}
DEBUG: 2022-01-30 15:36:52.2304 ParameterBinding Information: 0 : BIND NAMED cmd line args [Get-Date]
DEBUG: 2022-01-30 15:36:52.2309 ParameterBinding Information: 0 : BIND POSITIONAL cmd line args [Get-Date]
DEBUG: 2022-01-30 15:36:52.2312 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 15:36:52.2314 ParameterBinding Information: 0 :         BIND arg [3じ] to param [Date] SKIPPED
DEBUG: 2022-01-30 15:36:52.2315 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 15:36:52.2318 ParameterBinding Information: 0 :         BIND arg [3じ] to param [Date] SKIPPED
DEBUG: 2022-01-30 15:36:52.2320 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 15:36:52.2322 ParameterBinding Information: 0 :         BIND arg [3じ] to param [Date] SKIPPED
DEBUG: 2022-01-30 15:36:52.2325 ParameterBinding Information: 0 :     BIND arg [3じ] to parameter [Date]
DEBUG: 2022-01-30 15:36:52.2326 ParameterBinding Information: 0 :         COERCE arg to [System.DateTime]
DEBUG: 2022-01-30 15:36:52.2329 ParameterBinding Information: 0 :             Trying to convert argument value from System.String to System.DateTime
DEBUG: 2022-01-30 15:36:52.2331 ParameterBinding Information: 0 :             CONVERT arg type to param type using LanguagePrimitives.ConvertTo
DEBUG: 2022-01-30 15:36:52.2336 TypeConversion Information: 0 :             Converting "3じ" to "System.DateTime".
DEBUG: 2022-01-30 15:36:52.2346 TypeConversion Information: 0 :                 Exception calling Parse method with CultureInfo: "The string '3じ' was not recognized as a valid DateTime. There is an unknown word starting at index '1'.".
DEBUG: 2022-01-30 15:36:52.2350 ParameterBinding Information: 0 :             ERROR: ERROR: COERCE FAILED: arg [3じ] could not be converted to the parameter type [System.DateTime]
(...端折る...)
Get-Date: Cannot bind parameter 'Date'. Cannot convert value "3じ" to type "System.DateTime". Error: "The string '3じ' was not recognized as a valid DateTime. There is an unknown word starting at index '1'."
```

ParameterBinding Information のエラー出力までに TypeConversion Information が足されたのがわかる。いい感じじゃないか。

このキーワードを元にコードを探ると、

- [`LanguagePrimitives.cs` 内の `FigureParseConversion`](https://github.com/PowerShell/PowerShell/blob/0ef30e54c70b9d5d69a35d1aeecdf2820cc1ab3b/src/System.Management.Automation/engine/LanguagePrimitives.cs#L5226-L5287) でパースに使うメソッドをリフレクションで取得している
- [`LanguagePrimitives.cs` 内の `ConvertViaParseMethod`](https://github.com/PowerShell/PowerShell/blob/0ef30e54c70b9d5d69a35d1aeecdf2820cc1ab3b/src/System.Management.Automation/engine/LanguagePrimitives.cs#L3747-L3780) で取得したメソッドを使ったパースが行われてる

というのがわかった。

やっぱり `Datetime.Parse` を使ってたんや。あー、スッキリした！

というか PowerShell 使ってる割にちゃんと勉強してないから、せなあかんな。
