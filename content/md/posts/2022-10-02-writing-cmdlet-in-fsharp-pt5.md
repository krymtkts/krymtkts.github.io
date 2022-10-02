{:title "F#でコマンドレットを書いてる pt.5"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。ほぼ実装におけるメモ、壁打ち。

[Out-ConsoleGridView](https://github.com/PowerShell/GraphicalTools)を見て `hashtable` をフィルタできるのいいな～と思ったので、 `hashtable` サポートを始めている。誰得な機能ではあれど、これがあると自分にとって利用の幅が広がる。

`ProcessRecord` メソッドにおいて、 `IDictionary` 実装の場合のみ展開した要素を内部で保持するようにしてみた。一見して煩雑なので、なんかマシにしたい。 `list` にして `list` 外すあたりがどーにも。

```diff
     override __.ProcessRecord() =
-        input <- List.append input <| List.ofArray __.InputObject
+        let entries: list<obj> =
+            List.ofArray __.InputObject
+            |> List.collect (fun (o: PSObject) ->
+                match o.BaseObject with
+                | :? IDictionary as dct ->
+                    Seq.cast<DictionaryEntry> dct
+                    |> Seq.cast<obj>
+                    |> Seq.toList
+                | _ as o -> [ o ])
+
+        input <- List.append input entries
```

破壊的な操作ならすぐマシなのが思いつくが、ちょっと寝かせる。とはいえそっちでスッキリするならそっちがいいかな。
いや待て、ここまで書いて `fold` の方がスッキリする気が...してきたので後でやってみる。

---

この変更を加えることで `hashtable` の各要素を扱えるようになるけど、依然フィルタした結果は得られない。

`pocof` はいま内部的に各要素の `ToString` メソッドの結果に対して LINQ のクエリをかけてる。
そのため `DictionaryEntry` のような `ToString` の結果が自身のクラスを示す文字列 `System.Collections.DictionaryEntry` を返す場合、使い物にならない。

未実装のプロパティ指定可能にする機能があればまあ使えるので先にやるべきと考えるが、プロパティ未指定時の挙動が定義できてないといけない。
常時プロパティ指定なんてしないので、 `ToString` に値が反映されないタイプのオブジェクトの場合に、デフォルトでフィルタ可能な挙動を定義する必要がある。あーこっちのが大事やわ。
やはり `IDictionary` 実装用に特別処理みたいなのを書いてあげないといけないかなーという気が若干してる。コイツ以外にも色々出てきたら面倒なので気が向かないけど、特別扱いなのはたしかにそうなので。

つづく
