---
title: "PowerShell Local Resource Manager Part 13"
subtitle: pslrm-bump-action Part 7
tags: ["powershell", "github"]
---

[krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) の開発をした。

[v0.0.2](https://github.com/krymtkts/pslrm-bump-action/releases/tag/v0.0.2) 以降 signed commit の PR を作ってくれるし便利やなと～と思って使っていたのだけど、気になる点が出てきた。
[Dependabot version updates](https://docs.github.com/en/code-security/concepts/supply-chain-security/dependabot-version-updates) と比較すると、 PR に [label](https://docs.github.com/en/issues/using-labels-and-milestones-to-track-work/managing-labels) をつけないから素っ気ない感じがする。

そこで、 GitHub CLI で簡単に label の作成ができるようだし、 [#11](https://github.com/krymtkts/pslrm-bump-action/pull/11) で機能追加してみた。
[`gh label`](https://cli.github.com/manual/gh_label) の [`list`](https://cli.github.com/manual/gh_label_list) や [`create`](https://cli.github.com/manual/gh_label_create) を使っている。
pslrm-bump-action では、仮に label の作成に失敗しても PR 作成を阻害しないようにした。
label は付加的なもので、最悪なくてもいいから。

label の色は悩ましかったが Dependabot がつける `dependency` label はそのまま真似て、 `pslrm` label は微妙に色が違う PowerShell ぽい青にした。
pslrm-bump-action 自体の GitHub Marketplace での branding は purple なので合わせたい気もしたが、やめた。
purple だとどうしても個人的に F# 寄りに見える。純 PowerShell なのでなんか違うなという感じ。
どうでもいいが、あの branding の色の選び方融通がきかなくてなんかキライ。
branding で blue を選べば良かったのかも知れないが、なんか芸がないねんよな。

なにはともあれ、今回の label 付与を追加した版は v0.0.3 としてリリースした。
他の更新は特になく、小さなリリース。

[Release v0.0.3 · krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action/releases/tag/v0.0.3)

それでも pslrm-bump-action で作成される PR が小綺麗になって、満足している。
これでまた使いやすい Action になったんじゃないかな。
