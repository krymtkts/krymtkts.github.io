{:title "PowerShell でクソデカテキストファイルを作る"
:layout :post
:tags ["powershell"]}

先日、クソデカテキストファイルを作成しなければならない場面があり、以下のスクリプトをしたためた。
Windows なので単にサイズが大きいだけのファイルなら `fsutil createnew` を使えるけど、クソデカテキストファイルを作る手段は知らなかったからだ。

```powershell
Set-Content kusodeka.txt -Encoding ascii -NoNewline -Value ('x' * [Math]::Pow(1024, 3))
```

このスクリプト、 Out of Memory でエラー終了する。

```powershell
PS> Set-Content kusodeka.txt -Encoding ascii -NoNewline -Value ('x' * [Math]::Pow(1024, 3))
OperationStopped: Exception of type 'System.OutOfMemoryException' was thrown.
```

どうも文字列の確保できる最大サイズの制限みたい。

```powershell
PS> 'x' * [Math]::Pow(1024, 3) | Out-Null
OperationStopped: Exception of type 'System.OutOfMemoryException' was thrown.
```

↓ のサイズならいける。 1GB - 32B からはエラーになる。

```powershell
'x' * ([Math]::Pow(1024, 3)-33) | Out-Null
```

答えは.NET の`String`クラスのドキュメントに書いてた →[String Class (System) | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.string?redirectedfrom=MSDN&view=net-6.0)

> The maximum size of a String object in memory is 2-GB, or about 1 billion characters.

2GB には到底届いてないし、今回引っかかってるのは後者か。
ほーん、という感じ。最大の文字列長とか考えたこともなかったわ。

因みにこの最大文字数の超過エラーを回避してクソデカテキストファイルを作るには、以下のようにデータを分割して小分けに書き込みする必要がある。

```powershell
1..1024 | Add-Content kusodeka.txt -Encoding ascii -NoNewline -Value ('x' * [Math]::Pow(1024, 2))
```

ついでに調べた PowerShell の配列の上限は、添字の型であろう `int` の範囲っぽいがドキュメントは見当たらなかった。これはまた PowerShell のソースコードでも読むか。

```powershell
# いける
[int]::MinValue..[int]::MaxValue | Out-Null

# いけない(int の演算エラーかこれ)
[int]::MinValue..([int]::MaxValue + 1) | Out-Null
# > OperationStopped: Value was either too large or too small for an Int32.

# いけない(カッコで評価されちゃい overflow か)
([int]::MinValue..[int]::MaxValue) | Out-Null
# OperationStopped: Arithmetic operation resulted in an overflow.
```

世の中まだ知らないことがいっぱいあるもんやなあ。

### 2022-03-15 追記

```powershell
# これが限界ぽい。
(1..2147483591) | Out-Null

(1..2147483592) | Out-Null
# OperationStopped: Array dimensions exceeded supported range.
```

でエラー。この場合ちゃんと Array の range の問題だとエラーに出る。
カッコを入れてなかったら評価が端折られて期待の振る舞いをしていなかった。

[Array Class (System) | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.array?view=net-6.0#remarks) に記載のカッコの中に該当するわけやな。

> The array size is limited to a total of 4 billion elements, and to a maximum index of 0X7FEFFFFF in any given dimension (0X7FFFFFC7 for byte arrays and arrays of single-byte structures).

```powershell
[long]::Parse('7FFFFFC7',[System.Globalization.NumberStyles]::HexNumber)
# 2147483591
```

いやー、スッキリした！
