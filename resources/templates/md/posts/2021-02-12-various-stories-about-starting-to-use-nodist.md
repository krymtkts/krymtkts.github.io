{:title "色々あって Nodist を使い始めた話"
:layout :post
:tags ["node"]}

重い腰を上げて、[Slackbot のマッコールさん](https://github.com/krymtkts/mccall-bot)の Typo を直す気になった。
タイポの修正と追加のセンテンスを登録して、いざ deploy しようとしたら、Serverless Framework がエラーを吐くようになっていた。↓ らしい。

[Unable to deploy to Serverless due to 'empty zip' · Issue #8794 · serverless/serverless](https://github.com/serverless/serverless/issues/8794)

aws-cdk の方も同様のバグがあるらしいけど、あっちは直してくれてるみたい。

[lambda: corrupt zip archive asset produced when using node v15.6 · Issue #12536 · aws/aws-cdk](https://github.com/aws/aws-cdk/issues/12536)

残念ながら Serverless Framework の方は直してないっぽなので、Node.js のバージョンを 15 から 14 に落とす必要が出てきた。この時リアルタイムで友人に [nodist](https://github.com/nullivex/nodist) を教えてもらったので、これを使って複数の系を利用できるようにした。

~~(というか Chocolatey で Node.js の v15 を uninstall して v14 入れようとしてもなんかエラーになって、ログ見たら「新しい版いるから失敗するね！」てあって「はぁ!?」と調べたら`choco uninstall nodejs`は仕事してくれへんらしい。古き良き Chocolatey 流儀はやめろ。で日頃使ってた node modules も全部吹っ飛んで災難やで...)~~

```powershell
choco install nodist -y
# RapidEE で PATH に `C:\Program Files (x86)\Nodist\bin` を追加した

# nodist と一緒に install される系は古い
nodist list
  (x64)
> 11.13.0
# 15 系と 14 系の最新を入れる
nodist add 15.8.0
nodist add 14.15.5
# 14 系を選択
nodist 14.15.5
# npm も古い
nodist npm list
> 6.9.0
# 選択中の node と合わせる
nodist npm match
```

[Node.js のリリース一覧](https://nodejs.org/ja/download/releases/)から node と npm の対応を見て選んでってやってもいいけど、そんなんめんどすぎるので `nodist npm match` の一度ではないかと考える。
もし、厳密に指定のバージョン使いたいとかあったら加初環境をコンテナに構築したりすればいいし、そもそも nodist を使ってローカル PC のグローバル node をこねくったりしてないはず。

ちょっとおもしろいのが、node/npm の version を変えても install した node_modules は同じものを使えるところ。 v14.15.5 で入れたモジュールが v15.8.0 でも見れた。何度も同じモジュールをインストールしなくていいのは楽やけど、バージョン互換性の厳しいモジュールは使うのが難しいのでは。

node、npm、モジュールの実態はそれぞれ、`$env:NODIST_PREFIX/v-x64`(32bit が`$env:NODIST_PREFIX/v`?) `$env:NODIST_PREFIX/npmv` `$env:NODIST_PREFIX/bin/node_modules` 配下にインストールされる様子。

nodist、どうも 2019 年を最後にメンテが止まっている様子(単にマジで変更がないのかも知れん)。現時点でこいつが最後 [Fix deprecated use of Tar.Extract in npm.js, release 0.9.1 · nullivex/nodist@bb099ba](https://github.com/nullivex/nodist/commit/bb099ba3723027469bf46e3159f51171b5dd4b59)

とはいえ便利なので不都合ない限り利用してみるつもり。

と色々やったことでようやくマッコールさん Bot の最新版を deploy できるようになったとさ。めでたし x2。
