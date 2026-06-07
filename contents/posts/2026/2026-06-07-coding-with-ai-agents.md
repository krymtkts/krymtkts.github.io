---
title: "2026-06 時点の AI Agents との付き合い"
tags: ["llm"]
---

今週はあんま個人開発してないので、今日は現時点の AI Agents との付き合い方と課金の動向をメモしておく。
感想に関しては殆どが自分の手応えに基づく主観なので、客観性はない。

感覚的に 2025 年に AI Agents を利用した開発が仕事で実践投入できる様になったと感じ、自分の仕事では 2025 年後半辺りから利用の機会を増やしていった。
使いはじめは [Visual Studio Code](https://code.visualstudio.com/) で [GitHub Copilot](https://github.com/features/copilot) を利用していて、その後 2026 年までには GitHub Copilot CLI へ移行した。
その段階で Visual Studio Code でもう GitHub Copilot の autocomplete や NES は積極的には使わなくなった。
むしろ editor を使うときは自ら手を下して書く時なので suggest がでると邪魔的なことまである。

その頃から巷ではモデル・ツールの両面で Claude Code が持て囃されていたけど、 repository や CI 等諸々の統合の面から GitHub Copilot が好みだった。
AI CLI では Claude はツールとしては先駆者なのは間違いない。最近でも [Agent Teams](https://code.claude.com/docs/en/agent-teams) や [Dynamic workflows](https://claude.com/blog/introducing-dynamic-workflows-in-claude-code) のような尖った機能が出されてくるので流石だなと思ってる(何様)。
ただモデルやツールはどこも追いつけ追い越せで伸びていくので、多少の時間差はあれど大した問題じゃないかなという感じ。
GitHub Copilot code review は設定が楽で PR を使う開発フローへも組み込みやすい。
多少バカでも最近は [review depth を指定できるようになった](https://github.blog/changelog/2026-06-02-shape-copilot-code-review-around-your-team/)し良くなるかも知れない点も期待できる。
今では OpenAI Codex と Claude Code のどちらも GitHub App で楽に使うことはできるけどね。

Web で Claude Code 利用者の「GitHub Copilot はすごく使いにくい」という意見を目にしたことがある。
後追いのツールだししょぼい面はあるけど、個人的には先述の統合面と multi vendor の model を使える点が GitHub Copilot を選ぶ理由になっていた。
multi vendor の model で review することが、現時点では人間を介さずに model の出力の偏りを補正する唯一の方法じゃないかなと思ってる。
実際 GitHub Copilot はその利点を活かした [rubber duck agent](https://docs.github.com/en/copilot/concepts/agents/copilot-cli/rubber-duck) が搭載されたので、その点ニーズがあったのは確かだ。

個人的に Claude の徳倫理っぽい人格者的な振る舞いが気に入らなくて、積極的に使わなかった。
Claude と議論してるとその性格的特性が議論の方向性を侵食してるなと感じる時があり、 Claude と議論するのは好きじゃない。
どうもこの倫理おじさんぽさは意図的な [character training](https://www.anthropic.com/news/claude-character) によって付与されたものなので、確率的に custom instructions でどうこうするのは難しい。
ただし coding 性能はいいので、具体的な指示が明確化されているのには向いてるという実感。それでも指示を無視することがあるところ好かんけど。
個人的に GPT 系は比較的性格的な味付けがマイルドな感じで、議論の方向性が侵されることもなく、かつ指示を無視する傾向も Claude よりマシだと感じている。
よって答えが決まってない議論を掘り下げるのは今でも Claude には一切使ってない。専ら GPT 系を使う。

更にツールとしての Claude Code も微妙にムカつくところがあって、メインとしては使ってない。
例えば spinner の文字列がコロコロ変わり認知負荷になって気が散るので、普段使うときは何も表示しないようにしてる。
こういう細々とした Anthropic が「面白そう」「イケてそう」と考え意図的に追加してるであろう機能がいちいち気に入らなくてストレスの積み重ねになる。
仕事では使わない選択肢がなくて使うので、他の AI Agents で決めた計画に沿って実行させるとか、決まり切ったバグ修正とかを任せてる。

という感じで 2026 年前半までは GitHub Copilot を 3 ~ 5 並列で動かしてドバドバ仕事を回させてたのだけど、 2026-06-01 からの料金改定が発表されて一変してしまった。

[GitHub Copilot is moving to usage-based billing - The GitHub Blog](https://github.blog/news-insights/company-news/github-copilot-is-moving-to-usage-based-billing/)

2026 年前半までが AI Agent 黎明期で安く済む世界だった。 2026 年後半からは AI Agent の利用は金を払って生産性を買うものに変わったということかな。
元々 2025 年時点でも [Cursor も従量課金を強化する料金改定](https://cursor.com/blog/june-2025-pricing)してる。
2026 年には [OpenAI が Codex で従量課金のみの seat を使えるようにした](https://openai.com/index/codex-flexible-pricing-for-teams/)りの流れがあった。
agentic な利用による token 消費の増大が AI 業界全体的な課題だったという認識。
そして GitHub Copilot は元々先述の通り補完メインの料金設定だった。
そこから急激に AI Agents の用途が広まって「一部のユーザ」によるものすごい token 消費が料金設計を破壊していたのに対しての改定だという認識。
その「一部のユーザ」が自分のように AI Agents をドバドバ使う層であることは、自前の script で試算して判明していた。
さらに先月公開された[請求プレビュー機能](https://copilot-billing-preview.github.com/)でわたしももう安く使えないなというのが確定した感じ。

![7~8 倍になる見積もり](/img/2026-06-07-preview/usage-based-billing.png)

162 USD が 1246 USD になるのは流石にびっくりした。
わたしは高々 7 ~ 8 倍だが [discussion](https://github.com/orgs/community/discussions/192948) を見てると数十倍になる人もいて、 agentic な用途を使ってた人たちは軒並み爆発的な値上げになるみたい。
実際のところ生半可なエンジニアを雇うよりは AI Agents にコストを積む方が安く済むが、期中に想定外のコストが積まれるのはどの会社も受け入れにくい。
いくら仕事が速く大量にこなせるようになっても会社の金がものすごく速く溶けてしまうので、一旦は GitHub Copilot の利用を停止した。実に残念だ。
現状は Copilot CLI/cloud agent の常用をやめただけで、 code review やら Agentic Workflows の利用は継続してる。
それでも当然割り当てられた無償枠に収まらず、今月はその検証期間としている。

2026-06 から仕事では OpenAI Codex を主として Claude Code/Google Antigravity を併用してる。
それまで使ってた GitHub Copilot は一旦最小限の利用に留めている。
OpenAI Codex の usage limit 超過後の課金はひとまず 50 USD にしている。
ただ 2026-05 の最終週に 1 session ガッツリ使った感じだと 50 USD は 1 週間持たなかった。
まずは token 消費を減らすために `AGENTS.md` を整理したりといった細々とした工夫を重ねている。
そのおかげか 2026-06 第 1 週ではなんとか 50 USD を溶かしきらずに済んだ。

ここまでが仕事での AI Agents との付き合いと課金の話だ。
仕事なので、金を払って爆発的な生産性を買うという方向性しかないだろうなと早々に見切りをつけ、その方向の調整をしている。
しかし個人開発でとなるとこれまた難しい。
わたしの場合は週末メインで開発するし開発が十分にできない週末も多い。
そんな中で月額課金は中々ハードルが高い。完全従量課金にすれば良いかも知れないけど、それは青天井あるいは突然使えなくなることを意味する。
これは心の問題として中々難しい。

local LLM がいい感じに使えればよいが frontier model ほど答えのない議論をできないし、当面は難しいかな。
[RTX Spark](https://www.nvidia.com/en-us/products/rtx-spark/) の登場により local LLM ブームこないかなという期待だけは持ってるが、可能性はあってもすぐに実現されるものではなかろうな。

[OpenCode](https://opencode.ai/) のように安いサブスクで回すのが良いだろうか、はたまた editor を使う予定なく AI Agents のためだけで [Cursor](https://cursor.com/) にするか。
ベンダー提供のモデルが安くなる可能性としては、 [MAI の model が発表](https://microsoft.ai/news/building-a-hillclimbing-machine-launching-seven-new-mai-models/)され GitHub Copilot が救われるかと淡い期待を抱いた。
だが、現状 [MAI-Code-1-Flash は GPT-5.4 mini レベルの料金](https://docs.github.com/en/copilot/reference/copilot-billing/models-and-pricing)なので無理そう。
それに [MAI-Thinking-1](https://microsoft.ai/news/introducing-mai-thinking-1/) も、少なくとも Microsoft の公称評価では Sonnet 4.6 を上回る場面もあるらしいが、自分の用途で満足できるかは使ってみるまでわからない。

既存の AI Agents 無償版の締め付けも厳しくなってきている。
どうも 2026-06-01 から OpenAI Codex の Free は weekly じゃなく monthly usage limit になったみたい。
公式情報が見つけられないので、徐々に rollout されてるのかな。
利用できる token が爆発的に減ったのでもう開発には殆ど使えない。
Antigravity CLI に関しても登場時から 1 日もたない感じだったし。
もう個人開発であっても AI はタダで使えるものじゃなくなったと考え、相応の資金を投入しなければならない時代なんやろな。

続く。
