---
title: "Serverless FrameworkでAWS LambdaとCloudFront"
tags:  ["aws", "lambda", "serverless"]
---

AWS Lambdaって書くのはホント簡単。

でも手動でデプロイするのはほんまに勘弁してほしいわ...って感じだったのでAWS強メンの同僚に相談してみたところ、[Serverless Framework](https://serverless.com/framework/)ってのがいい具合に抽象化してくれてるので試しに使ってみては？と助言いただけた。

### 使ってみた

事前にNodeが必要なことくらい。

```powershell
# global install
$ npm install -g serverless

# installed version
$ serverless --version
Framework Core: 1.51.0
Plugin: 1.3.10
SDK: 2.1.0

# generate boilerplate
$ serverless create --template aws-python3
Serverless: Generating boilerplate...
 _______                             __
|   _   .-----.----.--.--.-----.----|  .-----.-----.-----.
|   |___|  -__|   _|  |  |  -__|   _|  |  -__|__ --|__ --|
|____   |_____|__|  \___/|_____|__| |__|_____|_____|_____|
|   |   |             The Serverless Application Framework
|       |                           serverless.com, v1.51.0
 -------'

Serverless: Successfully generated boilerplate for template: "aws-python3"
Serverless: NOTE: Please update the "service" property in serverless.yml with your service name
```

これで生成された関数に処理を書くだけ。あ～らかんたん😁

デプロイも超簡単なので、これならCDに組み込むのも楽そう。

```powershell
# deploy
serverless deploy -v

# deploy to specific stage
serverless deploy -v --stage dev
```

### CloudFrontを添えて

今回の仕事ではちょっと特殊な事情でCloudFrontを経由してLambdaのエンドポイントURLへリクエストする必要があった。

プラグインを使えばかんたんに記述することもできる。

[Droplr/serverless-api-cloudfront: Serverless Plugin - CloudFront distribution in front of your API Gateway](https://github.com/Droplr/serverless-api-cloudfront)

が、かんたんに記述できる＝かんたんな内容しかいじれない、のため片手落ちな点が多かった。諸々のパラメータの指定ができなくて細かな指定をする場合は結局`resources`セクションに自力でCloudFormationを書くことになった。

以下はresourceセクションのサンプル。`DomainName`の解決は以下のStackoverflowからヒントを得た。

[amazon web services - Deploying Lambda + API-gateway + CloudFront through serverless framework at a time - Stack Overflow](https://stackoverflow.com/questions/50931730/deploying-lambda-api-gateway-cloudfront-through-serverless-framework-at-a-ti)

```yaml
resources:
  Resources:
    LambdaDistribution:
      Type: AWS::CloudFront::Distribution
      Properties:
        DistributionConfig:
          Enabled: true
          Comment: "managed by serverless framewrok"
          HttpVersion: http2
          PriceClass: PriceClass_All
          Origins:
            - Id: ApiGateway
              DomainName: !Join
                - "."
                - - !Ref ApiGatewayRestApi
                  - execute-api
                  - !Ref AWS::Region
                  - amazonaws.com
              OriginPath: /
              CustomOriginConfig:
                HTTPPort: 80
                HTTPSPort: 443
                OriginProtocolPolicy: https-only
                OriginReadTimeout: 10
                OriginSSLProtocols:
                  - "SSLv3"
                  - "TLSv1"
                  - "TLSv1.1"
                  - "TLSv1.2"
          DefaultCacheBehavior:
            AllowedMethods:
              - HEAD
              - DELETE
              - POST
              - GET
              - OPTIONS
              - PUT
              - PATCH
            Compress: false
            DefaultTTL: 0
            MaxTTL: 0
            MinTTL: 0
            ForwardedValues:
              Cookies:
                Forward: none
              Headers:
                - x-api-key
              QueryString: false
            TargetOriginId: ApiGateway
            ViewerProtocolPolicy: redirect-to-https
```

ちなみにLambda@Edgeにはまだ対応していないみたいでプラグインの利用が必須となっている様子。

[Support for Lambda@Edge · Issue #3944 · serverless/serverless](https://github.com/serverless/serverless/issues/3944)

### 感想

AWSだけにベンダーロックインせずServeless Computingの開発ができるよーな抽象化層を提供するようなCLIのイメージ。

誕生は数年前でまだまだ新し目のため、web上には新旧の情報が玉石混交の状態であるからして、細かなYAMLの記述内容の確認なんかは[Serverless Framework Documentation](https://serverless.com/framework/docs/)を参考にし、大まかな書き方はそのへんのブログなどから引っこ抜いてくるのが良いと思われる。

まだ1プロダクトでしか使ってないけど、いい感触を得た。YAMLの記述内容は、AWSの場合でいうとresourcesはまんまCloudFormationなので、そのへんの知識があれば使いこなせそう。SaaSのやつはまだ使ってないので感触なし。

### おまけ: `sls`というコマンド名

ばかみたいな話なんやけど、Serverless Frameworkの短縮形コマンド`sls`はPowerShellで言うところの`Select-String`コマンドレットにエイリアスされてるので使えねえｗ

ではアデュー😘
