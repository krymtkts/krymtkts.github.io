---
title: "blog に llms.txt を追加した"
tags: ["fsharp", "dotnet", "llm", "llms-txt"]
---

当 blog に [`/llms.txt`](https://llmstxt.org/) を追加した([#564](https://github.com/krymtkts/blog-fable/pull/564))。
いつかやっとくか～と思ってから放置してたが、今回着手したら知らない内に v2 が出てたみたい。
なので v2 に対応したものとなっているはず。

生成するのは、 posts ・ pages と書籍毎の booklogs の詳細 Markdown(`*.html.md`) 、及びそれらの link をまとめる `llms.txt` 。
index 、 archive 、tags 、 booklog の top page や年ごとのまとめ頁は含めないようにした。
index は最新記事の写しだし、他は記事の内容やリンクを分類したりまとめただけなので、不要。

詳細 Markdown はそもそも各頁の元が Markdown なのでそれを流用し、 front matter の必要な情報は bullet list に変換して埋め込んだ。

`llms.txt` は仕様に従って動的に生成する。
仕様では、各詳細 Markdown  の link には要約をつけるのが例として書かれている。
以下は例示されている <https://www.fastht.ml/docs/llms.txt> からの引用。

```markdown
- [FastHTML quick start](https://fastht.ml/docs/tutorials/quickstart_for_web_devs.html.md): A brief overview of many FastHTML features
```

けど当 blog でそれをやろうとすると、各記事の要約を AI に自動生成させるか、ヒトがそれぞれの記事の要約を書く必要が出てくる。
AI にやらせると結果が非決定論的てブレが生じるから NG 。
でもヒトがそれをやるなら、各記事を書く度に要約を考えて front matter にかくとかしないといけなくなる。これはめんどい。
link の要約を省くのは仕様違反ではないようだし、当 blog は `title - sub-title` の形式にしているので、それで内容を表すのに十分な情報とみなし、以下の形式にした。

```markdown
- [F# で command-line predictor を書いてる Part 12 - 0.8.0](https://krymtkts.github.io/posts/2026-09-13-writing-cmdline-predictor-in-fsharp-pt12.html.md)
```

全体は以下のようになる。

```markdown
# krymtkts

> krymtkts's personal blog

## Posts

- [ブログを始めた](https://krymtkts.github.io/posts/2019-01-08-first-post.html.md)
... 省略 ...

## Pages

- [About Me](https://krymtkts.github.io/pages/about.html.md)

## Booklogs

- [アドレナリンジャンキー プロジェクトの現在と未来を映す 86 パターン](https://krymtkts.github.io/booklogs/adrenaline-junkies.html.md): Tom DeMarco, Peter Hruschka, Timothy Lister, Steve McMenamin, James Rovertson, Suzanne Robertson, 伊豆原弓
... 省略 ...

```

ブログを続けるだけページ数が増えてくので、結構 `llms.txt` の行数が多いが、ソレは仕方ないかな。
今後多過ぎることで問題になるようなら、最近の posts や booklogs だけにして量を減らすとかはありかも知れんが、網羅性という意味で劣るので様子見やな。

そして改めて見てみると... posts と booklog の並び順があまり好きじゃない。
posts ・ booklog ともに は日付降順の方が良い。今 posts は日付昇順、 booklog は slug 昇順になってるな。
仕様に触れない部分なので、サクッと直すのが良さそう。
