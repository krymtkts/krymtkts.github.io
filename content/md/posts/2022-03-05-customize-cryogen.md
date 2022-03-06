{:title "Cryogen のカスタマイズ"
:layout :post
:tags ["cryogen","clojure"]}

[前回の投稿](/posts/2022-02-27-issue-with-cryogen-version-bump)で、 [Cryogen](https://cryogenweb.org/) のバージョンアップに伴い Cryogen 自体のカスタマイズが必要だとを書いた。

カスタマイズ自体はかなり簡単にできたのだが、 Clojure 経験不足だからか正直なところドキュメントの記載だけではピンと来なかった。
ということで、またわからなくなること必至のため記しておく。

`cryogen-core` のバージョンは `0.4.1` だ。

対象のドキュメント [Cryogen: Customizing/Extending Cryogen](https://cryogenweb.org/docs/customizing-cryogen.html#customizing-the-code) を以下に全文引用する。

> You can copy the `cryogen-core.compiler` namespace directly into your project (where it will override the one from the cryogen-core.jar) and modify it to your liking. It is not very long or complicated and is quite easy to modify. That is [what we did for this site](https://github.com/cryogen-project/cryogen-docs/blob/fd601c857cc88f7cb633a41c47b4c692e1522ed8/src/cryogen/compiler.clj) (although it uses a much older version of cryogen-core, you may still use the same strategy today).

機械翻訳にリンクを添えた ↓

> `cryogen-core.compiler` 名前空間をプロジェクトに直接コピーし（cryogen-core.jar のものを上書きします）、好みに応じて変更することができます。これはそれほど長くもなく、複雑でもなく、非常に簡単に修正することができます。このサイトでは、[このような方法](https://github.com/cryogen-project/cryogen-docs/blob/fd601c857cc88f7cb633a41c47b4c692e1522ed8/src/cryogen/compiler.clj)をとっています（かなり古いバージョンの cryogen-core を使用していますが、現在でも同じ方法をとることができます）。

早い話が「Cryogen の公式ページのコードを真似ろ」。

はじめはナンノコッチャと思ったのだけど、Cryogen の repo のコードを眺めて「[`cryogen_core/compiler.clj`](https://github.com/cryogen-project/cryogen-core/blob/31bcbfdad59e8eaed4a6d417682e51ef1e90982c/src/cryogen_core/compiler.clj)をコピって自分でサイトジェネレータを書いたらええんやで？」だと理解した。

Cryogen のエントリポイントは `cryogen/core.clj` と `cryogen/server.clj` があるが、いずれもサイトジェネレータは `cryogen-core.compiler` 名前空間の `compile-assets-timed` を呼び出してるだけなので、これを `cryogen_core/compiler.clj` からコピった自分用ジェネレータに変える、という趣旨らしい。

とった手順は以下の通り。

1. [`cryogen_core/compiler.clj`](https://github.com/cryogen-project/cryogen-core/blob/31bcbfdad59e8eaed4a6d417682e51ef1e90982c/src/cryogen_core/compiler.clj) をコピって自分のサイトのコードに `src/cryogen/compiler.clj` として配置する
2. `src/cryogen/compiler.clj` を自分の必要な形に書き換える
   - 今回デフォルトの Cryogen から変えたかったのは、 RSS フィードの要約機能を取り除いて HTML 全文載せるようにすることだった
   - [`add-description`](https://github.com/cryogen-project/cryogen-core/blob/31bcbfdad59e8eaed4a6d417682e51ef1e90982c/src/cryogen_core/compiler.clj#L474-L487) 関数の中で利用されている `util/enlive->plain-text` を `util/enlive->html-text` に変更、`add-description` に依存する関数 `compile-assets`, `compile-assets-timed` を `cryogen.compiler` に定義した
   - それ以外の関数は `cryogen-core.compiler` を参照する
3. `cryogen/core.clj` と `cryogen/server.clj` で、 `compile-assets-timed` の名前空間を `cryogen-core.compiler` → `cryogen.compiler` に変える
4. `lein serve` してエラーがない＆期待の出力になっていれば完了

これで概ね自分が期待する出力を得られるようになった(はず)。
気付かないところで破壊的な変更をしているかも知れないので、それは経過観測していく。この投稿をした後で RSS フィードが本当に届くか心配だ。

あと既知の問題として、新たに追加された `lein serve:fast` がちゃんと動いてんのかこれ？ であったり、 Markdown 保存時の再生成が怪しかったりする。
これらは、ちまちま直していきたい。

これで repo の統合だったり GitHub Action 化が見えてきた。
