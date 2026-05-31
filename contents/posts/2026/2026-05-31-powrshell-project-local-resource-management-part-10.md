---
title: "PowerShell Local Resource Manager Part 10"
tags: ["powershell"]
---

[krymtkts/pslrm](https://github.com/krymtkts/pslrm) の開発をした。

pslrm は [`Microsoft.PowerShell.PSResourceGet`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/?view=powershellget-3.x) を module manifest の [`RequiredModules`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_module_manifests?view=powershell-7.6#requiredmodules) に指定していた。
`ModuleVersion` を指定して最低バージョンを宣言しているつもりだった。
しかし文書上は以下のように `ModuleVersion` を指定すると[最低バージョンとなる](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_module_manifests?view=powershell-7.6#requiredmodules)と書いてるけどそうならなかったようだ。

```powershell
    # Modules that must be imported into the global environment prior to importing this module
    RequiredModules = @(
        @{
            ModuleName = 'Microsoft.PowerShell.PSResourceGet'
            ModuleVersion = '1.0.0'
        }
    )
```

最低どころか exact 1.0.0 が同時に install 固定されてしまっていた。
仕方ないので最低バージョンの縛りを外したものを [0.0.1](https://www.powershellgallery.com/packages/pslrm/0.0.1) で公開した。

```powershell
    # Modules that must be imported into the global environment prior to importing this module
    RequiredModules = @(
        # Keep PSResourceGet unpinned here.
        # A pinned ModuleVersion turned into a fixed package dependency and blocked newer versions.
        'Microsoft.PowerShell.PSResourceGet'
    )
```

これで幾分マシになった。
しかしいまでも Windows PowerShell で pslrm を使おうとすると以下のようにややこしい設定をせねばならん。

```yaml
    - name: Install modules from PSGallery (powershell)
      shell: ${{ inputs.shell }}
      if: inputs.shell == 'powershell'
      run: |
        # NOTE: -Repository PSGallery is required in Windows PowerShell CI to avoid untrusted repository prompts that cannot be bypassed in non-interactive mode.
        if (-not (Get-Command Install-PSResource -ErrorAction SilentlyContinue)) {
          Write-Host "Installing Microsoft.PowerShell.PSResourceGet to bootstrap pslrm installation."
          Install-Module -Name Microsoft.PowerShell.PSResourceGet -Force -Scope CurrentUser -Repository PSGallery -SkipPublisherCheck -AllowPrerelease
          # NOTE: PSResourceGet v1.2.0 may fail on first run unless the repository store is initialized.
          # Cannot retrieve the dynamic parameters for the cmdlet. Loading repository store failed:  Could not find a part of the path 'C:\Users\runneradmin\AppData\Local\PSResourceGet\PSResourceRepository.xml'.
          Get-PSResourceRepository | Out-Null
        }
        Set-PSResourceRepository -Name PSGallery -Trusted
        # NOTE: In Windows PowerShell CI, we bootstrap Microsoft.PowerShell.PSResourceGet before calling Install-PSResource.
        # NOTE: pslrm declares PSResourceGet as a dependency, and the dependency install would otherwise try to replace the same loaded module under CurrentUser.
        # NOTE: SkipDependencyCheck avoids that self-reinstall conflict.
        Install-PSResource pslrm -Prerelease -Quiet -Scope CurrentUser -Repository PSGallery -SkipDependencyCheck
        Restore-PSLResource
```

Windows PowerShell 環境には PSResourceGet が存在しないようなので念の為存在を確認した後に install 。
しかしここでも一癖あり、何故か `$env:LOCALAPPDATA//PSResourceGet\PSResourceRepository.xml` が不足する。
それを解消するためには先に [`Get-PSResourceRepository`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/get-psresourcerepository?view=powershellget-3.x) を打つ必要がある。
PowerShell Gallery は既定で信頼されないので信用したうえでようやく install できるようになる。
ちなみに [`-Repository PSGallery`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x#-repository) [`-SkipDependencyCheck`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x#-skipdependencycheck) がないと module が見つからなかったり install 時に失敗したりする。
Windows PowerShell での PSResourceGet の setup は難しすぎる。

奇しくも [PSResourceGet が v1.3 から Windows PowerShell をサポート外にする](https://devblogs.microsoft.com/powershell/powershell-psresource-roadmap-and-best-practices/#dropping-support-for-windows-powershell)という話もあるし、この煩雑な bootstrap は残り続けるのだろう。
[pocof](https://github.com/krymtkts/pocof/) と [PSKeepAChangelogTools](https://github.com/krymtkts/PSKeepAChangelogTools) も Windows PowerShell をサポートしてるが面倒ではある。
PSResourceGet くらいでかいなら Windows PowerShell を切れたら相当楽になるだろう。
そして引きずられた pslrm ではどうすりゃいいんだ？となる。
現状 Windows PowerShell をサポートする限りは `RequiredModules` を [`PSEdition`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_powershell_editions?view=powershell-7.6) で分岐するしかないやろな。
