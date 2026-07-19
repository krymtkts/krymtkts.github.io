---
title: "2026-07 時点の AI Agents との付き合い"
tags: ["llm"]
---

今週は個人開発してないので、今日は現時点の AI Agents との付き合い方と課金の動向をメモしておく。
2026-06 以来なので 2 回めのメモだ。
感想に関しては殆どが自分の手応えに基づく主観なので、客観性はない。

[GitHub Copilot](https://github.com/features/copilot) は使用量に対する料金的にお付き合いできなくなったので距離を置かせてもらっている話は以前触れた。
その後個人開発では数週間無償版の [OpenAI Codex](https://openai.com/codex/) や [Google Antigravity](https://antigravity.google/) を使ってた。
ただ結局チャットでの議論とエージェントの組み合わせで使えるものとして最終的に OpenAI Codex を選択した。
Google Antigravity でも [Gemini 3.5 Flash](https://ai.google.dev/gemini-api/docs/models/gemini-3.5-flash) は多少性格がチャラいけどいい感じに使えていたが、無償枠では usage limit 枯渇が早すぎる。
契約は [ChatGPT Plus](https://chatgpt.com/plans/plus/) を利用している。日本円にして月 3,000 円。
正直安くないという実感だ。高いが週末に個人開発するにはちょうどよいくらいの usage limit のため、現状は料金の割に使えないということはない。
Claude Code は前の日記で触れたように、性格・音楽性の不一致のため使いたくないので、個人利用では OpenAI のサービスに落ち着いた。

あと、いくら usage limit があるといっても、 [`AGENTS.md`](https://agents.md/) が軽量になるように整備している。
必要なコンテキストは [Skills](https://agentskills.io/home) に書いて極力余計な token が消費されないようにしている。
それでも GPT 系の token の溶け方は尋常でない印象だ。週末に使うくらいでこんなに解けるかね。

ここまでで散々 Claude との音楽性の違いに触れているが、仕事では OpenAI Codex/Claude Code の二刀流で進めている。
OpenAI のエンプラの契約プランには現状他社の Premium seat に相当するものがないため、ゴリゴリ使うと超過課金の従量でドバドバ credit が消費される。これはかなり精神を蝕む。
なので探索的作業以外のコーディングの主力は [Claude Team](https://support.claude.com/en/articles/9266767-what-is-the-team-plan) の Premium Seat に流すことで token を従量課金を逃れている。

ここ 1 ヶ月ほど、仕事では [ChatGPT の Workspace agents](https://openai.com/index/introducing-workspace-agents-in-chatgpt/) を起点として GitHub Issue/PR を作る AI ワークフローを構築して運用している。
Workspace agent が毎時起動、 Slack に飛んできたサービスのエラー情報からコードをに基づいて原因と修正案を示す。
内容を Agent の memory に記憶し、ヒトとエラーの対応方法を議論し、最終的にヒトが agent との議論を経て agent  に起票を指示する。ここまでが ChatGPT Workflow agents の仕事だ。
GitHub Issue が起票されてからは、 Claude Code の Routine で PR 作成・レビューコメントに対応する。
これで基本的に Issue の内容で PR を作成してくれるが、ちょくちょく独断やレビューコメントの誘導で Issue の指摘外の仕事もしてしまうのが難点。
レビューと最終的な merge 判断はヒトに頼りっきりだが、 GitHub Copilot code review と OpenAI Codex でも PR のレビューを補助している。
問題はというと Workspace agent の課金が始まったはずだがまだ課金されていない点、起票・レビューと merge 判断でヒトに依存している点。

以上の AI ワークフローで、難点はあれどヒト以上に働いてくれるので、ここ 1 ヶ月ほどで尋常でない量の PR を作成してくれた。
多分課金額が明らかになっても相当高い学じゃないと利用を辞めることはないだろう。
ヒトのジュニアエンジニアより安く、それよりも良い働きをするからだ。

現時点でもボトルネックがヒトである点をあぶり出して問題はあるけど、今後第三者的な AI のレビュワが Issue や PR に意見できるようにすれば、さらに良くなりそうだなと考えている。
それでも最終的には自動起票・自動 PR したいが、まだ少し距離感は感じる。

続く。
