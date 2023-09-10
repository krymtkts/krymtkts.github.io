{:title "Fable でブログを再構築する pt.12"
:layout :post
:tags ["fsharp", "fable"]}

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

色調整して Accessibility 上げたかったけど Solarized Dark を使おうとしたらどうにもならんので諦めた。

[Elements must meet minimum color contrast ratio thresholds | Axe Rules | Deque University | Deque Systems](https://dequeuniversity.com/rules/axe/4.7/color-contrast)

ここで Foreground と Background を色比較してコントラスト比を見ることができる。
AAA に合格する比率まで色を明るくしたらもう印象が違い過ぎるから、やめた。

コードブロックやリンクがあると、それだけで Accessibility のスコアが下がるのでどうにもならん。

あと以下にも抵触してるみたい。

[Links must be distinguishable without relying on color | Axe Rules | Deque University | Deque Systems](https://dequeuniversity.com/rules/axe/4.7/link-in-text-block)

今はリンクと本文の違いが色しかないから、これに下線を足したりすればましになる気配はある。

Accessibility て難しいなーと改めて思う。

---

他には、本格的な乗り換えに備えて以下を行った。

- 公開日や更新日の表示忘れ対応とドキュメント更新 [#48](https://github.com/krymtkts/blog-fable/pull/48)
- サンプルの更新 [49](https://github.com/krymtkts/blog-fable/pull/49)

初めから乗り換えを目的にやってるけど、いざ手をつける段階になったらこれまた色々気になってくるので、なるべく処理してからやろうとしてる。
やれ dev server が反応しなくなる時があるとか、 TODO は乗り換えまでに解消しておきたいなとか。

乗り換え時には [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) と [krymtkts/krymtkts.github.io](https://github.com/krymtkts/krymtkts.github.io) の歴史を合流するつもりやから、先にできるだけ課題を潰しておきたいというのがある。
頻繁に両方メンテとかはやりたくなくて、なんか改善や変えたいことがあったら [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の方で試してからブログの方に投入するみたいなフローでやりたい。

歴史を合流する方法以外にコードを統合するマトモな術は、 Fable Module とか dotnet のテンプレ化あたり。
けどその辺に手を出すと注力するところが主目的から外れるので、やらないつもり。

npm package を含んだ Fable Module の作成は色々気を使うことがあり、ここに力割くのもどうかなって。
[Fable · Author a Fable library](https://fable.io/docs/your-fable-project/author-a-fable-library.html)

dotnet のテンプレ化に関しても、仮に誰か奇特な人が Fable でブログを作りたくて repo に辿り着いたとき便利だなとかその程度かな？
template repository になってても同じ感じか。

私的ブログの新しいコードを書いてるだけなので、機能を公開することに重きを置かず、一番楽にコードを統合できる歴史を合流する術で進めるという感じ。

まだやっときたいこと諸々あれど、ひとまず [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) を作り始めた当初の目標である年内乗り換えは現実味を帯びてきた気がする。
2023-10 末で期間としては半年になるから、その辺から年末の間でいい感じに移行したいなー。
