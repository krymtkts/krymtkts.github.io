---
title: "F# でコマンドレットを書いてる pt.29"
tags: ["fsharp", "powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

2023 年末に [pocof version 0.7](https://www.powershellgallery.com/packages/pocof/0.7.0) をリリースした。

その流れというか冬休みの宿題的に、 2024 年初は積んでた TODO から「長いクエリの場合は描画範囲に収まるよう部分表示する機能」を作ってみた。 [#105](https://github.com/krymtkts/pocof/pull/105)
言うなれば、クエリ入力がテキストボックスに長ーいテキストを入れたときと同じ挙動になる機能なのだけど、 query window と名付けてひとまず実装してみた。
まだ [`PSHostRawUserInterface.LengthInBufferCells`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.host.pshostrawuserinterface.lengthinbuffercells?view=powershellsdk-7.4.0) を使ってないから full-width character の場合に表示が崩れるのだけど、いい感じぽい。

[#105](https://github.com/krymtkts/pocof/pull/105) でざっくり query window を実装したあと、コードを小綺麗にすべく [#109](https://github.com/krymtkts/pocof/pull/109) でかなり書き換えた。
他にも諸々のコミットを積んだ。
それもあって詳細な変更に関してまとめることはダルくてできないが、備忘のために要所をまとめておく。

---

query window の実現のために最低限必要と判断した要素は以下で、いくつかは新しく追加する必要があった。

- クエリ文字列の全長
  - クエリの長さを取るだけ
- クエリ文字列中のカーソル位置
  - 従来のコンソール上の X 座標を拡張する
- コンソールの幅
  - 持ってなかったから追加する
- クエリ文字列の描画開始インデックス
  - 持ってなかったから追加する

描画域は、コンソールの幅からクエリの状態だとかのシステム情報表示の幅を減算して求める必要がある。

クエリ文字列の描画開始インデックスは、クエリ文字列の全長から描画域を引いて求める。なのでクエリ文字列の全長が描画行の幅を超えない限りは 0 のままだ。

クエリ文字列のうち描画するのが、描画開始インデックスから描画域の幅に収まる文字数だけとなる。

画面上の X 座標は、クエリ文字列中のカーソル位置からクエリ文字列の描画開始インデックスを引いて求める。

描画域はウィンドウサイズを変更したときのことを考えたらイベントで更新されるようにするのがベストだろうけど、今の pocof ではそのような機構がないので、キー入力後に幅を更新するようにした。

描画域の変更が必要なのは、ウィンドウサイズの変更以外にもある。
それはクエリの一致方法やら case sensitive にしたりとかの条件を切り替えたとか、クエリを打ち込んで絞り込まれた件数の桁が変わったとか、多岐に渡る。
ほぼ全ての入力操作でそれが起こるので、こまめに描画域の調整をする必要があった。

そんな感じに玉突きで状態の変更が発生しまくるので、レコードを書き換えて新しいレコードにするコードが大量発生した。

なのでシンプルに実装したとはとても言えない感じがするけど、ややこしいレコード操作をまとめるためにこまめに module 分けしたので、あとから自分で見てもそこそこ困らなくなったはず。

---

あと諸々のリファクタリングのついでに、計測していたコードカバレッジを [Codecov](https://about.codecov.io/) へ登録するようにしてみた。 [#111](https://github.com/krymtkts/pocof/pull/111)

[codecov/codecov-action](https://github.com/codecov/codecov-action) で cobertura の XML を送りつけるだけでいいので楽だ。
ただし composite action の中から secrets にアクセスできないため、以下のように composite action の input にしてやる必要だけあった。

```diff
index 01c3b6d..e75a80e 100644
--- a/.github/actions/test/action.yml
+++ b/.github/actions/test/action.yml
@@ -1,6 +1,11 @@
 name: Test
 description: Setup .NET, install PowerShell modules and run all tests.

+inputs:
+  codecov_token:
+    description: "Codecov token"
+    required: true
+
 runs:
   using: composite
   steps:
@@ -16,3 +21,9 @@ runs:
     - name: Execute All Tests
       shell: pwsh
       run: Invoke-Psake -taskList TestAll
+    - name: Upload coverage reports to Codecov
+      uses: codecov/codecov-action@v3
+      with:
+        file: ./src/pocof.Test/TestResults/coverage.cobertura.xml
+      env:
+        CODECOV_TOKEN: ${{ inputs.codecov_token }}
diff --git a/.github/workflows/main.yml b/.github/workflows/main.yml
index be56145..06f2b91 100644
--- a/.github/workflows/main.yml
+++ b/.github/workflows/main.yml
@@ -22,3 +22,5 @@ jobs:
         uses: actions/checkout@v4
       - name: Test
         uses: ./.github/actions/test
+        with:
+          codecov_token: ${{ secrets.CODECOV_TOKEN }}
```

---

他にも勉強がてら [Snyk](https://snyk.io/) に登録して脆弱性を検査するようにしてみた。 [#112](https://github.com/krymtkts/pocof/pull/112)
Codecov のような送りつけるだけに比べると、思ったよりも Snyk はサービスの理解が難しく感じた。

めちゃくちゃ細かいところだと、例えば対応する package manager 的には dotnet は大丈夫だが、 badge は dotnet に対応してないとか。

- [Badge Support for Repositories – Support Portal | Snyk](https://support.snyk.io/hc/en-us/articles/360003997277-Badge-Support-for-Repositories)
- [Unable to test GitHub repository-The repository doesn't exist, or does not contain a relevant manifest file. · Issue #489 · snyk/cli](https://github.com/snyk/cli/issues/489)

まず [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の方を実験台にしてたので、 dotnet と Node.js の複合プロジェクトだったから気づかなかった。

あと言語別に分かれた [snyk/actions](https://github.com/snyk/actions) がなにやってるかもはじめは分からなかった。
snyk/actions は、どの言語の action も単に言語の環境を準備してから Snyk CLI を実行するだけ。それがわかったら、あとは Snyk CLI のドキュメントを見るだけなのでやりやすかった。
この仕組みは [snyk/actions/dotnet/action.yml](https://github.com/snyk/actions/blob/3e2680e8df93a24b52d119b1305fb7cedc60ceae/dotnet/action.yml) を見て理解した。

特に blog-fable のような複数 package manager で構成される project なら [`--all-projects`](https://docs.snyk.io/snyk-cli/commands/test#all-projects) と [`--exclude`](https://docs.snyk.io/snyk-cli/commands/test#exclude-less-than-name-greater-than-less-than-name-greater-than-...greater-than) を使うのが良さそう。
pocof でも `--all-projects` を使った。
pocof は Linux での動作確認用の `Dockerfile` を置いてたのもあって、意外に検査対象があったんも勉強になった。

Snyk は GitHub の code scanning に upload する SARIF ファイルを [`--sarif-file-output`](https://docs.snyk.io/snyk-cli/commands/test#sarif-file-output-less-than-output_file_path-greater-than) で出力できる。
main branch への push でアップロードするようにしてみた。

ほぼ Snyk の文書通りのコピーで良いが、 `security-events: write` の permission を足さないといけない。それは GitHub 側の文書でわかった。めんど。

- [GitHub Actions for Snyk setup and checking for vulnerabilities - Snyk User Docs](https://docs.snyk.io/integrate-with-snyk/snyk-ci-cd-integrations/github-actions-for-snyk-setup-and-checking-for-vulnerabilities#github-code-scanning-support)
- [Uploading a SARIF file to GitHub - GitHub Enterprise Cloud Docs](https://docs.github.com/en/enterprise-cloud@latest/code-security/code-scanning/integrating-with-code-scanning/uploading-a-sarif-file-to-github#example-workflow-for-sarif-files-generated-outside-of-a-repository)

pull request 時に脆弱性があったら snyk/actions はエラーにしたい。
でも main branch の push で SARIF ファイルをアップロードする方がエラーになっては困る。
なのでそういったケースでは別々に定義するのが良さそう。
ココまでで Snyk の面倒さが伝わるはずだ。

あとコレは Snyk に関係ないけど xunit が依存してる `NetStandard.Library` 1.6 の依存関係にある脆弱性のおかげで High な脆弱性が検知される。

[xunit.runner.utility is still referencing NetStandard.Library 1.6.0 · Issue #2778 · xunit/xunit](https://github.com/xunit/xunit/issues/2778)

これを避けるには、 xunit を含む test project の検査をしないか、脆弱性を報告されている package の最新版への依存を追加するかの、どちらかしかない。
検査しないのもどうかなーと思ったので後者で対処した。

結果は以下の通りになった。
(コメントで snyk を synk に typo してるのは後で直す)

```diff
 .github/workflows/main.yml | 16 ++++++++++++++++
 1 file changed, 16 insertions(+)

diff --git a/.github/workflows/main.yml b/.github/workflows/main.yml
index 06f2b91..deed645 100644
--- a/.github/workflows/main.yml
+++ b/.github/workflows/main.yml
@@ -7,6 +7,7 @@ on:

 permissions:
   contents: read
+  security-events: write

 jobs:
   test:
@@ -24,3 +25,18 @@ jobs:
         uses: ./.github/actions/test
         with:
           codecov_token: ${{ secrets.CODECOV_TOKEN }}
+      - name: Run Snyk to check for vulnerabilities
+        uses: snyk/actions/dotnet@master
+        # synk/actions uses Container action that is only supported on Linux.
+        if: runner.os == 'Linux'
+        continue-on-error: true # To make sure that SARIF upload gets called
+        env:
+          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
+        with:
+          command: test
+          args: --all-projects --sarif-file-output=snyk.sarif
+      - name: Upload result to GitHub Code Scanning
+        if: runner.os == 'Linux'
+        uses: github/codeql-action/upload-sarif@v2
+        with:
+          sarif_file: snyk.sarif
 .github/workflows/pr.yml | 9 +++++++++
 1 file changed, 9 insertions(+)

diff --git a/.github/workflows/pr.yml b/.github/workflows/pr.yml
index 8fe46f9..e3305a1 100644
--- a/.github/workflows/pr.yml
+++ b/.github/workflows/pr.yml
@@ -23,3 +23,12 @@ jobs:
         uses: ./.github/actions/test
         with:
           codecov_token: ${{ secrets.CODECOV_TOKEN }}
+      - name: Run Snyk to check for vulnerabilities
+        # synk/actions uses Container action that is only supported on Linux.
+        if: runner.os == 'Linux'
+        uses: snyk/actions/dotnet@master
+        env:
+          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
+        with:
+          command: test
+          args: --all-projects
 src/pocof.Test/pocof.Test.fsproj | 3 +++
 1 file changed, 3 insertions(+)

diff --git a/src/pocof.Test/pocof.Test.fsproj b/src/pocof.Test/pocof.Test.fsproj
index 8b56bb8..d7316ae 100644
--- a/src/pocof.Test/pocof.Test.fsproj
+++ b/src/pocof.Test/pocof.Test.fsproj
@@ -31,6 +31,9 @@
     </PackageReference>
     <!-- NOTE: for creating mock PSObject -->
     <PackageReference Include="Microsoft.PowerShell.SDK" Version="7.4.0" />
+    <!-- NOTE: NOTE: for suppressing vulnerability report the transient dependencies from xunit that depends .NET Standard 1.6. -->
+    <PackageReference Include="System.Net.Http" Version="4.3.4" />
+    <PackageReference Include="System.Text.RegularExpressions" Version="4.3.1" />
   </ItemGroup>
```

---

Codecov と Snyk を使うことで 3rd party の token を secrets に保存して使う様になった。
わたしは全然知らなかったが、 `pull_request` イベントだと Dependabot は secrets へアクセスできないらしい。この Dependabot が secrets を読めなくなった変更は結構なインパクトあったぽいな。

[Dependabot triggered Actions cant access secrets or use a writable token · Issue #3253 · dependabot/dependabot-core](https://github.com/dependabot/dependabot-core/issues/3253)

pocof のケースだと Dependabot secrets に secrets と同じ内容を定義するだけで済んだ。
この Dependabot が作成した PR で Dependabot secrets にアクセスできるのは文書に書いてないぽい。

[Configuring access to private registries for Dependabot - GitHub Docs](https://docs.github.com/en/code-security/dependabot/working-with-dependabot/configuring-access-to-private-registries-for-dependabot)

唯一？この文書がそれっぽいか。でも `dependabot.yml` で使えるとは書いてるけど GitHub Actions で使えるとは書いてない。
ただ Dependabot secrets のところには以下のような記載があり、使えそうな雰囲気に見える。いつ GitHub の気が変わって使えなくなるかわからないけど。

> Dependabot secrets
> Secrets are credentials that are encrypted. Anyone with collaborator access to this repository can use these secrets for Dependabot.
>
> Secrets are not passed to forks.

こういう面倒なところを忌避して [Renovate](https://github.com/apps/renovate) を使う勢があるというのも理解した。

---

なんか色々書いたが、冬休みに色々仕込んだものを早く 0.8 release にしたいところ。
`PSHostRawUserInterface.LengthInBufferCells` を使って full-width character の表示に対応したら、 release するようにしようか。
