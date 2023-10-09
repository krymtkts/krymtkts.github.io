---
title: "Windows Terminal で OSC133 する"
tags: ["windows-terminal","oh-my-posh","powershell"]
---

先週は盆の関係で時間が取れず書くのをサボった。まあその前の日曜に余分に書いてたし相殺したことにしておく。

きょうは Windows Terminal でもシェル統合みたいなのができるらしいと気づいたので、それを試す。

---

ちょっと古い Windows Terminal Preview のリリースノートを見てて気づいた。

[Release Windows Terminal Preview v1.15.186 · microsoft/terminal](https://github.com/microsoft/terminal/releases/tag/v1.15.1862.0)

`` Get-GitHubRelease -OwnerName microsoft -RepositoryName terminal -Tag v1.15.1862.0 | select -ExpandProperty body | % {$_ -split "`n"} | sls -Pattern '\[Experimental\]' -Context 0,10 `` で雑に抽出。

> - [Experimental] We now support scrollbar marks! (#12948) (#13163) (#13291) (#13414)
>   - Use the `addMark` action to add a scrollbar mark
>     - The `color` optional parameter can be used to specify a color
>   - Use the `scrollToMark` action with a specified `direction` parameter to scroll between the marks
>   - Use the `clearMark` action to remove a selected mark
>   - Use the `clearAllMarks` action to remove all scrollbar marks
>   - The `experimental.autoMarkPrompts` profile setting can be set to `true` to automatically mark each prompt
>     - NOTE: This uses the FTCS_PROMPT sequence from FinalTerm, `OSC 133 ; A`, which we now support! (#13163)
>   - The `experimental.showMarksOnScrollbar` profile setting can also be set to `true` to display the marks on your scrollbar

ほう？
プロンプトに自動でマーカーを付けられる様になったらしい。

この機能使いたかったが、端末乗り換えて profile にシーケンス足して...みたいなのが必要っぽかったのでメンドイなーと思ってけど、端末だけで対応できるのなら非常に楽。

これはやりたい。試してみる。

### Windows Terminal の設定

利用している Windows Terminal Preview のバージョンは Version: 1.15.2282.0 だった。

以下のような感じの設定で試す。実際の `settings.json` は他の設定でゴチャついているので、要所だけ切り抜いた。

```json
{
  "$help": "https://aka.ms/terminal-documentation",
  "$schema": "https://raw.githubusercontent.com/microsoft/terminal/main/doc/cascadia/profiles.schema.json",
  "actions": [
    {
      "command": {
        "action": "addMark",
        "color": "#CB4B16"
      },
      "keys": "ctrl+m"
    },
    {
      "command": {
        "action": "scrollToMark",
        "direction": "previous"
      },
      "keys": "ctrl+up"
    },
    {
      "command": {
        "action": "scrollToMark",
        "direction": "next"
      },
      "keys": "ctrl+down"
    },
    {
      "command": "clearAllMarks",
      "keys": "ctrl+d"
    }
  ],
  "profiles": {
    "defaults": {
      "experimental.autoMarkPrompts": true,
      "experimental.showMarksOnScrollbar": true
    },
  },
```

JSON Schema については、`https://aka.ms/terminal-profiles-schema` の方はバージョンが古いので Preview 使ってると色々不都合がある。ので repo 直で見る。

試してみて、とりあえずすべて期待通りに動くのがわかった。

はじめスクロールバーに色がつかなくて、はて？だったけど、これは `settings.json` の記述が間違ってたからだった。
`experimental.autoMarkPrompts`, `experimental.showMarksOnScrollbar` は `profiles` 直下じゃない。
`profiles.defaults` に書かないとダメ。

これに気づいたのは [`settings.json` の JSON Schema のここ](https://github.com/microsoft/terminal/blob/c12987af415c5e0911d7a0a81b8494fbe6307328/doc/cascadia/profiles.schema.json#L2177-L2181)見たら、 `Profile` 毎の property であることがわかったから。

ドキュメントにもこの機能出てた。あとから気づいたわ。
[Scroll marks (Preview)- Windows Terminal Advanced Profile Settings | Microsoft Docs](https://docs.microsoft.com/en-us/windows/terminal/customize-settings/profile-advanced#scroll-marks-preview)

ドキュメントの方は、タイトルも「Advanced profile settings in Windows Terminal」 やし、こっちならパっと見で気づけたかな...まあよし。

---

今回この件を調べてから、 [megathread: Scrollbar Marks · Issue #11000 · microsoft/terminal](https://github.com/microsoft/terminal/issues/11000) という巨大な Issue を見つけた。
きょう試した experimental な feature もここ由来のもの。

まだ VS Code のそれに比べると相当しょぼい。
またコマンドの成功・失敗に対応してマーカーの色分けがつくとかもない。
けど、この Issue 見るにそれらはそのうちやるっぽい(`category` というやつかな)。なので、 subscribe しておいて新しいのが来たら是非試したい。

普段仕事で使う標準出力が長い `cdk` とか AWS Tools for PowerShell とかの実行結果を飛び回るのに、こんなに有効な機能が来たことは喜ばしい。
来週の仕事から早速使う。

### 余談

ついでに `useAcrylic = true` じゃないとき、 `opacity` だけで古き良き透過にできるのにも気づいた。
Windows 11 で ver1.12 から使えたらしい...気づくのに時間がかかったけど、これもやりたかったことのひとつなので、良い。

[Opacity - Windows Terminal Appearance Profile Settings | Microsoft Docs](https://docs.microsoft.com/en-us/windows/terminal/customize-settings/profile-appearance#transparency)
