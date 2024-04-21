---
title: "F# でコマンドレットを書いてる pt.37"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) ではあまり [Active Patterns](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns) を使ってなかった。
特に理由があって避けてたわけじゃなく、わたしの F# 力が低いのもあるけどなんか大げさな感じがして使ってなかった。

でもこの辺を読んでもっと気軽に使っても良さそうやな～という気になってきた。

- [F# tips weekly #10: Active patterns (1)](https://jindraivanek.hashnode.dev/f-tips-weekly-10-active-patterns-1)
- [F# tips weekly #11: Active patterns (2)](https://jindraivanek.hashnode.dev/f-tips-weekly-11-active-patterns-2)

そこで可読性高めるのと汎用化を期待して使いはじめてみた。
Partial Active Patterns やら Complete Active Patterns やらあるけど、気に入ってるのは Single-case Active Pattern だ。
値を変換するパターンがこれに相当する。

[こんな感じ](https://github.com/krymtkts/pocof/blob/8aefcad38295b5fa6e37d879b8b79b346a91d46e/src/pocof/Data.fs#L57-L58)でタプルを照準に並び替えて返すものを作ってみたが、良さそう。

```fsharp
    let (|Ascending|) (x, y) = if x < y then (x, y) else (y, x)
```

pocof は範囲選択の実装の都合上、数値を並び替える場面がいくつかあった。
あるいは 2 つの数値のうち小さい方、大きい方を取るとか。
そういったケースにこれがバッチリハマる。

並び替えるだけならこう。

```fsharp
    match x, y with Ascending xy -> xy
```

大小どちらかを取る場合、例えば小さい方はこう。

```fsharp
    match x, y with Ascending (x, _) -> x
```

既に 2 値のタプルがあるなら、こうするのがシンプルか。

```fsharp
    xy |> function Ascending xy -> xy
```

この状態だと単に関数呼び出しでしかないけど、他のパターンと組み合わせていい塩梅にできる。

```fsharp
let xy = (0, -1)
match xy with
| x, y when x < 0 || y < 0 -> None
| Ascending xy -> Some(xy)
```

いいやん。
ボトムアッププログラミングが捗るな(ゆーても既にモノ出来てるからボトムアップしようがないけど)。

あとは多少複雑なコードの人まとまりに意図を示すのにも向いてる。[こことか](https://github.com/krymtkts/pocof/blob/b7bbde9c273850d9a06fedfb72e9d8f34ca30106/src/pocof/Data.fs#L86-L90)

```fsharp
    let (|Found|_|) aType excludes name =
        FSharpType.GetUnionCases aType
        |> Seq.filter (fun u -> Set.contains u.Name excludes |> not)
        |> Seq.tryFind (fun u -> u.Name |> String.lower = name)


    let private fromString<'a> s =
        let name = String.lower s
        let aType = typeof<'a>

        match name with
        | Found aType (set []) u -> FSharpValue.MakeUnion(u, [||]) :?> 'a
        | _ -> failwithf $"Unknown %s{aType.Name} '%s{s}'."
```

Discriminated Union を文字列から作るとき(pocof のパラメータ変換で使う)にハマる。
元は `match ~ with` のところに `Found` active pattern 相当のコードを置いてたので、なおさら読みやすくなってる。
関数でもいいのだけど、 Match expression と組み合わせることでより文脈読みやすくなると、個人的に感じた。

まだ引数のパターンを示すのに使うような尖った？使い方は出来てないが、徐々になじませていくか。
