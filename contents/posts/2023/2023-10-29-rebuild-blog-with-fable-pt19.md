---
title: "Fable でブログを再構築する pt.19"
tags: ["fsharp", "fable", "github", "dependabot", "actions"]
---

[Fable](https://fable.io/) で作った [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) にブログ乗り換えたけど、まだ気になるとこをいじったりしている。

依存関係のメンテを楽にするため、 Dependabot version updates を使ってみることにした。以下を参考にして [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) で `dependabot.yml` を作成した。

- [About Dependabot version updates - GitHub Docs](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/about-dependabot-version-updates)
- [Configuration options for the dependabot.yml file - GitHub Docs](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file#rebase-strategy)

使い始めて 1 週間くらいの利用期間を経て色々見えてきた感じ。

---

使ってみて早々、コレ結構設定(`dependabot.yml`)で調整してても Fable の project とは相性悪いなーと思った。
Fable の依存関係って .NET(NuGet) と npm 、 2 つの package system にまたがってるものがある。
krymtkts/blog-fable における具体例だと [`Feliz`](https://github.com/Zaid-Ajaj/Feliz) から `react`,`react-dom` に依存してるけど、これを `dependabot.yml` では複数 `package-ecosystem` をまとめた `groups` を書けないみたい。 JSON Schema の `package-ecosystem` が `string` やし。
つまり dependabot サンが .NET で `Feliz` か npm で `react*` の PR を作ってきたら、自分で femto で更新して～みたいな流れを踏む必要がある。

結果的に npm 側の `react*` を 検知しないようにして `Fable.*`,`Feliz` だけ dependabot サンに PR 作ってもらい、自分で femto で更新するように、今はしている。
なんか、特定の依存関係の PR を dependabot サンが作ったのを引き金にして workflow で femto install して commit 的自動化ができたらカッコよさそう。
でも実現可能性についてはまだ調べておらず、さっぱりわからん。

現状このような形での設定となった。
JST 金曜にチェックするのはだいたい週末に趣味プロするから。

```yaml
version: 2
updates:
  # Maintain dependencies for GitHub Actions
  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "friday"
      time: "09:00"
      timezone: "Asia/Tokyo"
    groups:
      workflow:
        patterns:
          - "*"
    assignees:
      - "krymtkts"
    reviewers:
      - "krymtkts"
  # Maintain dependencies for NuGet
  - package-ecosystem: "nuget"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "friday"
      time: "09:00"
      timezone: "Asia/Tokyo"
    groups:
      dotnet-tools:
        patterns:
          - "fable"
          - "femto"
      fable:
        patterns:
          - "Fable.*"
          - "Feliz"
    assignees:
      - "krymtkts"
    reviewers:
      - "krymtkts"
  # Maintain dependencies for npm
  - package-ecosystem: "npm"
    directory: "/"
    schedule:
      interval: "weekly"
      day: "friday"
      time: "09:00"
      timezone: "Asia/Tokyo"
    groups:
      dependencies:
        dependency-type: "production"
        exclude-patterns:
          - "react*" # NOTE: react should be updated with Fable.
      dev-dependencies:
        dependency-type: "development"
    assignees:
      - "krymtkts"
    reviewers:
      - "krymtkts"
```

あと NuGet の lock file を使ってる場合とも相性が悪いぽくて、 dependabot サンは `*.fsproj` のバージョンは上げてくれるけどそこから lock file の作り直しはしてくれないみたい。
npm の `package.lock.json` はやってくれるのになんでだよと思ったが、やっぱ Issue 立ってるみたい ↓。

[Support for NuGet package lock files · Issue #1303 · dependabot/dependabot-core](https://github.com/dependabot/dependabot-core/issues/1303) コレみたい。間違ってたらスマン。

結局この場合も手でメンテしてやる必要があって、いまんところ以下が最も引っかかることなく作業できるかなって思ってる。ここまで来たら dependabot サンが対応するまでは bump 用に task runner 作っておいた方が良いよな...

```powershell
rm .\src\packages.lock.json
# この辺を一掃しておかないとエラーになる
fable clean
femto install Fable.Core ./sr
# 動作確認ができて lock file が再生成される
npm run build
```

更に、 dependabot サンから PR を受け付けたら、端的にいうと動作確認が必要になる。
お試し運用期間は動作確認スタイルで良いかなと始めてみたけど、依存関係の更新頻度 1 週間でも結構きついなという量だったので、テストの自動化は必須。
でも krymtkts/blog-fable にはテスト無いから、単純に fable によるビルドと Markdown からの SSG がコケないのだけはチェックできたら良いかなと。

なので Pull request 受け付けたときにはビルドだけして、 main branch への merge 後に GitHub Pages へ deploy って形になる。
そしたら追加した PR 用と元々ある deploy 用でビルド箇所の YAML は丸かぶりするわけ。コレもめんどい。

結局 composite action を作って使い回すのが良さそうだった。
[Creating a composite action - GitHub Docs](https://docs.github.com/en/actions/creating-actions/creating-a-composite-action)

試してみたら、 composite action で出力されたファイルはうまく後続の step でも使えた。
こういう composite action を用意した。

```yaml
name: "Build pages Action"
description: "Setup .NET and Node.js, install dependencies and build pages."

runs:
  using: "composite"
  steps:
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        global-json-file: ./global.json
        cache: true
        cache-dependency-path: src/packages.lock.json
    - name: Setup Node.js
      uses: actions/setup-node@v4
      with:
        node-version: 20
    - name: Install dependencies
      shell: bash
      run: npm install
    - name: Build pages
      shell: bash
      run: npm run build
```

使う方はこう。

```yml
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
        uses: actions/checkout@v4
      - name: Build pages
        uses: ./.github/actions/build # ここが composite action で楽になっている
      - name: Setup Pages
        uses: actions/configure-pages@v3
      - name: Upload artifact
        uses: actions/upload-pages-artifact@v2
        with:
          path: docs/

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v2
```

他にワークフローの再利用を使う手もあったけど、今回のケースでは `docs/` に出力したファイル群を使いまわせるか不明だったので確認がとれてた composite action でやった。 [Reusing workflows - GitHub Docs](https://docs.github.com/en/actions/using-workflows/reusing-workflows)

いずれもドキュメントにはそういう記述なかった気がするけど案外いけるのかな。今度試してみたい。

---

Dependabot version updates を試してみたことで必要性に駆られて composite action にも手を出した。
脇道それた感あるが、結果的に [pocof](https://github.com/krymtkts/pocof) にも適用できるものを得られたのでヨシ！😹
