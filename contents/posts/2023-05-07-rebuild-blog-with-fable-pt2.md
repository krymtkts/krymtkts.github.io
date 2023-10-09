{:title "Fable でブログを再構築する pt.2"
:layout :post
:tags ["fsharp", "fable"]}

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の話。
Fable でブログを再構築しようとしている。

repo 公開時点で `README.md` をレンダリングするだけに留めていたので、ブログらしく post と固定ページ を出力できるように最低限手を加えた。
[#2](https://github.com/krymtkts/blog-fable/pull/2)

あと node module に型を与える Fable bindings を利用しようと思い、 [markedjs/marked](https://github.com/markedjs/marked), [highlightjs/highlight.js](https://github.com/highlightjs/highlight.js) でそれを試した。
これが結構大変で、まだ慣れてない。

bindings は自動生成できる。それには [fable-compiler/ts2fable](https://github.com/fable-compiler/ts2fable) を利用する。

```powershell
# こんなん。型定義されてない module には使えない。
npx ts2fable node_modules\@types\marked\index.d.ts src/bindings/Marked.fs
npx ts2fable node_modules\@types\highlightjs\index.d.ts src/bindings/HighlightJs.fs
```

で出力された型を Fable の F# コードで利用するのだけど、うまく使うために色々覚えないといけない Fable 独自の型・関数・演算子がある。
`ImportAttribute` 、 Union Type の `U2` その他大勢、動的キャスト演算子 `!!` と Anonymous record の組み合わせ、 `jsOptions<'a>` ...
まだ勉強中だが、早い話 [Fable · Call JS from Fable](https://fable.io/docs/communicate/js-from-fable.html) に載ってるの全部知っといた方というか知っとかないとキツイ雰囲気を感じている。
何故なら Fable の真髄であろう JavaScript と F# の型で守られた相互運用がそこに詰まっており、知らないとうまくできないから。

寝かしつけ中にこのページ重要そうなの気づいて何度も読んだけど、実際に書いてみると全く活かせてない。また何度も読み直してる。
何の気なく書かれてる一行のサンプルコードが重要だったり。 JavaScript の Module system に詳しいとピンとくるんかなー。
このテーマに関しては GPT-4 に聞いたところでホラ吹いてくるので、自力で勉強せなあかん。

あと ts2fable で自動生成できない場合は自力で bindings を書く必要があるところも、パンチが効いてる。
今の marked で Syntax Highlighting を使うには[markedjs/marked-highlight: Add code highlighting to marked](https://github.com/markedjs/marked-highlight) を使う。
けどこいつには Type Declarations `.d.ts` が提供されてないので、今回は `Fable.Core.JsInterop` の `importMember` で茶を濁した。
highlight.js の方も、実は Type Definitions が古くて新しいインタフェースになってない。

このように Fable の技術スタックは JavaScript のそれの上に乗っかって相互に混ざり合ってるので、両方に対して結構詳しくなるまで辛い気がする。
ただその頂きを超えたところに Fable の楽しみがあるのかも、しらんけど。
まあ書いていておもろいのは確かで、色々つまづきながら進めている。

あとここまでやって気づいたのだけど、 marked 自体には Markdown 中に書かれた `title` とか `tag` とかを読み取る機能ないっぽい。
実現しようとしたらなんか自分で `---` でファイルコンテンツ区切って YAML にパースするとかしないとだめみたい。
そもそもこのメタ情報の部分 YAML だとこの機会に初めて知ったわ。
自力で書いてもいいけど、この際 [remarkjs/remark](https://github.com/remarkjs/remark) みたいなプラグインでそれが実現できるやつを使うのもいいかなと思った。
Markdown のレンダリング部分を換装するのは大変じゃなさそうなので、 navbar 作るとか RSS 作るとかの後に回す。

他にも、開発用途で問題ないとはいえ [tapio/live-server](https://github.com/tapio/live-server) にいちいち脆弱性の警告出るのも煩わしく、ここを F# のスクリプトに置き換えたい。

やりたいこと盛りだくさん。年内には SSG 乗り換え実施したいな...なんとなく。
