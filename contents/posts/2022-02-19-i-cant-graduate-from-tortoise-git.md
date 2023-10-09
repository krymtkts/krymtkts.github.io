---
title: "Tortoise Git から卒業できない"
tags: ["git","tortoisegit","powershell"]
---

今どきの Git の使い手は、 「CLI で使ってない奴はニワカ」みたいな硬派な人とか、[GitKraken](https://www.gitkraken.com/) とかのイケてる Git クライアントや VS Code で [GitLens](https://marketplace.visualstudio.com/items?itemName=eamodio.gitlens) やら [Git Graph](https://marketplace.visualstudio.com/items?itemName=mhutchie.git-graph) 使ってる勢なんじゃないだろうか。

わたしは長らく [TortoiseGit](https://tortoisegit.org/) から卒業できないでいる。

コミットやログ、ブランチやリセットやマージ等諸々の基本的な操作は全部 CLI でやるが、ある操作だけは TortoiseGit でやるのが楽過ぎて手放せないでいる。それは歴史の改竄だ。

わたしも VS Code ユーザなので、勿論 GitLens やら Git Graph を使ったことはある。
だが、殊この歴史の改竄については Tortoise Git を超えてない(機能を知らんだけかも知らんが)。
[jesseduffield/lazygit](https://github.com/jesseduffield/lazygit) だけはわたしの要求を満たせそうな素晴らしいツールに感じたのだが、 [Windows Terminal](https://github.com/microsoft/terminal) で利用すると`┐`とかの描画幅がワイド判定されて画面がクチャクチャになってしまう(わたしが pwsher でなくコマンドプロンプターならバッチリはまったであろうツールだ)。

具体的に言うと、 [Cherry picking](https://tortoisegit.org/docs/tortoisegit/tgit-dug-cherrypick.html) がめちゃくちゃ便利でずっと使ってるのだけど、みんな歴史を改竄しないのだろうか。
わたしが歴史を改竄するのは、まだ一度もリモートに push していないローカルで育てたブランチを、デビュー前に清書するためだ。ローカルで思いのままに吐き散らしたコミット粒度及びログを、push 前に整えるのはプログラマの嗜みだ。

使用例は以下の通り。

1. 思いのままにコミットを積み上げる
2. リセット前にタグを打つ
3. push 用のブランチをベースブランチにリセットする
4. 思いのままに積み上げたコミットを丁寧に cherry pick し、公開するに適切なコミット粒度・コミットログへ書き換える
5. タグを打った元の状態と差分がないことを確認した後でめでたく push

重要なのは 3、ここでコミットの順番を入れ替えたりまとめたり分割したりという操作をするが、Tortoise Git 以外ではこういうことができないように見える。流行り？の GitKraken でさえこの有様 → [Add support for splitting an existing commit - GitKraken](https://feedback.gitkraken.com/suggestions/191932/add-support-for-splitting-an-existing-commit)
え、分割できへんの！？的な。

最近見た限りだと、GitLens や Git Graph では reset したコミット以降の変更を 1 コミットにまとめるとかはできるっぽかった。でもそういうのがしたいわけじゃない。もっと派手にやりたいんじゃ！

1 で思いのままにコミットを積み上げる等言語道断という意見もあろうが、都度〃やった内容をペコペココミットするリズム感がほしいので結局こうやってしまう。みんないちいちコミットの粒度を頭に入れた上で、アソコを直してココを直して...とかやってるのだろうか。多分やってないでしょう。

より良い歴史改竄体験を求めて別のツールを探してみたいが、ググった感じだと派手な歴史改竄機能がみられず、あんまりみんなやらないっぽいのではと思っている。みんな素直に `git rebase --interactive`(Interactive Rebasing) してるんだろうきっと。

あーあとひとつ重要な機能を忘れていた。 [dahlbyk/posh-git](https://github.com/dahlbyk/posh-git) を使っていると [`tgit`](https://github.com/dahlbyk/posh-git/wiki/Posh--Git-Module-Functions) という素敵な関数が提供されることでより一層 Tortoise Git から離れにくくなる。 `tgit` は Tortoise Git の任意の機能を召喚する魔法の関数なのだ。
この珍妙な関数を使うせいで、ペアプロ時に「あ！コマンド間違ってますよ！」と言われたこともあるが、便利なんだから仕方がない。勿論 Tab 補完もついている。

もうここまで沼に飲まれていると使い続ければ良いのでは...という気もしないではないが、より良いツールが出てきたらぜひ乗り換えたい。あるいは全部 CLI に寄せるか。
未来の自分に託した。
