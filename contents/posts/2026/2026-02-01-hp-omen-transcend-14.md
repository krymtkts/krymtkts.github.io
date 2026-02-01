---
title: "HP OMEN Transcend 14 買った"
tags: ["windows"]
---

従来機 Raze Blade Stealth 2017 も随分古く遅くなったし、昨今のメモリ高騰もあるしで、久し振りに laptop を新調した。
購入したのは [HP OMEN Transcend 14 の OMEN by HP Transcend 14-fb1011TX スプリームモデル G2](https://jp.ext.hp.com/gaming/personal/omen_transcend_14/?msockid=08b31ccb0c12649719b60a250d1c6572) というやつ。
セールでも 38 万円ほどした。なかなかの出費だ。主要な Spec は以下の通り。

- [Intel Core Ultra 9 285H (max 5.40GHz, cache 24MB)](https://www.intel.co.jp/content/www/jp/ja/products/sku/241747/intel-core-ultra-9-processor-285h-24m-cache-up-to-5-40-ghz/specifications.html)
- 14.0 inch 2.8K OLED
- RAM 64GB (LPDDR5x-7467MT/s)
- 2TB PCIe Gen4 NVMe M.2 SSD TLC
- NVIDIA GeForce RTX 5070 Laptop

この機種は GPU の消費電力が制限されておりフルパワーが出せなかったり、 13 inch が好きなので 14 inch はちょっと大きいとか色々気になる点はあった。
64GB の大容量メモリだと [AMD Ryzen AI Max+ 395](https://www.amd.com/en/products/processors/laptop/ryzen/ai-300-series/amd-ryzen-ai-max-plus-395.html) が載った [ROG Flow Z13 (2025) GZ302](https://rog.asus.com/jp/laptops/rog-flow/rog-flow-z13-2025/) もいいなと思っていた。
でもいくら驚異的に速い integrated GPU を搭載しているからと言って、 UWQHD のような高解像度だと現時点では discrete GPU には叶わないのが調べてわかった。
次買い替えるときには状況はわかってるかもだが、今久し振りにゲームもするならもうええかということで HP OMEN Transcend 14 にした。
理想としては laptop のヒンジが 180 °開く機種を選びたかったのだけど、これも妥協点の 1 つ。
ただ総じて Spec は高いので、従来機に比べて現時点極めて高速で満足している。

しかし Microsoft アカウントの気に入らない仕様を回避するのには苦労した。
Windows 11 では初期設定時の Microsoft アカウントのログインは諸々の設定が同期されるので極めて楽だが、代償として username がアカウントメアド先頭 5 文字になってしまう。
`takatoshi~` が `takat` に。なんでなんだよ。
仕事では面倒でそのまま気にせず使ってるが、私用機ではこれは許容しがたい。
回避方法としてはネットワークを使わずに初期設定すれば local user を作って始めれるようなのだが、うっかり忘れて Wi-Fi に繋いでしまった。
一度 Wi-Fi に繋いだあとどうやって解除するかわからなかったので、ひとまず Microsoft アカウントで初期設定したのだが、これが良くなかった。

初期設定後、 Microsoft アカウントの user を local user に変更、期待の名前の local user を作成し Microsoft アカウントに関連付けた。
ここで Windows Hello の設定が成功しなかったり設定や OneDrive の同期が機能しなくなった。
既存 user に設定していた Windows Hello と PIN の設定を外してもうまくいかなかったので一晩寝かしたところ、うまくいく様になった。クラウド側でキャッシュされてたのかな。

OneDrive の同期だけは全く解消されなかったので、 AI とトラシューの上別の端末として管理されてしまったのかもなと考えて既存 user に連携済みファイルを移動してみた。
ここで急に同期が始まり Copy という suffix を持つ同じファイルが OneDrive にできてしまった... Copy 側を全部消して一旦同期済みになった。
Windows の設定が同期されないのはかなり痛いが、調べつつ個別に設定していった。

アプリケーション類のインストールは [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/) [Chocolatey](https://chocolatey.org/) を使った。
知らなかったのだが、昨今の Windows は winget がプリインストールされておりはじめから使える。
初回の script での package manager をインストールする手間が省ける。
今回は元々 winget で管理している package と Chocolatey を winget で、その他を Chocolatey でインストールした。
script 形式で配布されるアプリケーションはまだ winget ではインストールできないのもあるし、元々使ってるアプリケーションも Chocolatey で管理している。
が、 winget も package が拡充され使用感が良くなってるようなので、 binary 系は winget に寄せるのもいいのかもな。

処理系と依存関係の設定、 Context menu 置き換え等の Registry 書換は自前の PowerShell Profile [krymtkts/pwsh-profile](https://github.com/krymtkts/pwsh-profile/) で自動化している。
winget でも取れないアプリケーション、例えば [Windows Terminal Canary](https://github.com/microsoft/terminal?tab=readme-ov-file#installing-windows-terminal-canary) は profile で自動化している。

あと今回から OneDrive で設定、 backup 、自作フォントを同期して使ってみた。
旧 laptop で雑に管理されたファイルが新 laptop にも引き継がれてしまう点は懸念だが、移行はかなり楽になった。
この方法なら [Stylus](https://github.com/stylus/stylus) の user CSS を OneDrive に出力しておけば Google Chrome でも Edge でも両方使えて便利だ。
dotfiles のうち sensitive な一部は OneDrive で同期してコピペした。
PowerToys も backup を OneDrive に出力し restore する方法を採用した。
ただ例えば [Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview) のような一部の設定は出力されなかったようで、そこは手で変更している。

GnuPG の鍵束を移行するのにも手間取った。
鍵束の移行は始めたやったのでコピペでやったのだけど、所有権の違いでエラーになって使えなかった。
結局コピペは推奨されない方法ではあるものの、所有権を移せば利用できるようなので、今回はそれでしのいだ。

```powershell
# 管理者権限で
gpgconf --kill all
# backup の `gnupg` を `$env:APPDATA/gnupg` に配置
takeown /F $env:APPDATA\gnupg /R /D Y
icacls $env:APPDATA\gnupg /reset /T
icacls $env:APPDATA\gnupg /inheritance:r
icacls $env:APPDATA\gnupg /grant:r takatoshi:F /T
```

これで多分全て設定終わってるはず。
まだ微妙に痒いところが残ってるけど、それは追々調整していくものとする。

結局 1 月は対して開発もせずに過ごしたので、 2 月はもうちょい開発したいな。
