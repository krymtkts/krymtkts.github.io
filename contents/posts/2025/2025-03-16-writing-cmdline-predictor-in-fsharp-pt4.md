---
title: "F# で command-line predictor を書いてる Part 4"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発をした。

新しい機能として、 tooltip での絞り込みと、 snippet に付与した group での絞り込みを追加した。
[#22](https://github.com/krymtkts/SnippetPredictor/pull/22) [#23](https://github.com/krymtkts/SnippetPredictor/pull/23)

これによって以下の絞り込みのバリエーションが増えた。

- `:snp` で snippet の絞り込み
- `:tip` で tooltip の絞り込み
- `:{group}` で指定した識別子の group で snippet の絞り込み

```json
{
  "Snippets": [
    {
      "Snippet": "dotnet new classlib --name project --language F# --output ./src/project",
      "Tooltip": "Create a new F# class library project",
      "Group": "fsharp"
    },
    {
      "Snippet": "dotnet tool install fantomas --prerelease",
      "Tooltip": "Install or update Fantomas",
      "Group": "fsharp"
    },
    {
      "Snippet": "dotnet new sln",
      "Tooltip": "Create a new solution file",
      "Group": "project"
    }
  ]
}
```

みたいなんがあったとして、 `:fsharp` で絞れる。
この例のように、 F# 関連でまとめておきたい snippet があって snippet や tooltip に `F#` を含まないような場合に `"Group": "fsharp"` でまとめておけるというメリットがあるはず。
group のこの用途だと実は tooltip の記述を工夫すれば代用できるが、 `Get-Snippet` で取得した snippet を `Where-Object` で楽に絞り込むには、やはり別の field を持っているのが最適だと考えた。

今のところ、この group の絞り込みを使うことで絞り込み対象の母数が減って速くなるということはない。愚直に snippet の sequence を絞り込むのに group の条件がついただけになってる。
snippet の登録件数がそんなに多くならないだろうし、空間計算量に寄せるほどでもないかなという直感だ。
もし今後件数が増えまくったりして、 [command-line predictor の 20ms の制限](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/create-cmdline-predictor?view=powershell-7.5#creating-the-code)に触れてくるようなら再検討する。

`Group` field に設定する値に長さの制限はないが、文字種は `[a-zA-Z0-9]` しか使えなくしている。
`Add-Snippet` でも制限されているし、 `.snippet-predictor.json` 読み込み時にも制限されてる。
また `Group` field は省略可能なので、従来(というほど古くもないけど)の `.snippet-predictor` の記述通り `Group` なしの snippet も読める。

`Group` field の制限は [System.Text.Json Namespace](https://learn.microsoft.com/en-us/dotnet/api/system.text.json?view=net-9.0) を使ってこんなコードでできた。

```fsharp
type GroupJsonConverter() =
    inherit JsonConverter<string>()

    [<Literal>]
    static let pattern = "^[A-Za-z0-9]+$"

    static let regex = Regex(pattern)

    override _.Read(reader: byref<Utf8JsonReader>, _typeToConvert: Type, options: JsonSerializerOptions) =
        reader.GetString()
        |> function
            | null as value -> value // NOTE: unreachable when JsonIgnoreCondition.WhenWritingNull is used.
            | value when regex.IsMatch(value) -> value
            | value -> JsonException(sprintf "Invalid characters in group: %s" value) |> raise

    override _.Write(writer: Utf8JsonWriter, value: string, options: JsonSerializerOptions) =
        value |> writer.WriteStringValue

type SnippetEntry =
    { Snippet: string
      Tooltip: string
      [<JsonConverter(typeof<GroupJsonConverter>)>]
      [<JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)>]
      Group: string | null }
```

F# 的には group には [`option`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/options) を使うべきだろうけど、 .NET と直にやり取りしてるし変換コストを考えたらそのままがいいかなと。
F# 9 で [Nullable Reference Type](https://devblogs.microsoft.com/dotnet/nullable-reference-types-in-fsharp-9/) ができたことで、 `null` を取り扱わないといけないケースも `option` と遜色なく書けるようになった気がする。
代わりに到達不能な branch ができたり coverage 的には頭を悩ますことが多いが。

group には `:snp` や `:tip` との一体感やタイプ回数を考えたら小文字英数 3 文字くらいの識別子が良かろうが、分かりやすさ的に英単語を使うのもありかな。
いま個人的には英単語の方を、個別の識別子考えて覚える必要ないから使ってる

いま `if` 連発で愚直に書いただけのロジックになってるのをリファクタリングしたら、 v0.2.0 としてリリースしたい。
