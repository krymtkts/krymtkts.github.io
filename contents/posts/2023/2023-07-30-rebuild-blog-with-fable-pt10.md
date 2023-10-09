---
title: "Fable でブログを再構築する pt.10"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

最近日記には書いてなかったが諸々の Issue を解決してた。

- [#24](https://github.com/krymtkts/blog-fable/pull/24) [#27](https://github.com/krymtkts/blog-fable/pull/27) [#32](https://github.com/krymtkts/blog-fable/pull/32) [#34](https://github.com/krymtkts/blog-fable/pull/34) RSS の実装。
- [#31](https://github.com/krymtkts/blog-fable/pull/31) 出力パス構築を整理するために、手始めとしてエントリポイントの設定を整理。
- [#37](https://github.com/krymtkts/blog-fable/pull/37) 生 JS の排除。
- [#25](https://github.com/krymtkts/blog-fable/pull/25) [#28](https://github.com/krymtkts/blog-fable/pull/28) あと雑多な更新。

---

RSS はやっぱわたしがほぼ素人だったのもあってわからないことが多かった。
はじめは普段遣いしている [feedly](https://feedly.com/) で購読してみて目視で変なところを直した。
最終的には [Feed Validator Results: https://krymtkts.github.io/blog-fable/feed.xml](https://validator.w3.org/feed/check.cgi?url=https%3A%2F%2Fkrymtkts.github.io%2Fblog-fable%2Ffeed.xml) でエラーを解消する形にした。こんな便利なものがあるとは。とても役に立った。

RSS 周りの実装で改めて Fable が割る言い訳じゃないけど Fable のツラみを感じた。それは JavaScript の API がモロに出てくるところだ。
今回は feed の `pubDate` の表記を RFC 822 date format にする必要があって、 `Intl.DateTimeFormat` を使った。
こういうとき素直に .NET が使えるととても楽やのに...と思わざるを得ない。

いったんこういう ↓ 激薄 binding を書いて、 `obj` を引き回すことで無理くり使っている。

```fsharp
module Intl =
    [<Emit "new Intl.DateTimeFormat([$0], $1)">]
    let DateTimeFormat lang options = jsNative
```

```fsharp
module DateTime =
    open System

    let options: obj =
        !!{| weekday = "short"
             year = "numeric"
             month = "short"
             day = "2-digit"
             hour = "numeric"
             minute = "numeric"
             second = "numeric"
             hourCycle = "h23"
             timeZone = "Asia/Tokyo" // TODO: parametarize it.
             timeZoneName = "short" |}

    // TODO: write binding.
    let formatter: obj = Intl.DateTimeFormat "en-US" options
    let zonePattern = new Regex(@"GMT([+-])(\d+)")

    let toRFC822DateTime (d: DateTime) =
        let parts: obj [] = formatter?formatToParts (d)
        let p: string [] = parts |> Array.map (fun x -> x?value)
        let d = $"{p.[0]}{p.[1]}{p.[4]} {p.[2]} {p.[6]}"
        let t = (p.[8..12] |> String.concat "")

        let z =
            match p.[14] with
            | "UTC" -> "+0000"
            | z ->
                let item = zonePattern.Matches(z)
                let group = item.Item 0
                let op = (group.Groups.Item 1).Value
                let offset = int (group.Groups.Item 2).Value

                $"{op}%02d{offset}00"

        $"{d} {t} {z}"
```

日付操作のための module 入れればいいってだけでもあるけど、このためだけに module いれるのもなあ...となるべく自力で解決する方向。

ただ TODO 残してるのもあるけど、それより Dynamic typing で危なっかしいしちゃんと binding を書きたいところ。
あと添字でアクセスしまくるしか方法思いつかんかったけどもっとマシなのないのかな...

---

[#37](https://github.com/krymtkts/blog-fable/pull/37) で開発モードでの live reloading するために使ってた生 JavaScript を Fable が出力したやつを使うように変えた。

普通であれば webpack とかで bundling するんやろけど、開発モード以外で JavaScript を使う予定がないので、出力された js ファイルを出力先ディレクトリにコピることで実現している。
他に方法あるかわからん。
ただブラウザでもそのまま使える js ファイルを出力する豆知識は得られた。

```fsharp
open Browser.Dom
open Browser.WebSocket

let private init _ =
    let ws = WebSocket.Create $"ws://{window.location.host}/websocket"
    ws.onmessage <- fun _ -> window.location.reload ()

window.addEventListener ("load", init)
```

これ ↑ がこう ↓ なる。 `private` をつけておいたら `export` されない。

```js
function init(_arg) {
  const ws = new WebSocket(`ws://${window.location.host}/websocket`);
  ws.onmessage = (_arg_1) => {
    window.location.reload();
  };
}

window.addEventListener("load", (arg00$0040) => {
  init(arg00$0040);
});
```

---

[#31](https://github.com/krymtkts/blog-fable/pull/31) でエントリポイントの render 関数で諸々の設定をするように変えた。
元々好きに関数を組み合わせて出力を作れたらいいかなと思ってたけど、それぞれの関数で整合性を取らないといけない点があってめんどい、というのが設定を導入した理由。

ひとまずこれで外向きのインタフェースは固定して、あとは内側の重複したパス構築部分をまとめていけたら、楽に整合性取れるんじゃないかな。

8 月くらいにできるかなーって思ってたけど、使わない CSS を読んでるやつとかも使ってるやつだけビルドして出せるようにしたいとか、諸々考え出すと実際のところ無理かな。
気長にいこ。
