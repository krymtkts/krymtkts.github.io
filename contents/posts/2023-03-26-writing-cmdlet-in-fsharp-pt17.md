---
title: "F#でコマンドレットを書いてる pt.17"
tags: ["fsharp","powershell"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) の話。

もう 4 月なのでそろそろ Fable Compiler を試してみたいと思ってるが、 pocof のテスト書くのと bugfix が落ち着かず、できていない。
ちょっと今までのテストを小綺麗にする作業にも取り組んだ。

[前回](/posts/2023-03-19-writing-cmdlet-in-fsharp-pt16.html)、 `module` を多層化することでテストケースをグルーピングしている話を書いた。
その時はテストケースを 1 つのファイルにまとめて書いていたのだけど、 `PocofQuery.run` のテストを書き始めるにあたり、テストケースが爆増するのに備えてファイルを分割したいと思っていた。

はじめ F# ではどうテストプロジェクトのファイル分割をするのかわからなかったが、 F# の repo をいくつか参照してみて [fsprojects/FSharp.Data.Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive) が参考になりそうとわかった。
それを pocof に提供したところ、上手く分割できた。要は普通の F# Project と同じようにするだけだった。コミットは [krymtkts/pocof@dff10c8](https://github.com/krymtkts/pocof/commit/dff10c89963cdca60cd6ca9e7afeb7f7915e2ff4) 。

[FSharp.Data.Adaptive/src/Test/FSharp.Data.Adaptive.Tests](https://github.com/fsprojects/FSharp.Data.Adaptive/tree/5e92ab426e5a438d70986cefff638ecf2acef576/src/Test/FSharp.Data.Adaptive.Tests) がテストプロジェクトのディレクトリ。
ここに分割されたテストコードのファイルが色々あるのと、テストプロジェクトのエントリポイントになる `Program.fs` が配置されている。
プロジェクト [FSharp.Data.Adaptive.Tests.fsproj](https://github.com/fsprojects/FSharp.Data.Adaptive/blob/5e92ab426e5a438d70986cefff638ecf2acef576/src/Test/FSharp.Data.Adaptive.Tests/FSharp.Data.Adaptive.Tests.fsproj) を参照するとエントリポイント `Program.fs` が最後の読み込みなっているのがわかる。
それ以前のファイルにモジュールを小分けにしたテストを書いたら良い。

ファイル分割の粒度はモジュール別に 1 ファイルした。
あまり細かく分けると何処にテストを書くか悩みがちなので、シンプルにモジュールと対にしている。複雑なクエリのテストを書くとまた量が増えて更にファイルを分割したくなるだろうが、今ではない。

ひとまず満足な形に分解できたのでヨシ。

---

あとこの [fsprojects/FSharp.Data.Adaptive](https://github.com/fsprojects/FSharp.Data.Adaptive) を参考にしたとき気づいたのだけど、 [FsCheck](https://github.com/fscheck/FsCheck) というモジュールを使ってるようだった。 Property-based Testing をするためのやつだ(今書いてるのは所謂「おなじみ」の Example-based Testing という)。
[The 'Property Based Testing' series | F# for fun and profit](https://fsharpforfunandprofit.com/series/property-based-testing/) が参考になった。

実際 pocof でもテストケースが貧弱で、テストは成功するがバグってたようなケースがもちらほらあるので、こういうより強力な方法を使うのが良いかもなーと興味深く思っている。
とはいえ興味の範囲で無限にやること増えていく。
かなりやってみたいのだけど、キャッチアップのほうが追いついてない感じ。

まずは所謂「おなじみ」の Example-based Testing である程度カバーしてから、 FsCheck で強化するってステップを踏むのが良さげ。徐々に... ひとまず今ある draft の pull request を merge してから、今後どう取り組んでいくか考えよかな(タスクを未来にブン投げる)。
