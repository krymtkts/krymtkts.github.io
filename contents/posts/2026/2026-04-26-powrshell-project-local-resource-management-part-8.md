---
title: "PowerShell Local Resource Manager Part 8"
subtitle: pslrm-bump-action Part 3
tags: ["powershell", "github"]
---

[krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) の開発をした。

[krymtkts/pslrm-actions-sandbox](https://github.com/krymtkts/pslrm-actions-sandbox) でテストして出た不備を概ね修正できた。
テストを受けて krymtkts/pslrm-bump-action の [`outputs`](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idoutputs) も見直した。
従来の `changed` だけだと使い勝手も良くなかったので、以下の形に変更した。

```yaml
outputs:
  result:
    description: Final action result. One of `no_change`, `created`, `updated`, or `noop`.
    value: ${{ steps.finalize.outputs.result }}
  bump_branch_name:
    description: Bump branch name associated with the run. Empty when `result=no_change`.
    value: ${{ steps.finalize.outputs.bump_branch_name }}
  pull_request_number:
    description: Pull request number associated with the run. Empty when `result=no_change`.
    value: ${{ steps.finalize.outputs.pull_request_number }}
```

例えば、 PR を放置していると branch name が同じで lockfile の意味的な差分もない状態が在り得る。
これを簡潔に表現するのは `changed` だとできないので `result` の `noop` で表現する。
`changed` だけではテストを書こうにも状態が曖昧なので書けなかった。つまり利用者が outputs で状態を判断できないということ。
コレばっかりは krymtkts/pslrm-actions-sandbox で利用者としてテストした価値があったなといえる。

また全ての step が PowerShell/Windows PowerShell の両方でも動くように調整した。
最初 [pslrm](https://github.com/krymtkts/pslrm) の lockfile 更新部分だけで良いんじゃないかなと考えていた。
ただ self-hosted runner だと Windows PowerShell しかないケースがあるなと気付いたので対応させた。
third-party action を意識すると、どの環境で動かすのかまで考える必要があって中々難しいが、これは勉強になった。

これでようやく `v0.0.1` の tag を打って release をできるな～と考えたが、まだ足りなかった。
mutable tag `v0` を更新する workflow を作り忘れてたので、また次回にやりたいと考えている。
手動で作ってもいいが 2 回もやるのかよ的な。
今のところ release が Marketplace への公開作業でもあるので手動でやらないといけないみたいだし、手作業を減らすには tag のところしかない。
ここは妥協したくないな。

---

あと [krymtkts/pslrm](https://github.com/krymtkts/pslrm) 開発に AI Agent を使ったことの感想。
pslrm 関連の開発はほとんど GitHub Copilot に頼っている。色々わからないことが多かったし頼るのにちょうどよかった。
その感想としては、自分の趣味プロではちょっと違うなという感じがしてきてる。
仕事なら PHP と TypeScript で何並列にも GitHub Copilot CLI をブン回して不満はないのだけど、趣味プロはそうじゃない。
(とはいえ仕事でも「クソ忙しいのに暇」という謎の感情を生んでるのも確か)。
仕事の目的を果たせればいいというある種の妥協とは違って、わたしの趣味プロは作る過程も楽しんでるのがあるんだろうな。
そこを AI Agent に作業を任せることで楽しみを失ってる気がしてる。

学習の螺旋も途絶えはしないし何なら加速してるけど、プログラミングに関しては何か感触が違うと感じてる。
コードと自分の間で直に刺激を受けてた感触を失ったような気がする。細かい部分へのこだわりとか。
当然細かい書き方とか custom instructions に書いたり skills にすればいいのだけど、そういう問題じゃない。

この辺は今後の課題かな。今はなるべく自分にプログラミングを取り戻したい気がしてるがどうなんだろう。
当然今となっては自分の下手なプログラミングより AI Agent の方が物知りでコード品質も高い事が多い(指示さえ正しければ)ので利用は続けたい。
でも例えば、詳細な実装計画を Markdown にまとめて AI 自身の記憶以外にも持たせるとか工夫してるけど、中長期的な展望をなんぼ示しても道を見失うよな AI Agent さんは。
その方向修正に自分の創造の時間を消費してるとしたら、それは由々しき問題ですよ。仕事の交通整備を趣味プロでやりたいんじゃないから。
モデルの進化、時間が解決するのかな。

世の皆さんはどうしてるんだろうか。作りたいものを時短で作るのが最優先みたいなのなら多分この感覚はないのかな。
逆に作りたいものがなくて作ることを楽しんでるとしたら、伝わる可能性は微レ存か。しらんけど。
