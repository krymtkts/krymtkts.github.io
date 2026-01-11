---
title: Suave 3 のアクセスログ出力を Serilog でやる
tags: [dotnet, fsharp]
---

[以前](/posts/2025-12-14-suave-3.html) [Suave](https://github.com/SuaveIO/suave) 3 へ更新した際に `Suave.Logging` module がなくなった件に触れた。

> あと今のところは `Logging` module がなくなって `logger` が使えなくなってた。
> `Logging` がなくなったのは読み込まれたファイルのログが出なくなって dev server 的には地味に痛いのだが、ひょっとしたら別の方法で実現できたり復活の可能性もあるので、経過観測する。

それについて discussion に書かれてるのを見つけた。

[v3.1.0 · SuaveIO/suave · Discussion #793](https://github.com/SuaveIO/suave/discussions/793#discussioncomment-15252133)

> Everything should work pretty much the same way, the API hasn't changed significantly. For your logging needs you should use an alternative library like Serilog (https://serilog.net/).

標準の logger を積まなくなったので [Serilog](https://serilog.net/) のような logging library を使えばいいらしい。
.NET の logging 事情は知らないので他に何があるか軽く調べた。
[Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line) は標準に寄せるなら強いかなと思ったが、結果的に今は簡単さ重視で Serilog にした。
Serilog でも構造夏ログを使わないのであれば overkill な気がするけど広く使われていて利用も楽なので。
といいつつ今後 Microsoft.Extensions.Logging に乗り換える可能性はある。

また、 Suave 2 の時と同じような logging をどう実装するかよくわからなかったので GitHub Copilot サンに聞いた。
曰く、 simple にやるなら単に fish operator `>=>` で扱える `WebPart` を受けて返す関数を作ればいいらしい。

```fsharp
module Logging =
    open Serilog

    let logger =
        LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger()

    let log (logger: ILogger) : WebPart =
        fun ctx ->
            async {
                logger.Information(
                    "HTTP {Method} {Path} -> {Status} {Reason}",
                    ctx.request.``method``.ToString(),
                    ctx.request.rawPath,
                    ctx.response.status.code,
                    ctx.response.status.reason
                )

                return Some ctx
            }
```

このような薄い module を拵えた。
依存関係には [Serilog](https://www.nuget.org/packages/Serilog/) と console 出力の実装である [Serilog.Sinks.Console](https://www.nuget.org/packages/serilog.sinks.console) が要る。
これで以下のように Suave 2 の頃と同じ様な使い方ができる。 Suave 2 利用時と比較するとこんな感じ。

```diff
-    let logger = Logging.Log.create "dev-server" // Suave 2 はこう

     choose [

-        path "/websocket" >=> handShake socketHandler
+        path "/sse" >=> EventSource.handShake sseHandler >=> Logging.log Logging.logger

         GET
         >=> Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
         >=> Writers.setHeader "Pragma" "no-cache"
         >=> Writers.setHeader "Expires" "0"
         >=> choose [

             path $"/{root}/" >=> Files.browseFileHome $"{root}/index.html"
             path $"/{root}" >=> Redirection.redirect $"/{root}/"

             Files.browseHome

         ]
-        >=> log logger logFormat // Suave 2 はこう
+        >=> Logging.log Logging.logger

         Writers.setStatus HTTP_404
-        >=> logWithLevel Logging.Error logger logFormat // Suave 2 はこう
         >=> choose [
             Files.browseFileHome $"{root}/404.html"
             RequestErrors.NOT_FOUND "404 - Not Found" // NOTE: Fallback 404 page.
         ]
+        >=> Logging.log Logging.logger // TODO: ログレベルを指定できるようにする

     ]
```

この日記を書いてる最中にログレベル指定の対応忘れてるなと気付いたので後でやる。
こんな感じのログ出力になる。

```plaintext
[2026-01-11 12:30:30.091 INF] HTTP GET /index.html -> 200 OK
[2026-01-11 12:30:30.189 INF] HTTP GET /pagefind/pagefind-ui.css -> 200 OK
[2026-01-11 12:30:30.198 INF] HTTP GET /css/style.css -> 200 OK
[2026-01-11 12:30:30.211 INF] HTTP GET /css/solarized-dark.min.css -> 200 OK
[2026-01-11 12:30:30.231 INF] HTTP GET /pagefind/pagefind-ui.js -> 200 OK
[2026-01-11 12:30:30.257 INF] HTTP GET /js/dev.js -> 200 OK
[2026-01-11 12:30:30.274 INF] HTTP GET /js/handler.js -> 200 OK
[2026-01-11 12:30:30.741 INF] HTTP GET /sse -> 200 OK
```

これでひとまず dev server のアクセスログも復活できて Suave 2 の頃と同様な使用感になった。
