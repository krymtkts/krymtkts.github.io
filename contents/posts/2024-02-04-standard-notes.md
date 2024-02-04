---
title: "Standard Notes を使いはじめた"
tags: ["standard-notes", "powershell"]
---

去年末頃から [Standard Notes](https://standardnotes.com/) を使いはじめた。

長らく [Evernote](https://evernote.com/) を使ってたのだけど、 E2E Encryption 対応したメモサービスに乗り換えたいとずっと思ってた。思い立ったのは 2023 入った頃だったような記憶。

Evernote をずっと使ってたのは、汎用的なテキストエディタからアクセスできる拡張機能があったからだ。
昔 SublimeText 2 ~ 3 を使ってた頃は Evernote の拡張機能があって([bordaigorl/sublime-evernote](https://github.com/bordaigorl/sublime-evernote) かな？)とても優秀な拡張機能だった。
VS Code ではそこまで優秀じゃないが [michalyao/evermonkey](https://github.com/michalyao/evermonkey) があって、もうアクティブにメンテされてないが最近でもパッチを当てたら使えるので重宝してた。 [Patched version of converterplus.js of evermonkey.](https://gist.github.com/krymtkts/8a5a3a5a7e1efe9db7f2c6bbda337571)

普段遣いするエディタからメモを探せたり書き足せたりできる体験は控えめに言っても最高で、とにかく便利なのだ。

でもいい加減サービス乗り換え時かなと感じて色々調べていた。
[Obsidian](https://obsidian.md/) は課金すれば E2E Encryption を得られるけど、 VS Code から利用できないので汎用的なエディタからのアクセスを諦める必要があった。
他に [Skiff](https://skiff.com/) は無課金で E2E Encryption が得られるが汎用的なエディタを持ってなかった。 OSS なので気合を入れたら CLI コマンド作ったり VS Code 拡張作れるかな？と思ったが結構重いタスクやなあと思って気が乗らず。
あとメモを置きたいだけなのに対してサービスが多いのも気になった(メールのエイリアス機能は超便利なので調査とかで使ったりする)。

そうこうしているうちに 2023 後半頃、なんのきっかけか忘れたが Standard Notes を知った(スラドのコメントでかな？)。
無課金で E2E Encryption が得られるのと、 OSS で CLI が既にあって、これはいけそうやなと確信した。
[jonhadfield/sn-cli: a command line interface for standard notes](https://github.com/jonhadfield/sn-cli)

試しに sn-cli をちょっとの間使ってみたけど、結構いい感じがする。 CLI でデータが抜けさえすれば VS Code にそれを投げるのも容易いから、ひと手間増えるが便利なのには変わりない。
もっとこなれてきたら sn-cli に依存した VS Code 拡張を書くなんかも考えられる。全部フルで作るよりは重くないだろう。

---

sn-cli の利用に際して、 README.md に書かれてるのとは違うインストール方法にしているのと、PowerShell で便利関数の提供をしている感じ。

インストールはバイナリをダウンロードするのがだるいので、 `go install` で `latest` をビルドしている。
他にも Go 系のツールをまとめて install / update する関数を PowerShell にこさえていたので、そこにパスを足しただけ。

```powershell
function Install-GoModules {
    $mods = @(
        'github.com/x-motemen/ghq@latest'
        'mvdan.cc/sh/v3/cmd/shfmt@latest'
        'github.com/jonhadfield/sn-cli/cmd/sncli@latest'
    )
    $mods | ForEach-Object {
        $start = $_.LastIndexOf('/') + 1
        $name = $_.Substring($start, $_.Length - '@latest'.Length - $start)
        if (-not (Get-Command "*$name*" -ErrorAction SilentlyContinue)) {
            go install $_
        }
    }
}

function Update-GoModules {
    if (-not (Get-Command go -ErrorAction SilentlyContinue)) {
        Write-Error "Install go with command below. 'choco install golang -y'"
        return
    }
    ll $env:GOPATH/bin | ForEach-Object {
        go version -m $_
    } | Where-Object {
        $_ -like '*path*'
    } | ConvertFrom-StringData -Delimiter "`t" | Select-Object -ExpandProperty Values | ForEach-Object {
        go install "${_}@latest"
    }
}
```

この方法だとビルド後のコマンドの名前が変わってしまうので、そこは `Set-Alias` で吸収する。
あと autocomplete を作っておいたのと、まだ貧弱だが Standard Notes の note を開くための関数を作成した。

2023 末は bash の autocomplete しかなかった。依存パッケージの [urfave/cli](https://github.com/urfave/cli) が古いものを使ってたからだ。 PowerShell の autocomplete は bash のものをそのまま使えるから自分で書いた。
でも今日見たら PowerShell 版も提供されてた。 [sn-cli/autocomplete/powershell_autocomplete.ps1](https://github.com/jonhadfield/sn-cli/blob/24f49af4729c9f5e29b0cd27f94803018f12beea/autocomplete/powershell_autocomplete.ps1)

```powershell
Set-Alias sn -Value sncli -Option AllScope
if (Get-Command -Name sn -ErrorAction SilentlyContinue) {
    Register-ArgumentCompleter -Native -Command sn -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorPosition)
        Invoke-Expression "$commandAst --generate-bash-completion" | ForEach-Object {
            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)
        }
    }

    # NOTE: require `sncli session --add` before use this.
    function Open-SnNotes {
        param (
            [Parameter(Mandatory,
                Position = 0,
                ValueFromPipeline = $true,
                ValueFromPipelineByPropertyName = $true)]
            [ValidateNotNullOrEmpty()]
            [String]
            $Title
        )
        $n = sn --use-session get note --title $Title
        if ($n -and ($n -notlike 'no matches*')) {
            $n | ConvertFrom-Json | Select-Object -ExpandProperty items | ForEach-Object {
                $_.content.text
            } | code -
        }
    }
}
```

この sn-cli の難点があるとしたら、それは検索ワードを入れないと全件ノートを取ってしまうところ。そのあたりは PowerShell の関数でカバーできるから、もうちょっと制限を入れていきたい。
考えてみたら複数ノートから対象を選ぶのは [krymtkts/pocof](https://github.com/krymtkts/pocof) と相性良さそう。
あとメモの追加と更新あたりの PowerShell 関数も書いていきたい。

今は Evernote のメモの中でも重要なやつだけを Standard Notes に移行した段階で、まだ両方とも利用している。
移行ツールも OSS であるのでそれを使ってもいいのだけど、この際なのでメモの棚卸し兼ねていまは手動でちまちま移している感じ。
全部終わった暁には先に上げた関数を実装して、 VS Code での利用もこなれてきたら更にその先へ、って感じかなー。
