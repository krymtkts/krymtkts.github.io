---
title: "F# の AWS CDK で EventPattern の記述を無理やり通す"
tags: ["fsharp","aws"]
---

[前](/posts/2022-07-03-aws-cdk-with-fsharp.html)に書いた、 F# の AWS CDK で EventRule のパターンが正しいのにエラーとなるやつ。

こういうのをしたいところ、

```yaml
EventPattern:
  source:
    - aws.s3
  detail-type:
    - Object Created
  detail:
    bucket:
      name:
        - Ref: バケツ
    object:
      key:
        - prefix: test/
```

以下の記述だとエラーになる。

```fsharp
EventPattern =
    EventPattern(
        Source = [| "aws.s3" |],
        DetailType = [| "Object Created" |],
        Detail =
            dict [ ("bucket", dict [ ("name", [| bucket.BucketName |]) ])
                    ("object", dict [ ("key", [| dict [ ("prefix", "test/") ] |]) ]) ]
    )
```

```plaintext
Unhandled exception. System.ArgumentException: Could not infer JSII type for .NET type 'IDictionary`2' (Parameter 'type')
```

この Issue っぽい。
[DotNet: Unable to pass interface instance through in a Dictionary<string, object> · Issue #1044 · aws/jsii](https://github.com/aws/jsii/issues/1044)

けど、 `IDictionary<string,obj>` は使えてる部分もあって微妙に違うしなー。
より詳細な型がここ(`EventPattern`)にはないし、どないすりゃいいんじゃ。

色々とこねくり回した末に、 CFn のリソースを直接上書きすることで回避できるのがわかった。
[Escape hatches - AWS Cloud Development Kit (CDK) v2](https://docs.aws.amazon.com/cdk/v2/guide/cfn_layer.html#cfn_layer_raw)

[krymtkts/aws-cdk-fsharp-trial](https://github.com/krymtkts/aws-cdk-fsharp-trial) からコードを抜粋する。

配列内に `IDictionary<string,obj>` がいると型が解決できなようだったので、空の配列で定義しておき、後でから `IDictionary<string,obj>` 要素を差し込む！野蛮過ぎる...

```fsharp
    let rule =
        Rule(
            this,
            "bucket-event",
            RuleProps(
                RuleName = "buckt-event",
                Description = "bucket event.",
                EventPattern =
                    EventPattern(
                        Source = [| "aws.s3" |],
                        DetailType = [| "Object Created" |],
                        // NOTE: cannot write like below because JSII is unable to use `IDictionary<string, object>` inside the array.
                        // Detail =
                        //     dict [ ("bucket", dict [ ("name", [| bucket.BucketName |]) ])
                        //            ("object", dict [ ("key", [| dict [ ("prefix", "test/") ] |]) ]) ]
                        Detail =
                            dict [ ("bucket", dict [ ("name", [| bucket.BucketName |]) ])
                                   ("object", dict [ ("key", [||]) ]) ]
                    )
            )
        )

    // NOTE: the escape hatch for IDictionary<string, object>` inside the array is raw overrides.
    do
        match rule.Node.DefaultChild with
        | :? CfnRule as ep -> ep.AddPropertyOverride("EventPattern.detail.object.key.0", dict [ "prefix", "test/" ])
        | _ -> failwith "You passed a wrong variable that is not of type CfnRule!"

```

`cdk synth` でエラーせずに、以下の出力が得られるようになった。

```yaml
bucketeventF2FCD38A:
  Type: AWS::Events::Rule
  Properties:
    Description: bucket event.
    EventPattern:
      detail:
        bucket:
          name:
            - Ref: sourcebucketE323AAE3
        object:
          key:
            - prefix: test/
      detail-type:
        - Object Created
      source:
        - aws.s3
    Name: buckt-event
    State: ENABLED
    Targets:
      - Arn:
          Fn::GetAtt:
            - samplefunctionAA39FD5B
            - Arn
        Id: Target0
  Metadata:
    aws:cdk:path: AwsCdkFsharpStack/bucket-event/Resource
```

宣言的に書きたいのにこんなツギハギが要るとか、めちゃくちゃ不便極まりない。

これは F# のせいじゃないけど、この .NET の不便な側面を許容してまで F# で CDK したいかというと、まずないな...と思った。これに関連して AWS CDK の Issue をちょいちょい調べたが、 .NET や Java で CDK するの辛そうやな..とうっすら思えた。茨の道を突き進むなりの良さがあるのだろうか。

改めて、仕事は素直に TypeScript を採用して良かったと実感した。
