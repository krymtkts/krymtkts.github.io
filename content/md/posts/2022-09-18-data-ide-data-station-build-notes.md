{:title "データ IDE DataStation のビルド関連メモ"
:layout :post
:tags ["electron","go"]}

ちょっと前に [DataStation](https://datastation.multiprocess.io/) という OSS の IDE を知った。
GitHub の repo はこちら。[multiprocessio/datastation: App to easily query, script, and visualize data from every database, file, and API.](https://github.com/multiprocessio/datastation)

データベース・ファイル・HTTP リクエストまで多様なデータソースへの接続をサポートしてたり、簡単にグラフ化、コードで加工したり、とにかく良い。 Notebook みたいといえばそんな感じ。

ちょっとコントリしたいネタがあり、その動作確認のためにビルドして実行する必要があった。その時の手順・つまづきポイントを備忘のために雑メモで記しておく。

---

DataStation のアーキテクチャについてはこちらのメモにある。そんなに詳しくないので自力で色々読んだり試したりした方がイイか。

[datastation/ARCHITECTURE.md at main · multiprocessio/datastation](https://github.com/multiprocessio/datastation/blob/66b77f0cfe41040c49fe1e42e6f267b59f45b5bf/ARCHITECTURE.md)

### デスクトップアプリのビルドと実行

[datastation/HACKING.md at main · multiprocessio/datastation](https://github.com/multiprocessio/datastation/blob/66b77f0cfe41040c49fe1e42e6f267b59f45b5bf/HACKING.md)

手順はこれを参照した。でも試してる途中でよくわからなくなってくるので、[`package.json`](https://github.com/multiprocessio/datastation/blob/66b77f0cfe41040c49fe1e42e6f267b59f45b5bf/package.json) を読んで各スクリプトが何者かは読んだ方がイイかも(実際そうした)。

ビルドと実行には WSL2 を使う。 手順に記載のある通り Windows でもできるっぽい([tdm-gcc](https://jmeubank.github.io/tdm-gcc/)というのを使う)けど、なんか面倒な気配がしたので Linux で楽をする。いつかチャレンジしてもよいが今ではないと判断した。
GCC は Go の SQLite3 モジュールをビルドするので必要っぽい。

ビルドの事前準備には依存関係のインストールスクリプトが用意されてる(CI 用だけど)。それを使って楽をする。
[datastation/prepare_linux.sh](https://github.com/multiprocessio/datastation/blob/66b77f0cfe41040c49fe1e42e6f267b59f45b5bf/scripts/ci/prepare_linux.sh)

DataStation のデスクトップアプリは Electron アプリの様子。
以下のビルド実行時に権限が必要だったのと、それによって `--no-sandbox` が必要になった。

```sh
sudo yarn build-desktop --no-sandbox
```

このビルド実行でライブラリが不足しているのがわかり、以下を参考にパッケージをインストールした。なんのエラーが出たかはメモを失念したが、 lib\* が足りない系。
足りない依存関係は皆 Electron のビルドに必要なものばかりだった。

- [Puppeteer でライブラリ不足 libraries: libatk-1.0.so.0 - ノンカフェインであなたにやさしい](https://akinov.hatenablog.com/entry/2021/04/04/151851)
- [Missing shared libraries · Issue #486 · electron/electron-quick-start](https://github.com/electron/electron-quick-start/issues/486#issuecomment-1153535808)

この依存関係のインストール後にビルドが成功するようになったが、実行すると SQLite3 のエラーになった。
内容は SQLite3 が古いと言われるものだった(メモ失念)。エラーログを見る限りこれの解消には Electron の再ビルドが必要なようだった(これあとから見てもピンとくるのかわからん。エラーログをメモらなかったことが悔やまれる)。

↓ の記事を参考にした覚えあり。
[npm - Node - was compiled against a different Node.js version using NODE_MODULE_VERSION 51 - Stack Overflow](https://stackoverflow.com/questions/46384591/node-was-compiled-against-a-different-node-js-version-using-node-module-versio)

```sh
./node_modules/.bin/electron-rebuild
```

ここまでやって初めて ↓ のコマンドで Electron アプリを動かして動作確認できるようになった(理解のために結局 `package.json` に書かれたスクリプトを直で叩くようになる)。

```sh
yarn electron --trace-warning --unhandled-rejection=warn build/desktop.js --no-sandbox
```

### runner の UT

DataStation のデスクトップアプリはいわばフロントエンドで、データソースとの接続やデータ読み取りは Go で書かれた [datastation/runner](https://github.com/multiprocessio/datastation/tree/main/runner) で行われてるようだった。

なので UT の実行に関しては先述のディレクトリで `go test` するだけで OK だった。とはいえ前の節で先述した通り、 GCC に依存したモジュールのビルドがあるので WSL2 でやるのが良かろう。

終。
