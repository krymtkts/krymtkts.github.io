---
title: "F# で Cmdlet を書いてる pt.64"
tags: ["fsharp", "powershell", "dotnet", "ilspy"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

昨日 [0.19.0](https://www.powershellgallery.com/packages/pocof/0.19.0) をリリースをした。
1 月中にリリースしたかったが、 Linux で予期せぬエラーが発生する修正をしてしまってたので、リリースが遅れた。

ずっと気づいてなかったが、 Linux 端末上で例えば `UnixStats`[^1] のような platform 依存の property にアクセスすると以下のエラーが発生する。
以下の例では `Size` だが `User` とか `Mode` とか `UnixStats` に属するもの全てエラーになる。

```plaintext
PS /mnt/c/Users/takatoshi> $ErrorView = 'DetailedView'
PS /mnt/c/Users/takatoshi> get-ChildItem | pocof -Layout TopDownHalf ':Size 1'
query>:Size 1                                                                                                                                                                                                                                                  match and [0]
Exception             :
    Type            : System.AggregateException
    InnerExceptions :
        Type           : System.Management.Automation.GetValueInvocationException
        ErrorRecord    :
            Exception             :
                Type    : System.Management.Automation.ParentContainsErrorRecordException
                Message : Exception getting "Size": "There is no Runspace available to run scripts in this thread. You can provide one in the DefaultRunspace property of the System.Management.Automation.Runspaces.Runspace type. The script block you attempted to invoke was: $this.UnixStat.Size"
                HResult : -2146233087
            CategoryInfo          : NotSpecified: (:) [], ParentContainsErrorRecordException
            FullyQualifiedErrorId : ScriptgetValueInvalidOperationException
        TargetSite     :
            Name          : InvokeGetter
            DeclaringType : psscriptproperty
            MemberType    : Method
            Module        : System.Management.Automation.dll
        Message        : Exception getting "Size": "There is no Runspace available to run scripts in this thread. You can provide one in the DefaultRunspace property of the System.Management.Automation.Runspaces.Runspace type. The script block you attempted to invoke was: $this.UnixStat.Size"
        InnerException :
            Type        : System.Management.Automation.PSInvalidOperationException
            ErrorRecord :
                Exception             :
                    Type    : System.Management.Automation.ParentContainsErrorRecordException
                    Message : There is no Runspace available to run scripts in this thread. You can provide one in the DefaultRunspace property of the System.Management.Automation.Runspaces.Runspace type. The script block you attempted to invoke was: $this.UnixStat.Size
                    HResult : -2146233087
                CategoryInfo          : InvalidOperation: (:) [], ParentContainsErrorRecordException
                FullyQualifiedErrorId : ScriptBlockDelegateInvokedFromWrongThread
            TargetSite  :
                Name          : GetContextFromTLS
                DeclaringType : scriptblock
                MemberType    : Method
                Module        : System.Management.Automation.dll
            Message     : There is no Runspace available to run scripts in this thread. You can provide one in the DefaultRunspace property of the System.Management.Automation.Runspaces.Runspace type. The script block you attempted to invoke was: $this.UnixStat.Size
            Source      : System.Management.Automation
            HResult     : -2146233079
            StackTrace  :
   at System.Management.Automation.ScriptBlock.GetContextFromTLS()
   at System.Management.Automation.ScriptBlock.InvokeWithPipe(Boolean useLocalScope, ErrorHandlingBehavior errorHandlingBehavior, Object dollarUnder, Object input, Object scriptThis, Pipe outputPipe, InvocationInfo invocationInfo, Boolean propagateAllExceptionsToTop, List`1 variablesToDefine, Dictionary`2 functionsToDefine, Object[] args)
   at System.Management.Automation.PSScriptProperty.InvokeGetter(Object scriptThis)
        Source         : System.Management.Automation
        HResult        : -2146233087
        StackTrace     :
   at System.Management.Automation.PSScriptProperty.InvokeGetter(Object scriptThis)
   at Pocof.Query.x@162.Invoke(Entry entry)
   at lambda_method453(Closure, Entry)
   at System.Linq.Parallel.OrderPreservingPipeliningSpoolingTask`2.SpoolingWork()
   at System.Linq.Parallel.SpoolingTaskBase.Work()
   at System.Linq.Parallel.QueryTask.BaseWork(Object unused)
   at System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop(Thread threadPoolThread, ExecutionContext executionContext, ContextCallback callback, Object state)
...
```

[^1]: `UnixStat` のことはよく知らないが、[PowerShell のコードにちらっと現れる](https://github.com/PowerShell/PowerShell/blob/ed982b43384baf4e1f782f11b6cf558aa424ea1c/src/System.Management.Automation/namespaces/ProviderBase.cs#L1833)。こいつを掘り下げて理解するのが今後の宿題かな

`RunSpace` 割あたってると思ってたけどなんか違うらしい。
[`PSCmdlet.InvokeCommand.InvokeScript`](<https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.commandinvocationintrinsics.invokescript?view=powershellsdk-7.4.0#system-management-automation-commandinvocationintrinsics-invokescript(system-management-automation-sessionstate-system-management-automation-scriptblock-system-object())>) を main thread で実行してるだけなので、変なことなさそうなんやが。

実は [0.18.1](https://www.powershellgallery.com/packages/pocof/0.18.1) 以前からのエラーは出てて、単に [`try ... with ...`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/exception-handling/the-try-with-expression) でエラーを捕捉した場合に property の値が取れなかったとして `None` を返す仕様にしてたので気づいてないだけだった。
このエラー措置は元々 [`NullReferenceException`](https://learn.microsoft.com/en-us/dotnet/api/system.nullreferenceexception?view=net-9.0) 用に作ってた気がしたけど、結果的に他のエラーも救ってたことになる。
0.19.0 に含まれる修正で `NullReferenceException` の恐れがないとみなしこのエラー救護措置を取り除いたところ、ひょっこり現れたという形だ。

ひとまず従来の挙動とリリース優先を優先して `try ... with ...` でエラーを潰しただけなので、できればちゃんと対応したいところ。

[Resolve errors when accessing Unix-dependent properties such as `User`, `Group`, `UnixMode`, `Size`, etc. · Issue #319 · krymtkts/pocof](https://github.com/krymtkts/pocof/issues/319)

あとの修正は、依存関係を更新したり、 benchmark とって改善したりだけだ。
改善といっても大きくロジックを書き換えたのはなくて不要な list 変換を避けるとかそういうちまちましたものばかり。
この過程で [ILSpy](https://github.com/icsharpcode/ILSpy) で面白いことに気付いたので書き残しておく。

---

もし速さを追求して IL の命令数まで意識する必要がある場合、 F# っぽい書き方は C# っぽい書き方より overhead があって使いにくいかも知れないという話。

```powershell
> dotnet --version
9.0.102
> dotnet fsi --version
Microsoft (R) F# Interactive version 12.9.101.0 for F# 9.0
```

でビルドして、 ILSpy で C# 12 と一部 IL で見た。
また pocof の `TargetFramework` は `netstandard2.0` だ。
F# が吐く IL は version up で改善が続いてるし、この見解は今だけの話かもしれないことに留意すること。

こういうコードがあるとして、 `toString __ |> String.lower` の部分の書き方で生成される IL の違いを見た。

```fsharp
    [<RequireQualifiedAccess>]
    [<NoComparison>]
    [<Struct>]
    type Matcher =
        | Eq
        | Like
        | Match

        override __.ToString() =  toString __ |> String.lower
```

```csharp
// toString __ |> String.lower
public override string ToString()
{
    string s = toString(this);
    return LanguageExtension.String.lower(s);
}
```

```csharp
// _.Property shorthand
public override string ToString()
{
    string text = toString(this);
    string text2 = text;
    return text2.ToLower();
}
```

```plaintext
.method public hidebysig virtual
    instance string ToString () cil managed
{
    .param [0]
        .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
            01 00 00 00 00
        )
    // Method begins at RVA 0x9238
    // Header size: 12
    // Code size: 21 (0x15)
    .maxstack 3
    .locals init (
        [0] string 'Pipe #1 input at line 235',
        [1] string
    )

    IL_0000: ldarg.0
    IL_0001: ldobj Pocof.Data/Matcher
    IL_0006: call string Pocof.Data::toString<valuetype Pocof.Data/Matcher>(!!0)
    IL_000b: stloc.0
    IL_000c: ldloc.0
    IL_000d: stloc.1
    IL_000e: ldloc.1
    IL_000f: callvirt instance string [netstandard]System.String::ToLower()
    IL_0014: ret
} // end of method Matcher::ToString
```

inline expansion と \_.Property shorthand は同じ IL になる。

```csharp
// inline
public override string ToString()
{
    string text = toString(this);
    string text2 = text;
    return text2.ToLower();
}
```

C# ライクに書くのが最も命令数が少ない。

```csharp
// (toString __).ToLower()
public override string ToString()
{
    return toString(this).ToLower();
}
```

```plaintext
.method public hidebysig virtual
    instance string ToString () cil managed
{
    .param [0]
        .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
            01 00 00 00 00
        )
    // Method begins at RVA 0x9238
    // Header size: 1
    // Code size: 17 (0x11)
    .maxstack 8

    IL_0000: ldarg.0
    IL_0001: ldobj Pocof.Data/Matcher
    IL_0006: call string Pocof.Data::toString<valuetype Pocof.Data/Matcher>(!!0)
    IL_000b: callvirt instance string [netstandard]System.String::ToLower()
    IL_0010: ret
} // end of method Matcher::ToString
```

以下の `>>` と `|>` の結果から、関数合成は遅いという話は本当なんだ。
また、 pipeline 演算するだけで余計にコピーしてるのはちょっとショック。

```csharp
// >>
public override string ToString()
{
    Matcher matcher = this;
    FSharpFunc<Matcher, string> @_instance;
    @_instance = ToString@235.@_instance;
    FSharpFunc<string, string> @_instance2;
    @_instance2 = ToString@235-1.@_instance;
    Matcher matcher2 = matcher;
    return @_instance2.Invoke(@_instance.Invoke(matcher2));
}
```

```csharp
// |>
public override string ToString()
{
    Matcher x = this;
    string text = toString(x);
    string text2 = text;
    return text2.ToLower();
}
```

```plaintext
.method public hidebysig virtual
    instance string ToString () cil managed
{
    .param [0]
        .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
            01 00 00 00 00
        )
    // Method begins at RVA 0x9238
    // Header size: 12
    // Code size: 23 (0x17)
    .maxstack 3
    .locals init (
        [0] valuetype Pocof.Data/Matcher 'Pipe #1 input at line 235',
        [1] string 'Pipe #1 stage #1 at line 235',
        [2] string
    )

    IL_0000: ldarg.0
    IL_0001: ldobj Pocof.Data/Matcher
    IL_0006: stloc.0
    IL_0007: ldloc.0
    IL_0008: call string Pocof.Data::toString<valuetype Pocof.Data/Matcher>(!!0)
    IL_000d: stloc.1
    IL_000e: ldloc.1
    IL_000f: stloc.2
    IL_0010: ldloc.2
    IL_0011: callvirt instance string [netstandard]System.String::ToLower()
    IL_0016: ret
} // end of method Matcher::ToString
```

IL の命令数を極限まで減らしていこうと pocof はしてないのだけど、 \_.Property shorthand は気軽に使える高速な方法ってことで良さそう(実際そうした)。
逆にこれの結果に従うと C# like に書ける箇所は `.` で引きずり回す方が効率が良いのでそうしようとなってしまうが、 pipeline の方が好きなので当面は採用しないでおく。
極限まで削らないといけないところが出たら局所的に採用するかも知れんけど。

最近 [プログラマーのための CPU 入門 CPU は如何にしてソフトウェアを高速に実行するか](/booklogs/what-a-programmer-should-know-about-the-cpu.html) を読んでるのもあって命令数に敏感になってるから気になってるだけって感じではある。
もっと全体的に見直す価値ある箇所あるのではよそっちやれ、という自戒。
