---
title: "MavenAutoCompletion v0.2"
tags: ["powershell", "powershellgallery", "maven"]
---

およそ 1 年の時を経て、MavenAutoCompletion を更新した。とても小さな更新だ。

[PowerShell Gallery | MavenAutoCompletion 0.2](https://www.powershellgallery.com/packages/MavenAutoCompletion/0.2)

この 3 連休はひとりで暇なので、しょうもない更新をするのも億劫でない。

[前回](/posts/2019-04-02-pubslish-first-module-to-powershell-gallery.html)やった PSGallery への公開でのミスは、スクリプトを作成していたおかげもあり、1 年ぶりでもミスらなかった。

ただ...色々イケてないところも見つかっている。

- このモジュール、公開するものをサブディレクトリへコピペして公開しているのだけど、これってもともとそのディレクトリで開発してたらいい話
- 補完の定義が大量なので 1 ファイルの見通しが悪くなってる
  - 更に非推奨にしたい補完候補の説明を追加できない構造
- 対応するの忘れてる非推奨タグ残したままになってる
  - `�x��: <licenseUrl> element will be deprecated, please consider switching to specifying the license in the package. Learn more: https://aka.ms/deprecateLicenseUrl.`

とりあえず Issue を作っといて、忘れていても暇なときに課題を解消していけるように準備しておくかあ 😅
