---
title: "F# で Cmdlet を書いてる pt.84"
subtitle: ".NET 9 の macOS 固有の bug を workaround した"
tags: ["fsharp", "powershell", "dotnet"]
---

久し振りに [krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

GitHub Actions の CI が macOS だけで落ちることがあった。
macOS でだけ [`AccessViolationException`](https://learn.microsoft.com/en-us/dotnet/api/system.accessviolationexception?view=net-10.0) で process ごと落ちてテストが失敗する。
Windows と Linux では通るし、毎回落ちるわけでもないので、落ちた runner を再実行することでしのいでた。
でも最近になってかなり成功率が下がって来たので原因調査をはじめた。
stacktrace は以下の通り。

```plaintext
    Error output: Fatal error. System.AccessViolationException: Attempted to read or write protected memory. This is often an indication that other memory is corrupt.
       at System.SpanHelpers.IndexOfNullByte(Byte*)
       at System.Runtime.InteropServices.Marshal.PtrToStringUTF8(IntPtr)
       at Interop+Sys.AddMountPoint(Void*, Byte*)
       at Interop+Sys.GetAllMountPoints()
       at System.IO.DriveInfo.GetDrives()
       at Microsoft.PowerShell.Commands.FileSystemProvider.InitializeDefaultDrives()
       at System.Management.Automation.Provider.DriveCmdletProvider.InitializeDefaultDrives(System.Management.Automation.CmdletProviderContext)
       at System.Management.Automation.SessionStateInternal.InitializeProvider(System.Management.Automation.Provider.CmdletProvider, System.Management.Automation.ProviderInfo, System.Management.Automation.CmdletProviderContext)
       at System.Management.Automation.SessionStateInternal.NewProvider(System.Management.Automation.ProviderInfo)
       at System.Management.Automation.SessionStateInternal.AddProvider(System.Type, System.String, System.String, System.Management.Automation.PSSnapInInfo, System.Management.Automation.PSModuleInfo)
       at System.Management.Automation.SessionStateInternal.AddSessionStateEntry(System.Management.Automation.Runspaces.SessionStateProviderEntry)
       at System.Management.Automation.Runspaces.InitialSessionState.Bind_LoadProviders(System.Management.Automation.SessionStateInternal)
       at System.Management.Automation.Runspaces.InitialSessionState+<>c__DisplayClass129_1.<Bind>b__3()
       at System.Threading.ExecutionContext.RunInternal(System.Threading.ExecutionContext, System.Threading.ContextCallback, System.Object)
       at System.Threading.Tasks.Task.ExecuteWithThreadLocal(System.Threading.Tasks.Task ByRef, System.Threading.Thread)
       at System.Threading.Tasks.ThreadPoolTaskScheduler.TryExecuteTaskInline(System.Threading.Tasks.Task, Boolean)
       at System.Threading.Tasks.TaskScheduler.TryRunInline(System.Threading.Tasks.Task, Boolean)
       at System.Threading.Tasks.Task.WrappedTryRunInline()
       at System.Threading.Tasks.Task.WaitAllCore(System.ReadOnlySpan`1<System.Threading.Tasks.Task>, Int32, System.Threading.CancellationToken)
       at System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[])
       at System.Threading.Tasks.Parallel.Invoke(System.Threading.Tasks.ParallelOptions, System.Action[])
       at System.Management.Automation.Runspaces.InitialSessionState.Bind(System.Management.Automation.ExecutionContext, Boolean, System.Management.Automation.PSModuleInfo, Boolean, Boolean, Boolean)
       at System.Management.Automation.AutomationEngine..ctor(System.Management.Automation.Host.PSHost, System.Management.Automation.Runspaces.InitialSessionState)
       at System.Management.Automation.Runspaces.LocalRunspace.DoOpenHelper()
       at System.Management.Automation.Runspaces.LocalRunspace.OpenHelper(Boolean)
       at System.Management.Automation.Runspaces.RunspaceBase.CoreOpen(Boolean)
       at System.Management.Automation.Runspaces.RunspaceBase.Open()
       at System.Management.Automation.PowerShell+Worker.CreateRunspaceIfNeededAndDoWork(System.Management.Automation.Runspaces.Runspace, Boolean)
       at System.Management.Automation.PowerShell.CoreInvokeHelper[[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]](System.Management.Automation.PSDataCollection`1<System.__Canon>, System.Management.Automation.PSDataCollection`1<System.__Canon>, System.Management.Automation.PSInvocationSettings)
       at System.Management.Automation.PowerShell.CoreInvoke[[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]](System.Management.Automation.PSDataCollection`1<System.__Canon>, System.Management.Automation.PSDataCollection`1<System.__Canon>, System.Management.Automation.PSInvocationSettings)
       at System.Management.Automation.PowerShell.CoreInvoke[[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]](System.Collections.IEnumerable, System.Management.Automation.PSDataCollection`1<System.__Canon>, System.Management.Automation.PSInvocationSettings)
       at System.Management.Automation.PowerShell.Invoke(System.Collections.IEnumerable, System.Management.Automation.PSInvocationSettings)
       at System.Management.Automation.PowerShell.Invoke()
       at PocofTest.Screen+formatTableOutString@346T[[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]].Invoke(System.__Canon)
       at Pocof.Screen+Buff.WriteScreen(InternalState, Microsoft.FSharp.Core.FSharpResult`2<System.Collections.Generic.IEnumerable`1<System.String>,System.String>, System.Linq.ParallelQuery`1<Entry>)
       at PocofTest.Screen+tests Buff writeScreen@396-19.Invoke(Buff)
       at Microsoft.FSharp.Core.Operators.Using[[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]](System.__Canon, Microsoft.FSharp.Core.FSharpFunc`2<System.__Canon,System.__Canon>)
       at PocofTest.Screen+tests Buff writeScreen@393-18.Invoke(Microsoft.FSharp.Core.Unit)
       at Expecto.Impl+execTestAsync@579-1.Invoke(Microsoft.FSharp.Core.Unit)
       at Microsoft.FSharp.Control.AsyncPrimitives.CallThenInvoke[[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.__Canon, System.Private.CoreLib, Version=9.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]](Microsoft.FSharp.Control.AsyncActivation`1<System.__Canon>, System.__Canon, Microsoft.FSharp.Core.FSharpFunc`2<System.__Canon,Microsoft.FSharp.Control.FSharpAsync`1<System.__Canon>>)
       at Microsoft.FSharp.Control.Trampoline.Execute(Microsoft.FSharp.Core.FSharpFunc`2<Microsoft.FSharp.Core.Unit,Microsoft.FSharp.Control.AsyncReturn>)
       at <StartupCode$FSharp-Core>.$Async+clo@193-1.Invoke(System.Object)
       at System.Threading.ThreadPoolWorkQueue.Dispatch()
       at System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()
       at System.Threading.Thread.StartCallback()
```

調べたところ、どうも原因は .NET runtime の drive 一覧取得処理にある thread safety の bug のようだった。

[System.IO.DriveInfo.GetDrives() random AccessViolationException on macOS · Issue #122634 · dotnet/runtime](https://github.com/dotnet/runtime/issues/122634)

pocof のテストで drive 一覧を取得した覚えはない。
けど、 PowerShell の runspace 初期化で FileSystem provider の既定 drive を登録することで bug を踏むのは見つけた。
[`FileSystemProvider.InitializeDefaultDrives()`](https://github.com/PowerShell/PowerShell/blob/v7.4.21/src/System.Management.Automation/namespaces/FileSystemProvider.cs#L848) が内部で [`DriveInfo.GetDrives()`](https://learn.microsoft.com/en-us/dotnet/api/system.io.driveinfo.getdrives?view=net-10.0) を呼ぶ。
表示用の文字列を作りたいだけでも、そこへ至るまでに filesystem の初期化を通る。
複数のテストが並列で動くと、この初期化処理も重なる。
各テストで別々の PowerShell instance を作っていても、さらに下の runtime が共有する状態で競合してしまう。

この bug は .NET 側で既に修正されている。

[Fix thread-safety issue in DriveInfo.GetDrives() on macOS by replacing getmntinfo with getfsstat · Pull Request #122637 · dotnet/runtime](https://github.com/dotnet/runtime/pull/122637)

この修正は [.NET 10.0.10](https://github.com/dotnet/runtime/releases/tag/v10.0.10) にも backport されている。
ただ残念なことに、 pocof が使っている runtime には届いていないみたい。
pocof 本体の target は `net8.0` と `netstandard2.0` で、テストの実行先は `net9.0` だ。
build に .NET 10 SDK を使っていても、テストが動く .NET 9 runtime の bug はそのまま残る。
SDK や NuGet package を更新しただけでは解消しないので、今回はテスト側 [#515](https://github.com/krymtkts/pocof/pull/515) で workaround した。

workaround 実装は単純で、 macOS の場合だけ描画テストの PowerShell 呼び出しを lock で直列化しただけ。
Windows と Linux はこれまでどおりで、変更したのはテスト用の helper だけだ。

これで Dependabot が作成した依存関係更新 PR も CI がこけず動くようになった。
サポート期間中の .NET 9 に backport されない bugfix があるんやなというのを知れたのも大きいな。
ただ、書いていて気づいたけど、 test project を .NET 10 に上げて修正済みの runtime で動かすだけでも解消できたかもな。
なんで .NET 9 になってるか理由を忘れてしまった。単に忘れてるだけかもな。後ほど試すこととする。
