{:title "F#でコマンドレットを書いてる pt.12"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

プロパティ指定検索をおおむね実装できて、バグ取りに勤しんでいる。バグ取りしたので割とまともに動いてるな～という感じ。

また、 `Ctrl+Space` で入力候補の表示非表示切り替え、プロパティ名の自動補完もやりたいけど、 [Pull Request](https://github.com/krymtkts/pocof/pull/14) がずっと開きっぱなしで締まりがないし別の Issue に切り分けた。 [#19](https://github.com/krymtkts/pocof/issues/19), [#20](https://github.com/krymtkts/pocof/issues/20)

一旦プロパティ指定検索が完成したら `main` branch に合流する。

手が空いた時しか趣味プロやらないので、小分けにして取り組まないと何やってたかわからなくなる。これは今回デカめの feature にしてしまったことで当初の予定より大幅に長く開発してる。もっと小気味好いリズムを刻みたいので反省点として次に活かす。

...という計画で進めているのでテストケースを書いてたら、エグいバグに気づいた。これ `PSCustomObject` 利用した際に描画できないし、何なら戻り値が破壊される。やば。

ちょうどプロパティ指定検索でテストケースを書くときに `PSCustomObject` 使ってたら、期待の通り動かず、気づいた。このままやとテスト書けへんやん。

ちょっとショッキングなバグだったので初めからか？いつからか？と混乱したが、よくよく考えると普段自分で作った `PSCustomObject` に対してインタラクティブな絞り込みやってないわ...と気づいた。なんで使ってなかったのか？自分で定義するからインタラクティブに調べるほどでもなかったからか。
普段使ってない＆テストザルで見落としてたのか～かっこ悪う...

とりあえず最低限の原因調査として、PowerShell Gallery から `pocof` の旧版をインストールしテストしたところ、 `hahstable` をサポートした [pocof 0.2.0-alpha](https://www.powershellgallery.com/packages/pocof/0.2.0-alpha) でぶっ壊れた模様。
`PSObject` の wrapping を引き剥がすあたりでやってもーてそう。

再現コード。

```powershell
$a = [pscustomobject]@{'a'=1;'b'=2}
$a

# a b
# - -
# 1 2

$b = $a | pocof -NonInteractive
$b

# 虚無が出力される。
```

なんか `NoteProperty` が吹っ飛んでるっぽいのよね。

```powershell
$a.psobject.Members | ? -Property MemberType -eq NoteProperty | Format-list

# MemberType      : NoteProperty
# IsSettable      : True
# IsGettable      : True
# Value           : 1
# TypeNameOfValue : System.Int32
# Name            : a
# IsInstance      : True
#
# MemberType      : NoteProperty
# IsSettable      : True
# IsGettable      : True
# Value           : 2
# TypeNameOfValue : System.Int32
# Name            : b
# IsInstance      : True

$b.psobject.Members | ? -Property MemberType -eq NoteProperty | Format-list

# 無
```

このままだとプロパティ指定検索のテストが書けないので、この bugfix を優先する。
Issue 立てた。 [not work with `PSCustomObject` · Issue #21 · krymtkts/pocof](https://github.com/krymtkts/pocof/issues/21)

テストに関してももうちょい強化が必要かもな。今のところ [Pester](https://github.com/pester/Pester) で書いてる E2E テストのみなのだけど、 `PSCustomObject` 使ったケースなかったし。

コード自体もグチャァ...としてきたから [FsUnit](https://github.com/fsprojects/FsUnit) 導入してもう少し丁寧にやったほうが良さげ。
[`pocof` 書き始めた頃](/posts/2022-05-07-start-to-write-cmdlet-by-fsharp)に「可能な限りテストを書きたい所存」て書いてたけど有言不実行になってて笑える(笑えない)。
