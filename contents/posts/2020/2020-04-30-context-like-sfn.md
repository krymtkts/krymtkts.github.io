---
title: "Step Functions のステートをまたいでパラメータを伝播する"
tags: ["serverless", "aws"]
---

仕事でシンプルなバッチを組む必要があり、ちょうどワークフローみたいな感じだったので Step Functions で Lambda をつないで作っている。

3 つ Lambda が登場するのだけど、1 つ目の Lambda の Output を 2 つ目 3 つ目で使いたい。
でも、こいつらが Map ステートなのもあり Output でつなぐのはちょっと違う。
代わりに `ResultPath`, `OutputPath`, `ItemPath`, `Parameters` の組み合わせれば、Lambda の Output にない後続のステートにつなげるのがわかったのでメモしておく。

### 参考資料

- [InputPath、ResultPath、および OutputPath 例 - AWS Step Functions](https://docs.aws.amazon.com/ja_jp/step-functions/latest/dg/input-output-example.html)
  - これは読んでもナンノコッチャよくわからんかった
- [Step Functions の入出力処理の制御パラメータ（InputPath、 Parameters、ResultPath および OutputPath）を理解するために参照したドキュメント | Developers.IO](https://dev.classmethod.jp/articles/step-functions-parameters/)
  - 流石のクラスメソッドさん、わかりやすかった

### 構成

1. Task
2. Map
3. Map

のステートがあるとする。Task は文字列、配列 A、配列 B を Output する。これらのデータについては以下の通りとする。

- 文字列は、StateMachine 全体に渡って使用したいデータ。
- 配列 A は、1 つ目の Map タスクで分散したいデータ。
- 配列 B は、2 つ目の Map タスクで分散したいデータ。

#### 1. Task1

Lambda からの出力がこんなのだとする。

```json
{
  "string": "nanigashi",
  "arrayA": [
      0, 1, 2, 3, 4, 5, 6
  ],
  "arrayB": {
      "A", "B", "C", "D", "E"
  }
}
```

`ResultPath`を`$.key`にしておくと Task1 ステートの出力は ↓ になる。

```json
{
  "key": {
    "string": "nanigashi",
    "arrayA": [
        0, 1, 2, 3, 4, 5, 6
    ],
    "arrayB": {
        "A", "B", "C", "D", "E"
    }
  }
}
```

#### 2. Map1

- `ItemPath` に `$.key.arrayA` を指定し、配列 A でイテレーションする
- `Parameters` に Lambda へ渡したいパラメータを指定する。以下の通り
  - マッピングの各要素は`$$.Map.Item.Value`
  - 追加で渡したいパラメータを `$.key.string`
- `OutputPath` に前のタスクの出力 `$.key` を指定する
- マッピング処理の出力は無視したいので、`ResultPath`に`$.null`など`OutputPath`に含まれないパスを指定する

#### 3. Map2

- `ItemPath` に `$.arrayB` を指定し、配列 B でイテレーションする
- `Parameters` に Lambda へ渡したいパラメータを指定する。以下の通り
  - マッピングの各要素は`$$.Map.Item.Value`
  - 追加で渡したいパラメータを `$.key.string`
- 出力を制御したい場合は、Map1 同様に`OutputPath`、`ResultPath`を指定仕分ける

これで Task1 の出力を Map1 をまたいで Map2 で利用できる。

### まとめ

これで最初の Lambda の Output を加工せずそのまま後ろ 2 つの Lambda まで伝播できた。やったね 😂
文章だけじゃわからなさすぎる気がしてきた...今度サンプルコードを起こすことにする。

ただし懸念点として以下の気になる 2 点も備えており、どうしたもんかなと言う感じでもある 🤔

1. 伝播したい回数だけ階層化しないといけないのではないか
   - 出力を無視するためにはセクションを切り分けないといけなくなってるから
2. 可変長のパラメータを伝播する場合、ペイロードの上限値に注意しないといけない
   - ダブルクォートは`\`エスケープされるようだし計算が大変
   - だからセクションを切り分けるしかなくなってる

出力無視することさえできたら階層化いらんなー 🤔
