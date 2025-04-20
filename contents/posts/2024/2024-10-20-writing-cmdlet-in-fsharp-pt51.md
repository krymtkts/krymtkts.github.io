---
title: "F# で Cmdlet を書いてる pt.51"
subtitle: Windows PowerShell on GitHub Actions workflow
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

[以前発見した .NET の挙動が文書と違うやつ](/posts/2024-10-06-writing-cmdlet-in-fsharp-pt50.html) は .NET Framework に依存する Windows PowerShell だけで発生するエラーがあったためだが、それを自動的に検知する仕組みは作ってなかった(このときは手で見つけた)。
今後も同じような事があると面倒なので、 pull request を作成したときに自動で検知できるよう GitHub Actions workflow の matrix に Windows PowerShell を追加した。

job の step で Windows PowerShell を利用するには、[`jobs.<job_id>.steps[*].shell`](https://docs.github.com/en/actions/writing-workflows/workflow-syntax-for-github-actions#jobsjob_idstepsshell) に `powershell` と指定すればいい。
`jobs.<job_id>.steps[*].shell` を使うのは、 pocof の Github Actions workflow は処理を共通化するのに [Composite action](https://docs.github.com/en/actions/sharing-automations/creating-actions/creating-a-composite-action) を使ってるからで、 step ごとに指定する必要があるから(確か)。
当然コレを使えるのは platform が Windows の場合だけ。
job runner が Windows の場合だけ `pwsh` と `powershell` の 2 つの `shell` を Composite action に追加してやれば良い。

従来はすべての platform で `pwsh` を利用する [`matrix`](https://docs.github.com/en/actions/writing-workflows/workflow-syntax-for-github-actions#jobsjob_idstrategymatrix) になってたので、 Windows の場合だけ拡張してやる必要がある。
これには [`jobs.<job_id>.strategy.matrix.include`](https://docs.github.com/en/actions/writing-workflows/choosing-what-your-workflow-does/running-variations-of-jobs-in-a-workflow#expanding-or-adding-matrix-configurations) が使えた。
`matrix` 定義の追加や上書きができるみたい。便利すぎ。

結果この様になった。 [#246](https://github.com/krymtkts/pocof/pull/246)

composite action の [`inputs`](https://docs.github.com/en/actions/sharing-automations/creating-actions/metadata-syntax-for-github-actions#inputs) に `shell` を追加して、それで `jobs.<job_id>.steps[*].shell` を設定する。

```diff
inputs:
   codecov_token:
     description: "Codecov token"
     required: true
+  shell:
+    description: "Shell for the job. pwsh or powershell"
+    required: true
+    default: pwsh
```

```diff
       with:
         global-json-file: ./global.json
     - name: Install modules from PSGallery
-      shell: pwsh
+      shell: ${{ inputs.shell }}
       run: |
         Set-PSResourceRepository PSGallery -Trusted
         Install-PSResource Psake,Pester,PSScriptAnalyzer -Quiet -Reinstall -Scope CurrentUser
     - name: Execute All Tests
-      shell: pwsh
+      shell: ${{ inputs.shell }}
       run: Invoke-Psake -taskList TestAll
     - name: Upload coverage reports to Codecov
       uses: codecov/codecov-action@v4
```

`include` を使って Windows runner の `matrix` を拡張する。

```diff
     strategy:
       matrix:
         os:
           - windows-latest
           - ubuntu-latest
           - macos-latest
+        shell:
+          - pwsh
+        include:
+          - os: windows-latest
+            shell: pwsh
+          - os: windows-latest
+            shell: powershell
     runs-on: ${{ matrix.os }}
     steps:
       - name: Checkout
```

```diff
         uses: ./.github/actions/test
         with:
           codecov_token: ${{ secrets.CODECOV_TOKEN }}
+          shell: ${{ matrix.shell }}
       - name: Run Snyk to check for vulnerabilities
         uses: snyk/actions/dotnet@master
         # snyk/actions uses Container action that is only supported on Linux.
```

これで完成かと思いきや、 3 つ障害があった。
まず [Pester](https://github.com/pester/Pester) 用のテストスクリプトに Windows PowerShell 非互換の機能があったので取り除く必要があった。
また GitHub Actions workflow で使える Windows Server 2022 の runner には [PSResourceGet](https://github.com/PowerShell/PSResourceGet) が入ってなかったので、 PowerShell Module の準備を `pwsh` 用と `powershell` 用に分けた。
この 2 つはテストスクリプトと workflow を調整するだけで済んだ。

[`Split-Path -LeafBase`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/split-path?view=powershell-7.4#-leafbase) は PowerShell 6.0 以降の機能で Windows PowerShell で使えなかったので [`Get-ChildItem`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/get-childitem?view=powershell-7.4) で代用。

```diff
     if ($DryRun -eq $null) {
         $DryRun = $true
     }
-    $ModuleName = Resolve-Path ./src/*/*.psd1 | Split-Path -LeafBase
+    $ModuleName = Get-ChildItem ./src/*/*.psd1 | Select-Object -ExpandProperty BaseName
     $ModuleVersion = (Resolve-Path "./src/${ModuleName}/*.fsproj" | Select-Xml '//Version/text()').Node.Value
     $ModuleSrcPath = Resolve-Path "./src/${ModuleName}/"
     $ModulePublishPath = Resolve-Path "./publish/${ModuleName}/"
```

`shell` の内容に応じて [`Install-PSResource`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.psresourceget/install-psresource?view=powershellget-3.x) と [`Install-Module`](https://learn.microsoft.com/en-us/powershell/module/powershellget/install-module?view=powershellget-2.x) を使う `step` に分ける。

```diff
       uses: actions/setup-dotnet@v4
       with:
         global-json-file: ./global.json
-    - name: Install modules from PSGallery
+    - name: Install modules from PSGallery (pwsh)
       shell: ${{ inputs.shell }}
+      if: inputs.shell == 'pwsh'
       run: |
         Set-PSResourceRepository PSGallery -Trusted
         Install-PSResource Psake,Pester,PSScriptAnalyzer -Quiet -Reinstall -Scope CurrentUser
+    - name: Install modules from PSGallery (powershell)
+      shell: ${{ inputs.shell }}
+      if: inputs.shell == 'powershell'
+      run: |
+        Install-Module -Name Psake,Pester,PSScriptAnalyzer -Force -Scope CurrentUser -Repository PSGallery -SkipPublisherCheck
     - name: Execute All Tests
       shell: ${{ inputs.shell }}
       run: Invoke-Psake -taskList TestAll
```

最後の障害は解消できなかったので、暫定対応とした。
解決できなかったのは、 [`[System.Threading.Thread]::CurrentThread.CurrentCulture`](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.thread.currentculture?view=net-8.0) に設定した [`DateTimeFormat.ShortDatePattern`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.datetimeformatinfo.shortdatepattern?view=net-8.0) が pocof に伝播されない点だ。
この `DateTimeFormat.ShortDatePattern` に `'yyyy-MM-dd'` を設定したら、 [`DateTime.ToString()`](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.tostring?view=net-8.0) でそのパターンが参照されるはずだが、されなくてテストがコケる。
自 PC であれば PowerShell でも Windows PowerShell でも上手くいくが、 GitHub Actions の Windows Server 2022 runner では反映されなかった。

culture を設定した thread と違う thread で動いたのか？とか考えられるが、となると解消方法は `DateTime.ToString()` に culture を直に渡す方法になる。これはやりたいものではない。
現時点で恒久対応が難しかったので、 skip 判断で良かったと思う。
Pester で提供されてた [Conditional skip](https://pester.dev/docs/usage/skip#conditional-skip) 機能で条件付き skip を実現できた。便利すぎ。
[`$PSVersionTable.PSEdition -eq 'Desktop'`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_powershell_editions?view=powershell-7.4#long-description) で判断しているので Windows PowerShell だけこの test case が skip 対象となる。
ついでに culture の変更は [`BeforeEach`](https://pester.dev/docs/commands/BeforeEach) [`AfterEach`](https://pester.dev/docs/commands/AfterEach) を使うようにした(`*All` でいい気もするが)。

```diff
         Context 'with culture' -ForEach @(
             @{ Matcher = 'match'; Operator = 'or'; Query = '24-01' }
         ) {
+            BeforeEach {
+                $global:culture = [System.Threading.Thread]::CurrentThread.CurrentCulture
+                $global:testCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US').Clone()
+                $global:testCulture.DateTimeFormat.ShortDatePattern = 'yyyy-MM-dd'
+                [System.Threading.Thread]::CurrentThread.CurrentCulture = $global:testCulture
+            }
+            AfterEach {
+                [System.Threading.Thread]::CurrentThread.CurrentCulture = $global:culture
+            }
             It "Given '<InputObject>', it keeps order as '<Expected>'." -TestCases @(
                 @{InputObject = 1..12 | ForEach-Object {
                         Get-Date ('2024-{0:D2}-01' -f $_)
                     }; Expected = @(
                         Get-Date '2024-01-01'
                     ) ; Params = $BaseParam + $_
                 }
-            ) {
-                $culture = [System.Threading.Thread]::CurrentThread.CurrentCulture
-                $testCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US').Clone()
-                $testCulture.DateTimeFormat.ShortDatePattern = 'yyyy-MM-dd'
-                [System.Threading.Thread]::CurrentThread.CurrentCulture = $testCulture
+            ) -Skip:($PSVersionTable.PSEdition -eq 'Desktop' ) {
+                # TODO: This test is not working on Windows PowerShell on GitHub Actions. It works well locally.
+                # TODO: It seems that the culture is not changed.
                 $InputObject | Select-Pocof @Params | Should -BeExactly -ExpectedValue $Expected
-                [System.Threading.Thread]::CurrentThread.CurrentCulture = $culture
             }
         }

```

現状 skip してるが根治できたらしたいな。継続して調査する。

ひとまずこれで v0.16.0 のリリースしたあと Windows PowerShell 出だけ発生するエラーがあるとかは事前に気づける。
あとやっぱ今の PowerShell は Windows PowerShell より大分速いんやなと実感できた。 Windows PowerShell の job は他よりも完了するのがめちゃくちゃ遅い。
いい仕事をした。
