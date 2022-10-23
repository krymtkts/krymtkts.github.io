{:title "PowerShell の My Profile を掃除する"
:layout :post
:tags ["powershell"]}

PowerShell の profile を Gist で管理してるのだけど、この度掃除をした。

[My PowerShell profile.](https://gist.github.com/krymtkts/f8af667c32b16fc28a815243b316c5be)

片手間にできる範囲しかやらなかったので、使ってないモジュールと関数の取り除きが主。あとは順番をちょっと入れ替えてみたり、把握してなかった全貌を眺めてみた。

これらの取り除きついでに、供養がてら何のためにつかってたのかとか振り返ってみる。

### モジュール取り除き

[mklement0/ClipboardText: Universal clipboard text support for PowerShell, notably also in PowerShell Core (cross-platform) and Windows PowerShell v2-v4](https://github.com/mklement0/ClipboardText)

[ClipboardText](https://www.powershellgallery.com/packages/ClipboardText/0.1.8) というモジュール。コレなんで使ってたのかすら忘れてしまったけど、結構使ってた記憶がある。
`Set-ClipboardText` `Get-ClipboardText` が使えるようになる。
PowerShell Core を使い始めた頃はクリップボード操作の Cmdlet がなくてできなかったとかそんな理由だったか？

今やフツーに `Set-Clipboard` `Get-Clipboard` 使ってるので不要になったけどずっと消してなかった。今までありがとう、消した。

[poco](https://www.powershellgallery.com/packages/poco/1.1.0)も残ってるかな？と思ったが、そうでもなかった。
(変え漏れを除けば)意外にも [pocof](https://www.powershellgallery.com/packages/pocof/0.2.0-alpha) への変更は忘れてなかったらしい。

あとモジュールは跡形もなかったが、昔 [GoogleCloud](https://www.powershellgallery.com/packages/GoogleCloud/1.0.1.10) を使って勉強してたときの名残があったのでそれも消した。
このモジュールもう 4 年くらいメンテされてない様子、残念やけど多分重要でないんだろうな。

### 関数取り除き

#### 明らかな消し忘れ。 VirtualBox 関連

```powershell
function Start-VBoxMachine() {
    vboxmanage list vms | Select-Pocof -CaseSensitive | Out-String -Stream | Select-String -Pattern '\{(.+)\}' | ForEach-Object { vboxmanage startvm ($_.Matches[0].Groups['1'].Value) --type headless }
}

function Stop-VBoxMachine() {
    vboxmanage list runningvms | Select-Pocof -CaseSensitive | Out-String -Stream | Select-String -Pattern '\{(.+)\}' | ForEach-Object { vboxmanage controlvm ($_.Matches[0].Groups['1'].Value) poweroff }
}

function Get-RunningVBoxMachines() {
    vboxmanage list runningvms
}
```

こんなのがあった。VirtualBox の GUI を介さずに VM の起動・停止をするための奴らだったか確か。
前々職でコンテナ移行しきれてない秘伝の開発環境 VM がチラホラあって、それで使ってた記憶がある。あの VM ちゃん達は今も元気に VM してるのだろうか。
もはや使うこともないので消す。でも供養がてら Gist に残しておいた。

[krymtkts/scripts-for-virtualbox.ps1](https://gist.github.com/krymtkts/47f70697cbe006e81d7fd801e1e3b351)

#### なんだコレ。 `find` ぽい？やつ

```powershell
function find {
    [CmdletBinding()]
    param(
        [string]$path = '.',
        [Parameter(Mandatory = $True,
            ValueFromPipeline = $True)]
        [string[]]$name,
        [switch]$delete
    )

    begin {
    }

    process {
        foreach ($n in $Name) {
            if ($delete) {
                Get-ChildItem -Recurse -Path $path | Where-Object -Property Name -Like $n | Remove-Item
            }
            else {
                Get-ChildItem -Recurse -Path $path | Where-Object -Property Name -Like $n
            }
        }
    }

    end {
    }
}
```

なんか薄っすらと、 `Makefile` から `find` 呼ぶときに何のシェルか考えるの面倒で PowerShell で `find` 作ろうとしてた覚えがあるのだけど、それをいつやってたのかわからない。いま使ってもないのでなんでここにあるのか不明。
消す。

#### 何故 profile に足した？クソデカテキストファイルを作る関数

```powershell
function New-TextFile {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [string]
        $Name,
        [Parameter()]
        [long]
        $Byte = [Math]::Pow(1024, 3),
        [Parameter()]
        [int]
        $Basis = [Math]::Pow(1024, 2)
    )
    begin {
        if (Test-Path $Name) {
            Write-Error 'overrides currently not supported.'
            return
        }
        $Remains = $Byte % $Basis
        $Per = $Byte / $Basis
    }
    process {
        1..$Per | ForEach-Object { 'x' * $Basis | Add-Content $Name -Encoding ascii -NoNewline }
        if ($Remains -ne 0) {
            'x' * $Remains | Add-Content $Name -Encoding ascii -NoNewline
        }
    }
}
```

前に書いた [PowerShell でクソデカテキストファイルを作る](/posts/2022-03-13-create-huge-text-file-in-pwsh) の関数版だと思われる。
わざわざ profile に入れておいたのはなんでだろう。スクリプトにするだけで良いのでは...過去の自分に問いたい。消した。

#### `psake` の auto completer 壊れてるで...

あと今回の掃除を通して `psake` の auto completer 壊れてるやんと言うのに気づいたので、直した。

`Invoke-psake` の存在チェックをしてあったら `Register-ArgumentCompleter` するようにしてた。
けど、どっかのタイミングから `invoke-psake` が `invoke-cpsake` になってて、 auto completer が登録されなくなってた。

auto completer 周り、 `aws_completer` で `Register-ArgumentCompleter` 設定するとこも、 AWS CLI 1 の頃から使ってたのもあり AWS CLI 2 では要らなくなった部分がある(良くないけど PyPI からモジュール取ってたときの名残)。
今回は対応を先送りにした。

### おわり

そこまで消すものなかったな、というのが率直な感想だが、掃除してないな～という profile だった。

コマンドの存在をチェックしてから関数とか auto completer 登録するみたいなの増えてるので、この辺共通化したいなあ。
でも共通化しだすとファイル分割もしたくなり、結局のところ今の Gist から取ってくるの終焉に向かうのでは...と思った。

なんかより良い(楽な) PowerShell の Profile 管理手段ないかなー。
