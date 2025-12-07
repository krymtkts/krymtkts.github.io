---
title: NuGet の Dependabot がよくわからない
tags: [dotnet, github]
---

予定があって [krymtkts/pocof](https://github.com/krymtkts/pocof) の開発はできなかった。
なのでちまちました改善をするに留めてたのだが、 あらためて NuGet の [Dependabot version updates](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/about-dependabot-version-updates) の挙動がよくわからないなと思っている。

[以前](/posts/2025-04-06-fix-dependabot-nuget-project-not-found.html) project が見つからなくなった話をしたが、あれとはまた違う。
今回のは特定の依存関係だけ更新されずに残っているという挙動に出会った。

blog-fable の repo root で [`dotnet list package --outdated`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-list) したら以下が表示された。

```powershell
> dotnet list package --outdated
Restore complete (1.2s)

Build succeeded in 1.3s

The following sources were used:
   https://api.nuget.org/v3/index.json

Project `App` has the following updates to its packages
   [net10.0]:
   Top-level Package        Requested   Resolved   Latest
   > Fable.Browser.Dom      2.19.0      2.19.0     2.20.0

Project `test` has the following updates to its packages
   [net10.0]:
   Top-level Package           Requested   Resolved   Latest
   > Microsoft.Playwright      1.56.0      1.56.0     1.57.0

```

いつも JST 金曜 06:00 に更新してるから、 [`Microsoft.Playwright`](https://www.nuget.org/packages/Microsoft.Playwright) の方は 2025-12-04 21:29:03Z でチェックの後に更新されたからわかる。
ただ [`Fable.Browser.Dom`](https://www.nuget.org/packages/Fable.Browser.Dom#versions-body-tab) の方は 5 ヶ月も前に更新されてるから拾えないのはおかしい。
仕方ないのでどちらも手動で更新した。 [#416](https://github.com/krymtkts/blog-fable/pull/416)

---

この過程で blog の code snippet が正しく改行されなくなっているのも発見した。これは困る。
Microsoft (Chromium )Edge だと正しく改行されて Google Chrome で改行されないみたい。

1Password の Google Chrome 拡張機能が有効だと改行コードが破壊さて Syntax highlighting もされなくなるところまで突き止めた。
いまの version は 8.11.22.25 だった。

HTML 生成時点で code snippet は highlight.js の class が付与された状態に出力されているのだけど、 1Password 拡張機能付きで読み込むと動的に書き換えられてしまうみたい。
なんじゃこりゃーー。
似たような問題が報告されているのも確認できた。これは関連がありそうだ。

[[BUG] Beta and Nightly extension degrade page's original functionallity | 1Password Community](https://www.1password.community/discussions/1password/bug-beta-and-nightly-extension-degrade-pages-original-functionallity/165329)

通常はこう。

```plaintext
<code class="language-powershell"><span class="hljs-comment"># .NET 9 まで(.NET 10 でも動く)</span><br>dotnet add ./src/pocof package FSharp.Core <span class="hljs-literal">--version</span> <span class="hljs-number">10.0</span>.<span class="hljs-number">100</span><br><span class="hljs-comment"># .NET 10 から</span><br>dotnet package add FSharp.Core <span class="hljs-literal">--version</span> <span class="hljs-number">10.0</span>.<span class="hljs-number">100</span> <span class="hljs-literal">--project</span> ./src/pocof
</code>
```

でも 1Password が悪さするとこうなる。

```plaintext
<code class="language-powershell"><span class="token comment"># .NET 9 まで(.NET 10 でも動く)dotnet add ./src/pocof package FSharp.Core --version 10.0.100# .NET 10 からdotnet package add FSharp.Core --version 10.0.100 --project ./src/pocof</span>
</code>
```

ちょっと対応方法がわからないのでこれは様子見するかー。残念や。
