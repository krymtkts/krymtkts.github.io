{:title "F#でコマンドレットを書いてる pt.3"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

[PowerShell Class](https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.powershell?view=powershellsdk-7.0.0) を使って、現在の runspace を引き継がせるとかはちょっとわかりまして、 (F# というか .NET)の世界で PowerShell の表示文字列を取り回すには PowerShell Class を使い最後に `Out-String` した結果を取り回すのが典型っぽいのはわかった。

ただやっぱり PowerShell の力を借りない方法を諦めきれずにいた。
また今後プロパティ指定での検索を実装していったときに、指定したプロパティはなんか色付けするとかしようとしたらこっちの方が絶対やりやすいだろうというのを感じてたので、[Composite formatting](https://docs.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting) を使ってどうにかできないものか考えていた。

が、結論としては PowerShell の力を借りるのが良いのでは～...というところまできた。

結局のところ `format.ps1xml` に従って PowerShell で表示が制御されるので、 F# の中で `PSObject.Properties` を見たとてどいつを優先表示したらいいかわからん。またこの `Properties` から表示するテーブルを作るのは `hashtable` や `array` が引数の場合には適応したくないし、となると型でパターンマッチして～となる。
考えるとここにこだわるのはもっとプロパティでの絞り込みとかの他の機能を実装したあとでいいかなーという気になってきた。

`format.ps1xml` もそうだけど PowerShell 内部実装に対する基礎知識(`PSPropertyInfo`とかまじで情報ない)がやっぱ圧倒的に不足してるから、蓄積されるまでは黙って PowerShell に従うが良いかと考えを改めた。
であれば、もう PowerShell Class を使う方向に倒して、検索中のプロパティを `Format-Table` の `-Properties` で表示するようにしようかな、という選択肢もありかなと。

いったんこの形で検索中の表示を進めれば、他に組み込んでおきたい機能、例えばプロパティ指定の検索とかに取りかかれる。
週末の 1,2 時間が主な開発時間なので、亀な進捗の中でも実装を進められる判断が必要かなー。それもまた一興。
