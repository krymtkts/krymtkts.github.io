---
title: "AWS Tools for PowerShell のビルド素振り"
tags: ["powershell","aws"]
---

今年は積極的に？ AWS Tools for PowerShell を使ってることもあって、また Issue 書くことあるかなー、なんなら PR 書いて貢献した方がいいよなと考えるに至り、 [aws/aws-tools-for-powershell](https://github.com/aws/aws-tools-for-powershell) のビルドを素振りしてみた。
残念ながら、 contribution 関連でビルドの方法がレクチュアされてる文書とかなかったので、手探りでやってる。

まず、サービス毎の膨大なプロジェクトがあるし、試しに単体のプロジェクトをビルドしたかったのだけど、関連付く DLL が無くてビルドできなかった。 `Amazon.Runtime` とかいうやつ？ この repo 内のどれかのプロジェクトでビルドされるんだろうけど。

`buildtools/` 配下を見ると、どうも CI が CodeBuild らしい(当然のごとく)。この `buildtools/ci.buildspec.yml` の中に記載されている全体のビルドで使ってるプロジェクトなら上手くいきそうな雰囲気がしたので、これでフルビルドを試した。

```powershell
dotnet --version
# 6.0.302
dotnet build .\buildtools\build.proj
# めちゃくちゃ大量のビルドログが出力される...
```

ところがこのフルビルドしだしたらクッソ重い...ビルドが終わらない。
わたしの Razer blade stealth 2018 モデルだと CPU 使用率が天井に貼り付いて、過去にない位にヒートアップしてて心配な気持ちにさせてくれた。

結果、 41 分かかってなんとかビルドに成功した。

で、出力結果を見たら改行コードが変わってしまったみたいでエグい差分が発生してしまった。
元コードは全部 `LF` なのだけど、 Windows でのビルドによって `CRLF` になってしまったみたい。CodeBuild では .NET Core 3.1 でビルドしてるから .NET の関連ツールを使ってても `LF` になるんやろが、こちとら Windows 。
どこで変わったか調査して再発を防ぎたいけど、別の機会にする。

これで基盤的な DLL は生成されたであろうし、サービス個別のプロジェクトのビルドを試すと、うまくいった。

```powershell
dotnet build .\modules\AWSPowerShell\Cmdlets\Lambda\AWS.Tools.Lambda.csproj
# ...中略...
#
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
#
# Time Elapsed 00:00:01.81
```

テストは PowerShell で Cmdlet に対して行う Pester のやつがあるのみで、かつこれらの内容を見てると当然のごとく AWS リソースがある前提。なので、多分個人では金がかかってできないやつ。デプロイ用の CFn とかもないし。
PR のワークフローで対処してんのかなー。

想定以上に時間がかかったけど、とりあえずなんか修正したいときに手元でビルドするための知識は貯まった。
フルビルドで改行コードが書き換わってしまう、サービス単体のビルドに必要な最小限の依存関係を理解する、辺りが宿題かな。
