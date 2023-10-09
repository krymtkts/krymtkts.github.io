---
title: "Terminal-Icons のアイコングリフのコードポイントを変えたい"
tags: ["font","powershell"]
---

先日、[わたしの改造 Migu で Terminal-Icons の見栄えが悪い話](/posts/2021-07-01-i-want-to-change-codepoint.html)を書いた。
あの後、チマチマ作業を行い、ある程度納得の行くものが出来上がったのでまとめておく。

---

まず、先日の記事に書いていた豆腐は、ありゃー Material Design Icons を改造 Migu に組み込んでいないからであった。無知蒙昧。
あと Weather Icons もいらねーだろと思ってたしてなかったが、この際なので打ち込んじまえ！と意気込み処置を行うた。

- [Add fonts. · krymtkts/fontmerger@81681e6](https://github.com/krymtkts/fontmerger/commit/81681e6de10149ed34dda60e9b6b806374efa472)
- [Update font settings. · krymtkts/fontmerger@44c72fc](https://github.com/krymtkts/fontmerger/commit/44c72fc3cc6cbabb44d25c3268d4191f81c78fed)

Weather Icons は、なんか知らんがサイズを調整してパッチすると結構縦長になってしまってた。
が、ちょっと前に足しといた縦比/横比だけ調整するパラメータがいい感じに使え、我ながら先見の明を感じた(何

- [Split scale option to x and y scale. Add force_narrow option that for… · krymtkts/fontmerger@7653e06](https://github.com/krymtkts/fontmerger/commit/7653e06d1f106b8dbcc01f30dc9ea25c175a3916)

こうしてまたさらにイイカンジの Migu になったところで、Terminal-Icons の`glyphs.ps1`を上書きするモンをこしらえて完成とした。
ブツは Gist に上げた → [my Terminal-Icons glyphs.](https://gist.github.com/krymtkts/4457a23124b2db860a6b32eba6490b03)

Material Design Icons はコードポイントがわかりやすくずれるだけなので機械的にずらすだけで OK だった。
Weather Icons と Octicons あたりは Nerd Fonts ではよくわからん順番に組み替えてるようだった。
コード読むのめんどかったので、モウ泥臭くヒューマンマニピュレーションにて処置...気が遠くなるかと思いきや割とすぐできた感じではある。
あと`glyphs.ps1`を直接上書きするパワースタイルなので、なんか後から差し込めるようにしたい気はする。

![きれいなアイコンたち](/img/2021-07-11-terminal/icons.png)

---

全く関係ないが最近マイ Iris がやたらとチャタリングするようになてムカつくぜぇぇぇ...
ホコリが接点に侵入してるんやと思うんやけど、掃除しても掃除しても数日で再発する。
でもファミコンのカートリッジスタイルでフーッ！！すると割と改善する...そんな日々。
