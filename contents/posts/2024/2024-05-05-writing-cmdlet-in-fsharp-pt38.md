---
title: "F# でコマンドレットを書いてる pt.38"
tags: ["fsharp", "powershell", "dotnet"]
date: 2024-05-05
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 0.12.0 をリリースした。

[前に触れてた](/posts/2024-04-21-writing-cmdlet-in-fsharp-pt37.html) Active patterns の適用、あと key mapping の名称と割当の変更などのリファクタリング。
それと CPU 使用率が高くなるバグへの対応だ。これは速く対処して出したかった(それでも面倒で間が空いたが)。

[先日 `KeyAvailable` になるまで `ReadKey` しなくした](/posts/2024-04-07-writing-cmdlet-in-fsharp-pt36.html)ことでイベントループが回りまくって CPU 上げてた。
実はこれ pocof の初期実装では CPU load 下げるための `Thread.Sleep` が仕込まれており、要は考慮済みだった。
だけどその後紆余曲折で await 使ったりして `KeyAvailable` がなくなり、その後また復活して～ってなってその部分だけ忘れ去られてたという形。情けない。

---

あと最近気づいたお困りポイントが、 pocof に大量のデータを食わせたときの応答の遅さ [#177](https://github.com/krymtkts/pocof/issues/177) とメモリ使用量が多すぎる [#176](https://github.com/krymtkts/pocof/issues/176) 問題だ。
単純に `1..1000000 | pocof` のようなコマンドを実行してみたら起動も遅いし、キータイプごとに秒かかるし、 2GB 超メモリを食う。
やばすぎ。
キャンセルして終了したあと `[GC]::Collect()` でもしないと確保したメモリが解放されない始末(それも全部が解放されないという)。

これに気づいたのは以下のような関数を書いてるとめちゃくちゃ遅いので、その理由を調べたときのことだった。
この関数自体は pocof の遅さに由来して遅いのではなく、単に `Select-Object -Unique` が脅威の遅さだったためだが。 12,000 件程度しかなかったので大して pocof は遅くなかった。

```powershell
    function global:Show-ReadLineHistory() {
        Get-Content -Path (Get-PSReadLineOption).HistorySavePath | Select-Object -Unique | Select-Pocof -CaseSensitive -Layout TopDown
    }
    Set-Alias pghy Show-ReadLineHistory -Option ReadOnly -Force -Scope Global
```

応答が遅いのは 2 種類ある。

まずは、都度 O(N) かけて要素を絞り込んだり件数を算出したりしてたため、フィルタリングが遅い。
内部でデータを保持するのに `list` を使ってたのもあり、ひとまず `seq` に変更した。
`seq` は要素が必要なタイミングまで遅延評価される。
今のところ件数の表示がある限り遅延評価を活かせず速くならないが、件数の表示をなくせば表示範囲の件数だけ取れたら処理を終われる構造になった。
そういうオプションにすればいいのかもしれないが、とはいえこれも全件走査が必要な場合に効果がない。
根本的に内部のデータ構造を変えないと速くならんのではーと見ている。
でも pocof は依存関係基本なしの縛りがあるので、自前で効率化する方法やデータ構造の見直しを色々手探りしてみるしかないか。

もう 1 つは `EndProcessing` で描画を開始するので、 PowerShell の Cmdlet の仕組み上データ量が多いと `ProcessRecord` が終わるまでの時間も長くなり、初動が遅い。
アイデアベースではこれの解決案は考えついていて、単に `ProcessRecord` 中に画面描画開始できる件数が溜まったら描画し始めたらいいかなと考えている。
ただその際にキー入力を受け付け始めれるのか？とか、単に実装力の問題でアイデアをモノに落とし込めてない。
なのでこれも小さなサンプル Cmdlet でも作って手探りする必要がある。

最後にメモリ使用量が多い件。
まだ一番でかい原因を追いきれてないが、おそらく [Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) を各要素を wrap するのに使ってるからかと推測している。
というのも、まず `ProcessRecord` 毎に `list` が生成されるような構造だったので、試しに mutable な List を使って `for` で追加していく形に最適化してみた。
ところがそんなに爆発的には効果がなかった(ゆーても 2.3GB 超 RAM 消費する状態で 1.9 GB にはなったが)。
であれば Discriminated Unions が濃厚かなと考えるが、 pocof のままだとちょっと試すのも色々変えないといけないので億劫だ。
ひとまず Discriminated Union で wrap するときしないときのメモリ消費量を計測するためのサンプル Cmdlet でも書いてみて検証するしかないかな。

ちなみにこれらの点いずれも、 [PowerShell/ConsoleGuiTools](https://github.com/PowerShell/ConsoleGuiTools) のフィルタは初期ロードが死ぬほど遅いのを除けば[^1]、省メモリで高速にキビキビと動く。
なんか参考になる要素あるかもな。

[^1]: わたしのいまや貧弱になった Razer Blade Stealth 2018 だと `1..1000000 | Out-ConsoleGridView` でもしようものなら起動までに 7 分かかる。

2 年超の間のらりくらり F# で PowerShell の Cmdlet 書いてきてるが、(体系的な学習してないのもあって)知らないことに毎度のようにぶち当たる。
知るべきことを知ってない怠惰なのかも知れんが、ここは広範な知識の海で新たな発見が尽きないのだと、ポジティブに考えていきましょ。

---

今日は気になっている課題を書いたが、他にも pocof でやりたいことがちらほら残っている。
`Ctrl+LeftArrow`, `Ctrl+RightArrow` あたりで単語区切りの移動 [#173](https://github.com/krymtkts/pocof/issues/173) したい気分になってきた。
また、よく考えると選択範囲がある状態でのプロパティ補完時の挙動も未定義なままだった。
パフォ系の問題を処置する前にするか、処置したあとにするか...思いつき駆動なのでちょっと悩ましい。

---

### 追記 2024-05-05

> ひとまず Discriminated Union で wrap するときしないときのメモリ消費量を計測するためのサンプル Cmdlet でも書いてみて検証するしかないかな。

多少はメモリ食うがだいぶ的外れっぽい。
ヤバいのは `PSObject.properties` にアクセスして補完候補を集めてるところみたい...うーん。
