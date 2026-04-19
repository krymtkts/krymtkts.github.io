---
title: "PowerShell Local Resource Manager Part 7"
subtitle: pslrm-bump-action Part 2
tags: ["powershell", "github"]
---

[krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) の test repository を作った。
[pslrm-actions-sandbox](https://github.com/krymtkts/pslrm-actions-sandbox) だ。

ただ単に krymtkts/pslrm-bump-action の 変更有り無しを検査するだけなのだけど、なんとか作り上げた感じ。
めちゃくちゃしょぼいが、時間猶予的な話でこれが限界だったわ。
しかもその検査は、手動で workflow を実行する、要は [`workflow_dispatch`](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_dispatch) での手動実行だ。
ただお陰で krymtkts/pslrm-bump-action の不備は見つかったし目的は果たせているんじゃないかな。とりま bug は見つかったし。
個人的に、春周りの家族イベントを考えたら進捗が出せないのはまあ理解できるかなと。
これが続くとイライラから是正の必要があるけど。

test repository では差分がない/あるパターンの検証だけしている。
ちょっと正解がよくわからんなと思っているのが、差分ありのパターンだ。
個人的な理想で言えば、 PowerShell Gallery の更新があったら、 schedule でもいいからニアリアルタイム検証が走って欲しい。
かつ、検証で作られた pull request は時限で勝手に閉じるというのまで作れたらいい。
ただそこまで作り込むには third-party action の枠を超えてるので、いまはやってない。
schedule で動かしても殆どは空振りで終わるだろうし、あって意味ないかなという気もしつつ、どうなんかな。
現状は、 test repository で手動で workflow を実行して作成された PR は、自分が手動で閉じるのみ。

third-party action を初めて作ったが、GitHub Actions 特有の考える点があって新鮮ではある。
Pull request や branch が作られる場合の並行性制御だとか、コレまで考えたこともなかった。
今のところは、利用者自身が [`concurrency`](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency) で制御するしかないよなという理解だ。

他にも色々問題はある。
binary module だと project-local にしても DLL 読み込みで競合が発生するとか諸々。
とにかく時間が取れなくて不満があるけど、徐々にそういった根本的な課題にも対処できたら良いな。
