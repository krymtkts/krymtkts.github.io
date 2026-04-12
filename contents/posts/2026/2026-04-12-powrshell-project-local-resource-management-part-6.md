---
title: "PowerShell Local Resource Manager Part 6"
subtitle: pslrm-bump-action
tags: ["powershell", "github"]
---

[krymtkts/pslrm](https://github.com/krymtkts/pslrm) を使って lockfile を更新・ PR を作成する third-party action [krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) を作ってる。

Marketplace 公開目的で GitHub Actions を作るのが始めてなので、知らないことが多く中々難航してる。
でもとりあえず repository を公開できる程度には出来上がってきた。 push したら早速 self smoke test がコケたが。
このあと tag を打って release 作って Marketplace に公開し、 tag base で使えるようにするのが 1 つの目標地点。

初めて third-party action を作ったけど、自分の repository 用に書くのと随分違った。
JavaScript で書くやる気ないなと思い、 YAML ベースで [composite action](https://docs.github.com/en/actions/tutorials/create-actions/create-a-composite-action) 的にしつつ、ややこしい場所を PowerShell script に逃している。
具体的には PowerShell と Windows PowerShell の差分吸収とかを PowerShell script に逃がした。
Windows PowerShell の [PowerShellGet](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/?view=powershellget-3.x&viewFallbackFrom=powershellget-2.x) しかないところに確実に [PSResourceGet](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/?view=powershellget-3.x) を用意するとかが厄介なためだ。
[`jobs.<job_id>.steps[*].shell`](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax#jobsjob_idstepsshell) の値は、起動時点で決定されている必要がある様子。
そのため YAML 上でその 2 パターンを取り扱おうとすると冗長になる。
だから PowerShell script 内で handling した方が simple だった。

ちなみに composite action の引数で受けた値を `shell` に渡すなら問題ない。
だが third party action の場合は相対パスで composite action の YAML を探すところが信用できないみたい。
例えば current directory を third-party action を呼ぶまでに変えられてたりとかで、相対パスでは参照できない場合があるみたい？
まだこういったケースをわたし自身理解してないが、不確定要素をここで抱える意味もないなと思った。
なので気は進まなかったが PowerShell script で色々やることにした。

利用者がどういう使い方をして、 action 側は決定論的に module 解決するためにどうすべきか。
PAT を使わないと commit と pull request で後続の GitHub Actions を起動できないからどう documentation するとか。
知らない事が多いだけに学びも多いが、中々難しいな。
GitHub Copilot に色々聞きながらやってるから進められるが、これは単独趣味プロでやってたらもっと時間がかかるだろうな。

[Pester](https://pester.dev/) による unit testing と [nektos/act](https://github.com/nektos/act) で integration testing してる。
けど最終的な疎通確認は、生の GitHub repository でやらないといけないのがハードル高いな。
生の repository 相手だと CI に組み込むのも flaky だし、手動テストという形なる。
まだそのためのテスト用 repository は作れてない。 action 公開勢はみんなテスト用 repo 作ってやってるのかな？謎。
テストの度に PR が作成されたり面倒そうだが、これしか方法はないのかな。
sandbox できなものがあればいいのだけど見つけられなかったので、実 repository でやる想定。

それらが終わったら Marketplace に公開したいのだけど、公式文書には workflow が含まれたらだめとか書いてある。
[Workflow syntax for GitHub Actions - GitHub Docs](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-syntax)
何故。

でも [actions/checkout](https://github.com/actions/checkout) とか代表的な action 見る限り `.github/workflow` が含まれてるし、はて？となっている。
他にも、なんか文脈的に tag 打ちで GitHub CLI で作成する release には Marketplace への publish ができなそうに見えるとか諸々心配点がある。
よくわからんがまずは SHA base で自前テストしてみてから考えるかー。
