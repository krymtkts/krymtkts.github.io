---
title: "coverlet.console を coverlet.MTP に置き換える"
tags: ["dotnet", "mtp"]
---

[krymtkts/SnippetPredictor](https://github.com/krymtkts/SnippetPredictor) と [krymtkts/PSGameOfLife](https://github.com/krymtkts/PSGameOfLife) では [coverlet.console](https://www.nuget.org/packages/coverlet.console) を使ってきた。
これは [krymtkts/pocof](https://github.com/krymtkts/pocof) とは違う方法で coverlet を使いたいこともあってだった。
あと project に reference を足さなくていいあたりも楽かなと思ってた。
ただ MTPv2 以降、公式的にも .NET project では [coverlet.MTP](https://www.nuget.org/packages/coverlet.MTP) を使えよ？という流れみたいなので、今回直した。

[coverlet/Documentation/Coverlet.Architecture.md at cb4e8961b8a903322b4d5354a81b3da7ea8c8743 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/blob/cb4e8961b8a903322b4d5354a81b3da7ea8c8743/Documentation/Coverlet.Architecture.md#responsibilities-and-constraints-by-package)

coverlet.console はこの通り。

> Standalone orchestration around an external target command/process; instrumentation and report output

coverlet.MTP はこの通り。

> Extends MTP runner, instruments assemblies, exchanges state via environment/process lifecycle, generates reports

[coverlet/Documentation/DriversFeatures.md at cb4e8961b8a903322b4d5354a81b3da7ea8c8743 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/blob/cb4e8961b8a903322b4d5354a81b3da7ea8c8743/Documentation/DriversFeatures.md)

> > [!TIP]
> > The new coverlet.MTP extension should be used for new test projects. This package supports the modern **Microsoft Test Platform** (see [Microsoft.Testing.Platform and VSTest comparison](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-vs-vstest))

この説明を見ても、 .NET project には coverlet.MTP を使うのが良く読める。
逆に、どんなときに coverlet.console を使うんだろという感じがするが、少なくとも .NET project じゃない場合に使うっぽい。
外部起動とのことなので別のテストランナーで起動するようなケースなのかな。
また coverage engine はどれも同じとのことなので、先述の通りどうやってテスト実行と統合できるかが選択の分かれ目なのだと思われる。
よって F# project で MTPv2 なのであれば console.MTP を選ぶべきだということだ。

置き換えによって変わるのは、呼び出し方法が変わるだけ。
以下は PSGameOfLife の書き換え diff 。

```diff
 Task UnitTest {
-    dotnet test --verbosity detailed --hangdump --hangdump-timeout 5s --hangdump-type full --report-gh
+    Remove-Item ./TestResults/* -Recurse -Force -ErrorAction SilentlyContinue
+    dotnet test --project "./src/${ModuleName}.Test/${ModuleName}.Test.fsproj" `
+        --verbosity detailed `
+        --results-directory './TestResults' `
+        --report-gh `
+        --coverlet `
+        --coverlet-include "[${ModuleName}*]*" `
+        --coverlet-output-format cobertura `
+        --coverlet-exclude-by-attribute 'CompilerGeneratedAttribute' `
+        --hangdump `
+        --hangdump-timeout 5s `
+        --hangdump-type full
     if (-not $?) {
         throw 'dotnet test failed.'
     }
-}

-Task Coverage -Depends UnitTest {
-    $testDll = "./src/${ModuleName}.Test/bin/Debug/*/${ModuleName}.Test.dll"
-    if (-not (Test-Path $testDll)) {
-        Write-Warning "Test dll not found. $testDll"
-        return
+    $coverageFiles = @(Get-ChildItem ./TestResults -Filter 'coverage.cobertura.*.xml' -File)
+    if ($coverageFiles.Count -ne 1) {
+        throw "Expected exactly one coverage file, but found $($coverageFiles.Count)."
     }
-    $target = $testDll | Resolve-Path -Relative
-    dotnet coverlet $target --target 'dotnet' --targetargs 'test --no-build' --format cobertura --output ./coverage.cobertura.xml --include "[${ModuleName}*]*" --exclude-by-attribute 'CompilerGeneratedAttribute'

+    Move-Item $coverageFiles[0].FullName ./coverage.cobertura.xml -Force
+}
+
+Task Coverage -Depends UnitTest {
     Remove-Item ./coverage/* -Force -ErrorAction SilentlyContinue
     dotnet reportgenerator
 }
```

微妙に細かいところは違ってるけど、基本は coverlet.console では tool 経由で DLL に対して test を実行してる。
これが coverlet.MTP だと test 実行に組み込まれてるから、呼び出しが素直なわけか。

これまでは unit testing と coverage で 2 回手スト実行して無駄だった(やめればいいけどすぐ終わるし放置してた)ので、今回是正できてよかった。
