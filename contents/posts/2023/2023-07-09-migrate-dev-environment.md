---
title: "開発環境をプチ移行する"
tags: ["powershell", "vscode"]
---

旧聞だが、 PowerShellGet が PSResourceGet に改名した。
2023 秋の PowerShell 7.4 リリース前には GA するらしいのでそれに移行することとした。

[PSResourceGet Preview is Now Available - PowerShell Team](https://devblogs.microsoft.com/powershell/psresourceget-preview-is-now-available/)

ついでに VS Code から VS Code Insiders にも移行したのでメモがてら残す。

### VS Code Insiders

VS Code Insiders にする理由はまだ順番待ちの GitHub Copilot Chat が落ちてきたら即使えるようにするためだ。
もうかなり待っていても来ないが、最新の VS Code じゃないと動かないとかなんとか。

わたしは chocolatey で入れる。これも以下のコマンドを打つだけだ。
[Chocolatey Software | Visual Studio Code Insiders 1.80.0.20230704](https://community.chocolatey.org/packages/vscode-insiders)

```bat
choco install vscode-insiders -y
```

VS Code とは別に VS Code Insiders がインストールされる。

ここで `code-insiders .` を実行して VS Code Insiders を開くとまっさらになっている。はじめは Setting Sync が有効でないためだ。
最も簡単に VS Code から VS Code Insiders へ設定を移行する方法は、一時的に VS Code Insiders で Stable の Setting Sync に繋ぐ方法だろう。
わたしの場合は非公開になってしまった拡張機能以外はこれで問題なく移行できた。
移行後は Setting Sync を off にして Stable から離脱、再度 Setting Sync を on にして Insiders へ接続するようにすれば良い。
たまに Stable と Insiders で非互換があるらしいし、自身の利用端末全てで VS Code Insiders に移行するまではリスクがあると見て分けておくのが無難やろな。
例外的に、認証情報の類は `setting.json` に token を書くような野蛮なもの以外すべてポチポチ再認証が必要だが、それでも随分楽なもんやな。

VS Code Insiders ではコマンドも `code` から `code-insiders` に変わるので、これまた PowerShell の profile で該当の箇所を変えてやる必要がある。
普段から `code` を使う場面は [ghq](https://github.com/x-motemen/ghq) で一覧から選択した repo を開くときだけだ。
面倒なので今回 current directory を開く関数を profile に新設した。さすがに `code-insiders` って長すぎるので許容できない。

```powershell
# もとからあるやつ
function Set-SelectedRepository {
    ghq list | Select-Pocof | Select-Object -First 1 | ForEach-Object { Set-Location "$(ghq root)/$_" }
}
Set-Alias gcd Set-SelectedRepository -Option AllScope
# 新設
function Open-SelectedRepository {
    param(
        [Parameter()]
        [ValidateSet('Stable', 'Insider')]
        [string]
        $Channel = 'Insider'
    )
    $code = switch ($Channel) {
        'Stable' { 'code' }
        'Insider' { 'code-insiders' }
    }
    Set-SelectedRepository && & $code .
}
Set-Alias gcode Open-SelectedRepository -Option AllScope
```

これ `gcd` で選択肢がなかった場合の `code-insiders` が current directory になるのでなんか改善したほうが良いけど、いったんこれで。

### PSResourceGet

PSResourceGet のインストールも大したことない。ブログに示されるコマンドでインストールすれば良い。
わたしの場合は管理者権限で全ユーザ対象にインストールしてるので、追加でそのオプションだけつける。

```powershell
Install-Module -Name Microsoft.PowerShell.PSResourceGet -AllowPrerelease -Scope AllUsers
```

インストールしたら PowerShell の profile で書いてる PowerShellGet のコマンドを PSResourceGet のものに置き換える。
今回対象になったのは以下だ。基本 `Module` だった部分が `PSResource` になるだけだが、オプションも微妙に変わっているところもある。
profile で使ってるコマンドは以下の感じに置き換わる。意外に多かった。

- `Get-InstalledModule` -> `Get-InstalledPSResource`
  - `-Scope` の指定がいる
- `Install-Module` -> `Install-PSResource`
  - `-AllowPrerelease` -> `-Prerelease`
  - `-AllowClobber` -> default 挙動に。 従来の挙動は `-NoClobber`
- `Set-PSRepository` -> `Set-PSResourceRepository`
  - 追記: こいつもあったわ `-InstallationPolicy Trusted` -> `-Trusted`
- `Get-Module` -> ~~`Get-PSResource`~~
  - 追記:
    - `Get-PSResource` は PowerShellGet v3 にはあったけど PSResourceGet にはない。つまり何に変わったんだこれ？
    - `Get-Module` は `Microsoft.PowerShell.Core` が Source なのでそのまま使えば良いのか
- `Find-Module` -> `Find-PSResource`
- `Update-Module` -> `Update-PSResource`

これらの破壊的変更、どうも repo の CHANGELOG にまとまってないぽくて、すべてを一箇所で見つけることはできなかった。
[PowerShell Team のブログ](https://devblogs.microsoft.com/powershell/psresourceget-preview-is-now-available/) とか追っていったら全貌わかるかもだが、面倒だ。
`NoClobber` の挙動とかは以下を参考にした。信頼の Ironman 。
[What's new in PowerShellGet v3?](https://blog.ironmansoftware.com/powershellget-v3/)

これらの変更を profile に施したあとうまく動いてるのが確認できたら仕事機にも反映する。

個人的にちょっとめんどいのが [AWS Tools for PowerShell](https://github.com/aws/aws-tools-for-powershell) との今後の付き合い。
AWS Tools for PowerShell が PSResourceGet に移行するまで、当面は v2 v3 並行稼働という感じになるのだろうか。うまくいくのだろうか。
互換モジュールが提供されてるから、そっちを使うのが良さげ。

[PowerShell/CompatPowerShellGet: This module provide functions used with PowerShellGet v3 to provide compatibility with scripts expecting PowerShellGet v2](https://github.com/PowerShell/CompatPowerShellGet)

あと開発環境とは別に [pocof](https://github.com/krymtkts/pocof/) の方でも PSResourceGet への移行を反映したい。
具体的にはコマンドの名前が変わったこの辺。これらは Issue にしてればいいかな。未だ .NET 7 にも移行してないから、そういうのもやっていき。

- `Get-Module` -> ~~`Get-PSResource`~~
  - 追記: 先述の通り
- `Publish-Module` -> `Publish-PSResource`

### 追記 1

`Find-AWSToolsModule` が `Find-Module` に依存してて、事前にダミーの `Find-Module` してないとエラーになるやつ未だ直ってないようだ。

> Find-AWSToolsModule: The term 'PowerShellGet\Find-Module' is not recognized as a name of a cmdlet, function, script file, or executable program. Check the spelling of the name, or if a path was included, verify that the path is correct and try again.

自分用にはこれを回避するために関数を書いてたが、CompatPowerShellGet を使うとこの依存関係が永久に解決できないみたい。
module 違うからなあ...やはり v2 v3 の並行稼働しかなさそう...だいじょぶかな。

`Get-PSResource` も戻り値の property に変化があって、対処が必要だった。

- `Path` -> `InstalledLocation`

### 追記 2

PowerShellGet に `-AllowPrerelease` つけて v3 をインストールしてたがややこしい。
PSResourceGet にない Cmdlet もあったりして危ない。おまえのことやぞ `Get-PSResource` 。
PowerShellGet は prerelease 版を入れないようにするのが無難。
