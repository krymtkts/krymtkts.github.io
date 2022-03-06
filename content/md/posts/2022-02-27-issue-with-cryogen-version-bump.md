{:title "Cryogen バージョンアップに伴う困りごと"
:layout :post
:tags ["cryogen","clojure"]}

このブログを作るのに [Cryogen](https://github.com/cryogen-project/cryogen) を利用している。
作った当初から一切のバージョンアップをしていなかったのだが、先週からちまちま手を動かし始めた。

最近の Cryogen では出力先のディレクトリを指定できる様になっているので、 `docs` 出力するようにすれば今 [krymtkts/blog-cryogen](https://github.com/krymtkts/blog-cryogen) と [krymtkts/krymtkts.github.io](https://github.com/krymtkts/krymtkts.github.io) の 2 つに別れている repo を統合できる。
そも、出力結果を repo の管理下に置かず Github Actions で済ます選択もできるようになるんじゃないかな。

新しい Cryogen では雛形のディレクトリ構造が変わっているのだが、このバージョンアップによるマイグレーション自体は大したことはない。雑に言えば以下のタスクがあるだけだ。

1. 新しい Cryogen で雛形を作成する
   - 雛形に含まれるポストや利用しないテーマ等を取り除いておく
2. 既存のコンテンツとテーマを 1 に移動する
   - `resources\templates\themes` -> `themes`
   - `resources\templates\img` -> `content\img`
   - `resources\templates\md` -> `content\md`
3. `config.edn` 新しいパラメータに書き換える
4. `lein serve` で出力して確認

一通り手を動かしてみて、tag へのリンクが壊れたりの細かいバグはあったが、概ね破壊的な変更なく OK やなというところまではできた。

ところが困ったことに、RSS Feed の出力だけは大きく変わってしまうのを避けられなかった。どうも新しい Cryogen では RSS Feed への出力は要約だけにする仕様に変わったらしい。
ワークアラウンドとして、`config.edn` の `blocks-per-preview` の数値を大きくすれば要約に全文を含めることはできる。でもそれまで可能だった HTML での埋め込みはできなくなってしまった。
[RSS feed: only publishes article's "summary" · Issue #241 · cryogen-project/cryogen](https://github.com/cryogen-project/cryogen/issues/241)

コード的にこの辺。 ページの `description` が Feed に出力されるのだが、 plain text 以外の選択肢がない。なんでや。

[cryogen-core/compiler.clj at 31bcbfdad59e8eaed4a6d417682e51ef1e90982c · cryogen-project/cryogen-core](https://github.com/cryogen-project/cryogen-core/blob/31bcbfdad59e8eaed4a6d417682e51ef1e90982c/src/cryogen_core/compiler.clj#L474-L487)

```clojure
(defn add-description
  "Add plain text `:description` to the page/post for use in meta description etc."
  [{:keys [blocks-per-preview description-include-elements]
    :or   {description-include-elements #{:p :h1 :h2 :h3 :h4 :h5 :h6}}}
   page]
  (update
    page :description
    #(cond
       (false? %) nil  ;; if set via page meta to false, do not set
       % %    ;; if set via page meta, use it
       :else (->> (enlive/select
                    (preview-dom blocks-per-preview (:content-dom page))
                    [(set description-include-elements)])
                  (util/enlive->plain-text)))))
```

[cryogen-core/util.clj at 31bcbfdad59e8eaed4a6d417682e51ef1e90982c · cryogen-project/cryogen-core](https://github.com/cryogen-project/cryogen-core/blob/31bcbfdad59e8eaed4a6d417682e51ef1e90982c/src/cryogen_core/util.clj#L38-L41)

```clojure
(defn enlive->plain-text [node-or-nodes]
  (->> node-or-nodes
       (enlive/texts)
       (apply str)))
```

RSS リーダー利用者としての個人的な意見だが、正直なところ技術ブログなんかであれば全文マークアップ可能な状態で配信してほしい。割と RSS リーダーだけ読んで済ませることも多い。
最近はフィードに全文載っけないのが主流ぽくはあるが、これは多分広告表示とかアクセス解析のためにサイトを訪れてほしいからであって、そういう動機がないのであれば全文配信しない理由がない。

なのでわたしのブログもそのようにしていたのだけど、このバージョンアップでそれができなくなるのは個人的にちょっと受け入れられないと判断した。
RSS リーダーでこのブログを購読する最有力ユーザはわたし自身なので、自分の意見が一番えらい。

現状だとどうしようないのだが、 Cryogen 自体に手を入れることができるのでそれをやってみようとしている。

[Cryogen: Customizing/Extending Cryogen - Customizing the code](https://cryogenweb.org/docs/customizing-cryogen.html#customizing-the-code)

続く。
