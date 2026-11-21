---
title: "2026-11 時点の AI Agents との付き合い"
tags: ["llm"]
---

今日は現時点の AI Agents との付き合い方と課金の動向をメモしておく。
今回で 4 回目のメモだ。
感想に関しては殆どが自分の手応えに基づく主観なので、客観性はない。

今も [ChatGPT Plus](https://chatgpt.com/plans/plus/) で [OpenAI Codex](https://openai.com/codex/) を使ってる。
最近の利用モデルは [GPT-6.2 Sol](https://developers.openai.com/api/docs/models/gpt-6.2-sol) low effort に落ち着いてる。
[GPT-6 Luna](https://developers.openai.com/api/docs/models/gpt-6-luna) max effort fast mode も良かったが、 遅く感じてきたので、 [GPT-6 Sol](https://developers.openai.com/api/docs/models/gpt-6-sol) low effort に乗り換えてた。
その流れで今は最新の GPT-6.2 Sol を使ってる。

GPT-6.2 Sol low は知性と消費のバランスが良く感じてる。
[GPT-6.1 Terra](https://developers.openai.com/api/docs/models/gpt-6.1-terra) は更に安いし良さそうで試したけど、知性がイマイチでやめた。
medium effort でも意外に消費する感じで、そのくせ思慮が浅く、割りに合わなかった。
high effort あたりにしたら良いのかも知れんが消費の問題がきつくなる。
コードもあんまり満足行くキレイさじゃなかった(コピペっぽい感じに仕上がった)ので GPT-6.2 Sol に落ち着いてる。

GPT-6.2 Sol を使って毎日 Codex を利用してたら全然一週間保たないが、週末メインの開発と、頻繁な full reset のお陰でまだ使えてる。
full reset に関してはほんとに頻繁にあるから、相変わらず [banked reset](https://help.openai.com/en/articles/20001498-how-banked-codex-resets-work) を持て余しがち。
最近は仕事・個人開発の両方でもいい感じに reset を使えるようになってきたので、徐々に在庫を減らしてる感じ。

ただ 2 並列程度でも 5h usage limit が枯渇するので、週末でもフルで開発だと保たない。 1 つのテーマで家事の合間にちょこちょこ使う感じ。
自律的に AI Agents を動かすとなったら、節約のためまた luna に戻す可能性は十分ある。
この辺は model が出る度に調整続けるしかないやろな。
subscription に依存しない術としては local LLM も手段の 1 つだが、機器の価格帯を考えたらコスト削減で使える手段じゃない。

以上が同期的に使う AI Agents についての現状。
次は非同期的に使う AI Agents についての現状。

仕事で組んでいた [ChatGPT の Workspace agents](https://openai.com/index/introducing-workspace-agents-in-chatgpt/) と [Claude Routines](https://code.claude.com/docs/en/routines) を使った自動修正の AI ワークフローは、あまり進展してない。
最近は不正アクセス試行の激化への対処だとか、重大な脆弱性対応だとかで忙しいので、簡単なアプリケーション脆弱性の対処は AI に任せてる。

OpenAI の [DevDay 2026](https://openai.com/index/devday-2026-recap/) で発表された [dots](https://openai.com/index/introducing-dots/) と新しい [Environment](https://learn.chatgpt.com/docs/environments/cloud-environments) は試してる。

dots は ChatGPT Business plan なら Premium seat を割り当ててたら使える。
秘書になってもらう感じで、少しずつ情報を溜め込んで成長してもらってる感じ。
Codex の Cloud Environment も使えるってのが良いところで、自主的に alert から調査をしてくれたり便利。
当然そのための読み取り権限の範囲には気を配る必要はあるけど、積極的に使うことで調査時間が削減につながり、楽できる。

新しい Environment の登場で、従来 Claude Routine にだけ任せてた仕事も dots に頼める。
[OIDC](https://learn.chatgpt.com/docs/environments/cloud-environments#access-cloud-resources-with-oidc) が提供されてるので、 AWS resource を調査させるのに IAM User Access key 不要でできる点は優れてる。

非同期的に動かす AI Agents の仕組みとしてはループは結構出来上がってるのだが、あと課題のヒトの受け入れ工数だけは、試行錯誤の時間が取れなくて改善できてない。
単純な受け入れを AI にやらせるのは出来てきてるが、一番手間を減らしたい legacy の修正の受け入れは任せられてない。
legacy なためテストコードも足りないし実装も良くなかったり、弱い動的型付けなのもあり、決定論的に信頼できない点が多すぎる。
強い静的型付けプログラミング言語であればまだやりようがあるんやろけどなー。

続く。
