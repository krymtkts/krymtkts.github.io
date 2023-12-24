---
title: "F# でコマンドレットを書いてる pt.28"
tags: ["fsharp", "powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

今年最後の version 0.7 を release に向けて、これまでの TODOs を回収している最中。 [#98](https://github.com/krymtkts/pocof/pull/98)
全部の TODOs を解消できるわけではないけど、前から直したかった 2 点を修正している。

1. 不正なキーマップの検査
2. キー入力ごとのクエリ構築を毎回無から構築するのでなくて、入力されたキーに合わせた差分更新をする

結構大幅な内部設計の変更を [#98](https://github.com/krymtkts/pocof/pull/98) で行う感じ。
これを終えたら version 0.7 を来週の年末休み中にリリースする。

---

不正なキーマップの検査は単に [`EndProcessing`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.endprocessing?view=powershellsdk-7.4.0) から [`BeginProcessing`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.beginprocessing?view=powershellsdk-7.4.0) にオプションからのキーマップ構築を移すだけだったけど面倒で放置してた。これを今回サクッと書けそうだったので対応した。

失敗の可能性があるところは [`Results`](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results) を用いて失敗の可能性を示すのが一般的だが、従来は手抜きで `failWith` していたので、定石どおり関数の戻り値を `Results` に変えた。
これを `BeginProcessing` で `Error` を捕捉したら [`ArgumentException`](https://learn.microsoft.com/en-us/dotnet/api/system.argumentexception?view=net-8.0) を投げてパラメータの検査としている。

恥ずかしながら理解してなかったのだけど [`Seq.map`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seqmodule.html#map) や [`Seq.fold`](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-seqmodule.html#fold) は順序を保証しない。
`Hashtable` を `Seq.cast<DictionaryEntry>` して以降は `seq` で取り回していたので、エラーメッセージのユニットテスト段階でそれに気づいた。
順序が変わったとて使用に問題はないけど、テストの都度エラーメッセージの順序が変わるのは気持ちが悪いしユニットテストで検査のしようがないから、 `Seq.toList` して順序を固定化するようにしている。

```diff
@@ -81,25 +81,34 @@ module PocofAction =

     let convertKeymaps (h: Hashtable) =
         match h with
-        | null -> defaultKeymap
+        | null -> defaultKeymap |> Ok
         | x ->
-            let source = defaultKeymap |> Map.toSeq
-
-            let custom =
+            let ok, ng =
                 x
                 |> Seq.cast<DictionaryEntry>
-                |> Seq.map (fun e ->
+                |> Seq.toList
+                |> List.map (fun e ->
                     let k = string e.Key |> toKeyPattern
                     let v = string e.Value |> PocofData.Action.fromString

                     match (k, v) with
-                    | (Ok kv, Ok av) -> (kv, av)
-                    // TODO: enhance error handling.
-                    | (Error e1, Error e2) -> failwith <| e1 + e2
+                    | (Ok kv, Ok av) -> Ok(kv, av)
+                    | (Error e1, Error e2) -> e1 + e2 |> Error
                     | (Error e, _)
-                    | (_, Error e) -> failwith e)
+                    | (_, Error e) -> Error e)
+                |> List.fold
+                    (fun (fst, snd) o ->
+                        match o with
+                        | Ok (o) -> (o :: fst, snd)
+                        | Error e -> (fst, e :: snd))
+                    ([], [])
+
+            match ok, ng with
+            | c, [] ->
+                let source = defaultKeymap |> Map.toList
+                List.append source c |> Map.ofSeq |> Ok
+            | _, e -> e |> List.rev |> String.concat "\n" |> Error

-            Seq.append source custom |> Map.ofSeq
```

パフォ的にキーマップの入力が尋常に多いエントリ数を持つことも無いだろうし、 `List` で問題ないかな。

---

キー入力に合わせたクエリの差分更新は結構大改修になってる。
module の分割と関数シグネチャの変更、あと既存のユニットテストの大幅な書き換えが必要になってる。 `QueryContext` という型を作って、そこに元々 `PocofQuery.run` が内包していた関数などをパーツとして持たせておく。
それを `PocofAction` から切り出した `PocofHandle.invokeAction` の各入力キーに合わせた処理中で差分更新する。 `PocofQuery.run` は渡ってきた `QueryContext` を組み立てて実行するだけにした。
これで例えばカーソル移動の場合ならクエリ(`Queries`)を初めとした大部分の再構築が不要なので、処理の軽量化になる。

他の箇所は差分が多くて貼りきれない。あとから詳細を見るときは [#98](https://github.com/krymtkts/pocof/pull/98/files) を参照するものとして、ここでは部分的にコードを抜粋しておく。
レコードはこんな感じ。

```fsharp
    type TesterType<'a> = ('a -> bool) -> list<'a> -> bool

    type QueryContext =
        { Queries: Query list
          Test: TesterType<string * string>
          Is: string -> string -> bool
          Answer: bool -> bool
          Notification: string }
```

処理的には良くなってるのだけど、ユニットテストは `QueryContext` レコードに関数を持ってる関係でテストしにくい点があって、一部ザルになってる。
before/after でレコードが持つ関数の実行結果を比較したらいいけど、これまただるいな...という感じ。そこを頑張るよりも `PocofQuery.run` から個々に分解した関数自体のユニットテストをまだ書いてないから、そこで頑張る方がいいか。

あと ↑ に貼って思ったけど、 `QueryContext.Notification` て此処にあるべきではないな。これは `InternalState` に直に書き込んでやったら良さそう。早速直さな。

この対応によって、 `PocofHandle.invokeAction` の戻り値が 3-tuple から 4-tuple になって、 `loop` 関数の呼び出しがゴツくなってしまった。

```diff
@@ -52,8 +53,10 @@ module Pocof =
         |> function
             | Cancel -> []
             | Finish -> unwrap l
-            | Noop -> loop args l s pos NotRequired
-            | a -> invokeAction s pos args.props a |||> loop args l
+            | Noop -> loop args l s pos context NotRequired
+            | a ->
+                invokeAction s pos context a
+                |> fun (a, b, c, d) -> loop args l a b c d
```

これは流石にカッコ悪いので、 4-tuple 用のパイプライン演算子を定義してもいいのだけど...

```fsharp
let inline (||||>) (arg1, arg2, arg3, arg4) func = func arg1 arg2 arg3 arg4
```

素直に戻り値の構成を見直すのが良さそうと思ってる。
独自に演算子を作るよりは、元々 `InternalState` にいても良さそうな `refresh` を `loop` の引数から削って `InternalState` に移動する感じ。

```diff
     let rec loop
         (args: LoopFixedArguments)
         (results: Entry list)
         (state: InternalState)
         (pos: Position)
         (context: QueryContext)
-        (refresh: Refresh)
         =
```

```diff
     type InternalState =
         { Query: string
           QueryState: QueryState
           PropertySearch: PropertySearch
           Notification: string
           SuppressProperties: bool
-          Properties: string list }
+          Properties: string list
+          Refresh: Refresh }
```

どうかな。

---

この他にもクエリに利用可能なプロパティ一覧を `InternalState` に移動して `invokeAction` の引数をスリムにするとかやってる。
なるべく年内納得いく形にまとめて version 0.7 出したいのだけど、アレもコレもとなったら手に負えられないのでどっかで区切りをつけてリリース作業しよう。
