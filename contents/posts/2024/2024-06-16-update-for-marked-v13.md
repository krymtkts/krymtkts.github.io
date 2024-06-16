---
title: "blog-fable を Marked v13 に対応する"
tags: ["marked", "nodejs", "fable", "fsharp"]
---

今日は [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の話を書く。

[このブログ](https://github.com/krymtkts/krymtkts.github.io)の基盤 krymtkts/blog-fable はコンテンツを Markdown で管理していて、 Markdown を HTML にコンパイルするのに [markedjs/marked](https://github.com/markedjs/marked) を使っている。

つい先日 marked が v13 になったらしく Dependabot が PR を投げてきたので merge したらブログ記事が壊れた 💥。
ちゃんと調べてなかったのだけど、 v13 では parser に破壊的な変更があって、仕込んでいる Marked Extension が壊れたようだった。

[Release v13.0.0 · markedjs/marked](https://github.com/markedjs/marked/releases/tag/v13.0.0)

Marked v13.0.0 のリリースノートから変更前後のコードを拝借すると以下の通り。

```javascript
// v12 renderer extension

const extension = {
  renderer: {
    heading(text, level) {
      // increase level by 1
      return `<h${level + 1}>${text}</h${level + 1}>`;
    },
  },
};

// v13 renderer extension

const extension = {
  useNewRenderer: true,
  renderer: {
    heading(token) {
      // increase depth by 1
      const text = this.parser.parseInline(token.tokens);
      const level = token.depth;
      return `<h${level + 1}>${text}</h${level + 1}>`;
    },
  },
};
```

雑に言えば、 renderer の各要素に対応付く関数が parse 済み文字列を受け取っていたのが、 token を受け取って自身で parse するように変わった。

blog-fable では Markdown を HTML に変換する際 `heading` `link` `listitem` `checkbox` `image` の renderer を拡張している。
ここが壊れたわけだったので、関数の引数を変えたらいいだけと簡単に考えていた。
ただ結構手こずった。

Marked の新しい renderer は引数に [destructuring assignment](https://www.typescriptlang.org/docs/handbook/variable-declarations.html#destructuring) を使ってる。

この辺。 [marked/src/Renderer.ts at 70bb55e0af5128a657a14b8b25d7d406661e6936 · markedjs/marked](https://github.com/markedjs/marked/blob/70bb55e0af5128a657a14b8b25d7d406661e6936/src/Renderer.ts)

型定義ファイルだとこの様になっていた。

```typescript
/**
 * Renderer
 */
declare class _Renderer {
  options: MarkedOptions;
  parser: _Parser;
  constructor(options?: MarkedOptions);
  space(token: Tokens.Space): string;
  code({ text, lang, escaped }: Tokens.Code): string;
  blockquote({ tokens }: Tokens.Blockquote): string;
  html({ text }: Tokens.HTML | Tokens.Tag): string;
  heading({ tokens, depth }: Tokens.Heading): string;
  hr(token: Tokens.Hr): string;
  list(token: Tokens.List): string;
  listitem(item: Tokens.ListItem): string;
  checkbox({ checked }: Tokens.Checkbox): string;
  paragraph({ tokens }: Tokens.Paragraph): string;
  table(token: Tokens.Table): string;
  tablerow({ text }: Tokens.TableRow): string;
  tablecell(token: Tokens.TableCell): string;
  /**
   * span level renderer
   */
  strong({ tokens }: Tokens.Strong): string;
  em({ tokens }: Tokens.Em): string;
  codespan({ text }: Tokens.Codespan): string;
  br(token: Tokens.Br): string;
  del({ tokens }: Tokens.Del): string;
  link({ href, title, tokens }: Tokens.Link): string;
  image({ href, title, text }: Tokens.Image): string;
  text(token: Tokens.Text | Tokens.Escape | Tokens.Tag): string;
}
```

これらを Fable だとどうしたらいいのかわからなかった。
分解後のパラメータを受け取るのか？的な。
だが試しに動かして調べたら、結局のところ関数に渡ってくるのはすべて `Token.Xxx` だった。
なので binding では destructuring assignment を無視したらいいみたい。
以下のように Marked の binding を調整したらうまくいった。

```fsharp
    type Renderer = Renderer<obj>

    [<AllowNullLiteral>]
    type Renderer<'T> =
        abstract options: MarkedOptions with get, set

        abstract code: item: Tokens.Code -> string
        abstract blockquote:  item: Tokens.Blockquote -> string
        abstract html:  item: Tokens.HTML -> string
        abstract heading: item: Tokens.Heading -> string
        abstract hr:  item: Tokens.Hr -> string
        abstract list:  item: Tokens.List -> string
        abstract listitem: item: Tokens.ListItem -> string
        abstract checkbox:  item: Tokens.Checkbox -> string
        abstract paragraph:  item: Tokens.Paragraph -> string
        abstract table:  item: Tokens.Table -> string
        abstract tablerow:  item: Tokens.TableRow -> string
        abstract tablecell: item: Tokens.TableCell -> string
        abstract strong:  item: Tokens.Strong -> string
        abstract em:  item: Tokens.Em -> string
        abstract codespan:  item: Tokens.Codespan -> string
        abstract br:  item: Tokens.Br -> string
        abstract del:  item: Tokens.Del -> string
        abstract link: item: Tokens.Link -> string
        abstract image: item: Tokens.Image -> string
        abstract text:  item: Tokens.Text -> string
```

いま blog-fable で使ってる Marked の binding は使ってない型もいっぱい入ってるからわかりにくいし、いつか整理したい。
今回はいくつかをコメントアウトするだけに留めた。
なんならこういう破壊的な変更によるメンテ作業避ける名目で Markdown の parser & compiler を Fable で書くのもいいのかも。
これはやりたいことに積んどこう。

あと renderer 内で parser を呼び出す際の注意点もあった。
block 要素と inline 要素で parser method を使い分ける必要があって、例えば block の token を parseInline に渡すとエラーになってしまう。
エラーにしてるのはこの辺。

[marked/src/Parser.ts at 70bb55e0af5128a657a14b8b25d7d406661e6936 · markedjs/marked](https://github.com/markedjs/marked/blob/70bb55e0af5128a657a14b8b25d7d406661e6936/src/Parser.ts#L194-L202)

blog-fable とこのブログでは、今のところ `listitem` だけ block を含む可能性があるようだった。
そういう要素は [`parseInline`](https://github.com/markedjs/marked/blob/70bb55e0af5128a657a14b8b25d7d406661e6936/src/Parser.ts#L135-L206) の代わりに [`parse`](https://github.com/markedjs/marked/blob/70bb55e0af5128a657a14b8b25d7d406661e6936/src/Parser.ts#L42-L130) を使うことで対処できるが、 余計な `p` で囲まれるのは少し気になった。

これで多分ブログがそこそこ従来通りの表示をするようになったはずだ。
ぶっ壊れたまま気づいてない点とかあるだろうけど、今後見つけたら直すという方針でいこう。
