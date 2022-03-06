{:title "Cascadia Code PL と Migu Nerd Font の違い"
:layout :post
:tags ["font"]}

注意: アイコンフォントのコピペを多用しているので、対応していないフォントを使われている豆腐が見えます。

### TL;DR

これはズブのフォント素人による try&error なので大いに間違っている可能性もある。

結論から言うとまだ納得の行く改造版 Migu の作成には至っていない。

Cascadia も Hack も Narrow な Powerline グリフが埋め込まれているっぽい。
ここに来て Migu に直接 Narrow な Powerline グリフを埋め込んだらええやんけ、という Nerd Fonts 回帰案が浮上した(うまく行った試しないのだけど)。

To be Continued...

### 経緯

長年 Migu を愛用している。

Migu の良さはそのスリムでシンプルな見栄えだけでなく、Proportional と Monospace で同じ字体を使えることだ。
しかし最近の開発環境で使うにはアイコンフォントが同梱されないことで不便を感じることが多い。特に Powerline を使っているとアイコンフォントは必須。
ということで 5 年ほど前から [iij/fontmerger](https://github.com/iij/fontmerger) を使って Nerd fonts などを追加した自作フォントを使っている。

しかしこれがここ 1,2 年くらいで Windows Terminal を使い始めたことで納得いかない点が出てきた。
"Ambiguous"なフォントについては全部 Narrow サイズになる ≒ Certain なフォントについてはサイズが適用される？だと？

[TermControl: force all ambiguous glyphs to be narrow by DHowett-MSFT · Pull Request #2928 · microsoft/terminal](https://github.com/microsoft/terminal/pull/2928/commits)

Windows Terminal とともに提供される [Cascadia](https://github.com/microsoft/cascadia-code) に関してはどうもこの問題が発生しない。**Powerline グリフに関しては完璧なのだ**。

フォント素人のわたしはこの時こう考えた。

改造 Migu と Cascadia でコードポイントは同じだけどフォントを切り替えるだけで表示サイズが異なってくる。
→ つまり Cascadia フォントでは、コードポイント`0xE0B0`とかになんかの情報を入れ込んでるのではないか？と...！

グリフのサイズを目視確認するための一覧用コード。

```powershell
# Helper function to show Unicode character
function U {
    param
    (
        [int] $Code
    )

    if ((0 -le $Code) -and ($Code -le 0xFFFF)) {
        return [char] $Code
    }

    if ((0x10000 -le $Code) -and ($Code -le 0x10FFFF)) {
        return [char]::ConvertFromUtf32($Code)
    }

    throw "Invalid character code $Code"
}

# 数値は[Convert]::ToInt32('0xE080', 16)でHEX変換する
((170..61278) | %{U $_}) -join ''
```

全然わからない...クソむずすぎる。でも特定のグリフを移植するだけなら、元々改造 Migu を作るのに使っていた fontmerger が利用できる。とおもてたら DEPRECATED になっておった、そら Python2 やからな...

とりあえず最終目標までの段階的目標を立てた。

- お亡くなりになられた fontmerger を Python3 化して動かす
- 最新の Migu に対して fontmerger で Nerd font patch する
- `0xE0B0` を始めとした Cascadia でだけうまく表示されるグリフを Migu に移植する
- 完璧な Migu の完成！

### 2021-03-13

Cascadia を CLI で簡単に落としてくる方法はないものか？

```powershell
Invoke-WebRequest -Uri https://github.com/microsoft/cascadia-code/releases/download/v2102.25/CascadiaCode-2102.25.zip -OutFile CascadiaCode-2102.25.zip
Expand-Archive -Path .\CascadiaCode-2102.25.zip -DestinationPath CascadiaCode-2102.25
cd .\CascadiaCode-2102.25\
```

```powershell
                                                                                                                   2021-03-13 15:27
 takatoshi  ~\.\CascadiaCode-2102.25  ll

        Directory: C:\Users\takatoshi\desktop\CascadiaCode-2102.25


Mode                LastWriteTime         Length Name
----                -------------         ------ ----
la---        2021-03-13     15:24                  otf
la---        2021-03-13     15:24                  ttf
la---        2021-03-13     15:24                  woff2

```

フォントの拡張子がわからなくなったのでおさらい。

- otf ... Open Type Font
  - Adobe と MS で作った。MS 商標。今や一般的。リガチャ使うならこっちしか無理
- ttf ... True Type Font
  - Apple 作った。プラットフォーム互換がない
- woff ... Web Open Font Format 2
  - Web

```powershell
 takatoshi  ~\.\CascadiaCode-2102.25  ll .\otf\static\

        Directory: C:\Users\takatoshi\desktop\CascadiaCode-2102.25\otf\static


Mode                LastWriteTime         Length Name
----                -------------         ------ ----
la---        2021-02-25     20:32         161908   CascadiaCode-Bold.otf
la---        2021-02-25     20:32         150456   CascadiaCode-ExtraLight.otf
la---        2021-02-25     20:32         158652   CascadiaCode-Light.otf
la---        2021-02-25     20:32         158244   CascadiaCode-Regular.otf
la---        2021-02-25     20:32         161960   CascadiaCode-SemiBold.otf
la---        2021-02-25     20:32         159372   CascadiaCode-SemiLight.otf
la---        2021-02-25     20:32         185320   CascadiaCodePL-Bold.otf
la---        2021-02-25     20:32         175352   CascadiaCodePL-ExtraLight.otf
la---        2021-02-25     20:32         181752   CascadiaCodePL-Light.otf
la---        2021-02-25     20:32         181376   CascadiaCodePL-Regular.otf
la---        2021-02-25     20:32         185540   CascadiaCodePL-SemiBold.otf
la---        2021-02-25     20:32         182740   CascadiaCodePL-SemiLight.otf
la---        2021-02-25     20:32         145316   CascadiaMono-Bold.otf
la---        2021-02-25     20:32         133864   CascadiaMono-ExtraLight.otf
la---        2021-02-25     20:32         142060   CascadiaMono-Light.otf
la---        2021-02-25     20:32         141652   CascadiaMono-Regular.otf
la---        2021-02-25     20:32         145368   CascadiaMono-SemiBold.otf
la---        2021-02-25     20:32         142780   CascadiaMono-SemiLight.otf
la---        2021-02-25     20:32         168588   CascadiaMonoPL-Bold.otf
la---        2021-02-25     20:32         158620   CascadiaMonoPL-ExtraLight.otf
la---        2021-02-25     20:32         165020   CascadiaMonoPL-Light.otf
la---        2021-02-25     20:32         164644   CascadiaMonoPL-Regular.otf
la---        2021-02-25     20:32         168808   CascadiaMonoPL-SemiBold.otf
la---        2021-02-25     20:32         166008   CascadiaMonoPL-SemiLight.otf
```

ここで対象になるのは MonoPL(monospace の PowerLine 版)。まだリガチャを受け入れるだけの心のゆとりができていない。

次に [Migu][https://mix-mplus-ipa.osdn.jp/migu/] を落とす。OSDN のリダイレクトかまされ迂回方法がわからないので手で落とした。

```powershell
 takatoshi  ~\desktop  cd .\migu-1m-20200307\
 takatoshi  ~\.\migu-1m-20200307  ll .\migu-1m-20200307\

        Directory: C:\Users\takatoshi\desktop\migu-1m-20200307\migu-1m-20200307


Mode                LastWriteTime         Length Name
----                -------------         ------ ----
la---        2021-03-13     15:39                  ipag00303
la---        2021-03-13     15:39                  mplus-TESTFLIGHT-063a
la---        2020-03-07     22:14        3401580   migu-1m-bold.ttf
la---        2020-03-07     22:12        3144556   migu-1m-regular.ttf
la---        2020-03-08     16:00           2344   migu-README.txt
```

fontmerger のスクリプトがちょっと Windows では使いにくそうに見えたので、一旦 Ubuntu(WSL2)でやる。

でも Ubuntu16LTS の fontforge は 2019 年までのやつで古かったので、一旦 WSL2 の Ubuntu を更新することにする。

```sh
sudo apt update
sudo apt upgrade
sudo do-release-upgrade
```

途中で sshd_config が編集されとんぞ！？と言われて新しいのとローカルのどっち使うか選ばねばいけなかった。ここは新しい方を有効化。この流れで Ubuntu20LTS まで上げた。
Ubuntu20 では fontforge はデフォルトのパッケージではなくなったので、パッケージソースを追加する必要がある。

[Ubuntu – Details of package fontforge in focal](https://packages.ubuntu.com/focal/x11/fontforge)

universe にあるのがわかったので追加。

```sh
sudo apt-add-repository universe
sudo apt update
sudo apt install fontforge
```

```sh
mkdir ./patched
./bin/fontmerger --all -o patched --suffix=with-icons -- migu-1m-regular.ttf migu-1m-bold.ttf migu-1c-regular.ttf migu-1c-bold.ttf
```

結果、無反応。

```sh
krymtkts@krymtkts-stealth:/mnt/c/Users/takatoshi/dev/github.com/krymtkts/fontmerger$ /usr/bin/fontforge -script fontmerger/__init__.py
 --all -o patched --suffix=with-icons -- migu-1m-regular.ttf migu-1m-bold.ttf migu-1c-regular.ttf migu-1c-bold.ttf
Copyright (c) 2000-2020. See AUTHORS for Contributors.
 License GPLv3+: GNU GPL version 3 or later <http://gnu.org/licenses/gpl.html>
 with many parts BSD <http://fontforge.org/license.html>. Please read LICENSE.
 Version: 20190801
 Based on sources from 03:10 UTC  6-Mar-2020-ML-D-GDK3.
  File "fontmerger/__init__.py", line 103
    except Exception, e:
                    ^
SyntaxError: invalid syntax
Error in sys.excepthook:
Traceback (most recent call last):
  File "/usr/lib/python3.8/subprocess.py", line 64, in <module>
    import msvcrt
ModuleNotFoundError: No module named 'msvcrt'

During handling of the above exception, another exception occurred:

Traceback (most recent call last):
  File "/usr/lib/python3/dist-packages/apport_python_hook.py", line 72, in apport_excepthook
    from apport.fileutils import likely_packaged, get_recent_crashes
  File "/usr/lib/python3/dist-packages/apport/__init__.py", line 5, in <module>
    from apport.report import Report
  File "/usr/lib/python3/dist-packages/apport/report.py", line 12, in <module>
    import subprocess, tempfile, os.path, re, pwd, grp, os, time, io
  File "/usr/lib/python3.8/subprocess.py", line 69, in <module>
    import _posixsubprocess
ModuleNotFoundError: No module named '_posixsubprocess'

Original exception was:
  File "fontmerger/__init__.py", line 103
    except Exception, e:
                    ^
SyntaxError: invalid syntax
```

あー、Python2 の構文によるエラーね。解消して再実行してみる。引数も間違ってたし。

```sh
krymtkts@krymtkts-stealth:/mnt/c/Users/takatoshi/dev/github.com/krymtkts/fontmerger$ /usr/bin/fontforge -script fontmerger/__init__.py --all -o patched --suffix=with-icons -- ./source/migu-1m-regular.ttf ./source/migu-1m-bold.ttf ./source/migu-1c-regular.ttf ./source/migu-1c-bold.ttf
Copyright (c) 2000-2020. See AUTHORS for Contributors.
 License GPLv3+: GNU GPL version 3 or later <http://gnu.org/licenses/gpl.html>
 with many parts BSD <http://fontforge.org/license.html>. Please read LICENSE.
 Version: 20190801
 Based on sources from 03:10 UTC  6-Mar-2020-ML-D-GDK3.
The following table(s) in the font have been ignored by FontForge
  Ignoring 'EPAR'
The glyph named asterisk is mapped to U+F069.
  But its name indicates it should be mapped to U+002A.
The glyph named plus is mapped to U+F067.
  But its name indicates it should be mapped to U+002B.
The glyph named question is mapped to U+F128.
  But its name indicates it should be mapped to U+003F.
The glyph named minus is mapped to U+F068.
  But its name indicates it should be mapped to U+2212.
The glyph named heart is mapped to U+F004.
  But its name indicates it should be mapped to U+2665.
The glyph named home is mapped to U+F015.
  But its name indicates it should be mapped to U+21B8.
The glyph named check is mapped to U+F046.
  But its name indicates it should be mapped to U+2713.
The glyph named bell is mapped to U+F0A2.
  But its name indicates it should be mapped to U+2407.
The glyph named lessequal is mapped to U+F500.
  But its name indicates it should be mapped to U+2264.
The glyph named circle is mapped to U+F111.
  But its name indicates it should be mapped to U+25CB.
The glyph named smile is mapped to U+F118.
  But its name indicates it should be mapped to U+263A.
The glyph named frown is mapped to U+F119.
  But its name indicates it should be mapped to U+2322.
The glyph named bullseye is mapped to U+F140.
  But its name indicates it should be mapped to U+25CE.
The glyph named compass is mapped to U+F14E.
  But its name indicates it should be mapped to U+263C.
The glyph named female is mapped to U+F182.
  But its name indicates it should be mapped to U+2640.
The glyph named male is mapped to U+F183.
  But its name indicates it should be mapped to U+2642.
The glyph named sun is mapped to U+F185.
  But its name indicates it should be mapped to U+263C.
The glyph named venus is mapped to U+F221.
  But its name indicates it should be mapped to U+2640.
The glyph named slash is mapped to U+E016.
  But its name indicates it should be mapped to U+002F.
The glyph named pi is mapped to U+E02C.
  But its name indicates it should be mapped to U+03C0.
The glyph named ring is mapped to U+E03D.
  But its name indicates it should be mapped to U+02DA.
The glyph named infinity is mapped to U+E055.
  But its name indicates it should be mapped to U+221E.
The glyph named equal is mapped to U+E079.
  But its name indicates it should be mapped to U+003D.
The glyph named question is mapped to U+F02C.
  But its name indicates it should be mapped to U+003F.
The glyph named check is mapped to U+F03A.
  But its name indicates it should be mapped to U+2713.
The glyph named plus is mapped to U+F05D.
  But its name indicates it should be mapped to U+002B.
The glyph named x is mapped to U+F081.
  But its name indicates it should be mapped to U+0078.
The glyph named home is mapped to U+F08D.
  But its name indicates it should be mapped to U+21B8.
The glyph named ellipsis is mapped to U+F09A.
  But its name indicates it should be mapped to U+2026.
The glyph named bell is mapped to U+F0DE.
  But its name indicates it should be mapped to U+2407.
The following table(s) in the font have been ignored by FontForge
  Ignoring 'EPAR'
The glyph named asterisk is mapped to U+F069.
  But its name indicates it should be mapped to U+002A.
The glyph named plus is mapped to U+F067.
  But its name indicates it should be mapped to U+002B.
The glyph named question is mapped to U+F128.
  But its name indicates it should be mapped to U+003F.
The glyph named minus is mapped to U+F068.
  But its name indicates it should be mapped to U+2212.
The glyph named heart is mapped to U+F004.
  But its name indicates it should be mapped to U+2665.
The glyph named home is mapped to U+F015.
  But its name indicates it should be mapped to U+21B8.
The glyph named check is mapped to U+F046.
  But its name indicates it should be mapped to U+2713.
The glyph named bell is mapped to U+F0A2.
  But its name indicates it should be mapped to U+2407.
The glyph named lessequal is mapped to U+F500.
  But its name indicates it should be mapped to U+2264.
The glyph named circle is mapped to U+F111.
  But its name indicates it should be mapped to U+25CB.
The glyph named smile is mapped to U+F118.
  But its name indicates it should be mapped to U+263A.
The glyph named frown is mapped to U+F119.
  But its name indicates it should be mapped to U+2322.
The glyph named bullseye is mapped to U+F140.
  But its name indicates it should be mapped to U+25CE.
The glyph named compass is mapped to U+F14E.
  But its name indicates it should be mapped to U+263C.
The glyph named female is mapped to U+F182.
  But its name indicates it should be mapped to U+2640.
The glyph named male is mapped to U+F183.
  But its name indicates it should be mapped to U+2642.
The glyph named sun is mapped to U+F185.
  But its name indicates it should be mapped to U+263C.
The glyph named venus is mapped to U+F221.
  But its name indicates it should be mapped to U+2640.
The glyph named slash is mapped to U+E016.
  But its name indicates it should be mapped to U+002F.
The glyph named pi is mapped to U+E02C.
  But its name indicates it should be mapped to U+03C0.
The glyph named ring is mapped to U+E03D.
  But its name indicates it should be mapped to U+02DA.
The glyph named infinity is mapped to U+E055.
  But its name indicates it should be mapped to U+221E.
The glyph named equal is mapped to U+E079.
  But its name indicates it should be mapped to U+003D.
The glyph named question is mapped to U+F02C.
  But its name indicates it should be mapped to U+003F.
The glyph named check is mapped to U+F03A.
  But its name indicates it should be mapped to U+2713.
The glyph named plus is mapped to U+F05D.
  But its name indicates it should be mapped to U+002B.
The glyph named x is mapped to U+F081.
  But its name indicates it should be mapped to U+0078.
The glyph named home is mapped to U+F08D.
  But its name indicates it should be mapped to U+21B8.
The glyph named ellipsis is mapped to U+F09A.
  But its name indicates it should be mapped to U+2026.
The glyph named bell is mapped to U+F0DE.
  But its name indicates it should be mapped to U+2407.
The following table(s) in the font have been ignored by FontForge
  Ignoring 'EPAR'
The glyph named asterisk is mapped to U+F069.
  But its name indicates it should be mapped to U+002A.
The glyph named plus is mapped to U+F067.
  But its name indicates it should be mapped to U+002B.
The glyph named question is mapped to U+F128.
  But its name indicates it should be mapped to U+003F.
The glyph named minus is mapped to U+F068.
  But its name indicates it should be mapped to U+2212.
The glyph named heart is mapped to U+F004.
  But its name indicates it should be mapped to U+2665.
The glyph named home is mapped to U+F015.
  But its name indicates it should be mapped to U+21B8.
The glyph named check is mapped to U+F046.
  But its name indicates it should be mapped to U+2713.
The glyph named bell is mapped to U+F0A2.
  But its name indicates it should be mapped to U+2407.
The glyph named lessequal is mapped to U+F500.
  But its name indicates it should be mapped to U+2264.
The glyph named circle is mapped to U+F111.
  But its name indicates it should be mapped to U+25CB.
The glyph named smile is mapped to U+F118.
  But its name indicates it should be mapped to U+263A.
The glyph named frown is mapped to U+F119.
  But its name indicates it should be mapped to U+2322.
The glyph named bullseye is mapped to U+F140.
  But its name indicates it should be mapped to U+25CE.
The glyph named compass is mapped to U+F14E.
  But its name indicates it should be mapped to U+263C.
The glyph named female is mapped to U+F182.
  But its name indicates it should be mapped to U+2640.
The glyph named male is mapped to U+F183.
  But its name indicates it should be mapped to U+2642.
The glyph named sun is mapped to U+F185.
  But its name indicates it should be mapped to U+263C.
The glyph named venus is mapped to U+F221.
  But its name indicates it should be mapped to U+2640.
The glyph named slash is mapped to U+E016.
  But its name indicates it should be mapped to U+002F.
The glyph named pi is mapped to U+E02C.
  But its name indicates it should be mapped to U+03C0.
The glyph named ring is mapped to U+E03D.
  But its name indicates it should be mapped to U+02DA.
The glyph named infinity is mapped to U+E055.
  But its name indicates it should be mapped to U+221E.
The glyph named equal is mapped to U+E079.
  But its name indicates it should be mapped to U+003D.
The glyph named question is mapped to U+F02C.
  But its name indicates it should be mapped to U+003F.
The glyph named check is mapped to U+F03A.
  But its name indicates it should be mapped to U+2713.
The glyph named plus is mapped to U+F05D.
  But its name indicates it should be mapped to U+002B.
The glyph named x is mapped to U+F081.
  But its name indicates it should be mapped to U+0078.
The glyph named home is mapped to U+F08D.
  But its name indicates it should be mapped to U+21B8.
The glyph named ellipsis is mapped to U+F09A.
  But its name indicates it should be mapped to U+2026.
The glyph named bell is mapped to U+F0DE.
  But its name indicates it should be mapped to U+2407.
The following table(s) in the font have been ignored by FontForge
  Ignoring 'EPAR'
The glyph named asterisk is mapped to U+F069.
  But its name indicates it should be mapped to U+002A.
The glyph named plus is mapped to U+F067.
  But its name indicates it should be mapped to U+002B.
The glyph named question is mapped to U+F128.
  But its name indicates it should be mapped to U+003F.
The glyph named minus is mapped to U+F068.
  But its name indicates it should be mapped to U+2212.
The glyph named heart is mapped to U+F004.
  But its name indicates it should be mapped to U+2665.
The glyph named home is mapped to U+F015.
  But its name indicates it should be mapped to U+21B8.
The glyph named check is mapped to U+F046.
  But its name indicates it should be mapped to U+2713.
The glyph named bell is mapped to U+F0A2.
  But its name indicates it should be mapped to U+2407.
The glyph named lessequal is mapped to U+F500.
  But its name indicates it should be mapped to U+2264.
The glyph named circle is mapped to U+F111.
  But its name indicates it should be mapped to U+25CB.
The glyph named smile is mapped to U+F118.
  But its name indicates it should be mapped to U+263A.
The glyph named frown is mapped to U+F119.
  But its name indicates it should be mapped to U+2322.
The glyph named bullseye is mapped to U+F140.
  But its name indicates it should be mapped to U+25CE.
The glyph named compass is mapped to U+F14E.
  But its name indicates it should be mapped to U+263C.
The glyph named female is mapped to U+F182.
  But its name indicates it should be mapped to U+2640.
The glyph named male is mapped to U+F183.
  But its name indicates it should be mapped to U+2642.
The glyph named sun is mapped to U+F185.
  But its name indicates it should be mapped to U+263C.
The glyph named venus is mapped to U+F221.
  But its name indicates it should be mapped to U+2640.
The glyph named slash is mapped to U+E016.
  But its name indicates it should be mapped to U+002F.
The glyph named pi is mapped to U+E02C.
  But its name indicates it should be mapped to U+03C0.
The glyph named ring is mapped to U+E03D.
  But its name indicates it should be mapped to U+02DA.
The glyph named infinity is mapped to U+E055.
  But its name indicates it should be mapped to U+221E.
The glyph named equal is mapped to U+E079.
  But its name indicates it should be mapped to U+003D.
The glyph named question is mapped to U+F02C.
  But its name indicates it should be mapped to U+003F.
The glyph named check is mapped to U+F03A.
  But its name indicates it should be mapped to U+2713.
The glyph named plus is mapped to U+F05D.
  But its name indicates it should be mapped to U+002B.
The glyph named x is mapped to U+F081.
  But its name indicates it should be mapped to U+0078.
The glyph named home is mapped to U+F08D.
  But its name indicates it should be mapped to U+21B8.
The glyph named ellipsis is mapped to U+F09A.
  But its name indicates it should be mapped to U+2026.
The glyph named bell is mapped to U+F0DE.
  But its name indicates it should be mapped to U+2407.
```

警告出るがフォントの生成完了。開いてみるも、いやフォントが壊れてるわ...なんでや？

理由がわかった。fontmerger の実行後に出力先フォルダからフォントを移動するとプレビューできる。出力先フォルダに fontforge の謎のハンドルが残っている様子。

fontmerger の Python3 化が完了した。[GitHub - krymtkts/fontmerger: FontForge script for to merge any fonts](https://github.com/krymtkts/fontmerger)

### 2021-04-10

Cascadia からコピる範囲を決める。

```powershell
 takatoshi  ~\.\.\.\fontmerger   master ≣ +1 ~2 -6 !  [Convert]::ToInt32('0xE0A0', 16)
57504
 takatoshi  ~\.\.\.\fontmerger   master ≣ +1 ~2 -6 !  [Convert]::ToInt32('0xE0D4', 16)
57556                                                                                                             2021-04-10 15:17
 takatoshi  ~\.\.\.\fontmerger   master ≣ +1 ~2 -6 !  ((57504..57556) | %{U $_}) -join ''

```

Cascadia には Powerline の拡張グリフくらいしか入っていないので Powerline の部分を抜き出して Migu にコピーする。

```sh
/usr/bin/fontforge -script fontmerger/__init__.py --all -o patched --suffix=cascadia -- ./source/migu-1m-regular.ttf ./source/migu-1m-bold.ttf ./source/migu-1c-regular.ttf ./source/migu-1c-bold.ttf
```

結果は Cascadia からコピーした領域は以前 Ambiguous なままだった...なんでだろう。以下が追加の Cascadia からコピる設定。

```json
{
    "id": "cascadia-powerline",
    "name": "Cascadia Powerline Symbols",
    "description": "Powerline symbols copied from Cascadia Code. https://github.com/microsoft/cascadia-code",
    "filename": "./fonts/cascadia/CascadiaCodePL-Regular.otf",
    "unicode_range": [
      "E0A0",
      "E0D4"
    ]
  },
```

コピっても解消しないのか...と思ってた矢先、フォカのフォントを参考に調べていて [GitHub - yuru7/HackGen: HackGen is Japanese programming font which is a composed of Hack and GenJyuu-Gothic.](https://github.com/yuru7/HackGen) なるフォントを見つけた。

あれ、このフォント Powerline グリフが Windows Terminal でも崩れないし、見た目も結構好みでこれちょっとよいかも...と浮気しそうになるも、やはり字幅の広さが気に入らず Migu に返り咲くワイ。

それはそうと Powerline グリフが崩れないのはなんでか？と思ってみてたところ、これ Symbol フォントが Narrow スペースなのね。Cascadia もそう。つまり問題なのはフォント幅じゃね？と気づく。
更に々々、Nerd Fonts から提供されている Hack を使うと、PowerShell モジュールの Terminal Icons で表示されるファイルアイコンすらも小さくならずに表示できるではないかい！これやろ答え。

数年前に試してうまくいったことがないのが心配のタネだが、ここは原点回帰して Nerd Fonts の font-patcher で Migu に Narrow スペースでパッチしてみるか～という気持ちになった。

### 現状

ただ単に Migu の最新版にパッチしただけの状態になっているのだが、途中経過をまとめておかないと着手する時加齢に忘れてて辛いので一旦状況をまとめた。

- [x] お亡くなりになられた fontmerger を Python3 化して動かす
- [x] 最新の Migu に対して fontmerger で Nerd font patch する
- [x] `0xE0B0` を始めとした Cascadia でだけうまく表示されるグリフを Migu に移植する
  - そして効果なし！
- [ ] Nerd Fonts の font-patcher で Migu にパッチしてみる <- **NEW!!!**
- [ ] 完璧な Migu の完成！

俺たちの戦いはこれからだ！😭😭😭
