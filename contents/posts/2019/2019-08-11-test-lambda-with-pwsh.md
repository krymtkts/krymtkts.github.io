---
title: "AWS LambdaをPowershellでテスト実行する"
tags:  ["powershell", "aws", "lambda"]
---

へーしゃの支援によりGoogle Cloud Next Tokyo 2019へ参加できた。

とても有意義な時間を過ごせたのでブログをしたためたかったのだけど、日が経つにつれ仕事もメモもとっ散らかってしまい清書もできず、現時点でのブログへのまとめは断念中😫

代わりに小ネタを投稿する。

### curlの代わりのPowershell

最近仕事でAWS Lambdaを使う機会を得た。へーしゃはAWSもGCPもどっちでもアリだが、現状はAWS優勢。わたしはGCPがいいけど。
元々最初にCloud WatchとAWS Lambdaで作った方が良いのでは？という提案をしていたのだが、大した理由もなく却下。その後追加された機能がLambdaじゃないと実行環境を準備するのが難しいというのが判明して、棚からぼた餅なチャンス到来。

本題に入ろう。今回作ったLambdaはAPI Gatewayと組み合わせてWeb APIとして利用する。

そのためAPI Key(`x-api-key`)をRequest Headerに付与してリクエストされる想定でいる(現時点で)。

なのでブラウザなんかでテスト実行出来ないので、大抵のやつはcurlなんかでURLを叩く。しかしここはPowershellでやってみよう。

Web-APIを叩くのなら`Invoke-WebRequest`がよかろう。追加のヘッダーは`-Headers`にhashtableとして渡せるのだ。

```powershell
Invoke-WebRequest -Uri https://omae-no-api-endpoint/helloworld -Headers @{'x-api-key' = 'omae-no-api-key!!!'}
```

こうなる。そんだけ😉
