---
title: "Fable でブログを再構築する pt.6"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築しようとしている。

GitHub Actions で GitHub Pages のデプロイするようにした。
あと個人プロジェクトの repo を deploy したときの root 間違ってたので手直しする必要があった。
[#16](https://github.com/krymtkts/blog-fable/pull/16),
[#18](https://github.com/krymtkts/blog-fable/pull/18)

GitHub Pages はこちら → [Blog Title - February](https://krymtkts.github.io/blog-fable/index.html)

GitHub Actions を使った GitHub Pages の deploy は現行ブログでもやりたかったネタで、長らく放置してた。
ブログ再構築のタイミングなら導入するのにちょうどよいので以下の記事を参考に導入した。

- [Publishing with a custom GitHub Actions workflow - Configuring a publishing source for your GitHub Pages site - GitHub Docs](https://docs.github.com/en/pages/getting-started-with-github-pages/configuring-a-publishing-source-for-your-github-pages-site#publishing-with-a-custom-github-actions-workflow)
- [カスタムワークフローで GitHub Pages デプロイが可能に | 豆蔵デベロッパーサイト](https://developer.mamezou-tech.com/blogs/2022/09/08/github-pages-new-deploy-method/)

注意点は先述の通り、個人プロジェクトの場合は `{account}.github.io/{repo-name}/` がページの root になる点くらい。
これは自動的に付与されるので、その前提でリンクやら調整する必要がある。
ただし開発サーバでは自動で付与されないので、 root は `docs` にしつつ他は repo name のディレクトリを介すようにする。
今回のケースでは以下を調整していい感じにした。

- 諸々のリンクに repo name のディレクトリを入れる
- 開発用サーバの index や 404 を repo name のディレクトリを介すよう調整する
- `actions/upload-pages-artifact` の `path` は repo name のディレクトリにする ← 重要

いまの workflow と開発サーバの設定を転記しておく。

[blog-fable/gh-pages.yml at 4cb5d81ac8099889dd55e50ae39ebb20935e96f5 · krymtkts/blog-fable · GitHub](https://github.com/krymtkts/blog-fable/blob/4cb5d81ac8099889dd55e50ae39ebb20935e96f5/.github/workflows/gh-pages.yml)

```yaml
name: Deploy GitHub Pages

on:
  push:
    branches: ["main"]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: true

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v3
      - name: Setup Pages
        uses: actions/configure-pages@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 7.0.x
      - uses: actions/setup-node@v3
        with:
          node-version: 18
      - name: Install dependencies
        run: npm install
      - name: Build pages
        run: npm run build
      - name: Upload artifact
        uses: actions/upload-pages-artifact@v1
        with:
          path: docs/blog-fable/ # ここ重要。 docs/ ではない

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v1
```

開発サーバは今暫定的に [live-server](https://github.com/tapio/live-server) を使っているが、その場合以下のように index と 404 を指定して起動する。

```sh
live-server docs --open=blog-fable/index.html --entry-file=blog-fable/404.html
```

長々と書いたが、個人ページを作るなら `{account}.github.io/` の repo を使うだろうから、そんなに心配することはない。

今回の GitHub Pages の対応でベタ書きだったパス類をエントリポイントへ全部まとめるようにしたので、幾分設定しやすくなった。
これでサイトマップとフィードの XML 、開発サーバの置き換えあたりに着手できる。一段落までもうちょいかな。
