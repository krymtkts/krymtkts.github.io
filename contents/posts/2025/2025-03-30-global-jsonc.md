---
title: "`global.json` は JSONC"
tags: ["dotnet"]
---

旅行中のため小ネタ。
個人的な無知ゆえに知らなかったものだが、備忘のため記しておく。

[global.json overview - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)

> Comments in global.json files are supported using JavaScript or C# style comments. For example:
>
> ```json
> {
>   // This is a comment.
>   "sdk": {
>     "version": "8.0.300" /* This is comment 2*/
>     /* This is a
>   multiline comment.*/
>   }
> }
> ```

まじか。
[pocof](https://github.com/krymtkts/pocof) において [macOS の GitHub Actions workflow でのみ `dotnet tool restore` がエラーになる問題](https://github.com/dotnet/sdk/issues/46857)があったとき、 .NET SDK を version 固定しないといけなかったので[そのメモを残す](https://github.com/krymtkts/pocof/commit/d4ff96628ce470a52e74b29fdd0b67f44161e6e0)のに使った。

因みにもしやと思って他に思いつく .NET 系の設定ファイルで試したところ、 `dotnet-tools.json` はダメだった。

```powershell
> dotnet tool restore
Json parsing error in file C:\Users\takatoshi\dev\github.com\krymtkts\pocof\.config\dotnet-tools.json : '/' is invalid after a value. Expected either ',', '}', or ']'. LineNumber: 5 | BytePositionInLine: 6.
```

`dotnet-tools.json` で jspnc をサポートしたいという提案は過去にもあったらしいが、頓挫したようだ。

- [Support of comments in jsondocument · Issue #30316 · dotnet/runtime](https://github.com/dotnet/runtime/issues/30316)
- [Enable comments for tool manifest json · Issue #10384 · dotnet/sdk](https://github.com/dotnet/sdk/issues/10384)
- [[Feature Request] Support jsonc for dotnet tools manifest · Issue #16043 · dotnet/sdk](https://github.com/dotnet/sdk/issues/16043)

JSON のコメントと空白をどう扱うべきかに関してふにゃふにゃだったので XML に軍配が上がったて流れぽい。議論も止まった様子。

とりあえず `global.json` だけでも JSONC が使えるということやが、この流れが他にも派生していくかというと、[最近 sln が slnx になった](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/)流れを見てても XML 優勢なんかなと思えてくるな。

---

このネタ普通に[前の日記](/posts/2025-03-02-writing-cmdlet-in-fsharp-pt65.html)で書いてたわｗ
ネタ帳から削り忘れてたか...

まあ slnx とも繋げたので全く同じではないか...ということでご愛嬌。
