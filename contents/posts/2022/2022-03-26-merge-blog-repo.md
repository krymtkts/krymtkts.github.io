---
title: "Blog 用 Git repositories のマージ"
tags: ["git", "github"]
---

[Cryogen の更新](/posts/2022-03-05-customize-cryogen.html) で 2 つに別れていた repo を統合できるようになったので、した。

[krymtkts/krymtkts.github.io](https://github.com/krymtkts/krymtkts.github.io) に [krymtkts/blog-cryogen](https://github.com/krymtkts/blog-cryogen) の歴史をすべて引き込む。

元は 1 つのコンテンツを 2 つに分けてるだけなので、統合は簡単だった。

```powershell
git switch --create feature/merge-repo

# Move existing blog contents.
mkdir docs
ls -Exclude docs | %{mv $_.Name ./docs }
git add .
git commit -m "Move blog files to 'docs'."
# Merge generator repo.
git remote add upstream ssh://git@krymtkts.github.com:krymtkts/blog-cryogen.git
git fetch upstream
git merge --allow-unrelated-histories upstream/master
# miscellaneous work.
```

次に GitHub Pages の Source を `/docs` に変えて表示確認する。
PR 作成後、GitHub Pages の対象 branch をマージ用の branch に向けて、 GitHub Actions による deploy を確認した。

1. GitHub Pages の Source を `master` の `/(root)`-> `feature/merge-repo` の `/docs` に変えて deploy 確認
2. PR をマージ＆ branch を残す
3. GitHub Pages の Source を `feature/merge-repo` の `/docs` -> `master` の `/docs` に変えて deploy 確認
4. `feature/merge-repo` を消す

branch を消したときに自動で Source を追随してくれんのかも知れんが、 壊れたら面倒なので 1 手順ずつ確認した。

スッキリした。
