---
title: 年末の大掃除
tags: [dotnet, fsharp, fable, powershell]
---

先日 [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の dev server をいじってから、年末の大掃除的に他のも継続して修正してる。

細々した修正が主たるものだが、わかりやすいものだと blog-fable では振り返り日記をつけるにあたり、本を読んだ期間を本の link に添えたり [#433](https://github.com/krymtkts/blog-fable/pull/433) してみた。
わかりやすく視認性が上がったので満足している。

他にも触ってるのは [krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) で、雑に扱ってた resource management を小綺麗にできないか [#72](https://github.com/krymtkts/SnippetPredictor/pull/72) 試しているところ。
Command-line predictor の plugin なので plugin が remove されたら全部 resource 掃除されるしいいかと思ってたが是正してみてる。
PowerShell だし思わぬ形で長生きされても困るしな。
手法としては [krymtkts/pocof](https://github.com/krymtkts/pocof) で得た [`Volatile`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.volatile?view=net-10.0) などの知識を流用して、実装は AI に書かせてみてる。
毎日コツコツとリアル大掃除の方を進めているので、掃除中に実装してもらってレビューしている。

これらは大掃除が終わったら年を越す前に release してみたい気持ち。
使用感が変わるわけでもないが、全くリリースなく年を越すよりはなんとなくいいかなと。
あと数日大晦日まで時間があるので、その中でぼちぼち更新していきたい。

あと使い続けてる [Iris Keyboard](https://keeb.io/collections/iris-split-ergonomic-keyboard) の key の chattering が大変なことになっていて、特に Enter key が重大な問題になってる。
正しく打鍵どおりに入力できないから、しょうもないパスワード入力間違いや、実行したつもりの command が実行できてなかったり。
これを解消したいのだけど今のところホコリをとっても直らないしあとは接点復活剤か key 自体の取り換えしか選択肢がない。
年内に何らかの術でどうにか解消したいところだ。

これは機械接点に由来する問題だから mechanical switch の避けられない問題だ。
なので今の時代だと磁気スイッチとかが狙い目かも知れんが、中々自作ハードルが高いな。
