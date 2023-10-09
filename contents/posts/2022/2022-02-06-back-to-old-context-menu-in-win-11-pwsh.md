---
title: "Windows11 のコンテキストメニューを前のんに戻す(PowerShell で)"
tags: ["powershell"]
---

Windows11 にてコンテキストメニューの UI が変わった。
しかし自身は普段コンテキストメニューを使わないのであまり気にならなかった。

のだが、たまたま 7z 圧縮されたファイルを渡されたことで、右クリックで 7zip で開けないのクソめんどい...と気になり始めた。
なので前のコンテキストメニューに戻す。PowerShell で。

レジストリを編集する必要があるらしい。情報元 ↓
[How to Get Full Context Menus in Windows 11 | Tom's Hardware](https://www.tomshardware.com/how-to/windows-11-classic-context-menus)

```powershell
$path = 'HKCU:\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32'
New-Item -Path $path -Force
Split-Path $path -Parent | Get-ChildItem
Set-ItemProperty -Path $path -Name '(Default)' -Value ''
Get-ItemProperty $path

Stop-Process -Name explorer -Force
```

Gist → [Return to the previous context menu in Windows 11.](https://gist.github.com/krymtkts/30af31454d510ce0c34cfeb2fefec072)

↓ キーの名前は case insensitive なのかーい！

```powershell
PS> Get-ItemProperty $path

(default)    :
PSPath       : Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\In
               procServer32
PSParentPath : Microsoft.PowerShell.Core\Registry::HKEY_CURRENT_USER\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}
PSChildName  : InprocServer32
PSDrive      : HKCU
PSProvider   : Microsoft.PowerShell.Core\Registry
```

終わり。
