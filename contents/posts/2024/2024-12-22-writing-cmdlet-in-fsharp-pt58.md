---
title: "F# で Cmdlet を書いてる pt.58"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

年末に次のリリースする。そのための修正をいくつかした。
リリースするするフラグが折れないようにするためにも後何を解消しないといけないか書いておく。

1 つデカい破壊的な変更を入れた。
それは `Select-Pocof` の parameter の `position=0` を変えたことだ。
修正は軽微だがこれはデカい。 [#268](https://github.com/krymtkts/pocof/pull/268)

この変更により従来は `$datasource | pocof -Query nanigashi` としなければならなかったところを、 `$datasource | pocof nanigashi` でできるようにした。
もし pocof ユーザの方がいて pocof の `position=0` だった `InputObject` にフィルタリング対象のデータを渡している人がいたらすみません、以後 pipeline で渡してください。
破壊的変更なのでテストに影響あるかなーと思ったが影響なかったので問題ない認識。

この変更は、絞りたいキーワードが分かりきっててパパッと確認したいときなんかはやはり type 数が少ない方が良いよなという判断だ。
今のところただの query であれば問題ないのだけど、 property query だと指定した property が認識されてなくて絞り込めない bug がある。

```powershell
# NG
# 多分 property の Map 構築が間に合ってないか構築後の再描画に問題がある
ll | pocof ':Name .github'

# 単純な query はOK
ll | pocof '.github'
# -NonInteractive は OK
ll | pocof '.github' -NonInteractive
ll | pocof ':Name .github' -NonInteractive
```

非同期で処理してるためなので、過去にも似たような bug を直したがまだ直さないといけない箇所があるんやろなというお気持ち。
早く直してリリースしたい。
調査が億劫なので、他の repo の開発をしてたりして着手してない。

また別の話ではあるが、喜ばしことに人生で初めて自分の repo にプルリクをいただいた。 [#290](https://github.com/krymtkts/pocof/pull/270) 嬉しい限りである。
ｳｪｰｲって投げつける側は割と誰でもなれると思うが、もらう側になったのは初めてだったので良い経験になった。

GitHub の README 仕様の知らないところを教えていただいたところもあってこれまたありがたい。
みんな `README.md` から repo 内の別の文書へのリンクは相対パスで書くんやなと知った。

また external contributor から PR もらったときの GitHub Actions の挙動のチェック甘かったところもあぶり出され、ほんとありがたい限り。
エラーを起こしてしまった申し訳ない感じではあるが。
この機会に Codecov は token なしでも coverage を upload できる機能があるのを知った。
そして Snyk は token なしだと単にエラーになる。

- [Uploading without a token - Codecov Tokens](https://docs.codecov.com/docs/codecov-tokens#uploading-without-a-token)
- [snyk/actions: A set of GitHub actions for checking your projects for vulnerabilities](https://github.com/snyk/actions)
  - > Note that GitHub Actions will not pass on secrets set in the repository to forks being used in pull requests, and so the Snyk actions that require the token will fail to run.

今年も残すところわずかなので、件の課題に年末中に着手し、解決したい、というかしなければならない。
来週には今年の振り返りをしたいし。

続く。
