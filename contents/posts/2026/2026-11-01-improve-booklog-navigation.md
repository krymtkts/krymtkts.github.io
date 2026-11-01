---
title: "blog の booklog の使い勝手を少し改善した"
tags: ["fsharp", "dotnet", "booklog"]
---

今週は大して開発してない。
ここ最近 [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) に細かい改善を入れてたので、それについて書いておく。
[前回](/posts/2026-09-20-add-llms-txt-to-blog-fable.html)は [`/llms.txt`](https://llmstxt.org/) を追加した話を書いたが、今回は主に booklog の使い勝手を調整した話。

booklog は [2024 年の夏に始めた](/posts/2024-09-01-start-booklog.html)ので、もう 2 年以上経つ。
一日一読を続けてるので年ごとのページは少なくとも 365 エントリあって随分長い。
なので、ページ内を移動しやすくしてみた。

まず、年・本の一覧へ移動する jump link を追加 [#586](https://github.com/krymtkts/blog-fable/pull/586) した。
年ごとのページは calendar の下、書籍ごとのページは title と著者名の下に `Years ↓` と `Books ↓` を置いた。
それぞれページ内の一覧へ飛ぶだけの単純な anchor link だ。
年の記録を全部スクロールしなくても、その年に読んだ本や別の年の一覧を見られるようになった。

あと、ページの先頭と末尾へ移動する `↑` と `↓` のボタンを追加 [#610](https://github.com/krymtkts/blog-fable/pull/610) した。
年ごとのページと書籍ごとのページで、画面右下に固定表示する。
途中まで読んだところから先頭へ戻るとか、末尾の一覧へ移動するときに使える。

元々は Pagefind での検索で本にアクセスしやすいし、年ごとのページでは Home/End key で移動できりゃいいかな～と思って放置してきた。
でも実際のところ、検索ワードもおぼろげに調べるのであんま検索出来ないのよね。読みながらキーワードも思い出す。
となると年のページをスクロールしまくるのだけど、それをしながらキー操作は結構面倒で、マウスでそのまま操作したくなる。
jump link の追加と `↑` と `↓` のボタンの追加で、マウスでの先頭・末尾へのアクセス性が改善されて使いやすくなった。

ついでに [#587](https://github.com/krymtkts/blog-fable/pull/587) [#600](https://github.com/krymtkts/blog-fable/pull/600) 等 booklog 全体の style も整えてる。
streak の場所を整えたり、 booklog の本文や一覧を共通の [Bulma](https://bulma.io/) の section に収めたり。
全体の配置や余白の取り方が揃って Bulma らしく収まったのではないかな。

残りは依存関係の集中管理やら CI の整理。
blog-fable を作ってからもう数年経つので、そろそろ小綺麗にしたほうが良い感じかなというのもある。
[Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) にした [](https://github.com/krymtkts/blog-fable/pull/603) 。
NuGet と npm の脆弱性検査を CI に入れて [#601](https://github.com/krymtkts/blog-fable/pull/601) 、 [Snyk](https://snyk.io/) の scan を外した [#602](https://github.com/krymtkts/blog-fable/pull/602) 。
Snyk 良いのだけど、よくわからないところで private manifest の scan が消費されて、それを解消できなかった。
Free で使ってると今の開発頻度では usage limit が枯渇して CI がエラーで汚れるからやめた。
Snyk だけで見つけられる脆弱性ってのも blog-fable 等ではないし、 package manager 組み込みの脆弱性検査を使うことにした。
他、 CI の `npm install` を `npm ci` にし、 [Playwright](https://playwright.dev/) で Chromium だけを install するようにし、同じ PR の古い CI をキャンセルする等。
テストも整理した [#614](https://github.com/krymtkts/blog-fable/pull/614) [#615](https://github.com/krymtkts/blog-fable/pull/615)。

当面はこれで使いつつ、また面倒なところが出てきたらちまちま直していくつもり。
本体のコードには手を入れてないから、こいつも十分古くなったから手直ししたほうが良かろうな。
blog-fable は初めて作った SSG で、めちゃくちゃ書き捨てコードみたいになってるだろうし、もっとキレイにするのも一興か。
