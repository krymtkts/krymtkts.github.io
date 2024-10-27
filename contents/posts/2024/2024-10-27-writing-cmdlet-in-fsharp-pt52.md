---
title: "F# で Cmdlet を書いてる pt.52"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) 開発をした。

word 操作の実装中 [#173](https://github.com/krymtkts/pocof/issues/173) で、移動、選択、削除を雑に作った。

多分まだ日記でどういう方針で実装するかを書いてなかったので、備忘がてら実装開始前のメモを記しておく。

> word 操作の実装は PSReadLine を参考にするつもり。
>
> [PSReadLine/PSReadLine/Movement.cs at e87a265ef8d2c6c5498500deb155bf6258b34629 · PowerShell/PSReadLine](https://github.com/PowerShell/PSReadLine/blob/e87a265ef8d2c6c5498500deb155bf6258b34629/PSReadLine/Movement.cs#L406)
>
> `_singleton.Options.WordDelimiters` は `Get-PSReadLineOption | Select-Object WordDelimiters` のこと。
>
> `;:,.[]{}()/\|!?^&*-=+'"–—―`
>
> この文字列に対して独自の文字列一致関数を使って一致したか判定している。
>
> [PSReadLine/PSReadLine/StringBuilderCharacterExtensions.cs at e87a265ef8d2c6c5498500deb155bf6258b34629 · PowerShell/PSReadLine](https://github.com/PowerShell/PSReadLine/blob/e87a265ef8d2c6c5498500deb155bf6258b34629/PSReadLine/StringBuilderCharacterExtensions.cs#L32-L35)
>
> また加えて、
>
> [PSReadLine/PSReadLine/StringBuilderCharacterExtensions.cs at e87a265ef8d2c6c5498500deb155bf6258b34629 · PowerShell/PSReadLine](https://github.com/PowerShell/PSReadLine/blob/e87a265ef8d2c6c5498500deb155bf6258b34629/PSReadLine/StringBuilderCharacterExtensions.cs#L65-L77)
>
> を見てわかるように character が whitespace の場合は問答無用で区切る。
>
> pocof も同様に Option で指定できるようにしてジャンプ位置を指定するようにしようかな。

[PSReadLine](https://github.com/PowerShell/PSReadLine) の word の特徴？なのか bash とは別の動きするので PowerShell の流儀的に pocof もそれに倣うため実装を参考にしてる。
ただ、 pocof では選択範囲ありの行頭・行末までの削除の挙動が独自なのだけど、今回の word 削除でも同じく独自にした。
PSReadLine では選択範囲を無視して word を削除するが、 pocof では選択範囲も削除する。

あと当初の予定の範囲で実装できてないのが、区切り文字の Option 指定。
これは他の Option 同様に内部状態に加えるだけなので実装自体はたいしたことない。
個人的には `_` も区切りたいのだけど、これは自分でデフォルトパラメータを設定したらいいだけなので、足さない。

そこまでできたら最後にリファクタリングが必要だ。
何故なら、今回の実装で痛感したが、文字の選択と削除の実装が結構バラバラで苦労したからだ。
1 文字消すのと複数文字消すのと行頭行末まで消すので実装マチマチなので、なんか統一的な操作に落とし込めるだろこれｗみたいな。
過去の自分のツケが回ってきた。

あと特に認知的な負荷が高いと思ってるのが、選択範囲の実装。
正負の整数でカーソルの前後にある選択範囲の文字数を示してるので、最近全然触ってなかったし結構混乱した。
すでに discriminated union にくるまれた整数だが、更に DU を分けるべきなのか、 Units of measure で上手くできるもんなのか、検討する。
実際選択範囲を正負の整数で表現することで、カーソル位置から選択範囲を加減算するだけで選択範囲のカーソル位置がわかる便利さはあるのだけど、案外リファクタリングの過程で別のわかりやすい実装が見つかるかも知れんし。

続く。
