---
title: "Terminal-Icons が動かなくなるケース"

tags: ["powershell", "terminal-icons"]
---

9 月&10 月と文字起こしするのをサボった 😪

肉親の不幸やトラブルがあったりで気乗りしなかったのもあるが、お仕事の方で WFH になって以降最高潮に忙しさのピークを迎えていたというのもある。この辺はチームビルディングぽい要素大いにあったのでまたなんかまとめたい所存 🤔

この間、OSS 活動も特に何のアクションもなく過ごしたが、ただ無為に過ごしたわけではない(と言い聞かせたい)のでなんか貢献したぽいことをネタに記事に残そうと思う。

---

先日[Windows Terminal Tips and Tricks | Windows Command Line](https://devblogs.microsoft.com/commandline/windows-terminal-tips-and-tricks/)を見てたら、愛すべき oh my posh の紹介の後に [Terminal-Icons: A PowerShell module to show file and folder icons in the terminal](https://github.com/devblackops/Terminal-Icons) が紹介されていた。
なんじゃこりゃー即導入せねばなるまいな、という感じで導入してみたのだが、初回は`Get-ChildItem`の結果に可愛らしいアイコンフォントが付与されるのに、2 回目以降は普通の見た目に戻ってしまい、なんでや...とトラシューしてみた。

わかったのは、一緒に使っていた [Get-ChildItemColor: Add coloring to the output of Get-ChildItem Cmdlet of PowerShell.](https://github.com/joonro/Get-ChildItemColor) の実行後に、Terminal-Icons のアイコンフォントが反映されなくなること。
試しに `Remove-Module Get-ChildItemColor` すればあ～ら不思議、アイコンが復活するのである。

Terminal-Icons では`format.ps1xml` で表示内容の改変を行っているのだけど、`Get-ChildItemColor`も色の改変をしてるし、競合してるのだろう。詳しくは追っていくのがめんどくて競合できないという結論だけだした。↓ の Issue で同じく困ってる人がいたので共有してあげた。

[No icons after installation. · Issue #12 · devblackops/Terminal-Icons · GitHub](https://github.com/devblackops/Terminal-Icons/issues/12)
