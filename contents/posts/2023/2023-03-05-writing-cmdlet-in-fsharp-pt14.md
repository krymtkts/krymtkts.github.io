---
title: "F#でコマンドレットを書いてる pt.14"
tags: ["fsharp","powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

以下を参考に xUnit と FsUnit を導入した。

- [dotnet テストと xUnit を使用した .NET Core での単体テスト F# - .NET | Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/core/testing/unit-testing-fsharp-with-dotnet-test)
- [FsUnit for xUnit](https://fsprojects.github.io/FsUnit/xUnit.html)
- [What is FsUnitTyped?](https://fsprojects.github.io/FsUnit/FsUnitTyped.html)
- [単体テストにコードカバレッジを使用する - .NET | Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/core/testing/unit-testing-code-coverage?tabs=windows#code-coverage-tooling)

repo ルートを作業ディレクトリとする。

```powershell
cd (mkdir pocof.Test)
dotnet new xunit -lang "F#"
dotnet add reference ../pocof/pocof.fsproj
dotnet sln ../../pocof.sln add .\pocof.Test.fsproj
cd ../../
dotnet build
```

一応ここでちゃんとテストできるか確認をする。`Tests.fs` のコードを単純なのに書き換える。

```fsharp
module Tests

open System
open Xunit

[<Fact>]
let ``My test`` () = Assert.True(true)
```

VS Code からでもよし、 CLI でなら repo のルートで以下の通り。

```powershell
PS> dotnet test
  Determining projects to restore...
  All projects are up-to-date for restore.
  pocof -> C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof\bin\Debug\net6.0\pocof.dll
C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof.Test\Tests.fs(7,39): warning FS0988: Main module of program is empty: noth
ing will happen when it is run [C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof.Test\pocof.Test.fsproj]
  pocof.Test -> C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof.Test\bin\Debug\net6.0\pocof.Test.dll
Test run for C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof.Test\bin\Debug\net6.0\pocof.Test.dll (.NETCoreApp,Version=v6.0)
Microsoft (R) Test Execution Command Line Tool Version 17.5.0 (x64)
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: < 1 ms - pocof.Test.dll (net6.0)
```

あと `dotnet new xunit -lang "F#"` で生成された `Program.fs` は不要なので削除、 `pocof.Text.fsproj` からも削除する。

次に FsUnit を追加する。

```powershell
dotnet add ./src/pocof.Test/pocof.Test.fsproj package FsUnit.xUnit
```

テストをこんな感じで書き換え、 `dotnet test` が成功したら完了。

```diff
 module Tests

-open System
 open Xunit
+open FsUnitTyped

-[<Fact>]
-let ``My test`` () = Assert.True(true)
+module ``Pocof Tests`` =
+
+    [<Fact>]
+    let ``Sample test should equals 1`` () = 1 |> shouldEqual 1
+
+[<EntryPoint>]
+let main argv = 0
```

["Main module of program is empty: nothing will happen when it is run" warning when running xunit tests on .net core. · Issue #2669 · dotnet/fsharp](https://github.com/dotnet/fsharp/issues/2669) で知ったが、 テストプロジェクトはコンソールアプリなので `<EntryPoint>` を必ず持たないと警告がでる。 xUnit 追加後シンプルにしたテストコードでは `<EntryPoint>` がなかったので警告が出てた。

ちょっとわからないのが VS Code からの実行だとハングしてるようだった。
テストを実行するための DotNet CLI が後ろで実行されるみたいだが、そのプロセスは上手く行ってそうに見える。でも GUI がずっとくるくる止まる。どうも Inonide がエラーしてるっぽい。 xUnit の実行確認のときはいけたので FsUnit 起因か？ちょっと調べる必要ありだが、今回は先送りにする。

```plaintext
2023-03-04 15:50:47.005 [error] Error:
    at MapTreeModule_find (c:\Users\takatoshi\.vscode\extensions\ionide.ionide-fsharp-7.5.1\webpack:\out\fable_modules\fable-library.4.0.0-theta-018\Map.js:245:15)
    at FSharpMap__get_Item (c:\Users\takatoshi\.vscode\extensions\ionide.ionide-fsharp-7.5.1\webpack:\out\fable_modules\fable-library.4.0.0-theta-018\Map.js:1179:12)
    at f (c:\Users\takatoshi\.vscode\extensions\ionide.ionide-fsharp-7.5.1\webpack:\out\fable_modules\fable-library.4.0.0-theta-018\Map.js:1297:12)
    at Kt (c:\Users\takatoshi\.vscode\extensions\ionide.ionide-fsharp-7.5.1\webpack:\out\fable_modules\fable-library.4.0.0-theta-018\Array.js:70:21)
    at c:\Users\takatoshi\.vscode\extensions\ionide.ionide-fsharp-7.5.1\webpack:\out\Components\TestExplorer.js:138:27
    at processTicksAndRejections (node:internal/process/task_queues:96:5)
    at async Promise.all (index 0)
```

コードカバレッジは xUnit のテンプレなら初めから統合されてるらしくて、すぐ出せる。これはめちゃくちゃ楽。

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

こんな感じでレポート出力できる。出力しておいたら VS Code の [Coverage Gutters](https://marketplace.visualstudio.com/items?itemName=ryanluker.vscode-coverage-gutters) で取り込めるしこりゃいいな。

```powershell
reportgenerator -reports:".\src\pocof.Test\TestResults\*\coverage.cobertura.xml" -targetdir:"coverage" -reporttypes:Html
```

---

昔は Visual Studio からしか Solution とか作るしかなかったようなキヲクをがある(無能だっただけやも知れんが)。
いまの DotNet CLI ある時代めちゃくちゃ便利でびっくりした。

あと FsUnit 非常によい。
入力キーから処理を導くところのリファクタするのに役立っている。
サクサク書けるし、バグってた状態で放置してるとこが見つかったり([#33](https://github.com/krymtkts/pocof/pull/33))いまのところ良いことしか無い。

いい感じにテスト揃ってきたら、 GitHub Actions で FsUnit と Pester 実行するようにしよう。
