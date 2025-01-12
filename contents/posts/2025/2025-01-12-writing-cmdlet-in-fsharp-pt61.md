---
title: "F# で Cmdlet を書いてる pt.61"
tags: ["fsharp", "powershell", "dotnet", "benchmark"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[BenchmarkDotnet](https://github.com/dotnet/BenchmarkDotNet) に続き [ObjectLayoutInspector](https://github.com/SergeyTeplyakov/ObjectLayoutInspector) で pocof の [record](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/records) とか [DU](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) の memory layout を可視化し始めた。[#300](https://github.com/krymtkts/pocof/pull/300)
benchmark とるのや memory layout の考え方は以下の blog が詳しく、参考にさせてもらっている。

[Writing high performance F# code](https://www.bartoszsypytkowski.com/writing-high-performance-f-code/)

[半年ほど前](/posts/2024-08-04-writing-cmdlet-in-fsharp-pt46.html)にもこの blog を見てたのだけど、 .NET 力と F# 力が低かったから手を付けれずにいた。
今もレベル低いが前よりは高まったので、ようやく手を動かす気になったので試しているところだ。
pocof に取り込むに当たり練習しておくための repo [krymtkts/fsharp-bench](https://github.com/krymtkts/fsharp-bench) も作った。
以下は pocof の型を分析した例で、 record と DU の memory layout を印刷できて便利。

```plaintext
Type layout for 'Refresh'
Size: 8 bytes. Paddings: 4 bytes (%50 of empty space)
|=============================|
| Object Header (8 bytes)     |
|-----------------------------|
| Method Table Ptr (8 bytes)  |
|=============================|
|   0-3: Int32 _tag (4 bytes) |
|-----------------------------|
|   4-7: padding (4 bytes)    |
|=============================|
```

この `Refresh` DU は独自の field を持たず、 tag (DU のケース分け)しかないのでこういうのは[構造体](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/structs)とすることで、Object header と vtable(method table) pointer を削減できる分パフォ改善できるケースが多い。
tag だけなら `int32 _tag` の 4 bytes しか使わないから、参照ポインタの 8 bytes の半分になる。
コピーコスト以外にも利用する側でも padding が発生しなければ効率が良くなる。

ただ DU を構造体として扱うときの難しいところは、各 tag の filed 全ての memory を確保するところ。

```plaintext
Type layout for 'Key'
Size: 32 bytes. Paddings: 10 bytes (%31 of empty space)
|====================================|
|   0-3: ConsoleKey _key (4 bytes)   |
| |================================| |
| |   0-3: Int32 value__ (4 bytes) | |
| |================================| |
|------------------------------------|
|   4-7: Int32 _tag (4 bytes)        |
|------------------------------------|
|   8-9: Char _c (2 bytes)           |
|------------------------------------|
| 10-15: padding (6 bytes)           |
|------------------------------------|
| 16-31: Action _action (16 bytes)   |
| |================================| |
| |   0-7: String _query (8 bytes) | |
| |--------------------------------| |
| |  8-11: Int32 _tag (4 bytes)    | |
| |--------------------------------| |
| | 12-15: padding (4 bytes)       | |
| |================================| |
|====================================|
```

この `Key` DU は `StructAttribute` 付与してて、 内包する `Action` も構造体なので、以下のように大きめになる。
`Key` DU に限れば、 内包する `Action` DU を使ってる箇所で `String query` を使うケースは除外されてるので、別の Struct DU を作って置き換えるべきだろう。
前に軽率に `Struct` を付与したのが裏目に出てる。

```plaintext
Type layout for 'InternalState'
Size: 88 bytes. Paddings: 7 bytes (%7 of empty space)
|=====================================================|
| Object Header (8 bytes)                             |
|-----------------------------------------------------|
| Method Table Ptr (8 bytes)                          |
|=====================================================|
|   0-7: QueryState QueryState@ (8 bytes)             |
|-----------------------------------------------------|
|  8-15: QueryCondition QueryCondition@ (8 bytes)     |
|-----------------------------------------------------|
| 16-23: PropertySearch PropertySearch@ (8 bytes)     |
|-----------------------------------------------------|
| 24-31: FSharpOption`1 Notification@ (8 bytes)       |
|-----------------------------------------------------|
| 32-39: IReadOnlyCollection`1 Properties@ (8 bytes)  |
|-----------------------------------------------------|
| 40-47: IReadOnlyDictionary`2 PropertyMap@ (8 bytes) |
|-----------------------------------------------------|
| 48-55: String Prompt@ (8 bytes)                     |
|-----------------------------------------------------|
| 56-63: String WordDelimiters@ (8 bytes)             |
|-----------------------------------------------------|
| 64-71: Refresh Refresh@ (8 bytes)                   |
|-----------------------------------------------------|
| 72-75: Int32 PromptLength@ (4 bytes)                |
|-----------------------------------------------------|
| 76-79: Int32 ConsoleWidth@ (4 bytes)                |
|-----------------------------------------------------|
|    80: Boolean SuppressProperties@ (1 byte)         |
|-----------------------------------------------------|
| 81-87: padding (7 bytes)                            |
|=====================================================|
```

こちらはデカすぎる `InternalState` record。こういうのは構造体にするとコピーコストかさむため重くなる。
先に挙げたような単純な DU を構造体にすれば参照ポインタよりサイズを小さくできるのでメモリを節約できるか。
他にも最近の改善で `Prompt` は切り分けられるようになったし、改善できるかな。
benchmark 調べた感じだとデカい record 一発で取り回すより細かい record に分けて取り回した方が速いから、分けるべきなのかも。

ちなみに、 ObjectLayoutInspector の `fsproj` は実行形式(`OutputType` `Exe`) にしておかないと標準出力できないので、 `TargetFramework` は `net9` にしている。
pocof は PowerShell の各 version との互換性のために `netstandard2.0` を対象にしてて、 `ProjectReference` で `pocof.fsproj` を参照してるから、 pocof 側の record や DU は `netstandard2.0` 準拠で compile され、実際に利用されるときと同じ memory layout が出力される(はず)。

一通り record や DU の memory layout は見れるようにしたのだけど、 benchmark は簡単なのだけがある状態なので、 pocof のパフォで気になる部分の benchmark を作って計測してみないことには最適化に着手できない。
これ、どういう風に実際と同じような benchmark を作るか考えるのも中々難しいよな。
pocof は module 毎に公開されてる関数が概ねその module の entrypoint になってて、そこは benchmark を作りやすい。
けど組み合わせたパターンになると、 PowerShell の E2E testing 改善の文脈と同じような autopilot mode みたいなの作り込むってところにつながってく気がするな。
いま unit test でやってる [`ICommandRuntime`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.icommandruntime?view=powershellsdk-7.4.0) のテスト用実装で [`PSHostUserInterface`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.host.pshostuserinterface?view=powershellsdk-7.4.0) テスト用派生クラスをもう少し作れたらかのうせいがあるのかも(今は空っぽ)。
キー入力と仮想スクリーンをうまく作れるか次第よな～。

まあ何にせよ benchmark が取れて memory layout を可視化できるようになって、準備は揃っている。
このあたりの知識は .NET の内部的な話で、先に挙げた blog のような解析をしてる人の記事以外は文書が見当たらないし、先人の通った道を自分も通ることでしか習熟しないと思うねんよな。
何より ChatGPT や Copilot ｻﾝも全然詳しくなくて当てにならないところ。尚更自分でやるしかない。

続く。
