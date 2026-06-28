---
title: "F# で command-line predictor を書いてる Part 9"
subtitle: "0.6.0"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.6.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.6.0) をリリースした。

2025 年末の [v0.5.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.5.0) 以来放置してた。
なんなら v0.5.0 のリリースも日記に書き忘れてた。

今回の v0.6.0 は CI や内部の改善しかなく、機能的には簡単な修正しかしてないが、それ自体も年初に実装されたものだ。
その後すっかり忘れていたのだけど、 [krymtkts/PSKeepAChangelogTools](https://github.com/krymtkts/PSKeepAChangelogTools) を導入しようとしてたときに気付いた。

ついでに [PowerShell Gallery](https://www.powershellgallery.com/) への公開や [GitHub Release](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases) の作成も GitHub Actions workflow にした。
今回のリリースは [GitHub Actions workflow](https://docs.github.com/en/actions/concepts/workflows-and-actions/workflows) で行うつもりだっが、 PowerShell Gallery への公開が [psake](https://github.com/psake/psake) の script の bug で出来なかった。
なので仕方なく手動で行った。次回再チャレンジする。
同じような workflow や script が repo 横断的に溜まってきて、そろそろ共通化やツール化が出来ないかなと考えてるけど、ユースケースを汎化すると複雑になるし、今は見送り中。
changelog くらいドンズバのユースケースなら楽なんだが。
また PowerShell Gallery への公開については、認証なしの third-party action ならいくつかあるみたいだけど、依存関係がリスクになりかねないので採用してない。
コレを言い出すと [krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action)  も同類なのでアレで悩ましいところだが。

あと今回のリリースで多分初めて `FSharp.Core.dll` の minor の assembly version が変わって、 DLL が読み込めない問題健在化した。

```powershell
Import-Module: Could not load file or assembly 'FSharp.Core, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The
located assembly's manifest definition does not match the assembly reference. (0x80131040)
```

これまでは minor version が進んでも assembly version が変わったことはなかった記憶だが、 2026 の 10.1 系から変わってるみたい。
F# の文書などからは変わってることは読み解けなかったけど、なんか方針が変わったのかな。ここは今後重点的に調べないといけない。
一応過去の .NET での F# の assembly version を確認してみたけど、 10.1 で初めて minor version が上がってるのは確かだ。

```powershell
> dotnet --list-sdks | % {
>   $_ -match '^(\d+\.\d+\.\d+)\s+\[(.+)\]$' | Out-Null
>   "$($Matches[2])\$($Matches[1])"
> } | % {
>   $loc = "${_}\FSharp\FSharp.Core.dll"
>   [pscustomobject]@{
>     Location=$loc;
>     FullName = [Reflection.AssemblyName]::GetAssemblyName($loc).FullName
>   }
> } | Format-List

# Location : C:\Program Files\dotnet\sdk\8.0.422\FSharp\FSharp.Core.dll
# FullName : FSharp.Core, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#
# Location : C:\Program Files\dotnet\sdk\9.0.315\FSharp\FSharp.Core.dll
# FullName : FSharp.Core, Version=9.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#
# Location : C:\Program Files\dotnet\sdk\10.0.109\FSharp\FSharp.Core.dll
# FullName : FSharp.Core, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#
# Location : C:\Program Files\dotnet\sdk\10.0.204\FSharp\FSharp.Core.dll
# FullName : FSharp.Core, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
#
# Location : C:\Program Files\dotnet\sdk\10.0.301\FSharp\FSharp.Core.dll
# FullName : FSharp.Core, Version=10.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
```

なので今後は、対応した `FSharp.Core.dll` の version を F# PowerShell Module 間で統一しないと `Import-Module` が成功しなくなってる。
今は、 F# のコレまでの assembly version も踏まえて、配布物の完全性を重視して `FSharp.Core.dll` を同梱して配布してるけど、これだと 10.1 以降相性が悪い。
F# PowerShell Module の特性的に PowerShell に load された同名 assembly は version が一致しないと今回のようにエラーになるからだ。
他の手段としては、動的に互換性がある version の `FSharp.Core.dll` が読み込まれてたら～みたいなチェックを自前実装して load できる。
ただしこれは結構面倒だしテスト対象の組み合わせが爆発するので採用しにくい。
たとえ動的な assembly loading を作ったとて、他の作者の F# PowerShell Module が競合するかも知れないし、現状は version を合わせていくしかなさげ。

ひとまず今やらないといけないのは、自分の日常的な利用で最新版が使えないことは問題アリなので、 10.1 系に揃えた版を急ぎ release すること。
ただ今回の SnippetPredictor みたいに changelog と release 自動化も一気に組み込みたいので、結局ちまちま進めることになる想定。
