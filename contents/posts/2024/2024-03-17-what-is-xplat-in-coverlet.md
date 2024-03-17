---
title: "Coverlet の XPlat て何だよ"
tags: ["dotnet", "coverlet", "tips"]
---

久しぶりに大酒を飲んでボロボロの二日酔いになってしまったので、 [krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をできなかった。

代わりに、ずっと気になってた coverlet のキーワードの正体を調べた。
Coverlet を起動する `dotnet test --collect:"XPlat Code Coverage"` の `"XPlat Code Coverage"` が何なのかを知りたかった。
世の中は広いので、同じことを考えた人が先にいた。ありがたい。

[What is "XPlat Code Coverage" · Issue #1065 · coverlet-coverage/coverlet](https://github.com/coverlet-coverage/coverlet/issues/1065#issuecomment-765290037)

[vstest/src/vstest.console/Processors/CollectArgumentProcessor.cs at 724f79b5f4fb4ad33c5c6411ea74b31aea6e8776 · microsoft/vstest](https://github.com/microsoft/vstest/blob/724f79b5f4fb4ad33c5c6411ea74b31aea6e8776/src/vstest.console/Processors/CollectArgumentProcessor.cs)

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

ふーむ、 VSTest が決め打ちしている `coverlet.collector` を起動するキーワードなのはわかったが、結局 `XPlat` て何？

[Use code coverage for unit testing - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage?tabs=windows#integrate-with-net-test)

> The "XPlat Code Coverage" argument is a friendly name that corresponds to the data collectors from Coverlet. This name is required but is case insensitive. To use .NET's built-in Code Coverage data collector, use "Code Coverage".

フレンドリーな名前とは...

それはおそらくこちらが答えなんだろうなと思っている。

[vstest/docs/contribute.md at 724f79b5f4fb4ad33c5c6411ea74b31aea6e8776 · microsoft/vstest](https://github.com/microsoft/vstest/blob/724f79b5f4fb4ad33c5c6411ea74b31aea6e8776/docs/contribute.md?plain=1#L63-L64)

```md
- A portable `vstest.console` for desktop (net46 target) and xplat (netcoreapp)
  target
```

.NET の Desktop と NETCoreApp の対比から考えて、結局 `XPlat` て cross platform てことやねんな。
そう思って検索してみたら cross platform て意味で使ってる例がいくつか見つかる。一般的なキーワードなんだろうか。
こんなん不惑超えるまで知らんかったわ...

おわり。
