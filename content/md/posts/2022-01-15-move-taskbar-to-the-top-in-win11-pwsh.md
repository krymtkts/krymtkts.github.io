{:title "Windows 11 のタスクバーを天に(PowerShell で)"
:layout :post
:tags ["powershell","windows"]}

Windows11 ではメニューからタスクバーを天に持ち上げれなくなったので、みんなレジストリを操作して実現している。手順はどこでも手に入るが、以下のページを参照した。

[How to Move the Taskbar to the Top in Windows 11 | Tom's Hardware](https://www.tomshardware.com/how-to/windows-11-taskbar-move-to-top)

1. `regedit` を開く
2. `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3` を開く
3. `Settings` を編集し保存する
   - 2 行目を `7A F4 00 00 03 00 00 00` -> `7A F4 00 00 01 00 00 00`にする
     - デフォルトの`03`が下、`01`が上というわけ
4. `explorer` を再起動する
   - コマンドプロンプトで
     1. `taskkill /f /im explorer.exe`
     2. `start explorer.exe`

これが PC 変わる度にやるのクソめんどいので、PowerShell でスクリプト化した。
そんなに機会はないけど一々覚えてないので先程のページを見にったりと、とにかくめんどい。

PowerShell 7.2.1 でやった。

```powershell
$ShowHex = {param ([array]$arr) ($arr | %{[System.Convert]::ToHexString($_)}) -join ' '}
$path = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\StuckRects3'
$key = 'Settings'
$org = Get-ItemProperty $path | Select-Object -ExpandProperty $key
$ShowHex.Invoke((,$org))

$new = @() + $org
$new[12] = 0x01
$ShowHex.Invoke((,$new))
Compare-Object $org $new

Set-ItemProperty $path -name $key -Value $new

Stop-Process -Name explorer -Force
## if explorer doesn't restart, start explorer manually.
# Start-Process -Name explorer
```

万が一失敗してたら `$org` で `Set-ItemProperty` して戻す必要があるので、成功(≒ 天にタスクバー)を確認するまで窓を閉じない方が良かろう。
ちゃんと期待の更新ができているか確認するために、レジストリの値を 16 進数に変換して標準出力までしちゃう。

[Gist はこちら](https://gist.github.com/krymtkts/ba83a0612bba84b5e8229d64e9d8681a)。

### おまけ

`taskkill` は PowerShell で言うところの何か調べたときのページ ↓

[What Is The PowerShell Equivalent Of Taskkill | PDQ.com](https://www.pdq.com/blog/what-is-the-powershell-equivalent-of-taskkill/)

> Okay, I'll be the first to admit it; the name is a little lackluster. Especially when compared to **TASKKILL!!!!!!** **Stop-Process** just doesn't carry the same hostile undertones Thankfully, Microsoft at least gave us **kill** as an alias, so we've got that going for us. Regardless, let's see if it still packs the same task-killing punch.

> さて、最初に断っておきますが、このネーミングは少し物足りないですね。特に**TASKKILL!!!!!!**と比較するとね。 ありがたいことに、Microsoft は少なくとも**kill**という別名をつけてくれました。 ともかく、同じようにタスクを殺すパンチをパックしているかどうか見てみましょう。

こんなん笑うわｗ
