{:title "シェルスクリプトのフォーマッタをCLIでインストールする"
 :layout :post
 :tags  ["sh", "golang"]}

小ネタ。

へーしゃの仕事では、Bashで書かれたスクリプトの出番がめちゃくちゃ多い。

Bashが得意じゃないマンのわたしにとっては、そういったスクリプトを読み書きに慣れてないこともあり、結構疲れる仕事である。

そこでせめて書くのだけは少しでも楽したいと思って、フォーマッタを導入しようと思った。

### ツールを導入する

shfmtというgolang製のツールがあるので、それを採択。

[mvdan/sh: A shell parser, formatter, and interpreter (POSIX/Bash/mksh)](https://github.com/mvdan/sh)

しかし悲しいかな、Windows用に提供されてるパッケージはScoopのみ(リンク切れてるけど)でChocolateyにない様子🤔(筆者はChocolateyユーザである)。

代わりにCLIでのインストールを使うことにする。

```sh
cd $(mktemp -d); go mod init tmp; go get mvdan.cc/sh/v3/cmd/shfmt
```

でも`mktemp`コマンドはPowerShellにはないし...そこは`mkdir`で茶を濁す。あとtmpフォルダの後始末もする。

```powershell
cd $(mkdir tmp); go mod init tmp; go get mvdan.cc/sh/v3/cmd/shfmt; cd ../; rm -r tmp
```

ええがな。

### 雑記

5月後半はうまく波に乗れずブログを書けなかった。まだアウトプットが習慣化していないようなので、きちんと積み重ねしていきたいもんやで🤔
