---
title: "F# でコマンドレットを書いてる pt.24"
tags: ["fsharp", "powershell"]
---

[pocof](https://github.com/krymtkts/pocof) の開発をした。

描画部分で [`PSHostRawUserInterface`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.host.pshostrawuserinterface?view=powershellsdk-7.3.0) をそのまま取り回してたところに一層別の型を噛まして、ちょっとだけ unit test を書けるようにした。 [#73](https://github.com/krymtkts/pocof/pull/73)

`PSHostRawUserInterface` の mock をそのまま書こうものなら大量のメソッドを実装することになりそうだし、一層噛まして楽にできたと思う。
まだ触り程度の test しか書いてないけど、あるとないでは天地の差よ。

この対応の中でおもしろかったのが、 platform 違いを体感できたこと。
キー入力周りの .NET の cross platform の挙動の違いだったりバグについては .NET ブログかなんかで見たことがあったのだけど、今回初めてほんまに違うんやなと実感した。

`PSHostRawUserInterface` を wrap した型の mock を unit test で書いたのだけど、そこでたまたま既存コードのコピペで `Console.TreatControlCAsInput` を使ったところがあった。
これ、 Windows でのみアクセス時に `System.IO.IOException : The handle is invalid.` が発生する。 Linux や Mac ではこの不正な handler は発生せずテストが成功していた。 [Refactor PocofScreen. · krymtkts/pocof@3de1579](https://github.com/krymtkts/pocof/actions/runs/6838909170)

以下こけてた Windows のログを抜粋。 [test (windows-latest) · krymtkts/pocof@3de1579](https://github.com/krymtkts/pocof/actions/runs/6838909170/job/18596749738#step:5:66)

```plaintext
[xUnit.net 00:00:01.01]     PocofUI+Buff writeScreen.should render top down. [FAIL]
  Error Message:
   System.IO.IOException : The handle is invalid.
  Stack Trace:
     at System.ConsolePal.get_TreatControlCAsInput()
   at PocofUI.MockRawUI..ctor() in D:\a\pocof\pocof\src\pocof.Test\PocofUI.fs:line 19
   at PocofUI.Buff writeScreen.should render bottom up.() in D:\a\pocof\pocof\src\pocof.Test\PocofUI.fs:line 70
   at System.RuntimeMethodHandle.InvokeMethod(Object target, Void** arguments, Signature sig, Boolean isConstructor)
   at System.Reflection.MethodInvoker.Invoke(Object obj, IntPtr* args, BindingFlags invokeAttr)
```

CLI のハンドラのなんかが起こってるのは間違いなそうやけど何も解決策分からず。 unit test なので実際に CLI の in/out を司る者がいないからこうなるんやろけど。
mock やし単に true が欲しかっただけの箇所なので、 `Console.TreatControlCAsInput` を利用しないようにさえすれば起こらないのでそう直した。

---

あと別件で、 .NET 8 がリリースされ PowerShell 7.4 が出たことで [`Microsoft.PowerShell.SDK` 7.4.0](https://www.nuget.org/packages/Microsoft.PowerShell.SDK/7.4.0) が来てた。
`dependabot.yml` の設定がまずかったからその変更で PR を作られてしまった。
.NET 7 のままだからか `Microsoft.PowerShell.SDK` を利用してる箇所のテストコードが null reference でクラッシュするという事態に。
[Bump the test-lib group with 2 updates · krymtkts/pocof@acf0c31](https://github.com/krymtkts/pocof/actions/runs/6896506479/job/18808902758)
すまん dependabot-san 。

こういうインパクトの大きい更新は自分でやるから、 `Microsoft.PowerShell.SDK` と `PowerShellStandard.Library` の patch version しか受けないよう変更した。
.NET と PowerShell のバージョンが相互に関係し合うのは知ってるけど、他に利用してる NuGet module がどう影響受けるか把握してないので、ちょくちょく着手していきたい。
ゆーても Chocolatey には 7.4 の更新きてるのを確認して自機の PowerShell 7.4 に上げたばっかりなので、ちょっと後で。

(余談やけど [`PowerShellStandard.Library` 7.0.0-preview.1](https://www.nuget.org/packages/PowerShellStandard.Library/7.0.0-preview.1) が unlisted なの初めて知った。
[Microsoft.PowerShell.Standard.Module.Template](https://github.com/PowerShell/PowerShellStandard/blob/59998dced0948864a33fe6aed5f0a07bd12a91a6/src/dotnetTemplate/Microsoft.PowerShell.Standard.Module.Template/Microsoft.PowerShell.Standard.Module.Template/Microsoft.PowerShell.Standard.Module.Template.csproj#L9) からは参照されたままなのだけど数年動き無いしどうなるんやろ。)

[pocof/.github/dependabot.yml](https://github.com/krymtkts/pocof/blob/7dd35cdadeed14444fa85608eba256c6be24d82a/.github/dependabot.yml) から NuGet module のところだけ抜粋。

```yml
- package-ecosystem: "nuget"
  directory: "/"
  schedule:
    interval: "weekly"
    day: "friday"
    time: "06:00"
    timezone: "Asia/Tokyo"
  groups:
    pwsh-std:
      patterns:
        - "PowerShellStandard.Library"
      update-types:
        - "patch"
    pwsh-sdk:
      patterns:
        - "Microsoft.PowerShell.SDK"
      update-types:
        - "patch"
    test-lib:
      patterns:
        - "*"
      exclude-patterns:
        - "PowerShellStandard.Library"
        - "Microsoft.PowerShell.SDK"
  ignore:
    - dependency-name: "PowerShellStandard.Library"
      update-types:
        - "version-update:semver-major"
        - "version-update:semver-minor"
    - dependency-name: "Microsoft.PowerShell.SDK"
      update-types:
        - "version-update:semver-major"
        - "version-update:semver-minor"
  assignees:
    - "krymtkts"
  reviewers:
    - "krymtkts"
```

このように特定のモジュールの major minor update を除外したいとなると、[`groups`](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file#groups) の指定だけではできない。
明示的に [`ignore`](https://docs.github.com/en/code-security/dependabot/dependabot-version-updates/configuration-options-for-the-dependabot.yml-file#ignore) に該当モジュールを入れないと個別に PR 送る対象になってしまうので、なんか煩雑な感じの記述になった。

dependabot-san の設定変更できつかったのが、 main branch に `dependabot.yml` が反映されないと内容の検査ができないところ。
まだ linter とかの検証方法無いよな？ そういう Issue も開いたままだし。

[Make it possible to validate Dependabot config before it lands on `main` · Issue #4605 · dependabot/dependabot-core](https://github.com/dependabot/dependabot-core/issues/4605)

何度もやり直すのはまあ良しとしても、きついのは PR 作りまくったり Git の履歴が汚れるところ。
なんかいい方法ないんかいな。
