---
title: "PowerShell のツールチェーン"
tags: ["powershell"]
---

今更読んだ ↓

[PowerShellGet 3.0 Preview 11 Release - PowerShell Team](https://devblogs.microsoft.com/powershell/powershellget-3-0-preview-11-release/#features-to-expect-in-coming-preview-releases)

モジュールの依存性管理がくるっぽ。

これを機に NuGet のバージョンレンジ記法を学ばないといけないかな。PowerShell 使うけど NuGet と直接的な縁ないので触れずに来た。
[NuGet Package Version Reference | Microsoft Docs](https://docs.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges-and-wildcards)

それはさておき、先述の PowerShellGet のネタはまだ正式なものでもなく、プレビュー機能が来てるわけでもない。だから今はまだ従来の術を使うのが良いだろうと考えている。
真面目に PowerShell 開発したことがないので、その辺の知ってるモジュールを棚卸しし、調べ直した。

ここに書いたあるような内容は、PowerShell で書かれているアプリの GitHub repo を見たらだいたい出てくるのじゃないだろうか。
わたしの場合は、[jasonmarcher/poco](https://github.com/jasonmarcher/poco)で初めて`psakefile.ps1`を見つけてそこから世界へ踏み入れた感じ。

そして、この記事をまとめているときに「ああそういえば Awesome 〇〇ってあったなー」と思いググると、PowerShell 版も見つかったので置いておく。この記事に書いたツールチェーンは全部 Awesome の方に載ってた...

[janikvonrotz/awesome-powershell: A curated list of delightful PowerShell modules and resources](https://github.com/janikvonrotz/awesome-powershell)

---

### ビルド

PowerShell はスクリプトなのでコンパイルはないが、静的解析・テスト・パッケージング等のことをひっくるめて、ここではビルドと呼ぶことにする。

やはり[psake](https://github.com/psake) が有名でしょう。[^1]

[psake/psake](https://github.com/psake/psake) は基盤のようなもので、汎用的なビルドタスクなんかは[psake/PowerShellBuild](https://github.com/psake/PowerShellBuild)に定義されている。
肝心の[psake/psake](https://github.com/psake/psake)のビルドには自身ではなく[RamblingCookieMonster/BuildHelpers](https://github.com/RamblingCookieMonster/BuildHelpers)が使われているというのがこれまたややこしい。
[RamblingCookieMonster/BuildHelpers](https://github.com/RamblingCookieMonster/BuildHelpers)それ自身は、CI/CD シナリオで使えるヘルパーだぜ？と自称しているだけあり、その用途(GitHub Actions)で使われている

因みに[Invoke-Build](https://github.com/nightroman/Invoke-Build)なんていうのもいて、これは使ったことない。GitHub のグラフはこちらの方が比較的アクティブかな。
README.md 見る限り[psake](https://github.com/psake/psake)よりも使いやすいぜ！って書いてあるので、何か大変なことでもあったのかなと勘ぐってしまう。
わたしはまだ真面目に使い込めていないこともあり、[psake](https://github.com/psake/psake)の闇を知らないだけかも知れない。

[^1]: と書いたがどう考えても有名じゃない。わたしも知ったの 2,3 年くらい前。最近は repo のグラフもほとんど息してなく見える。「枯れてる」のかも知れんけど。

### 依存性管理

モジュールあるところに依存性管理あり。PowerShell も例に漏れずある。
[RamblingCookieMonster/PSDepend](https://github.com/RamblingCookieMonster/PSDepend)

話は変わるが、[RamblingCookieMonster (Warren Frame)](https://github.com/RamblingCookieMonster)さんは他にも PowerShell のツールを色々書かれている。
[RamblingCookieMonster/PSDeploy](https://github.com/RamblingCookieMonster/PSDeploy)だったり、わたしも最近所用で使った[RamblingCookieMonster/PSSlack](https://github.com/RamblingCookieMonster/PSSlack)だったり(最新の API に対応してないけど)。

### テスト/静的解析

[pester/Pester](https://github.com/pester/Pester) しか知らん。BDD スタイルでクールよね。
[PowerShell/PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) しか知らん。
いずれも開発もアクティブだし唯一無二か？

---

Awesome ~ を見つけたことだし、他にも色々見てみるか。
