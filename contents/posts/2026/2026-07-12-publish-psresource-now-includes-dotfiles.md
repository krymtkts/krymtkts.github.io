---
title: "`Publish-PSResource` が dotfiles を含めるようになってた"
tags: ["powershell", "dotnet"]
---

依然 [F# 10.1 から assembly version の minor version が上がる](/posts/2026-06-28-writing-cmdline-predictor-in-fsharp-pt9.html)ようになったと触れた。
それに伴いようやく [krymtkts/pocof](https://github.com/krymtkts/pocof) と [krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) も 10.1 の `FSharp.Core.dll` にした版をリリースした。

そこで気づいたのだけど、 [`Publish-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/publish-psresource?view=powershellget-3.x) の挙動が変わってるぽい。しかも結構前から。
何が変わったかと言うと `.gitignore` や `.gitkeep` といった dotfiles はコレまで除外されていたのに、まとめて publish されるようになってる。
これまでの除外される挙動が便利だったのでその挙動を利用してたが、今回 [PowerShell Gallery](https://www.powershellgallery.com/) に公開した版には dotfiles が含まれており、かっこ悪くなってしまった。
v1.2.0-preview4 からなので、結構前(2025-11-08)から変わってる。

[Release Release of Microsoft.PowerShell.PSResourceGet v1.2.0-preview4 · PowerShell/PSResourceGet](https://github.com/PowerShell/PSResourceGet/releases/tag/v1.2.0-preview4)

> Fix Compress-PSResource ignoring .gitkeep and other dotfiles by @Copilot in [#1889](https://github.com/PowerShell/PSResourceGet/pull/1889)

元は NuGet の既定の packing に従ってたので、除外されてたらしい。
今は [NuGet CLI の `-NoDefaultExcludes` option](https://learn.microsoft.com/en-us/nuget/reference/cli-reference/cli-ref-pack#options) に相当する flag を立てるようになって全部含まれる様になった。
`Publish-PSResource` は内部的に [`Compress-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/compress-psresource?view=powershellget-3.x) を呼ぶので、この影響を受けてしまった。

個人的に、 GitHub repo で利用する directory は明示的に構成管理されているのが好きで、そのために `.gitignore` や `.gitkeep` で管理下に置いてる。
今回のケースだと、 F# で書いてた PowerShell Module で、 publish するための directory をそのように構成管理に含めていた。
その結果 `.gitignore` や `.gitkeep` がまとめて公開されてしまった形になる。
先月 [krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) を公開したときはこの挙動にハマらなかったので、そのときは古い PSResourceGet を使ってたのかな。
そこはちょっと定かでない。

issue 的にはこの辺を解消する目的だったぽい。

- [Save-PSResource & Install-PSResource do not include empty files & folders · Issue #1819 · PowerShell/PSResourceGet](https://github.com/PowerShell/PSResourceGet/issues/1819)
- [Compress-PSResource ignores files and folders · Issue #1882 · PowerShell/PSResourceGet](https://github.com/PowerShell/PSResourceGet/issues/1882)

bugfix だが、既存の publish の挙動が変わってしまうのは、好ましくない気がするな。
bug が仕様になるというやつなのでは。
でも、まだ世の中でこの件に触れてるヒトはいないみたいだし、罠にハマったのはわたしだけなのだろうか。

結局、今回の変更でこれまでと同じ管理方法を続けると、 PowerShell Gallery へのリリースで余計な clean up が必要になるため、諦めた。
[`dotnet publish`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) すれば directory は自動作成されるので、自動作成される前提で directory が存在しなくても psake の task がコケないように調整して完了。
[krymtkts/pocof#467](https://github.com/krymtkts/pocof/pull/467), [krymtkts/SnippetPredictor#118](https://github.com/krymtkts/SnippetPredictor/pull/118), [krymtkts/PSGameOfLife#89](https://github.com/krymtkts/PSGameOfLife/pull/89) で修正。

これから自分の PowerShell module では作業 directory は自動作成で repo 管理しない方に統一していくか。
これまでも [krymtkts/pslrm](https://github.com/krymtkts/pslrm) のような純 PowerShell の Module は build がないから自前で publish 用の directory を作成するようにしてたし。
時代の流れか。
