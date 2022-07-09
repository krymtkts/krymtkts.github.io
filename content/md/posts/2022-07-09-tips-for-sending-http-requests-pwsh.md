{:title "PowerShell で大量の HTTP リクエストを送る場合の Tips"
:layout :post
:tags ["powershell"]}

大量のアクセスログがほしい事情で、テスト環境に大量のリクエストを送りつける必要があった。

JMeter とか Gatling 使えば済む話なのだけど、すぐに使い回せるものがなかったし、パパっとやってしまいたかったので PowerShell を使ってみたら、エラーになった。

```powershell
$requests = 'aaa=10&bbb=20', 'aaa=11&bbb=21', 'aaa=12&bbb=22'
$requests * 100000 | ForEach-Object -Parallel {
    Invoke-WebRequest -Method Get -Uri "https://$using:testDomaain?$_"
    $jitter = Get-Random -Minimum 3 -Maximum 23
    Start-Sleep -Milliseconds (95 + $jitter) # 待ち
} -ThrottleLimit 100 | ConvertTo-Json | Set-Content ./responses.json
```

こういうのでタコ殴りにすると...

`Only one usage of each socket address (protocol/network address/port) is normally permitted`

15000 件を超えた辺りでこうなった。なんだ？ググる。

- [System.Net.Sockets.SocketException: Only one usage of each socket address (protocol/network address/port) is normally permitted](https://social.msdn.microsoft.com/Forums/aspnet/en-US/ab5e4f6d-e96a-4bef-bba2-870eda412ea3/systemnetsocketssocketexception-only-one-usage-of-each-socket-address-protocolnetwork?forum=AzureFunctions)

ポートが枯渇するんだと。 PowerShell の実装を見た訳じゃないけど、 大量に実行された `Invoke-WebRequest` で `HttpClient` を大量作成したのであろうことは、想像するに易い。

同じ `HttpClient` を使い回せば良いらしいので、そうした。

```powershell
$requests = 'aaa=10&bbb=20', 'aaa=11&bbb=21', 'aaa=12&bbb=22'
$h = New-Object System.Net.Http.HttpClient
$requests * 100000 | ForEach-Object -Parallel {
    ($using:h).GetAsync("https://$using:testDomaain?$_").GetAwaiter()
    $jitter = Get-Random -Minimum 3 -Maximum 23
    Start-Sleep -Milliseconds (95 + $jitter) # 待ち
} -ThrottleLimit 100 | ForEach-Object {$_.GetResult()} | ConvertTo-Json | Set-Content ./responses.json
```

そんだけ。

- 参照
  - [Need Official Guidance On using HttpClient in Functions · Issue #1806 · Azure/azure-functions-host](https://github.com/Azure/azure-functions-host/issues/1806)
  - [Calling Async .NET Methods from PowerShell](https://blog.ironmansoftware.com/powershell-async-method/)
