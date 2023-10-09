---
title: "F#でコマンドレットを書いてる pt.6"
tags: ["fsharp","powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

`hashtable` サポートした。

> やはり `IDictionary` 実装用に特別処理みたいなのを書いてあげないといけないかなーという気が若干してる。コイツ以外にも色々出てきたら面倒なので気が向かないけど、特別扱いなのはたしかにそうなので。

前回コメントしていた実装方針にした。先のことはわからないので、いま想定できない未来は未来の自分に託す。

これによって `pocof` は `hashtable` のエントリをフィルタできるようになった。 クエリは `hashtable` の `Key` / `Value` それぞれに適用される。こんな感じ ↓。

```powershell
@{a='1';b='2';c='3';d='a1';e='b2'} | pocof -NonInteractive -Query a

# Name                           Value
# ----                           -----
# a                              1
# d                              a1
```

ただしフィルタして得られる値は `hashtable` ではなく、 `Object[]` (中身は `System.Collections.DictionaryEntry`) になる。
一瞬 `hashtable` に戻したいかな～と思ったが、色々使い道を模索しているときに以下のようなおもしろ利用法あるとわかったので、そのケースで不都合ありそうでやめた。

```powershell
# 十把一絡げ
$misc = @(Get-Date; @{a=1;b=2;c=3}; (10..15))

# Sunday, 16 October, 2022 15:25:19
#
# Key   : c
# Value : 3
# Name  : c
#
#
# Key   : b
# Value : 2
# Name  : b
#
#
# Key   : a
# Value : 1
# Name  : a
#
# 10
# 11
# 12
# 13
# 14
# 15

$misc | pocof -NonInteractive -Query 15

# Sunday, 16 October, 2022 15:25:19
# 15
```

因みにこういったケースの活用方法はまだ見出していない。ただなんかこのファジーな検索あると面白そうなので残している。例えば、色んなものを array に詰め込んでおいて後でから見ようってアプローチとかになるんではないかと。
仕事の製品コードにこういうの見つけたら卒倒しそうやけど、自分が terminal でなんかデータを捏ねくり回すときにあったら、新たな世界が開けるのでは？的な。しらんけど。

[`Out-ConsoleGridView`](https://github.com/PowerShell/GraphicalTools) はこういう事できなくて、同じ型が揃ってないとエラーになる。

```powershell
$misc | Out-ConsoleGridView
# Out-ConsoleGridView: Object reference not set to an instance of an object.
```

[`Where-Object`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/where-object?view=powershell-7.2) は流石懐が深くて、 `-FilterScript` を使えば同様のことができる。

```powershell
$misc | ? {$_ -match 15}
```

ひとまず `hashtable` サポートも終えたので、 プロパティ指定でフィルタする機能を実装する気持ちが高まってきてる。
だけど、このファジーなフィルタの発見によって、フィルタ対象の array に登場する型をチェックしてプロパティを得て...みたいなことが必要になり、面倒さが増した。

率直に言ってコレだるいｗのだけど、こういう楽しみって趣味プロならではの醍醐味じゃないかな。第一ユーザが自分なので、自分が納得できないモノは許容し難いのよな。

まだまだゆるく長く楽しめそうな気配がしていて、良い。
