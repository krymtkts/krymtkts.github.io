---
title: "Fable でブログを再構築する pt.17"
tags: ["fsharp", "fable"]
---

[Fable](https://fable.io/) で作ったブログに乗り換えた。
このブログは既に Powered by [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) だ。

[前回](/posts/2023-10-08-rebuild-blog-with-fable-pt16.html)検証したときの手順ベースでうまくできた。

```powershell
# krymtkts/krymtkts.github.io の root directory にて
git switch --create feature/blog-fable

# 現行ブログのリソースを削除する
ll -Exclude content,contents | rm -Recurse

git add -u
git commit -m "Remove old resources."

# blog-fable repo を merge する
git remote add blog-fable ssh://git@github.com/krymtkts/blog-fable.git
git fetch blog-fable
git merge --allow-unrelated-histories blog-fable/main

# Sample を消す
rm contents/* -Recurse
git add -u
git commit -m "Remove sample posts and pages."

'posts','pages' | %{mv "./content/md/$_" "./contents/$_"}
'img' | %{mv "./content/$_" "./contents/$_"}

# 旧コンテンツを消す
rm content -Recurse

git commit -m "Move contents."

# エラーしないように Markdown を書き換える
# 自動でできる分
. .\scripts\convert-contents.ps1
# あと少しの手動書き換え
# App.fs の設定を書き換える

# 実行して動作確認する
npm ci
npm run dev
```

自動で書き換え可能と判断した以下は PowerShell でスクリプト化(`scripts\convert-contents.ps1`)した。
今思えば F# スクリプトで書いても良かったなという感じもするけど。

- posts の front matter を edn から YAML に変換
- 内部リンクに `.html` を付与
- コードブロックの言語 typo とか非対応言語の置換

少し手で書き換えたのは以下。対処法ないものやめんどいもので止む無く。

- 言語なしのコードブロック(` ``` `)
  - 閉じる方特別つかず、手動で ` ```plaintext ` に変更した
- edn で書かれた front matter の tags リストにカンマがないやつ
  - スクリプトでできる気がしたが数少なかったので
- pages の書き換え
  - 1 ページしかなかったので
- 相対リンクで書かれてたかしょ
  - 1 箇所しかなかったので...

因みにこの手順だけでは終われなくて、 GitHub の repository の設定をイジる必要があった。
具体的には、 branch から deploy される設定を GitHub Actions で deploy するように変更した。

- Settings -> Pages -> Build and deployment の Source を GitHub Actions に変える

これを既存の Deploy from a branch から変えるのは公開中ページに影響ないか度胸がいったが、サクッと変えてもサイトが非公開化されたりしなかったので、安心してよい。

ちゃんと準備してきたこともあってすんなり終わった。

Markdown parser だったり highlighter は自分で書いてないけど、結構な部分自分で作った感じするので、愛着が湧いて良い。
けっこう速くできてるけど、まだ高速化できる箇所もあったりなんかメンテの楽しみが増えて良いな。
期間にして半年くらいの週末と休日の数時間を使って作ってきて、時間かかり過ぎな気もするが現時点で満足の行くものになった気がする。
