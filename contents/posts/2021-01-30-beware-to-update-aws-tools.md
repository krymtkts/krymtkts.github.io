---
title: "AWS.Tools.Installer で入れたモジュールの更新は気をつけろよ"
tags: ["powershell", "aws"]
---

```powershell
Install-Package: C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\PowerShellGet\2.2.5\PSModule.psm1:13069
 Line |
13069 |  …           $sid = PackageManagement\Install-Package @PSBoundParameters
      |                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
      | Unable to find repository 'C:\Users\takatoshi\AppData\Local\Temp\xeqcnbnp.pgl'. Use Get-PSRepository to see all available repositories.
```

普段 PowerShell Module を一括更新しているのだけど、変なエラーが出るよになった。

一括更新は、 `Get-InstalledModule | Update-Module -AllowPrerelease` で。

ちょうど最近、 [Publish-Module うまくいかない問題](https://github.com/PowerShell/PowerShellGetv2/issues/303) を解消したり Win10 の更新したりしてたので、そのへんかなーと思い NuGet の Provider の登録し直しとか色々やっても直らず 😥

初心に帰って、↑ のログを見てみたら「何やねんこの謎 repo」というのに気づき、 改めて`Get-InstalledModule`。

```PowerShell
 ⚡ takatoshi  ~  Get-InstalledModule

Version              Name                                Repository           Description
-------              ----                                ----------           -----------
4.1.7.0              AWS.Tools.Amplify                   C:\Users\takatoshi\… The Amplify module of AWS Tools for PowerShell lets developers and administrators manage AWS Amplify from the PowerShell scripting environment. In order to manage each AWS service, install th…
4.1.7.0              AWS.Tools.CloudFormation            C:\Users\takatoshi\… The CloudFormation module of AWS Tools for PowerShell lets developers and administrators manage AWS CloudFormation from the PowerShell scripting environment. In order to manage each AWS servi…
4.1.7.0              AWS.Tools.Common                    C:\Users\takatoshi\… The AWS Tools for PowerShell lets developers and administrators manage their AWS services from the PowerShell scripting environment. In order to manage each AWS service, install the correspon…
4.1.7.0              AWS.Tools.EC2                       C:\Users\takatoshi\… The EC2 module of AWS Tools for PowerShell lets developers and administrators manage Amazon Elastic Compute Cloud (EC2) from the PowerShell scripting environment. In order to manage each AWS …
4.1.7.0              AWS.Tools.IdentityManagement        C:\Users\takatoshi\… The IdentityManagement module of AWS Tools for PowerShell lets developers and administrators manage AWS Identity and Access Management from the PowerShell scripting environment. In order to m…
1.0.2.0              AWS.Tools.Installer                 PSGallery            The AWS.Tools.Installer module makes it easier to install, update and uninstall other AWS.Tools modules (see https://www.powershellgallery.com/packages/AWS.Tools.Common/).…
4.1.7.0              AWS.Tools.Lambda                    C:\Users\takatoshi\… The Lambda module of AWS Tools for PowerShell lets developers and administrators manage AWS Lambda from the PowerShell scripting environment. In order to manage each AWS service, install the …
4.1.7.0              AWS.Tools.S3                        C:\Users\takatoshi\… The S3 module of AWS Tools for PowerShell lets developers and administrators manage Amazon Simple Storage Service (S3) from the PowerShell scripting environment. In order to manage each AWS s…
4.1.7.0              AWS.Tools.SecretsManager            C:\Users\takatoshi\… The SecretsManager module of AWS Tools for PowerShell lets developers and administrators manage AWS Secrets Manager from the PowerShell scripting environment. In order to manage each AWS serv…
0.1.8                ClipboardText                       PSGallery            Support for text-based clipboard operations for PowerShell Core (cross-platform) and older versions of Windows PowerShell
1.3.1                Configuration                       PSGallery            A module for storing and reading configuration values, with full PS Data serialization, automatic configuration for modules and scripts, etc.
1.2010.0.201211      DockerCompletion                    PSGallery            Docker command completion for PowerShell.
1.27.0.200908        DockerComposeCompletion             PSGallery            Docker Compose command completion for PowerShell.
0.16.2.190903        DockerMachineCompletion             PSGallery            Docker Machine command completion for PowerShell.
0.2                  MavenAutoCompletion                 PSGallery            Maven Auto Completion provides a simple auto completion of Maven 3 to PowerShell.
3.75.0-beta          oh-my-posh                          PSGallery            A prompt theme engine for any shell
1.4.7                PackageManagement                   PSGallery            PackageManagement (a.k.a. OneGet) is a new way to discover and install software packages from around the web.…
5.1.0-rc1            Pester                              PSGallery            Pester provides a framework for running BDD style Tests to execute and validate PowerShell commands inside of PowerShell and offers a powerful set of Mocking Functions that allow tests to mim…
1.1.0                poco                                PSGallery            Interactive filtering command based on peco
1.0.0-beta3          posh-git                            PSGallery            Provides prompt with Git status summary information and tab completion for Git commands, parameters, remotes and branch names.
0.16.0               PowerShellForGitHub                 PSGallery            PowerShell wrapper for GitHub API
3.0.0-beta10         PowerShellGet                       PSGallery            PowerShell module with commands for discovering, installing, updating and publishing the PowerShell artifacts like Modules, DSC Resources, Role Capabilities and Scripts.
4.9.0                psake                               PSGallery            psake is a build automation tool written in PowerShell.
2.2.0-beta1          PSReadLine                          PSGallery            Great command line editing in the PowerShell console host
1.19.1               PSScriptAnalyzer                    PSGallery            PSScriptAnalyzer provides script analysis and checks for potential code defects in the scripts by applying a group of built-in or customized rules on the scripts being analyzed.
0.2.2                Terminal-Icons                      PSGallery            PowerShell module to add file icons to terminal based on file extension
2.2.0                Get-ChildItemColor                  PSGallery            Get-ChildItemColor provides colored versions of Get-ChildItem Cmdlet and Get-ChildItem | Format-Wide (ls equivalent)
0.1.2                Get-GzipContent                     PSGallery            Gets the content of the gzip archive at the specified location.
1.0.1.10             GoogleCloud                         PSGallery            PowerShell cmdlets for the Google Cloud Platform.
```

ﾌｷﾞｬｰ、AWS.Tools.Installer で入れたモジュールは全て repo が一時ファイルになっとるやんけ！😇

これは事案ですね。ということでとりま暫定対処として Repository が PSGallery のやつだけにした。

```powershell
Get-InstalledModule | Where-Object -Property Repository -eq 'PSGallery' | Update-Module -AllowPrerelease
```

[AWS Tools の GitHub](https://github.com/aws/aws-tools-for-powershell/issues?q=is%3Aissue+is%3Aopen+unable+to+find+repository) を見てみても誰も同じような話はしてないし、みんな真面目に`Update-AWSToolsModule`と`Update-Module`使い分けれてんねな～エライ！というのに気づいた一日であった。

ﾁｬﾝﾁｬﾝ。
