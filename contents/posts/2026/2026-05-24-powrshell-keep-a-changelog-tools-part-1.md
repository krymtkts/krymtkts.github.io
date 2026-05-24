---
title: "PowerShell Keep a Changelog tools Part 1"
tags: ["powershell", "keepachangelog"]
---

[krymtkts/pslrm](https://github.com/krymtkts/pslrm) と [krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) で [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/) を採用したのでそのためのツールを開発した。

- [krymtkts/PSKeepAChangelogTools](https://github.com/krymtkts/PSKeepAChangelogTools)
- [PowerShell Gallery | PSKeepAChangelogTools 0.1.0](https://www.powershellgallery.com/packages/PSKeepAChangelogTools/0.1.0)

命名は難しかった。
[PSKeepAChangeLog](https://www.powershellgallery.com/packages/PSKeepAChangeLog/) と [KeepAChangelog](https://www.powershellgallery.com/packages/KeepAChangelog/) が既に存在したからだ。
PowerShell の [module manifest](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_module_manifests) に特化した tools の側面あるなというところから、結局 Tools を付け足した。
[ChangelogManagement](https://www.powershellgallery.com/packages/ChangelogManagement/) みたいな名前自体が汎化されてる前例もある。
でも Keep a Changelog のスタイルしか対応しないよというスタンスであるので、 module 名で示す必要があった。

krymtkts/pslrm と krymtkts/pslrm-bump-action で Keep a Changelog 用に作った関数を切り出して汎化した。
提供される機能としては多分他と大差ないけど、 module manifest への書き込み機能を持つのは多分唯一かな。
作った動機が module manifest へのコピペは避けたかったからなので、そこがほぼメインと言って過言でない。
CI でも task runner でも使える形にはなってるはず。
わたし自身は local で task runner で実行して commit してる。

そもそも Keep a Changelog を採用してるのは、 [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) が好きじゃないからだ。
機械にまとめさせられる点でも便利だし、何なら AI Agents はみんな暗黙の了解で Conventional Commit を使ってくる。
ただ commit message に構造化された識別子を付与する、ってのがどうにも気に入らん。
1 つの識別子にするって結構難しいよなと感じる。そこで削げ落ちる情報があるなら採用しにくいな、というのがわたしの考えだ。
あと意図を commit message に込めたいなと思っており、 Conventional Commit だとどうしても何をしたかになってしまうのも好きくない。
Keep a Changelog なら changelog を書くのは人間の責務なので、融通も利くしそれで良いかなと考えた。

```powershell
Get-KeepAChangelogEntry -Version 0.1.0
# ### Added
#
# - Add `Get-KeepAChangelogSection` for reading changelog sections by version.
# - Add `Get-KeepAChangelogEntry` for reading rendered changelog entries by version.
# - Add `Assert-KeepAChangelogReleaseMetadata` for validating changelog versions and release tags.
# - Add `Get-KeepAChangelogManifestReleaseNotes` for rendering manifest release notes from `CHANGELOG.md`.
# - Add `Set-KeepAChangelogManifestReleaseNotes` for updating manifest release notes.
# - Add build tasks for linting, tests, and release note synchronization.
# - Add staged-module integration tests, CI, and release automation.
#
# ### Notes
#
# - This is the first public release of `PSKeepAChangelogTools`.
# - Supported PowerShell versions are Windows PowerShell 5.1 through PowerShell 7.x.
# - The module scope is intentionally limited to Keep a Changelog style changelogs.

Assert-KeepAChangelogReleaseMetadata -Version 0.1.0
# No output because the version exists.

Assert-KeepAChangelogReleaseMetadata -Version 0.1.1
# Exception: C:\Program Files\PowerShell\Modules\PSKeepAChangelogTools\0.1.0\src\KeepAChangelog.Core.ps1:72
# Line |
#   72 |          throw "Changelog entry not found for version: $Version"
#      |          ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
#      | Changelog entry not found for version: 0.1.1
```

以下で release note を module manifest に書き込む。
でも改めて見るとこの分離面倒なので、 parameter set でまとまった挙動をする version 追加したいな。

```powershell
$releaseNotes = Get-KeepAChangelogManifestReleaseNotes -Path $ChangelogPath -Version $ModuleVersion -FullChangelogUrl $FullChangelogUrl
Set-KeepAChangelogManifestReleaseNotes -ManifestPath $ModuleManifest.FullName -ReleaseNotes $releaseNotes
```
