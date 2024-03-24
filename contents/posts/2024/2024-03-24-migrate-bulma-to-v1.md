---
title: "blog-fable を Bulma 1.0.0 に対応する"
tags: ["dotnet", "coverlet", "tips"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) に [query string selection](https://github.com/krymtkts/pocof/issues/44) を実装するのはそれなりに進んだ。
一段落ついたらまた日記にまとめたいが、その前に [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable)(このブログ) へデカ目の変更が降りてきたので、今日はそっちを書く。

---

デカ目の変更というのは、 [Bulma の 1.0.0](https://github.com/jgthms/bulma/releases/tag/1.0.0) のリリースだ。
捕捉してなかったから、これには正直驚いた。
どうも 2024-01 頃から準備が進んでたらしい。 [v1 release date? · Issue #3708 · jgthms/bulma](https://github.com/jgthms/bulma/issues/3708)

正直どこも賑わってなさそうなんやけど、 Hacker News にスレは立ってた。[The Bulma CSS framework reaches 1.0 | Hacker News](https://news.ycombinator.com/item?id=39790365)

blog-fable を作った(このブログを Fable で作り直した)のは 2023 で、当時の Bulma の最新は 0.9.4 だった。
0.9.4 が 2022-05 頃に[Build 0.9.4 · jgthms/bulma@3e00a8e](https://github.com/jgthms/bulma/commit/3e00a8e6d0d0e566d507328f0185ef84854effba)リリースされて以降の Bulma は、リリースがなかったわけだ。
GitHub にアクティビティはあったけど、スポンサーのロゴを差し替えたりって変更とかが多くて、割と Issue, Pull Request も積んでた感じ。

今どきはみんな [Tailwind CSS](https://tailwindcss.com/) を使うのだろうけど、 blog-fable では CSS を書く労力を減らしたかったから Bulma を使った。
Bulma なら class 指定しなくてもそこそこ default で style が利くので、構造自体に集中しやすい。
かつ JS 不要な CSS framework で、 Sass を使ってある程度 bundle を小さくできる余地があった。
Fable の Bulma wrapper である [Fulma](https://fulma.github.io/Fulma/) もあるけど、これは[当時フィーリングが合わなかったみたい](/posts/2023-06-04-rebuild-blog-with-fable-pt5.html)で採用しなかった。

---

昔話はこの程度にして、 Bulma 1.0.0 での変更点を見ていく。

[Migrating to Bulma v1 | Bulma: Free, open source, and modern CSS framework based on Flexbox](https://bulma.io/documentation/start/migrating-to-v1/)

Bulma 1.0.0 は全面的に [Dart Sass](https://sass-lang.com/dart-sass/) で書き直されたらしい。
`@import` も非推奨となり、 `@use` がちゃんと使えるようになったのは喜ばしい。

blog-fable ではカラースキームの変更と bundle 最小化のために、以下のように Sass を使ってスタイルを自作している。
カラースキームは Solarized で light/dark のモード切替なしで dark 固定だ。
Bulma 0.9.4 だと `@use` を使った場合、必要な `*.sass` だけインポートする方法だとエラーで使えなくて、古き良き `@import` を使っていた。

[blog-fable/sass/style.scss at b51df81f0ef32e3065c18a43248c6599f15d9da8 · krymtkts/blog-fable](https://github.com/krymtkts/blog-fable/blob/b51df81f0ef32e3065c18a43248c6599f15d9da8/sass/style.scss)

```scss
@charset "utf-8";

// NOTE: based on https://ethanschoonover.com/solarized/
$base03: #002b36;
$base02: #073642;
$base01: #586e75;
$base00: #657b83;
$base0: #839496;
$base1: #93a1a1;
$base2: #eee8d5;
$base3: #fdf6e3;
$yellow: #b58900;
$orange: #cb4b16;
$red: #dc322f;
$magenta: #d33682;
$violet: #6c71c4;
$blue: #268bd2;
$cyan: #2aa198;
$green: #859900;

// // NOTE: Solarized Light.
// $black: $base03,
// $black-bis: $base02,
// $black-ter: $base02,
// $grey-darker: $base00,
// $grey-dark: $base00,
// $grey: $base01,
// $grey-light: $base0,
// $grey-lighter: $base1,
// $grey-lightest: $base1,
// $white-ter: $base2,
// $white-bis: $base2,
// $white: $base3,
// $background: $base3,
// $code: $base01,
// $tag-background-color: $base3,
// NOTE: Solarized Dark.
$black: $base3;
$black-bis: $base2;
$black-ter: $base2;
$grey-darker: $base1;
$grey-dark: $base1;
$grey: $base0;
$grey-light: $base00;
$grey-lighter: $base01;
$grey-lightest: $base01;
$white-ter: $base02;
$white-bis: $base02;
$white: $base03;
$background: $base03;
$code: $base0;
$tag-background-color: $base03;
$hr-background-color: $base01;

$orange: $orange;
$yellow: $yellow;
$green: $green;
$turquoise: $cyan;
$cyan: $cyan;
$blue: $blue;
$purple: $violet;
$red: $red;

$code-size: 1em;
$link-hover: $violet;
$section-padding: 1.5rem;
$section-padding-desktop: 1.5rem;
$body-line-height: 1.7;

@import "../node_modules/bulma/sass/utilities/_all.sass";
@import "../node_modules/bulma/sass/base/_all.sass";
@import "../node_modules/bulma/sass/components/navbar.sass";
@import "../node_modules/bulma/sass/components/tabs.sass";
@import "../node_modules/bulma/sass/elements/button.sass";
@import "../node_modules/bulma/sass/elements/container.sass";
@import "../node_modules/bulma/sass/elements/content.sass";
@import "../node_modules/bulma/sass/elements/image.sass";
@import "../node_modules/bulma/sass/elements/tag.sass";
@import "../node_modules/bulma/sass/elements/title.sass";
@import "../node_modules/bulma/sass/form/shared.sass";
@import "../node_modules/bulma/sass/form/checkbox-radio.sass";
@import "../node_modules/bulma/sass/layout/footer.sass";
@import "../node_modules/bulma/sass/layout/section.sass";

// NOTE: adjust non-parameterized styles.
.content a {
  text-decoration: underline;
}
a:visited {
  color: $purple;
}
.checkbox {
  margin-right: 0.5em;
}
main {
  padding-bottom: 1.5em;
}
code {
  word-wrap: break-word;
}
code,
pre,
.tag:not(body) {
  border: 1px solid $grey-lighter;
  border-radius: $radius;
}
pre code {
  border: none;
  line-height: 1.5;
}
.content {
  li:first-child {
    margin-top: 0.25em;
  }
}
.date {
  color: $base01;
}
.prev a:before {
  content: "\2190";
}
.next a:after {
  content: "\2192";
}
```

Bulma 1.0.0 になったことで普通に使う分には非推奨のコンポーネントが出たくらいだろうが、昨日試してみた感じだと、先述したような独自のスタイルを構築している場合に結構影響を受ける感じだった。
なんか従来通りカラースキームを指定しても背景に使われないのよね。

試行錯誤して、 [automatic Dark Mode](https://bulma.io/documentation/features/dark-mode/) と [CSS variables](https://bulma.io/documentation/features/css-variables/) の仕組みを理解してうまく使えば、ひとまずいい感じにできそうとわかってきた。

automatic Dark Mode の場合、 light ↔ dark の色を [`hsl`, `hsla`](https://developer.mozilla.org/en-US/docs/Web/CSS/color_value/hsl) で計算して出してる。
そのベースとなる CSS variable に色を指定する必要があるみたい。
0.9.4 のときは単純に `$black` とかの色の Sass の変数を変えるだけでいけた。
けど、 1.0.0 からは `--bulma-scheme-h`, `--bulma-scheme-s`, `--bulma-scheme-l` 等で色相、彩度、輝度を指定しないといけないみたい。

`button` や `tag` などのコンポーネントはそれで背景色が決まるので、ここを攻略しないとどうにもならん。
最悪彩度と輝度に無効な値を指定すれば無視できるのはわかったが、きちんと指定しておきたい。
でもまだドキュメント読んでもあんまわからん。ドキュメントではの変数を参照してるとか調べるのが難しい。

ということで、開発者ツールで手探りして以下を設定してみたら、今のところほぼ同じにできた。
ほぼと書いたのは、箇条書きの line height が微妙に違いそうだからだ。
また、 `button` の border の色が `hsl` で勝手に定まるのだけ対処法わからなかったので style を上書きした。
light/dark の style を 2 箇所ずつ書かないといけないので、コピペしなくていいよう `@mixin` にまとめてある。

[blog-fable/sass/style.scss at aa668a3e991a46e9b3b3d0302bdbc717d146665e · krymtkts/blog-fable](https://github.com/krymtkts/blog-fable/blob/aa668a3e991a46e9b3b3d0302bdbc717d146665e/sass/style.scss)

```scss
@charset "utf-8";

// NOTE: based on https://ethanschoonover.com/solarized/
$sl-base03: #002b36;
$sl-base02: #073642;
$sl-base01: #586e75;
$sl-base00: #657b83;
$sl-base0: #839496;
$sl-base1: #93a1a1;
$sl-base2: #eee8d5;
$sl-base3: #fdf6e3;
$sl-yellow: #b58900;
$sl-orange: #cb4b16;
$sl-red: #dc322f;
$sl-magenta: #d33682;
$sl-violet: #6c71c4;
$sl-blue: #268bd2;
$sl-cyan: #2aa198;
$sl-green: #859900;

@use "../node_modules/bulma/sass" as * with (
  $orange: $sl-orange,
  $yellow: $sl-yellow,
  $green: $sl-green,
  $turquoise: $sl-cyan,
  $cyan: $sl-cyan,
  $blue: $sl-blue,
  $purple: $sl-violet,
  $red: $sl-red,

  $code-size: 1em,

  $body-line-height: 1.7,

  $section-padding: 1.5rem,
  $section-padding-desktop: 1.5rem
);

@mixin light-theme {
  // NOTE: Solarized Light.
  --bulma-black: #{$sl-base03};
  --bulma-black-bis: #{$sl-base02};
  --bulma-black-ter: #{$sl-base02};
  --bulma-grey-darker: #{$sl-base00};
  --bulma-grey-dark: #{$sl-base00};
  --bulma-grey: #{$sl-base01};
  --bulma-grey-light: #{$sl-base0};
  --bulma-grey-lighter: #{$sl-base1};
  --bulma-grey-lightest: #{$sl-base1};
  --bulma-white-ter: #{$sl-base2};
  --bulma-white-bis: #{$sl-base2};
  --bulma-white: #{$sl-base3};

  --bulma-text: #{$sl-base01};
  --bulma-text-strong: #{$sl-base01};

  --bulma-scheme-main: #{$sl-base3};
  --bulma-scheme-main-bis: #{$sl-base2};
  --bulma-scheme-main-ter: #{$sl-base2};

  --bulma-scheme-h: 44;
  --bulma-scheme-s: 87%;
  --bulma-scheme-l: 94%;

  --bulma-background-l: 94%;

  --bulma-scheme-main-s: 87%;
  --bulma-scheme-main-bis-s: 87%;
  --bulma-scheme-main-ter-s: 87%;
  --bulma-scheme-main-l: 94%;
  --bulma-scheme-main-bis-l: 94%;
  --bulma-scheme-main-ter-l: 94%;

  --bulma-code: #{$sl-base00};

  --bulma-hr-background-color: #{$sl-base1};

  --bulma-border: #{$sl-base1};
}

@mixin dark-theme {
  // NOTE: Solarized Dark.
  --bulma-black: #{$sl-base3};
  --bulma-black-bis: #{$sl-base2};
  --bulma-black-ter: #{$sl-base2};
  --bulma-grey-darker: #{$sl-base1};
  --bulma-grey-dark: #{$sl-base1};
  --bulma-grey: #{$sl-base0};
  --bulma-grey-light: #{$sl-base00};
  --bulma-grey-lighter: #{$sl-base01};
  --bulma-grey-lightest: #{$sl-base01};
  --bulma-white-ter: #{$sl-base02};
  --bulma-white-bis: #{$sl-base02};
  --bulma-white: #{$sl-base03};

  --bulma-text: #{$sl-base1};
  --bulma-text-strong: #{$sl-base1};

  --bulma-scheme-main: #{$sl-base03};
  --bulma-scheme-main-bis: #{$sl-base02};
  --bulma-scheme-main-ter: #{$sl-base02};

  --bulma-scheme-h: 192;
  --bulma-scheme-s: 100%;
  --bulma-scheme-l: 11%;

  --bulma-background-l: 11%;

  --bulma-scheme-main-s: 100%;
  --bulma-scheme-main-bis-s: 100%;
  --bulma-scheme-main-ter-s: 100%;
  --bulma-scheme-main-l: 11%;
  --bulma-scheme-main-bis-l: 11%;
  --bulma-scheme-main-ter-l: 11%;

  --bulma-code: #{$sl-base0};

  --bulma-hr-background-color: #{$sl-base01};

  --bulma-border: #{$sl-base01};
}

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

// NOTE: adjust non-parameterized styles.
.content a {
  text-decoration: underline;
}
a:visited {
  color: $purple;
}
.checkbox {
  margin-right: 0.5em;
}
main {
  padding-bottom: 1.5em;
}
code {
  word-wrap: break-word;
}
code,
pre,
.tag:not(body),
a.button {
  border: 1px solid $sl-base01;
  border-radius: $radius;
}
pre code {
  border: none;
  line-height: 1.5;
}
.content {
  li:first-child {
    margin-top: 0.25em;
  }
}
.date {
  color: $sl-base01;
}
.prev a:before {
  content: "\2190";
}
.next a:after {
  content: "\2192";
}
```

とりまこっから個々の `*.scss` を `@use` するように変えてみて、 bundle を縮小したい。
いま 68.7kb もあってやっぱデカいなと思わせるものがある。

あと手動で light/dark mode を切り替える実装について。

ドキュメントにも記載あるが、 Bulma の automatic dark mode は [`prefers-color-scheme`](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-color-scheme) の media query を使ってる。
そこで利用者自身が light/dark mode を使い分けるには `data-theme="dark"` を指定するようにしないとダメっぽい。
先に挙げた SCSS で `[data-theme="dark"] { ~ }` 等としているのはそのためだ。
システムのカラースキームに合わせて反対の色を定義しないといけないのが、わかるまでちょっとつまづいた。

このあと、 HTML に `data-theme="dark"` や `data-theme="light"` を差し込む機能を、画面のどっかに設ける必要がある。
↓ こういうのを Fable で作らないといけない。

```js
document.documentElement.setAttribute("data-theme", "dark");
```

はじめはどうなるかと不安に思ったが、気合い入れたら終わり見えて良かった。

Bulma も 1.0.0 が出たばかりだしちょっとの間は更新リリースが続くんじゃないかな。
テクノロジー的に「枯れる」というのは確かにあるが、取り巻く環境の変化に合わせて rewrite されていくのは良いことやな。素直に尊敬するわ。
blog-fable でも自力での対応を放置してた light/dark mode に棚ぼたであやかれて嬉しい限り。

つづく。
