---
title: "Migu Nerd Font のアイコンフォントを narrow にしたい"
tags: ["font"]
---

[以前](/posts/2021-04-11-consideration-of-difference-between-cascadia-and-migu.html)、Nerd Fonts の`font-patcher`で Migu に Narrow サイズの Symbol グリフぶちこみゃええやんけ、まで来てた。
その後を記す。

### Nerd Fonts

Nerd Font の font-patcher を試す。事前にパッチ済みの M+を見ると完璧なので期待に胸が高ぶる。実行してみると Python モジュールに Windows のライブラリが含まれていたので WSL2 内の Ubuntu では実行できなかった。Windows で実行する。

```powershell
fontforge -script font-patcher migu-1m-regular.ttf -s -l -w -c --careful --progressbars -out patched
```

結果、全然アカン... `-s, --mono, --use-single-width-glyphs`オプションを有効にすると全グリフが single-width になってしまい日本語フォントとして使いものにならなくなる。

逆にこの完璧な M+が惚れ惚れする出来なので、こちらに半濁音を移植するだけでいい気がしてきた。

## font-merger 再び

コードポイントがわからないので、取得するための関数を PowerShell で作る。

```powershell
function UC {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true,
            Position = 0,
            ValueFromPipeline = $true,
            ValueFromPipelineByPropertyName = $true)]
        [ValidateNotNullOrEmpty()]
        [String[]]$s
    )
    process {
        foreach ($c in $s) {
            [Convert]::ToInt32($c -as [char]).ToString("x")
        }
    }
}

('がぎぐげござじずぜぞだぢづでどばぱびぴぶぷべぺぼぽゔ'+''+''+'ゞガギグゲゴザジズゼゾダヂヅデドバパビピブプベペボポヴヷヸヹヺ').ToCharArray() | UC
```

これで置き換えたい文字(Migu のチャーミングな濁音半濁音たち)のコードポイントを取れるよにした。
特定のコードポイントの移植といえば font-merger 使えるやん！というところではあるが、ほしいコードポイントは連続せず細切れになっているので、これを 1 個ずつ設定に書くのはめんどい...
ということで font-merger の改造をした。

[Add codepoint option to copy glyph from specific code points. · krymtkts/fontmerger@33c775e](https://github.com/krymtkts/fontmerger/commit/33c775ef4f1fd47f90b6359f5ae74529552a87a6)

```sh
/usr/bin/fontforge -script fontmerger/__init__.py -x migu-1m-regular -o patched --suffix=migu -- ./source/M+1mNerdFontCompleteWindowsCompatible.ttf
```

コードポイントを個別に指定できるようにしまして実行したところ、なんかそれっぽく半濁点が反映されてる。
いや待てよ...微妙に縦長になってしまった...これは M+と Migu でフォントの縦横比が異なるせいやろな。

もう FontForge のスクリプティング真面目にやってくしか残された将来はない気がしてきた。😰

## font-merger 再び 再び

Nerd Fonts の font-patcher を読んでいるときに気づいたのが、`--mono` オプションを有効にしている場合すべての glyph に narrow 幅を設定するようになっていたこと。

[nerd-fonts/font-patcher at master · ryanoasis/nerd-fonts · GitHub](https://github.com/ryanoasis/nerd-fonts/blob/master/font-patcher#L71-L74)

日本語フォントの場合 wide 幅を書き換えてしまうから先述の通り Nerf Fonts はえらいことになってしまっていたので、これをパッチするグリフにのみ適用すれば良いという力技に気づく。
ということで更に font-merger を改造した。

[Adds mono command line option that forces glyph to be single width. · krymtkts/fontmerger@25ed2b1](https://github.com/krymtkts/fontmerger/commit/25ed2b1dfd547d6599417793fb679ea9fdbe4548)

## 現状

これにより全ての追加したアイコングリフが narrow 幅になることで、漸く Windows Terminal でもﾐﾆﾐﾆアイコンフォントにならずに表示できるようになった。

![現在のpowerline。左向き三角が残念](/img/2021-05-07-terminal/mypowerline.png "現在のpowerline。左向き三角が残念")

しかしご覧の通り、右に寄るべきフォントの見た目が非常に残念なので、今後もちょいちょいいじらねばならない。完成はいつになるのか...😪
(幅 500 以上になってるためずれる)

なんとなく、次は Powerline グリフの narrow 幅を大きく超過するものを左右の align をつけて変形しないといけない気がしている。
この辺の頂点の xmax, ymax からどないか計算できんかな →
[fontforge — FontForge 20201107 documentation](https://fontforge.org/docs/scripting/python/fontforge.html#fontforge.contour.boundingBox)

- [x] お亡くなりになられた fontmerger を Python3 化して動かす
- [x] 最新の Migu に対して fontmerger で Nerd font patch する
- [x] `0xE0B0` を始めとした Cascadia でだけうまく表示されるグリフを Migu に移植する
  - そして効果なし！
- [x] Nerd Fonts の font-patcher で Migu にパッチしてみる
  - フォントが使い物にならなくなった
- [x] Migu のチャーミングな部分を M+に移植
  - 縦横比の違いから縦長に...
- [x] font-merger がパッチするグリフのみ narrow 幅にしてみる
  - おしい！右よりフォントが残念
- [ ] 完璧な Migu の完成！
  - To Be Continued...
