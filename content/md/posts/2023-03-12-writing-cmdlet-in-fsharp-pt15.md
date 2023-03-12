{:title "F#でコマンドレットを書いてる pt.15"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

前回困ってた Ionide 、理解が難しくあんまわかってないけどこれっぽい。
[Test Explorer doesn't support tests with `TestCaseAttribute` · Issue #1756 · ionide/ionide-vscode-fsharp](https://github.com/ionide/ionide-vscode-fsharp/issues/1756)
Subscribe して様子を見る。

FsUnit を使ったテストコード追加とリファクタをほそぼそと続けている。 [#32](https://github.com/krymtkts/pocof/pull/32), [#37](https://github.com/krymtkts/pocof/pull/37)

テストをあとから書いてると、ケアレスミスの bug ポツポツ見つかる。わたしは練習不足なのかテストファーストどうしてもできないのであとがきでもいいかと思ってるが、こう bug が多いとちょっとドキッとする。 main branch へ merge する前にあとがきテストを添えるようにして減らそう。

いま簡単なところからテストを書いてるので、本丸である `Query.fs` のところを書けたらかなりいい感じ。 `UI.fs` に関してはどないしょ...という感じになってるけど。
いま直で `Console.Write` へアクセスするような形になってるの、差し替え可能にしておいた方がいいんやろな。

また、 `psakefile.ps1` にも unit test や coverage 用のタスクを追加した。というかそれ以外のタスクも見直す必要があった。
というのも、 `src` ディレクトリ配下からビルド対象のプロジェクトを見つけるスクリプトが単一のプロジェクトしか想定してなかったので、そのまま使えなかったからだ。この度アプリとテストの 2 プロジェクトができたので、指定のプロジェクトのみ対象とするよう変更した。

まだ頻出ワードを変数にしておくとかの最適化してなかったり、タスクランナー自体小綺麗にできるんじゃないかなーというのはあるが、無闇に依存関係を増やすのもなと思って手を出せてない。
[PowerShellBuild](https://github.com/psake/PowerShellBuild), [PSDepend](https://github.com/RamblingCookieMonster/PSDepend) あたりの導入できれいになるかも知れんけど、なるべく依存関係をなくす方が結果的にシンプルなプロジェクトになるのを知ってるし。
現時点でも psake PSScriptAnalyzer Pester PowerShellGet に依存してるから、四の五の言わずに PSDepend だけでもはよ入れろよという感じはある。
だが、覚悟ができてからやる(先延ばし)。

---

### おまけ

最近になって仕事でも AI チャットを使う流れがきており、練習がてら [PowerShellAI](https://github.com/dfinke/PowerShellAI) と一緒に仕事し始めてみた。 VS Code Extension はちょっと種類が多すぎてどれが信頼できるかいちいち調べられてないし、やってない。
上手く使えて、わたし・ Tabnine ・ OpenAI の三人力な感じを出せると良いが。
ちょっと使ってみた感覚的に、仕事で使うような特定用途に特化した回答をさせたい時と普段遣いで雑に使う感じでは、 prompt に結構差がある感触。
これほんま練習になるんかな。

にしても OpenAI の token がガリガリ消費されるのを見てると「ﾋｪｴ...」となる。
