{:title "Windows のタスクを操作する(PowerShell で)"
:layout :post
:tags ["powershell","windows"]}

決まった時間までにやらなければいけないことがあるとする。それを人間力でカバーするのは、それなりに資源の浪費になるので自動化したいとする。
最近の Windows ならタスクスケジューラで直に書いてもいいけど、操作めんどすぎるので普通に考えたらスクリプトにするでしょう。
これを Windows 11 と PowerShell 7.2.1 でやる。

それでは [ScheduledTasks Module](https://docs.microsoft.com/en-us/powershell/module/scheduledtasks/?view=windowsserver2019-ps) を使う。
このモジュールは Window なら以下のシステムフォルダにひっそりと存在する。令和のこの時代になんつー古い(v1)話なんや、とは思う。

```powershell
Get-Module -Name ScheduledTasks -ListAvailable

    Directory: C:\WINDOWS\system32\WindowsPowerShell\v1.0\Modules

ModuleType Version    PreRelease Name                                PSEdition ExportedCommands
---------- -------    ---------- ----                                --------- ----------------
Manifest   1.0.0.0               ScheduledTasks                      Core,Desk {Get-ScheduledTask, Set-ScheduledTask, Register-Sche…
```

WindowsPowerShell(v5.1)なら [PSScheduledJob Module](https://docs.microsoft.com/en-us/powershell/module/psscheduledjob/?view=powershell-5.1) が使えるが、 PowerShell 7 では使えない(`Import-Module` もできない)ので、 `ScheduledTask` 一択かと。

ではいくつかのレシピを以下に記す。

### 日次で指定時間に実行(人間味のあるズレを添えて)

```powershell
$pwsh = (get-command pwsh).Source
$action = New-ScheduledTaskAction -Execute $pwsh -Argument '-NonInteractive -Command "Invoke-MyCommand"'
$trigger = New-ScheduledTaskTrigger -Daily -At 7:46 -RandomDelay 00:10
$task = New-ScheduledTask -Action $action -Trigger $trigger
Register-ScheduledTask -InputObject $task -TaskName 'morning-action'
```

### 指定時間に起動して実行待ちするタスク ≒ 実行許諾の通知的なもの

```powershell
$pwsh = (get-command pwsh).Source
$action = New-ScheduledTaskAction -Execute $pwsh -Argument '-Command "{Read-Host `"press key`" | Out-Null; Invoke-MyCommand}.Invoke()"'
$trigger = New-ScheduledTaskTrigger -Daily -At 17:00
$task = New-ScheduledTask -Action $action -Trigger $trigger
Register-ScheduledTask -InputObject $task -TaskName 'evening-action'
```

### 単発で指定時間に実行(人間味のあるズレを添えて)

```powershell
$pwsh = (get-command pwsh).Source
$action = New-ScheduledTaskAction -Execute $pwsh -Argument '-NonInteractive -Command "Invoke-MyCommand"'
$jitter = (Get-Random -Minimum 30 -Maximum (60*5))
$timing = (Get-Date '2022-01-30 17:05').AddSeconds($jitter)
$trigger = New-ScheduledTaskTrigger -At $timing -Once
$setting = New-ScheduledTaskSettingsSet -DeleteExpiredTaskAfter 00:00:10
$task = New-ScheduledTask -Action $action -Trigger $trigger -Settings $setting | `
    %{ $_.Triggers[0].EndBoundary = $timing.AddMinutes(1).ToString('s'); $_}
Register-ScheduledTask -InputObject $task -TaskName 'single-action'
```

このレシピではズレの算出は自前で行っている。`New-ScheduledTaskTrigger` に `-RandomDelay` を指定しておくのもアリだが、単発であるし具体的にいつ実行されるかがわかる方が好ましいかと考えた。

単発タスクなので、実行後にタスクを廃棄したいとする。その場合は例のように `-Settings` で自動削除の設定を有効にする必要がある。
同時に、タスクの期限切れの日時も指定する必要があるが、これはコマンドレットのオプションでは設定できない。直接オブジェクトに代入することで設定する(各 `Triggers` で `EndBoundary` を設定)。

参照: [Powershell v4. Create remote task scheduler task set to expire and delete - Stack Overflow](https://stackoverflow.com/questions/29337135/powershell-v4-create-remote-task-scheduler-task-set-to-expire-and-delete/35777432#35777432)

### 注意

いくつかの注意点がある。

#### 実行ファイルは絶対パス指定

実行ファイルは絶パス(絶対パスのわかりにくい略称)指定じゃないといけない。コマンドプロンプトでパスを通していたとしても、 `pwsh` とかだとタスクスケジューラちゃんは実行ファイルを見つけられない。

#### `pwsh` の引数 `Command` はダブルクォートで書く

`New-ScheduledTaskAction` の引数 `Argument` に、 `pwsh` に渡す引数を定義する。
この時、引数 `Command` に渡す文字列はダブルクォートで書くこと。
コマンドプロンプトで試せばわかるが、ダブルクォートはコマンドプロンプトで文字列として解釈され `pwsh` に渡るのに対し、シングルクォートは文字列と解釈されないそのままが渡されている様子。

```cmd
C:\Windows\system32>pwsh -NonInteractive -Command "Write-Host 123"
123

C:\Windows\system32>pwsh -NonInteractive -Command 'Write-Host 123'
Write-Host 123
```

この挙動のソースはコレ ↓ くらいしか見つからんかった。オフィシャルな情報はないのかな。あったら是非引用したい。(コマンドプロンプト界の常識過ぎるテーマなのか？)

[cmd - What does single-quoting do in Windows batch files? - Stack Overflow](https://stackoverflow.com/questions/24173825/what-does-single-quoting-do-in-windows-batch-files/24181667#24181667)

### `Read-Host` するからにゃぁ Interactive で `pwsh` を起動しろよな

`Read-Host` する例なのに、間違って `-NoInteractive` をつけてしまうと、このようにおもしろエラーを頂戴するのでご注意。

```powershell
C:\Windows\system32>pwsh -NonInteractive -Command "{Read-Host `"press key`" | Out-Null; Invoke-MyCommand}.Invoke()"
MethodInvocationException: Exception calling "Invoke" with "0" argument(s): "PowerShell is in NonInteractive mode. Read and Prompt functionality is not available."
```
