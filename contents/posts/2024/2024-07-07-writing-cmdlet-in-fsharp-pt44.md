---
title: "F# でコマンドレットを書いてる pt.44"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[前回の対応](/posts/2024-06-30-writing-cmdlet-in-fsharp-pt43.html) で非同期のバグが云々言ってたやつは、普通に immutable なデータ構造に起因するバグだった。ショボ。 [a9e5bab](https://github.com/krymtkts/pocof/pull/197/commits/a9e5bab2564239239f80e3349a3bee897f699a82)

> Prerelease 出すにあたり最後に解消しておきたいのが、 `-NonInteractive` 時に結構な確率で [Pester](https://github.com/pester/Pester) のテスト ≒ end-to-end テストがコケるところだ。いわゆる flaky test 化してしまった。

前に書いた ↑ は検討外れで、検索可能なプロパティの一覧を保持する際の内部データに F# の [`Map`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-fsharpmap-2.html) を利用していたので、非同期で要素が追加された際に結果が反映されないというものだった。
要はただの凡ミスだったわけなので .NET の非同期コレクションで置き換えて解消した。
他データ読み込み中に操作がない場合の描画間隔を調整したり [#198](https://github.com/krymtkts/pocof/pull/198) 細々とした修正を入れている。

他にも試したいことがあったのだけど、うまくいかなくて頓挫した。
以下に記すのは施工の整理のためのその記録。

---

まず code coverage の計測を [coverlet](https://github.com/coverlet-coverage/coverlet) から [altCover](https://github.com/SteveGilham/altcover) に変えてみようと思い、試してみた。
coverlet は F# が生成する IL 向けにチューニングされてないので、コード上に現れない微妙な branches のせいで coverage を落とす。↓ これ。

[Multiple branch points created for F# string slice · Issue #1208 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/issues/1208)

そのため F# らしく書けない事がある。
altCover でそれが解消せんかな的なことを考えて試してみた。
また F# の OSS を使うという点でも良い。
けどデフォルト設定で altCover で計測すると pocof 以外のバイナリも検査対象になり、そのせいか実行速度が 5 倍くらいになってしまった。
なんてこった。
対象のアセンブリを絞る設定が要るようだ。
でも設定してみても速くなったり F# の IL に最適化される確証ないなーと思ったのでこれは今のところナシとした。
sandbox で試してみてから pocof に導入できるか検討するのがまともな判断かな。

次に [pocof で大量データを操作すると遅い件](https://github.com/krymtkts/pocof/issues/177)に関する対応だ。
この対応として非同期で描画して遅く感じさせない改良を入れてきたけど、肝心の検索自体は速くなってない。
内部データの [seq](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seq-1.html) (実際は [ConcurrentQueue](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1?view=net-8.0)) に対して正規表現で検索し、件数を数えるので、時間計算量が O(N) かかってる。
もちろん件数を数えるときにも O(N) かかってる。

コレクションを分割して `Task` で並行処理したら速くならんかなと試してみたが更に遅くなった。
元々 CPU bound な処理だから並列じゃない限り速くならんのよな。
そもそも内部データの seq を分割する `Seq.chunkBySize` や処理後の `Seq.concat` で O(N) かかるし。
ただこの過程で [Array.Parallel](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-arraymodule-parallel.html) や [F# Parallel Sequences](https://fsprojects.github.io/FSharp.Collections.ParallelSeq/) を知ったので、なんか参考にできるのかも知れない。

他の方法として内部データ構造の変更が思いつくので、 ChatGPT や GitHub Copilot に相談してみた。
彼らは初め「[Trie](<https://ja.wikipedia.org/wiki/%E3%83%88%E3%83%A9%E3%82%A4_(%E3%83%87%E3%83%BC%E3%82%BF%E6%A7%8B%E9%80%A0)>) にしたらいいやん」って言ってきた。
プロパティ毎に Trie 作成するってことかあ？と思ったので、でも「 1 億要素あって `PSObject` の任意のプロパティに対して正規表現の検索が可能でないとあかんねんけど...」と伝えたら「それはデータ構造だけやと理論的に難しいで」とのことだった。
そら難しくなかったらお前に相談せんやろ...とりあえず検索パターンごとの最適化みたいなのを先に試してみるべきなのかも知れん。

例えば AND 検索であれば、クエリが追記されていく限りはそれより前の絞り込み結果に対して検索する方が少ない N 減で済むから速くなるとか。
でもこの方針だと、 OR 検索の場合や非同期コレクションが更新された場合は常に全体を検索せねばならず、どうにもならんけど。
pocof は無駄にリッチに AND 検索と OR 検索ができて、正規表現とワイルドカードのどちらでも検索もできるため、打つべき手段も色々ややこしいことになってる。
他に、検索機能を database なんかの外に出してみたらどうかとも思いつくが、なんか負けた気がするからそれは却下。 自前の何かで解決したい。
今のところ、全列に索引が貼られたテーブルみたいな如何にもメモリ食いそうな構造しか思いつかないので、なんか探求してみる。

そもそもの話、生の seq や `ConcurrentQueue` の件数を取ってみてもそんな爆発的に遅くないねんよな。
つまり pocof で入れてる実装に起因して遅い可能性も微レ存だと。
`PSObject` を文字列にするあたりだとか、存在するかわからないプロパティに reflection でアクセスするあたりだとか、なんか `PSObject` の取り扱いが難しいんじゃないのこれ。
前にプロパティ一覧取ってメモリ爆食いしてたやつを思い出した。

まだまだ伸びしろがあるということで一旦の締めとする。
結構直してきたので、この初の非同期描画版になった pocof はもういい加減リリースしたいな(何度目)。
