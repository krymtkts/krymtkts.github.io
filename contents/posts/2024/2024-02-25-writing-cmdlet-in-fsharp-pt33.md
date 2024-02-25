---
title: "F# でコマンドレットを書いてる pt.33"
tags: ["fsharp", "powershell", "psscriptanalyzer", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

[FSharpLint](https://fsprojects.github.io/FSharpLint/) を入れたのでついでに [Fantomas](https://fsprojects.github.io/fantomas/) も入れてみた。 [#145](https://github.com/krymtkts/pocof/pull/145)

これまで VS Code の [Ionide](https://ionide.io/) での整形しかやってなかったが、それを機械的に CI でもチェックしようかなという考えだ。
実際 fantomas 入れてみたことで Ionide でファイル保存したときの整形も変わったので、なんか進化したのかも知れん。
pocof での fantomas の設定は以下の通りほとんどない(以下もデフォルト値でほぼ要らんレベル)。

```ini
[*.{fs,fsx,fsi}]
indent_size = 4
end_of_line = lf
```

個人的にはあまりコードフォーマットに独自性をもたせることは興味がなく、誰が書いても同じになるのが考えることも減って楽でいいと思っている。
なので標準的な何かにさえ準拠してそれが自動的に適用されていればよい。

F# のコードフォーマットに関しては以下のスタイルガイドのページがある。
[F# code formatting guidelines - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/style-guide/formatting)

> The Fantomas code formatter is the F# community standard tool for automatic code formatting. The default settings correspond to this style guide.

とのことなので、スタイルダイドのお墨付きがある fantomas を使えばそれを簡単に手に入れることができる。

fantomas を導入したことで意外に「あ、そこの改行もなくなるわけ？」みたいな整形は結構あった。
が、それは気づかないうちに自己流の何かを入れてたわけであり、ある意味導入によってデトックスできたなという気がする。

あと思わぬ収穫として Ionide の挙動が fantomas の有無で変わるのを確認できた。
具体的には先述した fantomas 導入前後で改行がなくなるようフォーマットされたケースだ。
1 行あたりの文字数が上限未満なら改行を取り払っている様子。
以前は Ionide 内臓の fantomas で整形されてると思ってたが、先述の通りインデントのサイズと改行コードの指定しかなくてそれが変わるのなら、なんかのエッセンスが入ってるのか？
これはちょっと気持ちが悪いので、後追いでも調べたいところ。

---

F# のフォーマットを入れたのもあって、ついでに [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) で [psake](https://github.com/psake/psake) のファイルと [Pester](https://github.com/pester/Pester) のファイルも整形する気になった。
これらも普段は VS Code の PowerShell 拡張に任せっきりだったが、 CI で整形済みかチェックできるようにした。 [#146](https://github.com/krymtkts/pocof/pull/146)

PSScriptAnalyzer でちょっと面倒だったのが、 PowerShell 拡張が PSScriptAnalyzer のどのルールでチェック・整形されているか分からなかったところだ。
なんかのデフォルト + 追記した `powershell.codeFormatting.*` 拡張機能のオプションが反映されてるっぽい。
あんまり真面目に使ったことなかった、すまん PSScriptAnalyzer ... でも今回色々調べたお陰でルールの作り方がわかった。

PSScriptAnalyzer は、設定を指定しないデフォルトで全ルールのチェックをしてくれるのでそれが一番楽だが、ルールを調整したいときは設定を自前で用意しないといけない。
以下のドキュメントにルールの一覧があるが、その中から必要なものを 1 つずつコピるのは苦行すぎる。

[List of PSScriptAnalyzer rules - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/utility-modules/psscriptanalyzer/rules/readme?view=ps-modules)

その場合に楽なのは、 PSScriptAnalyzer に見込みのデフォルトルールをコピって、必要な変更を加える方法だ。
pocof では v1.21.0 の `CodeFormatting.psd1` ↓ を流用し、 [`AlignAssignmentStatement`](https://learn.microsoft.com/en-us/powershell/utility-modules/psscriptanalyzer/rules/alignassignmentstatement?view=ps-modules) を無効化する等の調整をした。
[PSScriptAnalyzer/Engine/Settings/CodeFormatting.psd1 at 1.21.0 · PowerShell/PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer/blob/1.21.0/Engine/Settings/CodeFormatting.psd1)

`AlignAssignmentStatement` を有効化していたら ASCII ならキレイにフォーマットされるが、マルチバイト文字があるとその途端ぐちゃぐちゃになる。
これは PSScriptAnalyzer が悪いのではなく、 Go やその他の言語の alignment も同じなので、非英語圏の言語だけの問題なのかも知れない。
というわけで好きじゃないオプションなので、基本無効化する。 pocof でもそうした。
この件は昔ブログに書いた。
[krymtkts - East Asian な全角文字をキーに使った場合のアラインメントについて考える](/posts/2022-05-14-think-about-alignment-of-full-width-chars.html)

このように作成した設定を読み込ませる方法は、明示的な方法と暗黙的な方法の 2 つある。
PSScriptAnalyzer は `Path` オプションで検査対象のスクリプトを指定する。
その指定した `Path` のディレクトリに `PSScriptAnalyzerSettings.psd1` があれば暗黙的な設定として読み込まれる。

[Using PSScriptAnalyzer - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/utility-modules/psscriptanalyzer/using-scriptanalyzer?view=ps-modules#implicit)

> If you place a settings file named PSScriptAnalyzerSettings.psd1 in your project root, PSScriptAnalyzer discovers it when you pass the project root as the Path parameter.

pocof では検査したいスクリプトは 2 つしかなく、しかも別の場所にあるので明示的に `Settings` で設定を読み込ませるようにした。

あと PSScriptAnalyzer の CI でのコケさせ方だが、チェック結果が 1 件以上あれば以上があると判定するようにした。
これはほんとにこれでいいのかよくわからないが、失格したチェックがあってもエラーコードが変わらないし。
少なくともチェックに失格したものがあれば結果が出力されるので、多分あってる。

```powershell
Task Lint {
    dotnet fsharplint lint "${ModuleName}.sln"
    if (-not $?) {
        throw 'dotnet fsharplint failed.'
    }
    dotnet fantomas ./src --check
    if (-not $?) {
        throw 'dotnet fantomas failed.'
    }
    # これでいいのか...
    $warn = Invoke-ScriptAnalyzer -Path .\psakefile.ps1 -Settings .\PSScriptAnalyzerSettings.psd1
    if ($warn) {
        throw 'Invoke-ScriptAnalyzer for psakefile.ps1 failed.'
    }
    $warn = Invoke-ScriptAnalyzer -Path .\tests\pocof.Tests.ps1 -Settings .\PSScriptAnalyzerSettings.psd1
    if ($warn) {
        throw 'Invoke-ScriptAnalyzer for pocof.Tests.ps1 failed.'
    }
}
```

これでもう機能開発以外の逃げ道はなくなったし、ほんとに pocof 0.10.0 リリースしよう。
あとはガチの未実装機能を実装していくだけ。もう寄り道はなしにしよう(どの口が言うか)。
