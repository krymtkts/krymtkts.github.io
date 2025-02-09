---
title: "GitHub 通知設定の Tips"
tags: ["github"]
---

わざわざ書き残すようなものでもないかと思ったが、全然知らなかったので残しておく。

GitHub の repo の Watch とか、 Issue や Pull Request の subscription の設定。
アレをいちいちいつどこで何の理由で設定したとか覚えてなくて、たまに何ぞ？と思うような通知が来ることがある。

一括管理できないのかなと ChatGPT に聞いたところ、なんと https://github.com/watching と https://github.com/notifications/subscriptions で見れるらしい。
初めて知った...
これどこから辿れるんやと思って Notifications 画面を見てみたら、左下に確かにあった...

[Configuring notifications - GitHub Docs](https://docs.github.com/en/account-and-profile/managing-subscriptions-and-notifications-on-github/setting-up-notifications/configuring-notifications)

公式文書にも思いっきり書いてあった...

[How can I see all the issues I'm watching on Github? - Stack Overflow](https://stackoverflow.com/questions/25438721/how-can-i-see-all-the-issues-im-watching-on-github)

みんな結構知らないんやな。
とても便利な機能でありがたい。

手動で Issue とか Pull Request の subscription をしたやつを一覧するには、 Reason を Manual で絞れば良いっぽい。
https://github.com/notifications/subscriptions?reason=manual

watch している repo 一覧は [Github CLI](https://cli.github.com/) でも取れるっぽい。
[PowerShellForGitHub](https://github.com/microsoft/PowerShellForGitHub) にはなさそう？

```powershell
gh api user/subscriptions --paginate --jq '.[].full_name'
# fsprojects/fantomas
# fscheck/FsCheck
# fsprojects/FSharpLint
# PowerShell/platyPS
# PowerShell/PowerShell-RFC
# ...
```

最近自分で使ってる F# とか PowerShell の repo の activity が全部飛ぶように watch 設定してるから、今何が watch になってるか一覧できるのはとても助かる。

Issue や Pull Request のうち手動で subscribe したものは CLI でどうすれば一覧できるかわからなかった。
subscribe してる Issue や Pull Request は、大体何らかの bug を踏んだり気になる機能追加を追いたい場合に使ってるので、こっちも一覧したかったが...
一覧したところで何に使うねんというのはあるけどな。対応が終わった Issue や Pull Request から通知が飛ぶことはないから棚卸しして購読を外すなんてこともやらなくていいし。
まあでも振り返ってみるときに一覧したいかも...
