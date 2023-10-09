{:title "F#でコマンドレットを書いてる pt.4"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

めちゃめちゃ放置してたのだけど、 8 月末くらいから触れるようになってきたので、気になる部分ですぐ変えられる部分を書き換えて、 PowerShell Gallery に公開した。 [はじめたころ](/posts/2022-05-07-start-to-write-cmdlet-by-fsharp)から 4 ヶ月くらい経ったのか。

[PowerShell Gallery | pocof 0.1.0-alpha](https://www.powershellgallery.com/packages/pocof/0.1.0-alpha)

現時点に公開する動機としては、自分の普段遣いでは未だ `poco` を使ってるというのもあって、折角なのでそれを `pocof` に変えようと考えた次第だ。
一部機能に関しては未実装なので、オプションはコメントアウトして隠した形で公開している。

大きい変更としては、PowerShell オブジェクトを作成せずに `PSCmdlet` のサブクラスの中で PowerShell の Cmdlet を呼び出す方法がわかったので、その辺を書き換えた。
これが非常に参考になった →[InvokeCommand.InvokeScript not returning Output · Issue #12137 · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues/12137)

DLL の モジュールを公開するのは初めてだったので、これまた躓きつつも先達の知恵を借りて乗り越えられた。
これ → [Writing a PowerShell Core Module With F#, A Complete Guide | Brianary](https://webcoder.info/fspsmodule.html)

結局始めた頃はフォルダ指定の公開でええんちゃうかと思ってたが、 `Import-Module` してから公開する方がめっちゃ楽なので、参照元に従いそう変えちゃった(こだわり無し)。
そもそもこの記事がなかったら `pocof` はサクッと始めてなかったので、感謝しかない。

それと特に意図があった訳ではないが、 [PowerShell Module Manifest](https://docs.microsoft.com/en-us/powershell/scripting/developer/module/how-to-write-a-powershell-module-manifest?view=powershell-7.2) で使ったことがなかった `PreRelease` を使ってみたりもした。

公開にあたっての目玉機能は何もない。何なら `poco` の機能で未だ実装してないものもある。
ただいくつか自分が `poco` を使っていて困ってたことは、これで解消される。

- 正規表現パターンの記述中にエラーで落ちない
- クエリ記述中に左右カーソル移動ができる(buggy な疑いアリ)
- (あとテスト用の非対話モード ← 普段利用ではまじで意味ないけどテストが楽)

今後欲しい機能としては、とりあえず自分の使い方だと、プロパティ周り。
プロパティ指定のフィルタとプロパティ入力の補完ができたら相当楽になる。表示するプロパティが選べたりしたら最高。

`pocof` で印字されたアイコングリフが `??` になってたり他にも色々未実装・直さないといけない点あれど、自分が普段使うツールを自分の手でコントロールできる(しかも学習まで兼ねて)というのは、イイことやなと改めて思った。
