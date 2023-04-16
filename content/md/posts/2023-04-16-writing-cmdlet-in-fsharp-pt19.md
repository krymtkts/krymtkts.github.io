{:title "F#でコマンドレットを書いてる pt.19"
:layout :post
:tags ["fsharp","powershell"]}

本当は Fable のメモを記録したかった。 Fable の練習を始めようと思って [Fable · Start a new project](https://fable.io/docs/2-steps/your-first-fable-project.html) を参考にプロジェクトを作成(以下)した。

```powershell
dotnet new --install Fable.template
```

その後作成されたプロジェクトを VS Code で開いたら OOM で VS Code がお亡くなりになったので、また今度やるか...という気持ちに切り替わってしまった。

---

閑話休題。

また [krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

もう 2023-04 も半ばを過ぎ、前回の pocof 0.4.0-alpha から 2 ヶ月経とうとしてるが、粛々とテスト追加と書き直しをしている。

以前 GitHub Actions の Workflow を作成した時に、 `ubuntu_latest` つまり非 Windows でも [Pester](https://github.com/pester/Pester) のテストが問題なく緑になるんだなというのがわかった。
これを機に OS の build matrix を組んで Windows, Linux, Mac の 3 つのプラットフォームのテストを有効にしてみた。 [#48](https://github.com/krymtkts/pocof/pull/48)

すべてのプラットフォームでテストが成功したのを見て、 .NET 良く出来てるなあと思った(小並感)。
とはいえ実際に PowerShell on Ubuntu でインタラクティブな動作テストしたわけじゃないし、 .NET はキー周りが大変だと聞く。
わたしが持っているこの辺の知識はこれしかないけど →
[Console.ReadKey improvements in .NET 7 - .NET Blog](https://devblogs.microsoft.com/dotnet/console-readkey-improvements-in-net-7/)

なので、いつか時間を取って人力で確認しないといけないのではと思っている。
実際問題、例えば Ubuntu に PowerShell 入れるのって相当動機づけされてないとやらなそうなのだけど、機が熟しつつある。
良いのか悪いのかは知らん。ただ面白そうではある。

pocof の機能追加はやってなくて、 CLI の UI をいじるところ以外はテストも揃ってきたので、リファクタリングと称して主に構造変換をそれほど伴わない書き直しをしている。 [#49](https://github.com/krymtkts/pocof/pull/49)

pocof は PowerShell で書かれた [poco](https://github.com/jasonmarcher/poco) を F# で写経するところから始まっているので、あまり F# ぽくない感じの箇所が多々ある。その環境の中にコードを足していってるので周りに引きづられて F# ぽくない部分も多かった。

それらは例えば `string` じゃなくて `ToString` メソッドを使ってるとか、一番気になってたのは conditional expression の `if` をいっぱい使ってた点。
別に `if` の方が短く済むときもあるし使っても構わんのだけど、式としてじゃなくて制御構文として使ったときの `else` が必須じゃないのが気になってた。また match expression と違って網羅できてるか人目でわからないのが一番心配で、結局これらを `match ... with` で書き直すことにした。
その結果、一部には active pattern も利用したりして、パッと見わかりやすくなった気がする。

`if ... then` を `match ... with` に書き換えたら以下のようになって、 `true` がなんか判然とせんなーという気は多少あるので、自前のロジックに関しては判別共用体に直すとか検討するとより良いのかも知れん。

```fsharp
// before
if nanigashi x then
    Some(x)
else
    None

// after
match nanigashi x with
| true -> Some(x) // この true ってのもなーんか気に食わん
| _ ->  None
```

こういうところにも active pattern を積極的に使っていったら良いのかもやけど、今回はまだ使わなかった。
データの変換・分解を伴うようなパターンだとバッチリハマった感じだったが、単純なケースに使うと記述量だけ増えて表現力変わらないなとい感触だったため(書き方がまずいのか)。
とはいえ積極的に使いたいものではあった。

コードの見通しも良くなってきて、そろそろ積み残しタスクにも着手しやすくなってきた感じがする。次やるとしたら [#35](https://github.com/krymtkts/pocof/issues/35) あたりかな。

でもテスト追加とリファクタリングはまだやりたいところが残っていて、悩ましい。 UI のテストを入れて置きたいのと、 `PocofQuery` をシンプルにするのと、 `Library.fs` から極力ロジックを引き剥がす点だ。
なんかダルそうだなというのもあって手が出しにくいので、積み残しタスク減らしてからやると気分転換になるのかも知れん。
