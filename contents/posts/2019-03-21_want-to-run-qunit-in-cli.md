{:title "QUnit で CI したい その 1"
:layout :post
:tags ["nodejs", "qunit", "puppeteer"]}

2 月に転職した。

へーしゃでの初めての仕事はフロントエンド、と言ってもトラッキングに関わる部分である。

生の ES5 で書かれてて[Google Closure Compiler](https://github.com/google/closure-compiler)で minify されてるようなのだけど、困ったことにユニットテストがない様子？
どこそこのチームで作ってたことがあったらしいという情報は得たので、それを CI に組み込めたらいいかなと思っていた。

蓋を開けてみると、2 年間ほどメンテされていない死んだユニットテストで、最新のコードベースに対するテストでなかった(テストコードと同ディレクトリにテスト対象のコードがコピられてて)🤔

CI に組み込まれないテストは陳腐化すると云うが、まさかそれを身をもって実感することになろうとは思わなんだ。

~~仕方ないので~~既存の資産を活かすためにも、テスティングフレームワークなど既存のものを利用して少なくとも CI に組み込めるところまでは持っていこうと考えた。CI に組み込まれればビルド失敗の通知を恐れてみんなユニットテストを書くのをサボらなくなる。

というわけで、許可を取り付けた上で仕事の合間を縫ってユニットテストの整備を行うことにした。

### 現状把握と展望

ユニットテストは[QUnit](https://qunitjs.com/)で書かれていて、モッキングフレームワークには[Sinon.JS](https://sinonjs.org/)が使われていた。

linter は[ESLint - Pluggable JavaScript linter](https://eslint.org/)を用意してあるようなのだけどどうも長らく利用されてなかったようで、試しに実行すると 41 件の autofix 可能なエラーが...😅

ブラウザで`index.html`を開くことでテストが実行されるタイプのやつで、CLI での実行は用意されてなかった。
プロダクトコードが`window`オブジェクトに依存してることもあって、QUnit のテストをヘッドレスブラウザで実行するのが良さげかな。

ここにカバレッジ計測も追加して、最終的にはプルリクをトリガーにした自動テストが CI に組み込まれるのが良さげかなと思う。

### やったこと＆やらなかったこと

とりあえずカバレッジは置いといて今ある QUnit の CLI 実行を優先した。

node でのカバレッジ計測に関しては知らないことが多かったので軽く下調べだけした。
istabul というやつが node 界隈で強かったみたいだが、こいつは 2 年ほどメンテされてなくて、後続の[nyc - npm](https://www.npmjs.com/package/nyc)と言うやつがみつかる 👀
puppeteer もそれ単体でカバレッジが測れるようす ↓

- [Using Puppeteer to Extract Code Coverage Data from Chrome Dev Tools](https://www.philkrie.me/2018/07/04/extracting-coverage.html)
- [istanbuljs/puppeteer-to-istanbul: given coverage information output by puppeteer's API output a format consumable by Istanbul reports](https://github.com/istanbuljs/puppeteer-to-istanbul)

#### やったこと

まず元々ある QUnit もそのまま使えないとブラウザで見つつの開発の利便性も下がっちゃうので、そこは担保したかった。
(`window`オブジェクトがないので CommonJS は死ぬ)

[qunit-puppeteer - npm](https://www.npmjs.com/package/qunit-puppeteer)は簡単にテスト実行できて楽だったのだけど、テストの url を絶対パスで指定しないといけず＆自分で URL をこねくり回せなくて却下。

なのでそのへん柔軟な[node-qunit-puppeteer - npm](https://www.npmjs.com/package/node-qunit-puppeteer)しか選択肢が残らなかったわけだ。

とりま`eslint`だけあるからソイツはそのままに、QUnit を headless browser 実行するための module だけ足す。

```powershell
npm install --save-dev node-qunit-puppeteer
```

`package.json`は以下のような感じにした

```json
  "scripts": {
    "lint": "eslint --fix ./",
    "test": "node test/run.js"
  },
```

`lint`はメンテされてなかった関係で 40 件くらいの error があるけど、最終的には実行できるようになる(ハズな)ので用意しておく。

`test/run.js`は以下の通りで、[ameshkov/node-qunit-puppeteer: A simple node module for running qunit tests with headless Chromium](https://github.com/ameshkov/node-qunit-puppeteer)の Example とほぼ同じで行ける便利さ。しびれる。

```js
#! /usr/bin/env node
// almost the same as the sample code :-p
// https://github.com/ameshkov/node-qunit-puppeteer

const path = require("path");
const { runQunitPuppeteer, printOutput } = require("node-qunit-puppeteer");

const qunitArgs = {
  targetUrl: `file://${path.join(__dirname, "/index.html")}`,
  redirectConsole: true,
};

runQunitPuppeteer(qunitArgs)
  .then((result) => {
    // Print the test result to the output
    printOutput(result, console);
    if (result.stats.failed > 0) {
      // Handle the failed test run
      // currently notghing to do.
    }
  })
  .catch((ex) => {
    console.error(ex);
  });
```

実行結果は以下のような感じで出る。

```powershell
$ npm run test

> qunit@1.0.0 test C:\Users\takatoshi\dev\javascript\qunit
> node test/run.js

Module: hello module
  hello test
    Status: success
    Passed assertions: 2 of 2
    Elapsed: 1ms

Test run result: success
Total tests: 1
  Assertions: 2
  Passed assertions: 2
  Failed assertions: 0
  Elapsed: 8ms
```

と、ここまで書いた内容を会社のコードに組み込んだのだけど、会社で作ったものは外に出せないので模倣したゴミプロジェクトを作った ↓

[krymtkts/qunit-trial: sandbox to enhance legacy QUnit test.](https://github.com/krymtkts/qunit-trial)

今後いじくり回すときの砂場としても使おうかな。

#### あとやりたいこと

ユニットテストだけあってカバレッジ計測がないのはちょっとアレなので早々に追加したい所存 🤔

そもそもユニットテスト書くにしてもどの経路通ったとかわからないのでテスト品質を保証しづらく、コード網羅率 Level C1 を 100%にしたいマンなのもありカバレッジ必須。

でも事前に調べてた nyc や puppeteer での方法だと、現状の node-qunit-puppeteer を使ったテストのカバレッジ計測できなさそう...

もう少し調べる必要がありそう 😳

続く
