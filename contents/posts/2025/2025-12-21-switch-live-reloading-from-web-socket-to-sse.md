---
title: blog の Live reloading を WebSocket から SSE に切り替えた
tags: [dotnet, fsharp, fable]
---

先日 [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) の dev server を [Suave](https://github.com/SuaveIO/suave) [3.2.0](https://www.nuget.org/packages/Suave/3.2.0) に更新した。
その流れで dev server を refactor した。 [#424](https://github.com/krymtkts/blog-fable/pull/424)

blog-fable では元々ファイル変更時に client へ event 通知する、要は live reloading するのに WebSocket で通知してた。
でも相互通信が必要な訳ではなく、単に server -> client の単方向通知だけが目的だと過剰なので、 [Server-Sent Events](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events/Using_server-sent_events) にしようと考えた。
幸い Suave には [`EventSource` module](https://github.com/SuaveIO/suave/blob/a856edacfa9209d0ffb2078932169a7b8e084f18/src/Suave/Combinators.fs#L770-L866) があって、それを使うことで簡単に実装できる。
`EventSource` module について何処かにあるのかも知れないが document は見当たらなかった。
Sample code には多少出現してそうなのだけど、そこからはあまり使い方がわからなかった。

GitHub Copilot(GPT-5.2) に方針を示したらサクッと実装してくれた。
余談だが [Fable の binding](https://fable.io/docs/javascript/features.html#utilities) をサラッと書いてくれた辺りは中々やるなと思った。
当然多少の手直しが必要なのは変わらないけど。

再帰を使ってた部分も単純な while loop に直した。
コードが圧縮されて良いかなと。
[`CancellationToken`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtoken?view=net-10.0) は Suave の config の方に移ったのでここでの出番はなくなってる。

```diff
-    let socketHandler (ws: WebSocket) _ =
-        let rec refreshLoop (ct: CancellationToken) =
-            task {
-                ct.ThrowIfCancellationRequested()
-                do! refreshEvent.Publish |> Async.AwaitEvent
-
-                printfn "refresh client."
-                let seg = ASCII.bytes "refreshed" |> ByteSegment
-                let! _ = ws.send Text seg true
-
-                return! refreshLoop ct
-            }
-
-        let rec mainLoop (cts: CancellationTokenSource) =
-            socket {
-                let! msg = ws.read ()
-
-                match msg with
-                | Close, _, _ ->
-                    // use _ = cts
-                    cts.Cancel()
-
-                    let emptyResponse = [||] |> ByteSegment
-                    do! ws.send Close emptyResponse true
-                    printfn "WebSocket connection closed gracefully."
-                | _ -> return! mainLoop cts
-            }
-
-        let cts = new CancellationTokenSource()
-        refreshLoop cts.Token |> ignore
-        mainLoop cts
-
-    handleWatcherEvents, socketHandler
+    let sseHandler (conn: Connection) : Task<unit> =
+        task {
+            try
+                // NOTE: Tell the browser how long to wait before retrying the connection.
+                do! EventSource.retry conn 1000u
+
+                // NOTE: Initial event so the client can confirm it is connected.
+                do! EventSource.eventType conn "connected"
+                do! EventSource.data conn "ok"
+                do! EventSource.dispatch conn
+
+                while true do
+                    do!
+                        Async.StartAsTask(
+                            refreshEvent.Publish |> Async.AwaitEvent,
+                            cancellationToken = CancellationToken.None
+                        )
+
+                    do! EventSource.eventType conn "refresh"
+                    do! EventSource.data conn "refreshed"
+                    do! EventSource.dispatch conn
+            with
+            | :? OperationCanceledException -> ()
+            | :? SocketException -> ()
+        }
+
+    handleWatcherEvents, sseHandler
```

この関数を `EventSource.handShake` に渡すだけ。便利だ。
Suave 3 系になって `handShake` 系の関数は `Task` を要求する signature に変わってる。昔は `Async` だったなとしみじみと感じる。

```diff
     choose [

-        path "/websocket" >=> handShake socketHandler
+        path "/sse" >=> EventSource.handShake sseHandler

         GET
         >=> Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
```

client 側は [`EventSource`](https://developer.mozilla.org/en-US/docs/Web/API/EventSource) を使う。
ここで Fable の binding が活きる。
元々使っていた [`Fable.Browser.WebSocket`](https://www.nuget.org/packages/Fable.Browser.WebSocket) の代わりに [`Fable.Browser.EventSource`](https://www.nuget.org/packages/Fable.Browser.EventSource/) を使うというの手もあったが、使わなかった。
`Fable.Browser.EventSource` も小さい module だし利用箇所はもっと小さいので、自前の方が依存関係を減らせて何かと管理が楽だ。

```diff
 module Dev

 open Browser.Dom
-open Browser.WebSocket
+open Fable.Core

-let private initLiveReloading _ =
-    // NOTE: don't use string interpolation here, it will break the code because of importing String module.
-    let ws = WebSocket.Create <| "ws://" + window.location.host + "/websocket"
+[<Emit("typeof EventSource !== 'undefined'")>]
+let private hasEventSource: bool = jsNative
+
+[<AllowNullLiteral>]
+type private IEventSource =
+    abstract addEventListener: string * (obj -> unit) -> unit
+    abstract close: unit -> unit
+    abstract onmessage: (obj -> unit) with get, set
+
+[<Emit("new EventSource($0)")>]
+let private createEventSource (_url: string) : IEventSource = jsNative
+
+let private initLiveReloadingViaSse () =
+    let es = createEventSource "/sse"

-    ws.onmessage <-
-        fun _ ->
-            ws.close (1000, "reload")
-            window.location.reload ()
+    let reload (_: obj) =
+        es.close ()
+        window.location.reload ()

-    window.addEventListener ("beforeunload", (fun _ -> ws.close ()))
+    es.addEventListener ("refresh", reload)
+    es.onmessage <- reload
+
+    window.addEventListener ("beforeunload", (fun _ -> es.close ()))
+
+let private initLiveReloading _ =
+    // SSE only.
+    // If the browser doesn't support EventSource, do nothing.
+    if hasEventSource then
+        initLiveReloadingViaSse ()

 window.addEventListener ("load", initLiveReloading)
```

以上備忘のメモ。

今回のような普段書き直さないような部分の書き直しは新たな再発見があっていいな。
あと log はまだ来てないようだし、 Suave 3 の更新がある限りはちまちまと触る機会になりそうで、良い。
