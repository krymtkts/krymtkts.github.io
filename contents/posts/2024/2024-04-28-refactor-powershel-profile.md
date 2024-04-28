---
title: "PowerShell の profile を再構成する"
tags: ["powershell"]
---

長らく[自作 PowerShell の profile](https://gist.github.com/krymtkts/f8af667c32b16fc28a815243b316c5be) を使ってる。
長年継ぎ足ししてきた謂わば秘伝のタレのようなもので、全体はかなりごチャついている。
これを整理して再構成する必要が出てきたので、今日はそれを記す。

---

再構成が必要だと思ったのは、毎週行う PowerShell module はじめとした諸々のモジュールの更新のときだった。
PowerShell module の更新は以下の関数で行うのだけど、違和感わかってもらえるだろうか？

```powershell
function Update-InstalledModules {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    Get-InstalledPSResource -Scope AllUsers | Where-Object -Property Repository -EQ 'PSGallery' | ForEach-Object {
        $Prerelease = $_.Name -notin $pinStable
        $_.Name | Update-PSResource -Prerelease:$Prerelease -Scope AllUsers
    }
}
```

更新後にたまたま PowerShell Gallery を見たら妙に [krymtkts/pocof](https://github.com/krymtkts/pocof) の download 数が増えてたのでなんでや？と思って見てみたら、犯人はこいつだった。
`Installed-PSResource` は module name x version の組み合わせを返すので、インストールされている pocof の数だけ download が増えるという...
このバグが入り込んだタイミングは PowerShellGet -> Microsoft.PowerShell.PSResourceGet に変わったタイミングだろう。

```powershell
function global:Update-InstalledModules {
    [CmdletBinding(SupportsShouldProcess)]
    param()

    Uninstall-OutdatedPSResources
    Get-InstalledPSResource -Scope AllUsers | Where-Object -Property Repository -EQ 'PSGallery' | Group-Object -Property Name | ForEach-Object {
        $Prerelease = $_.Name -notin $global:pinStable
        Write-Host "Update $($_.Name) $(if ($Prerelease) {'Prerelease'} else {''})"
        # NOTE: -WhatIf is not work with Update-PSResource in some cases.
        Update-PSResource -Name $_.Name -Prerelease:$Prerelease -Scope AllUsers
    }
}
```

なのでこんな感じで `Group-Object` で複数バージョンあっても折りたたむ形にした。

余談だが、 `Update-PSResource` を `WhatIf` で動作確認してたら妙なエラーになってよくわからなかった。

```plaintext
> Update-PSResource pocof -Prerelease -WhatIf -Scope AllUsers
What if: Performing the operation "Update-PSResource" on target "Package to install: 'pocof', version: '0.11.0'".
What if: Performing the operation "Update-PSResource" on target "Exit ShouldProcess".

Exception             :
    Type    : Microsoft.PowerShell.PSResourceGet.UtilClasses.ResourceNotFoundException
    Message : Package(s) 'pocof' could not be installed from repository 'PSGallery'.
    HResult : -2146233088
TargetObject          : Microsoft.PowerShell.PSResourceGet.Cmdlets.UpdatePSResource
CategoryInfo          : InvalidData: (Microsoft.PowerShel…ts.UpdatePSResource:UpdatePSResource) [Update-PSResource], ResourceNotFoundException
FullyQualifiedErrorId : InstallPackageFailure,Microsoft.PowerShell.PSResourceGet.Cmdlets.UpdatePSResource
InvocationInfo        :
    MyCommand        : Update-PSResource
    ScriptLineNumber : 1
    OffsetInLine     : 1
    HistoryId        : 13
    Line             : Update-PSResource pocof -Prerelease -WhatIf -Scope AllUsers
    Statement        : Update-PSResource pocof -Prerelease -WhatIf -Scope AllUsers
    PositionMessage  : At line:1 char:1
                       + Update-PSResource pocof -Prerelease -WhatIf -Scope AllUsers
                       + ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    InvocationName   : Update-PSResource
    CommandOrigin    : Internal
ScriptStackTrace      : at <ScriptBlock>, <No file>: line 1
PipelineIterationInfo :
      0
      1
```

[`-WhatIf` results in extra, unwanted output and a spurious error · Issue #1541 · PowerShell/PSResourceGet](https://github.com/PowerShell/PSResourceGet/issues/1541)

Cmdlet は違うがこれと同じっぽい。
アップデートできるモジュールがある場合に起こる。
パッと見の挙動は `-WhatIf` 付きのときにインストール済み module 名を消せてないから「インストールできなかった」エラー判定されるようだった。焦らすなや～。
コード追ってみたがエラー発生箇所はわかったけどどうやったら解決するのかは追いきれなかった。

[PSResourceGet/src/code/InstallHelper.cs at bb4c0d0402a43cb561b33c681024ec6312a8474d · PowerShell/PSResourceGet](https://github.com/PowerShell/PSResourceGet/blob/bb4c0d0402a43cb561b33c681024ec6312a8474d/src/code/InstallHelper.cs#L333-L341)

---

あと Gist でずっと管理してたやつを GitHub repo に移した。 [krymtkts/pwsh-profile](https://github.com/krymtkts/pwsh-profile)

Gist で最新版を管理するのが手軽であるのと、 snippet に対して直にコメントもらったりのコミュニケーションを取りやすいのが利点だと認識している。
ただわたしの PowerShell profile の場合そういう化学反応起こらないので、履歴管理の面倒さからも Gist をやめたくなった次第。
どうせ履歴の更新に関しては従来通り GistPad を使ってコピペしていくだけなので、何も変わらない。
移行は以下の手順で行った。びみょーに実際の手順と違って間違いあるかもしれんが大体こんな感じなはず。

```powershell
# new repo.
New-GitHubRepository -RepositoryName pwsh-profile -Private
# mistake.
$r = Get-GitHubRepository -OwnerName krymtkts -RepositoryName pwsh-profile

# clone gist.
ghq get https://gist.github.com/krymtkts/f8af667c32b16fc28a815243b316c5be
# move to gist repo.
gcd
git remote add github $r.ssh_url
# change master to main.
git switch --create main
# push to the GitHub repo.
git push -u github main
$remove = pwd

# clone new repo.
ghq get -p $r.ssh_url
# move to new repo.
gcd
# publish repo.
Set-GitHubRepository -OwnerName krymtkts -RepositoryName pwsh-profile -Private:$false

# remove gist repo.
rm -Recurse $remove -Force
Remove-GitHubGist -Gist f8af667c32b16fc28a815243b316c5be -Force
```

はじめ `Remove-GitHubGist` が 404 Not Found で返ってきて何ぞ？と思ったが、 Access Token の権限足りないようだった。 403 とかじゃないんだ。

---

バグ修正の他に初期化の処理をカテゴリ毎に関数を分類してみたりしたが、これどこまでやるべきなんやろかというのは悩ましいところだ。
profile の処理を分類したかったら結局ファイルを分けることになって、ファイルを分けると profile のディレクトリ全体を履歴管理するのが定石。
でもそのような profile 全体を git の管理下に置くのは個人的には好きでないので、どうしたもんかな。

つづく。
