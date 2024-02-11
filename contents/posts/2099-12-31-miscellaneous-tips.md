---
title: "下書き"
tags: ["dotnet", "git"]
---

### `git config --system core.longpaths true`

以前 [dotnet/docs](https://github.com/dotnet/docs) に PR した流れで [dotnet/sdk](https://github.com/dotnet/sdk) にも同じ PowerShell script があって直すことになった。

dotnet/sdk の git clone するには `git config --system core.longpaths true` を設定しておく必要がある。
Windows だと Path の長さ制限に引っかかって clone できない。 Windows なら例え C 直下でも無理。

[Filename too long in Git for Windows - Stack Overflow](https://stackoverflow.com/questions/22575662/filename-too-long-in-git-for-windows)

ここ数年クソ長パスを扱うことがなかったので、 PC setup 時の設定に入れてなかった。デフォで入って て困るもんでもないし常時入れるようにする。

---

### [What is "XPlat Code Coverage" · Issue #1065 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/issues/1065#issuecomment-765290037)

Coverlet を起動する `dotnet test --collect:"XPlat Code Coverage"` の `"XPlat Code Coverage"` が何なのかを知りたかった。

[What is "XPlat Code Coverage" · Issue #1065 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/issues/1065#issuecomment-765290037)

[vstest/src/vstest.console/Processors/CollectArgumentProcessor.cs at 843cfc974be286d0d2cf4badfc76084d61a26e5f · microsoft/vstest](https://github.com/microsoft/vstest/blob/843cfc974be286d0d2cf4badfc76084d61a26e5f/src/vstest.console/Processors/CollectArgumentProcessor.cs#L295)

```csharp
    internal static class CoverletConstants
    {
        /// <summary>
        /// Coverlet in-proc data collector friendly name
        /// </summary>
        public const string CoverletDataCollectorFriendlyName = "XPlat Code Coverage";

        /// <summary>
        /// Coverlet in-proc data collector assembly qualified name
        /// </summary>
        public const string CoverletDataCollectorAssemblyQualifiedName = "Coverlet.Collector.DataCollection.CoverletInProcDataCollector, coverlet.collector, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        /// <summary>
        /// Coverlet in-proc data collector code base
        /// </summary>
        public const string CoverletDataCollectorCodebase = "coverlet.collector.dll";
    }
```

ふーむ VSTest の決め打ちなのはわかった。

[vstest/docs/contribute.md at 843cfc974be286d0d2cf4badfc76084d61a26e5f · microsoft/vstest](https://github.com/microsoft/vstest/blob/843cfc974be286d0d2cf4badfc76084d61a26e5f/docs/contribute.md?plain=1#L168)

結局 `XPlat` て cross platform てことやねんな。不惑超えるまで知らんかったわ。
