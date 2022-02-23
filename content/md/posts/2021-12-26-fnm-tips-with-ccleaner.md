{:title "fnm でインストールした処理系は Temp フォルダからシンボリックリンクされるので ccleaner で消える話(当然)"
:layout :post
:tags ["nodejs","fnm"]}

最近、 Node.js 処理系のバージョン管理に fnm を使っている。

[Schniz/fnm: 🚀 Fast and simple Node.js version manager, built in Rust](https://github.com/Schniz/fnm)

お仕事 PC(Windows) では Yarn を使うのだが、 それまで使っていた Nodist だとどうにも Yarn が内部的に参照する npx が新しいバージョンに変わらなくてエラーが解消できなかった。

これが fnm なら何の問題もなくサクサク動く。[chocolatey](https://community.chocolatey.org/packages/fnm) でインストールできるし、`.node-version`, `.nvmrc ` をうまく使える点でも良、重宝している。

`fnm env` で使う処理系を指定した環境変数を生成＆利用したいツールに反映させる必要があるため、 VS Code なんかは terminal 経由で起動する必要があるのだけど(それしか方法を知らん)、これは元々わたしの作業スタイルだったので特に問題ない。

### 事件は突然に

ある日、怪現象に悩まされた。

何気なく VS Code を立ち上げると、 `textlint` が見つからないというエラーが出力されていた。`textlint` だけでなく、 `npm` も `node` も消え去っていたのだ。その時はさっさと作業を始めたかったので、処理系やモジュールを再インストールすることで現状復帰した。これが最も手っ取り早い。

この原因は後でからわかったのだが非常に単純な話であって、Temp フォルダの中身を再帰的に消したことで fnm がインストールした処理系全ても無に帰したのだ。

わたしは Temp フォルダやその他のゴミ掃除目的に ccleaner を長らく使っている。ccleaner は Temp フォルダの中身を一覧して消す。

`fnm` がインストールした処理系自体は `$env:FNM_DIR` に配置される。
`fnm env` を実行すると、Temp フォルダ内に `$env:FNM_DIR` 配下の特定バージョンへ向けたシンボリックリンクを作成する。そのシンボリックリンクのパスは `$env:FNM_MULTICHELL_PATH` に格納されている。

おわかりいただけただろうか。

ccleaner のように Temp フォルダを探索的に掃除するなら、以下のコマンドで事前に削除されるファイルが何処のものか知ると良い。こんなの事故るまで頭が回らんわ。

```powershell
Get-ChildItem $env:FNM_MULTISHELL_PATH/../ | Select-Object -Property LinkTarget
```

### 対策

現状、ccleaner 等の Temp フォルダを掃除するアプリで、 `fnm` のフォルダを除外するしかないか。
しかしそうなると、使われなくなった `$env:FNM_MULTISHELL_PATH` の掃除を自力でする必要が出てくる。
`fnm env` の度に処理系へのシンボリックリンクが作成されるので、掃除はこまめに行う必要がある(だからこそ Temp フォルダに作成しているのだろうけど)。

`fnm` 自体には手動でこのシンボリックリンクを消すコマンドもないみたいなので、とりあえずは PowerShell の Profile で古いやつを消す様にするのが妥当なラインかな。

```powershell
# てきとーにこんなのを想像
Get-ChildItem $env:FNM_MULTISHELL_PATH/../ | Where-Object -Property CreationTime -LE (Get-Date).AddDays(-1) | Remove-Item
```
