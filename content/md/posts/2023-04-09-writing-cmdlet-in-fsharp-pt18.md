{:title "F#でコマンドレットを書いてる pt.18"
:layout :post
:tags ["fsharp","powershell","github"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

`PocofQuery.run` のテストをそれなりに足した。
多少テストが増えてきたので CI が欲しくなってきた。ここで以前立てた Issue [#34](https://github.com/krymtkts/pocof/issues/34) を回収しておくとことにした。

Issue にも書いてたけど、基本 GitHub Docs に書いてたやつをなぞるだけ。

- [Building and testing .NET - GitHub Docs](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-net)
- [Building and testing PowerShell - GitHub Docs](https://docs.github.com/en/actions/automating-builds-and-tests/building-and-testing-powershell)

runner image は今の開発環境に合わせて `windows-latest` にしておく。 pocof 自体は今後他の OS でも動くことを保証してみたいけど、最初なのでシンプルにいく。
この記事を書いた時点の `windows-latest` に含まれるツール類は以下を参照した。

[runner-images/Windows2022-Readme.md at main · actions/runner-images · GitHub](https://github.com/actions/runner-images/blob/main/images/win/Windows2022-Readme.md)

`dotnet` のセットアップは必須のよう。 pocof はまだ 6 なのでそれを使う。

PowerShell はデフォで 7.2.10 が入ってるらしい。いろんなバージョンで動くことをチェックするのであれば PowerShell の version で `matrix` 組むのが妥当だが、これも最初なので簡単に。

pocof のテストに必要な PowerShell モジュール Pester, PSScriptAnalyzer あたりも入ってるが、肝心の [psake](https://github.com/psake/psake) が入ってない。ならば workflow で全部ひっくるめで force install するようにしておく。
このへんも [PSDepend](https://github.com/RamblingCookieMonster/PSDepend) を使うようにしておけば YAML にかき分ける必要もないし良さげか。

ここまでやれば `psakefile.ps1` に書いた全テストタスクを実行すれば完了する。キャッシュあった方が速いだろうけど、そもそもそんなに時間かからんので対応しないでおく。

あと一番重要な GitHub Actions の YAML のファイル名と名前の部分、結構いろんな repo みても付け方がバラバラでイケてるパクリ元がない。
ここは GitHub の Pull Request や Actions の画面で見られることを考えてない名称は避けたかった。
最終的に、 [FSharp.Formatting](https://github.com/fsprojects/FSharp.Formatting/tree/3fdd5b9a186e35798d87ceee4bee692374304bed/.github/workflows) の命名規則が気に入ったのでそれを模した。
ファイル名は `pull-requests.yml` を短縮して `pr.yml` で、名前は `Build and Test on Pull Request` だ。これなら GitHub の画面で見ても自然だ。

出来上がった素朴な YAML はこちら。 [pocof/pr.yml](https://github.com/krymtkts/pocof/blob/1dda928ff1ae56938a26ae70568a9a05807f0852/.github/workflows/pr.yml)

```yaml
name: Build and Test on Pull Request

on:
  pull_request:
    branches: ["main"]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 6.0.x
      - name: Install modules from PSGallery
        shell: pwsh
        run: |
          Set-PSRepository PSGallery -InstallationPolicy Trusted
          Install-Module Psake,Pester,PSScriptAnalyzer -Scope CurrentUser -Force
      - name: Execute All Tests
        shell: pwsh
        run: Invoke-Psake -taskList TestAll
```

GitHub Actions の難点としてテストしにくい点があるが、[nektos/act](https://github.com/nektos/act) を使えば `ubuntu-*` の runner に限ればテストできる。
仕事では act を使ってるが自機には積んでなかったんで、この際に Chocolaty で入れた。

```dos
choco install act-cli
```

ではテストとばかりに `windows-latest` を `ubuntu-latest` に変えて実行する。

```powershell
PS> act pull_request --verbose --platform windows-latest=catthehacker/ubuntu:act-latest
# ...省略
| OCI runtime exec failed: exec failed: unable to start container process: exec: "pwsh": executable file not found in $PATH: unknown
# ...省略
Error: Job 'test' failed
```

なん...だと...
`pwsh` コマンドが無いと言われた... `act` のセットアップ時に MEDIUM 選んだらどうも入ってないっぽい。
流石は minority の PowerShell 、参ったね。

しかし `act` が使うイメージを見てたら `pwsh` というキーワード付きの image があるじゃないの。 [catthehacker/docker_images: Docker images](https://github.com/catthehacker/docker_images#images-available)

> /linux/ubuntu/pwsh - ghcr.io/catthehacker/ubuntu:act-\* but with pwsh tools and modules installed

こりゃきたな。試す。

```powershell
PS> act pull_request --verbose --platform windows-latest=ghcr.io/catthehacker/ubuntu:pwsh-latest
# ...省略
[Build and Test on Pull Request/test] 🏁  Job succeeded
```

おーいけた。 6 分超かかったけど。
これでなんとか PowerShell の場合でもローカルテストできそう。
Docker Hub に同じイメージ [catthehacker/ubuntu:pwsh-latest](https://hub.docker.com/layers/catthehacker/ubuntu/pwsh-latest/images/sha256-3b4b83b4458dd875509bc74b9f1d4ecaa57101c65ec529cb463fed2d859228e4?context=explore) あったので、これを使っていこう。

```powershell
act pull_request --verbose --platform windows-latest=catthehacker/ubuntu:pwsh-latest
```

...でもいちいちこのクソながオプション書きたく無いなあ...設定ファイルとかはなさそう。関数にするか psake のタスクにするしかないか。なんか大げさな気がするけど。

そんなこんなで [#45](https://github.com/krymtkts/pocof/pull/45) を作れた。
matrix はまだ設定してないけど、少なくともテスト類は ubuntu でも動くことわかったし、追々対応していく。

---

おまけ。

DockerDesktop でログインしてると、 `act` 実行中にある DockerHub から `catthehacker/ubuntu:act-latest` を pull するのができなかった。ログアウトしてるといける。↓ このへんの関係ぽいが深く追ってない。

[m1: act fails to pull with unauthorized: incorrect username or password · nektos/act · Discussion #1165](https://github.com/nektos/act/discussions/1165)

あとなんか知らんが `act` 実行後に `[45;3R` って謎キーワードが terminal の input に出てくる、必ず。 ansi escape sequences ぽいけどよくわからん。
