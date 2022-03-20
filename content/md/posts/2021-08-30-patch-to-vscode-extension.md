{:title "VS Code の拡張機能に ローカル patch する"
:layout :post
:tags ["vscode", "evernote"]}

VS Code の拡張機能にバグがある場合、メンテナが Marketplace に修正版を公開するのを待つ以外にも、自力で修正する方法がある。
ちょうどよく使っている[michalyao/evermonkey](https://github.com/michalyao/evermonkey)が、壊れてしまってから 3 週間程経っても一向に対応されないので、その方法で一時的に回避した。

Issue は以下の通り。

- [Evernote Error: 11 - Illegal to contain comments in ENML · Issue #161 · michalyao/evermonkey](https://github.com/michalyao/evermonkey/issues/161)。

エラーの原因は[ENML](https://dev.evernote.com/doc/articles/enml.php)が HTML コメントを許容しなくなったことで、 これにより upload できなくなってしまった。コメントを許容しないって記述は一見なさそうだが、Upload 時のエラーになってる。

この[ENML](https://dev.evernote.com/doc/articles/enml.php)の変更に対する Pull Request は[ある](https://github.com/michalyao/evermonkey/pull/162)のだが、どうでもいいところでコンフリクトしていたり、メンテナが忙しいのかチェックされていない状態にある。
すぐに修正を適用するには、この PR の patch を自分の VS Code に適用すればいい。

VS Code の拡張機能は `~/.vscode/extensions` にある[^1]。今回の対象となるファイルは`converterplus.js`。
PowerShell には`patch`コマンドが無いので今回は手で patch した。
元は TypeScript で記述されているが、[変更内容](https://github.com/cancastilho/evermonkey/commit/70991c155f08101d14a4ab4c64ad36d66f9850a3?branch=70991c155f08101d14a4ab4c64ad36d66f9850a3&diff=split)は素の JavaScript と同じなのでそのままコピペできる。

```powershell
code $(Get-ChildItem ~\.vscode\extensions\michalyao.evermonkey-2.4.5\out\src\converterplus.js).FullName
```

↑ クソどうでもいいコードスニペット。
でも知らなかった点として、`~`は PowerShell がユーザーディレクトリに評価するのでそのままだと VS Code に渡せなかった、というのがわかった記念に。

変更が終わったら、 VS Code で`Developer: Reload Windows`すれば拡張機能に施した変更が VS Code に取り込まれる。
因みにここでは`Developer: Restart Extension Host`で良いはずだが、割と Extension Host を起動できない Notification が表示される。
それが面倒なので`Developer: Reload Windows`で丸ごと再起動している。

---

これで無事に Evernote を利用できる状況まで戻った。でもこのままメンテナが音信不通だとこの拡張機能を VS Code で使い続けることも難しくなりそう。こりゃ参ったね。

[^1]: [Install extension - vscode-docs](https://vscode-docs.readthedocs.io/en/stable/extensions/install-extension/)
