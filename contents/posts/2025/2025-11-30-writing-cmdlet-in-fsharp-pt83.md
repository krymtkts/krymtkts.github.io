---
title: "F# で Cmdlet を書いてる pt.82"
subtitle: "pocof 0.22.0"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。
[0.22.0](https://www.powershellgallery.com/packages/pocof/0.22.0) をリリースをした。

最適化サポートを LTS の PowerShell 、今だと 7.4 なので .NET 8.0 を基準[^1]にして、その旨を `README.md` に明記した。

[^1]: [PowerShell Support Lifecycle - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/install/powershell-support-lifecycle?view=powershell-7.5#powershell-end-of-support-dates)

あと、クエリに変更がなければキャッシュした結果を使い回す機能をうっかり実装してしまった [#403](https://github.com/krymtkts/pocof/pull/403) ので、それもリリースに含めた。
クエリに変更がないというのは、クエリ文字列・一致条件・組み合わせ・反転などが一致している状態を指す。
これは当然 whitespace を打ち込んだ場合も cache を使えないということでもある。
なので現時点ではカーソル移動の時だけ有効な感じ。
とりあえずの出発点としてはこれでよいかと。

クエリ構築は結構重いので、生成しなくてよく無くなるだけで ms level ではあるが節約になる。

| Method        | QueryCount | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------- |----------- |-------------:|------------:|------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| prepare       | 1          | 1,389.987 ns |  23.8900 ns |  42.4645 ns | 1,382.051 ns | 1.001 |    0.04 | 0.7038 |      - |    2944 B |        1.00 |
| prepareCached | 1          |     7.167 ns |   0.2167 ns |   0.3963 ns |     7.133 ns | 0.005 |    0.00 |      - |      - |         - |        0.00 |
|               |            |              |             |             |              |       |         |        |        |           |             |
| prepare       | 3          | 2,739.519 ns |  54.3090 ns | 113.3631 ns | 2,697.941 ns | 1.002 |    0.06 | 1.3695 | 0.0038 |    5728 B |        1.00 |
| prepareCached | 3          |     7.104 ns |   0.2165 ns |   0.3497 ns |     7.149 ns | 0.003 |    0.00 |      - |      - |         - |        0.00 |
|               |            |              |             |             |              |       |         |        |        |           |             |
| prepare       | 5          | 4,242.972 ns |  83.1371 ns | 224.7660 ns | 4,166.865 ns | 1.003 |    0.07 | 2.0294 | 0.0076 |    8512 B |        1.00 |
| prepareCached | 5          |     7.288 ns |   0.2180 ns |   0.5054 ns |     7.249 ns | 0.002 |    0.00 |      - |      - |         - |        0.00 |
|               |            |              |             |             |              |       |         |        |        |           |             |
| prepare       | 7          | 6,242.056 ns | 280.0643 ns | 825.7758 ns | 5,906.579 ns | 1.017 |    0.19 | 2.7084 | 0.0076 |   11328 B |        1.00 |
| prepareCached | 7          |     6.755 ns |   0.2117 ns |   0.3357 ns |     6.662 ns | 0.001 |    0.00 |      - |      - |         - |        0.00 |

このクエリ構築が重いのは、キャッシュを履歴化するだとか、差分コンパイルするだとかで改善できると考えられるが、どこまでやるかやな。
どのみちクエリの実行自体が最も重いので、こういうのは細かな調整レベルの話なのだが、色々考えることがあって楽しい。

prerelease は久しぶりに使ったのだけど、 prerelease 版と正規版がどちらもバージョン文字列の directory を利用することをすっかり忘れていた。
お陰で PowerShell Gallery から最新版(正規版)を更新するとき、 PowerShell が `FSharp.Core.dll` を掴んだままで消せなくて手間取った。
これは .NET がプロセスを死なせるまで握り続けてしまうからで、開いている PowerShell をすべて殺してから更新するしか術がない。
この事象に出くわしたのも久しぶりで何？となったのだけど、これは .NET の未来永劫変わらないのかな。面倒なのでいつか改善してくれればいいのだけど。
うっかり忘れがちなので pocof で prerelease を使う時は次の version まで飛ばしてしまう方が安定していて良いか。

あと 1 ヶ月程で 2025 年も終わるが、あと 1 回位は改善リリースができたら良さそうかな。
まだ Multi targeting にした利点を全く活かせてないので、そこ攻めたいよな。
