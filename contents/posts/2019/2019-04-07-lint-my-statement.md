---
title: "下手な文章を Lint する"
tags: ["vscode", "nodejs", "textlin"]
---

転職してから 2 ヶ月の間、海外労働者の同僚から、日本語のドキュメントも英語のドキュメントもレビューコメントをいただいていてつらい。

英語だけならまだしろ、わしゃ日本語もろくに扱えん日本人なんかと思うと涙ちょちょぎれる次第であります。

なので自分の文章に Lint をかけようと思った。

### textlint

いつだったか文章の Linter についてググってたら、以下のようなものを見つけた。

- [GitHub - textlint/textlint](https://github.com/textlint/textlint)
- [Qiita - VS Code で textlint を使って文章をチェックする](https://qiita.com/azu/items/2c565a38df5ed4c9f4e1)
- [textlint-ja/textlint-rule-preset-ja-technical-writing: 技術文書向けの textlint ルールプリセット](https://github.com/textlint-ja/textlint-rule-preset-ja-technical-writing)

...すげえな！近い内に使えるようにしよう。と思ってから数ヶ月後、実務でまともな文章を書く能力が必要になるとわ...😭
というか vscode-textlint ってホイル焼きで有名な方の作品なのですね。敬意を払うべきプロダクトだ。

以下にわたしのセットアップ手順を記す(実行は PowerShell だよ)。

1. VSCode で vscode-textlin を入れる
2. `npm i -g textlint textlint-rule-preset-ja-technical-writing`
3. `cd ~`で`textlint --init`してルールを書き込み
4. VSCode では設定ファイルを絶対パスで利用するからどうしたものか...(複数端末で Sync してる関係で)
   1. `~/,textlint`にしたらユーザ名とか考えなくていいから、なんかいい 😁
5. ついかパッケージ `npm i -g textlint-rule-date-weekday-mismatch textlint-rule-terminology textlint-rule-ginger`

これで簡単な typo は減らせるんじゃないかな。

お気に入りのルールは`textlint-rule-date-weekday-mismatch`。これからのオレが曜日を間違うことはないぜええええ？(フラグ
