---
title: "F# で Cmdlet を書いてる pt.60"
tags: ["fsharp", "powershell", "dotnet"]
---

年末年始は結局 [krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。
command-line predictor の開発は始めず。

0.18.1 のリリースで小綺麗にできるようになったのもあって、基本コード最適化みたいなやつをやった。
[#285](https://github.com/krymtkts/pocof/pull/285) [#293](https://github.com/krymtkts/pocof/pull/293) [#295](https://github.com/krymtkts/pocof/pull/295) [#296](https://github.com/krymtkts/pocof/pull/296) [#297](https://github.com/krymtkts/pocof/pull/297) [#298](https://github.com/krymtkts/pocof/pull/298)
ずっとやらずに置かれていたページめくり [#16](https://github.com/krymtkts/pocof/issues/16) や行選択 [#17](https://github.com/krymtkts/pocof/issues/17) などで行を選択するやつを not planned で閉じたりもした。
PowerShell の [Format.ps1xml](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_format.ps1xml?view=powershell-7.4) で変わる見た目を今のところ尊重してるから使えんなという判断でだ。

他に新たな試みとしては、成果物のサイズを減らしてみたり [#284](https://github.com/krymtkts/pocof/pull/284) 、
[BenchmarkDotnet](https://github.com/dotnet/BenchmarkDotNet) でベンチマーク測定できるようにしたり [#294](https://github.com/krymtkts/pocof/pull/294) 。

ベンチマークはまだ GitHub Actions workflow に組み込んでない。どうも実行のたびにスペックが固定されてないらしくて、安定したベンチマーク測定に向かないらしいという情報をいくつか見かけた。
ただ相対値を測るくらいなら問題ないので、ベンチマークテストも追加できたら良さそう。これは今後に託す。

あと取り組んだけどうまくいかなかったものもある。 [FsUnit](https://github.com/fsprojects/FsUnit) と [FsCheck](https://github.com/fscheck/FsCheck) が依存する [xUnit](https://github.com/xunit/xunit) の major update だ。

---

FsUnit が [7.0.0](https://github.com/fsprojects/FsUnit/releases/tag/7.0.0) で xUnit 3 系に対応したので試したら、 `.nupkg` に dll と exe が含まれてて依存関係が解決できずそのままでは使えなかった。
報告して workaround を教えてもらったり対応してもらった [fsprojects/FsUnit#298](https://github.com/fsprojects/FsUnit/issues/298) ので、いざ pocof でそれを使いだしたら PBT が動かなくなってしまった。
すっかり忘れてたのだけど、 FsUnit だけじゃなく FsCheck も xUnit 依存してたのだった。

xUnit は v3 から NuGet package name が [`xunit`](https://www.nuget.org/packages/xunit) から [`xunit.v3`](https://www.nuget.org/packages/xunit.v3) に変わるという変更だけでも大きいのだけど、 xUnit の extension に提供されてる interface が大幅に変更されてた。
[Migrating from v2 to v3 [Unit test authors] > xUnit.net](https://xunit.net/docs/getting-started/v3/migration#migrating-to-v3-packages) 以降に詳しく書かれてる。
FsUnit の方は[ほぼコードが xUnit に依存してなくて package 変更くらいしか影響なかった](https://github.com/fsprojects/FsUnit/pull/297/files)けど、
FsCheck の方は interface 変更のあおりを受けてかなり変更しないといけない [fscheck/FsCheck#690](https://github.com/fscheck/FsCheck/issues/690)感じ。
(他の人もそう考えてたみたいだが)わたしもサクッと変更できるならコントリした方が速いなと考えて試しにやってみたが、この interface 変更やらがやたらメンドくていったん諦めた(ｵｲ)。
多分先述の xUnit の文書をガッツリ読み込めて、 xUnit の extension に詳しい人なら難しくないのだろうけど、ちょっと xUnit 素人がパパっと終えれるものではなかった。

FsCheck はいま [v3 の release candidate](https://github.com/fscheck/FsCheck/releases/tag/3.0.0-rc3) の段階だし、ここでこのクソデカ変更を入れるのか？みたいなのも判断難しいと思う。
xUnit は v2, v3 別々にメンテしていくつもりで NuGet package name 分けちゃってるしな。どえらいこっちゃなという印象を受けたが、こういうの一般的なんだろうか。
FsUnit が採用した v2 との後方互換性を落として v3 一択にする方針が、コミュニティベースで開発する OSS には向いてると思うけど、 FsCheck がどうするかはまだ動きが見えてない。
メンテナの人が昔別の OSS で xUnit v1 → xUnit v2 の経験があるらしくて、そのときは [NuGet package name を分ける対応](https://github.com/fscheck/FsCheck/issues/690#issuecomment-2568432777)をしたらしい。 xUnit 流のやり方に寄せるのかなー。複数パッケージをひとつの repo から提供した経験ないから興味深く観察する。

年末年始で xUnit v3 に上げれなかったのはちょっと惜しかったなと思うものの、その過程で [package に含まれる任意のファイルを消し去る技](https://github.com/fsprojects/FsUnit/issues/298#issuecomment-2565124825)を知れたり、 xUnit のように package name を変えて複数バージョンサポートするスタイルがあるのを知れたのはいい経験になった。
