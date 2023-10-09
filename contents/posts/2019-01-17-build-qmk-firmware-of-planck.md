---
title: "Planckのキーマップをビルドする"
tags:  ["mechanicalkeyboard" "planck" "qmkfirmware"]
---

> 当時使ってたPCではプログラミングしなくなったのと、改めてQMK firmwareのdocument見たらbuild toolsんとこの記事が変わってるので、今度環境構築がてら再確認してfirmwareのとこだけ新しく書こうと思う。
>
> わたしのkeymapのrepo([krymtkts/qmk_firmware](https://github.com/krymtkts/qmk_firmware))は「デフォのキーマップをコピった」的なコミットを最後に止まってるので、旧PCに遺物が残されてそう...作り直したほうが早いわ😅
>
> 去年はWSLで書き込む方法が確か使えてたはずだけど今は非推奨になってて、MSYS2を使う方法が主流になった？？？謎い🤔

[前回](./2019-01-14-ortho-linear-keyboard-planck)のこの辺の続き。

### ビルド環境をセットアップする

ドキュメント[QMK Firmware - Installing Build Tools](https://docs.qmk.fm/#/getting_started_build_tools)に記載のWSLの手順をそのままでOK。ちょっとWSLを使いたかったのでMSYS2でのやつではあえてやらなかった。

WSL用のセットアップは`util/wsl_install.sh`で行う。

```bash
$ cd ./c/qmk_firmware/
$ ./util/wsl_install.sh
```

途中ドライバを全部入れる？と聞かれて、それは流石に...と思ったのでConnectedという接続したドライバだけ入れるやつ`C`にした。

Flip入れる？と聞かれたけどつかわないだろうし必要なときに、と思ったので`N`

`wsl_install.sh`完了後にPlanckのrev4のdefaultキーマップを試しにコンパイルする。

bashを再起動してサンプルのコマンドを実行する。

```bash
$ make planck/rev4:default
```

でバイナリが吐き出されたのでOK。


### Planckのfirmwareをビルドする

わたしのキーマップはこちら→[qmk\_firmware/keymap.c at master · krymtkts/qmk_firmware](https://github.com/krymtkts/qmk_firmware/blob/master/keyboards/planck/keymaps/krymtkts/keymap.c)

2017年末頃にビルドしたころとは`keyboards/planck/`配下のコードが結構変わってるようなので、作成済みのキーマップを削除して新しく`default`キーマップを作成する。それ用のシェルがあるので使う。

```bash
$ ./util/new_keymap.sh planck krymtkts
```

これで一旦ビルドする。revisionはわからんけど時期的に5だと思う。[要出典?]

```bash
$ make planck/rev5:krymtkts
```

コマンドラインでfirmwareを書き込む。revision5では以下のコマンドが正しい。

```bash
$ make planck/rev5:krymtkts:dfu
```

でもエラーになった。なんかエラーが無限に繰り返される。CTRL+Cで中断

```bash
$ make planck/rev5:krymtkts:dfu
QMK Firmware 0.6.228
Making planck/rev5 with keymap krymtkts and target dfu

avr-gcc (GCC) 4.9.2
Copyright (C) 2014 Free Software Foundation, Inc.
This is free software; see the source for copying conditions.  There is NO
warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

Size before:
text    data     bss     dec     hex filename
    0   26614       0   26614    67f6 .build/planck_rev5_krymtkts.hex

Compiling: keyboards/planck/keymaps/krymtkts/keymap.c                                               [OK]
Linking: .build/planck_rev5_krymtkts.elf                                                            [OK]
Creating load file for flashing: .build/planck_rev5_krymtkts.hex                                    [OK]
Copying planck_rev5_krymtkts.hex to qmk_firmware folder                                             [OK]
Checking file size of planck_rev5_krymtkts.hex                                                      [OK]
* The firmware size is fine - 26614/28672 (2058 bytes free)
Error: Bootloader not found. Trying again in 5s.
Error: Bootloader not found. Trying again in 5s.
Error: Bootloader not found. Trying again in 5s.
Error: Bootloader not found. Trying again in 5s.
Error: Bootloader not found. Trying again in 5s.
^Ctmk_core/avr.mk:141: recipe for target 'dfu' failed
make[1]: *** [dfu] Interrupt
Makefile:529: recipe for target 'planck/rev5:krymtkts:dfu' failed
make: *** [planck/rev5:krymtkts:dfu] Interrupt
```

エラーダイアログで「libusb0.dllがない」というような旨が表示される。WSLでやろうとしたからダメだったのか？

コマンドラインで焼くのを一旦諦めて、`qmk_tookbox.exe`を使うことにしたら一発成功、Planckに自分のキーマップを焼くことに成功した。

でもコマンドラインで焼けないのは困るので、PlanckのMakefileの`rules.mk`見てみたところ、

```makefile
ifeq ($(strip $(KEYBOARD)), planck/rev4)
    BOOTLOADER = atmel-dfu
endif
ifeq ($(strip $(KEYBOARD)), planck/rev5)
    BOOTLOADER = qmk-dfu
endif
```

とあって、rev4と4rev5でbootloaderが変わってることから、あれ？わたしのPlanckもしかしてrev4じゃ？と思って試してみたところ...

```bash
$ make planck/rev4:krymtkts:dfu
QMK Firmware 0.6.228
Making planck/rev4 with keymap krymtkts and target dfu

avr-gcc (GCC) 4.9.2
Copyright (C) 2014 Free Software Foundation, Inc.
This is free software; see the source for copying conditions.  There is NO
warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

Compiling: quantum/audio/muse.c                                                                     [OK]
Compiling: keyboards/planck/planck.c                                                                [OK]
Compiling: keyboards/planck/keymaps/krymtkts/keymap.c                                               [OK]
Compiling: quantum/quantum.c                                                                        [OK]
Compiling: quantum/keymap_common.c                                                                  [OK]
Compiling: quantum/keycode_config.c                                                                 [OK]
Compiling: quantum/matrix.c                                                                         [OK]
Compiling: quantum/process_keycode/process_audio.c                                                  [OK]
Compiling: quantum/process_keycode/process_clicky.c                                                 [OK]
Compiling: quantum/audio/audio.c                                                                    [OK]
Compiling: quantum/audio/voices.c                                                                   [OK]
Compiling: quantum/audio/luts.c                                                                     [OK]
Compiling: quantum/process_keycode/process_music.c                                                  [OK]
Compiling: tmk_core/common/host.c                                                                   [OK]
Compiling: tmk_core/common/keyboard.c                                                               [OK]
Compiling: tmk_core/common/action.c                                                                 [OK]
Compiling: tmk_core/common/action_tapping.c                                                         [OK]
Compiling: tmk_core/common/action_macro.c                                                           [OK]
Compiling: tmk_core/common/action_layer.c                                                           [OK]
Compiling: tmk_core/common/action_util.c                                                            [OK]
Compiling: tmk_core/common/print.c                                                                  [OK]
Compiling: tmk_core/common/debug.c                                                                  [OK]
Compiling: tmk_core/common/util.c                                                                   [OK]
Compiling: tmk_core/common/eeconfig.c                                                               [OK]
Compiling: tmk_core/common/report.c                                                                 [OK]
Compiling: tmk_core/common/avr/suspend.c                                                            [OK]
Compiling: tmk_core/common/avr/timer.c                                                              [OK]
Compiling: tmk_core/common/avr/bootloader.c                                                         [OK]
Assembling: tmk_core/common/avr/xprintf.S                                                           [OK]
Compiling: tmk_core/common/magic.c                                                                  [OK]
Compiling: tmk_core/protocol/lufa/lufa.c                                                            [OK]
Compiling: tmk_core/protocol/usb_descriptor.c                                                       [OK]
Compiling: tmk_core/protocol/lufa/outputselect.c                                                    [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Class/Common/HIDParser.c                                       [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/Device_AVR8.c                                        [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/EndpointStream_AVR8.c                                [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/Endpoint_AVR8.c                                      [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/Host_AVR8.c                                          [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/PipeStream_AVR8.c                                    [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/Pipe_AVR8.c                                          [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/USBController_AVR8.c                                 [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/AVR8/USBInterrupt_AVR8.c                                  [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/ConfigDescriptors.c                                       [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/DeviceStandardReq.c                                       [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/Events.c                                                  [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/HostStandardReq.c                                         [OK]
Compiling: lib/lufa/LUFA/Drivers/USB/Core/USBTask.c                                                 [OK]
Linking: .build/planck_rev4_krymtkts.elf                                                            [OK]
Creating load file for flashing: .build/planck_rev4_krymtkts.hex                                    [OK]
Copying planck_rev4_krymtkts.hex to qmk_firmware folder                                             [OK]
Checking file size of planck_rev4_krymtkts.hex                                                      [OK]
 * The firmware size is fine - 26614/28672 (2058 bytes free)
Bootloader Version: 0x00 (0)
Erasing flash...  Success
Checking memory from 0x0 to 0x6FFF...  Empty.
Checking memory from 0x0 to 0x67FF...  Empty.
0%                            100%  Programming 0x6800 bytes...
[>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>]  Success
0%                            100%  Reading 0x7000 bytes...
[>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>]  Success
Validating...  Success
0x6800 bytes written into 0x7000 bytes memory (92.86%).
```

成功した。時期的にrev5だと思ってたけどrev4だったとは😅まあうまくいってよかった。

キーマップはまだしっくり来ていないのでちまちま更新する予定。