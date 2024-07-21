---
title: "Chocolatey の chocolateyInstall.ps1 が壊れている場合の workaround"
tags: ["powershell", "chocolatey"]
---

わたしは長らく Windows のパッケージ管理に [Chocolatey](https://chocolatey.org/) を使ってるのだけど、壊れたパッケージを無理やり直してインストールする機会があった。
これはもう直ってる問題に対して「こうしましたよ」という記録を残すものだ。

壊れたパッケージというのはわたしのお気に入りの [PowerToys](https://community.chocolatey.org/packages/powertoys) だ。

[Update PowerToys script to newer tag by jaimecbernardo · Pull Request #253 · mkevenaar/chocolatey-packages](https://github.com/mkevenaar/chocolatey-packages/pull/253)

上記は PowerToys 開発チームの dev lead の人が chocolatey package の repo に作ってくれた PR だ。親切極まりない。
PR によれば次のようにして壊れたらしい。

先日 PowerToys 開発チームでリリース時に tag を `v0.82.1` が正しいところ `V0.82.1` とつけてしまったらしい(tag は `v0.82.1` につけ直されてた)。
その影響を受けて、 [PowerToy の chocolatey package を作ってくれてる人の package](https://github.com/mkevenaar/chocolatey-packages) が自動更新なのか知らんが壊れたっぽい。

どう壊れたかというと、 [`chocolateyInstall.ps1`](https://docs.chocolatey.org/en-us/chocolatey-install-ps1/) が存在しなくなったタグ `V0.82.1` を指したままになった。
`chocolateyInstall.ps1` は `choco upgrade powertoys -y` で download されてくる。
要は download したホヤホヤの `chocolateyInstall.ps1` が存在しない URL からバイナリを download しようとするので、常に 404 でエラーするようになった。

これだと修正が merge されないと chocolatey package が更新できない。
直し方はわかってるし、自力で力技の解決できないか試したら、できた。

1. 事前に `chocolateInstall.ps1` を新しいバージョンの URL と checksum に変えておき、クリップボードにコピーしておく。 checksum は GitHub の Release ページから取れる
   - [Release Release v0.82.1 · microsoft/PowerToys](https://github.com/microsoft/PowerToys/releases/tag/v0.82.1)
2. `chocolateInstall.ps1` をエディタで開きっぱなしにしておく
3. `choco upgrade powertoys -y` を実行する
4. `chocolateInstall.ps1` がダウンロードされ次第(≒ エディタで表示してる内容が変わったら)変更内容をコピペして即座に保存する
5. うまくいったらコピペで変更されたファイルを使ってバイナリのダウンロードが始まる

はじめこれでうまくいくかわからんかったので手でやったけど、これなら [FAKE の ChangeWatcher](https://fake.build/reference/fake-io-changewatcher.html) や素の [FileSystemWatcher](https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-8.0) で自動化できそう。
こんなもん自動化してどうすねんというのはあるけど。

今回は Chocolatey の正当な仕組み全く理解せずとも、ログを追ったらこのような力技ができた。
ちゃんとした仕組みに乗ったもっとマシな patch 方法ないのかなとか、この仕組みはインジェクション可能ということで危なくないのかなと思ったりした。
一度 Chocolatey の流儀を学んでちゃんとした方法を調べるかー？
いい加減 Chocolatey にフリーライドし続けてきたのでちゃんと仕組みを理解して貢献できたらいい気もするしな。
積みタスク行き。
