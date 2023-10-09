---
title: "pocoで捗る日常生活"
tags:  ["powershell", "poco"]
---

[以前ちょっとだけ触れたpoco](/posts/2019-06-23-poco.html)を使いだしてから、よく使うディレクトリへの移動とか、`PSReadline`のHistoryからよく使うコマンドを引っ張り出すとかの、インタラクティブなコマンドが作りやすくとて捗っている。

[Gistにあげたプロファイル](https://gist.github.com/krymtkts/f8af667c32b16fc28a815243b316c5be)にまるっと書いているのだけど、ココではコメントも添えて書いておく。

### `PSReadline`のhistoryを見る/実行する

作った関数は以下の通り。似たようなモジュールはPSGalleryに何個かあるけど、pocoで書きたかったので自作した次第。

```powershell
function Show-ReadLineHistory() {
    Get-Content -Path (Get-PSReadlineOption).HistorySavePath | Select-Object -Unique | Select-Poco -CaseSensitive
}
Set-Alias pghy Show-ReadLineHistory -Option AllScope

function Invoke-ReadLineHistory() {
    Show-ReadLineHistory | Select-Object -First 1 | Invoke-Expression
}
Set-Alias pihy Invoke-ReadLineHistory -Option AllScope
```

途中で`Select-Object -Unique`を挟んでいるのは、わたしがやたら同じコマンドを繰り返すので重複を省くため😅

`Select-Object -First 1`はpoco自体の機能不足(選択機能がない)のを補うためである😭

ちなみにPowerShellのHistoryが謎な件についてはソースはこの辺。

- [PowerShellの完全な履歴を取得する - Qiita](https://qiita.com/yuta0801/items/ad0cf608144fb1546e54)
    - [How can I see the command history across all PowerShell sessions in Windows Server 2016? - Stack Overflow](https://stackoverflow.com/questions/44104043/how-can-i-see-the-command-history-across-all-powershell-sessions-in-windows-serv)

`Get-History`は現在のセッションの情報しか取らない。でも実は`(Get-PSREeadlineOption).HistorySavePath`に保存されている、という話🤔

`~\AppData\Roaming\Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt`な感じでテキストファイルに保存されている。

ちなみに本来の`Get-History`実行結果のような履歴ごとにIDを振るのはやってない。

### お気に入りのディレクトリへ移動する

これはパクリ。仕事で使うリポジトリはghqを使わないで決められたディレクトリへcloneすることを期待されてたりするので、そのときにこういう任意のディレクトリへの移動ができるやつが重宝してる。

[Big Sky :: Windows のコマンドプロンプトを10倍便利にするコマンド「peco」](https://mattn.kaoriya.net/software/peco.htm)

あと編集機能は未実装。わたしの用途では編集よりも削除機能のほうが良いかなと思ったりしてるところ🤔

```powershell
function Set-SelectedLocation {
    param(
        [ValidateSet("Add", "Move", "Edit")]$Mode = "Move",
        [string]$Location
    )
    switch ($Mode) {
        "Add" {
            if ($Location) {
                Write-Output "$Location" | Out-File -Append -Encoding UTF8 "~/.poco-cd"
                break
            }
        }
        "Move" {
            Get-Content -Path "~/.poco-cd" | Select-Poco -CaseSensitive | Select-Object -First 1 | Set-Location
            break
        }
        "Edit" {
            break
        }
    }
}
Set-Alias pcd Set-SelectedLocation -Option AllScope
```

### 雑なまとめ

これらのツールをpocoの補完ツールとしてもうちと洗練してもいいかもしれんなあと思ったりしてる🤔
