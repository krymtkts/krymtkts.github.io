{:title "CementというCLIフレームワーク"
 :layout :post
 :tags  ["python", "cement"]}

仕事で、なんかDBに通信しながらAWSのAPIをぶん殴ってゴニョゴニョするバックエンド処理を書く必要があって、Pythonで書くことになった。

[Python ヒッチハイク・ガイド — The Hitchhiker's Guide to Python](https://python-guideja.readthedocs.io/ja/latest/index.html)を参考にCLIのフレームワークを見ていって、サンプル的に作ってみた感じや継続的にメンテされてることとかで[Cement Framework](https://builtoncement.com/)を採用することにした。

簡潔に説明すると、CLIアプリケーション作成を容易にするためのフレームワーク。なんとAWS Elastic Beanstalkに使われてるらしい。

数少ない日本語の資料。

- [自分が必要とする最低限の Cement の情報 - Qiita](https://qiita.com/ma2saka/items/9aacc46e20b9886ec156)

ほんとに少ないのでなんか自分用にメモを取ろうとこの記事をしたためた次第である。

ボイラープレートでCLIのベースはできてしまうので、あとはその中身を書いていくだけ。ちょっとAPIドキュメントがわかりにくい気もするが、YAMLの設定ファイル読み込みやロギングなども拡張機能としてある。今の所はとてもよくできた使いやすいFWだと思っている。こんどコードの生成らへんの簡単な手順をまとめてみたい。

すでに途中までCementをベースに開発しているのだけど、少し困った点がある。

### 困った点

[Cement not compatible with pipenv · Issue #549 · datafolklabs/cement](https://github.com/datafolklabs/cement/issues/549)

バグでpipenvは`setup.py`をインストール出来ないのでエラーで死ぬのだ😭わたしが考えた対策としてはイカのトーリである。

- 開発環境
    - `pipenv install -r requirement.txt`などを使いつつ開発
    - 実行時には作業ディレクトリでスクリプト実行する感じ`python -m myapp.main`
- 製品環境
    - `pip`を使ったインストール及び`setup.py`でモジュールをインストール
    - モジュール実行を行うのでどこでもok(なはず)

まだ開発途上なので、製品環境の想定がそのままうまくいくかはビミョーなところ🤔また試行錯誤しなくては。

最近は[Poetry(https://cocoatomo.github.io/poetry-ja/index.html)のほうがイケてると聞くし、はじめからPipenvを使わないようにしてたら良かった感はしなくはない😭
