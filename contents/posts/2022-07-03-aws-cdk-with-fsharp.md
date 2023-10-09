---
title: "F# で AWS CDK して躓いてる"
tags: ["fsharp","aws"]
---

最近仕事で AWS CDK を使ってみたが、非常に良い感触だった。
仕事では TypeScript を採用したが、テンプレートが提供されている言語を見るとなんと F# がいるではないか。

```powershell
PS> cdk init --list
Available templates:
* app: Template for a CDK Application
   └─ cdk init app --language=[csharp|fsharp|go|java|javascript|python|typescript]
* lib: Template for a CDK Construct Library
   └─ cdk init lib --language=typescript
* sample-app: Example CDK Application with some constructs
   └─ cdk init sample-app --language=[csharp|fsharp|go|java|javascript|python|typescript]
```

単に「C# のクラス用意したから継承して使ってや～」的なもので F# 向けに調整されてないだろうが、コレ誰が使うねんという気がしたので、試してみた。

参考にした前例たち。非常に参考にさせてもらった。

- [Functional Programming with AWS CDK | AWS Maniac](https://awsmaniac.com/functional-programming-with-aws-cdk/)
- [Part 5 - AWS Cloud Development Kit (CDK) — Cloudgnosis collection 0.1 documentation](https://cloudgnosis.org/fsharp/fsharp-for-the-cloud-worker/part5.html)

---

まずテンプレからプロジェクトを生成した素の状態で CFn を作成してみる。

```powershell
cdk init sample-app --language=fsharp
cdk synth
```

ちゃんと CFn をビルドできた。 どんなコードが出るか知りたかったので `sample-app` を使ったが、ここで `app` テンプレを使うと、利用されない `this` が残ってるので警告が出る。でも勿論 CFn をビルドできる。

記述に関しては TypeScript など他の言語と大差なさそう。ただ予想通り C# との相互運用は前提になってる。
宣言的に記述する箇所については `let` バインディングして、 リソースに手続き的に何かすると `do` バインディングみたい。

試しに S3 と Lambda を EventBridge で繋いでみたい。

F# の CDK では `Amazon.CDK` に必要な Construct が全部入りしてるみたい。なので `Amazon.CDK.xxx` みたいな感じでポチポチ `.` を刻んでいけば、ドキュメントがなくてもそれなりに探せる。

F# は言語固有モジュールが無いので、ここに例えば Lambda を足すとすると、個別にビルドする感じになる。あれめちゃくちゃ楽で好きなんやけど、 .Net 向けにはない。
プロジェクトを別途足す方向で作るらしい。

```powershell
# テンプレなかったので探してインストール
dotnet new lambda.EmptyFunction --search
dotnet new --install Amazon.Lambda.Templates

dotnet new lambda.S3 --output . --name api --language "F#"

cd src
dotnet sln add api
```

---

stack をザーッと書いてみたが、めちゃくちゃつまづきどころがある。

- `open System.Collections` したら `Stack` が競合するので注意
- `MemorySize`に`128` を渡してたら Ionide や `dotnet build` ではエラー検知できなかったが、 `cdk synth` でエラー(めんどくせ)
  - `MemorySize`を`128`から`(Some 128.0 |> Option.toNullable)`に
- 必須プロパティがビルドまでわからないのつらい
- `EventPattern` の `IDictionary<string,obj>`を一息で書くのかなりキツイ(まじでめんどい)
  - `Value` の型が揃わないところは個別に `let` してあとで組み合わせたり
  - `dict` に食わせる `tuple` はきっちりカッコで囲むと先述の `let` が不要になった
- `EventPattern` の `Detail` が `cdk synth` でエラーになる

Prefix のパターンをやりたいのだけど、 [Content filtering in Amazon EventBridge event patterns - Amazon EventBridge](https://docs.aws.amazon.com/eventbridge/latest/userguide/eb-event-patterns-content-based-filtering.html#eb-filtering-prefix-matching)

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

`dict [ ("prefix", "test/") ]` の代わりに文字列を渡してると問題なくなるけど、それってスキーマ違反なんですけど。
この辺 TypeScript は困った記憶ないので、参ったなという感じ。

この辺の回答が出たら割りと参考になるかもなーと思って見ている。[How to set detail on an eventpattern in java · Discussion #20894 · aws/aws-cdk](https://github.com/aws/aws-cdk/discussions/20894)
単純に F# 力の低さに起因して記述できないだけだといいけど。

もうちょっと深掘したいが今週はここまで。
とりあえず repo は作った。 [krymtkts/aws-cdk-fsharp-trial](https://github.com/krymtkts/aws-cdk-fsharp-trial)
