---
title: "PSResourceGet メモ"
tags: ["powershell"]
---

[PSResourceGet](https://github.com/PowerShell/PSResourceGet) を使い始めてまだちょっとだが、明らかに変わったことがある。
これ PowerShellGet v2 より圧倒的に速いな。
PowerShellGet v3 時代からインストールするようにはしてたけど日々使ってなかったから気づいてなかった。

ただ [開発環境をプチ移行する](/posts/2023-07-09-migrate-dev-environment#PSResourceGet.html) にも追記したように、 v3 には `Get-PSResource` みたいなややこしい Cmdlet もいる。
PSResourceGet を入れてるなら v3 の方は消しとくのが無難。

なのでこの度普段定期的にしかやってなかった古いモジュールの整理をした。既に誰でもやってそうだが習慣的に行えるよう関数にした。
なおこの関数、モジュール依存関係を壊すようなのがあればエラーになるだろう。
それを回避したければ `-SkipDependencyCheck` 足してもいいかもだが、とりあえず壊れたときめんどいのでやめた(壊れたモジュール使うまで気づけなさそう)。

```powershell
function Uninstall-OutdatedModules {
    [CmdletBinding(SupportsShouldProcess)]
    param()
    Get-InstalledPSResource -Scope AllUsers | Group-Object -Property Name | Where-Object -Property Count -GT 1 | ForEach-Object {
        $_.Group | Sort-Object -Property Version -Descending | Select-Object -Skip 1
    } | Uninstall-PSResource -Scope AllUsers
}
```

上記の関数の通り、 `Get-InstalledPSResource` は複数 ver 入ってるとそのままあるだけ返してくれる。
v2 時代は `Get-Module` しないとダメだったのは [以前書いた PowerShell モジュールの大掃除日記](/posts/2022-11-12-clean-up-pwsh-modules.html) でのこと。時代は変わったな。

また、こういう処理も v2 であれば `Get-InstalledModule` は結構遅かったのが、 PSResourceGet になってとても速くなった。

AllUsers の scope に 30 位のモジュールがある。
実行するのは自環境、 Razer Blade Stealth 13 Intel(R) Core(TM) i7-8550U CPU @ 1.80GHz 2.00 GHz, Windows 11 22H2, PowerShell 7.3.6 にて。
初回の実行で v2 だと 1 秒くらい、 PSResourceGet だと 50ms 未満。
ショボマシンでこの差は結構でかいな。何らかのローカルなデータストアにキャッシュでもしてんのかな PSResourceGet は。

MS の PowerShell Team の blog 読んだらここに書いたようなこと書いてたかもだが、改めて自分で試してみて新しいツールの利用が身体化されてる感じはするな。

あと [pocof](https://github.com/krymtkts/pocof/) で気になってる `Publish-PSResource` のテストモジュール使った動作確認は未だやってない(めんどくて)。
のでそろそろ手を付けないといけないけど、 web のどっかで情報出てきたらうれしいのだけど。

そういや勤怠スクリプト類などの他の PowerShell モジュールのこと忘れてた。今年の秋には PSResourceGet 対応しとかないとあかんか。
