---
title: "QUnit で CI したい その 2"
tags: ["nodejs", "qunit", "puppeteer", "nyc"]
---

[前回](./2019-03-21_want-to-run-qunit-in-cli.md)、QUnit のユニットテストを puppeteer で CLI 実行できるようにした

今回は code coverage を計測できるようにする。

### 前回のおさらい

puppeteer で JavaScript のカバレッジを計測することができる([What's New In DevTools (Chrome 59)  |  Web  |  Google Developers](https://developers.google.com/web/updates/2017/04/devtools-release-notes))のだが、現状利用している[node-qunit-puppeteer](https://www.npmjs.com/package/node-qunit-puppeteer)だと、puppeteer の部分をモジュール外部から触れないので利用できなくて困った、ということろまで書いた。

理想ではユニットテストの実施と同時にカバレッジを計測したいところなのだが、一旦は簡単のために別々に、つまりユニットテストの実行後さらにカバレッジ取得のためにユニットテスを再実行する、というかたちで楽しようと考えた。ユニットテストの実行が軽いうちは 2 度実行したところで大した負荷にならないというのもあり。

### やったこと

これ ↓

[istanbuljs/puppeteer-to-istanbul: given coverage information output by puppeteer's API output a format consumable by Istanbul reports](https://github.com/istanbuljs/puppeteer-to-istanbul)

puppeteer で計測したカバレッジを istanbul(nyc)で利用できる形に書き出すモジュールを使う。

[すでにある node-qunit-puppeteer のテストランナー](https://github.com/krymtkts/qunit-trial/blob/master/test/run.js)と一緒には使えないので、カバレッジ計測のためのスクリプトも新たに書く(無駄)。

```js
const pti = require("puppeteer-to-istanbul");
const puppeteer = require("puppeteer");
const path = require("path");

(async () => {
  try {
    const browser = await puppeteer.launch();
    const page = await browser.newPage();

    await page.coverage.startJSCoverage();
    await page.goto(`file://${path.join(__dirname, "/index.html")}`);
    const jsCoverage = await page.coverage.stopJSCoverage();

    pti.write(jsCoverage);
    await browser.close();
  } catch (error) {
    console.error(error);
  }
})();
```

これもほぼサンプルママで使えた。読み込ませるページのパスだけ工夫が必要ってだけ。

あとはカバレッジ計測に成功したら nyc のレポート作成を実行するだけでおｋ。これを npm のタスクにする。

```sh
node test/coverage.js && nyc report --reporter=html
```

これでページを開いたときに読み込まれる JS のカバレッジを計測できた。ただしこのままだと、QUnit やユニットテスト自体のカバレッジも含まれてしまうので、仕事の CI で使う場合なんかには特定のファイルのカバレッジだけを見るか、あるいは除外設定があればいいのだけど。

### まとめ

また今回試した内容は以下の repo に反映してある

[krymtkts/qunit-trial: sandbox to enhance legacy QUnit test.](https://github.com/krymtkts/qunit-trial)

2 回ユニットテストを実行していて無駄感があるので、node-qunit-puppeteer に手を入れることも検討していいかも 🤔

続く
