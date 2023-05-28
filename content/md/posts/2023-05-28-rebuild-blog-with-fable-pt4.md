{:title "Fable でブログを再構築する pt.4"
:layout :post
:tags ["fsharp", "fable"]}

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築しようとしている。

YAML で書かれたメタデータ(Front Matter というらしい)の抽出を作った。 [#5](https://github.com/krymtkts/blog-fable/pull/5)

またそれに伴って記事へのリンクのタイトルに front matter が使えるようになったので諸々手直しした。 [#10](https://github.com/krymtkts/blog-fable/pull/10)

front matter 抽出については、まず最初に remark 使ってみた。
けど、 [ts2fable](https://github.com/fable-compiler/ts2fable) で binding を出力したところ [unified](https://github.com/unifiedjs/unified) 、 [remark](https://github.com/remarkjs/remark) とその Plugin たちの間で型の整合性が取れなかった。
整合取ろうとしたら自力で調整しないとどうにもならないので、とてもじゃないがすぐには使えないと判断して早々に諦めた。
remark のパーサを作る部分を JavaScript で書いて、それを import する形でやれば簡単にできるが、そこは F# で書いてこそ意味がある。

というわけで marked のまま、 front matter の抽出を古典的な方法、つまり正規表現を使って自力でやった。
front matter 自体は YAML で書くので、 [eemeli/yaml](https://github.com/eemeli/yaml) を導入した。 yaml module は ts2fable で Fable の binding が生成されなかったので、雑なやつを手で書いた。

front matter 抽出ができるようになると、 Archives や Tags で作ってたページリンクにメタデータのタイトルを使えるようになる。
タグ付けされたページにタグリンクもつけられるし。
それに伴い諸々の手直しをし、ついでに参照元のコードのまま使ってた CSS とかを最新のものまで更新した。とりまいまは CDN のを借りてる。
ついでに highlight.js の CSS を足した。
Index Archives Tags が出揃って、ページにタグが付き、 syntax highlighting も効くようになったから、結構ブログっぽくなってきたのでは？

諸々のスタイル調整はぜんぜんやってないけど。
スタイル調整に対する欲求が低くて、どうしようか考えるとのがとてつもなくだるい。とりあえず色味だけは Solarized Dark でいこうと思ってるけど。ただ Solarized Dark のコンテンツに Solarized Dark の syntax highlighting だと境界ワカランのちゃうかな？というのはある。なので現にいまのブログでは Solarized Light をコンテンツに使ってるし。
スタイルに関してはこのまま Bulma でいくか決めないといけないのだけど、こういう感じの自分であまり考えたくないケースに案外ハマってるかも、しらんけど。

最近の機能拡充に伴ってエントリーポイントである `App.fs` と `Helpers.fs` がむやみに肥大化しているので、そろそろ整理したいところ。
いまは `App.fs` に色々書いてるけど、これ多分 `render` 関数以外はもう module に切り出した方がいいねんよな。

イメージ的には、 `render` 関数で用意した関数を組み合わせるだけにする。
いま navbar に表示するページだったり index に最新のポストを表示するのが固定でやってるけど、こういうのも front matter に寄せたらスッキリするかな。
設定ファイルとかはなしで、ディレクトリ構造とコンテンツの front matter だけで制御できたら楽かなあ。

道程は長い。
