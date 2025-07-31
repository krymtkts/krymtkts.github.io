---
title: "PowerShell の profile を再構成する pt.2"
tags: ["powershell"]
---

[前に PowerShell の profile を再構成し始めたこと](/posts/2024-04-28-refactor-powershel-profile.html)に触れた。

> バグ修正の他に初期化の処理をカテゴリ毎に関数を分類してみたりしたが、これどこまでやるべきなんやろかというのは悩ましいところだ。
> profile の処理を分類したかったら結局ファイルを分けることになって、ファイルを分けると profile のディレクトリ全体を履歴管理するのが定石。
> でもそのような profile 全体を git の管理下に置くのは個人的には好きでないので、どうしたもんかな。

結局関数に小分けしたとて 1 つのファイルんい書いてることから、使わない機能があっても丸ごとスクリプトに含まれてるし、スクリプトが長くなって見にくい。
もうこれはファイル分割せざるを得ないなと諦めて、ひとまず自分で決めたカテゴリ毎に野良 `psm1` 二分割してモジュール化した。
それらを `Update-Profile` という PowerShell の profile を GitHub から Download して読み込む関数に組み、最新化できるようにした。

個人的に、多少しょぼいモジュールであっても、PowerShell Gallery に公開できるようなものについてはそちらに publish すべきだと考えている。
ニッチであれば世界のどこかの 10 人くらいは使ってくれるだろうってのは体感的に知っているし。
であるが profile に含まれるものの大半が、自分の局所的用途にしか使われないので、ここでは野良モジュールがふさわしい。

まだ若干掃除しきれてないが、大体こんな構造になってる。
[pwsh-profile/Microsoft.PowerShell_profile.ps1 at main · krymtkts/pwsh-profile](https://github.com/krymtkts/pwsh-profile/blob/main/Microsoft.PowerShell_profile.ps1#L18-L72)

```powershell
# ③profile 読み込み時に野良モジュールを読み込む
Get-ChildItem "$($PROFILE | Split-Path -Parent)/Scripts" -Recurse -File -Filter *.psm1 | Import-Module -Force

# ②野良モジュールを GitHub repo から download する
function Update-ProfileScripts {
    @(
        'Autocomplete'
        'AWS'
        'Functions'
        'Get-Hash'
        'Git'
        'Go'
        'Mod'
        'Nodejs'
        'OpenAI'
        'Pocof'
        'Psake'
        'PSResource'
        'Python'
        'StandardNotes'
        'Strings'
        'Windows'
    ) | ForEach-Object {
        $modulePath = "${_}/${_}.psm1"
        $scriptPath = "${ProfileHome}/Scripts/${modulePath}"
        if (-not (Split-Path $scriptPath -Parent | Test-Path)) {
            New-Item -ItemType Directory -Path (Split-Path $scriptPath -Parent) -Force
        }
        $params = @{
            Uri = "${baseUrl}/Scripts/${modulePath}"
            OutFile = $scriptPath
        }
        Invoke-WebRequest @params | Out-Null
    }
}

# ① PowerShell profile を GitHub repo から download する
function Update-Profile {
    $ProfileHome = ($PROFILE | Split-Path -Parent)
    $ProfilePath = "${ProfileHome}/Microsoft.PowerShell_profile.ps1"
    $baseUrl = 'https://raw.githubusercontent.com/krymtkts/pwsh-profile/main/'
    $params = @{
        Uri = "${baseUrl}/Microsoft.PowerShell_profile.ps1"
        OutFile = $ProfilePath
    }
    Invoke-WebRequest @params | Out-Null

    if (-not (Test-Path "${ProfileHome}/Microsoft.VSCode_profile.ps1")) {
        New-Item -ItemType HardLink -Path $ProfileHome -Name 'Microsoft.VSCode_profile.ps1' -Value $ProfilePath
    }
    # TODO: load the profile to prepare new psm1 files.
    . $ProfilePath

    Update-ProfileScripts

    # TODO: load the profile again to apply new psm1 files.
    . $ProfilePath
}
```

野良モジュールは `$PROFILE` と同じディレクトリの `Scripts` 配下へ配置するようにした。
野良モジュールといえど `Modules` に配置するのが定石だろうが、パッと見だと見分けつかないし自分の用途だと `Scripts` は遊んでて空いてるので。

分割したモジュールを更新するのだけちょっとトリッキーになってる。
profile が更新されたときって `Update-ProfileScripts` の中も更新されてる可能性があるが、それは最新の profile を download するまでわからない。
追加されたばかりの野良モジュールは、最新の profile を読み込んだ後の `Update-ProfileScripts` が実行されて初めて取得できる。
どうにも解決法がわからなかったので、ここは力技で `Update-ProfileScripts` 前に一度 profile を読み込んでいる。

最初に `Update-Profile` を実行して profile を download する(①)
そして最新化された `Update-ProfileScripts` を実行して最新の野良モジュールを download (②)したら、最後に再度 profile を読み込めば最新化が完了する(③)。

とはいえなんかもっといい方法ないのかという気もする。
あと、今見たら `Update-ProfileScripts` が `Update-Profile` で定義した global 変数を参照して dynamic scope もどきの挙動してるから危ない気もするけど、一旦このままかな。

この profile を細かく野良モジュールに分解するスタイルは最初小さくいくつかのモジュールから導入した。
いまではほぼ profile の更新機能以外は野良モジュールに分割した。
まだ使ってみて 1,2 週くらい？だが、モジュール分割さえ済んでしまえば GistPad から更新も容易だし不都合はなさそう。
(モジュール分割は GistPad でやるには面倒なので repository を clone してやることが多い)

今は野良モジュールとして腐らせてるけど、今後いい感じのやつが育って熟成したら正規のモジュールとして publish してやるのもありか。
