---
title: "F# で Cmdlet を書いてる pt.74"
subtitle: "Reference Tuple と Struct Tuple"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。

[#364](https://github.com/krymtkts/pocof/pull/364) で Reference Tuple を Struct Tuple に置き換えて allocation を抑えた。
全てのケースではないが一部高速化に寄与した。
色々勉強になったので、メモがてら記録しておく。

---

Reference Tuple は [`System.Tuple`](https://learn.microsoft.com/en-us/dotnet/api/system.tuple?view=net-9.0) に compile される。
Struct Tuple は [`System.ValueTuple`](https://learn.microsoft.com/en-us/dotnet/api/system.valuetuple?view=net-9.0) に compile される。
そのため F# の Struct Tuple は C# と Visual Basic の tuple と相互運用できる特徴があるらしい。
これらの情報は Microsoft Learn のページが詳しい(当然か)。

[Tuples - F# | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/tuples)

Reference Tuple は、(64 bit なら) object header の 8B と method table pointer の 8B の計 16B がデフォルトで確保される[^1]。
追加で定義した field 分のメモリが heap に確保される。
value type ではこの object header/method table pointer 分が節約できる。
代わりに引数等で渡したときに参照アドレスでなく値がコピーされる。
つまりサイズが大きい tuple だとコピー効率が悪くなって遅くなることがある。
小さい tuple であれば十分速くてメモリ効率もよく、特に hot path で改善効果が見込める。
概ね節約分に収まれば大丈夫だろう。

[^1]:
    どうも [The Book of the Runtime](https://github.com/dotnet/runtime/tree/main/docs/design/coreclr/botr) を読めばそれがわかるらしいが調べられてない。わたしは [ObjectLayoutInspector](https://github.com/SergeyTeplyakov/ObjectLayoutInspector) を使ってその存在を知っているだけだ。
    また、いくつかの古い文書では可視化されたそれらの画像を確認できる。
    [Managed object internals, Part 1. The layout - Developer Support](https://devblogs.microsoft.com/premier-developer/managed-object-internals-part-1-layout/) は ObjectLayoutInspector 作者の記事。
    [.NET Framework Internals: How the CLR Creates Runtime Objects | Microsoft Learn](https://learn.microsoft.com/en-us/archive/msdn-magazine/2005/may/net-framework-internals-how-the-clr-creates-runtime-objects) の ObjectInstance の節とか。

ここまではわたしも知っていた value type の特徴だ。

ここからは今回初めて知った Reference Tuple の振る舞いについて。
よく知られたものと思われるが、個人的におもしろかったので残しておく。

先述の通り Reference Tuple を使っていると heap 割当が発生しそうで全部 Struct Tuple に変えたくなる。
だが benchmark 計測してみると、必ずしも heap 割当が発生するわけではなく、小さな Struct Tuple でも遅くなることもある。

例えば以下は [ILSpy](https://github.com/icsharpcode/ILSpy) で F# のコードを decompile して C# のコードで表したものだ。
前者は関数内の値の受け渡しに Struct Tuple を使っていた版で、後者は Reference Tuple を使っていた版。

```csharp
// pocof, Version=0.20.0.0, Culture=neutral, PublicKeyToken=null
// Pocof.Handle
using Microsoft.FSharp.Core;

[CompilationArgumentCounts(new int[] { 1, 1 })]
internal static (Data.InternalState, Query.QueryContext) deleteForwardInput(Data.InternalState state, Query.QueryContext context)
{
    int num = StringModule.Length(state.QueryState@.Query@);
    Data.InputMode inputMode = state.QueryState@.InputMode@;
    (Data.InternalState, Query.QueryContext, int) valueTuple;
    int num2;
    if (inputMode.Tag != 1)
    {
        valueTuple = (state, context, state.QueryState@.Cursor@);
    }
    else
    {
        num2 = inputMode.count;
        int num3 = state.QueryState@.Cursor@;
        int num4 = state.QueryState@.Cursor@ - num2;
        int num5 = ((num3 >= num4) ? num4 : num3);
        (Data.InternalState, Query.QueryContext) tuple = FSharpFunc<int, Data.InputMode>.InvokeFast(setCursor, num5, Data.InputMode.Input, state, context);
        valueTuple = (tuple.Item1, tuple.Item2, num5);
    }
    Data.InternalState state2;
    Query.QueryContext context2;
    (state2, context2, num2) = valueTuple;
    return removeCharsWithInputMode(Direction.Forward, num - num2, state2, context2);
}


// pocof, Version=0.20.0.0, Culture=neutral, PublicKeyToken=null
// Pocof.Handle
using Microsoft.FSharp.Core;

[CompilationArgumentCounts(new int[] { 1, 1 })]
internal static (Data.InternalState, Query.QueryContext) deleteForwardInput(Data.InternalState state, Query.QueryContext context)
{
    int num = StringModule.Length(state.QueryState@.Query@);
    Data.InputMode inputMode = state.QueryState@.InputMode@;
    Data.InternalState state2;
    Query.QueryContext context2;
    int num2;
    if (inputMode.Tag != 1)
    {
        state2 = state;
        context2 = context;
        num2 = state.QueryState@.Cursor@;
    }
    else
    {
        int num3 = inputMode.count;
        int num4 = state.QueryState@.Cursor@;
        int num5 = state.QueryState@.Cursor@ - num3;
        int num6 = ((num4 >= num5) ? num5 : num4);
        (Data.InternalState, Query.QueryContext) tuple = FSharpFunc<int, Data.InputMode>.InvokeFast(setCursor, num6, Data.InputMode.Input, state, context);
        Data.InternalState item = tuple.Item1;
        Query.QueryContext item2 = tuple.Item2;
        state2 = item;
        context2 = item2;
        num2 = num6;
    }
    return removeCharsWithInputMode(Direction.Forward, num - num2, state2, context2);
}
```

Reference Tuple の方は、最適化で Reference Tuple の存在が消し去られているのだろうとわかる。
Struct Tuple の方は、そのまま関数内の値の受け渡しにも tuple が使われたままになることがあった。
その tuple がある分のコストがかかるので、この場合は Reference Tuple の方が速いしメモリの割当も少ない。
このような tuple が公開されていない場合は Struct Tuple が不利となる場合があると理解できる。

でも例えば、公開された関数の戻り値が Reference Tuple の場合は、確実に Reference Tuple の利用が発生する。
以下は、前者は戻り値の型が Struct Tuple を使っていた版で、後者は Reference Tuple を使っていた版の F# → (decompile) → C# のコード。

```csharp
public override (int, int, int, int) Invoke(Unit unitVar0)
{
  int windowHeight = this.@this.rui.GetWindowHeight();
  return (0, 1, 2, windowHeight - 3);
}

public override Tuple<int, int, int, int> Invoke(Unit unitVar0)
{
    int windowHeight = this.@this.rui.GetWindowHeight();
    return new Tuple<int, int, int, int>(0, 1, 2, windowHeight - 3);
}
```

このケースでは前者が圧倒的に良い。
メモリも 4 + 4 + 4 + 4 = 16 byte のみ。
Reference Tuple なら object header + method table pointer = 16 byte が更に乗って 32 byte だからだ。

端的に言えば Reference Tuple は F# の compile 時の optimization で優遇されているみたい、というのが今回具体的にわかった。
この特徴からも、よく言われる「 Benchmark を測って利用が最適か評価する」必要があるということになる。
いちいち計測するのも時間がかかって大変だが。
勉強になったわ～。
