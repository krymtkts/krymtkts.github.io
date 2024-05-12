---
title: "F# でコマンドレットを書いてる pt.39"
tags: ["fsharp", "powershell", "dotnet"]
date: 2024-05-05
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の開発をした。

[前に](/posts/2024-05-05-writing-cmdlet-in-fsharp-pt38.html) メモリ浪費について触れ、それがどうも [`PSObject.Properties`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.psobject.properties?view=powershellsdk-7.4.0) によるものだと書いた。

> > ひとまず Discriminated Union で wrap するときしないときのメモリ消費量を計測するためのサンプル Cmdlet でも書いてみて検証するしかないかな。
>
> 多少はメモリ食うがだいぶ的外れっぽい。
> ヤバいのは `PSObject.properties` にアクセスして補完候補を集めてるところみたい...うーん。

これイマイチ挙動がわからないのだが、 [`ProcessRecord`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.cmdlet.processrecord?view=powershellsdk-7.4.0) の中で `PSObject.Properties` にアクセスしたとき条件が揃うと、症状が悪化するぽいとわかった。
浪費する量は Discriminated Unions に包むことで増えるメモリ量の比でない。まさに爆増。

あまりにわからなかったので宣言通り検証用モジュールを作り、こねくった。

- [fsharp-cmdlet-sandbox/src/wrap-dus-or-not/Library.fs at main · krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox/blob/main/src/wrap-dus-or-not/Library.fs)
- [fsharp-cmdlet-sandbox/test-wrap-dus-or-not.ps1 at main · krymtkts/fsharp-cmdlet-sandbox](https://github.com/krymtkts/fsharp-cmdlet-sandbox/blob/main/test-wrap-dus-or-not.ps1)

100 万個の `int` を流し込むパターンと 100 万個の `PSCustomObject` を流し込むパターンを作ってみた。
そのそれぞれで、生か DUs でくるんで追加、プロパティアクセスの有無などの組み合わせでパターン分けして検証してみた。なるべく pocof の利用パターンに近い形で。

わかったのは、100 万回 `ProcessRecord` が呼ばれても、その中でただ `PSObject.Properties` **だけ**アクセスするのであればメモリを浪費しない。
`ProcessRecord` の中で、 `PSObject` 本体をコレクション (今の pocof では [`System.Collection.Generic.List`](https://learn.microsoft.com/ja-jp/dotnet/api/system.collections.generic.list-1?view=net-8.0)) に格納し、かつ `PSObject.Properties` にアクセスすると延々メモリを浪費するのがわかった。
「アクセスする」と書いてるのは、言葉通り `PSObject.Properties` を参照するだけでメモリ消費につながるからだ。↓ これもダメ。

```fsharp
    let throughProps (io: PSObject) =
        match io.BaseObject with
        | _ ->
            for p in io.Properties do
                // p.Name |> ignore
                ()
```

なんだこれ。

`ProcessRecord` の中で `PSObject` 本体を保存しない選択肢はないので、ならば `PSObject.Properties` に都度アクセスしなければ良いと仮定し、 [`PSObject.BaseObject`](https://learn.microsoft.com/ja-jp/dotnet/api/system.management.automation.psobject.baseobject?view=powershellsdk-1.1.0) で読み込み済み or not を判定してプロパティを収集するように変えたら、メモリ浪費を抑えられる事がわかった。
なので pocof では一旦それに倣ってメモリを消費しない形を実現できた。
でも [`PSCustomObject`](https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.pscustomobject?view=powershellsdk-7.4.0) の場合は依然メモリを食いまくることがわかり、これはどうすりゃいんだとなった。
が、これは [`Out-ConsoleGridView`](https://github.com/PowerShell/ConsoleGuiTools/blob/main/docs/Microsoft.PowerShell.ConsoleGuiTools/Out-ConsoleGridView.md) でも浪費するのがわかった(し pocof の方が多少メモリ消費も低い)ので一旦放置した。

これで一旦 pocof のメモリ浪費対策はそこそこに対応できたかなと。
あと今書いてて `hashtable` のパターン見忘れてるの気づいたので、やっておこう。

ただし当然のごとく、この `PSObject.Properties` の挙動は不可解なので PowerShell の実装を見てみた。
が、残念ながらよくわからなかった。

[PowerShell/src/System.Management.Automation/engine/MshObject.cs at d564d0fff95b6251dfb9e79d8243b319a7c0aecf · PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/blob/d564d0fff95b6251dfb9e79d8243b319a7c0aecf/src/System.Management.Automation/engine/MshObject.cs#L751-L768)

```csharp
        /// <summary>
        /// Gets the Property collection, or the members that are actually properties.
        /// </summary>
        public PSMemberInfoCollection<PSPropertyInfo> Properties
        {
            get
            {
                if (_properties == null)
                {
                    lock (_lockObject)
                    {
                        _properties ??= new PSMemberInfoIntegratingCollection<PSPropertyInfo>(this, s_propertyCollection);
                    }
                }

                return _properties;
            }
        }
```

一度プロパティが読み込まれたら生成したコレクションをキャッシュをするようになってた。
同一 `PSObject` から連続で読む限りはキャッシュが保持されるが、 `ProcessRecord` のなかで呼ばれるのはそれぞれ別の `PSObject` なので、 pocof の利用ケースではそもそもキャッシュは使われない。
別の `PSObject` であれば `lock` も関係ないし。
`new PSMemberInfoIntegratingCollection<PSPropertyInfo>(this, s_propertyCollection)` の部分がメモリ消費しまくってると考えれば、アクセスするだけでダメなのも頷ける。
逆に何故 `PSObject.Properties` **だけ**アクセスする方のメモリ消費が低くなるのか気になってきた。
キャッシュが使われているとしか思えんが、最適化で暗黙的な使いまわしが発生してんのか...？
この挙動を追い詰めるのに今回は時間を使いたくなかったので、そのまま放置。これは将来の宿題とする。

---

ひとまず pocof の patch version up でこの修正を release しておくかー。
