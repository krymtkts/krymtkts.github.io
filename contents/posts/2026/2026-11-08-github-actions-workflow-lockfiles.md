---
title: "GitHub Actions の lockfile を導入して躓いた"
tags: ["github", "github-actions"]
---

[GitHub Actions の workflow lockfile が GA になった](https://github.com/orgs/community/discussions/194494)ので、自分の repository に導入してみた。
個人開発では問題なく入れられたのだけど、仕事の private repository にも入れようとしたら一部の workflow で支障があった。
2026-11-08 JST 時点でまだ解消できてないので、記録しておく。

GitHub Actions の lockfile は、 tag や branch で書いた action の参照を、 commit hash に固定するみたい。
直接使う action だけでなく、その action が使う action も対象になる。
workflow の `uses` を長い SHA で埋めなくても、 `.github/workflows/actions.lock` に実行対象を固定できるのが利点ぽい。
仕組みは [GitHub の docs](https://docs.github.com/en/actions/concepts/security/workflow-dependency-lockfiles) にまとまっている。

例えば [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) では [#617](https://github.com/krymtkts/blog-fable/pull/617) で導入した。
利用するには GitHub CLI の [gh-actions-lock](https://github.com/github/gh-actions-lock) extension を導入する必要がある。

```powershell
gh extension install github/gh-actions-lock
gh actions-lock --migrate-local-actions
```

blog-fable は build の共通処理を local composite action にしている。
`--migrate-local-actions` は、その参照を `./` から、新しく導入された `$/` に書き換える。
導入後の PR 用 workflow は以下のようになった。 job の部分だけ抜粋。

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v7.0.1
      - name: Build pages
        uses: $/.github/actions/build
```

`./` は runner の workspace に checkout されたファイルを使うが、 `$/` は workflow と同じ commit の action を参照する。
そのため local composite action の中で使っている [`actions/setup-dotnet`](https://github.com/actions/setup-dotnet) や [`actions/setup-node`](https://github.com/actions/setup-node) まで辿って lock できる。
この違いは[文書](https://docs.github.com/en/actions/how-tos/secure-your-work/lock-workflow-dependencies)でも説明されている。
transitive dependencies の固定がわかりやすくなったってことでいいのかな。
commit hash に対する優位性は、後述する検査機構にある認識。

`gh actions-lock --verify` を利用することで、 lockfile の整合性を確認できる。
lockfile の内容と、 workflow の参照が一致しなければ、 action をこかすことができる。

ただ、仕事の private repository でも同じように導入したら、一部の workflow が失敗するようになった。

- GitHub Copilot の setup workflow (`copilot-pull-request-reviewer`)
- GitHub Advanced Security の Code scanning AI findings (`github-advanced-security`)

local composite action を `$/` で参照すると、以下のエラーになる。

```plaintext
Error: Unable to resolve self-reference '$/'. This can occur when the server does not support this syntax, the feature flag is disabled, or the workflow context (repository/SHA) is unavailable.
```

通常の workflow では使えるが、これらの実行経路では `$/` を解決できていないようだった。
どうも GitHub の AI 系サービス側で動く内部の workflow が GA した GitHub Actions lockfile に追随してないんじゃないかな、と考えている。

因みに、同じようなエラーは他の OSS でも出ているみたい。

[Fix Copilot code review failing on the setup steps · Pull Request #55597 · home-assistant/frontend](https://github.com/home-assistant/frontend/pull/55597)

こちらでも通常の workflow は動くが、 Copilot code review の setup では `$/` が使えないと報告されている。
workaround は local composite action の steps を setup workflow に inline で展開することしか今のところなさそう。

この件、単に `./` へ戻せないのが厄介だった。
local action が別の action を使う場合、 lockfile の生成時に依存を辿れずエラーになる。
docs には `--ignore-local-actions` で `./` を残し、依存だけ別途記録する手順もある。
でも `--ignore-local-actions` を付けるだけでは local action 内の依存が lockfile に記録されない。
なので、 GitHub Actions workflow 実行時の検証で失敗してしまう。

[Locking action dependencies in a repository's workflows - GitHub Docs](https://docs.github.com/en/actions/how-tos/secure-your-work/lock-workflow-dependencies#keeping-a-local-path-action)

今のところ把握している workaround は、 `$/` を使わず local action の steps を inline で展開する方法だけ。
共通化したものをまたコピーして持つことになるので、まあかなり気に入らない workaround やなと。

既知の問題なのか、対応予定があるのかを知りたくて、 2026-11-05 JST に discussion にも[質問した](https://github.com/orgs/community/discussions/194494?sort=new#discussioncomment-19409537)のだけど進展なし。
質問してる場所が悪いのか、はたまた GA したのに誰も使ってない機能なのか、謎は深まるばかり。
とはいえ、通常の workflow には入れられたし、機能自体は気に入ってるのでこのまま使い続けるだろう。
願わくば、問題が早く解消されること、若しくはもし自分の使い方が間違っているなら正しい方法を知りたい、て感じかな。

久し振りに GA とは何なのかを考えさせられるガッカリ感だった。
続く。
