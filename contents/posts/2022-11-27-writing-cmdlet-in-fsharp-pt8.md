{:title "F#でコマンドレットを書いてる pt.8"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

前回書いた、 composite query の実装をした。 [#9](https://github.com/krymtkts/pocof/issues/9)

デフォルトでは and で複数のクエリを合成するようにしてる。
一応理由がある。
最初から or だと絞り込む前の状態を覚えていて、追加でほしい候補のためにクエリを書くような使い方になる。
そういう使い方もできるが、ちょっと上級者感がするので、まずは見える範囲で徐々に候補を絞り込めるような and をデフォルトとすることにした。

そのうちショートカット含めた初期値の変更にも対応したいと思ってたし、起動時から or にしたい場合はそこに盛り込めたらいい。

ただこの and での合成は hashtable を絞り込むときに出来が悪くて、まだ思ったような絞り込みになっていない。現状 hashtable は or でのみ気持ちよく絞り込むのができなかった。

これは、 `List.allPairs` した複合クエリの List × エントリの List に and の場合は `List.forall` or の場合は `List.exists` してることで、 hashtable はエントリ Key-Value の二要素あるので and だと両方に共通の文字がないと釣れない...となっているため。
正しくは、 エントリ内の検索候補プロパティは or で、各エントリ毎は and / or のモードを適用できるようにするのが良いか。
プロパティ指定の絞り込みするときにこの問題対処するの必須。

あと like matcher のときだけクエリが空の状態でもフィルタリングされて表示が 0 件になる挙動を直した。 [#10](https://github.com/krymtkts/pocof/issues/10)

流石に初期表示が 0 件だとはじめ何で絞り込んだらいいんや...(wildcard 埋めたらいいだけやけど)と困惑するので、はじめは全件表示する。

最後に、今回 PowerShell Gallery に公開する時手間取ったのが、 [platyPS](https://github.com/PowerShell/platyPS) 。

[前にモジュールの掃除した](/posts/2022-11-12-clean-up-pwsh-modules)影響で [platyPS](https://github.com/PowerShell/platyPS/tree/v2) の version 2 preview より前のモジュールを消し去ってた。
コレにより `New-ExternalHelp` Cmdlet がなくなっていて、 psake のリリーススクリプトがエラーするようになってた。
全く想定外。 v2 で消えるのかこの Cmdlet ？？て感じ。
まだ [README.md](https://github.com/PowerShell/platyPS/blob/8ca28935c376ae8dc36ac142e4960fc6d3b725e5/README.md) に消えるような話書いてないし、対応待ってたらいだけなんかな ↓↓

> Create external help xml files (MAML) from platyPS Markdown.

とりあえず preview でない platyPS を使うことで回避した。
pocof に v1 刻むときにはヘルプファイルもきれいにする必要があるし、その時までに platyPS が v2 でほんとに Cmdlet 失われるのかとかチェックする必要が出てきた。
