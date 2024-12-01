---
title: "F# で Cmdlet を書いてる pt.55"
tags: ["fsharp", "powershell", "dotnet", "fscheck"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

.NET 9 への更新は [Ionide が .NET 9 に対応](https://github.com/ionide/ionide-vscode-fsharp/issues/2048)して 1 つ課題が解消された。
あと [.NET 9 SDK の bug](https://github.com/dotnet/sdk/issues/44838) の影響で [Fantomas](https://github.com/fsprojects/fantomas) が動かないやつが解消されたら最低限 OK かなと。
[FSharpLint](https://github.com/fsprojects/FSharpLint) が動かなくなったやつはどうにもならなそうなので何かできることがないか長期的に模索しようと考えている。

---

さて、その間に他の開発が止まるのもイマイチなので、新しい取り組みとして [FsCheck](https://fscheck.github.io/FsCheck/index.html) の利用を始めてみた。
[最近 PBT の本を読んでる](/booklogs/property-based-testing.html)ことで感覚が培われてきたような気がするので、多分機は熟している。
初回の対応は従来のテストケースの置き換え。 [#264](https://github.com/krymtkts/pocof/pull/264)
あまり PBT しても意味ないようなケースだが、欲しい型のジェネレータを自分で書く辺りは良い練習になるかなと思って試した。

リヨウしている FsCheck の version は [3.0.0-rc](https://www.nuget.org/packages/FsCheck/3.0.0-rc3) 。

```fsharp
open Xunit
open FsUnitTyped
open FsCheck.FSharp
open FsCheck.Xunit

module unwrap =
    open System.Collections
    open System.Management.Automation

    let psObjectGen =
        ArbMap.defaults
        |> ArbMap.generate<string>
        |> Gen.map (PSObject.AsPSObject >> Entry.Obj)

    let dictionaryEntryGen =
        ArbMap.defaults
        |> ArbMap.generate<string>
        |> Gen.two
        |> Gen.map (DictionaryEntry >> Entry.Dict)

    type EntryPSObject =
        static member Double() = psObjectGen |> Arb.fromGen

    [<Property(Arbitrary = [| typeof<EntryPSObject> |], EndSize = 1000)>]
    let ``should return PSObject sequence.`` (data: Entry list) =
        data
        |> unwrap
        |> List.ofSeq
        |> shouldEqual (
            data
            |> List.map (function
                | Entry.Obj x -> x
                | _ -> failwith "Dict is unreachable")
        )
        |> Prop.collect (List.length data)

    type EntryDictionaryEntry =
        static member Double() = dictionaryEntryGen |> Arb.fromGen

    // 略

    type MixedEntry =
        static member Generate() =
            Gen.oneof [ psObjectGen; dictionaryEntryGen ] |> Gen.listOf |> Arb.fromGen

    [<Property(Arbitrary = [| typeof<MixedEntry> |], EndSize = 1000)>]
    let ``should return mixed sequence.`` (data: Entry list) =
        data
        |> unwrap
        |> List.ofSeq
        |> shouldEqual (
            data
            |> List.map (function
                | Entry.Obj x -> x
                | Entry.Dict x -> x)
        )
        |> Prop.collect (
            List.length data,
            // TODO: use .Is* after bumping to F# 9.
            data
            |> List.filter (function
                | Entry.Obj _ -> true
                | _ -> false)
            |> List.length,
            data
            |> List.filter (function
                | Entry.Dict _ -> true
                | _ -> false)
            |> List.length
        )
```

Xunit, FsUnit, FsCheck が滑らかに統合されておりいい感じ。まだ解像度高くないから今後高まったときに気になる点とか出てくるかもしれんな。
手を動かしてみていくつか覚えたことをまとめておく。
概ね公式文書の焼き直しだが、動かしてみてわかった点もある。

[Using FsCheck.Xunit - Running tests](https://fscheck.github.io/FsCheck/RunningTests.html#Using-FsCheck-Xunit)

- xUnit を利用している場合は `FsCheck.Xunit` にある `PropertyAttribute` を使うと、従来のテストケースと FsCheck のテストケースが滑らかに繋がって良い
  - `Arbitrary` instance の上書きを使うとケース、 class, module, assembly 毎にジェネレータを設定できる。ケース以外に設置する場合は `PropertiesAttribute` を使う
- `Arbitrary` instance の `Arbitrary<'Value>` を返す static method の名前は何でもいいみたい
  - 今回の実装では先 2 つのジェネレータがコピペ元のママ `Double` になってて、その後書き加えたジェネレータで `Generate` にしても問題なく動くいてるので reflection で拾ってきてるぽい(実装はまだ追えてない)
- PBT 本で学んだ通り統計情報を出しておいた方がどのようなテストが行われたか可視化出来て良い(ただしログは汚れる)。 FsCheck では `Prop.collect` で出せる
- `PSObject` `DictionaryEntry` を `ArbMap.generate` で直に生成できないので `ArbMap.generate<string>` で生成したデータを詰めれば良い

当面はステートレスプロパティでの置き換えを先にやってみて、次にステートフルプロパティ、多分 FsCheck で言うところの [Model-based Testing](https://fscheck.github.io/FsCheck//StatefulTestingNew.html) に手を出そうかと考えている。

公式文書にもあるが、内容が 3.x に追随してないらしくて、唯一 API docs が最新を反映されてる状態らしい。 API docs 見て感覚つかめる人ならそれで学んで、わたしのようによくわからんなーとなってしまう人なら docs 見つつ直にコード書いて試すのが良さそう。

続く。
