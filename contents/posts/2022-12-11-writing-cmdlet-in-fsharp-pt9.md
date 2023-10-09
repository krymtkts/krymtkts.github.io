{:title "F#でコマンドレットを書いてる pt.9"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

師走は何かと忙しくて pocof のプロパティ指定検索対応に時間が割けていない。
あんまり手も動かせないので、プロパティ指定どうやったらいいかなーと思案している。
つまりこの記事は壁打ちだ。

どー考えても一番楽なのは、 [`PSCmdlet.InvokeCommand`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.pscmdlet.invokecommand?view=powershellsdk-7.0.0) にクエリ文字列から組み立てた `FilterScript` を渡す方法。
でもこのやり方でやったとして自分の知見が広がるわけでもないし、楽しくない。

ということでどうせなら F# の世界からどうにかしてやろうと思っている。
やはり正攻法なのは、[`PSObject.Properties`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.psobject.properties?view=powershellsdk-7.3.0)にアクセスする方法だろう。

難点は、 pocof の `InputObject` を内部では `PSObject[]` から `obj list` に変えてしまってるので、それらを適宜補正してやる必要があるくらい(そもそもなんで `obj list` にしたか覚えてない)。なんかできそうに思えてきた。

フィルタの方は、 `PSObject.Properties` にクエリ文字列をパースして抜き出したプロパティ名で値を取れば、クエリ式内で `where` に使うのは造作もないことだろう。
`hashtable` のときだけ特別サポートしているので、そこと折り合いつけれれば困ること無いのでは(油断している)。

プロパティの入力候補出すやつは...どうかな。
パラメータ `InputObject` に単一の型だけが含まれるのであれば、先頭の要素の `PSObject.Properties` から候補を一覧するだけでいいのだけど、奇しくも pocof は複数の型も許容する包容力を持ってるから...

まー単純に考えて、全走査しているタイミングは 2 つあるので、そこで型情報も一緒に控えておくのが良かろう。

1. `PSCmdlet.ProcessRecord` で `list` に詰めてるのでその時に `GetType` する
2. `List.rev` してるときか(`ProcessRecord` で計算量減らすため逆に詰めているから)

フツーに `PSCmdlet.ProcessRecord` いいな。型情報取るだけならなんかこれもできそうな気配してきたな。

問題は取得したプロパティをどう表示するかやけど、スペース的に空いてるのは正規表現パターンのエラー表示するための行だけなので、そこに出そう。要素数が多い場合は水平方向のスクロール機能(！)を実装しないと使い物にならなさそう...全く実装イメージつかんなこれ。

会社の期末が 2023-01 なので、なんか来年頭は忙しくなりそうやな～と思うし、この年末年始のお休み期間で如何に F# と戯れることができるかが重要な気がする。 pocof の至らぬ点諸々の棚卸しもやっておきたいし。
それに今年の振り返りもボチボチやり始めとかないと...
