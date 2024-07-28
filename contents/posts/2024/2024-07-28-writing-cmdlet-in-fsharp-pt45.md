---
title: "F# でコマンドレットを書いてる pt.45"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[前回のバグ対応](/posts/2024-07-07-writing-cmdlet-in-fsharp-pt44.html) を [v0.14.1](https://www.powershellgallery.com/packages/pocof/0.14.1) としてリリースして以降、 pocof をパフォ改善する開発はあんまうまく進んでいない。
そろそろ真面目に profiling すべきなんかも。 .NET 門外漢過ぎてわからん。

pocof の最適化については前にも触れてるが、当初 Trie や Suffix Tree を使って事前に検索しやすい形を構築しようかと考えていた。
けど、これを全プロパティ + 文字列化されたオブジェクト分で構築すると空間計算量がかなり高く付くので、これはどうなんかな～と思って踏ん切りがつかなかった。
同時に正規表現を捨てるのも覚悟が足りず。
自身の利用シーンでは最低限として中間一致だけの検索モードを提供するってのはありかもなーと思ったが、これも踏ん切りがつかず。

他にできることとして [PSObject](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.psobject?view=powershellsdk-7.4.0) を何度も [toString](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.psobject.tostring?view=powershellsdk-7.4.0) している箇所が重いんかなと生成回数を減らすよう変えてみたが、効果は全くなかった。

もう CPU で殴るしかないかなということで、先達である [fsprojects/FSharp.Collections.ParallelSeq](https://github.com/fsprojects/FSharp.Collections.ParallelSeq/tree/main) のアプローチを便器ょした。
そこから [ParallelEnumerable](https://learn.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable?view=net-8.0) を使って、 pocof に足る範囲の並列可能な F# 風 sequence (追加順維持)をこしらえてみた。 [#207](https://github.com/krymtkts/pocof/pull/207)

pocof の用途に合わせて、必要な用途に絞った最小の実装にした。
[ParallelEnumerable.Where Method](https://learn.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable.where?view=net-8.0) で `filter` 、
[ParallelEnumerable.Count Method](<https://learn.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable.count?view=net-8.0#system-linq-parallelenumerable-count-1(system-linq-parallelquery((-0)))>) で `length` を作った。
pocof では追加順を維持するのが重要なので、 [ParallelEnumerable.AsOrdered](<https://learn.microsoft.com/en-us/dotnet/api/system.linq.parallelenumerable.asordered?view=net-8.0#system-linq-parallelenumerable-asordered(system-linq-parallelquery)>) を使ってる。

`AsOrdered` にしたので通常の ParallelEnumerable よりパフォ劣化するせいと思うが 100 万 ~ 1000 万件位のデータ量で確認した限り普通の seq と比べて速いわけでもなく、むしろ少し遅い。
それでも CPU ゴリゴリに使って頑張ってくれるのはわかるが、そこまで速くなく更に CPU 使いすぎるのもどうかな～と思えてきた。
大量データのときだけ CPU 頑張ってくれるだけなのでこれは main branch へ投入したけど、あまり良くなければ戻すかも。

CPU で殴っても効果がないとなるとあとやれることはロジック自体の改善。
ということで、いまは `Query` module の対象 object か調べる処理自体を末尾再帰で書き直して速くならんか見ている。
[#208](https://github.com/krymtkts/pocof/pull/208)

元々は 1 object 毎にクエリの list から filter 条件の list を作り、それに対して [List.exists](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-listmodule.html#exists) か [List.forall](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-listmodule.html#forall) する形だった。
そこを末尾再帰で書き換えたことで、 list の生成処理が丸ごとなくなるのもあってちょっと効果出ている。
ただ結構コードの循環的複雑度が高まったので、清書する必要がある。

この 1 objects あたりの処理量を減らす方向性は間違ってないっぽいので(爆発的な効果は出てないが)、他にもできることがないか検討してみる。
機能維持しつつ爆発的に速くなる方法があればよかったけど現実的にそういうのはまず起こらないので、地道に改善重ねていくしかないかな。
AND 検索しかなかったらドンドン母数を絞り込めるからクソチョロなんやけど、 pocof は親切なことに OR 検索があるから...コツコツやるかデータ構造変更しかない。
非同期描画のときは結局 alpha で出さなかったので、今回の並列＆最適化こそは prerelease するタイミングなのではないかと思っている(やるかはわからん)。

続く。
