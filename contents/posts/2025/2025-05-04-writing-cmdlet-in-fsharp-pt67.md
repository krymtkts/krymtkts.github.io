---
title: "F# で Cmdlet を書いてる pt.67"
subtitle: "Completed migration from FsUnit.xUnit to Expecto"
tags: ["fsharp", "powershell", "dotnet", "fsunit", "xunit", "expecto"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[FsUnit.xUnit v6.x](https://www.nuget.org/packages/FsUnit.Xunit/) を [Expecto](https://github.com/haf/expecto) へ置き換えた。 [#343](https://github.com/krymtkts/pocof/pull/343)

多くは単純な書き換え作業だったし、書き換え方を決めたりサンプル実装を示せば、あとは GitHub Copilot Agent にほぼ作業を任せられたのは楽だったと言えるか。
ただしうまく働かせるにはそれなりの工夫が必要だった。

Copilot Agent にとって初めてのファイルので作業は、プロンプト等の参考になる情報を渡してもうまく作れなかった。
代替手段として、同じファイル内にサンプル実装を足してやれば、自分で修正できるみたい。
知名度的に学習ソースにないからか、 [Expecto](https://github.com/haf/expecto) で利用可能な assertion が何か知らなくてその辺もサポートしてやる必要があった。

あと 1000 行超えるような長いファイルになると途端に処理が遅くなってダメだった。
CPU が天井に張り付いてたし、開発 PC の laptop が 2017 モノでしょぼいからだと思われる。
Agent 自体は local の性能そんなに必要としないと思ってたが違うようだ。
でも `module` や `type` で小分けに作業を頼むと、遅いがなんとか少しずつ進められるようだった。 80 点くらいのコードは出せる感じなところも優秀な新人ぽい感触。
なので頼んでは別のことをして、回答が揃ったら手直ししてやるみたいな進め方が一番良い。

ただこの方法でも、スコープ内のテストケースが多いとテストケースを削った結果を出力してきて困った。
テストケースの数が減ってるから全ケースを書き直して出力して、と指示しても頑なに守れなかったので手で直して。この場合は回避手段あるのか怪しい。 Agent でも読み解けるような量のコードにする必要があるのかもな。

単純な書き換えのあとは、 xUnit と FsCheck の統合 [`PropertyAttribute`](https://fscheck.github.io/FsCheck/RunningTests.html#Using-FsCheck-Xunit) を Expecto 形式に書き換えた。
FsUnit.xUnit 用の PropertyAttribute を使ってるのもあって、ちょっと書き換え面倒なのではと思ったが、 config に書き換えるだけだったのでさっくりいった。
以下のように書き換えた。

```fsharp
    // FsUnit
    [<Property(Arbitrary = [| typeof<UnknownAction> |])>]
    let ``should return unknown action error.`` (data: string) =
        data
        |> Action.fromString
        |> shouldEqual (Error $"Unknown Action '{data}'.")
        |> Prop.collect data

    // Expecto
    [<Tests>]
    let tests_unknownAction =
        let configUnknown =
            { FsCheckConfig.defaultConfig with
                arbitrary = [ typeof<UnknownAction> ] }

        testList
            "Action.fromString (unknown)"
            [

              testPropertyWithConfig configUnknown "When unknown action, should return error"
              <| fun (data: string) ->
                  data
                  |> Action.fromString
                  |> Expect.equal "should return unknown action error" (Error $"Unknown Action '{data}'.")
                  |> Prop.collect data

              ]
```

コード量増えとるがな！というのは置いといて、依存関係の競合を解消するためには乗り換えるのが最善手だったので良いのだ。

これで終わりだと思っていたが、 [BenchmarkDotnet](https://github.com/dotnet/BenchmarkDotNet) を使っている benchmark の project で `dotnet build` 時にエラーが出るようになった。

```plaintext
Files in libraries or multiple-file applications must begin with a namespace or module declaration, e.g. 'namespace SomeNamespace.SubNamespace' or 'module SomeNamespace.SomeModule'. Only the last source file of an application may omit such a declaration.
```

どういうわけか BenchmarkDotnet を呼び出すための [`EntryPoint`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/functions/entry-point#explicit-entry-point) 付き `main` 関数が競合しているという。
始めよくわからなかったが、コード上には競合がないし、自動生成される entry point なんやろなということろでピンときた。

- benchmark の project は test 用の UI の mock を project reference で借りてきてる
- test project は Expecto を利用するようになったので [YoloDev.Expecto.TestSdk](https://github.com/YoloDev/YoloDev.Expecto.TestSdk) を使っている
- YoloDev.Expecto.TestSdk は [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli) に対応しており実行ファイルが生成される

つまりこいつが entry point を生成してるみたい。
今考えたらそらそうかという感じなのだけど。

以下のように project の asset から除外するものを指定したらエラーを解消できた。 [#344](https://github.com/krymtkts/pocof/pull/344)

```xml
    <ProjectReference Include="..\pocof.Test\pocof.Test.fsproj" >
      <ExcludeAssets>build; analyzers; buildtransitive</ExcludeAssets>
    </ProjectReference>
```

結構作業は多かったが乗り換え完了。念願果たせた。
前回も触れたが testing framework の乗り換えってあまり人生でも起こらない(面倒でやらない)から、いい経験になった。
