---
title: "PowerShell Local Resource Manager Part 3"
subtitle: pslrm 0.0.1-alpha
tags: ["powershell"]
---

[krymtkts/pslrm](https://github.com/krymtkts/pslrm) の開発をした。

[0.0.1-alpha](https://www.powershellgallery.com/packages/pslrm/0.0.1-alpha) として PowerShell Gallery に公開した。
まだ使い勝手はかなり悪いが、最低限の版として一度公開しておきたかった。
コレまでの機能実装からは特に機能的な拡充をしてなくて、専らに CI/CD を中心に整備した。
release したかったのは、この CI/CD 周りの最後の動作確認をしたかったからでもある(そして build script の不具合が見つかり目論見通り動作確認の意味があった)。

pull request 時や push 時の CI は元々作ってあった。
これに加えて release note 更新、 [PowerShell Gallery](https://www.powershellgallery.com/) への公開と [GitHub の Release](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository) 作成の流れを GitHub Actions に起こした感じ。
特に release note に関しては、 PowerShell Gallery の事例を見つつなるべくマシな形になるよううまく整理したんじゃないかなと。

これまで自分で作ってきた PowerShell module は管理がてきとーだったので release note を作ってこなかった。
今回からは真面目にやろうと考えて、 PowerShell の [module manifest](https://learn.microsoft.com/en-us/powershell/scripting/developer/module/how-to-write-a-powershell-module-manifest?view=powershell-7.6) に含まれる [`ReleaseNotes`](https://learn.microsoft.com/en-us/powershell/scripting/developer/module/how-to-write-a-powershell-module-manifest?view=powershell-7.6#module-manifest-elements) の更新なんかも真面目に検討した。
ここでいう release note は changelog の一部であると考えてもらいたい。
PowerShell module には release note の属性が用意されているが、そこに何を書くかは明確に定義されていない。

わたしの知る限り、 PowerShell Gallery に公開されてる module では大体以下のパターンが存在する。

1. release note への外部リンク(URL 文字列)
2. 累積の release note
3. 何もなし

1 は、大体 GitHub の release とか changelog のファイルへの外部リンクが多い。
中には全然関係ない URL もあったりするようだ。
この場合 PowerShell Gallery に表示されたら、リンクになっている。
[`Get-Module`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/get-module?view=powershell-7.6) や [`Get-InstalledPSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/get-installedpsresource?view=powershellget-3.x) で得た `ReleaseNotes` はただの URL 文字列だ。
代表例は [Pester](https://www.powershellgallery.com/packages/Pester/5.1.0) なんかがそう。

```plaintext
> Get-InstalledPSResource -Name pester -Scope AllUsers | % ReleaseNotes
https://github.com/pester/Pester/releases/tag/6.0.0-alpha5
```

module manifest の `ReleaseNotes` の更新も URL 文字列をブチ込むだけなので簡単。
だからこれは運用最適化のパターンで、利用者としてはその場で確認できないという意味でちょっと味気ないなと感じている。

2 はちょっとややこしい。
始まった頃からの累積 release note と、 major や minor 毎の累積 release note がありそう。
開発者は `ReleaseNotes` を自動生成・更新するなり手で更新する必要がある。
また module manifest が肥大化する恐れがある(サイズ制限の有無は不明)。
運用の負荷が一番高いが、利用者は URL をたどる必要がないので情報を得やすい。
パッと見、 [PSResourceGet](https://www.powershellgallery.com/packages/PowerShellGet/2.2.5) はこの minor version 毎の累積 release note っぽい。
pull request を見るに module manifest は手で更新してるのかな～と思ってる。
そのためか、ファイルが肥大化しないように [changelog を version で分割してる](https://github.com/PowerShell/PSResourceGet/tree/9c8bd26a63dad0f79448c8620870c730ccd8169e/CHANGELOG)みたい。

```plaintext
> Get-InstalledPSResource -Name *PSResourceGet -Scope AllUsers -Version 1.2.0-rc3 | % ReleaseNotes
## 1.2.0-rc3

## Bug fix
- Packages that depend on a specific version should search for the dependency with NormalizedVersion (#1941)

## 1.2.0-rc2

## Bug fix
- For packages with dependency on a specific version use specific version instead of version range (#1937)

... 略
```

3 は simple だ。何もなし。
開発者は `ReleaseNotes` には何もしなくて良い。
利用者は `ProjectUri` から自力でたどり着く必要がある。
開発者は楽だが利用者に対して親切ではないかなと考えている運用。
代表例は [PSScriptAnalyzer](https://www.powershellgallery.com/packages/PSScriptAnalyzer/1.24.0)
また、利用者が `ProjectUri` を辿ってもそこに release note が存在する保証はない。
わたしのこれまでの module はこれにあたる。

PowerShell module の release note 事情はこのように現時点での正解はないと思っている。
であれば自分で考える正解を突き進むのが良かろうと思った。
個人的には release note はあった方が良い。また運用負荷は下げたくて手作業はやりたくない。
module manifest の肥大化も避けたい。
というわけで以下のルールとした。

- `CHANGELOG.md` を source of truth にする
- 直近 3 release 分の release note を module manifest の `ReleaseNotes` に含める
- 以降は省略し `ReleaseNotes` 最下部に full release note である `CHANGELOG.md` への URL を記載する
- GitHub Release には release 対象 version の release note を記載する(当たり前)

情報を示しつつ完全版への道筋も示す 1 と 2 の複合版が多分いま一番マシなんじゃないかなと考えた。

`CHANGELOG.md` は [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) を参考にしつつ、情報の付加や自動化に有利な独自のルールを付加している。
具体的に言うと `Notes` で release ごとの付加情報を添えたのと、自動化を簡単にするため footer の区切りとして `---` を入れたところ。
ほかは Keep a Changelog に倣っている。

今どきは conventional commit が多いが、個人的に好きではないので使ってないのもあり、利用者向けの情報となる release note は人間がまとめる方針とした。
`CHANGELOG.md` を source of truth にして `ReleaseNotes` と GitHub Release への反映を自動化した task を用意している。

0.0.1-alpha の場合以下のようになった。多分そんなに変なことにはなってないはず。

```plaintext
> Get-InstalledPSResource -Name pslrm -Scope AllUsers | % ReleaseNotes
## [0.0.1-alpha]

### Added

- Add project-local PowerShell resource management based on PSResourceGet.
- Add requirements and lockfile workflows with `Install-PSLResource` and `Update-PSLResource`.
- Add lockfile restore and removal workflows with `Restore-PSLResource` and `Uninstall-PSLResource`.
- Add `Get-InstalledPSLResource` for reading installed project resources from the lockfile.
- Add `Invoke-PSLResource` for running commands from project-local resources in an isolated runspace.
- Add build, lint, unit test, and integration test tasks through `Invoke-Build`.

### Notes

- This is the initial alpha release track for `pslrm`.
- Supported PowerShell versions are Windows PowerShell 5.1 through PowerShell 7.x.
- Supported repository is PowerShell Gallery.
- `Invoke-PSLResource` uses `IsolatedRunspace` execution. `InProcess` execution is not implemented.

Full CHANGELOG: https://github.com/krymtkts/pslrm/blob/main/CHANGELOG.md
```

準備には手間がかかったが、コレによって PowerShell Gallery や module 自体に理解しやすい release note を提供できる。
また `CHANGELOG.md` 自体への導線も保ちつつ、自動化で付加も低減できた。
個人開発で release note をするのは今回が初めてなので、真面目に検討した価値があったんじゃないかな。
当面この感じで使ってみて問題点現れないかやってみるつもり。
