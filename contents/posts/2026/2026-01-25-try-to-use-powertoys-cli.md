---
title: "PowerToys CLI を使ってみる"
tags: ["powertoys", "powershell"]
---

[PowerToys 0.97 is here: a big Command Palette update and a new mouse utility - Windows Command Line](https://devblogs.microsoft.com/commandline/powertoys-0-97-is-here-a-big-command-palette-update-and-a-new-mouse-utility/)

> In the last release we added CLI support for Peek, and we’re expanding that even further. FancyZones, Image Resizer, and File Locksmith can now all be controlled from the command line. Whether you want to switch layouts, resize a batch of images, or unlock files, it’s all possible through the CLI. Be sure to check the docs for the full list of supported commands.

どうも Fancy Zones とかを CLI 経由で呼べるようになったぽいのよね。想像通りであれば起動 script として有用なのか判断するため調査したい。

- [FancyZones Window Manager for Windows - PowerToys | Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/fancyzones#command-line-reference)
- [File Locksmith Utility for Windows - PowerToys | Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/file-locksmith#command-line-reference)
- [Image Resizer utility for Windows - PowerToys | Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/image-resizer#command-line-reference)
- [PowerToys Peek Utility - Preview Files Without Opening Apps | Microsoft Learn](https://learn.microsoft.com/en-us/windows/powertoys/peek#use-peek-from-the-command-line)

調べてみたら 0.97.0 から CLI 専用の実行ファイル `FancyZoneCli.exe` `FileLocksmithCLI.exe` が生えて、それを経由して使えるってことみたい。
`PowerToys.ImageResizerCLI.exe` と `PowerToys.Peek.UI.exe` は見当たらなかった。使ってないからなのか、謎。

```powershell
> ll $env:ProgramFiles -filter *powertoys* | ll -Filter *CLI.exe

        Directory: C:\Program Files\PowerToys


Mode                LastWriteTime         Length Name
----                -------------         ------ ----
-a---        2026-01-19     12:07         166984 󿬔  FancyZonesCLI.exe
-a---        2026-01-19     12:07         726560 󿬔  FileLocksmithCLI.exe
```

因みに上記 `ll` を `Get-ChildItem` の alias にしてる。

```powershell
> Get-Alias ll

CommandType     Name                                               Version    Source
-----------     ----                                               -------    ------
Alias           ll -> Get-ChildItem
```

場所はわかったが、ここには大量のファイルがあるので `Path` を通したくない感じ。

```powershell
> ll $env:ProgramFiles -filter *powertoys* | ll | measure

Count             : 703
Average           :
Sum               :
Maximum           :
Minimum           :
StandardDeviation :
Property          :
```

ひとまず `cd` して使ってみることにする。
早速 Fancy Zones を試す。

```powershell
> ./FancyZonesCLI --help
FancyZones CLI - Command line interface for FancyZones

Usage: FancyZonesCLI [command] [options]

Options:

Commands:
  open-editor, e                 Launch FancyZones layout editor
  get-monitors, m                List monitors and FancyZones metadata
  get-layouts, ls                List available layouts
  get-active-layout, active      Show currently active layout
  set-layout <layout>, s         Set layout by UUID or template name
  open-settings, settings        Open FancyZones settings page
  get-hotkeys, hk                List all layout hotkeys
  set-hotkey <key> <layout>, shk Assign hotkey (0-9) to a custom layout
  remove-hotkey <key>, rhk       Remove hotkey assignment

Examples:
  FancyZonesCLI --help
  FancyZonesCLI --version
  FancyZonesCLI get-monitors
  FancyZonesCLI set-layout focus
  FancyZonesCLI set-layout <uuid> --monitor 1
  FancyZonesCLI get-hotkeys
```

参照系だけ使ってみるか。

```powershell
> ./FancyZonesCLI get-layouts
=== Built-in Template Layouts (6 total) ===

[T1] blank
    Zones: 0

    Visual Preview:
    (No zones)

[T2] focus
    Zones: 3

    Visual Preview:
    +-------+
    |       |
    | +-------+
    +-|       |
      | +-------+
      +-|       |
        ...
        (total: 3 zones)
        ...
        | +-------+
        +-|       |
          |       |
          +-------+

[T3] columns
    Zones: 3, Spacing: 16px

    Visual Preview:
    +---------+---------+---------+
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    +---------+---------+---------+

[T4] rows
    Zones: 3, Spacing: 16px

    Visual Preview:
    +-----------------------------+
    |                             |
    +-----------------------------+
    |                             |
    +-----------------------------+
    |                             |
    +-----------------------------+

[T5] grid
    Zones: 2

    Visual Preview:
    +--------------+--------------+
    |              |              |
    |              |              |
    |              |              |
    |              |              |
    |              |              |
    |              |              |
    |              |              |
    +--------------+--------------+

[T6] priority-grid
    Zones: 3, Spacing: 16px

    Visual Preview:
    +---------+---------+---------+
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    |         |         |         |
    +---------+---------+---------+


=== Custom Layouts (1 total) ===
[1] Cockpit
    UUID: {04599F6F-0BB0-46DF-9809-2E631E026DBC}
    Type: grid (1x2 grid)

    Visual Preview:
    +---------------------------------------+
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    |                   |                  |
    +---------------------------------------+

Use 'FancyZonesCLI.exe set-layout <UUID>' to apply a layout.
```

ほー。なんか人間が目で見て判断する系の出力してる。自動化には向かなそう。
(Monitor Instance と Serial Number 等、一部の情報は伏せる)

```powershell
> ./FancyZonesCLI get-monitors
=== Monitors (2 total) ===

Monitor 1:
  Monitor: SHP1493
  Monitor Instance: XXXXXXXXXXXXXXXXXXXXXX
  Monitor Number: 1
  Serial Number: XXXXXXXXXXXX
  Virtual Desktop: {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}
  DPI: 192
  Resolution: 1600x900
  Work Area: 1600x900
  Position: (0, 0)
  Selected: False

  Active Layout: blank
  Zone Count: 0
  Sensitivity Radius: 0px
  Layout UUID: {2205FD22-B38D-4BA9-9550-2E59F6298B91}

Monitor 2:
  Monitor: DELA0F4
  Monitor Instance: XXXXXXXXXXXXXXXXXXXXXX
  Monitor Number: 2
  Serial Number: XXXXXXXXXXXX
  Virtual Desktop: {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}
  DPI: 96
  Resolution: 3840x1600
  Work Area: 3840x1600
  Position: (-332, -1600)
  Selected: True

  Active Layout: custom
  Zone Count: 3
  Sensitivity Radius: 20px
  Layout UUID: {04599F6F-0BB0-46DF-9809-2E631E026DBC}
```

```powershell
> ./FancyZonesCLI get-active-layout

=== Active FancyZones Layout(s) ===

Monitor 1: SHP1493
  Layout UUID: {2205FD22-B38D-4BA9-9550-2E59F6298B91}
  Layout Type: blank
  Zone Count: 0
  Sensitivity Radius: 0px

Monitor 2: DELA0F4
  Layout UUID: {04599F6F-0BB0-46DF-9809-2E631E026DBC}
  Layout Type: custom
  Zone Count: 3
  Spacing: -1px
  Sensitivity Radius: 20px
```

```powershell
> ./FancyZonesCLI get-hotkeys
=== Layout Hotkeys ===

Press Win + Ctrl + Alt + <number> to switch layouts:

  [0] => {04599F6F-0BB0-46DF-9809-2E631E026DBC}
```

わたしの想像力が欠如してるのかも知れんが、これいまだとできることあんまないなという感じがした。
指定の window を layout 内の何番の位置に飛ばすとかができたらいいかなと思ってたのだけど、そういうのは今ないみたい。
個人的には UWQHD 使ってるのもあり Fancy Zones の使い方として起動後に layout を切り替えることもないから、今提供されている CLI 機能はほぼ使わんな。
そういう細かな位置の調整は起動時に Workspaces でやってくれよってことなんかな。なんかいまいち同一アプリ複数 window を使いこなせなくて使ってないねんよな Workspaces 。

---

File Locksmith も見てみる。

```powershell
> ./FileLocksmithCLI --help
Usage: FileLocksmithCLI.exe [options] <path1> [path2] ...
Options:
  --kill      Kill processes locking the files
  --json      Output results in JSON format
  --wait      Wait for files to be unlocked
  --timeout   Timeout in milliseconds for --wait
  --help      Show this help message
```

コッチは subcommand 形式じゃないんかい。
でも `--json` があるお陰で使いやすくはあるか。
試しに、 Desktop に転がってた PDF を開いて lock の状況を出してみる。

```powershell
> ./FileLocksmithCLI ~/OneDrive/Desktop/sample.pdf --json
{"processes":[{"pid":3100,"name":"Acrobat.exe","user":"takatoshi","files":["C:\\Users\\takatoshi\\OneDrive\\Desktop\\sample.pdf"]}]}
```

```powershell
> ./FileLocksmithCLI C:\Users\takatoshi\OneDrive\Desktop\sample.pdf --json | ConvertFrom-Json | % processes

 pid name        user      files
 --- ----        ----      -----
3100 Acrobat.exe takatoshi {C:\Users\takatoshi\OneDrive\Desktop\sample.pdf}
```

File Locksmith は Fancy Zones に比べて楽に PowerShell と連携できそう。
ただ普段 File Locksmith のお世話にあんまならんからそんなに使わないだろうな。

---

取り敢えずざっと見てみてすぐ使いそうなものはないかな～という感覚は得た。
autocomplete を出力する option なんかもないから、使うなら自前で補助してやる必要がある。
ひとまずは適当な wrapper function を PowerShell の profile に仕込んでおくとかはやってもいいかもな。
