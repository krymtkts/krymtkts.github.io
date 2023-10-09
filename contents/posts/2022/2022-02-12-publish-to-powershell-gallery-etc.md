---
title: "PoweShell Gallery へ公開するためのステップ覚書"
tags: ["powershell","powershellgallery"]
---

タイトルの通り。これは未来の自分へのバトンだ。

毎回 PoweShell Gallery への公開方法を忘れたりする。 [弊ブログの`powershellgallery`タグ](/tags/powershellgallery.html)を参照すればどれだけ同じミスを繰り返してるかアホさがわかる。
加えて [microsoft/PowerShellForGitHub](https://github.com/microsoft/PowerShellForGitHub) で repo を作るのもしょっちゅう忘れる。
こちらに関してはもうそろそろ [`gh`](https://github.com/cli/cli)に乗り換えた方がいいのかも知れん。(けど、そうすると PowerShell の旨味であるオブジェクトでゴニョゴニョやりやすい世界がなくなってしまうのは困る)

なのでこれらを定型化して `psake` タスクに落とし込む等したいな～と考えている(今度はその`psake`タスクが秘伝のソース化するかも知れんがそれはそれ)。
そのためにいつも何をやっているかを以下にリストアップする。

---

前提。

- `PSMFAttendance` の作成時にやったことを踏まえている
- [ghq](https://github.com/x-motemen/ghq) を使っている前提
- MS 公式文書はこちら [PowerShell Gallery Publishing Guidelines and Best Practices - PowerShell | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/scripting/gallery/concepts/publishing-guidelines?view=powershell-7.2)
  - 残念ながら過去のメモ([krymtkts/Get-GzipContent](https://github.com/krymtkts/Get-GzipContent)を公開したとき)からは URL が変わっていた。今回はどうかな。

### Step 1. repo とモジュールマニフェストの作成

まず最初のステップはモジュールの雛形作成。

```powershell
# create repo.
$owner = 'krymtkts'
$module = 'PSMFAttendance'
New-GitHubRepository -RepositoryName $module -Private -LicenseTemplate MIT
ghq get -p (Get-GitHubRepository -OwnerName krymtkts -RepositoryName $module | Select-Object -ExpandProperty ssh_url)
cd "$(ghq root)/$(ghq list $module)"

# create module manifest.
mkdir $module
$author = 'Takatoshi Kuriyama'
New-ModuleManifest -Path "./$module/$module.psd1" -ModuleVersion 1.0 -Author $author -Copyright "(c) $((get-date).Year) $author. All rights reserved."
```

PowerShell Gallery への公開コマンド [`Publish-Module` (PowerShellGet)](https://docs.microsoft.com/en-us/powershell/module/powershellget/publish-module?view=powershell-7.2) は、指定したディレクトリの中身を全部 PowerShell Gallery にぶち上げるため、モジュール以外のファイルを配置していても全て雲の上に持っていかれてしまう(除外設定がないのだ)。
そういう事故を起こさないためにも、モジュールリリース用のディレクトリを作成することをおすすめする。
そこでモジュールを開発するか、リリース対象のコードをそこにコピーした上で公開する、というのに限るのではないだろうか。わたしはコピーしたあとの掃除とか考えるのが面倒なので前者。

先にモジュールマニフェストを作るのは、動作確認なんかで関数がエクスポートできているか見るのに使うからだ。

### Step 2. モジュールマニフェストの更新

次は実装して、マニフェストの更新。
マニフェストに記すべき内容については公式のドキュメントを読むのが良い。

[Creating and publishing an item - PowerShell | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/scripting/gallery/how-to/publishing-packages/publishing-a-package?view=powershell-7.2#required-metadata-for-items-published-to-the-powershell-gallery)

とはいえわたしは大したモジュールを作らないのもあり、いつも更新するのは限られたフィールドだけだ。主に以下。

- `Description`
- `PowerShellVersion`
- `*ToExport`
- `PrivateData.PSData.Tags`
- `PrivateData.PSData.LicenseUri`
- `PrivateData.PSData.ProjectUri`

この中でわかりにくいのが、互換性のあるプラットフォームの指定。これは `Tags` で表す。
[PowerShell Gallery Publishing Guidelines and Best Practices - PowerShell | Microsoft Docs](https://docs.microsoft.com/en-us/powershell/scripting/gallery/concepts/publishing-guidelines?view=powershell-7.2#tag-your-package-with-the-compatible-pseditions-and-platforms)

マニフェストの更新は手で書いてもよいが、`Update-ModuleManifest` も使える。
ただし `New-ModuleManifest` と `Update-ModuleManifest` で出力結果のフォーマットが異なる(後者はインデントされない)のがイライラするので、手でやることが多いか。
ただ自動化していくとすれば、ここは `Update-ModuleManifest` に従うところか(こいつインデントしてくれへんのだが)。

### Last Step. PowerShell Gallery 公開

最後は PowerShell Gallery への公開だ。

PowerShell Gallery への公開を実施する前に、API キーの期限が切れていないか必ずチェックしておく。
キーの有効期限が切れている場合のエラーが非常にわかりにくいので、無駄にトラシューに時間を費やさずに済ますためにも公開前にチェック兼ねて毎回キーを更新するのが妥当では？
API キーの有効期限は最長 1 年しかないので、しょっちゅう切らしている。むしろ公開前に更新するフローであれば、期限も最短にできるのでよりセキュアかも知れない。

API キー はモジュール名に対して [glob パターン](<https://en.wikipedia.org/wiki/Glob_(programming)>)でスコープを切れる。過去には面倒を回避するために`*`パターンを使ったりしていたが、今はパッケージ名の完全一致を利用していて、パッケージ毎に API キーを分けるようにしている。

公開の手順は [krymtkts/Get-GzipContent](https://github.com/krymtkts/Get-GzipContent)を公開したときスクリプトを使う。

このスクリプトでは `PSScriptAnalyzer` を使ったチェックが成功した後に公開する。
また、`WhatIf` は Dry Run として置き換えた上で使うようにしていた。
`WhatIf` を使わないことで覚えることが増えて面倒な気もするが、とにかく間違って公開すると面倒なので、初期値を Dry Run にしたいという意図だった(多分)。この辺は [`ShouldProcess`](https://docs.microsoft.com/en-us/powershell/scripting/learn/deep-dives/everything-about-shouldprocess?view=powershell-7.2) の勉強をしたらより良い案があるかも知れん。

PowerShell Gallery でのバージョニングは基本的に Semantic Versioning なので、何かしらミスったらモジュールの非公開はできるけど、同じバージョンへの更新はできない。パッチバージョンを上げて再公開とかしかできない。これはミスったら恥ずかしいしやり直しがきかないから、 Dry Run しまくる。この「何かしらミスったら」を Pester とかで事前チェックできると良いのだろうけど。

```powershell
Param (
    [String]$ApiKey,
    [ValidateSet('Publish', 'DryRun')]$Mode = 'DryRun'
)

$ModuleName = Get-ChildItem -File -Path ./ -Recurse -Name '*.psd1' | Split-Path -Parent
$ArtifactPath = ".\$ModuleName\"
Write-Host "Check modules under $ArtifactPath."

$report = Invoke-ScriptAnalyzer -Path "$ArtifactPath" -Recurse -Settings PSGallery
if ($report) {
    Write-Host "Violation found."
    $report
    exit
}
Write-Host "Check passed."

switch ($Mode) {
    'Publish' {
        Write-Host "Publishing module: $ModuleName"
        Publish-Module -Path $ArtifactPath -NugetAPIKey $ApiKey -Verbose
    }
    'DryRun' {
        Write-Host "[DRY-RUN]Publishing module: $ModuleName"
        Publish-Module -Path $ArtifactPath -NugetAPIKey $ApiKey -Verbose -WhatIf
    }
}
if ($?) {
    Write-Host 'Successfully published.'
}
else {
    Write-Error 'Failed to publish.'
}
```

今改めて見るとこのスクリプト、API キーは Credential に変更した方がマトモだ。ぜひ対応したい。

このように使う。

```powershell
.\publish.ps1 -Mode DryRun -ApiKey xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

.\publish.ps1 -Mode Publish -ApiKey xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

公開後は最終チェック、自端末へモジュールをインストールして一通り使えるか見ている。何しかちゃんと出来てるか不安。

公開できたら、 repo の`README.md` に以下を加筆する。これは流石に自動化無理なのでいいや。

- PowerShell Gallery からのインストール方法
- [Shields.io](https://shields.io/category/downloads) の PowerShell Gallery のダウンロード

終。

---

### おわりに

手順起こしてみてあれやけど、これもうすぐにでも `psake` タスク化できそう。単にサボってただけだったか...
