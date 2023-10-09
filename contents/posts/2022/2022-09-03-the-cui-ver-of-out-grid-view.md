---
title: "Out-GridView の CUI 版 Out-ConsoleGridView"
tags: ["powershell"]
---

最近知ったのだけど、 `Out-GridView` の PowerShell Team 謹製 CUI 版 `Out-ConsoleGridView` があったらしい。これが使えるヤツだったら `poco` も今作ってる `pocof` も要らんやん、と思った。

[PowerShell/GraphicalTools: Modules that mix PowerShell and GUIs/CUIs! - built on Avalonia and gui.cs](https://github.com/PowerShell/GraphicalTools)

元々 CUI 使ってるのに `Out-GridView` で GUI 表示しないとインタラクティブな絞り込みできないのがイヤで [peco](https://github.com/peco/peco) を使ってたってのがある。
`peco` は PowerShell に最適化されてないから、その後オブジェクトのまま取り扱える [jasonmarcher/poco](https://github.com/jasonmarcher/poco) にたどり着いた。
その後 `poco` の独自実装を自分で始めたのが [`pocof`](https://github.com/krymtkts/pocof)(F# の練習がてら)。

なもんで、 `Out-GridView` の CUI 版があってもしイイ感じに使えるのなら、この長年の変遷に終止符を打つんちゃうかな的な。

以下 `README.md` に従い試す。

```powershell
Install-Module Microsoft.PowerShell.ConsoleGuiTools -AllowPrerelease -Scope AllUsers
```

```powershell
Get-ChildItem | Out-ConsoleGridView
```

![水色の背景に印字される `ls` の結果](/img/2022-09-03-capture/capture.png)

`space` キーでオブジェクトを選択するか `Ctrl+A` で全選択して、 `Enter` で選択した結果を出力する。
`-Filter` オプションに渡した値で初期表示がフィルタリングされた形になる。そのままタイプしてもフィルタ入力できない。`Tab` キーを押してフィルタのテキストにフォーカスできる。
フィルタは正規表現のみみたい。不正なパターンを入れたらちゃんとエラー表示されてエライ。
必ず結果を洗濯した状態で`Enter` 押さないと結果が得られないのはちょっとメンドイ。
わたしは使わないけどマウスコントロール(クリックどころかスクロールまで)できるのもすごいな。
背景変わるの好きになれないので、今後色とかキーとかのオプション充実したりするかなあ。
インクリメンタルサーチじゃないのも微妙に好みじゃない操作性だ。

`poco`、 `pocof` 共にバッファを元に戻すとレイアウトの崩れが起きるのだけど、 `Out-ConsoleGridView` は崩れない。素晴らしい。
表示域にないスクロール可能な部分は吹っ飛んでしまうけど、崩れない方法は真似させてもらいたい(MIT ライセンスなので)。けど、コード読んだ感じどこかわからなかった。

`Out-ConsoleGridView` は CmdLet の出力に [FormatViewDefinition Class (System.Management.Automation) | Microsoft Docs](https://docs.microsoft.com/en-us/dotnet/api/system.management.automation.formatviewdefinition?view=powershellsdk-7.0.0)を自前で作ってるみたい。
なので PowerShell で表示される内容とちょっと違う。
それ故か、例えば `Get-InstalledModule` のような情報量の多い結果を表示するとちょっと悲しい感じになってしまった。 `format.ps1xml` が利用されないせいかな。しかしこのクラス使い方がわからなかったので勉強になるわ。

```powershell
Get-InstalledModule | Out-ConsoleGridView
```

![すし詰めに印字される `Get-InstalledModule` の結果](/img/2022-09-03-capture/jam-packed-capture.png)

`pocof` でやりたいことと方向性が違うなあという感じなので、作ってるものを今すぐブン投げ捨てる必要はなかったようだ。こちらの開発は趣味プロで続けようと思った。しかしコード参考にできるものが増えたのでとても助かるなぁ。
