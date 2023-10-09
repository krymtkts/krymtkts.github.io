{:title "F#でコマンドレットを書いてる pt.13"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。
[pocof 0.4.0-alpha](https://www.powershellgallery.com/packages/pocof/0.4.0-alpha) を公開した。

諸々の bugfix を主として行った。
[#28](https://github.com/krymtkts/pocof/issues/28) [#25](https://github.com/krymtkts/pocof/issues/25) [#23](https://github.com/krymtkts/pocof/issues/23) [#21](https://github.com/krymtkts/pocof/issues/21) [#20](https://github.com/krymtkts/pocof/issues/20) [#18](https://github.com/krymtkts/pocof/issues/18)

修正自体は軽微だが、結構使っていてイヤーな感じをもたらす bug が多かったし、直して良かった。
機能的なところでは入力中のプロパティ候補の表示非表示を切り替えるオプションを付与したのみに留まる。

正直なところプロパティの TAB 補完できてから公開したかったが、先送りにした。
こいつをやるには内部的な状態を増やす必要があると思っていて、そのためにはリファクタリングをしてから着手する方が良い。
要はリファクタリングしだすと、色々気になって手を入れてしまいズルズルとリリースを遅らせがちなので、切の良いタイミングで公開しておいたという算段だ。

増やす状態は、カーソルがクエリの分かち書きの何個目にいるか、単語の何文字目にいるとかを指す。 TAB 補完したときにクエリの書き換えとカーソル末尾への移動をするが、その計算にこれらの状態があると良い。
今の実装だと単純にクエリ文字列と検索中のプロパティを保持しているだけなので、そこを小綺麗に整理した後にそれらを付与するようなイメージ。判別共用体を上手く使ってできる感覚を持っている。

ただしデータ構造の変更に伴いクエリ実行そのものも書き換える必要がある。
[前の日記](/posts/2023-02-05-writing-cmdlet-in-fsharp-pt12) でも触れてたように、やっぱこのタイミングで [FsUnit](https://github.com/fsprojects/FsUnit) 導入して動作保証しながらの開発は必須、避けられない。

話は逸れるが、FsUnit 使うに当たり .NET のテストライブラリを入れる必要があるのだけど、全然知らん。
[What is FsUnit?](https://fsprojects.github.io/FsUnit/index.html) を見るに [xunit](https://github.com/xunit/xunit) 、 [nunit](https://github.com/nunit/nunit) と [MSTest](https://github.com/microsoft/testfx) がある。
FsUnit からはどれでも使えるけどそれぞれの推しどころわからず、どれを使えばいいかわからんというアレな状態。
GitHub の star 見るに xUnit が人気ぽいが、 [fantomas](https://github.com/fsprojects/fantomas/blob/a7ed99fb74fee6db55487f315005c0200d19a0b4/src/Fantomas.Tests/Fantomas.Tests.fsproj) は FsUnit と NUnit の組み合わせっぽかった。
FsUnit のドキュメント見てたら、強力な型付きの [FsUnitTyped](https://fsprojects.github.io/FsUnit/FsUnitTyped.html) は NUnit か xUnit しかサポートしてないって書いてるし、なんとか二択までは絞れた...こっからがだるい。

話を戻して、クエリの内部状態以外にも判別共用体で書き直したい箇所があって、それは入力時のキー判定や今は文字列で保持しているキーマップだったりだ。
キーマップの方は外部保存した設定ファイルを読み込む方式も検討したいのでハマるかわからんけど。入力時のキー判定の方は、いま if 式をこねくっているのもあって、パターンマッチング式に置き換えられるからハマるなという感覚がある。

また機能追加が滞りそうではあるが、あと 2 ヶ月ほどで pocof の initial commit から 1 年経つのもあり、掃除するにはちょうどいい頃合いではないかと。ちょっとは F# Ninja Level も上がったやろ。
