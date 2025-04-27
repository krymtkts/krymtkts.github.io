---
title: "F# で Cmdlet を書いてる pt.66"
subtitle: "from FsUnit.xUnit to Expecto"
tags: ["fsharp", "powershell", "dotnet", "fsunit", "xunit", "expecto"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。
[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) の開発で得た知識を展開している。

[FSharpLint](https://github.com/fsprojects/FSharpLint) に代わる [FSharp.Analyzer.SDK](https://github.com/ionide/FSharp.Analyzers.SDK/) [Ionide.Analyzers](https://github.com/ionide/ionide-analyzers/) の導入は簡単に済んだ。
[#340](https://github.com/krymtkts/pocof/pull/340)
良い。
[bool Partial active Pattern](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/active-patterns#return-type-for-partial-active-patterns) のような F# 9 で増えた feature はチェックできてないみたい？なのでそこは追って調べていくつもり。

次に念願であり一番の大物である [FsUnit.xUnit v6.x](https://www.nuget.org/packages/FsUnit.Xunit/) の [Expecto](https://github.com/haf/expecto) へ置き換えを始めた。
これはちょっと時間がかかりそうだ。
[前にも触れた](/posts/2025-01-05-writing-cmdlet-in-fsharp-pt60.html)が、 FsUnit.xUnit v7 は xUnit v3 に依存してるが [FsCheck](https://github.com/fscheck/FsCheck) は現状 v2 に依存している。
これによって依存関係が塩漬けされているので、この際乗り換えようという算段だ。

まずひとつ学んだのは、 FsUnit.xUnit(というか xUnit v2) と Expecto は共存できるということ。
ただし Expecto は [YoloDev.Expecto.TestSdk](https://github.com/YoloDev/YoloDev.Expecto.TestSdk) を導入して [Microsoft Testing Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro?tabs=dotnetcli) で動かす前提だ。 Expecto のテスト実行を以下のように `EntryPoint` に含めてもだめだった。
ちゃんと調べてないが xUnit v2 のテスト検出の仕組みでは `EntryPoint` はただの compile を通すだけのハリボテて対象に含まれないのだと思われる。

```fsharp
module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsInAssemblyWithCLIArgs [] argv // こうしても Expecto のテストが検出されない
```

先述の通り YoloDev.Expecto.TestSdk を使い以下の構成してみたらうまく両方のテストが実行されて素晴らしかった。
testing framework の共存・乗り換えってあまり人生でも起こらないし、いい経験になった。

```xml
  <!-- 略 -->
  <ItemGroup>
    <!-- ここから-->
    <PackageReference Include="Expecto" Version="10.2.3" />
    <PackageReference Include="YoloDev.Expecto.TestSdk" Version="0.15.3" />
    <!-- ここまでが増えた-->
    <!-- NOTE: FsCheck.Xunit does not support xUnit.v3 yet. -->
    <PackageReference Include="FsCheck.Xunit" Version="3.2.0" />
    <PackageReference Include="FsUnit.xUnit" Version="6.0.1" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <!-- NOTE: Replace xunit with xunit.v3 from FsUnit.xUnit 7. -->
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <!-- 略 -->
  </ItemGroup>
  <!-- 略 -->
```

この辺なんでうまくいったのか理解できるような Microsoft Testing Platform の解像度を持ち得ていないので、そこを掘り下げるのは今後の宿題としたい。

これで FsUnit.xUnit 形式のテストを Expecto で書き換え始めたわけで、 test case の書き換えを始めた。
[#343](https://github.com/krymtkts/pocof/pull/343)

変更に関しては、これまで以下の前者の形式だったやつを、後者の通りにするだけ。

```fsharp
    // FsUnit.xUnit
    module Option =
        // 略

        [<Fact>]
        let ``shouldn't call Dispose if Some.`` () =
        // 略

    // Expecto
    [<Tests>]
    let tests_Option =
        testList
            "Option"
            // このカッコの書き方は行を余計に確保するが、この方がテストケースを足すとか先頭末尾のカッコの重なりを回避できてよろしい
            [

              test "When Some." {
                  // 略
                  mock.disposed |> Expect.isTrue "should call Dispose"
              }

              ]
```

だがご覧の通り、 FsUnit.xUnit では module name と let で記述していたテストの内容を Expecto では `name` なり `message` に分割して記述する必要がある。
これまでデタラメ English で書いてた test case の説明を校正するような感じ。

人力でやるの辛いなと思い GitHub Copilot に託してみた。
最初 GitHub Copilot は Chat も Agent mode でもイマイチだったが、一度お手本を書いてやると良い感じに動き出した。恐ろしい学習力やな。
Copilot Agent はこの説明を校正するのを初手ではうまくやれなくて重複した文章を取り除いたりできなかったが、 session 中で回を重ねるとどんどん良くなる。恐ろしい子。

`[<Fact>]` → `[<Test>]` の書き換えは簡単にできるとわかったから、ざーっと全体を直してしまえそう。
その後は FsUnit.xUnit の FsCheck 統合 `[<Property>]` を Expecto 形式に直すイメージで進める。
