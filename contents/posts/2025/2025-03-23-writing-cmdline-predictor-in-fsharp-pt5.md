---
title: "F# で command-line predictor を書いてる Part 5"
subtitle: "coverage 100% にする"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.2.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.2.0) をリリースした。

v0.2.0 の新しい機能は、[前回触れた tooltip や snippet に付与した group での絞り込み](/posts/2025-03-16-writing-cmdline-predictor-in-fsharp-pt4.html) や、他にも `:snp` 等の symbol 指定で絞り込む際に case-insensitive にしてみた。
ただこれは [PSReadLine](https://github.com/PowerShell/PSReadLine) の挙動的には [`HistorySearchCaseSensitive`](https://learn.microsoft.com/en-us/powershell/module/psreadline/set-psreadlineoption?view=powershell-7.5#-historysearchcasesensitive) で制御可能なので、設定ファイルに持たせた方がいいかも。

あと今回の version から coverage 100% にしてみた。
やはり 100% となると、 GitHub Actions で cross platform な test してるのもあって利用可能な全ての platform で一度はコードが実行されてエラーにならないのを保証できるので、安心感が違うなと感じる。
今回 100% にするために、半ば無理やり全部通すような test を書いた。
unit test に適さない環境変数周りの処理や、 [`ICommandPredictor`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.subsystem.icommandpredictor?view=powershellsdk-7.2.0) の使わない method のような通常利用で到達不能な部分もだ。

例えば以下の [`JsonIgnoreCondition.WhenWritingNull`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonignorecondition?view=net-9.0) と [`JsonConverter`](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonconverter-1?view=net-9.0) 実装の組み合わせだと、 `reader.GetString` が `null` を返す場面がない。
`null` の処理が skip されることによるみたい。

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

しかも `GroupJsonConverter` を直接テストしようにも、シグネチャファイルに記載してなかったのでそのままだとテストできない。
公開するつもりもない型なので足す気もないし。
結果的に `DEBUG` 条件付きでシグネチャファイルに含めるようにして直接テストするようにした。

シグネチャファイルがこう。

```fsharp
#if DEBUG
type GroupJsonConverter =
    inherit System.Text.Json.Serialization.JsonConverter<string>

    new: unit -> GroupJsonConverter

    override Read:
        reader: byref<System.Text.Json.Utf8JsonReader> *
        _typeToConvert: System.Type *
        options: System.Text.Json.JsonSerializerOptions ->
            string

    override Write:
        writer: System.Text.Json.Utf8JsonWriter * value: string * options: System.Text.Json.JsonSerializerOptions ->
            unit
#endif
```

テストコードはこう。

```fsharp
#if DEBUG

module GroupJsonConverter =
    open System.Text.Json

    [<Tests>]
    let test_GroupJsonConverter =
        testList
            "GroupJsonConverter"
            [

              test "when the value is null " {
                  let json = """{"key": null}"""
                  let mutable reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json))

                  reader.Read() |> ignore // {
                  reader.Read() |> ignore // "key"
                  reader.Read() |> ignore // null

                  let result: string | null =
                      GroupJsonConverter().Read(&reader, typeof<string>, JsonSerializerOptions())

                  match result with
                  | null -> ()
                  | _ -> failtest "Expected null but got a different value"
              }

              ]

#endif
```

あと F# ならではの coverage 100% の難しさとして、 [IL(中間言語)](https://learn.microsoft.com/ja-jp/dotnet/standard/glossary#il) に到達不可能な pass が生成されるケースがある。
既知のものでいうと例えば [string の slicing](https://krymtkts.github.io/posts/2024-04-14-how-to-check-coverage-for-inline-functions.html) 。
今回は新たに [`static let`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/members/let-bindings-in-classes) でも到達不能な pass が生成されるのを確認した。

例えば以下、 `static let` によって `internal static int init@47` が生成され、 `Read` method に分岐が生えてしまう。

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
```

上記コードは [ILSpy](https://github.com/icsharpcode/ILSpy) で decompile して C# 表示したらこうなる。 `int@47` という属性の分岐が確認できる。

```csharp
[Serializable]
[CompilationMapping(/*Could not decode attribute arguments.*/)]
internal class GroupJsonConverter : JsonConverter<string>
{
    internal static Regex regex;

    internal static int init@47;

    public override string? Read(ref Utf8JsonReader reader, Type _typeToConvert, JsonSerializerOptions options)
    {
        string @string = reader.GetString();
        string text = @string;
        string text2 = text;
        if (text2 != null)
        {
            string value = text2;
            if (init@47 < 2)
            {
                IntrinsicFunctions.FailStaticInit();
            }
```

初期化済みか判断する用途くさいが具体的に値が設定される箇所とかがわからなかった。
取り敢えず `static let` を使うと期待しない分岐が発生するのはわかったので、別 module に切り出すことで分岐をなくした。

ややこしい気もするが 100% を目指すと汚れ仕事も必要なのだ。

SnippetPredictor の開発は機能的にはこれで一段落かな。
あとは日常利用で、先述の case-insensitive にするやつとか細々とした気になる点に手をいれるくらいの予想。
またなんか新しいアイデア探すか。
