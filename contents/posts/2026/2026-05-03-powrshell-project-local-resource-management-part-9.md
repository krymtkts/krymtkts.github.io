---
title: "PowerShell Local Resource Manager Part 9"
subtitle: pslrm-bump-action Part 4
tags: ["powershell", "github"]
---

[krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) の開発をした。
ようやく GitHub Marketplace に [pslrm Bump Action](https://github.com/marketplace/actions/pslrm-bump-action) として公開した。
prerelease として 0.0.1-alpha としてる。次は 0.0.1 かな。全く semantic でないけど。

公開にあたり [krymtkts/pslrm-actions-sandbox](https://github.com/krymtkts/pslrm-actions-sandbox) で PowerShell/Windows PowerShell の動作確認するなどしてる。
利用する上で最低限問題ない機能とセキュリティ施策はできてるはず。
ただ、作成される PR で GitHub Actions を動かすには Content と Pull Request の Read/Write が必要なので、かなり使う側として不安感があるはず。
そこは、追加のセキュリティ施策で強化するのが良かろうなと考えている。

今回初めて GitHub Actions を公開したので色々勉強になった。
`action.yml` に [`branding`](https://docs.github.com/en/actions/reference/workflows-and-actions/metadata-syntax#branding) metadata を書いた方が良いとか。
一番面倒なのが、現状 GitHub Marketplace への公開は手動でしかできないぽいところ。
GUI 上では確認できる規約の承諾と、公開のためのチェックボックスに相当する部分は GitHub CLI に見当たらない。

また GitHub Actions では immutable exact version tag と mutable major version tag を運用するのが一般的と思われる。
なので、 pslrm-bump-action でもそうした。
最近はセキュリティ的な施策で hash や exact version tag を利用されることが多いけど。
ただそうなると、作成する release は手動だし tag は 2 回打たないといけないし、面倒だ。

コレだと運用が面倒なので、 pslrm-bump-action では tag と GitHub release の作成を task runner に寄せて可能な限り省力化している。
task は以下の通り。 task 自体は短くなるようにしたが helper function を含めると煩雑でそこそこある。

[pslrm-bump-action/.build.ps1 at df39f29bb0c3ee800c415067124f4d44a0a139db · krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action/blob/df39f29bb0c3ee800c415067124f4d44a0a139db/.build.ps1#L173-L214)

release note を含む exact version の signed tag を打って push して GitHub release の draft を作成する形にしてある。
mutable major version tag は exact version tag が push されたら起動して更新される。
pslrm-bump-action は [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) に則っているから release note 作成は自動化が比較的容易だ。
task runner にしておけば GitHub Actions で実行するようにしても概ねそのまま使える。
その場合の問題は signed tag があるけど、それはそうなったときに考える。

これで PowerShell の project-local な依存関係管理が整ったなと思ってたが、自分の利用ケースではまだ足りないのが判明した。
現状 [pslrm](https://github.com/krymtkts/pslrm) は分離に [runspace](https://learn.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-runspaces?view=powershell-7.6) を使うようにしてある。
片や、わたしが F# で書く PowerShell module は全てのテストをして [`Import-Module`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/import-module?view=powershell-7.6) したあと手動でテストできるようにしておくのがお作法となっている。
そのため、 pslrm で [psake](https://github.com/psake/psake) なり [Invoke-Build](https://github.com/nightroman/Invoke-Build) の task を実行しても runspace の中に import した module が置き去りとなってしまう。

解消するには、使用感は悪くなるけど各 task が直に pslrm を使うようにするか、 import は別の手段を使うか、かな。
思わぬ伏兵が現れたなと思っている。
こうなると現在の PowerShell session に依存関係をばら撒くパターンが必要になるのかもなあ。
ただ assembly の読み込み問題を考えたら process level の分離が必要だし、色々やることが拡散してきた。

更にいうと Keep a Changelog の script も毎回核のだるいので、これも module にしたいと思えてきた。
この先、そういう細々したツールを拡充していくのもアリかもな。
