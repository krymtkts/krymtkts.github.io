{:title "F#でコマンドレットを書いてる pt.2"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

平日仕事の方ばっかりやってるので亀な進捗だ。
ひとまず[`System.Management.Automation.WildcardPattern` の使い方がわかった](/posts/2022-06-05-whitespace-comparison-in-pwsh) のを皮切りに、 `-like`,`-match`,`-eq` あたりのフィルタを実装した。

あとプロパティを指定しての絞り込みとかを実装してないが、これは `PSObject` から目当てのプロパティを拾ってマッチするだけなので、多分むずくないだろう。

いま一番頭を悩ませている課題は、絞り込みを確定するまでの間印字する内容についてだ。
やはり PowerShell でインタラクティブなフィルタリングをするのであれば、印字する内容も PowerShell の `Format-Table` ライクな印字をしたい。
例えば [Terminal-Icons](https://github.com/devblackops/Terminal-Icons) を使っていたら、カラフルな `Get-ChildItem` の結果のママ絞り込みしたい。ただそのやり方がさっぱり分からない。

全くわからんなりに、とりあえず F# 内から PowerShell を実行する練習として、 `PowerShell.Create` でコマンドレットを動かしてみただけというコードは動かしてみた。

```fsharp
[<Cmdlet(VerbsDiagnostic.Test, "Pocof")>]
[<OutputType(typeof<PSObject>)>]
type TestPocofCommand() =
    inherit PSCmdlet()

    override __.EndProcessing() =
        __.WriteObject
        <| PowerShell
            .Create()
            .AddCommand("Get-ChildItem")
            .AddCommand("Format-Table")
            .Invoke() // まじで意味ない
```

このへんまだ調査不足のため、以下に記すのはメモ書きレベル。つか [PowerShell Class](https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.powershell?view=powershellsdk-7.0.0) だけでなく PowerShell SDK のドキュメントむずくない？
全体的に読んでてもよくわからん(愚痴)。

- これをやろうとしたら [Format.ps1xml](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_format.ps1xml?view=powershell-7.2) が反映された状態の文字列を F# 内で作らないといけないが、 F# 内で `PowerShell.Create` したとてセッションの引き継ぎができるのかがわからん
  - [PowerShell.Create](<https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.powershell.create?view=powershellsdk-7.0.0#system-management-automation-powershell-create(system-management-automation-runspacemode)>) あたりでどうにかなりそうに見えるけど試せてない
- PowerShell SDK の中には `FormatTableCommand` という名のまさに `Format-Table` そのものがあるが、こいつの Input に `SelectPocofCommand` からデータを食わす方法がわからん
  - これも多分実行時に引数で渡せるようにみえるけど(略) [PowerShell.Invoke Method](<https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.powershell.invoke?view=powershellsdk-7.0.0#system-management-automation-powershell-invoke-1(system-collections-ienumerable)>)
- これらが想定通りで期待の Output が得られたとて、正直な気持ちは F# 内で PowerShell の実行エンジン作ってまでやりたくないなー(なんか重そう)、もっと簡単に `Format-Table` の出力を得る方法はないんかいな、というお気持ち

`System.Management.Automation.WildcardPattern` みたいな外部から見える場所に PowerShell 内部で使ってる機能がいい感じに提供されてたらいいのだけど、そんな感じではなさそう。

見た目に動きのある機能実装ができたときの嬉しさはやっぱひとしおなので、取り組みたい、けどまだ情報＆能力的に不足してるなーというところ。何やるにしても手を動かせるまでに異様に時間がかかる。
ひとまずは調査継続しつつ他の課題潰していくか。

ほんま仕事とは一切接点無くて趣味プロとして最高のテーマになってる。
