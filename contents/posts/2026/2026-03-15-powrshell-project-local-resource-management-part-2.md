---
title: "PowerShell Local Resource Manager Part 2"
tags: ["powershell"]
---

[krymtkts/pslrm](https://github.com/krymtkts/pslrm) の開発をした。

[Invoke-Build](https://github.com/nightroman/Invoke-Build) のような task runner 経由であればそういうことができるようにした。
この対応、個人的にかなり難しく GitHub Copilot の導く方向もちょいちょい間違ってたので、要所要所手作りの温もりで調整したつもり。

[このへんのコミット](https://github.com/krymtkts/pslrm/compare/3a999c942ee37eb8ade8516d16978f60a9e66b82...3f63fe6b742c88e5eba7d013cb89a318850e211d)がそうなんだが、入れ子で呼び出される場合に Host の取り扱いが良くなかったみたい。
[`Write-Host`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/write-host) が実行されたら null reference で落ちてしまってた。
GitHub Copilot は独自 Host 実装を作ればいいと言ってたが、んなあほな...ということで階層が深い場合は一番上の Host が使われるように調整して、いけるようになった。

GitHub Actions 上に CI も整備して cross platform のテストを回してみた。
Windows & PowerShell は問題なし。
Windows PowerShell の方は互換性を維持して置き換えたら動くようになった。
Ubuntu & PowerShell の方は成功するけどたまに Host 周りが失敗してるから、 flaky ぽい。
local で [nektos/act](https://github.com/nektos/act) で動かした限りは Ubuntu でも問題なく動いてそうなんやが。
macOS はまだ成功してるところを見たことないが、どうだろう。
このテストは GitHub Copilot サンに書いてもらってるので理解浅いから、後追いで理解していかんと。
特に、今回は背視したような入れ子の Host 問題みたいなケースで本質を追うのは GitHub Copilot サンには難しいから、自力でしか解決できんしな。

まだ結構イマイチな箇所が多いけど、 pslrm の開発で使うレベルでは徐々に [Pester](https://pester.dev/), [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer), Invoke-Build の実行はできるようになった。

例えば [Pester v6](https://pester.dev/docs/v6/quick-start) のように独自提供の型を使って設定を組み上げるようなケースは、煩雑だが以下のようにできる。

```powershell
Invoke-PSLResource Invoke-Pester -Arguments @('-Configuration', (Invoke-PSLResource New-PesterConfiguration))
```

[PSResourceGet](https://github.com/PowerShell/PSResourceGet) しか入ってない Windows PowerShell でも動いてるので多分うまくいってるんじゃないかな。
さらにいいところは以下。

[pslrm/.github/actions/restore-and-test/action.yml at 0eb5c4d62508c80f6704feb404e69c61d56cc508 · krymtkts/pslrm](https://github.com/krymtkts/pslrm/blob/0eb5c4d62508c80f6704feb404e69c61d56cc508/.github/actions/restore-and-test/action.yml#L22-L27)

```yml
- name: Restore modules from PSGallery
  shell: ${{ inputs.shell }}
  run: |
    Set-PSResourceRepository PSGallery -Trusted
    Import-Module ./pslrm.psd1 -Force
    Restore-PSLResource
```

こんな風に依存関係の restore が `Restore-PSLResource` 一発で済んで simple になってるのでとてもいいなと思ってる。
もうちょっといい感じになったら [PowerShell Gallery](https://www.powershellgallery.com/) へ publish しよう。
