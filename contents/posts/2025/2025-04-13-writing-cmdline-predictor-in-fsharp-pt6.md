---
title: "F# で command-line predictor を書いてる Part 6"
subtitle: "FSharp.Analyzer.SDK"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.3.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.3.0) をリリースした。

v0.3.0 の新しい機能は、[前](/posts/2025-03-23-writing-cmdline-predictor-in-fsharp-pt5.html)に触れていた snippet の検索の case-sensitivity を指定する option の追加だ。

> ただこれは [PSReadLine](https://github.com/PowerShell/PSReadLine) の挙動的には [`HistorySearchCaseSensitive`](https://learn.microsoft.com/en-us/powershell/module/psreadline/set-psreadlineoption?view=powershell-7.5#-historysearchcasesensitive) で制御可能なので、設定ファイルに持たせた方がいいかも。

今度こそ一通りの機能が揃ったか。
あと使い出して感じてるのが、よく使う snippet の表示順位を上げられたらいいなというところだが、この場合 snippet を選んだ履歴を記録する必要があるからなんともなという感じ。
`.snippet-predictor.json` とは別に二重で記録するわけになるからなんともな。これは本当に必要か検討を要する。

あと、 SnippetPredictor は長らく linter の類を CI に組み込んでなかったのだけど、追加したのでそれを記録しておく。

---

SnippetPredictor は [pocof での経験から FSharpLint をうまく使えてない](/posts/2024-11-24-writing-cmdlet-in-fsharp-pt54.html)のもあって、 linter の類を CI に組み込んでなかった。
しかし最近 [Fsharp.Analyzers.SDK](https://github.com/ionide/FSharp.Analyzers.SDK/) で解析できるのを知ったので、これを導入してコード解析する。
知ったきっかけは [Fantomas](https://github.com/fsprojects/fantomas) で使われてるのを見つけたから。
スキャン結果の SARIF report を生成して GitHub の Code Scanning にアップロードしてた。

[fantomas/.github/workflows/main.yml at 938160e6c9660af5bda33e4d20c7b522b8359789 · fsprojects/fantomas](https://github.com/fsprojects/fantomas/blob/938160e6c9660af5bda33e4d20c7b522b8359789/.github/workflows/main.yml)

数ヶ月 FSharpLint が使えなくなって無防備になってるわたしにとっては、こりゃ良さそうだと考えた。

導入の手順は [Installation | FSharp.Analyzers.SDK](https://ionide.io/FSharp.Analyzers.SDK/content/getting-started/Installing%20Analyzers.html) と [Ionide.Analyzers | ionide-analyzers](https://ionide.io/ionide-analyzers/) を参考にした。

はじめに tool を導入する。

```powershell
dotnet tool install fsharp-analyzers
```

次に解析対象の project に `Ionide.Analyzers` を追加する。
初めてなので、単純化のため [G-Research.FSharp.Analyzers](https://github.com/G-Research/fsharp-analyzers/) の導入はしてない。
最新 version がわからなかったので、 `dotnet add ~` で追加してから `IncludeAssets` を編集した。

```powershell
dotnet add ./src/SnippetPredictor package Ionide.Analyzers
```

```xml
    <!-- Analyzer configurations. -->
    <PackageReference Include="Ionide.Analyzers" Version="0.14.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
```

[Paket](https://github.com/fsprojects/Paket) を使ってない場合、実行するには analyzer の path を `--analyzers-path` に指定する必要がある。
FSharp.Analyzers.SDK にはその path を知る方法が書いてない(熟練の .NET ユーザにならよく知られたものかも知れないが)。
Ionide.Analyzers の方はその点わかりやすく、具体的に NuGet で install された analyzer の配置先を取得するための手順が記載されている。
これを使って以下のようにコマンドを構築でき、実行できるようになった。

```powershell
$analyzerPath = dotnet build ./src/SnippetPredictor  --getProperty:PkgIonide_Analyzers
dotnet fsharp-analyzers --project ./src/SnippetPredictor/SnippetPredictor.fsproj --analyzers-path $analyzerPath
# C:\Users\takatoshi\dev\github.com\krymtkts\SnippetPredictor\src\SnippetPredictor\Snippet.fs(163,4): Hint IONIDE-012 : Consider adding [<Struct>] to Discriminated Union
# C:\Users\takatoshi\dev\github.com\krymtkts\SnippetPredictor\src\SnippetPredictor\Snippet.fs(314,28): Hint IONIDE-010 : Seq.filter |> Seq.map can be combined into Seq.choose
# C:\Users\takatoshi\dev\github.com\krymtkts\SnippetPredictor\src\SnippetPredictor\Snippet.fs(89,16): Info IONIDE-002 : Prefer postfix syntax for arrays.
# C:\Users\takatoshi\dev\github.com\krymtkts\SnippetPredictor\src\SnippetPredictor\Snippet.fs(144,15): Warning IONIDE-005 : Test for empty strings should use the String.Length property or the String.IsNullOrEmpty method.
# C:\Users\takatoshi\dev\github.com\krymtkts\SnippetPredictor\src\SnippetPredictor\Snippet.fs(319,18): Warning IONIDE-005 : Test for empty strings should use the String.Length property or the String.IsNullOrEmpty method.
# C:\Users\takatoshi\dev\github.com\krymtkts\SnippetPredictor\src\SnippetPredictor\Library.fs(65,20): Info IONIDE-002 : Prefer postfix syntax for arrays.
```

コマンドのオプションは文書に載ってなかったので、 `--help` で確認した。
[SARIF](https://sarifweb.azurewebsites.net/) report を出力するには `--report` に file path を指定するだけで良い。

とりあえずコード解析はうまくできるのがわかったので SnippetPredictor の CI に組み込んだ。 [#45](https://github.com/krymtkts/SnippetPredictor/pull/45)

CI 的には warn は error にして task をこかせたいところだが、今のところ `--treat_as_error` で個別の識別子を指定するしかないっぽい。どうしたもんかな。
他にも、今は test の project を解析対象にしてないのもあるし、 [`Directory.Build.props`](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory?view=vs-2022#directorybuildprops-and-directorybuildtargets) で全体に反映するのがいいかな。
Fantomas では tool の `fsharp-analyzers` は使ってなくて MSBuild でやってるのだけど、同じ方法を採用するかはちょっと検討。

もうちょっと使ってみて、いい感じの設定ができたら [pocof](https://github.com/krymtkts/pocof) にも反映させよう。
