---
title: "F# でミニゲームを書きたい"
tags: ["fsharp", "game", "dotnet"]
---

[前回](/posts/2025-05-11-writing-cmdline-predictor-in-fsharp-pt8.html)で [krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) は落ち着いたつもり。
[krymtkts/pocof](https://github.com/krymtkts/pocof) もまだ弄りたいところはあるけどちまちましたもののつもり。
なので今日は今後やりたいなと思ってることをメモしておく。

なんとなく最近触れてないタイプのテクノロジに触れたいなーと思っており、そういえばここ数年まともに GUI やってないなと気付いた。
ここ数年の仕事では React やら PHP やらもっと前なら Delphi やら Visual Basic やらで GUI 色々やってた。けど趣味ではやったことはない。
そこで、折角なので F# で GUI やれないかなと考えた。ネイティブ寄りの GUI を。
やるならミニゲームやな。ゲーム開発となると全くの未経験なのでちょうどよかろう。

ひとまず GUI のフレームワークに関して。
まずきょう時点のわたしが知る限りの、 F# の GUI 周りをまとめておく。
とはいうものの、 F# の UI 事情がちょっとややこしくてわかってないので、振り返りも兼ねている。

最初は [Elmish](https://github.com/elmish/elmish) について。

最初 Elmish は [Fable](https://github.com/fable-compiler/Fable) のものかと思ってたが、 [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netdesktop-9.0) や [Avalonia](https://github.com/AvaloniaUI/Avalonia) をターゲットにしてもいけるらしい。 Web だけじゃないんか。
要は Elmish 自体はビルド先が何かは関係ない。
Elm-ish な Model-View-Update architecture を導入するメタパッケージ的なやつで、 platform に合わせた実装部分が切り離されてる。
少なくとも 2025-05-17 時点では。

> The goal of the architecture is to provide a solid UI-independent core to build the rest of the functionality around. Elm architecture operates using the following concepts, as they translate to Elmish:

Elmish のページにもそんなふうに書いてある。

次は、先に名前が上がったが、 [Avalonia](https://github.com/AvaloniaUI/Avalonia) について。

Avalonia もよくわからん。
OSS の .NET の coss platform UI framework 。
[Windows Forms](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/overview/?view=netdesktop-9.0) 、 WPF や [MAUI](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui?view=net-maui-9.0) といった MS 謹製 UI framework とは別の世界にいるみたい。
F# で Avalonia を使うなら、 [fsprojects/Avalonia.FuncUI](https://github.com/fsprojects/Avalonia.FuncUI) があって、 FP らしい宣言的 UI の書き方ができる。
Avalonia.FuncUI はそれ単体で使えるけど、[Avalonia.FuncUI.Elmish](https://www.nuget.org/packages/Avalonia.FuncUI.Elmish) という package もあって「え... Elmish で使えんの？」というのもわかった。
Avalonia を Elm-ish に使うための package やと [Avalonia.FuncUI の `README.md`](https://github.com/fsprojects/Avalonia.FuncUI?tab=readme-ov-file#example-using-elmish) に sample が載ってる。
登場人物 2 名の時点でややこしくなってきた。

あと F# には [Fabulous](https://github.com/fabulous-dev/Fabulous) もあったわ。

Fabulous はモダンな宣言的 UI framework で、 Elmish 同様に MVU architecture を導入するためのメタパッケージ。
それ自体は UI control を持たない。
↓ に引用した文が [Fabulous 2.0 | Fabulous](https://docs.fabulous.dev/) に書いてあった。

> Note that Fabulous itself does not provide UI controls, so you'll need to combine it with another framework like Xamarin.Forms, .NET MAUI or AvaloniaUI.

最後に Windows Forms や WFP (多少面倒らしい)は F# 直でも使え。 MAUI は無理なんか情報見当たらず。
いったんこれは抜きに考えようか。

結局のところ以下になるのかな。ややこしすぎる。

- 何らかの UI のパターンを導入するなら Elmish, Avalonia.FuncUI, Fabulous の 3 つの選択肢
- cross platform 抜きに考えたら Windows Forms, WPF, MAUI, Avalonia の 4 つの選択肢

もっと踏み込んだ話になると、 UI component 毎に得意な Platform がそれぞれ違うらしいが、もうそこまでは追いきれないからやめ。

とりあえず GUI 周りはこんな感じか。
でもこれらが pixel 描画に向いてるかどうかは全く知らんのよね。
これらの土俵じゃないんかなー。わからなさすぎて最高。
可能であれば cross platform にいきたいけど、面倒そうなら最初は Windows Forms で始めて...みたいなのもありか。 `System.Drawing` を使えばいいのよな。

題材は、お試しなので Game of Life みたいなよく知られた簡単そうなやつから始められたら良さそう。
ひとまず F# の CUI の Game of Life 実装があったから fork した。

[krymtkts/conways-game-of-life-fsharp: F# implementation of Conway's game of life](https://github.com/krymtkts/conways-game-of-life-fsharp)

試しに動かしてみたら、わたしの 2017 年の laptop ではパフォーマンスに問題があったので最適化してみた。
そのコードをいじる過程に並行して Wikipedia の Game of Life のページを見たりして理解を深めたつもり。
ゲームのコアの部分と描画部分は切り分け可能なので、こいつで得た知識をもとにコアを実装、 GUI は別途練習して、最後につなげるみたいな流れでできたらいいのかな。

日記に書く暇あったら sample だけでもはよ作れよという感じではあるが。

続く。
