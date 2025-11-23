---
title: "F# で Cmdlet を書いてる pt.82"
subtitle: "pocof 0.22.0-alpha"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。
Multi targeting と bugfix を含めたものを [0.22.0-alpha](https://www.powershellgallery.com/packages/pocof/0.22.0-alpha) としてリリースをした。
初めて Multi targeting するのもありほんとにちゃんと publish されるのかなと不安があったので、久々の [prerelease](https://learn.microsoft.com/en-us/powershell/gallery/concepts/module-prerelease-support?view=powershellget-3.x) にした。

リリースに際してこれまで import した module ベースでの publish してたのをやめた。
代わりにより再現性高い path ベースので publish に切り替えた。
また今回は prerelease として出したのだけど、リリース用の psakefile を調整して prerelease version まで検査できるように直した。
これはなかなか気に入っている。
元は pocof を始めるとき参考にした blog の情報に基づき [`Import-LocalizedData`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/import-localizeddata?view=powershell-7.5) を使ってた。
でも pocof では多言語化意味ないので単純な [`Import-PowerShellDataFile`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/import-powershelldatafile?view=powershell-7.5) に切り替えた。
これによって [`System.Version`](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-version) で取り扱えない prerelease suffix もいい感じに扱える。

今回のリリース後に Linux でのみ crash する bug [#397](https://github.com/krymtkts/pocof/issues/397) を発見した。
いつから壊れてるのかわからないが多分 [`StringBuilder`](https://learn.microsoft.com/en-us/dotnet/api/system.text.stringbuilder?view=net-8.0) を使いだしたころからかな。
これは早めに対処したいが、 platform 依存の bug は対処難しいのでどうなることやら。

因みに今回の prerelease で .NET 6.0 と .NET Standard 2.0 の binary が配信されるようになったのだけど、両者にパフォーマンス上の違いはほぼなさそうだった。
以下に未来の自分に対して target framework の変更程度で高速化するなんて儚い希望は持つなよという意味で結果を残しておく。

---

以下は benchmark の project を `net 9.0` で回した場合。

[TFM](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) が `netstandard2.0`。

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26200.7171/25H2/2025Update/HudsonValley2)
Intel Core i7-8550U CPU 1.80GHz (Max: 2.00GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100
[Host] : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3 DEBUG
DefaultJob : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3

| Method                                 |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------- | ---------: | -------: | -------: | ----: | ------: | -----: | --------: | ----------: |
| invokeAction_Noop                      |   138.4 ns |  2.68 ns |  2.38 ns |  1.00 |    0.02 | 0.0248 |     104 B |        1.00 |
| invokeAction_AddQuery                  |   862.1 ns | 17.14 ns | 25.12 ns |  6.23 |    0.21 | 0.3920 |    1640 B |       15.77 |
| invokeAction_BackwardChar              |   476.1 ns |  9.45 ns |  7.89 ns |  3.44 |    0.08 | 0.0248 |     104 B |        1.00 |
| invokeAction_BackwardWord              |   639.9 ns | 12.34 ns | 14.21 ns |  4.63 |    0.13 | 0.0477 |     200 B |        1.92 |
| invokeAction_DeleteBackwardChar        |   235.5 ns |  4.51 ns |  5.01 ns |  1.70 |    0.05 | 0.0248 |     104 B |        1.00 |
| invokeAction_DeleteBackwardWord        | 2,380.1 ns | 46.32 ns | 70.74 ns | 17.21 |    0.58 | 0.8392 |    3512 B |       33.77 |
| invokeAction_SelectBackwardChar        |   516.8 ns |  9.39 ns |  9.64 ns |  3.74 |    0.09 | 0.0401 |     168 B |        1.62 |
| invokeAction_SelectBackwardWord        |   566.6 ns | 11.23 ns | 14.99 ns |  4.10 |    0.13 | 0.0629 |     264 B |        2.54 |
| invokeAction_RotateMatcher             |   160.2 ns |  3.02 ns |  2.96 ns |  1.16 |    0.03 | 0.0496 |     208 B |        2.00 |
| invokeAction_CompleteProperty_NoSearch |   139.3 ns |  2.32 ns |  1.94 ns |  1.01 |    0.02 | 0.0248 |     104 B |        1.00 |
| invokeAction_CompleteProperty_Search   |   529.8 ns | 10.45 ns | 13.59 ns |  3.83 |    0.12 | 0.1564 |     656 B |        6.31 |
| invokeAction_CompleteProperty_Rotate   |   420.9 ns |  8.44 ns | 10.05 ns |  3.04 |    0.09 | 0.1392 |     584 B |        5.62 |

TFM が `net6.0` 。

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26200.7171/25H2/2025Update/HudsonValley2)
Intel Core i7-8550U CPU 1.80GHz (Max: 2.00GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100
[Host] : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3 DEBUG
DefaultJob : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3

| Method                                 |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------- | ---------: | -------: | -------: | ----: | ------: | -----: | --------: | ----------: |
| invokeAction_Noop                      |   136.4 ns |  2.73 ns |  3.83 ns |  1.00 |    0.04 | 0.0248 |     104 B |        1.00 |
| invokeAction_AddQuery                  |   829.1 ns | 16.56 ns | 17.72 ns |  6.08 |    0.21 | 0.3920 |    1640 B |       15.77 |
| invokeAction_BackwardChar              |   463.7 ns |  9.26 ns |  9.09 ns |  3.40 |    0.11 | 0.0248 |     104 B |        1.00 |
| invokeAction_BackwardWord              |   627.2 ns | 11.09 ns |  9.83 ns |  4.60 |    0.14 | 0.0477 |     200 B |        1.92 |
| invokeAction_DeleteBackwardChar        |   230.9 ns |  2.47 ns |  1.93 ns |  1.69 |    0.05 | 0.0248 |     104 B |        1.00 |
| invokeAction_DeleteBackwardWord        | 2,293.5 ns | 40.80 ns | 57.19 ns | 16.83 |    0.62 | 0.8392 |    3512 B |       33.77 |
| invokeAction_SelectBackwardChar        |   498.0 ns |  9.16 ns |  7.15 ns |  3.65 |    0.11 | 0.0401 |     168 B |        1.62 |
| invokeAction_SelectBackwardWord        |   569.3 ns | 11.32 ns | 12.58 ns |  4.18 |    0.14 | 0.0629 |     264 B |        2.54 |
| invokeAction_RotateMatcher             |   161.7 ns |  3.15 ns |  2.79 ns |  1.19 |    0.04 | 0.0496 |     208 B |        2.00 |
| invokeAction_CompleteProperty_NoSearch |   141.1 ns |  2.92 ns |  3.58 ns |  1.03 |    0.04 | 0.0248 |     104 B |        1.00 |
| invokeAction_CompleteProperty_Search   |   525.6 ns | 10.31 ns | 14.11 ns |  3.86 |    0.15 | 0.1564 |     656 B |        6.31 |
| invokeAction_CompleteProperty_Rotate   |   460.4 ns |  8.93 ns |  7.91 ns |  3.38 |    0.11 | 0.1392 |     584 B |        5.62 |

悪あがきで試しに TFM が `net8.0` の場合も取った。

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26200.7171/25H2/2025Update/HudsonValley2)
Intel Core i7-8550U CPU 1.80GHz (Max: 2.00GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100
[Host] : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3 DEBUG
DefaultJob : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3

| Method                                 |       Mean |    Error |   StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------- | ---------: | -------: | -------: | ----: | ------: | -----: | --------: | ----------: |
| invokeAction_Noop                      |   139.6 ns |  2.80 ns |  2.99 ns |  1.00 |    0.03 | 0.0248 |     104 B |        1.00 |
| invokeAction_AddQuery                  |   854.5 ns | 16.77 ns | 22.38 ns |  6.12 |    0.20 | 0.3920 |    1640 B |       15.77 |
| invokeAction_BackwardChar              |   473.8 ns |  9.30 ns |  8.70 ns |  3.39 |    0.09 | 0.0248 |     104 B |        1.00 |
| invokeAction_BackwardWord              |   624.0 ns | 12.34 ns | 13.21 ns |  4.47 |    0.13 | 0.0477 |     200 B |        1.92 |
| invokeAction_DeleteBackwardChar        |   237.2 ns |  4.74 ns |  6.80 ns |  1.70 |    0.06 | 0.0248 |     104 B |        1.00 |
| invokeAction_DeleteBackwardWord        | 2,400.6 ns | 45.94 ns | 54.68 ns | 17.20 |    0.52 | 0.8392 |    3512 B |       33.77 |
| invokeAction_SelectBackwardChar        |   501.0 ns |  9.87 ns | 11.74 ns |  3.59 |    0.11 | 0.0401 |     168 B |        1.62 |
| invokeAction_SelectBackwardWord        |   592.5 ns | 11.65 ns | 13.87 ns |  4.25 |    0.13 | 0.0629 |     264 B |        2.54 |
| invokeAction_RotateMatcher             |   164.6 ns |  2.76 ns |  2.83 ns |  1.18 |    0.03 | 0.0496 |     208 B |        2.00 |
| invokeAction_CompleteProperty_NoSearch |   138.5 ns |  2.70 ns |  3.00 ns |  0.99 |    0.03 | 0.0248 |     104 B |        1.00 |
| invokeAction_CompleteProperty_Search   |   525.2 ns | 10.47 ns | 13.62 ns |  3.76 |    0.12 | 0.1564 |     656 B |        6.31 |
| invokeAction_CompleteProperty_Rotate   |   425.4 ns |  8.46 ns | 12.66 ns |  3.05 |    0.11 | 0.1392 |     584 B |        5.62 |

---

以下は benchmark の project を `net10.0` で回した場合。

TFM が `netstandard2.0`。

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26200.7171/25H2/2025Update/HudsonValley2)
Intel Core i7-8550U CPU 1.80GHz (Max: 2.00GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100
[Host] : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3 DEBUG
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3

| Method                                 |        Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------- | ----------: | --------: | --------: | ----: | ------: | -----: | --------: | ----------: |
| invokeAction_Noop                      |    61.81 ns |  0.711 ns |  0.593 ns |  1.00 |    0.01 | 0.0248 |     104 B |        1.00 |
| invokeAction_AddQuery                  |   634.17 ns | 12.236 ns | 12.018 ns | 10.26 |    0.21 | 0.3920 |    1640 B |       15.77 |
| invokeAction_BackwardChar              |   181.10 ns |  2.864 ns |  2.679 ns |  2.93 |    0.05 | 0.0248 |     104 B |        1.00 |
| invokeAction_BackwardWord              |   269.64 ns |  5.362 ns |  6.585 ns |  4.36 |    0.11 | 0.0477 |     200 B |        1.92 |
| invokeAction_DeleteBackwardChar        |    94.00 ns |  1.895 ns |  1.773 ns |  1.52 |    0.03 | 0.0248 |     104 B |        1.00 |
| invokeAction_DeleteBackwardWord        | 1,820.17 ns | 35.804 ns | 42.623 ns | 29.45 |    0.73 | 0.8392 |    3512 B |       33.77 |
| invokeAction_SelectBackwardChar        |   199.02 ns |  3.845 ns |  4.577 ns |  3.22 |    0.08 | 0.0401 |     168 B |        1.62 |
| invokeAction_SelectBackwardWord        |   272.08 ns |  5.230 ns |  5.136 ns |  4.40 |    0.09 | 0.0629 |     264 B |        2.54 |
| invokeAction_RotateMatcher             |    87.81 ns |  1.705 ns |  1.963 ns |  1.42 |    0.03 | 0.0497 |     208 B |        2.00 |
| invokeAction_CompleteProperty_NoSearch |    67.76 ns |  1.424 ns |  1.801 ns |  1.10 |    0.03 | 0.0248 |     104 B |        1.00 |
| invokeAction_CompleteProperty_Search   |   363.46 ns |  7.280 ns |  9.207 ns |  5.88 |    0.16 | 0.1564 |     656 B |        6.31 |
| invokeAction_CompleteProperty_Rotate   |   307.18 ns |  6.190 ns |  9.996 ns |  4.97 |    0.17 | 0.1392 |     584 B |        5.62 |

TFM が `net6.0` 。

BenchmarkDotNet v0.15.7, Windows 11 (10.0.26200.7171/25H2/2025Update/HudsonValley2)
Intel Core i7-8550U CPU 1.80GHz (Max: 2.00GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100
[Host] : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3 DEBUG
DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3

| Method                                 |        Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------------------------- | ----------: | --------: | --------: | ----: | ------: | -----: | --------: | ----------: |
| invokeAction_Noop                      |    64.63 ns |  1.324 ns |  1.416 ns |  1.00 |    0.03 | 0.0248 |     104 B |        1.00 |
| invokeAction_AddQuery                  |   647.66 ns | 12.872 ns | 12.642 ns | 10.03 |    0.28 | 0.3920 |    1640 B |       15.77 |
| invokeAction_BackwardChar              |   185.61 ns |  3.599 ns |  4.145 ns |  2.87 |    0.09 | 0.0248 |     104 B |        1.00 |
| invokeAction_BackwardWord              |   309.75 ns |  6.180 ns |  8.863 ns |  4.79 |    0.17 | 0.0477 |     200 B |        1.92 |
| invokeAction_DeleteBackwardChar        |    95.88 ns |  1.965 ns |  3.117 ns |  1.48 |    0.06 | 0.0248 |     104 B |        1.00 |
| invokeAction_DeleteBackwardWord        | 1,802.35 ns | 35.392 ns | 33.106 ns | 27.90 |    0.77 | 0.8392 |    3512 B |       33.77 |
| invokeAction_SelectBackwardChar        |   214.03 ns |  4.136 ns |  5.231 ns |  3.31 |    0.11 | 0.0401 |     168 B |        1.62 |
| invokeAction_SelectBackwardWord        |   275.16 ns |  5.398 ns |  7.742 ns |  4.26 |    0.15 | 0.0629 |     264 B |        2.54 |
| invokeAction_RotateMatcher             |    91.23 ns |  1.822 ns |  2.305 ns |  1.41 |    0.05 | 0.0497 |     208 B |        2.00 |
| invokeAction_CompleteProperty_NoSearch |    68.79 ns |  1.428 ns |  2.386 ns |  1.06 |    0.04 | 0.0248 |     104 B |        1.00 |
| invokeAction_CompleteProperty_Search   |   365.48 ns |  7.125 ns |  9.988 ns |  5.66 |    0.19 | 0.1564 |     656 B |        6.31 |
| invokeAction_CompleteProperty_Rotate   |   306.81 ns |  6.065 ns |  9.965 ns |  4.75 |    0.18 | 0.1392 |     584 B |        5.62 |

---

見ての通り、高速化されたといわれてる .NET 10 を benchmark project の Target framework にした場合は明らかに速かった。
つまり実行するところの runtime が一番影響するから、 PowerShell であれば PowerShell 自体の .NET version が影響するって考えたらいいかな。

PowerShell 自体が速くなるのを待っても良いけど、新しい framework 向けの最適化を入れていきたいと検討してる。
今回の prerelease で Multi targeting を実現したが、 prerelease を外す際にはサポートする platform のルール策定をしておきたい。
一番シンプルなのは LTS の PowerShell に合わせる方法なので、現行 LTS の PowerShell 7.4 つまり .NET 8.0 を基準にするのが 1 つの選択肢かな。
つまり今だと PowerShell 7.2 で .NET 8 が最適化サポート最低ライン。
ほかは .NET Standard 2.0 になる。
いま試しに Multi targeting を始めてみた時点では .NET 6.0 と .NET Standard 2.0 なので、 それの最適化される方の閾値を上げる感じ。
ひとつの問題として LTS が進んだらどんどん最適化ラインから外されてしまうという課題もあるのだけど、 PowerShell の LTS を考えたら妥当と思える。
とりまサポート戦略を決めたら `README.md` に書くなりして alpha 外した 0.22.0 として publish してみるのがよさそうかな。
