---
title: "2026-09 時点の AI Agents との付き合い"
tags: ["llm"]
---

今日は現時点の AI Agents との付き合い方と課金の動向をメモしておく。
今回で 3 回目のメモだ。
感想に関しては殆どが自分の手応えに基づく主観なので、客観性はない。

[ChatGPT Plus](https://chatgpt.com/plans/plus/) での [OpenAI Codex](https://openai.com/codex/) との付き合いは良好だ。
最近の利用モデル変遷は以下の通り。

- [GPT-5.6 Sol](https://developers.openai.com/api/docs/models/gpt-5.6-sol) medium effort
- [GPT-5.6 Luna](https://developers.openai.com/api/docs/models/gpt-5.6-luna) max effort fast mode
- [GPT-6 Luna](https://developers.openai.com/api/docs/models/gpt-6-luna) max effort fast mode ← 今ココ

GPT-5.6 Sol medium effort は default 設定なのもあってバランスよく使えるから気に入っていた。
でも如何せん Claude Code に比べれば OpenAI Codex は token を溶かすのが速く、 GPT-5.6 Sol だと尚更だ。
仕事では、漸く ChatGPT にも登場した Premium seat で、 GPT-5.6 Sol を使ってる(それでも Claude Code の Premium seat よりはよく溶ける)。
いま長期休みなので GPT-6 Sol はまだ使ってないが、休み明けから乗り換える予定。
ただし、個人開発用に契約してる ChatGPT Plus だと、 Sol を選びにくいのは変わらない。

GPT-5.6 Luna の大幅値下げ後、 max effort & fast mode で GPT-5.6 Sol の代わりをさせるムーブがあるようで、わたしもそれに倣ってみた。
Sol に比べて圧倒的に token 消費が緩やかで、底知れない。
Sol に比べ多少知性は劣るが、個人開発で使うなら利用量の安心感の方を優先する。
当然良くないところもあって、それは max effort まで上げると fast mode でも流石に遅い。ただこればかりは仕方ない。
[GPT-5.6 Terra](https://developers.openai.com/api/docs/models/gpt-5.6-terra) はコスパ良くないから全く使わなかった。 Terra 使うなら Sol にする。
[GPT-6 Astra](https://developers.openai.com/api/docs/models/gpt-6-astra) は low effort でも十分優秀だが、とんでもない速さで token を溶かすので、個人開発では怖くて使わない。
今利用中の GPT-6 Luna は GPT-5.6 Luna の半分の単価で、 output token が増えるケースはあるらしいが、そもそも安いので全く気にならない。

この使い方だと、個人開発では [banked reset](https://help.openai.com/en/articles/20001498-how-banked-codex-resets-work) を持て余してる。
賞味期限切れ前に、高いモデルを試すときくらいしか使ってない。

ChatGPT の subscription は頻繁に usage reset が行われてることは、多分よく知られてるだろう。
その reset がいつ行われるか・行われたかを追跡するサービスもいくつかあるみたい。
わたしは仕事では OpenAI Codex と Claude Code を併用する。
なので [QuotaResets](https://quotaresets.com/) で両方を追跡してる。

以上が同期的に使う AI Agents についての現状。次は非同期的に使う AI Agents についての現状。
仕事で組んでいた [ChatGPT の Workspace agents](https://openai.com/index/introducing-workspace-agents-in-chatgpt/) と [Claude Routines](https://code.claude.com/docs/en/routines) を使った自動修正の AI ワークフローは、結構進化した。

当初は以下の通りの単純なワークフローだった。

1. Workspace agent がエラーログを記録、 GitHub Issue 起票
2. Claude Routines が GitHub Issue に基づき PR 作成

今は triage と test reviewer を切り離した 4 段階だ。

1. Workspace agent がエラーログを記録、 GitHub Issue 起票
2. Claude Routines が GitHub Issue 内容を AWS のログ等を元に調査・分析、自動修正かヒトの確認を入れるか triage
3. 自動修正なら Claude Routines が更新された GitHub Issue に基づき PR 作成
4. Claude Routines がテスト設計・テストコードの妥当性を検証

既定で動いてる GitHub Copilot と OpenAI Codex の reviewer を入れたら 6 段階になる。

これで GitHub Issue に記載される内容の解像度も高まったし、何よりテストの内容が適切でガードも厚くなった。
複数のステップをそれぞれ 1 つの単目的に分割して、 AI Agents には 1 つずつ役割に注力させた方が良いのは、 sub-agents やヒトに仕事を頼むのと同じみたい。
ただリードタイムが長くなるのと、 token 消費が増える点は難点だけど。

あと 2 番目の AWS へのアクセス権限を持った Claude Routines は、 今のところ Workspace Agent では実現できない。
OAuth の AWS MCP Server は提供されてるけど、 refresh token が 12 時間で切れるので、非対話環境では全くといっていいほど役に立たない。
この点について Workspace Agents と Claude Routines は同じ境遇だが、 [Claude Code on the Web](https://code.claude.com/docs/en/claude-code-on-the-web) は [Environment](https://code.claude.com/docs/en/cloud-environments) が使える。

Environment は secrets を持てないのがイマイチだが、 container が起動できるし、環境変数に AWS IAM User Access key を仕込んでおくことはできる。
あとは AWS CLI を setup すれば、 AWS へアクセスできる環境が手に入る。
ただ Environment を organization 内で共有をすると Access key が暴露されるので、 machine user に閉じるか、誰にも共有しないかという手段になる。
要は Claude Code on the Web も自動化についてはイマイチ機能が追い込まれておらず、現状はセキュリティリスクを許容して妥協して使ってるということだ。
[Claude が利用する Egress IP](https://platform.claude.com/docs/en/api/ip-addresses) が公開されているので、 policy に IP address 制限をつけてリスク低減した方が良い。
また、万が一 Anthropic のインフラが侵害されたり、 prompt injection された場合に備え、認可する範囲は極小化するのがいい。
利用状況の追跡も必要だ。
この辺の備えは Access key を使う場合の定石に倣えば良い。

この Environment を使った AWS アクセスの実現は過激な方法なので、組織によっては採用できないかもな。
今だと [Self-hosted environments](https://code.claude.com/docs/en/self-hosted-environments) が public beta らしい。
そっちの方が認証情報に関しては安全に運用できるはず。
ただ起動させておく分のコストや負荷分散といった運用課題が乗ってくるだろう。

せめて [OIDC](https://openid.net/developers/how-connect-works/) が提供されてたら良いのだけど。
あと [GitHub Actions](https://github.com/features/actions) を使うというのもあるけど、いつまた Claude Code の非対話的実行が課金対象に変えられるかわからないし、採用しにくい。
Anthropic の料金設計が信用ならない・ GitHub Actions の課金がかさむという課題から、現在の妥協案に落ち着いた。
Claude Code の Premium seat 内で使いたいので、その他の選択肢は端からテーブルに乗らなかった。

最近は、この一連の AI ワークフローで、試験的に 1 日に 1 件だけ Issue を自動起票するようにした。
記録済みのエラーログのいくつかを分析、修正方針の確度が高そうな候補を起票、 triage して自動修正可能と判断されたら PR 作成まで勝手に進む。
いまのところ修正の精度も高く、テストも適切な厚みで実装されるようになった。
あとは自動で起票・修正する本数を増やしていくことになるが、現時点でもレビューにおいてヒトがボトルネックになってるので、それをどう緩和するかが次の課題やな。

続く。
