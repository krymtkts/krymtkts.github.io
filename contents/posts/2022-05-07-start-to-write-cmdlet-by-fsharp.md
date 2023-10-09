---
title: "F#で PowerShell コマンドレットを書き始めた"
tags: ["fsharp","powershell"]
---

勉強がてら、F# で PowerShell のコマンドレットを書きはじめる。
↓ を参考にやる。ただし .NET 6.0 を対象にする。

[Writing a PowerShell Core Module With F#, A Complete Guide | Brianary](https://webcoder.info/fspsmodule.html)

初めての要素が多いので、ゆっくり進める。

- F# でアプリ書くのが初
- コマンドレットを書くのが初
- dotnet CLI を使った開発が初
- その他 PowerShell 系のツール([Pester](https://github.com/pester/Pester),[platyPS](https://github.com/PowerShell/platyPS))をちゃんと使うのが初
  - 参考にする記事にはないが、ビルドスクリプトも [psake](https://github.com/psake/psake) で書く

題材としては、[poco](https://github.com/jasonmarcher/poco) の再実装をしてみるつもり。

諸々のツールが出てくるので一覧しておく。

- GitHub の Repo の作成に [PowerShellForGitHub](https://github.com/microsoft/PowerShellForGitHub)
- remote repo の管理に [ghq](https://github.com/x-motemen/ghq)
- コミット署名のために [Gpg4win](https://community.chocolatey.org/packages/Gpg4win)

### repo とプロジェクトを作成する

動くものになるまでは private repo で運用する。動かないなら公開しても意味がないからね。

```powershell
# private repo を作成する。
$owner = 'krymtkts'
$module = 'pocof'
New-GitHubRepository -RepositoryName 'pocof' -Private

# remote repo を clone する。
ghq get -p (Get-GitHubRepository -OwnerName $owner -RepositoryName $module | Select-Object -ExpandProperty ssh_url)
cd "$(ghq root)/$(ghq list $module)"

# initial commit を刻む。
gpg-connect-agent reloadagent /bye # <- gpg-agent が立ち上がってこないので先に起こす。
git commit --allow-empty -m 'Initial commit.'
dotnet new sln

# 空のプロジェクトを作成。
dotnet new classlib --language 'F#' --framework net6.0 -o src/$module
dotnet sln add src/$module/$module.fsproj
cd src/$module
# PowerShell 開発の依存関係を追加。
dotnet add package PowerShellStandard.Library
New-ModuleManifest "$module.psd1"
'<helpItems schema="maml" xmlns="http://msh" />' | Set-Content "$module.dll-Help.xml" -Encoding utf8
```

これで repo と空の F# プロジェクト `pocof.fsproj` ができた。
次に、記事に記載の通り、 `pocof.fsproj` に必要な情報を加筆する。

- `PropertyGroup` に `Version` 属性を追加
- `ItemGroup` 属性を追加して、コピーするファイルを記載する

また PowerShell のモジュール情報を `pocof.psd1` 書き込む。
コマンドレットなので、 `RootModule` を書き忘れるとコマンドがインポートできないのでご注意(書き忘れてて `Import-Module` しても使えない！となった)。

あと作成後に気づいたのだが、 作成された `*.sln` 等のエンコーディングが UTF8 with BOM だったり改行が CRLF だったりするので、それらを UTF8 と LF に手で補正した。

最後に 空の XML-based help file を作成しているが、これは `Get-Help` で使うヘルプファイルで、後で PlatyPS で上書きされるやつ。
[Naming Help Files - PowerShell | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/scripting/developer/help/naming-help-files?view=powershell-7.2)

### F# 開発環境を準備する

わたしは VS Code を使っているので、F# 用の拡張機能を入れる。

- [Ionide for F# - Visual Studio Marketplace](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp)

F# でフォーマッタといえば `fantomas` のようなのだけど、個別に入れなくても Ionide がうまく拾えるらしい。 [Formatting Settings · Issue #1346 · ionide/ionide-vscode-fsharp](https://github.com/ionide/ionide-vscode-fsharp/issues/1346)

### コードを編集する

自動生成されたコードを編集する。
記事に記載のものだとパラメータに `Position` がなくて Pester こけそうなのと、自分のやりたいことに合わせた引数を書くなど諸々の調整をする。

Pester のテストコードも作成する。テストコードは `tests` ディレクトリ配下に配置した。

### ビルド＆テスト実行

記事に記載の通りだと typo があったり `Import-Module` 前に削除があって消えてしまう等ある。
編集して実行した。

```powershell
$ModuleName = Resolve-Path ./src/* | Split-Path -Leaf
Import-LocalizedData -BindingVariable module -BaseDirectory (Resolve-Path ./src/*) -FileName "$ModuleName.psd1"
if ($module.ModuleVersion -ne (Resolve-Path ./src/*/*.fsproj | Select-Xml '//Version/text()').Node.Value) {
    throw 'Module manifest (.psd1) version does not match project (.fsproj) version.'
}
dotnet publish
cp (Resolve-Path ./src/*/bin/Debug/*/publish/FSharp.Core.dll) (Resolve-Path ./src/*/bin/Debug/*/) -Verbose

# import して Pester 実行。
# モジュールのバージョンであったり export される Cmdlet が正しいかなど見る。
Import-Module (Resolve-Path ./src/*/bin/Debug/*/publish/*.psd1) -Force
Invoke-Pester
```

あとこれらを `psake` で実行できるように `psakefile.ps1` に記載した。

F# のプロジェクトだったら [FAKE](https://github.com/fsprojects/FAKE) を使った方がいいのかなーと考えていた。となると、依存する [Paket](https://fsprojects.github.io/Paket/) も入れないといけない。いま `dotnet` CLI で管理してたのでいらないかなーと思った。
また、今回作りたいのは PowerShell のコマンドレットなので、 周辺のツールも PowerShell だ。これをいちいち `fake.cmd` や `dotnet fake build`を介して実行するのなんかダルいな...と億劫だったので、 FAKE の導入は見送ることにした。

### ドキュメントを生成する

記事では [platyPS](https://github.com/PowerShell/platyPS) を使っているのでそれに従う。

```powershell
# 事前にビルドしたモジュールを Import-Module しておく。
New-MarkdownHelp -Module pocof -OutputFolder ./docs -ErrorAction SilentlyContinue
# dll-Help.xml を作るときに実行する。
New-ExternalHelp docs -OutputPath (Resolve-Path ./src/*/) -Force
```

コメントに記した通り、他のタスクと依存関係にある。こういうのは `psakefile.ps1` にまとめる。

ヘルプの作成元は Markdown をオリジナルとする方針。

### PowerShell Gallery へのリリース

まだやらないけど手順だけ確立しておく。

記事に記載のものだと、ビルドコンフィグレーションを Release にして Import、その後 Import したモジュールを公開、という手順になっている。

個人的にやったことがあるのはフォルダを指定して公開する方だけなので、どっちにするかなーと悩んでいる。間違って古い Import 済みモジュールを公開するミスとかないのかな、というのが疑問。

ま、勉強のためのものであるし、やったことない方を接客的に選んでみるのも一興か。コマンドレットが正しくエクスポートできているか、リリース前にテストするにもこの方法を取るのが良さそうだし。

とはいえ記事に記載の通りやると、自分が普段利用している PowerShell Module のパスに直でリリース前のモジュールを打ち込んでしまう。
これはちょっと実用始めたら困りそうなので、ディレクトリを変える必要がある。
となると結局いつものパス指定でのリリース方式でええんちゃうか...

いずれを選択するかはまた検討したい。

また、これらのリリースタスクも `psakefile.ps1` に定義するものとする。

### おわりに

これでひとまず準備完了。
ほぼ記事に記載のとおりやってきたけど、ちょいちょい自分用に変えたり、新しい要素については色々調べながらやってるので、進捗は亀のスピードだった。
動くものをこしらえれたら public repo にしよう。

現時点で不足しているとわかっている点もある。
今回書きたいコマンドレットはインタラクティブなものなので、きっと Pester でテストできない点が出てくる。
それらは [FsUnit](https://fsprojects.github.io/FsUnit/) を使って可能な限りテストを書きたい所存。

あと困っているのが、参考にしている記事にも記載があったが、 DLL への参照が切れなくてファイルを消したりできなくなる(`Remove-Module` 忘れとかでなく)。いまは都度 PowerShell のセッションを作り直してるけど相当に面倒なので、原因を突き止めてどうにかしたいな。

未経験のものに触れるのは普段得られない刺激があって良い。
