---
title: "F# で Cmdlet を書いてる pt.59"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

年内になんとか [0.18.0](https://www.powershellgallery.com/packages/pocof/0.18.0) 、その bug 修正版の [0.18.1](https://www.powershellgallery.com/packages/pocof/0.18.1) をリリースをできた。
[前回触れた以下の bug](/posts/2024-12-22-writing-cmdlet-in-fsharp-pt58.html) の対処が間に合って良かった。 [#276](https://github.com/krymtkts/pocof/pull/276)

> 今のところただの query であれば問題ないのだけど、 property query だと指定した property が認識されてなくて絞り込めない bug がある。

対象の絞り込みと描画が非同期に行われるため、 `-Query` option を指定した pocof 起動直後の絞り込み時に選択可能な property の list が完成していないと property query が無効になる bug だった。
なので絞り込みのタイミングをずらすために、 query の絞り込みと property name の候補の結果を遅延評価するようにした。
F# なら [Lazy Expressions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lazy-expressions) で簡単に導入できる。
一点 `Entry seq` の引数に `Entry pseq` を取り回せていたところが、遅延評価にしたことで `Entry pseq Lazy` と明示的な型の指定が必要になったのでそこだけ想定外だったか(理解不足かもしれんが元の制約が緩かっただけかな)。

結果を得るタイミングを遅らせただけなので、実質的に根治策というよりある種の緩和策なところだけが懸念点か。
仕組み的に、ものすごく膨大な検索対象に複数の object が含まれていた場合、登場するのが遅い object の property はこれまで同様に起動直後の property query として無効になるだろう。
とはいえそんな膨大な検索対象の場合は [`ProcessRecord`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.cmdlet.processrecord?view=powershellsdk-7.4.0) 中の定期描画で救われるだろうから、大きく問題になることはない多分(心配なので今度試す)。

遅延評価に切り替える過程で、 UI のリファクタも行った。
これは従来のレイアウトである `{prompt}>{query} {matcher} {operator} [{filter count}]` だと遅延評価を組み込むのに支障があったためだ。
query window の幅を算出するのに `{query}` 前後の `{prompt}` と `{matcher} {operator} [{filter count}]` の文字数を知る必要があったが、絞り込み件数を正格評価しないと `{filter count}` の文字数を正しく算出できない。
遅延評価されても query window の幅算出に影響ないレイアウトであればよいので、解決策として 1 行ずらして notification 等を表示する行に `{matcher} {operator} {filter count}` をずらした。
`{filter count}` が膨大な検索対象がある場合のパフォ劣化の原因でもあるので消してもいいけど、個人的に良く見てる項目なので置いておきたかったというのもある。

これが、

```plaintext
>:Name a                        match and [7]
note>xxxxxx
```

こうなった。

```plaintext
>:Name a
note>xxxxxx                     match and [7]
```

多少見た目悪い気もするがこれによって query window 幅の動的な計算が不要になり、今後の内部状態の設計もスッキリしそうなので、良いタイミングでの決定だったかなと。
ただ残念ながらこの [0.18.0](https://www.powershellgallery.com/packages/pocof/0.18.0) のリリースで terminal 端っこにカーソルが位置したときに UI が崩壊する bug を仕込んしまったので、急遽直した。それが [0.18.1](https://www.powershellgallery.com/packages/pocof/0.18.1) になった。 [#281](https://github.com/krymtkts/pocof/pull/281)

うわ～あったなこの挙動と bug の原因に気づいてから思い出したのだけど、 [PSReadLine](https://github.com/PowerShell/PSReadLine) なのか [Console](https://learn.microsoft.com/en-us/dotnet/api/system.console?view=net-9.0) なのか何に由来する挙動か知らないが、行末端までカーソルが進むと改行して次の行に移るやつがあって、その影響っぽかった(忘れる前にこの現象についても調べておかな)。
行末にカーソル位置がなければ問題ではないので、現状 query window の幅を 1 文字縮めることで回避している。
自動で次の行に進んでしまうので、進んでから戻すよりも、進ませないのが手数を少なくするのを優先した対応としては正しいはず...多分。

もうちょい事前にテスト出来てたら 100% 気づけた bug だが、現状手動テストしか出来ないところではあるし、中々難しおまんな。
ただ debug build でのみ有効な autopilot mode みたいなの作り込めたら E2E testing 改善の可能性あるか。
そうすれば理論的に PBT における stateful properties か state machine properties が書けるハズや。
[FsCheck](https://github.com/fscheck/FsCheck) で [Model-based Testing](https://fscheck.github.io/FsCheck//StatefulTestingNew.html#Model-based-Testing-Experimental) がそれに該当するはずやから試す価値はあるよな(まだ初歩的な generator しか使ってないし)。

来年も pocof の開発することありそうな感じやな～。
やりたいことがいっぱいあるというのはいいことや。
