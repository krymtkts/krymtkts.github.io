---
title: "F# の PowerShell Module の Template を作りたい Part 4"
subtitle: "PowerShell の project-base なモジュール管理"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/FsPowerShellTemplate](https://github.com/krymtkts/FsPowerShellTemplate) の開発をした。
task runner の task を拡充をした。 unit/end-to-end testing, documentation, PowerShell Gallery への publish 等。
あと coverage も足しておきたいなと考えてるが、まだ着手できていない。

[Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli) に対応した [Coverlet](https://github.com/coverlet-coverage/coverlet) ができたのでそれを使おうと考えてる。
けど、他の v8 系は出ているけど MTP 版だけ NuGet に公開する権限の問題でまだ出てないらしい。以下の issue にそれが書かれている。

[NuGet "coverlet.MTP" package does not exist? · Issue #1816 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/issues/1816#issuecomment-3903692885)

上記以外は概ね揃えたつもりなのだけど、最後に PowerShell Module のモジュール管理を project-local でできないことが気になっている。
[PSResourceGet](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/?view=powershellget-3.x) だと project local に依存関係を管理する仕組みがないはず。
module の install は、 machine local か current user local が対象になる。
これは開発環境では project 毎に複数の module version があると支障がある。
`$PSModulePath` を指定していても machine や user の module を拾ってしう。
(container で分けろという世界観なのかも知れないが)

今できる方法としては、わたしが作ってる他の PowerShell Module 同様に、 version を指定せずに使う。
あるいは、愚直に script を書いて利用 version の固定をやればいいの。
だが汎用性に欠けるし、そもそも開発環境でも CI でも同じ方法で管理できるのが一番うれしい。
要は lockfile base で PowerShell Module を restore できるようなのが欲しい。

PowerShell のモジュール依存関係の管理として、昔から [PSDepend](https://github.com/RamblingCookieMonster/PSDepend) がある。
長らく開発停止してるみたい。
わたしは使ったことがないが、見た感じでは lockfile がなくようなので求める機能がなさそう。
仮に動いても、見たところ PowerShell Gallery 以外の source にも対応しているから結構多機能なのでこんなに重厚なやつが欲しいのではないかなという感じ。

[`Install-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x) には [`-RequiredResourceFile`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x#-requiredresourcefile), [`-RequiredResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x#-requiredresource) という option がある。
この option を使えば、 [`psd1`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_data_files?view=powershell-7.5) ファイルに exact version を記入することで擬似的に lockfile として使えそう。
でも install で自動生成されないし、先に上げた user と machine の汚染は防げないから、隔離環境でしか使えない。
なら [`Save-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/save-psresource?view=powershellget-3.x) が requirements-base で動かせたらいいのだけど、残念ながらできない。

ないのであればということで、いま PSResourceGet をベースとして、上記の project-local な依存管理を提供する薄い wrapper module を試作している。
仮に名前は PowerShell Local Resource Manager ということで pslrm としている。
今の時代は略語が多すぎて何を考えても重複するリスクがあって、これも名前が衝突してるが、仕方ない。
今はまだ実感として使える感じがなくて、わたしには珍しく private repository にして開発してる。歴史直したりするしな。
pslrm が多少愚直でも求める機能を提供できるようになったら、 template に組み込んで使うつもり。

将来的に PSResourceGet がその機能を持つこともあるかも知れないが、今はないので多分価値があるねんよな。
PSResourceGet で machine や user から分離した module management ができるようになったら、薄い wrapper module は要らなくなるはず。
付け焼き刃の出番をなくすためにも、本体の進化を期待するばかり。

当面は pslrm 試作に時間を割くので、 template の方は手を付けないつもり。
その間に MTP 版の Coverlet が NuGet に登録されたらいいなという淡い期待。
