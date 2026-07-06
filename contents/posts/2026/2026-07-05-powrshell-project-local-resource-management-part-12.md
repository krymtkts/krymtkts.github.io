---
title: "PowerShell Local Resource Manager Part 12"
subtitle: pslrm-bump-action Part 6
tags: ["powershell", "github"]
---

[krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action) の開発をした。

自分で pslrm-bump-action を利用してる中で気づいたのだけど、 signed commit にし忘れてた。
このせいで signed commit しか受け付けない krymtkts/SnippetPredictor のような repo で merge できない。
(merge してもいいけどキレイにルールを守れなくてイヤ)

よって signed commit の対応を進めることにした。
これまで全然知らなかったが、 bot による commit に committer の署名情報などが含まれなかったら自動で署名検証されるらしい。
これを使うには `git` で操作せずに GitHub API で操作すれば良いとのこと。
以下の文書でそれに触れられてるのだけどわたしはよくわからなかったので AI に仕組みを説明してもらった。

- [About commit signature verification - GitHub Docs](https://docs.github.com/en/authentication/managing-commit-signature-verification/about-commit-signature-verification#signature-verification-for-bots)
- [REST API endpoints for Git commits - GitHub Docs](https://docs.github.com/en/rest/git/commits?apiVersion=2022-11-28)

とりあえず GitHub API で以下の流れを踏めば正しく Verified な commit となることを PowerShell runner で確認した。

1. `GET /repos/{owner}/{repo}/git/commits/{parent_sha}`
   - 親 commit の `tree.sha` を取得する
2. `POST /repos/{owner}/{repo}/git/trees`
   - 親 tree を base_tree に指定する
   - 更新後 `psreq.lock.psd1` を blob として上書きした新しい tree を作る
3. `POST /repos/{owner}/{repo}/git/commits`
   - 新しい tree SHA と親 commit SHA から commit を作る
   - author / committer は指定しない
4. `POST /repos/{owner}/{repo}/git/refs`
   - bump branch がなければ `refs/heads/{branch}` を作る
5. `PATCH /repos/{owner}/{repo}/git/refs/heads/{branch}`
   - bump branch があれば、新しい commit SHA へ `force: false` で更新する

しかし Windows PowerShell でだけどうしても詰まってしまって、すごく時間を浪費してしまった。
原因は GitHub API のために `ConvertTo-Json` を使って深い JSON を処理しようとしたところだった。
PowerShell だと詰まらないが Windows PowerShell では詰まってしまう。
Windows PowerShell でありがちなことなのでもっと早く気付けても良かったけど、初めて GitHub API を捏ねくったこともありバイアスで気づくのが遅くなった。練習不足やな。

とりあえずなんとか時間をかけてでも解決できて良かった。
PR は [#9](https://github.com/krymtkts/pslrm-bump-action/pull/9) 。
この signed commit 版は 0.0.2 としてリリースした。

[Release v0.0.2 · krymtkts/pslrm-bump-action](https://github.com/krymtkts/pslrm-bump-action/releases/tag/v0.0.2)

これでより使いやすい Action になったんじゃないかな。
