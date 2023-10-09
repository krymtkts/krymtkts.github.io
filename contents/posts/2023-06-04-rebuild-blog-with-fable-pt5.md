---
title: "Fable でブログを再構築する pt.5"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築しようとしている。

雑多な更新 [#11](https://github.com/krymtkts/blog-fable/pull/11) [#12](https://github.com/krymtkts/blog-fable/pull/12) [#13](https://github.com/krymtkts/blog-fable/pull/13) 。
favicon が出てなかったの直したり、 404 ページ作ったり、あとやりたかった module の整理。

ファイルは雑に 共通系 -> 生成系 -> アプリ の依存関係に切り分けて、それぞれのファイル内に雑多な module が詰まってるようにした。 元は 2 ファイルだったのでこれでもだいぶマシかな。でも module の整理は今結構雑な感じで置いていて、もうちょいと関数を細切れにできるのでその後また見直す。互いに変な依存関係してたり、 生成系関数のインタフェースが不一致だったりするのとか、気になるのが残ってる感じ。
それより優先して細々した直したい部分があってわざと置いてる。

細々した直したい部分というのは navbar で、 Bulma (暫定的にこいつで行くことにしたから)の [Navbar Component](https://bulma.io/documentation/components/navbar/) 見てみたのだけどこんな複雑に使い分けなあかんの？みたいな div div した感じになってて、これ導入したくないなあと感じてた。
nav -> ul -> li でだめなんか。
[Tailwind の Navigation](https://v1.tailwindcss.com/components/navigation) だと思ったようなシンプルな感じのものが得られるのだけど、あの大量の class をつけるのもなあという感じだった。
世の中 Navbar はみんなグリグリ動いたり深ーい階層の div div したやつを、好きでやってんのかな。いやないはずだ。
とりあえず自分のやつではやりたくないなって感じの奴らなことは確かな感触を持った。
ちょうど [Tabs Component](https://bulma.io/documentation/components/tabs/) というもっとシンプルで ul -> li に被せられるスタイルがあったので、これを適用する感じにし、諸々調整した。 [#14](https://github.com/krymtkts/blog-fable/pull/14) [#15](https://github.com/krymtkts/blog-fable/pull/15)
Tailwind の Navigation で得られるシンプルなやつに近い感じ。モバイルなんかでもグリグリ UI が変わらず同じ見た目でスライドするだけなので、この方がいい。

とはいえこういう div div したくないというのは [Feliz](https://github.com/Zaid-Ajaj/Feliz) で HTML を直書きする方向性を選んだから書くのがだるいのも多少ある。
もちろん生成物が div div してるとキモいなというのが一番だけど。
ここにきて[Fulma](https://fulma.github.io/Fulma/#home)を導入したら楽になるのか？というのを Navbar のくだりで多少試したのだけど、なんか Feliz より書き味良くなかった(慣れの問題かも)ので今は据え置きにしている。
仮に Fulma を導入するとなると、あの div div した生成物を受け入れるってことやからな。

div div 書きまくってどれだけあれが好きでないかというのが伝わっただろうが、仕事ではそういう div div した構造を目当てのレイアウトを得るために書くこともあるということは断っておきたい。
必要な div なら仕方がない。でも自分の趣味プロで書くのであれば、仕事と同じことしても余白が狭すぎるし、極力簡素にしたいということでね。

もう最近やったことまとめと化してるけど、これも振り返りにちょうどよいので悪くない。
ひとり(プラス GitHub Copilot サンと ChatGPT サン)で黙々とやってると、フィードバック得られるところも限られるからな。こういうことをダラダラと書き連ねるだけでも、次のアイデアにつながる。
ちょうどこの記事をしたためいている中で、 footer 要素がないわ...というのに気づけた。
とりあえずこの記事を push してから、それに取り掛かることとする。
