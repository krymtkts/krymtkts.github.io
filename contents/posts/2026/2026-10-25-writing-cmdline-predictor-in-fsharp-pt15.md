---
title: "F# で command-line predictor を書いてる Part 15"
subtitle: "0.10.0"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.10.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.10.0) をリリースした。

changelog は以下の通り。

- 追加
  - identifier を入力したときだけ snippet を suggest する設定 `PredictOnlyWithIdentifier` を追加
  - `;` 、 `|` 、先頭の block comment に続く入力で、それ以前の入力を保持したまま snippet を予測する
  - snippet 設定と [`Add-Snippet`](https://github.com/krymtkts/SnippetPredictor/blob/v0.10.0/docs/SnippetPredictor/Add-Snippet.md) に `PredictionContexts` を追加。pipeline 内の予測には明示的な許可が必要。同じ context の重複指定は不正とする
  - editor での補完、hover help、設定の検証に使う JSON Schema を同梱
- 変更
  - suggest を完全一致、前方一致、単語先頭一致、その他の部分一致の順に並べる。同順位では従来の順序を維持する
- 修正
  - 大文字・小文字を区別せずに設定キーの重複を検出し、不正設定として拒否する

念願の AST ベースでの suggest をはじめとして、 suggest の表示順の調整も気に入ってる機能。
表示順序については [以前](/posts/2026-10-11-writing-cmdline-predictor-in-fsharp-pt14.html) に触れたので、ここでは AST ベースの suggest について書く。

0.9.0 までの SnippetPredictor では入力している文全体の snippet の置き換えしか提供していなかった。
0.10.0 からは AST ベースで文末と pipeline を判定し、その後ろを snippet で置換できるようになった。
要は入力と snippet の合成機能が提供されるようになったということ。

使い道としては、よく使う command とその option を snippet にしておいて、 command に続けて書くときにも呼び出すというのがある。
以下に、文末と pipeline の後ろの両方で使える稀有な例を示す。
[`Get-Date`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/get-date?view=powershell-7.6) は pipeline で [DateTime](https://learn.microsoft.com/en-us/dotnet/api/system.datetime?view=net-10.0) を受け取れるので、この snippet はどちらでも使える。
日時の書式は毎回入力が面倒なので、以下の snippet を `.snippet-predictor.json` の `Snippets` に登録しておく、というもの。

```json
{
  "Snippet": "Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK'",
  "Tooltip": "Get the current date and time in the specified format.",
  "PredictionContexts": ["Command", "Pipeline"]
}
```

`PredictionContexts` で、その snippet をどの位置で suggest するか指定する。
`Command` は従来の挙動を拡張したもので、行頭や `;` の後、先頭の block comment の後ろに suggest する。
`PredictionContexts` の既定値は `Command` なので、既存の snippet はそのまま使える。
例えば、先頭が `Get-D` の場合に suggest するだけでなく、以下のような入力でも `Get-D` の部分から suggest できる。

```powershell
# ; で文を続けて入力するパターン
Write-Host 'now'; Get-D
# これはあまり使うことがなさそうなコメントの後ろに入力するパターン
<# 作業記録 #> Get-D
```

suggest を選ぶと、それぞれ以下になる。

```powershell
Write-Host 'now'; Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK'
<# 作業記録 #> Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK'
```

`Pipeline` を指定すると pipeline の後でも suggest するようになる。

単独なら現在時刻を整形し、 pipeline なら受け取った日時を整形する。
例えば、指定の日時を同じ書式で表示したいときは以下のように入力する。

```powershell
'2026-10-26' | Get-D
```

候補を選ぶとこうなる。

```powershell
'2026-10-26' | Get-Date -Format 'yyyy-MM-ddTHH:mm:ssK'
```

注意点としては、 SnippetPredictor が pipeline 入力を受け取れる command か調べているわけではないこと。
利用者自身が、 pipeline の途中で使う snippet を自分で指定する必要がある。
例のような `Command`/`Pipeline` 両方で利用する snippet は、 [`Select-Object`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/select-object?view=powershell-7.6) みたいな input がなくてもエラーにならない Cmdlet を使うのが安全かな。

あと、 `.snippet-predictor.json` の schema も複雑になってきたので、 [JSON Schema](https://github.com/krymtkts/SnippetPredictor/blob/v0.10.0/schemas/snippet-predictor.schema.json) を作成した。
VS Code であれば以下のように User setting 等に指定すれば利用できる。

```json
  "json.schemas": [
    {
      "fileMatch": ["**/.snippet-predictor.json"],
      "url": "https://raw.githubusercontent.com/krymtkts/SnippetPredictor/main/schemas/snippet-predictor.schema.json",
    }
  ]
```

厳密に version を指定したければ以下。

```json
  "json.schemas": [
    {
      "fileMatch": ["**/.snippet-predictor.json"],
      "url": "https://raw.githubusercontent.com/krymtkts/SnippetPredictor/v0.10.0/schemas/snippet-predictor.schema.json",
    }
  ]
```

まだ JSON SchemaStore への登録は検討してないんで、考えてみる。

0.10.0 でコレまでの SnippetPredictor と違う方向性が見えてきたのは面白い。
とはいえ `PredictionContexts: Pipeline` の活用を自分自身最大化出来てないから、使える snippet を色々試す必要がある。
AST ベースの suggest は他にもできることがありそうなので、少しずつ検討を進めてみる。
