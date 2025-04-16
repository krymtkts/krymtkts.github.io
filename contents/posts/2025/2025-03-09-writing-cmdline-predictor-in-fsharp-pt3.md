---
title: "F# で command-line predictor を書いてる Part 3"
subtitle: "Expecto"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor", "expecto"]
---

主に [krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発をした。
2 ~ 3 月は公私ともに忙しくあまり進捗はないが、テスト周りを整備し始めた。

[以前触れた](/posts/2025-02-23-writing-cmdline-predictor-in-fsharp-pt2.html)ように、 test framework には [Expecto](https://github.com/haf/expecto) をしてみた。
[年末年始に調べた](/posts/2025-01-05-writing-cmdlet-in-fsharp-pt60.html) [FsUnit](https://github.com/fsprojects/FsUnit) と [FsCheck](https://github.com/fscheck/FsCheck) を組み合わせた場合の [xUnit](https://github.com/xunit/xunit) version 固定のことを考えたら、より依存関係の少ない Expecto の方が制御可能だろうという見立て。

少し前に、 Microsoft の blog で主要な test framework が [Microsoft.Testing.Platform](https://github.com/microsoft/testfx) に対応したという記事が流れてたが、その中に Expecto も含まれてる。
因みに主要な、とあるようにこれは xUnit も v3 から対応してる。 FsCheck と共に使ってると v2 荷据え置きなので使えないけど。

[Microsoft.Testing.Platform: Now Supported by All Major .NET Test Frameworks - .NET Blog](https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/)

わたしは hobby F# Ninja で .NET のテスト事情門外漢のため、この対応によって安定性だとか移植性だとかが向上しているようだが、具体的に何が変わったのかというのは説明が難しい。
たった 1 つ明確に言えることといえば、 Expecto も xUnit も test project の [EntryPoint](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/entry-point#explicit-entry-point) が不要になったところか。

```fsharp
module Program

[<EntryPoint>]
let main _ = 0
```

従来は明示的に `[<EntryPoint>]` を作っておかないとテスト実行できなかった。
pocof は今も xUnit を使っているので、 [/src/pocof.Test/Program.fs](https://github.com/krymtkts/pocof/blob/v0.18.1/src/pocof.Test/Program.fs) がそうなってる。
こういうおまじないがなくなっただけでも、割と十分かなと思っている。

Expecto を初めて使ってみた感触は、Expecto 基本的には関数でモリモリ書くような感じの使い心地。 repo にも書いてあったが使ってみた感じは framework というより library だ。
[Attribute](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/attributes) も少なく、今のところテスト対象を指定する `[<Tests>]` しか使ってない。

また `Expecto.Flip` を使いさえすれば pipe operator で流れるように test code を書けるので、 FsUnit 同等に使えるなという感触だ。事前にこのことを把握してなかったら移行してないくらい重要なこと。
この `Expecto.Flip` については FsUnit の Issue も触れられてる。 [FsUnit.Expecto · Issue #120 · fsprojects/FsUnit](https://github.com/fsprojects/FsUnit/issues/120#issuecomment-537664863)

いまは example-based test を Expecto で書いているが、 [Expecto は FsCheck との統合も自然な感じ](https://github.com/haf/expecto?tab=readme-ov-file#property-based-tests)なので、 PBT も早く試してみたいと考えている。
pocof でも少し PBT を書いているが未熟なので、初っ端から書くというのができてない状態だ。
まずは Expecto の練習に努める。

Expecto 自体の話はここまで。
次は SnippetPredictor のテストのし難さについてメモしておく。

まず snippet を保存する先がファイルなのもあって .NET の I/O 関連をよく使うのだけど、 [Nullable reference type](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/values/null-values#null-values-starting-with-f-9) を有効にしてるので、 .NET 由来の library には頻繁に `null` の取り扱いを示さないといけない。
これ自体は安全性のために良いのだけど、 SnippetPredictor で到達不能な pass に於いて発生するもんだから coverage に悪影響及ぼす。
仕方ないこととはいえ可能な限り経路をチェックしたいので頭を悩ますポイントだ。

次にこれは一般論でもあるが I/O そのもののテストでの取り扱いも難しい。
0.1.0 は機能をリリースするために testability 考えず作ったので、 I/O 抜きでテストが難しい点がある。
pocof でも似たような課題があり、 unit test ではシンプルな currying ベースの injection を使って処理を差し替えることでテスト可能な範囲を広げたので、 SnippetPredictor でも I/O や環境変数へのアクセスで同様の方針で再構築を考えている。

ただこれの良くない点は実際のコードの一部がテストを通さない点で、即ち coverage に悪影響する。
Pester でのテストで確認できるのであれば不要とも考えることができるが、やはり個人開発で 1 週間とかの比較的長い繰り返し周期で開発するのであれば、 coverage はガチガチにチェックしてた方が安心なのでなんとかしたい。どうせすぐ実装忘れるし。
今 I/O に関しては、環境変数で snippet のファイルを配置する directory を指定できる機能を設けてあるので、当面はそれで以てカバーするかも。

ただ Expecto はデフォで並列・非同期に動くらしいから、それを考慮した実装になってないと上記の手段で実装したテストは flaky になるだろう。
なので SnippetPredictor 内部の snippet を保持する実装も module ベタに書くんじゃなくて同時実行性を考慮して改修する必要がある。

追加したい機能の案も浮かんでおり、やることは沢山ありそうやけど、そのへんとテストとをいい塩梅に進めていく。
続く。
