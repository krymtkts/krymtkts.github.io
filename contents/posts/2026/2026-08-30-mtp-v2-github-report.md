---
title: "Microsoft.Testing.Extensions.GitHubActionsReport"
tags: ["dotnet", "mtp"]
---

以下の記事を見て [GitHub Actions reports](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-test-reports#github-actions-reports) が良さそうだなと思って対応してみた。

[Test reporting in Microsoft.Testing.Platform: from red build to root cause - .NET Blog](https://devblogs.microsoft.com/dotnet/microsoft-testing-platform-reporting/#put-the-failure-where-the-author-is-already-looking)

使い方は [MTP v2](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-migration-from-v1-to-v2) の test project で `dotnet test` に `--gh-report` を渡すだけ。
これで GitHub Actions の Summary にテストサマリが表示されるようになる。

いつぞや [Microsoft.Testing.Platform (MTP) v1 to v2 出来なかった話](/posts/2026-05-10-mtp-v1-to-v2-failed.md) という日記を書いた。
その後 [YoloDev.Expecto.TestSdk](https://github.com/YoloDev/YoloDev.Expecto.TestSdk) が [0.16.0](https://github.com/YoloDev/YoloDev.Expecto.TestSdk/releases/tag/YoloDev.Expecto.TestSdk-v0.16.0) で MTP v2 対応した。
わたしの F# project はそれで MTP v2 の native runner になってたつもりだったが、見てみたら、 pocof だけ v1 のママだった。
今回 Microsoft.Testing.Extensions.GitHubActionsReport を入れる前準備として [#485](https://github.com/krymtkts/pocof/pull/485) で MTP v2 に更新した。

Microsoft.Testing.Extensions.GitHubActionsReport を使うのはとても簡単。
先述の `--report-gh` と、既定の挙動を変えたければ追加の option を渡すだけだ。
ただ pocof の場合はサポート対象の PowerShell で .NET Standard 2.0 と .NET 8.0 の Multi targeting してる。
よってテストレポートも build した target framework 毎にラベルをつけたかったのだが、既定ではできない。
仕方ないので、以下のように自前で [`GITHUB_STEP_SUMMARY`](https://docs.github.com/en/actions/reference/workflows-and-actions/workflow-commands#adding-a-job-summary) に追記することで、 target framework がわかるようにした。

```powershell
    $TargetFrameworks | ForEach-Object {
        "Run unit tests for target framework: ${_}"
        # NOTE: The test assembly always targets net9.0.
        # NOTE: So label each report with the pocof target framework selected by the project reference.
        if ($env:GITHUB_ACTIONS -eq 'true' -and $env:GITHUB_STEP_SUMMARY) {
            Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value @(
                ''
                "## pocof target framework: ${_}"
                ''
            )
        }
        dotnet test --project "./src/${ModuleName}.Test/${ModuleName}.Test.fsproj" `
            -p:TestTargetFramework=$_ `
            --results-directory "${TestResultsRootPath}" `
            --output Detailed `
            --report-gh `
            --coverlet `
            --coverlet-include "[pocof]*" `
            --coverlet-output-format cobertura `
            --hangdump `
            --hangdump-timeout 20s `
            --hangdump-type Full
        if (-not $?) {
            throw "dotnet test failed for target framework: ${_}"
        }
        Move-Item "${TestResultsRootPath}/coverage.cobertura.*.xml" "${TestResultsRootPath}/coverage.${_}.cobertura.xml" -Force
    }
```

これで、以下のようなの Markdown が `GITHUB_STEP_SUMMARY` に書き込まれる。
`os`/`pwsh` or `powershell` 毎の [matrix](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/run-job-variations#about-matrix-strategies) でコレが出る。いい感じだ。

```markdown
## pocof target framework: net8.0

## ✅ Test Run Summary — pocof.Test (net9.0)

| Total | Passed | Failed | Skipped | Duration |
|---:|---:|---:|---:|---:|
| 291 | 291 | 0 | 0 | 14.88s |

### ⏱ Slowest tests

- `SelectPocofCommand.When cancellation received, should return` — 1.92s
- `SelectPocofCommand.When Escape is Finish, should return values` — 1.92s
- `render.When handler has a quit event, should return ContinueProcessing.StopUpstreamCommands` — 1.53s
- `Periodic.When idling rendering for 1 second, should invoke idling rendering action` — 1.32s
- `Buff writeScreen.When rendering entries over y` — 1.12s
- `Buff writeScreen.When rendering entries under y` — 1.12s
- `Periodic.When idling for 1 second, should invoke idling action` — 1.00s
- `Buff writeScreen.When rendering many wide entries` — 816ms
- `Buff writeScreen.When rendering multi-byte string entries` — 790ms
- `unwrap.When DictionaryEntry wrapped` — 416ms


## pocof target framework: netstandard2.0

## ✅ Test Run Summary — pocof.Test (net9.0)

| Total | Passed | Failed | Skipped | Duration |
|---:|---:|---:|---:|---:|
| 291 | 291 | 0 | 0 | 11.95s |

### ⏱ Slowest tests

- `SelectPocofCommand.When cancellation received, should return` — 2.12s
- `SelectPocofCommand.When Escape is Finish, should return values` — 1.94s
- `Periodic.When idling rendering for 1 second, should invoke idling rendering action` — 1.31s
- `Periodic.When idling for 1 second, should invoke idling action` — 1.00s
- `render.When handler has a quit event, should return ContinueProcessing.StopUpstreamCommands` — 902ms
- `Buff writeScreen.When rendering entries under y` — 464ms
- `Buff writeScreen.When rendering entries over y` — 456ms
- `unwrap.When DictionaryEntry wrapped` — 446ms
- `unwrap.When PSObject nad DictionaryEntry wrapped` — 414ms
- `Buff writeScreen.When rendering multi-byte string entries` — 411ms
```

理想は PR にも comment 出来たらいいけどそういうのは自分で作らないとないっぽい。
いったんコレでいく。
