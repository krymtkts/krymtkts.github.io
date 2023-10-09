---
title: "`ping`の代わりに`Test-Connection`を使う"
tags:  ["powershell"]
---

PowerShellには`Test-Connection`というやつがあるのを今更ながら知った。

今の仕事では、仮想マシン(dockerでない)を複数立ち上げて作業することが常になってるのだが、結構立ち上げ忘れてたりしてうっかりSSHしたときに繋げないのである😢

疎通確認と合わせてSSHするすべを探してたところ、これを知ったので以下のようなスクリプトを作ってCmderのTaskに登録して使っている。

```powershell
$waken = Test-Connection -TargetName $ip -Count 1 -Quiet
if (!$waken) {
    Write-Output "target not found. please start up $ip"
    $Host.UI.RawUI.ReadKey() | Out-Null
    exit
}
ssh "admin@$ip"
```

[Test-Connectionが遅い理由と対策方法について - しばたテックブログ](https://blog.shibata.tech/entry/2016/06/16/231239)

↑このような話もあるようなので目を通しておきたいところ🤔

### おまけ

[PowerShellでPause - Qiita](https://qiita.com/twinkfrag/items/f3ecf79b68ea09eadec2)

最近のPowerShellには`Pause`なるもんがデフォで入ってるが、古来からの方法でEnter以外のキーでも使えるようにしておくのが良いであらうか？

```powershell
Write-Output "type key to continue..."
$Host.UI.RawUI.ReadKey() | Out-Null
```

スクリプトの中でコレを呼んだら、なんかのキーを押すまで止まる。

戻り値が標準出力されないようにするために`Out-Null`にパイプラインする。`$null`に代入でもよいが。

### 余談

この記事をしたためておるときに`lein ring server`でエラーが出るようになってたのを解消したので、メモがてら次回に記す。
