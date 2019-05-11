{:title "Pipenvのテンプレを作った"
 :layout :post
 :tags  ["python"]}

[前回](./2019-04-29-bias-amp-2issue.md)の最後に触れた、仕事で経験を得た2019年のイケてそうなPython開発環境構築について。

この度新し目と思われるPythonのプロジェクト構築について学んだので、次回Pythonを触ることがあったとしてもぱぱっと始められるようにテンプレートを作ろうと思った次第である。

過去のeasy_installとかpipまでで知識が止まってたが、新たにpipenvを知ってかなり便利に使えるいい感じの印象を得たのもあって、動機付けされた感じ。

なので、勉強がてらGW中のお楽しみにちょろっと作った。

### 作ったもの

[krymtkts/pipenv-skeleton](https://github.com/krymtkts/pipenv-skeleton)

いろいろとググって、pipとかvirtualenvの時代は過ぎ去っており、いまはpienvがイケていると直感を得た。
なのでpipenvについて以下の記事などを参考にこのテンプレを作った。

- [Pipenv: 人間のためのPython開発ワークフロー — pipenv 2018.11.27.dev0 ドキュメント](https://pipenv-ja.readthedocs.io/ja/translate-ja/)
- [Python環境構築ベストプラクティス2019 - ばいおいんふぉっぽいの！](https://www.natsukium.com/blog/2019-02-18/python/)
- [Windows + Python 3.6 + PipEnv + Visual Studio Code でPython開発環境 - Qiita](https://qiita.com/youkidkk/items/b6a6e39ee3a109001c75)
- [既存プロジェクトに pipenv を導入した方法 - Qiita](https://qiita.com/tonluqclml/items/b09f4a5ed04ebcbd0af1)

予めLinterやらFormatterやらをdevPackageとして用意している。LinterにはPylintを用意したが、エラーの自動修正がないようなのでautopep8も用意した。

あと静的型付けに守られた世界で仕事できるように、MyPyも用意している。Better Bashとして使うときは大げさかもしれないけど、その時は単にMyPyを使わなかったらいいだけ。

このプロジェクトをコピペして、moduleのところをこねこねして使うイメージ。

Pythonのモジュールシステムについてはまだ理解が浅い。以下を参考にドキュメントをあたって、`__init.py__`,`__main.py__`のらへんを定型化した感じ。

- [python - What is __init__.py for? - Stack Overflow](https://stackoverflow.com/questions/448271/what-is-init-py-for)
- [python - What is __main__.py? - Stack Overflow](https://stackoverflow.com/questions/4042905/what-is-main-py)

ただ、`__init.py__`は名前空間パッケージではなくていいようだし、よりモダンな方法に寄せたいので再検討するかな。単に今はわたしの知識がそこまで及んでない😭Pythonの言語仕様もちゃんと勉強したいのう。

いろいろ足りない点があるが、それらは今後改善できれば良いかな。

- moduleの名前をいろいろ変えないといけないのが面倒なので、なんか改善ができれば良。
- テストがないので足したい。
- なんかバッジ足したい。ビルドとか...
- mypyで

### まとめ

Python3たのしい。

せっかくテンプレを作ったので、なんかゴミスクリプトでも良いのでちまちま書いていきたい所存🤔

mypyについてもメモためていこ。