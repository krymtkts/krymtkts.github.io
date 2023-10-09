---
title: "Gist の便利な使い方 ~ Permalink"
tags: ["gist","powershell"]
---

今更ながら最近知った。 Gist の Raw コンテンツの URL からコミットハッシュを取り除けば常に最新のリビジョンへの Permalink になる。

[How do I get the raw version of a gist from github? - Stack Overflow](https://stackoverflow.com/questions/16589511/how-do-i-get-the-raw-version-of-a-gist-from-github/16589638#16589638)

これを使えば、ローカルで patch を当てるのにめちゃくちゃ役に立つと気づいた。
ダウンロードの保存先を patch したいコード直にしたらそれで終わり。
patch を当てたいファイルの数だけ Gist 作らなあかんやんけ、というのはあれど、あまりに多い場合は Gist の中に複数ファイル置くとかかな。まだその規模まで届いてないのでそこはまあよい。

これとか。 [VS Code の拡張機能に ローカル patch する](https://krymtkts.github.io/posts/2021-08-30-patch-to-vscode-extension)

メンテナがもう活動してないっぽくて PR がマージされることもないので、ローカル patch するのが手っ取り早い。

Gist に変更対象のコードを置いて、こういう関数を作りまして、実行すればパッチが完了する。

```powershell
function Edit-EverMonkey {
    $evermonkey = '~\.vscode\extensions\michalyao.evermonkey-2.4.5'
    if (-not $evermonkey) {
        Write-Verbose 'There is no evermonkey.'
        return
    }
    $params = @{
        Uri     = 'https://gist.githubusercontent.com/krymtkts/8a5a3a5a7e1efe9db7f2c6bbda337571/raw/converterplus.js'
        OutFile = "$evermonkey\out\src\converterplus.js"
    }
    Invoke-WebRequest @params
}
```

あとこれ。[Terminal-Icons のアイコングリフのコードポイントを変えたい](/posts/2021-07-11-my-terminal-icons.html)

version up の度にいっつも自分用グリフを上書きしてるけど、それが楽になる。

```powershell
function Edit-TerminalIcons {
    $ti = Get-Module Terminal-Icons -ErrorAction SilentlyContinue
    if (-not $ti) {
        Write-Error 'Terminal-Icons not found. install it!'
        return
    }
    $params = @{
        Uri     = 'https://gist.githubusercontent.com/krymtkts/4457a23124b2db860a6b32eba6490b03/raw/glyphs.ps1'
        OutFile = "$(Split-Path $ti.Path -Parent)\Data\glyphs.ps1"
    }
    Invoke-WebRequest @params
}
```

他に PowerShell の profile も Gist で管理しているので、複数の端末間で共有するのに使っている。

```powershell
function Update-Profile {
    $profilehome = ($PROFILE | Split-Path -Parent)
    $params = @{
        Uri     = 'https://gist.githubusercontent.com/krymtkts/f8af667c32b16fc28a815243b316c5be/raw/Microsoft.PowerShell_profile.ps1'
        OutFile = "$profilehome/Microsoft.PowerShell_profile.ps1"
    }
    Invoke-WebRequest @params

    if (-not (Test-Path "$profilehome\Microsoft.VSCode_profile.ps1")) {
        New-Item -ItemType HardLink -Path $profilehome -Name 'Microsoft.VSCode_profile.ps1' -Value "$profilehome\Microsoft.PowerShell_profile.ps1"
    }
}
```

便利だ。
