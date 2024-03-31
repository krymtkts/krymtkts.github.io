---
title: "Bulma 1.0.0 の automatic dark mode を制御する"
tags: ["bulma", "sass", "fable"]
---

今回も引き続き Bulma 1.0.0 ネタだ。

[前回 Bulma 1.0.0 に合わせてスタイル調整した](/posts/2024-03-24-migrate-bulma-to-v1.html)話を書いた。
加えて Bulma 1.0.0 で [automatic Dark mode](https://bulma.io/documentation/features/dark-mode/) が導入されたのにも触れたが、これをうまく制御するのに多少のコツが必要だっので記しておく。

ドキュメントにも記載あるが、 Bulma の automatic Dark mode は [`prefers-color-scheme`](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-color-scheme) の media query を使ってる。これが automatic Dark mode の automatic 正体だ。
手動で light/dark mode を使い分けるには `data-theme` を指定する。この `data-theme="light` `data-theme="dark"` 属性を HTML に差し込んだり、 ユーザごとに設定を保存するのは、 JavaScript を書かないといけない。

そういう仕組みなので light/dark mode に関する style は以下のようにするのが多分 Bulma の推奨パターンのはず。

[blog-fable/sass/style.scss at aa668a3e991a46e9b3b3d0302bdbc717d146665e · krymtkts/blog-fable](https://github.com/krymtkts/blog-fable/blob/aa668a3e991a46e9b3b3d0302bdbc717d146665e/sass/style.scss)

```scss
@media (prefers-color-scheme: light) {
  :root {
    @include light-theme();
  }

  [data-theme="dark"] {
    @include dark-theme();
  }
}

@media (prefers-color-scheme: dark) {
  :root {
    @include dark-theme();
  }

  [data-theme="light"] {
    @include light-theme();
  }
}
```

このように `prefers-color-scheme` の内側に逆の色の `[data-theme="*"]` を仕込むことで、システムのテーマ設定に関わらず指定のカラーテーマにできる。
システムにテーマを表明されないときは `prefers-color-scheme` が `light` になるらしい。
確認したことはないが、古い OS でカラーテーマが取れないときとか？ [prefers-color-scheme - CSS: Cascading Style Sheets | MDN](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-color-scheme)

ただ、この仕組みにしていると多少問題がある。
`prefers-color-scheme` が取れるタイミングと `[data-theme="*"]` が取れるタイミングに差がある実装だと、ページ遷移時にスタイルの切り替わりで画面がちらつく。
「`prefers-color-scheme` のカラースキーマ反映 -> JavaScript で `data-theme` 設定される -> `data-theme` のカラースキーマ反映」となるとダメ。
このチラつきは [Bulma の official page](https://bulma.io/) でも起こってる。

これを解消するには、HTML 文書で JavaScript → CSS の読み込み順になるよう調整し、かつ JavaScript 読み込み時にユーザ設定値を復元して `data-theme` に設定する。
ただし注意点としては、 `data-theme` の設定は JavaScript 読み込み時にやりたくても、テーマ切り替えのボタンに event を binding するのは後回しの方が良さそう。
blog-fable ではあまりに早過ぎて event を bind したい要素が準備されてなかったようで bind できなかったので、 `load` event で binding するようにした。

以下 blog-fable の例。 [blog-fable/src/Handler.fs](https://github.com/krymtkts/blog-fable/blob/cc19e2bdbe6b4fb7d802ca8772a53d17cd11156b/src/Handler.fs)

```fsharp
module Handler

open Browser.Dom
open Browser.WebStorage

// NOTE: Don't use Fable library in this file because it is directly bundled int HTML files.
//       This includes, discriminated union, List, etc.

let private themeKey = "theme-mode"
let private themeAttributeName = "data-theme"

let private setThemeMode (t: string) =
    match t with
    | "light"
    | "dark" ->
        localStorage.setItem (themeKey, t)
        document.documentElement.setAttribute (themeAttributeName, t)
    | _ ->
        localStorage.removeItem themeKey
        document.documentElement.removeAttribute themeAttributeName

localStorage.getItem "theme-mode" |> setThemeMode

let private init _ =
    let els = document.querySelectorAll (".theme-toggle")

    for i = 0 to els.length - 1 do
        let el = els.item (i)
        let themeMode = el.getAttribute "data-theme"
        el.addEventListener ("click", (fun _ -> setThemeMode themeMode))

window.addEventListener ("load", init)
```

blog-fable は F# からトランスパイルした JavaScript を成果物にコピっているだけなので、 Fable のライブラリのインポートを必要とするコードは書けないという縛りがある。
なので discriminated union や `List.*` といったものも使わない質素な実装になっている。
ここまでやったら、 blog-fable のページ遷移に限ってかも知れないがチラつきは一切起こらなくなり、マニュアルでのテーマ切り替えも動作するようになった。

---

他にもちまちまとスタイルの補正をしている箇所はあるが、これにて大体 Bulma 1.0.0 対応ができたもといえそう。
前より自作スタイルの CSS のサイズが 2 倍超デカくなってるのは気になるが、これも少しずつ改善重ねていけれたらよいか。
