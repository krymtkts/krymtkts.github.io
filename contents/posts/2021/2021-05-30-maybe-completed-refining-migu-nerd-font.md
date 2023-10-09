---
title: "Migu Nerd Font の改善が完了したっぽい"
tags: ["font"]
---

(2021-05-16 に書いたまま投稿するのを忘れていた)

[以前](/posts/2021-05-07-i-want-to-resize-migu-icon.html)の続き。まだやってる。

横も縦も微妙にサイズが合わないのであれば、強制的に矯正するしかない！ということでまた fontmerger に機能追加した。[contour.boundingBox](https://fontforge.org/docs/scripting/python/fontforge.html#fontforge.contour.boundingBox)は結局やめた。今のコードでもフォント設定を分ければ実現が容易だったからだ。

- フォントごとの `scale` オプションを x,y 軸で 2 つのオプション(`scale_x`, `scale_y`)に分割
- narrow 幅に矯正する `force_narrow` オプションを追加

[Split scale option to x and y scale. Add force_narrow option that for… · krymtkts/fontmerger@690830d](https://github.com/krymtkts/fontmerger/commit/690830d0842a21445d7ca4e3aa367e1bbf859c31)

コードは愚直に書いただけで Cognitive Complexy が 16 を突破してしまったが、期待の通りのフォント変換ができた模様。
これにより残念だった Powerline の隙間・見切れ問題が解決したものと思う。これで現時点では完璧や...という Migu になったので当分は使用を確かめてみようと思う。

![現在のpowerline](/img/2021-05-16-terminal/mypowerline.png)

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
- [x] font-merger の scale オプションを x,y で分割、強制 narrow 幅オプション追加
- [x] 完璧な Migu の完成！

完
