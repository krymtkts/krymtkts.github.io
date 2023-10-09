---
title: "PowerShell で忘れがちなこと pt.1"
tags: ["powershell"]
---

小ネタ。

`hashtable` は PowerFighter(勝手に作った PowerShell 使いの呼び名) なら誰しも使いまくるに違いない。

リテラルが用意されており書きやすいから、ついつい適さない用途にふんだんに利用してしまい、たまにハマる(自分調べ)。
この度、仕事で使っている PowerShell で書いたスクリプトを久しぶりにメンテしたときハマったので、備忘のため記す。
ハマったのは次のような内容だ。

「複数の数値から重複を取り除く術として `hashtable` を使ったが、`hashtable` に重複が残った」

```powershell
PS> $hash

Name                           Value
----                           -----
1                              True
1                              True
```

なんだこりゃ。

コレが起こった理由は簡単。 1 つ目と 2 つ目の型が異なるからだ。

```powershell
PS> $hash.Keys | % GetType

IsPublic IsSerial Name                                     BaseType
-------- -------- ----                                     --------
True     True     String                                   System.Object
True     True     Int32                                    System.ValueType
```

理由がわかれば簡単でまじでしょーもない内容だったが、本当にコレで小一時間ハマった。
異なる型が混在したのは単純に全ての分岐で同じ型になっていなかったバグなのだけど、REPL だと一見同じ値にしか見えないのでびっくりする。

こんなことにならないように、重複を除く用途では型を厳密にできる [System.Collections.Generic 名前空間](https://learn.microsoft.com/ja-jp/dotnet/api/system.collections.generic?view=net-7.0)のコレクションを使うべきだろう。
型属性がクソ長くなるけど。

先述の、億劫して `hashtable` を使った重複の除去は、 [System.Collections.Generic.HashSet<T>](https://learn.microsoft.com/ja-jp/dotnet/api/system.collections.generic.hashset-1?view=net-7.0) を使えば以下のように安全だ。

ただ各メソッドに戻り値があるので PowerShell の戻り値と相性悪い。
そこが面倒なら [System.Collections.Generic.Dictionary<TKey,TValue>](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2?view=net-7.0) かなあ...宣言がさらに長くなる。

```powershell
PS> $h1 = @{}
PS> $h1[1] = 1
PS> $h1['1'] = 2
PS> $h1

Name                           Value
----                           -----
1                              2
1                              1

PS> [System.Collections.Generic.HashSet[int]]$h2 = @{}
PS> $h2.add(1) | Out-Null # 戻り値を無に帰すのがメンドイ
PS> $h2.add('1') | Out-Null # 同上
PS> $h2

Key Value
--- -----
  1     2

PS> [System.Collections.Generic.Dictionary[int,int]]$h3 = @{} # 長いな...
PS> $h3[1] = 1
PS> $h3['1'] = 2
PS> $h3

Key Value
--- -----
  1     2

```

とはいえ、型属性の長さやメソッドの戻り値の取り回しにさえ目を瞑れれば、あとは提供される暗黙的変換ライフにより安全な生活が始まる。

簡潔な構文糖は手に入らないけど、「こんなに長いのタイプできないにょ...」となるときは、代わりの術はあるから...それで以て茶を濁す。

- Terminal でなら `[hashset` とタイプして TAB を押下すれば、あ～ら不思議、完全名称が自動補完される(これは速くてまじですごい)
- VS Code であれば `[hashset` とタイプして補完候補から選ぶ。Terminal と比較すると若干もっさり感じる

雑多に書きたいときに Generic Type を使うのはちょっと大げさかなという気がしないでもないが、安心安全の PowerLife を送るにはこういうのも必要ということで。
↓ に引用した通り Generics のサポートも手厚くなってるわけだし。

[about Calling Generic Methods - PowerShell | Microsoft Learn](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_calling_generic_methods?view=powershell-7.3)

> Beginning with PowerShell 7.3, you can specify the types for a generic method.
