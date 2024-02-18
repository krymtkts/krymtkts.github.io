---
title: "F# でコマンドレットを書いてる pt.32"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

[前回](/posts/2024-02-11-writing-cmdlet-in-fsharp-pt31.html) 以降バグつぶしや更新漏れに対応した。
[#138](https://github.com/krymtkts/pocof/pull/138) で `TopDownHalf` の境界値バグ修正、 [#139](https://github.com/krymtkts/pocof/pull/139) で長らく放置してた日付のフィルタリングバグ修正をした。
そんでこれまた放置気味だったドキュメント更新を [#140](https://github.com/krymtkts/pocof/pull/140) でやった。

[#139](https://github.com/krymtkts/pocof/pull/139) の原因は早い話ローカライズされるべきところがされてなかったことによる。
あんまり理解してなかったことなので、ここに改めてメモを残すことにする。

---

事象としては、わたしの環境で `Get-ChildItem | pocof` したときに、日付を表記通り絞り込めないというバグだった。
プロ .NETer なら多分コレだけで気づくのだろうけど、これは `System.DateTime.ToString` がカルチャの書式情報を利用するためだった。

F# の `string` で `System.DateTime` に対して使うとカルチャ情報に依存しない形で文字列に変換される。
[fsharp/src/FSharp.Core/prim-types.fs · dotnet/fsharp](https://github.com/dotnet/fsharp/blob/fd321f3197981276b52b9fd4f16c912e2d5af8f7/src/FSharp.Core/prim-types.fs#L4950)

```fsharp
             when 'T : DateTime       = let x = (# "" value : DateTime #) in x.ToString(null, CultureInfo.InvariantCulture)
```

かたや PowerShell は通常カルチャ情報に依存した文字列を表示するので異なる結果になる。

わたしの環境はちょっとしたこだわりのせいでカルチャ情報をいじっており、 `[System.Threading.Thread]::CurrentThread.CurrentCulture` は `"en-US"` 相当で、 `[System.Threading.Thread]::CurrentThread.CurrentCulture.DateTimeFormat.ShortDatePattern` を `yyyy-MM-dd` に変えてある。
(`[System.Threading.Thread]::CurrentThread.CurrentCulture.DateTimeFormat.DateSeparator` も `"-"`)

そのためわたしの環境では `System.DateTime.ToString` と F# の `string System.DateTime` はこんな感じで異なる結果になる。

```fsharp
> System.DateTime.Now.ToString();;
val it: string = "2024-02-12 17:15:15"

> string System.DateTime.Now;;
val it: string = "02/12/2024 17:15:22"
```

ふつーに理解してなかったので pocof では `string System.DateTime` してたから、 PowerShell で想定される出力と全然違ってたわけだ。
コレを機にローカライズされた文字列でフィルタリングされるように直ったハズ。

無知は怖いね。

---

あと CI でも [FSharpLint](https://fsprojects.github.io/FSharpLint/) を使ってみたかったので試してみている。
普段は [Ionide](https://ionide.io/) で linting されるのを見てるがやはり自前でルールを用意した上で CI でも見たい。
早速試してみたところ、最新の 0.24.0 だと .NET 8 で実行できない issue があるようだった。

[Execution fails when using dotnet 7.x or 8.x to target net6.0 project · Issue #687 · fsprojects/FSharpLint](https://github.com/fsprojects/FSharpLint/issues/687)

issue にあるように prerelease の 0.24.1--date20240212-1103.git-08ceae7 どっちも試したがダメ。
これは導入諦めて様子見しかないか？と思ったが Ionide の方はなんでうまくいってんのかなと調べてみた。
どうも [FsAutoComplete](https://github.com/fsharp/FsAutoComplete) で使ってる [FSharpLint.Core](https://www.nuget.org/packages/FSharpLint.Core/) の version がちょいと古い 0.21.2 なら大丈夫みたい。
packet の lockfile がそんな感じだった。 [FsAutoComplete/paket.lock · fsharp/FsAutoComplete](https://github.com/fsharp/FsAutoComplete/blob/de453f7f6292f436e1af769bedef202b6d40930c/paket.lock#L87C22-L87C28)

0.21.x 系なら行けるのかと思いきや 0.21.3 と 0.21.10 試してもエラー。 `System.Runtime` 云々出ているので先述の issue に関連するんやろか。
ひとまず linter 導入が先なので、深追いせず一旦ここで留めておく。宿題ということで。

0.24.0 で出るエラー。

```plaintext
Module: pocof ver0.9.0 root=C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof\ publish=C:\Users\takatoshi\dev\github.com\krymtkts\pocof\publish\pocof\
Executing Lint
Unhandled exception. System.TypeInitializationException: The type initializer for 'FSharpLint.Console.Program' threw an exception.
 ---> System.TypeInitializationException: The type initializer for '<StartupCode$dotnet-fsharplint>.$FSharpLint.Console.Program' threw an exception.
 ---> System.TypeInitializationException: The type initializer for '<StartupCode$Ionide-ProjInfo>.$Library' threw an exception.
 ---> System.ComponentModel.Win32Exception (2): An error occurred trying to start process 'C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe' with working directory 'C:\Program Files (x86)\Microsoft Visual Studio\Installer'. The system cannot find the file specified.
...
```

0.24.1--date20240212-1103.git-08ceae7 で出るエラー。

```plaintext
Module: pocof ver0.9.0 root=C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof\ publish=C:\Users\takatoshi\dev\github.com\krymtkts\pocof\publish\pocof\
Executing Lint
Lint failed while analysing pocof.sln.
Failed with: Could not load file or assembly 'System.Runtime, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.
Stack trace:    at Ionide.ProjInfo.ProjectLoader.loadProject(String path, BinaryLogGeneration binaryLogs, FSharpList`1 globalProperties)
...
```

0.21.3 で出るエラー。

```plaintext
Failed to parse file C:\Users\takatoshi\dev\github.com\krymtkts\pocof\src\pocof.Test\Pocof.fs
Exception Message:
The exception has been reported. This internal exception should now be caught at an error recovery point on the stack. Original message: A reference to the type 'System.Runtime.Serialization.ISerializable' in assembly 'System.Runtime' was found, but the type could not be found in that assembly)
Exception Stack Trace:
...
```

0.21.2 を使う workaround がうまくいったので、 `dotnet fsharplint lint pocof.sln` で実行してテストコード含めた全体をチェックすることにする。
ドキュメントに書いてた通りディレクトリは対応してないみたいで、 `pocof.sln` を渡すことにした。間違った情報を渡してもエラーにならずサンプル的なのが出る。ちょっとわかりづらい。

```powershell
> dotnet fsharplint lint ./src/pocof
========== Linting /home/user/Dog.Test.fsx ==========
========== Finished: 0 warnings ==========
========== Summary: 0 warnings ==========
```

warning があると return code が 0 じゃなくなるみたい。
`$? = False` になるみたいなのでそういう `psakefile.ps1` にしといたら task runner への組み込みも容易そう。
とりま GitHub Actions への組み込みまで目標にやってみるか。
