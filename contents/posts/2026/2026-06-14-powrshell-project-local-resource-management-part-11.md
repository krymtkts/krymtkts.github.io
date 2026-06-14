---
title: "PowerShell Local Resource Manager Part 11"
subtitle: pslrm-bump-action Part 5
tags: ["powershell", "github"]
---

[krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) の文書を更新した。

最近 GitHub の bot が作成した PR に対して承認制で後続の GitHub Actions workflow が起動できるようになった。

[Bot-created pull requests can run workflows if approved - GitHub Changelog](https://github.blog/changelog/2026-06-11-bot-created-pull-requests-can-run-workflows-if-approved/)

これは結構大きな前進なんじゃないかな。
これまでは、 pslrm-bump-action のように GitHub Actions bot に作成させた PR は workflow を起動できなかった。
例えば [PAT](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens) を使わないと、作成された PR を close -> open して [pull_request](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#pull_request) event の workflow を起動するしかなかった。
これがユーザの承認を経て起動できるようになっていた。
[Agentic Workflows](https://github.github.com/gh-aw/) のように AI がもりもり PR を作る時代に合わせた変化だろう。
Agentic Workflows 側でも `copilot-requests: write` が導入され、従来の PAT を使った `GH_AW_CI_TRIGGER_TOKEN` を不要になったみたい。
`COPILOT_GITHUB_TOKEN` に PAT を渡さなくて良くなったし、いい流れだ。

- [Authentication | GitHub Agentic Workflows](https://github.github.com/gh-aw/reference/auth/#copilot-requests-write-permission)
- [Agentic workflows no longer need a personal access token - GitHub Changelog](https://github.blog/changelog/2026-06-11-agentic-workflows-no-longer-need-a-personal-access-token/)

今回の変更を [krymtkts/pslrm-actions-sandbox](https://github.com/krymtkts/pslrm-actions-sandbox) で動作確認したところ、 [Environment](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/manage-environments) で approval を必要としたときの挙動と同じになってた。
つまり、完全に自動で後続の workflow を起動したい場合は、依然 PAT が必要ということになる。
しかし PAT の長寿命 credential/管理対象 secret の追加/権限スコープの過剰付与、といったリスクを鑑みると、手動承認在りきで動かす方が管理対象を減らせて良い。
よって、 pslrm-bump-action での推奨は workflow の `github.token` を渡して手動承認する方法にした。
完全自動の workflow が必要な場合のみリスクを許容して PAT利用という流れを想定している。
repo の数が多いと、 pslrm-bump-action が作成する PR をいちいち承認しないと起動しないのは面倒かも知れない。
ただその場合でも、 PR を CI の成功で自動 merge する設定ではない限りは必ずヒトの判断が入るはずだ。
なのでそこに承認をする step を組み込む形が妥当かな。
ヒトが判断を下す前に CI が終わっているべきだという考えもあるけど、特に public repo では自動実行するより承認制の方が余計な心配事を減らせて良い。

pslrm-bump-action 外部の変更だけど、推奨の利用方法が変わるので、ひとまず文書だけの更新をして changelog にも記載している。
他にも問答無用に [PSResourceGet](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/?view=powershellget-3.x) を install していたのを修正し、加えて install させないための switch も作った [#7](https://github.com/krymtkts/pslrm-bump-action/pull/7) 。
次 version で release の予定。
