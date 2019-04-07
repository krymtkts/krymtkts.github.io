{:title "下手な文章をLintする"
 :layout :post
 :tags  ["vscode", "node", "textlin"]}

転職してから2ヶ月の間、海外労働者の同僚から、日本語のドキュメントも英語のドキュメントもレビューコメントをいただいていてつらい。

英語だけならまだしろ、わしゃ日本語もろくに扱えん日本人なんかと思うと涙ちょちょぎれる次第であります。

なので自分の文章にLintをかけようと思った。

### textlint

いつだったか文章のLinterについてググってたら、以下のようなものを見つけた。

- [GitHub - textlint/textlint](https://github.com/textlint/textlint)
- [Qiita - VS Codeでtextlintを使って文章をチェックする](https://qiita.com/azu/items/2c565a38df5ed4c9f4e1)
- [textlint-ja/textlint-rule-preset-ja-technical-writing: 技術文書向けのtextlintルールプリセット](https://github.com/textlint-ja/textlint-rule-preset-ja-technical-writing)

...すげえな！近い内に使えるようにしよう。と思ってから数ヶ月後、実務でまともな文章を書く能力が必要になるとわ...😭
というかvscode-textlintってホイル焼きで有名な方の作品なのですね。敬意を払うべきプロダクトだ。

以下にわたしのセットアップ手順を記す(実行はPowerShellだよ)。

1. VSCodeでvscode-textlinを入れる
2. `npm i -g textlint textlint-rule-preset-ja-technical-writing`
3. `cd ~`で`textlint --init`してルールを書き込み
4. VSCodeでは設定ファイルを絶対パスで利用するからどうしたものか...(複数端末でSyncしてる関係で)
   1. `~/,textlint`にしたらユーザ名とか考えなくていいから、なんかいい😁
5. ついかパッケージ `npm i -g textlint-rule-date-weekday-mismatch textlint-rule-terminology textlint-rule-ginger`

これで簡単なtypoは減らせるんじゃないかな。

お気に入りのルールは`textlint-rule-date-weekday-mismatch`。これからのオレが曜日を間違うことはないぜええええ？(フラグ
