{:title "PowerShell の特殊なモジュールを更新する方法"
:layout :post
:tags ["powershell"]}

`PSReadLine` のような特殊な PowerShell モジュールを更新する術として、以下の方法を使うようにしている。

- 管理者権限で起動したコマンドプロンプトから
- 非対話モードで
- PSReadline を読み込んでるプロファイルを読み込まずに
- プレリリース許可＆全ユーザ＆サイドバイサイド でインストール

```bat
REM 、かつ、-NonInteractive でPowerShell Coreを実行
pwsh -NonInteractive -NoProfile -Command "Install-Module PSReadLine -AllowPrerelease -Scope AllUsers -Force"
```

`Remove-Module` さえちゃんとできてたら更新できると思うんやけど、プロファイルとの組み合わせで意図せず`Import-Module`してしまい、しょっちゅうエラーしてしまうので上記手順が楽。
ほんとは pwsh 内からいい感じに処理できればよいのだけどトラシューの時間をこんなとこに割きたくない関係で、更新の都度初手一発でうまくいく手順をやりがち。

エラーになりがちな奴ら。ワイの profile が依存してる関係でエラーになりがち。

- PSReadLine
- PowerShellGet
- posh-git
- Pester

[about_Pwsh - PowerShell | Microsoft Docs](https://docs.microsoft.com/ja-jp/powershell/module/microsoft.powershell.core/about/about_pwsh?view=powershell-7.1)
