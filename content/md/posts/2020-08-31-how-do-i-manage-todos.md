{:title "TODO の管理どないしてまっか？"
:layout :post
:tags ["todo", "vscode"]}

最近、TODO 管理のやり方を変えたので記しておく。

いろんなタスク管理術がある。昔は Remember the Milk 使ってみたり、Evernote に TODO リスト書いたり。最近では Google Tasks 使ってみたり。へーしゃ内では Getting Things Done が優勢なんかな。
とはいえ結局の所いずれの手段もわたしにはうまく使いこなせず、結局日報的なものに落ちつていた、もう 7 年以上？日報のようなお仕事日記をつけている。

ところが、そのスタイルに狂いが生じだしたのが、COVID-19 流行による出社自粛 →WFH を始めてからだ。

WFH になってかなり仕事とプライベートの距離感が急接近。結果どういうわけか仕事と私生活の両方において、タスクの消化をそれまでの様にできない状態になりつつあった。7,8 割くらいのパフォが出たらいい感じ。実際はもっと悪い。

思うに、往復 2 時間の通勤時間でコンテキストスイッチを緩やかに行っていたり、処理すべきタスクの組み換えとかを行っていたのだろう。歩く時間も 30 分はあったし。
スキマ時間がなくなったことで、ギアが上がりきらないままタスク消化を急いで空回りでもしてたのだろうか。
もともと仕事の日記にはプライベートのタスクを書かずに続けていたのだけど、とりあえずコンテキストスイッチを抑えるために、仕事と私生活のタスクを一括管理しようと考えた。とはいえお仕事日記は自身の振り返りのためにも使っていて、あまり私生活のノイジー？な内容を書いていくのもどうかなと。
そんな時ちょうど Chrome のサジェスト記事で [todotxt-mode - Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=davraamides.todotxt-mode) の存在を知ったので、[todo.txt](https://github.com/todotxt/todo.txt) で、単純にタスクの管理だけを一括管理することにした。

[todo.txt](https://github.com/todotxt/todo.txt)はシンプルにフォーマットが決まっているのがありがたい。また詳細を note に切り出せるのでタスク自体をシンプルに保てるのが気に入っている。

複数の端末でこの「TODO を記したプレーンテキスト」を同期する必要があるが、これについては現状 [Google Drive™ for VSCode - Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=GustavoASC.google-drive-vscode) を使っている。
Upload したファイルが上書きじゃなく、都度新しいファイルになるのがかなり気に食わないけど、生のテキストファイルが使えることを優先してのチョイスだ。

今の所はゆるく todo.txt のフォーマットの一部(priority, context, due ぐらいしか使えてない)だけ運用している。
数年来のタスク管理を変える機会なので慣れないことも多いが、折角の変化の機会なので楽しんで模索してみたい。
