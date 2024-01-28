---
title: "Oh My Posh のテーマを更新する 2024"
tags: ["oh-my-posh", "powershell"]
---

[Oh My Posh](https://ohmyposh.dev/) が PowerShell だけの v2 から multi shell な v3 になったあと、 PowerShell から JSON の設定に乗り換えた。
2 年ほど前に JSON の自分テーマを作って以降放置してた。
[My own oh-my-posh v3 theme.](https://gist.github.com/krymtkts/d320ff5ec30fa47b138c2df018f95423)

その後設定ファイルに JSON だけじゃなく [YAML](https://yaml.org/) や [TOML](https://toml.io/en/) のサポートもされたのから随分時間が経った。
JSON もシンプルで良いのだけど、いかんせん最後の要素のカンマ調整がめんどくて。個人用途ならまず避けたい。
この機会に設定が微妙な部分とか、 JSON を YAML か TOML のどっちかに変えようと思った次第。

---

PowerShell はデフォで ConvertFrom-Json, ConvertTo-Json がある。
更に現時点の PowerShell Gallery には [powershell-yaml 0.4.7](https://www.powershellgallery.com/packages/powershell-yaml/0.4.7) と [PSToml 0.1.0](https://www.powershellgallery.com/packages/PSToml/0.1.0) がある。
JSON を YAML や TOML に変換するのは容易い。

Oh My Posh の設定ファイルを変換するには以下のようにする。

```powershell
# JSON to YAML.
Get-Content "$env:USERPROFILE/.oh-my-posh.omp.json" | ConvertFrom-Json | ConvertTo-Yaml | Set-Content -Path "$env:USERPROFILE/.oh-my-posh.omp.yaml"

# JSON to TOML. use Depth option to avoid truncated result.
Get-Content "$env:USERPROFILE/.oh-my-posh.omp.json" | ConvertFrom-Json | ConvertTo-Toml -Depth 10 | Set-Content -Path "$env:USERPROFILE/.oh-my-posh.omp.toml"
```

個人的に TOML は ini ライクで結構好きなのだけど、こと Oh My Posh の設定ファイルに使うと読みにくすぎる気がした。
先に挙げた自作テーマ JSON は 150 行あるのだけど、 YAML だと 103 行、 TOML だと 134 行になった。
これらはすべて最終行の改行含む。 TOML の場合はヘッダーの前に空行を入れたいので TOML だけ行数での比較は不利な感じだけど。

Oh My Posh の設定は [Block](https://ohmyposh.dev/docs/configuration/block) Array と各 block に入れ子構造の [Segment](https://ohmyposh.dev/docs/configuration/segment) Array や更に入れ子になった [properties](https://ohmyposh.dev/docs/configuration/segment#properties) がいる。
これを TOML に置き換えたら [Array of Tables](https://toml.io/en/v1.0.0#array-of-tables) が頻出して、個人的にこれが見にくいと感じるところかな。
TOML が階層と繰り返し構造の組み合わせだとこんなに読みにくくなるとは気づかなかった。
新たな発見や。

比較として、以下に部分的に一覽してみる。
JSON で [PUA](https://en.wikipedia.org/wiki/Private_Use_Areas) に割り当てられた文字を直書きしてたところは、変換の際にキャラクターコードへと変換されてた。
(アイコングリフが同梱されたフォントを使ってなかったり、自分専用のコードポイントに当たってる箇所は豆腐で表示されるだろうがご了承いただきたい。)

```json
{
  "$schema": "https://raw.githubusercontent.com/JanDeDobbeleer/oh-my-posh/main/themes/schema.json",
  "blocks": [
    {
      "alignment": "left",
      "newline": true,
      "segments": [
        {
          "foreground": "#839496",
          "style": "plain",
          "type": "text"
        }
      ],
      "type": "prompt"
    },
    {
      "alignment": "left",
      "segments": [
        {
          "background": "#002b36",
          "foreground": "#839496",
          "properties": {
            "home_icon": "🏠",
            "style": "mixed"
          },
          "style": "diamond",
          "template": "{{ .Path }} ",
          "type": "path"
        },
        {
          "background": "#859900",
          "foreground": "#002b36",
          "powerline_symbol": "\ue0bc",
          "properties": {
            "azure_devops_icon": "󿴄 ",
            "fetch_stash_count": true,
            "fetch_status": true,
            "fetch_upstream_icon": true,
            "fetch_worktree_count": true,
            "no_commits_icon": "󿖕 "
          },
          "style": "powerline",
          "template": " {{ .HEAD }} {{ .BranchStatus }}{{ if .Working.Changed }} \uf044 {{ .Working.String }}{{ end }}{{ if and (.Staging.Changed) (.Working.Changed) }} |{{ end }}{{ if .Staging.Changed }} \uf046 {{ .Staging.String }}{{ end }}{{ if gt .StashCount 0}}󿚓 {{ .StashCount }}{{ end }}{{ if gt .WorktreeCount 0}} \uf1bb {{ .WorktreeCount }}{{ end }} ",
          "type": "git"
        },
        {
          "background": "#002b36",
          "background_templates": [
            "{{ if gt .Code 0 }}red{{ end }}"
          ],
          "foreground": "#d33682",
          "powerline_symbol": "\ue0bc",
          "properties": {
            "always_enabled": true
          },
          "style": "powerline",
          "template": " {{ if gt .Code 0 }}󿮋  {{ .Meaning }}{{ else }}\uf004 {{ end }} ",
          "type": "exit"
        }
      ],
      "type": "prompt"
    },
```

```yaml
$schema: https://raw.githubusercontent.com/JanDeDobbeleer/oh-my-posh/main/themes/schema.json
blocks:
  - alignment: left
    newline: true
    segments:
      - foreground: "#839496"
        style: plain
        type: text
    type: prompt
  - alignment: left
    segments:
      - background: "#002b36"
        foreground: "#839496"
        properties:
          home_icon: "\U0001F3E0"
          style: mixed
        style: diamond
        template: "{{ .Path }} "
        type: path
      - background: "#859900"
        foreground: "#002b36"
        powerline_symbol: 
        properties:
          azure_devops_icon: "\U000FFD04 "
          fetch_stash_count: true
          fetch_status: true
          fetch_upstream_icon: true
          fetch_worktree_count: true
          no_commits_icon: "\U000FF595 "
        style: powerline
        template: " {{ .HEAD }} {{ .BranchStatus }}{{ if .Working.Changed }}  {{ .Working.String }}{{ end }}{{ if and (.Staging.Changed) (.Working.Changed) }} |{{ end }}{{ if .Staging.Changed }}  {{ .Staging.String }}{{ end }}{{ if gt .StashCount 0}}\U000FF693 {{ .StashCount }}{{ end }}{{ if gt .WorktreeCount 0}}  {{ .WorktreeCount }}{{ end }} "
        type: git
      - background: "#002b36"
        background_templates:
          - "{{ if gt .Code 0 }}red{{ end }}"
        foreground: "#d33682"
        powerline_symbol: 
        properties:
          always_enabled: true
        style: powerline
        template: " {{ if gt .Code 0 }}\U000FFB8B  {{ .Meaning }}{{ else }} {{ end }} "
        type: exit
    type: prompt
```

```toml
"$schema" = "https://raw.githubusercontent.com/JanDeDobbeleer/oh-my-posh/main/themes/schema.json"
console_title_template = "{{if .Root}}Admin: {{end}} {{.Folder}}"
final_space = true
version = 2

[[blocks]]
alignment = "left"
newline = true
type = "prompt"

[[blocks.segments]]
foreground = "#839496"
style = "plain"
type = "text"

[[blocks]]
alignment = "left"
type = "prompt"

[[blocks.segments]]
background = "#002b36"
foreground = "#839496"
style = "diamond"
template = "{{ .Path }} "
type = "path"

[blocks.segments.properties]
home_icon = "🏠"
style = "mixed"

[[blocks.segments]]
background = "#859900"
foreground = "#002b36"
powerline_symbol = ""
style = "powerline"
template = " {{ .HEAD }} {{ .BranchStatus }}{{ if .Working.Changed }}  {{ .Working.String }}{{ end }}{{ if and (.Staging.Changed) (.Working.Changed) }} |{{ end }}{{ if .Staging.Changed }}  {{ .Staging.String }}{{ end }}{{ if gt .StashCount 0}}󿚓 {{ .StashCount }}{{ end }}{{ if gt .WorktreeCount 0}}  {{ .WorktreeCount }}{{ end }} "
type = "git"

[blocks.segments.properties]
azure_devops_icon = "󿴄 "
fetch_stash_count = true
fetch_status = true
fetch_upstream_icon = true
fetch_worktree_count = true
no_commits_icon = "󿖕 "

[[blocks.segments]]
background = "#002b36"
background_templates = ["{{ if gt .Code 0 }}red{{ end }}"]
foreground = "#d33682"
powerline_symbol = ""
style = "powerline"
template = " {{ if gt .Code 0 }}󿮋  {{ .Meaning }}{{ else }} {{ end }} "
type = "exit"

[blocks.segments.properties]
always_enabled = true
```

個人的にはこの中なら YAML が一番ましかな...ということで YAML でいくことにした。

インデントすれば TOML も見やすくなるだろうけど、 JSON や YAML で不要な table 名の繰り返しがきつい。

あと VS Code だと保存時の format で好きな項目順に並べられないしインデントも消失する。
フォーマットなしで保存するようにしたらいいのかも知れんが、今どき自動フォーマットに従わないのもカッコ悪い。
YAML ならインデントが階層構造を示すし、保存時に項目が並び替わらなかったので、その点も良い。

Oh My Posh の JSON はコメント書けたけど、 VS Code で編集する時いちいち言語を JSONC に変えないとエラー表示されて面倒だったが、 YAML ならこの悩みはない。

ひとまず以下のようにした。

```yaml
$schema: https://raw.githubusercontent.com/JanDeDobbeleer/oh-my-posh/main/themes/schema.json

version: 2
console_title_template: "{{ if .Root }}Admin: {{ end }} {{ .Folder }}"
final_space: true
blocks:
  # First line left.
  - type: prompt
    newline: true
    alignment: left
    segments:
      - type: text
        style: plain
  - type: prompt
    alignment: left
    segments:
      - type: os
        background: "#002b36"
        foreground: "#2aa198"
        style: diamond
        trailing_diamond: 
        template: "{{ if .WSL }}  at {{ end }}{{ .Icon }} "
      - type: root
        background: "#002b36"
        foreground: "#b58900"
        style: diamond
        trailing_diamond: 
        template: ""
      - type: session
        background: "#2aa198"
        foreground: "#073642"
        style: diamond
        trailing_diamond: 
        properties:
          display_host: false
        template: " {{ .UserName }} "
      - type: path
        background: "#002b36"
        foreground: "#839496"
        properties:
          style: mixed
        style: diamond
        template: "{{ .Path }} "
      - type: git
        background: "#859900"
        foreground: "#002b36"
        style: powerline
        powerline_symbol: 
        properties:
          fetch_stash_count: true
          fetch_status: true
          fetch_upstream_icon: true
          fetch_worktree_count: true
          github_icon: " "
          bitbucket_icon: " "
          no_commits_icon: "󿖕 "
        template: " {{ .UpstreamIcon }} {{ .HEAD }} {{ if .BranchStatus }}{{ .BranchStatus }}{{ end }}{{ if .Working.Changed }}  {{ .Working.String }}{{ end }}{{ if and (.Staging.Changed) (.Working.Changed) }} |{{ end }}{{ if .Staging.Changed }}  {{ .Staging.String }}{{ end }}{{ if gt .StashCount 0}}󿚓 {{ .StashCount }}{{ end }}{{ if gt .WorktreeCount 0}}  {{ .WorktreeCount }}{{ end }} "
      - type: text
        background: "#002b36"
        foreground: "#d33682"
        style: powerline
        powerline_symbol: 
        background_templates:
          - "{{ if gt .Code 0 }}#dc322f{{ end }}"
        foreground_templates:
          - "{{ if gt .Code 0 }}#002b36{{ end }}"
        properties:
          always_enabled: true
        template: " {{ if gt .Code 0 }}󿮋 {{ else }} {{ end }} "
  # First line right.
  - type: prompt
    alignment: right
    segments:
      - type: shell
        foreground: "#93a1a1"
        style: diamond
        template: "󿚍 {{ .Name }}"
      - type: time
        background: "#002b36"
        foreground: "#839496"
        style: diamond
        leading_diamond: 
        trailing_diamond: 
        properties:
          time_format: 15:04:05 MST
        template: "{{ .CurrentDate | date .Format }}"
      - type: executiontime
        background: "#073642"
        foreground: "#839496"
        style: diamond
        properties:
          always_enabled: true
        template: " {{ .FormattedMs }}"

  # Second line.
  - type: prompt
    newline: true
    alignment: left
    segments:
      - type: text
        foreground: "#b58900"
        style: plain
        template: ">"
```

要らない設定を削り、 2 行レイアウトの 1 行目に情報を詰め込んでみた。
最近 Powerline じゃなくてもいいかもなと感じることあるのだけど、やはり視認性ではこちらに分があるので、情報表示する 1 行目は踏襲。
2 行目は限りなくシンプルに。

2 行目に慣れてきたら背景色なくして、文字色やアイコングリフだけでも視認性よく感じるかもなー。
