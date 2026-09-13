---
title: "F# で command-line predictor を書いてる Part 12"
subtitle: "0.8.0"
tags: ["fsharp", "powershell", "dotnet", "command-line-predictor"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の [v0.8.0](https://www.powershellgallery.com/packages/SnippetPredictor/0.8.0) をリリースした。

`:` 打鍵後の tab で snippet group が補完されるようになったので全然構わなかったが、自分のよくある誤操作を補完することを目的としたリリースだ。
`:xxx` みたいな SnippetPredictor 独自の group 識別子を打鍵したあと、 Tab を打たずにそのまま Enter を打ってしまう事がよくある。
当然実行可能な形式でなくエラーになるので、その Enter で変換候補がある場合だけ置き換える機能を opt-in で作った。 [#141](https://github.com/krymtkts/SnippetPredictor/pull/141)
opt-in なのは、多分この挙動は [PSReadLine](https://learn.microsoft.com/en-us/powershell/module/psreadline/?view=powershell-7.6) 的には不自然な振る舞いなので、利用者自身の意思で有効化すべきと考えたため。
PSReadLine の KeyHandler を直にいじることってそんなによくやることでもないとだろうし、多分 opt-in が最適。
他はテスト周りの調整だとか依存関係の更新だけ。比較的小さいリリースになった。

これで多分 SnippetPredictor に付け加えたいと思っていた補完周りの機能は完成したつもり。
普段使いを通してまた気になる点が出てくる可能性は十分にあるが、今の時点では補完周りを改修することはないかな。
強いて言えば、 SnippetPredictor の存在しない group 識別子で PSReadLine の `AcceptLine` をしたときは、 history に残さなくはしたいかな。
でもどういうフィードバックをユーザに返すかは考えてない。

SnippetPredictor で他に改善するとしたら、入力に対する検索かな。
今はただの中間一致になってるから、何らかの優先順位で表示順序を変えるってのはやって良いかも知れない。
ただ現状の実装だと list 操作を複数回やるような感じになって無駄な演算は増えるので、その辺は内部実装の改善も含めて検討したほうが良いかもな。
