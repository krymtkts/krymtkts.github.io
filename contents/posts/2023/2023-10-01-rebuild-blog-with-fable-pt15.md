---
title: "Fable でブログを再構築する pt.15"
tags: ["fsharp", "fable"]
---

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

TODO の回収と細々とした使い勝手の改善など
[#61](https://github.com/krymtkts/blog-fable/pull/61)
[#62](https://github.com/krymtkts/blog-fable/pull/62)
[#64](https://github.com/krymtkts/blog-fable/pull/64)
をした。
内部のリファクタリングなり実装忘れの機能追加をしてきて、かなり移行への気持ちも高まってきた(毎回言ってる)。

その中でも自分でいい感じにできたんじゃないかなという所は、 [#63](https://github.com/krymtkts/blog-fable/pull/63)
で `Intl.DateTimeFormat` のごく小さな binding を書いてみたところ。

調べた感じ、昔の Fable.Import.Browser にはあったけど、分割されたあとなくなってるぽい。

[Import of 'intl' package fails in production mode · Issue #1925 · fable-compiler/Fable](https://github.com/fable-compiler/Fable/issues/1925)
[fable-import/Browser/Fable.Import.Browser.fs at a24f2c51bcfc79737427613e5c4aa5e99e114d40 · fable-compiler/fable-import](https://github.com/fable-compiler/fable-import/blob/a24f2c51bcfc79737427613e5c4aa5e99e114d40/Browser/Fable.Import.Browser.fs#L12129-L12241)

分割先のモジュールを調べるにはこの Issue を見る → [Track repo splits · Issue #80 · fable-compiler/fable-import](https://github.com/fable-compiler/fable-import/issues/80)。

これを参考に足りない部分を補う形で以下のように記述した。
自分が使いたい `formatToParts` `DataTimeFormatPart` はなかったから自前で書いて、 `DateTimeFormatOptions` は違いなし。この `Create` の書き方は学びになった。
MDN を参考にしたら簡単だったのでふつーにいけた。 [Intl.DateTimeFormat.prototype.formatToParts() - JavaScript | MDN](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Intl/DateTimeFormat/formatToParts)

```fsharp
// NOTE: minimum implementation for Intl.DateTimeFormat.formatToParts.
[<RequireQualifiedAccess>]
module Intl =
    [<Global>]
    let DateTimeFormat: DateTimeFormat = jsNative

    [<AllowNullLiteral>]
    type DateTimeFormatOptions =
        abstract weekday: string with get, set
        abstract year: string with get, set
        abstract month: string with get, set
        abstract day: string with get, set
        abstract hour: string with get, set
        abstract minute: string with get, set
        abstract second: string with get, set
        abstract hourCycle: string with get, set
        abstract timeZone: string with get, set
        abstract timeZoneName: string with get, set

    [<AllowNullLiteral>]
    type DateTimeFormatPart =
        abstract ``type``: string with get, set
        abstract value: string with get, set

    [<AllowNullLiteral>]
    type DateTimeFormat =
        [<Emit "new Intl.$0($1, $2)">]
        abstract Create: lang: string -> options: DateTimeFormatOptions -> DateTimeFormat

        abstract formatToParts: date: System.DateTime -> DateTimeFormatPart array
```

コレを書いたことでちょっとは安心感持って使えるかな？という感じやが、
そもそも書き直すことあんまなさそうなのと結局戻り値はサイズ固定配列なので、そこまだなんとかしたい感じ。

ただ小さい binding でも書いてみることで Fable binding への理解度上がった感ある。

あと完全に忘れてたのが、画像ファイルがあった場合に出力先へコピる機能。
初めは、 favicon とか画像類はスクリプトを手でいじって好きにやる想定にしてたけど途中で変えたから、まるっと忘れてた。
機能的にはそんなもんちゃうかな？結構機能は削りまくってるのでなんか忘れてそうではあるが。

あと気になるのはパフォ面。
Fable のビルドは遅いけどそれ以外は結構速いから期待できんじゃないかな。
Cryogen での SSG は記事が多いともっさりしてて 151 記事で `lein run` の終了まで 25 秒くらかかる。
8 記事しかない状態で 4 秒未満のとこしか見てないから、 Cryogen と同じ記事数でどうなるか...
