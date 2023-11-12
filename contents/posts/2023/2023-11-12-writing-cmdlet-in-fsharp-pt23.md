---
title: "F# でコマンドレットを書いてる pt.23"
tags: ["fsharp", "powershell"]
---

[pocof](https://github.com/krymtkts/pocof) の開発をした。

作り始めた頃からずっと放置してた bottom up のレイアウトに対応した。 [#72](https://github.com/krymtkts/pocof/pull/72)

自分で Issue 作っといてなんやけど、当初よりコレある意味あんのかな？という感じがしてて、やり始めるまで気乗りしなかった。
しかしいざ手を動かしてみたら top down と部分的に違うだけでほぼ同じやし、長らく放置してた pocof の描画部分を思い出すのにもちょうどいいかもなと思えてきた。
久しぶりに pocof の開発する通過儀礼としてはいい感じだった。
あと逆さまに検索内容が表示されるのも案外面白かったので、あってもいいかなコレという気にはなれた。

描画部分を久しぶりに触ったので [`PSHostRawUserInterface`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.host.pshostrawuserinterface?view=powershellsdk-7.3.0) をそのまま取り回してるから unit test 書けない(書きづらい)ねんよなというのも思い出した。
でも今になって見た感じだと、`PSHostRawUserInterface` をくるんだ型を作っといて一層挟んであげればどうにかなるなと言う感触がする。

検索で絞り込んだ内容をデバッグログに出力する機能も長らく作ってなくて、開発時にはてきとーに開発用のコードを挟んでたりしたので、そのへんも合わせてリファクタリングしたいなーという気持ちになってきた。

今回 top down / bottom up に対応した描画ロジックは以下の通り。
[pocof/src/pocof/UI.fs](https://github.com/krymtkts/pocof/blob/d68a172ffb6ca94fc7751fca2fc624d1e9683c16/src/pocof/UI.fs#L64-L120)

```fsharp
        member __.writeScreen
            (layout: PocofData.Layout)
            (state: PocofData.InternalState)
            (x: int)
            (entries: PocofData.Entry list)
            (props: Result<string list, string>)
            =
            let basePosition, firstLine, toHeight = // NOTE: 主にいじったのここ。 layout に応じて作られた let binding を使うように全体調整しただけ
                match layout with
                | PocofData.TopDown -> 0, 1, (+) 2
                | PocofData.BottomUp ->
                    let basePosition = __.rui.WindowSize.Height - 1 // NOTE: こういう PSHostRawUserInterface に直接アクセスする部分を取り除く
                    basePosition, basePosition - 1, (-) (basePosition - 2)

            __.writeScreenLine basePosition
            <| __.prompt + ">" + state.Query

            __.writeRightInfo state entries.Length basePosition

            // NOTE: これオプションでどうにかする
            // PocofDebug.logFile "./debug.log" [ List.length entries ]

            __.writeScreenLine firstLine
            <| match state.Notification with
               | "" ->
                   match props with
                   | Ok (p) -> (String.concat " " p).[.. __.rui.WindowSize.Width - 1]
                   | Error (e) -> "note>" + e
               | _ -> "note>" + state.Notification

            let h = __.rui.WindowSize.Height - 3

            let out =
                match List.length entries < h with
                | true -> entries
                | _ -> List.take h entries
                |> PocofData.unwrap
                |> __.invoke
                |> Seq.fold
                    (fun acc s ->
                        s.Split Environment.NewLine
                        |> List.ofArray
                        |> (@) acc)
                    []

            seq { 0..h }
            |> Seq.iter (fun i ->
                __.writeScreenLine
                <| toHeight i
                <| match List.tryItem i out with
                   | Some s ->
                       // NOTE: これオプションでどうにかする
                       // logFile "./debug.log" [ s ]
                       s
                   | None -> String.Empty)

            __.setCursorPosition
            <| __.getCursorPositionX state.Query x
            <| basePosition
```

関数がでかいしゴチャついてる。デバッグログ出力がコメントアウトで残されており雑い。でも色々やれるイメージ湧いてきて、良い。ひとまず NOTE コメントのある箇所をどうにかしたい。

`__.rui` が `PSHostRawUserInterface` なのだけど、こいつをまず引き剥がしたいところ。
その後デバッグモードの組み込みをしてデバッグログ出力を改善する。
page up / down のサポートをするときには、描画対象となるデータを取ってる `let out` のところを表示中のページと描画域の行の数で調整して～とか。

描画で [Console.Write](https://learn.microsoft.com/en-us/dotnet/api/system.console.write?view=net-7.0) を使って茶を濁してるところはもうちょっと先。コンソールバッファの操作に関してわからないことが多いから調べないといけない。クロスプラットフォーム見据えたら .NET だけでできる範囲限られてそうやし。 Windows 以外ではインタラクティブな操作をテストしてないのもあって。

[blog-fable](https://github.com/krymtkts/blog-fable) の方を積極的にいじる熱落ち着いてるので、これから当面イメージが湧く限り [pocof](https://github.com/krymtkts/pocof) に集中していけそう。いつまで続くか知らんけど。
