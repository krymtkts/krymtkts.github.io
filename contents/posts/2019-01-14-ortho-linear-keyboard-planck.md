---
title: "Ortho-Linear Keyboard \"Planck\""
tags:  ["mechanicalkeyboard" "planck"]
---

わたしが[Planck](https://olkb.com/planck/)を手に入れたのは割と遅めのタイミング。

2017年の春に[Massdrop](https://www.massdrop.com/)で買って、6月末に届く予定だったのだけど、手元に届いたのが秋頃だった。jackhumbertさん(OLKBの中の人)随分忙しかったようで遅れまくり。

普段の仕事では[Ergodox EZ](https://ergodox-ez.com/)を使ってるんやが、持ち運びに便利なminimalなキーボードが欲しくて買った。

以下はその作業ログである。

<hr/>

_date: 2017-10-07_

![者共](/img/2019-01-14-planck/planck-case-keyswitches.jpg)

ケース付き、キースイッチ付きでDIYキットを購入。キースイッチはCherry MX Clear。

### はじめに

#### 手順の理解

Jack Humbert氏のYoutubeを見てイメージを掴む。

[How to Actually Build a Planck (or Preonic or Atomic) from OLKB](https://www.youtube.com/watch?v=S2FApwzVxAQ)

説明動画を見てケッコー以外だったのが、key switchの端子の歪みを素手で直してた所。ピンセットとか使わないんや...

### 作業に入る

#### 検品

![パーツ欠品確認](/img/2019-01-14-planck/planck-picking.jpg)

パーツは全部揃っている。数も数えたしOK

一箇所Switch Plateに多分プレス時にズレかなんかあったであろう痕があってちょっと残念。

![プレスミス？](/img/2019-01-14-planck/planck-unfortunate.jpg)

PCBはUSBに接続して音がなればOKとのこと。ファンシーなnoiseが奏でられた

PlanckはLEDにも対応してるけど、今回はデフォ実装するためナシ。でも今後欲しい感じもする。

#### Switchはめ込み

Sitch PlateにKey Switchをはめる

48Keyで使う。ちょっと数が多いしはめ込むのは面倒だけど、黙々とやる。
KeySwitchの端子がひん曲がっているやつはピンで直しながらはめ込んでいきく。

真ん中の1 or 2 keyがえらべるところはPCBにはんだ付けするまでswitchがスライドするので注意。

![半分まではめ込んだ](/img/2019-01-14-planck/planck-half.jpg)

全部はめ込んだ痕でSwitch Plateの裏表があるっぽいことに気づく。裏側のほうがピカピカしてる。でも傷が多いので今のままで行こうと決定

#### Soldering

マスクを着用。

今回ハンダゴテを新調した。白光のいいやつで温度調節ができる。調べた感じだとkeyboardのPCBにはんだ付けする最適温度が350とのことなので、それがえらべるのが良い。

![ハンダゴテ比較](/img/2019-01-14-planck/planck-solders.jpg)

ギターの配線用に使ってた旧・ハンダゴテとは随分と違う...

黙々とはんだ付けする。格子状に並んでいるので非常に楽。いやハンダゴテが良いモノだからなのかも。

![作業中](/img/2019-01-14-planck/planck-soldering.jpg)

はんだ付け完了したのがこちら。

![はんだ付け完了](/img/2019-01-14-planck/planck-soldered.jpg)

ところどころ熱で弾けたであろうヤニが付いてるので、組込前に掃除にした。

#### keycapはめ込み

今回keycapはセットで買えたXDAのやつにした。もちろん印字なんかは不要。

作業中の写真なし。無念の撮り忘れ😭

完成！

![美しい完成品](/img/2019-01-14-planck/planck-complete.jpg)

<hr/>

...最後のfirmwareのビルドのところがログに残ってないのだけど、Ergodox EZのキーマップをビルドするDockerでできてたような気がする。firmwareも書き込み済み。

当時使ってたPCではプログラミングしなくなったのと、改めてQMK firmwareのdocument見たらbuild toolsんとこの記事が変わってるので、今度環境構築がてら再確認してfirmwareのとこだけ新しく書こうと思ってる(作業中)。

わたしのkeymapのrepo([krymtkts/qmk_firmware](https://github.com/krymtkts/qmk_firmware))は「デフォのキーマップをコピった」的なコミットを最後に止まってるので、旧PCに遺物が残されてそう😅

去年はWSLで書き込む方法が確か使えてたはずだけど今は非推奨になってて、MSYS2を使う方法が主流になった？？？謎い🤔

にしてもや、この文章を認めるためにOLKBのページを久しぶりに見たが、今のPCBはrevision.6で、hot swappableなkey switch、接続はUSB Type-Cという進化っぷりに驚きを隠せない😰わたしのはrev.5かそれ以前(忘れた)
