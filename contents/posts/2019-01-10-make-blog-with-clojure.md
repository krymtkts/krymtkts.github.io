---
title: "Clojureでブログを作った"
tags:  ["clojure", "cryogen"]
---

このブログはClojure製の静的サイトジェネレータ[Cryogen](http://cryogenweb.org/)で作った。

### 動機

現在有給消化中のため暇である。プログラマ的暇潰しが必要だったのだが、ブログを作るのはそれなりに楽しめそうな気がした。

あと、過去にブログサービスを使ってたときブログが長続きしなかった理由に、わたしは物書きじゃあないので簡単にブログが書けるとかいう部分が琴線に触れない、というようなものが根底にあるような気もする🤔

自分で作るならその心配はない。

ついでに折角GitHubのアカウントも持ってるので、GitHub Pagesを使わない手はない。blogのコードをrepoに登録しておけば芝生も青々としてええな！的な。

また別の観点としてブログサービスを選ぶとなった場合、書く記事の内容が技術的なものだったり単なる趣味の話だったり内容がブレると思うので、利用規約に触れて垢バンされるようなサービスは選び辛い。ちょうど良い選択かも知れない。

そこで今回はブログ自体を自分で作ってしまおうと決めた。

因みにClojureを選んだのは個人的な好みである。

### 静的サイトジェネレータの選定

普通にググって調べようと思ってたけど、いまはこんなのクソ便利なんがあるのね...→[StaticGen](https://www.staticgen.com/)

LanguageをClojureにしたら3個hitしたのでそれぞれ見てみた。

- [Cryogen](https://github.com/cryogen-project/cryogen)
- [Misaki](https://github.com/liquidz/misaki)
- [Perun](https://github.com/hashobject/perun)

Misakiは永らくメンテされてないからなし。
CryogenもPerunも、検索したら日本語の情報にhitするのでやりがいに違いはなさそう。
単純に⭐が多いのとBootを使ったことがないというだけの理由で[Cryogen](https://github.com/cryogen-project/cryogen)を使うことにした。

(後知恵だが、ここはもっと慎重に考えた方が良かった)

### [Cryogen](https://github.com/cryogen-project/cryogen)を使ってぶろぐを作ろうぜ

ドキュメントが充実してるので、書いたてることに従うだけで簡単にできた。

出来上がったコードはこちら→[My personal blog project](https://github.com/krymtkts/blog-cryogen)

以降に記すコマンド例はPowerShellで実行したものである。筆者はWindows10ユーザなので。

#### 手始めに

Leiningenでテンプレートを作成する。

```powershell
lein new cryogen blog
Retrieving cryogen/lein-template/0.3.7/lein-template-0.3.7.pom from clojars
Retrieving leinjacker/leinjacker/0.4.2/leinjacker-0.4.2.pom from clojars
Retrieving org/clojure/core.contracts/0.0.1/core.contracts-0.0.1.pom from central
Retrieving org/clojure/pom.contrib/0.0.26/pom.contrib-0.0.26.pom from central
Retrieving org/clojure/core.unify/0.5.3/core.unify-0.5.3.pom from central
Retrieving cryogen/lein-template/0.3.7/lein-template-0.3.7.jar from clojars
Retrieving org/clojure/core.contracts/0.0.1/core.contracts-0.0.1.jar from central
Retrieving org/clojure/core.unify/0.5.3/core.unify-0.5.3.jar from central
Retrieving org/clojure/clojure/1.4.0/clojure-1.4.0.jar from central
Retrieving leinjacker/leinjacker/0.4.2/leinjacker-0.4.2.jar from clojars
Generating fresh 'lein new' Cryogen project.
```

ブログを動かしてみよう

```powershell
lein ring server
```

ブラウザに表示された。OK👍

#### コンテンツを整理する

わたしの場合はMarkdownで書くのでAsciiDocのディレクトリは消してしまう。サーバ起動中にこれをやると例外が発生する、止めてからやるのが良いだろう。

```powershell
Remove-Item -Recurse -Path ./resources/templates/ascii
```

あとsampleで置いてあるpostやaboutを自分用に書き換えるなど。

#### テーマを作る

themeはデフォで`blue`, `blue_centered`, `lotus`, `nucleus`の4種類置いてある。`lotus`を使うとエラーになったけど、これを直すのが目的じゃないので無視した🙈

自分用のテーマとしては何が良いかな？と検討して、お気に入りのSolarizedにしようと決めた。terminalやeditorはSolarized darkを使っているが、ブログはlightでやろうと思う。syntax highlightingについてはいつも通りのdarkを採用することにした。

[Solarized](https://ethanschoonover.com/solarized/)を参考に自分でthemeを作る。元にするのは`blue_centered`にした。ブラウザの開発者ツールで見てみてもレスポンシブデザインになってたし、cssのコード量も少なくてシンプルなのがいい。

`resources/templates/themes`にファイル名`solarized_light`でコピって書き換える。

基本は書いたあるルールに沿うけど一部それとなく変える。カラーコードの編集はsassにしたら楽だろうけどコピった元はそうじゃないので、該当する箇所を書き換えるだけに留めた。

Cryogenのsyntax highlightingは`highlight.js`が採用されている。
デフォの24種だと使わないやつもいるので削って、使いそうな言語を足したものを[highlight.js](https://highlightjs.org/download/)で作ってダウンロードする。
これは手動でダウンロードして、デフォの`highlight.pack.js`に上書きした。

スタイルは`base.html`で`default`が指定されているので`solarized_dark`を選ぶ。ついでにhighlight.jsのversionも9.7から9.13.1へ上げちゃう。

404 Page not foundのときのページがどうやって表示されるのかわからなかったが、GitHub Pagesの機能で提供されるルールに従えば良い様子。

[Customizing GitHub Pages / Creating a custom 404 page for your GitHub Pages site](https://help.github.com/articles/creating-a-custom-404-page-for-your-github-pages-site/)

デフォの404ページのスタイルが他のページとぜんぜん違うので、スタイルに合わせておいた。

ページが縦に長くなって垂直スクロールバーが出るとコンテンツのズレが生じるのだけど、これ多分bootstrapに起因する問題か🤔悩ましいが一旦そのままに。

これでおおよそのデザイン面は完成した。

作業中のコードは一旦BitBucketのprivate repoにブチ込んでおいたのだけど、GitHubの無償アカウントでもprivate repoが使えるようになって分ける理由がなくなってしまった...まあよし。

あと、

- Bootstrap3から4に上げたい
- Google Analytics

など残しているが、一旦はコンテンツの公開を優先して後回しにする予定。

#### GitHub Pagesにうｐる

[GitHub Pages](https://pages.github.com/)の説明と[Cryogen - GitHub Pages](http://cryogenweb.org/docs/deploying-to-github-pages.html)を見たらできる。

作るのはユーザーのpageなので、まず`krymtkts.github.io`のrepoを作る。
中身は空で。Licenseの選択もなし。

Cryogenのドキュメントに従い、`config.edn`の`blog-prefix`キーの値は空にする。

あとはCryogenが出力した`resource/public`を先程作ったrepoのmasterブランチにpushするのみ。

この出力先`resource/public`を変更する方法がわからなかったので、`krymtkts.github.io`という名前のシンボリックリンクを作って、あたかもそういう名前のフォルダをGitで管理してる感を醸し出して茶を濁した。

```powershell
New-Item -Value '.\blog\resources\public\' -Path './' -Name 'krymtkts.github.io' -ItemType SymbolicLink
cd krymtkts.github.io
git init
echo "# krymtkts.github.io" >> README.md
git add README.md
git commit -m "First commit"
git add .
git commit -m 'Add contents'
git remote add origin git@github.com:krymtkts/krymtkts.github.io.git
git push -u origin master
```

これでわたしのブログがpublishされたのであった🎉

めでたしめでたし...

### To be continued

で終わらなかった。

これを作りきって、急激にCryogenに興味がなくなってしまった。

結局の所、テンプレートエンジンにSelmerを使ってることで、HTMLやスタイルの編集自体にClojure感の薄さがあってなんか楽しみがないのかなと。最初から予想できそうな結果やけどな🤔

コンテンツ自体はMarkdownで書くし可搬性があるので記事の更新はしつつ、次段階としてPerunで作り直してみようと思う。

破壊と創造こそが人類の本質ですね(違う
