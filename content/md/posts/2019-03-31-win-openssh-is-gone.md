{:title "Windows10の更新でOpenSSHが逝った"
 :layout :post
 :tags  ["windows", "openssh"]}

今更ながら、Raser Blade Stealth 2018にもWindows10 ver1809の更新が来てたようだった。

何の気なしに更新してみたところ、更新自体はすぐに終わってあっさりいったなと思っていたのだが、terminalを立ち上げると...

### TL;DR

Windows10 ver1809のOpenSSHは既知のバグがあるのでOpenSSH Portableを入れ直そう。

### まずSSH Agentのサービスが無効になってた

```powershell
Get-Process : Cannot find a process with the name "ssh-agent". Verify the process name and call the cmdlet again.
At C:\Users\takatoshi\OneDrive\Documents\PowerShell\Microsoft.PowerShell_profile.ps1:40 char:8
+ if (! (Get-Process -Name 'ssh-agent')) {
+        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
+ CategoryInfo          : ObjectNotFound: (ssh-agent:String) [Get-Process], ProcessCommandException
+ FullyQualifiedErrorId : NoProcessFoundForGivenName,Microsoft.PowerShell.Commands.GetProcessCommand
```

PowerShellのprofileで`ssh-agent`のプロセスの存在を確認するようにしてたのだけど、Windowsの更新でサービスの自動実行が無効になってたようなのでこのエラーが...とりあえず今回は手動で自動実行するように変えて終わった。

あとどういうわけかわからないが、PC起動時のサービスの立ち上がりがめちゃくちゃ遅くなった気がする(体感)。PC起動後にすぐterminalを立ち上げると同じエラーがまだ出るから。根治させるにはprofileをいじらないといけないかな🤔

後述の問題に比べれば、こちらは楽しいアクシデント程度のものだ。

### Win10備え付けのOpenSSHに既知のバグが有るとか云々

```powershell
$ git remote show origin
warning: agent returned different signature type ssh-rsa (expected rsa-sha2-512)
...
```

↑この警告が常時出るようになった。むかつく💢

直さな...😭と思ってググってみると...

[ssh-agent: agent returned different signature type · Issue #1263 · PowerShell/Win32-OpenSSH](https://github.com/PowerShell/Win32-OpenSSH/issues/1263)

これの様子🤔

更に調べると、これマジIssueやなと思わざるを得ない... -> [Fixes to ssh-agent issues by manojampalam · Pull Request #366 · PowerShell/openssh-portable](https://github.com/PowerShell/openssh-portable/pull/366)

つまりはバグが直ったOpenSSH-Portbale入れないと解決しないってんでFAかな...キレそう❤

[Moving from Windows 1809's OpenSSH to OpenSSH Portable](https://blog.frankfu.com.au/2019/03/21/moving-from-windows-1809s-openssh-to-openssh-portable/)

幸いにも同じ障害を解消したブログがあったので助かる🙏

#### 処置する

[Moving from Windows 1809's OpenSSH to OpenSSH Portable](https://blog.frankfu.com.au/2019/03/21/moving-from-windows-1809s-openssh-to-openssh-portable/)

この記事にそのまま従えばいける。

Windowsに関わる操作はPowerShell Coreではできないので、PowerShell CoreとWindowsPowerShellを使い分けた(WindowsPowerShellで全部やればいいものを...😅)

Coreでできる範囲から始める。

```powershell
$ Get-Service -Name ssh-agent | Stop-Service
$ sc.exe delete ssh-agent
[SC] DeleteService SUCCESS
```

WindowsPowerShellでしかできない範囲

```powershell
$ Remove-WindowsCapability -Online -Name "OpenSSH.Client~~~~0.0.1.0"


Path          :
Online        : True
RestartNeeded : False



$  Remove-WindowsCapability -Online -Name "OpenSSH.Server~~~~0.0.1.0"


Path          :
Online        : True
RestartNeeded : False
```

Coreで続きをやる

```powershell
$ choco install openssh --package-parameters="/SSHAgentFeature"
```

```
Chocolatey v0.10.13
2 validations performed. 1 success(es), 1 warning(s), and 0 error(s).

Validation Warnings:
 - A pending system reboot request has been detected, however, this is
   being ignored due to the current Chocolatey configuration.  If you
   want to halt when this occurs, then either set the global feature
   using:
     choco feature enable -name=exitOnRebootDetected
   or pass the option --exit-when-reboot-detected.

Installing the following packages:
openssh
By installing you accept licenses for the packages.
Progress: Downloading openssh 7.9.0.1... 100%

openssh v7.9.0.1 [Approved]
openssh package files install completed. Performing other installation steps.
The package openssh wants to run 'chocolateyinstall.ps1'.
Note: If you don't run this script, the installation will fail.
Note: To confirm automatically next time, use '-y' or consider:
choco feature enable -n allowGlobalConfirmation
Do you want to run the script?([Y]es/[N]o/[P]rint): y

Running on: Windows 10 Home, (Core)
Windows Version: 10.0.17763

************************************************************************************
************************************************************************************
This package is a Universal Installer and can ALSO install Win32-OpenSSH on
Nano, Server Core, Docker Containers and more WITHOUT using Chocolatey.

See the following for more details:
https://github.com/DarwinJS/ChocoPackages/blob/master/openssh/readme.md
************************************************************************************
************************************************************************************

/SSHAgentFeature was used, including SSH Agent Service.
Extracting C:\ProgramData\chocolatey\lib\openssh\tools\OpenSSH-Win64.zip to C:\Users\takatoshi\AppData\Local\Temp\chocolatey\OpenSSHTemp...
C:\Users\takatoshi\AppData\Local\Temp\chocolatey\OpenSSHTemp
Source files are internal to the package, checksums are not required nor checked.
C:\Program Files\OpenSSH-Win64
C:\Program Files\OpenSSH-Win64\FixHostFilePermissions.ps1
C:\Program Files\OpenSSH-Win64\FixUserFilePermissions.ps1
C:\Program Files\OpenSSH-Win64\install-sshd.ps1
C:\Program Files\OpenSSH-Win64\libcrypto.dll
C:\Program Files\OpenSSH-Win64\openssh-events.man
C:\Program Files\OpenSSH-Win64\OpenSSHUtils.psd1
C:\Program Files\OpenSSH-Win64\OpenSSHUtils.psm1
C:\Program Files\OpenSSH-Win64\scp.exe
C:\Program Files\OpenSSH-Win64\sftp-server.exe
C:\Program Files\OpenSSH-Win64\sftp.exe
C:\Program Files\OpenSSH-Win64\ssh-add.exe
C:\Program Files\OpenSSH-Win64\ssh-agent.exe
C:\Program Files\OpenSSH-Win64\ssh-keygen.exe
C:\Program Files\OpenSSH-Win64\ssh-keyscan.exe
C:\Program Files\OpenSSH-Win64\ssh-shellhost.exe
C:\Program Files\OpenSSH-Win64\ssh.exe
C:\Program Files\OpenSSH-Win64\sshd.exe
C:\Program Files\OpenSSH-Win64\sshd_config_default
C:\Program Files\OpenSSH-Win64\uninstall-sshd.ps1
C:\Program Files\OpenSSH-Win64\Set-SSHDefaultShell.ps1
PATH environment variable does not have C:\Program Files\OpenSSH-Win64 in it. Adding...
Updating machine environment variable TERM from "" to ""
[SC] SetServiceObjectSecurity SUCCESS
Starting SSH-Agent...

NEW VERSIONS OF SSH EXES:

FileName                                         FileVersion
--------                                         -----------
C:\Program Files\OpenSSH-Win64\scp.exe           7.9.0.0
C:\Program Files\OpenSSH-Win64\sftp-server.exe   7.9.0.0
C:\Program Files\OpenSSH-Win64\sftp.exe          7.9.0.0
C:\Program Files\OpenSSH-Win64\ssh-add.exe       7.9.0.0
C:\Program Files\OpenSSH-Win64\ssh-agent.exe     7.9.0.0
C:\Program Files\OpenSSH-Win64\ssh-keygen.exe    7.9.0.0
C:\Program Files\OpenSSH-Win64\ssh-keyscan.exe   7.9.0.0
C:\Program Files\OpenSSH-Win64\ssh-shellhost.exe 7.9.0.0
C:\Program Files\OpenSSH-Win64\ssh.exe           7.9.0.0
C:\Program Files\OpenSSH-Win64\sshd.exe          7.9.0.0




WARNING: You must start a new prompt, or use the command 'refreshenv' (provided by your chocolatey install) to re-read the environment for the tools to be available in this shell session.
Environment Vars (like PATH) have changed. Close/reopen your shell to
 see the changes (or in powershell/cmd.exe just type `refreshenv`).
 The install of openssh was successful.
  Software installed to 'C:\Users\takatoshi\AppData\Local\Temp\chocolatey\OpenSSHTemp'

Chocolatey installed 1/1 packages.
 See the log for details (C:\ProgramData\chocolatey\logs\chocolatey.log).
```

```powershell
$ Get-Service ssh-agent

Status   Name               DisplayName
------   ----               -----------
Running  ssh-agent          ssh-agent
```

```powershell
$ git config --global core.sshCommand "'C:\Program Files\OpenSSH-Win64\ssh.exe'"
```

環境変数PATHを反映させてから、terminalでwariningが消えたのを確認

### 振り返り

Windowsのでかい更新の際はちゃんと注意して取り組まなあかんな🤔(めんどい)

あとこれまた既知の別件なのだけど、ver1809だとterminalのemojiの表示が中点とかに化けるので、これもなんとかしたいわ...

Cmder/ConEmuじゃなくてWindows自体の問題みたい。
