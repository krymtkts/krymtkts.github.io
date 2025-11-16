---
title: "F# で Cmdlet を書いてる pt.79"
subtitle: "pocof 0.21.0"
tags: ["fsharp", "powershell", "dotnet"]
---

今週末はビールとワインのイベントにのもあって [krymtkts/pocof](https://github.com/krymtkts/pocof) の開発はできてない。
代わりにこれまでの改善を含めた [0.21.0](https://www.powershellgallery.com/packages/pocof/0.21.0) をリリースをした。
2025-11 は .NET 10 が来るので、 pocof を .NET 10 で開発する準備として、これまでの改善をリリースを挟んでおくのが良いと考えた。

0.21.0 での変更点は基本的に速度面メモリ面でのパフォ改善に尽きるので、 bug を仕込んでなければ特に使用感の変化なく使えるはずだ。
さらりと触ってる感覚では、内部的な最適化よりも描画の効率化が一番利いており、キビキビ描画されるようになった気がする。

今後考えている pocof 開発の大きな変更点としては、 [Multi targeting](https://learn.microsoft.com/en-us/visualstudio/msbuild/net-sdk-multitargeting) に挑戦してみたいと考えている。
やはり [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) に縛られたままだと高速化に限界があるので、新し目の platform に関してより最適化するならこれしかない。
ここで手を加えておくことによって、万が一にでも Windows PowerShell がくたばったとしても次に繋げやすいはずだ。

ただこれまでの単純な PowerShell module 構造でなくなるので多少勉強が必要だが、多分やれる。
それもいい経験になるだろう。
これまで loader script で import する dll を切り分けるような PowerShell module はいくつか見たことがある。
アレの具体的な仕組みは勉強したことないので、これを機に学ぶ。
探し方が良くないのかそれを示した具体的な公式文書は見たことがないし、ダメもとでまずそれを探すところからかな。
これまで経験的にはそういうのは公式になさそうなので、検証用の module で色々試して、まとめればよいかと考えている。

これは日記に何度も書いていることだが、仕事ではコスパ的にバランスしなくてやらないようなことでも、趣味プロならとことんやることができる。
改めて、これはほんと良いなと感じる。
