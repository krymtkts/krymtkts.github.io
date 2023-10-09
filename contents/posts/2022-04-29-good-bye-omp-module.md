---
title: "さよなら oh-my-posh モジュール"
tags: ["oh-my-posh","powershell"]
---

いつものように PowerShell Module を更新して PowerShell を立ち上げると次のようなメッセージが表示された。

```txt
Hey friend

In an effort to grow oh-my-posh, the decision was made to no
longer support the PowerShell module. Over the past year, the
added benefit of the module disappeared, while the burden of
maintaining it increased.

However, this doesn't mean oh-my-posh disappears from your
terminal, it just means that you'll have to use a different
tool to install it.

All you need to do, is follow the migration guide here:

https://ohmyposh.dev/docs/migrating
```

あら... oh-my-posh 3 から Go で実装されて、マルチプラットフォームなプロンプトテーマエンジンになってたけど、ついにその時が来たか...という感じ。

とりあえず使えなくなる(というか更新できなくなる)と困るので、[移行手順](https://ohmyposh.dev/docs/migrating) に記載された移行手順を行うことにした。

### わたしの移行手順

記載の順番にやってもいいけど、慎重を期すため順番を変えた。

まず `oh-my-posh` が `PATH` にある状態で、 `Set-PoshPrompt` を `oh-my-posh init pwsh` に変え、プロンプトの表示をチェックした。

```powershell
# Set-PoshPrompt -Theme ~/.oh-my-posh.omp.json
oh-my-posh init pwsh --config ~/.custom.omp.json | Invoke-Expression
```

当然のごとくきれいに出力されるので、 PowerShell Module から置き換える。

[Windows 向けの公式なインストール手順](https://ohmyposh.dev/docs/installation/windows)では `winget`, `scoop` それか手動での更新になっている。

でもわたしは永らく `chocolatey` を使っていることもあり、有志が公開してくれているパッケージを利用する。見たところバージョンも最新に追随していていい感じ。
[Chocolatey Software | Oh-My-Posh 7.74.3](https://community.chocolatey.org/packages/oh-my-posh#versionhistory)

管理者権限で `cmd` を開き、インストールする。補足：PowerShell でもインストールできるが、PowerShell 自体を `chocolatey` でインストールしているのもあり `chocolatey` でのインストールは `cmd` で行うようにしている。

```cmd
choco install oh-my-posh -y
```

このインストールの最後に、

```plaintext
PROFILE: C:\Users\takatoshi\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1
oh-my-posh has been added to your profile. You may wish to append 'Set-PoshPrompt paradox' to set a theme
```

と言われたが、 Windows PowerShell のプロファイルはおろかどこにも加筆されてないようだった。

管理者権限で PowerShell を起動し、 oh-my-posh の Powershell Module を削除する。

```powershell
Uninstall-Module oh-my-posh -AllVersions
```

`Import-Module oh-my-posh` は元々書いてなかったので削除なし。代わりにプロファイルの中でインストール・更新するモジュール名を管理しているので、そこから取り除いた。

以上。

### 感想

何事もなく移行できて良かった。

ただこれを機に、もう oh-my-posh にこだわらず、 [Starship](https://starship.rs/) みたいなもっとイケてるプロンプトテーマエンジンに積極的に乗り換えてもいいかなーと思えてきた。

元々 oh-my-posh を使ってたのは、 2018 年頃の PowerShell でいい感じの Powerline ができる唯一のツールだったからだ(わたしの観測範囲では。もう一つその名の通り Powerline というモジュールがあったが満足の行くカスタマイズができなかった[^1])。
テーマを PowerShell で書けて、PowerShellGet からインストールできるのも楽だった。

とはいえ、それがマルチプラットフォームに対応した oh-my-posh の足枷になったみたいやけど。

逆に利用者の立場からいえば、 oh-my-posh が明確に PowerShell に特化しなくなったということ。
これは、わたしが Power Fighter(勝手に作った PowerShell 使いの呼び名)であるから oh-my-posh を使い続けていたという理由もなくなったことになる。

oh-my-posh が Go で書き換わったときに一番気に入らなかったのは _**設定ファイルが JSON**_ なとこなので、そのフラストレーションを解消するいい機会をもらったのかも知れない。

[^1]: まだあったわ →[PowerShell Gallery | PowerLine 3.4.0](https://www.powershellgallery.com/packages/PowerLine/3.4.0)
