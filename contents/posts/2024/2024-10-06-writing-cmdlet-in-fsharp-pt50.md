---
title: "F# で Cmdlet を書いてる pt.50"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。 [v0.16.0](https://www.powershellgallery.com/packages/pocof/0.16.0) と、そのバグ修正版の [v0.16.1](https://www.powershellgallery.com/packages/pocof/0.16.1) をリリースしたのでその話を書く。

[コードクォート](https://learn.microsoft.com/ja-jp/dotnet/fsharp/language-reference/code-quotations)でクエリの述語を実装してみた版を v0.16.0 でリリースした。
pocof は [.NET Standard 2.0](https://learn.microsoft.com/ja-jp/dotnet/standard/net-standard?tabs=net-standard-2-0) を対象にしてるので PowerShell と Windows PowerShell の両方で動くのだけど、この版に含まれる変更に起因して Windows PowerShell でだけエラーになるバグ [#235](https://github.com/krymtkts/pocof/issues/235) が発生した。

```powershell
> '' | pocof

# pocof : The startIndex argument must be greater than or equal to zero.
# Parameter name: startIndex
# At line:1 char:6
# + '' | pocof
# +      ~~~~~
#     + CategoryInfo          : NotSpecified: (:) [Select-Pocof], ArgumentOutOfRangeException
#     + FullyQualifiedErrorId : System.ArgumentOutOfRangeException,Pocof.SelectPocofCommand
```

Windows/Linux/MacOS の PowerShell ではエラーにならない。率直に Windows PowerShell になにか問題あるよなと考えた。

バグが入り込んだコミット [krymtkts/pocof@2554092](https://github.com/krymtkts/pocof/commit/255409205df5a238abf00dbe2806b65e411e2d82) は人力二分探索で特定した。
該当のコミットでの変更差分は以下の通り。

```diff
         member __.Receive() =
-            match renderStack.Count with
-            | 0 -> RenderMessage.None
-            | c ->
-                let items = Array.zeroCreate<RenderEvent> c
-                renderStack.TryPopRange(items) |> ignore
-                items |> Array.toList |> getLatestEvent
+            let items = Array.zeroCreate<RenderEvent> renderStack.Count
+            renderStack.TryPopRange(items) |> ignore
+            items |> Array.toList |> getLatestEvent
```

コレ見て分かる通り、 0 のケースを [`ConcurrentStack<T>.TryPopRange`](<https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentstack-1.trypoprange?view=net-8.0#system-collections-concurrent-concurrentstack-1-trypoprange(-0())>) に寄せたのが良くなかったみたい。
PowerShell 7.4 と Windows PowerShell 5.1 はそれぞれ .NET 8 と .NET Framework 4.5 に依存しててたまに挙動の違いがあるからこういうこともあるよなという感じ。
今回はそこのテストが不十分だった。

このエラーは Windows PowerShell でも確認できる。依存する .NET/.NET Framework で動くからな。
(Windows PowerShell のほうが CLRVersion 4.0. ~ なのがよくわからんかったが CLRVersion は major version で一緒ということを知った)

```powershell
# PowerShell の場合
> $PSVersionTable; $s = [System.Collections.Concurrent.ConcurrentStack[System.String]]::new(); $s.Count; $s.TryPopRange(@())

# Name                           Value
# ----                           -----
# PSVersion                      7.4.5
# PSEdition                      Core
# GitCommitId                    7.4.5
# OS                             Microsoft Windows 10.0.22631
# Platform                       Win32NT
# PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0…}
# PSRemotingProtocolVersion      2.3
# SerializationVersion           1.1.0.1
# WSManStackVersion              3.0
# 0
# 0
```

```powershell
# Windows PowerShell の場合
> $PSVersionTable; $s = [System.Collections.Concurrent.ConcurrentStack[System.String]]::new(); $s.Count; $s.TryPopRange(@())

# Name                           Value
# ----                           -----
# PSVersion                      5.1.22621.4249
# PSEdition                      Desktop
# PSCompatibleVersions           {1.0, 2.0, 3.0, 4.0...}
# BuildVersion                   10.0.22621.4249
# CLRVersion                     4.0.30319.42000
# WSManStackVersion              3.0
# PSRemotingProtocolVersion      2.3
# SerializationVersion           1.1.0.1
# 0
# Exception calling "TryPopRange" with "1" argument(s): "The startIndex argument must be greater than or equal to zero.
# Parameter name: startIndex"
# At line:1 char:104
# + ... ConcurrentStack[System.String]]::new(); $s.Count; $s.TryPopRange(@())
# +                                                       ~~~~~~~~~~~~~~~~~~~
#     + CategoryInfo          : NotSpecified: (:) [], MethodInvocationException
#     + FullyQualifiedErrorId : ArgumentOutOfRangeException
```

とここまで追い込んだら修正するだけだったので、 [#237](https://github.com/krymtkts/pocof/pull/237) で修正して v0.16.1 を出した。

---

ここからはタダの brain dump だ。

直し方がわかって修正したらあとはこの動作の差異が何なのかというのが気になる。コードを見に行った。
[microsoft/referencesource](https://github.com/microsoft/referencesource/?tab=MIT-1-ov-file#readme) [dotnet/runtime/](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) どちらもライセンスが MIT なので snippet を貼る。
以下は `ConcurrentStack<T>.TryPopRange` の引数の validation をしているメソッドだ。

.NET の方は [runtime/src/libraries/System.Collections.Concurrent/src/System/Collections/Concurrent/ConcurrentStack.cs](https://github.com/dotnet/runtime/blob/48dbc4fc836da67cf1efb6b348499a918c4dea8e/src/libraries/System.Collections.Concurrent/src/System/Collections/Concurrent/ConcurrentStack.cs#L390-L404) から [runtime/src/libraries/System.Collections/src/System/Collections/ThrowHelper.cs](https://github.com/dotnet/runtime/blob/48dbc4fc836da67cf1efb6b348499a918c4dea8e/src/libraries/System.Collections/src/System/Collections/ThrowHelper.cs#L64-L71) を呼ぶ。

```csharp
        private static void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
        {
            ArgumentNullException.ThrowIfNull(items);

            ArgumentOutOfRangeException.ThrowIfNegative(count);

            int length = items.Length;
            ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, length);
            ArgumentOutOfRangeException.ThrowIfNegative(startIndex);

            if (length - count < startIndex) //instead of (startIndex + count > items.Length) to prevent overflow
            {
                throw new ArgumentException(SR.ConcurrentStack_PushPopRange_InvalidCount);
            }
        }
```

```csharp
        public static void ThrowIfGreaterThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string? paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) > 0)
            {
                ThrowGreater(value, other, paramName);
            }
        }
```

.NET Framework の方は [referencesource/mscorlib/system/collections/Concurrent/ConcurrentStack.cs](https://github.com/microsoft/referencesource/blob/51cf7850defa8a17d815b4700b67116e3fa283c2/mscorlib/system/collections/Concurrent/ConcurrentStack.cs#L473C1-L492)

```csharp
        private void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ConcurrentStack_PushPopRange_CountOutOfRange"));
            }
            int length = items.Length;
            if (startIndex >= length || startIndex < 0)
            {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ConcurrentStack_PushPopRange_StartOutOfRange"));
            }
            if (length - count < startIndex) //instead of (startIndex + count > items.Length) to prevent overflow
            {
                throw new ArgumentException(Environment.GetResourceString("ConcurrentStack_PushPopRange_InvalidCount"));
            }
        }
```

.NET の方で `ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, length)` になったとき `startIndex = length` のケースがエラーの対象に含まれなくなってんのよね。

ドキュメントも見てきた。
.NET 8 が [ConcurrentStack<T>.TryPopRange Method (System.Collections.Concurrent) | Microsoft Learn](<https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentstack-1.trypoprange?view=net-8.0#system-collections-concurrent-concurrentstack-1-trypoprange(-0())>) 。
.NET Framework 4.5.2 が [ConcurrentStack<T>.TryPopRange Method (System.Collections.Concurrent) | Microsoft Learn](<https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentstack-1.trypoprange?view=netframework-4.5.2#system-collections-concurrent-concurrentstack-1-trypoprange(-0()-system-int32-system-int32)>) 。

どっちも `ArgumentOutOfRangeException` にはこう書いてる。

> ArgumentOutOfRangeException
> startIndex or count is negative. Or **startIndex is greater than or equal to the length of items.**

これって実装がドキュメントの挙動と違うねんけど、どっちが間違ってんの？となって理解進んでないのが現状。いやさっさと聞きに行ったら良いねんけど、どっちが妥当なんか自分の中で腑に落ちてなくて立ち止まってるところ。
空の配列渡したら何の操作もなく戻る現状の挙動の方が使い良くて好きやけど、ちゃんとした裏付けしたいのでもうちょっと調べる。

例えばやで、仮にこう ↓ するとエラーにならんのよね。これは `item.Length` より大きい index を `startIndex` が指してるからやっぱおかしい気もするわ。
`count = 0` の場合を特別扱いにするとかしないと辻褄あった API 仕様にならんのちゃうかなあ。

```powershell
$s = [System.Collections.Concurrent.ConcurrentStack[System.String]]::new(); $s.Push("a"); $s.Count; $a = @($null); $s.TryPopRange(@('a'), 1, 0)
# 1
# 0
```

続く。
