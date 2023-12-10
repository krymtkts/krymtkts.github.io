---
title: "F# でコマンドレットを書いてる pt.26"
tags: ["fsharp", "powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

.NET 8 へ更新して、 F# 8 の機能を使ってみた。

---

SDK の更新ちょっといい手順がよくわからんのよな。
pocof の場合は以下でやった。 [#88](https://github.com/krymtkts/pocof/pull/88)

```powershell
dotnet new globaljson --sdk-version 8.0.100 --roll-forward latestFeature --force
dotnet add ./src/pocof.Test package Microsoft.PowerShell.SDK --version 7.4.0
```

test project の方は `TargetFramework` も `net8.0` にしたのだけど、本体の方はやめておいた。
なぜかというと `netstandard2.0` にして Windows PowerShell もサポートしてみたいなという気持ちが突如湧き、そのようにしたからだ。

参考までに、 lockfile を生成している blog-fable の場合だと以下の手順になった。
[krymtkts/blog-fable#120](https://github.com/krymtkts/blog-fable/pull/120/files)

````md
```powershell
dotnet new globaljson --sdk-version 8.0.100 --roll-forward latestFeature --force
```

remove these from `App.fsproj`.

```xml
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <RestoreLockedMode>true</RestoreLockedMode>
```

```powershell
dotnet clean
fable clean
npm run build
# build success.
```

restore `App.fsproj` configuration.

```powershell
npm run build
# lockfile regenerated successfully.
```
````

このように、まず `fsproj` でロックしない設定にしてからビルドしないと、 F# の DLL の lockfile が期待のとおりにならなくてエラーを解消できなかった。

---

さて、ここまで来てようやく F# 8 の機能が使えるようになった。

[Announcing F# 8 - .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-fsharp-8/)

上記のアナウンスを参考にして、 Nested record field update 、 `_.Property` shorthand と `TailCall`` attribute を導入してみた。
[#90](https://github.com/krymtkts/pocof/pull/90)

```diff
     let private switchMatcher (state: InternalState) =
         { state with
-            QueryState =
-                { state.QueryState with
-                    Matcher =
-                        match state.QueryState.Matcher with
-                        | EQ -> LIKE
-                        | LIKE -> MATCH
-                        | MATCH -> EQ } }
+            InternalState.QueryState.Matcher =
+                match state.QueryState.Matcher with
+                | EQ -> LIKE
+                | LIKE -> MATCH
+                | MATCH -> EQ }
```

書き換えるときに良さは感じなかったけど、これ実際に書くときに良さを感じるはず。

```diff
                         |> Seq.head
                         |> PSObject.AsPSObject
-                        |> (fun o -> o.Properties)
+                        |> _.Properties
                 | _ -> o.Properties)
-            |> Seq.map (fun p -> p.Name)
+            |> Seq.map _.Name
             |> Seq.fold (fun acc n -> acc.Add n) properties
```

もうこれは書き換えるときでも読みやすくなったなって感じがするわな。

`TailCall` attribute に関しては、 module 直下の関数かメソッドしか使えないみたい。関数内の関数に付与した場合、それが末尾再帰になってなくても警告されない。
どっかで文書を見かけた気がするのだけど今探したら見つけられなかった。なんしか自分で動作確認をしてそうなってるというのは確認している。
だからこれを使うためだけに関数内の関数を外に出した。

やっぱこういう検査機構は、ミスってないお墨付きがつくから気持ちが良い。
もちろん今回付与した部分も大丈夫だった。

---

あとこの流れの中で PowerShell Gallery へ up してる配布物が pdb を含んだままになってると気づいたので、 DL サイズ下げたいしなくすことにした。

以下の文書を参考に設定した。

[Common MSBuild Project Properties - MSBuild | Microsoft Learn](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties?view=vs-2022)

```xml
  <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
```

これでちょっとだけ配信サイズが縮んだ。

---

もうそろそろ次のリリースをしていい加減 alpha 外したい気持ちがしてきた。
でもせめて Linux 用ビルドができからやりたいなーという気持ちもあり、年内どう最後を締めるか悩んでる。
目標届かなくても改善点は細々あるし、なんなら .NET Standard 2.0 まで下げたのもあるから、出していいと思うけどなー。気持ちの面が邪魔してくる。
