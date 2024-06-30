---
title: "F# でコマンドレットを書いてる pt.43"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[前回の対応](/posts/2024-06-09-writing-cmdlet-in-fsharp-pt42.html) で非同期で読み込みながら即座に対話式 CLI を起動するようにはなったが、バグ・未対応や TODO が散見されるのでボチボチその対処をしていた。
Prerelease したいなといっていたが、自分で日常的に使うまでもなくなんかイマイチなところがあったので延期して、ちまちまと直している。
やることが多いので pull request も小分けに切らず、 このへん [#191](https://github.com/krymtkts/pocof/pull/191) [#192](https://github.com/krymtkts/pocof/pull/192) [#195](https://github.com/krymtkts/pocof/pull/195) で大雑把に対応をしている。
[`ProcessRecord`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.processrecord?view=powershellsdk-7.4.0) 中に操作がなくても描画をリフレッシュする実装も雑だが仕込んだ。
もうちょいで Prerelease 出せそうな気がする。

Prerelease 出すにあたり最後に解消しておきたいのが、 `-NonInteractive` 時に結構な確率で [Pester](https://github.com/pester/Pester) のテスト ≒ end-to-end テストがコケるところだ。いわゆる flaky test 化してしまった。
pocof はクロスプラットフォームを謳っておるのもあり、テストランナーが Mac, Linux, Windows だが、コケるのは大体 Mac か Windows だ。なんでだよ。
interactive mode を模倣した unit test はコケないので、非同期の対応で `-NonInteractive` によるバグを生んでしまってるのはほぼ間違いないやろなとみている。
ただ調べるのめんどくさい上位に君臨する非同期系バグの可能性もあり、対処を後回しにしてきた。
だがついに対峙せなばならんときが来たのだろう。

他はコツコツ課題を解消できてきた。
[#195](https://github.com/krymtkts/pocof/pull/195) で [`StopUpstreamCommandsException`](https://github.com/PowerShell/PowerShell/issues/3821) を投げるあたりのコードを mock 可能にしてテストを通せるようにしたり。
[xUnit](https://xunit.net/) でテストしてると `StopUpstreamCommandsException` を reflection で拾おうにも `null` になりエラーでテストできなかった。
他にテストする方法がないか考えてたが、結局シンプルな挿げ替え方式を採用した。
`StopUpstreamCommandsException` の代わりに [`Exception`](https://learn.microsoft.com/en-us/dotnet/api/system.exception?view=net-8.0) を継承した mock に挿げ替えてテストを通せる。
pocof は依存関係なしの縛りプレイなため DI のライブラリを持ってないので、テスト時に手動で挿げ替える。

同様の方法で [`Console.ReadKey`](https://learn.microsoft.com/en-us/dotnet/api/system.console.readkey?view=net-8.0) 等に依存していた箇所を挿げ替え方式にしたことで、
code coverage もステップレベルだと四捨五入 98 % まで引き上げた。
良い感じ。
いつか 100% 到達したいな。

余談だが、よくビジネスのシステム開発の文脈に於いては「code coverage は 8 割強に落ち着く」という人が多いけど、個人プロジェクトにおいては当てはまらない。
好きでやってるから、コストとのバランスを度外視できる。
coverage も縛りプレイの追求ポイントなので、どこに落ち着くという概念は存在せず、ただ突き詰めるのみｗ

閑話休題、テスト可能な構造にするにあたり、 Cmdlet を継承した型に abstract method を追加した。
元々使っていたテクニックだが、 default implementation を override で変えることで、テスト時に狙った挙動をさせられる。
今回対応範囲を増やしたことで、ほぼ Cmdlet の動作パターンを模倣できるようになった。
例えば以下は、 `StopUpstreamCommandsException` で Cmdlet の record processing のフローを抜けるパターンを模倣している。便利や。

```fsharp
    type SelectPocofCommandForTest() =
        inherit SelectPocofCommand()

        member val Host: PSHost = new Mock.Host()
        override __.Invoke(input: 'a seq) = input |> Seq.map string
        override __.PSHost() = __.Host

        override __.ConsoleInterface() = new MockConsoleInterface()

        override __.GetStopUpstreamCommandsExceptionType() = typeof<MockException>

        // NOTE: emulate the Cmdlet record precessing flow.
        member __.InvokeForTest2() =
            __.BeginProcessing()

            let mutable loop = true

            while loop do
                try
                    Thread.Sleep 50
                    __.ProcessRecord()
                with :? MockException as _ ->
                    loop <- false
```

いいことづくめのようだけど、これによって以前こしらえた interface が必要なくなったんじゃないかなとか、構造的に気になる点でてきたので、そこは新たな TODO として積まれた...

何にせよ、早々に Prerelease 出したい。
