{:title "F#でコマンドレットを書いてる pt.16"
:layout :post
:tags ["fsharp","powershell"]}

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

[FsUnit x xUnit](https://fsprojects.github.io/FsUnit/xUnit.html) を使ったテストコード追加とリファクタはまだ続けていて、前回から引き続きバグがもりもり見つかる。
`PocofData.invokeAction` はアクションに対応づいた各ロジックを呼び出して、内部状態を担うレコードを変更して返すだけの処理だけど、油断ならん。
[#39](https://github.com/krymtkts/pocof/issues/39), [#40](https://github.com/krymtkts/pocof/issues/40)

まだ合計時間にしたら 10 時間も FsUnit(というか [FsUnitTyped](https://fsprojects.github.io/FsUnit/FsUnitTyped.html)) 使ってないし、なんなら `shouldEqual` と `shouldFail` くらいしか使ってない。
けど、手になじんできた気がする。

`shouldEqual` 使っていて良いのは、型間違うことがないところ(当たり前すぎるか)。

[`should`](https://github.com/fsprojects/FsUnit/blob/d8b95201efc7f478da3d677215291b5aa5487185/src/FsUnit.Xunit/FsUnit.fs#L32) の関数シグネチャがこう `obj` になってる。

```fsharp
let inline should (f: 'a -> ^b) x (actual: obj) =
```

片や [shouldEqual](https://github.com/fsprojects/FsUnit/blob/d8b95201efc7f478da3d677215291b5aa5487185/src/FsUnit.Xunit/FsUnitTyped.fs#L13)はジェネリックになってる。

```fsharp
    let shouldEqual<'a> (expected: 'a) (actual: 'a) =
```

当然ながらその分テストを書くときに楽ができる。実際には以下のような一見して間違いがわかる派手な例はないだろうけど。
テストコードのバグも有り得るので、とにかく安全な方に激ぶりしてかつ楽ができるのは、良い。

```fsharp
"a" |> should equal 1 // エラーじゃない。
"a" |> shouldEqual 1 // 型不一致のエラー。良い。
```

あとは、パイプ演算子のお陰で「英文ぽい」順番で記述できるのも良い。ただこの文脈でいえば、FsUnit の見た目に分がある。 `should` と `equal` 等分かち書きできるからな。

```fsharp
actual |> shouldEqual expected
actual |> should equal expected // <- 見た目とても良いけど型検査できなくて片手落ち。
```

OOP なんかでも `test(actual).shouldEqual(expected)` みたいな形式なら同じ感覚だろうけど、個人的にカッコがなくより自然(Lisper みたいに熟達すればカッコが透けて見える可能性もあろうが)。

とはいえ良いこと尽くめではない。ちょっと困ったのはテストのグループ化。
例えば同じ関数に対するテストとか、関連あるグループをまとめたら平置きじゃない分だけ注力スべきコードが絞られて見やすくなるので、個人的には積極的にグループ化する。

FsUnit というか xUnit だと、 F# ではこんな感じに `module` を多層化するか `module` と `type` を使うか 2 通りの手法があるみたい。以下にその 2 通りのテストコードもどき。

```fsharp
module Tests

// module と type。
module ``PocofAction Tests`` =
    type ``toKeyPattern should returns`` () =
        [<Fact>]
        member x.``A without modifiers.`` () = // 以下略。

// module 多層化。
module ``PocofAction Tests`` =
    module ``toKeyPattern should returns`` =
        [<Fact>]
        let ``A without modifiers.`` () = // 以下略。
```

pocof ではまだテストで状態を持つことないのと、単にカッコや自己識別子を書く回数が減るってだけで `module` で多層化してる。

まあここまでは良い。これを `dotnet test --list-tests` したときにでるテストケースごとの名称のつなぎ方？が気に入らなくて、なんとかならんのかなと思ってる。
先述の例の場合だと、 `Tests+PocofAction Tests+toKeyPattern shoud returns.A without modifiers.` のようなテストケース名になる。これ `+` とか `.` とかどうにかならんのかな。理想は whitespace で繋いでほしい。
単に制御可能でその設定方法を知らないだけという可能性もあるので、追って調べたい。

多少気に入らないところを愚痴ったが、概ね満足している。今後もテストを足していってもりもりバグを洗い出す。かなーりテストコードに時間かかってる気がするけど、マイペースに。
