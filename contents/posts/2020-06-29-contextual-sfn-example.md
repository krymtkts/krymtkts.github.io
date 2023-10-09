{:title "Step Functions のステートをまたいでパラメータを伝播するパターン"
:layout :post
:tags ["serverless", "aws", "stepfunctions"]}

[前の記事](2020-04-30-context-like-sfn)で記したアイデアの実装例を残しておいた。

[krymtkts/contextual-sfn: Example for passing parameters across states.](https://github.com/krymtkts/contextual-sfn)

現時点で実際にお仕事で使っているパターンなのだけど、前述の通りペイロードが大幅に大きくなると問題になり得るので、より良くするすべはないものかと考え中。

### おさらい

StateMachine の構成。

1. Task
2. Map
3. Reduce

のステートがあるとする。Task は文字列、配列 A、配列 B を Output する。これらのデータについては以下の通りとする。

- 文字列(`t`)は、フロー全体に渡って使用したいデータ。
- 配列 A(`ia`) は、1 つ目の Map タスクで分散したいデータ。
- 配列 B(`sa`) は、2 つ目の Map タスクで分散したいデータ。

このパターンを実装した背景としては以下の通り。

- Map と Reduce で使いたいデータを Aurora Serverless から取得する必要がある
- 普段停止しているクラスタは結構起動に時間がかかる
- Map では Reduce のパラメータも利用したい
- フローの最初に全部取ってしまおう！

#### 1. Task

Lambda からの出力がこんなのだとする。

```json
{
  "t": "nanigashi",
  "ia": [1, 2, 4, 8, 16, 32, 64],
  "sa": ["A", "B", "C", "D", "E"]
}
```

`ResultPath`を`$.data`にしておくと Task1 ステートの出力は ↓ になる。

```json
{
  "data": {
    "t": "nanigashi",
    "ia": [1, 2, 4, 8, 16, 32, 64],
    "sa": ["A", "B", "C", "D", "E"]
  }
}
```

YAML はこう。

```yml
Entrypoint:
  Type: Task
  Resource:
    Fn::GetAtt: [task, Arn]
  ResultPath: $.data
  Next: Map
```

#### 2. Map

- `ItemsPath` に `$.data.ia` を指定し、配列 A でイテレーションする
- `Parameters` に Lambda へ渡したいパラメータを指定する。以下の通り
  - マッピングの各要素は`$$.Map.Item.Value`
  - 追加で渡したいパラメータを `$.data.t`, `$.data.sa`
  - パラメータ名末尾の`.$`忘れがち
- `OutputPath` に前のタスクの出力 `$.data` を指定すれば、同じパラメータを次のステートに回せる
- ここではマッピングの出力は無視するとして、`ResultPath`に`$.null`など`OutputPath`に含まれないパスを指定する

```yml
Map:
  Type: Map
  MaxConcurrency: 10
  Parameters:
    k.$: $$.Map.Item.Value
    t.$: $.data.t
    a.$: $.data.sa
  ItemsPath: $.data.ia
  ResultPath: $.null
  OutputPath: $.data
  Iterator:
    StartAt: MapTask
    States:
      MapTask:
        Type: Task
        Resource: !GetAtt [map, Arn]
        End: true
  Next: Reduce
```

#### 3. Reduce

- `ItemsPath` に `$.sa` を指定し、配列 B でイテレーションする
- `Parameters` に Lambda へ渡したいパラメータを指定する。以下の通り
  - マッピングの各要素は`$$.Map.Item.Value`
  - 追加で渡したいパラメータを `$.data.t`
  - パラメータ名末尾の`.$`忘れがち(2 回目)
- 出力を制御したい場合は、Map 同様に`OutputPath`、`ResultPath`を指定仕分ける

```yml
Reduce:
  Type: Map
  Parameters:
    k.$: $$.Map.Item.Value
    t.$: $.t
  ItemsPath: $.sa
  ResultPath: $.null
  OutputPath: $.t
  Iterator:
    StartAt: ReduceTask
    States:
      ReduceTask:
        Type: Task
        Resource: !GetAtt [reduce, Arn]
        End: true
  End: true
```

### まとめ

こういう例、ググっても見つからずあまり使われないパターンかも知れない。
必要だった＆実現できたので制限を理解した上で、容量用法守って使えれば良いかな。
