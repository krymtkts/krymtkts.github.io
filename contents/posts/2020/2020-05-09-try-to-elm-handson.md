---
title: "Elm のハンズオンを試した"
tags: ["elm"]
---

会社の勉強会で Elm のハンズオンをしてみた。それだけ。

### Elm とわたし

いつ頃 Elm を知ったのだろう。なんか前職ですごい H 本の輪読会をした 2015 年らへんか、2016 年とかそのあたりに知ったような気がする。

爆発炎上中の新製品開発(Backbone.js なのに誰も使えてなくてやばい製品だった)のヘルプに入ってちょっとした頃。フロントエンドを全然知らなかったので一通り新しめの情報をさらってて、その中に Elm を見つけたような気がしている。

最近までずっと Elm と付き合うことなく来た[^1]が、ちょうどへーしゃにてフロントエンド刷新の機運があると聞き、Elm 一択ですね！と押し込むためにも開催してみた。今思えばこいつ何様やねん 🤔

[^1]: [基礎からわかる Elm](https://www.amazon.co.jp/dp/4863542224/)はもちろん読んでるで。ポチってからかなり待ったけど、無事出版され、読むことができてホッとした。

### 勉強会について

[mather/elm-handson: ハンズオン資料](https://github.com/mather/elm-handson) を流用させていただいた。めちゃくちゃやりやすかったですありがとうございました。

カンペ用にわたしは回答例みたいなのを fork して添えさせていただいた。[krymtkts/elm-handson: ハンズオン資料](https://github.com/krymtkts/elm-handson)

元々はデータストアがあって、CRUD 操作ができて、みたいなバックエンドも含めたやつをやろうとしてた。
のだけど、わたしに Elm でハンズオンを作りきるパワーもなく、「う、めんどくせえ！」となり、またインターネッツで探してみるもめぼしいものを見つけられなかった。

やはり導入部ということもあり、もっとシンプルにできるものはないか－と探していたところ、前述のハンズオンにたどり着いたのだ。

### やってみた感想

へーしゃのフロントエンドに Elm を採用するのには至らなそうだけど、他のエンジニアに興味を持ってもらえり静的型付け＆関数型言語のパワーを知ってもらったのは良かった。Message を足したり型変えたらめちゃくちゃ親切なコンパイルエラーになるやでー、が結構ウケた印象。
現行が複雑怪奇な PHP(Smarty)や jQuery でメンテも苦痛なところに、Elm が爽やかな風を送り込んでくれたのだから、そりゃエンジニアの認識が変わるのも必然やで。

最終的に CRUD ある画面のハンズオンを目指す。遊び用 AWS 垢が支給されてるし、Amplify で Sample 探すか。
まだ何回か企画してるので、刺激を与えていきたい所存。
