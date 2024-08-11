---
title: "F# で Cmdlet を書いてる pt.47"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[前回に引き続き](/posts/2024-08-04-writing-cmdlet-in-fsharp-pt46.html) `Query.run` 自体の最適化をしている。いつ終わるんだよ。

[#208](https://github.com/krymtkts/pocof/pull/208) 、 [#209](https://github.com/krymtkts/pocof/pull/209) でコツコツ改善？してみた。
わたしの laptop で 1 ~ 10 万までの数字から 1 つ選ぶのに 4 秒切りたかったがそこには達せず、伸びも見られなくなってきた。
単純な検索についてはそうなんやけど、プロパティ検索時においては修正前後で `null` の扱いがめちゃくちゃ雑なママ放置してて例外処理に任せてた部分を適切に取り持ったことで爆発的に速くなった。
他にもプロパティ検索周りはまだ改善の余地があるので、後ほど処置したい。

[判別共用体](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) を [構造体](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/structs) に置き換える最適化は、適用しどころを間違ってたみたい。
[`Data.Entry`](https://github.com/krymtkts/pocof/blob/cc9c317247adf010ff54cfdfc03bfbe70044922b/src/pocof/Data.fs#L138-L142) を構造体にしても効果なかった。
直感が外れたのは単純に理解が間違ってたからで、変更頻度が高い使い捨てのデータに対してこのアプローチをすべきだったからだ。
`Data.Entry` はオブジェクトの生成頻度こそ高けれど、一度生成されたあと変更されることがなく、アプリが終了するまで永続的に保持される。
そのため構造体が適しておらず最適化に貢献しなかったという理解だ。
代わりに [`processQueries`](https://github.com/krymtkts/pocof/blob/cc9c317247adf010ff54cfdfc03bfbe70044922b/src/pocof/Query.fs#L169-L201) 内でのみ利用される [`QueryResult`](https://github.com/krymtkts/pocof/blob/cc9c317247adf010ff54cfdfc03bfbe70044922b/src/pocof/Query.fs#L158-L167) に対して利用した。
繰り返し生成されて関数内で利用されるのみでまさに使い捨て用途がハマってる。でもそんな爆発的な効果は出てなくて少し残念。

実は最近のこの最適化で結構バグってるみたいで、細かく見れていなかった箇所のテストケースを追加するなどもしている。
その中で [`Operator.None`](https://github.com/krymtkts/pocof/blob/cc9c317247adf010ff54cfdfc03bfbe70044922b/src/pocof/Data.fs#L238) もう要らんのちゃうかないう気が湧いてきたので、今後取り除いていくつもり。
先述した最適化に伴うバグには、この `Operator.None` の挙動が変わってしまってるのも含まれる。
`Operator.None` を取り除いたことでロジックが簡略化されれば多少最適化にも貢献するだろう的なことも期待している。

元々空白を含めて検索したい場合に正規表現気にせず使えたらいいかなと思って準備したけど、キーワードの組み合わせで絞り込むことが多くてほぼ使わんし。
作った本人が使いこなせていないというのは恥ずかしい話やが、不要だと思った時に機能を取り除けるのは作った本人しか無理なので、この際やってしまう。

結構この最適化周りで修正重ねてるけどいまいちリリースしていいかなって思える状態にならない。
はよ出したいけど次の v0.15 を出すのはもう少し先になりそう。
