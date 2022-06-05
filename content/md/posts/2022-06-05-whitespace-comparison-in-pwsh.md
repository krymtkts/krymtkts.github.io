{:title "PoweShell で半角スペース(U+0020) -eq 全角スペース(U+3000) が True となる"
:layout :post
:tags ["powershell"]}

転職して 3 ヶ月目を迎えた。
入社以降は何の因果か CRM の導入をやることになり、既存データ(スプレッドシート)の以降のために PowerShell を駆使している。

そんな中で今まで知らなかった PowerShell の表情をいくつも知ることができ(例えばコレとか)、なんやかんやで楽しんでいる。

その中で度肝を抜かれたのが、今日のテーマ。

`' '`(U+0020) `-eq` `' '`(U+3000) が `True` となる。

PowerShell 7 と Windows PowerShell 5.1 で試した。

```plaintext
Name                           Value
----                           -----
PSVersion                      7.2.4
PSEdition                      Core
GitCommitId                    7.2.4
OS                             Microsoft Windows 10.0.22000
Platform                       Win32NT
PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0…}
PSRemotingProtocolVersion      2.3
SerializationVersion           1.1.0.1
WSManStackVersion              3.0
```

```plaintext
Name                           Value
----                           -----
PSVersion                      5.1.22000.653
PSEdition                      Desktop
PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0...}
BuildVersion                   10.0.22000.653
CLRVersion                     4.0.30319.42000
WSManStackVersion              3.0
PSRemotingProtocolVersion      2.3
SerializationVersion           1.1.0.1
```

7 だと `True` 、 5.1 だと `False` だ。
`-ceq` だと 7 でも `False` を返すので `-ieq` の判定が違うのだけど、 PowerShell Core からこうなんだろうか？
PowerShell の実装を追ってみたが、追いきれなかった。 めちゃくちゃ[ココ](https://github.com/PowerShell/PowerShell/blob/87f621eb1fa94f1d114b0cc4a5fb7aab5d3133c9/src/System.Management.Automation/engine/parser/Compiler.cs#L5804%E3%83%BCL5806)っぽいのだけど、 LINQ に由来するクラス [DynamicExpression](<https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.dynamicexpression.dynamic?view=net-6.0#system-linq-expressions-dynamicexpression-dynamic(system-runtime-compilerservices-callsitebinder-system-type-system-linq-expressions-expression-system-linq-expressions-expression)>) とかの知識がないのでまだ調査中。

どう説明したらいいか悩むのが、いくつか試してみていてわかってきたのは、 `String` の `-ieq` でこの事象が見られること。 `Char` だと起こらない。

```powershell
' ' -ieq '　' # True
([char][int]'0x20') -ieq ([char][int]'0x3000')  # Char だと False
([string][char][int]'0x20') -ieq ([string][char][int]'0x3000') # String だと True
```

そしてもう 1 つ、 Unicode で General category が Separator, space と定義されているものが等値と判定されること。

[Whitespace character - Wikipedia](https://en.wikipedia.org/wiki/Whitespace_character#Unicode) の表が非常にわかりやすい。ここから以下のテストコードを作成し、先述の事実を導いた。

```powershell
'0x0009', '0x000A', '0x000B', '0x000C', '0x000D', '0x0020', '0x0085', '0x00A0', '0x1680', '0x2000', '0x2001', '0x2002', '0x2003', '0x2004', '0x2005', '0x2006', '0x2007', '0x2008', '0x2009', '0x200A', '0x2028', '0x2029', '0x202F', '0x205F', '0x3000' | % { [PSCustomObject]@{
    CodePoint = $_
    isEquals = ([string][char][int]$_) -eq ' '
}}
```

```plaintext
CodePoint isEquals
--------- --------
0x0009       False
0x000A       False
0x000B       False
0x000C       False
0x000D       False
0x0020        True
0x0085       False
0x00A0        True
0x1680        True
0x2000        True
0x2001        True
0x2002        True
0x2003        True
0x2004        True
0x2005        True
0x2006        True
0x2007        True
0x2008        True
0x2009        True
0x200A        True
0x2028       False
0x2029       False
0x202F        True
0x205F        True
0x3000        True
```

あーココまで来ると、意図的な挙動 ≒ 仕様というのに当たりがつく。
なので .NET の String クラスの挙動を見てみると...

```powershell
' '.Equals('　', [StringComparison]::CurrentCultureIgnoreCase) # True

'0x0009', '0x000A', '0x000B', '0x000C', '0x000D', '0x0020', '0x0085', '0x00A0', '0x1680', '0x2000', '0x2001', '0x2002', '0x2003', '0x2004', '0x2005', '0x2006', '0x2007', '0x2008', '0x2009', '0x200A', '0x2028', '0x2029', '0x202F', '0x205F', '0x3000' | % { [PSCustomObject]@{
>     CodePoint = $_
>     isEquals = ([string][char][int]$_).Equals(' ', [StringComparison]::CurrentCultureIgnoreCase)
> }}
```

```plaintext
CodePoint isEquals
--------- --------
0x0009       False
0x000A       False
0x000B       False
0x000C       False
0x000D       False
0x0020        True
0x0085       False
0x00A0        True
0x1680        True
0x2000        True
0x2001        True
0x2002        True
0x2003        True
0x2004        True
0x2005        True
0x2006        True
0x2007        True
0x2008        True
0x2009        True
0x200A        True
0x2028       False
0x2029       False
0x202F        True
0x205F        True
0x3000        True
```

`StringComparison.InvariantCultureIgnoreCase` は同じ結果。 `StringComparison.OrdinalIgnoreCase` は違う結果となった。

[StringComparison Enum (System) | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.stringcomparison?view=net-6.0)

全然知らんかった...また世の中に恥を晒してしまったと共に、1 つ学んだわ。
まだ確かな情報源を得たわけじゃないけど、きっと世の中のﾄﾞｯﾄﾈｯﾀｰにとっては常識なんやろなあ。
手始めに ↓ コレちゃんと読むようにしよう...

[Best Practices for Comparing Strings in .NET | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings)

---

ちなみにこの調査において、 `-like` には `System.Management.Automation.WildcardPattern` が使われていて、しかも公開されてるクラスというのがわかった。こんなのがあるの知らなかったわ。

そのまま [pocof](https://github.com/krymtkts/pocof#readme) のワイルドカード検索で利用することにした。↓ こういう使い方。

```powershell
([System.Management.Automation.WildcardPattern]"*ui*").IsMatch('kouiuno')
```
