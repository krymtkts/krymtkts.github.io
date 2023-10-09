---
title: "Textile を Markdown に変換する(いい感じに)"
tags: ["pandoc", "powershell"]
---

プレーンテキストで日記をつけ始めて今年で 9 年目になったぽい。
その日記だが、昔 Redmine を使ってたこともあって、最初の 5 年位の間 [Textile](https://textile-lang.com/) で日記を書いていた時期がある。
この頃の文書を Markdown に変換したいと度々思っていたが、最近重い腰を上げて取り組み始めた。

マークアップ言語の相互変換といえば、やはり Haskell で書かれた [Pandoc](https://pandoc.org/) やろーというのが個人的なイメージ。簡単なケースで永らく使ってきたが、今回は困ったというか一筋縄ではいかない点が出てきた。

例えば、 Pandoc で過去の日記を Textile から Markdown に変換するとしたらこんな感じのコマンドになる。

(以降は視認性を確保するため、実際に実行したコマンドに改行を含めて記載する)

```powershell
pandoc --from=textile --to=gfm+east_asian_line_breaks `
       --shift-heading-level-by=-4 --eol=lf --wrap=preserve `
       ./diary.textile --output=./diary.md
```

参照 [Pandoc - Pandoc User’s Guide](https://pandoc.org/MANUAL.html)

結構いい感じの結果を導くオプションの組み合わせに手間取った。

- 改行に半角スペース 2 つを使い、表の syntax も使うので `gfm+east_asian_line_breaks`
- 見出しのレベルを変えるための `shift-heading-level-by=-4` を指定
  - Redmine の見た目上 `h5.` にしてた(確か)
- 改行コードは LF で統一する `--eol=lf`
- 長い行に空白があると折り返そうとするので `--wrap=preserve` 原文を維持する

仮に日記が以下のような記述とする。

```plaintext
h5. 見出し

# 順序付きリスト1
** 箇条書きリスト1-1
# 順序付きリスト2
** bullet listアイテム2-1xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
* 箇条書きリスト3
## 箇条書きリスト3-1
箇条書きリスト3-1の 改行したコンテンツ

* *箇条書きリスト2*
箇条書きリスト2。
|_.行1|1|
|_.行2|20|

* *箇条書きリスト3*
<pre>
コード
</pre>
テキスト

|_.行1|1|
|_.行2|20|

    |_.行1|1|
    |_.行2|20|

<pre><code class='sh'>
コード
</code></pre>
```

これを先述のコマンドで Textile から Markdown に変換すると...

```md
# 見出し

\# 順序付きリスト1

箇条書きリスト1-1

\# 順序付きリスト2

bullet listアイテム2-1xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

-   箇条書きリスト3
    1.  箇条書きリスト3-1  
        箇条書きリスト3-1の 改行したコンテンツ

<!-- -->

-   **箇条書きリスト2**  
    箇条書きリスト2。  
    \|\_.行1\|1\|  
    \|\_.行2\|20\|

<!-- -->

-   **箇条書きリスト3**
        コード

      
    テキスト

| 行1 | 1   |
|-----|-----|
| 行2 | 20  |

\|\_.行1\|1\|  
\|\_.行2\|20\|

    <code class='sh'>
    コード
    </code>
```

このように、おもしろいことになる。

1. 順序付きリストと箇条書きリストが同じレベルに存在すると、後ろの方(この場合箇条書きリスト)しか変換できない
   - これは事前に混在するリストを Textile から取り除くしかないか
2. リストと文の間の過剰な空白が気持ち悪い
   - まだどうにかできそうなオプションを見つけられていないので、これは Textile -> Markdown 変換後に Prettier で変換する必要があるか
3. 新たに挿入された謎の HTML コメントの行は、以下の Pandoc の仕様によるらしい
   - [Ending a list | Pandoc - Pandoc User’s Guide](https://pandoc.org/MANUAL.html#ending-a-list)
   - > To “cut off” the list after item two, you can insert some non-indented content, like an HTML comment, which won’t produce visible output in any format:
4. 表の変換は、箇条書きに連続しない＆インデントされていない箇所に限り、 Markdown の Table Syntax に変換される
5. 当時の Redmine で使えなかったこともあり、コードブロックを `<pre><code class='lang'>` で表現していた。これが Markdown に変換したとき `pre` 要素だけ解釈されてしまう

1,4 は Pandoc で変換をかける前に手で直さざるを得ないので、正規表現に引っかかる範囲は一括変換をかける。1000 超える Textile ファイルがあるので流石に手作業は無理、可能な限り一括変換や。

2, 3, 5 は変換後に対処する。これも機械的にどうにでもできそうか。

日記のディレクトリ構造はこのような形になっており、変換作業はルートディレクトリから行う。

```plaintext
+---2013
|   +---2013-01
|   |       17.textile
|   |       18.textile
|   ︙      ︙
|   \---2013-12
︙
\---2022
   +---2022-01
   ︙
   \---2022-03
            01.md
            ︙
            31.md
```

(色々 try & error して変換していたので、 入力の手間を省く目的でわかりきったコマンドはエイリアスを多用している)

### 4 の解消

インデントされている表はなかったが、箇条書きの後に表が記載されているパターンがあり、そのうち、以下の 2 パターンがあった。

1. 箇条書き直後に表がある
2. 箇条書きの後、更に文を挟んだ後に表がある

この 2 を引っ掛けるパターンを書くのに難儀した。
何故なら、表の syntax の行末に余分に空白や不要な文字がついていて、どうにもわたしの正規表現力では引っ掛けられない。

どうにもうまくできないので妥協して、 2 回に分けて変換する。

1 については雑ながらも一括変換可能。

```powershell
ls -Recurse -File *.textile |
? {(cat $_.FullName ) -join "`n" | sls -Pattern '(\*|#).+\n\|'} |
%{
    $file = $_.FullName
    ((cat $file) -join "`n") -replace '((\*|#).+\n)(\|)',"`$1`n`$3" |
        Set-Content $file -NoNewline
}
```

改行コードは LF を使うので `-NoNewLine` だ。

2 はリストアップして清書がてら手メンテ...

```powershell
# ファイル一覧をクリップボードへ
ls -Recurse -File *.textile | ? {
    (cat $_.FullName ) -join "`n" | sls -Pattern '\n[^\*#\|].+\n\|'
} | select -ExpandProperty FullName | Set-Clipboard
```

### 1 の解消

Textile を見るに、箇条書きの後に太字 `*〇〇*` を書いて見出しの代わりにしたかったような気配がするので、それを除外した順序づき/箇条書きリストを変換の対象にする。

番号を参照していたりすると文脈が失われるので、一括変換できるか対象 23 件を目検したところ、レベル誤りだったり単に順序付きリストにし忘れているだけの箇所だったり。
それぞれに対処の仕方が異なるので、仕方ないがリストアップした対象を清書がてら手メンテ(2 回目)...

```powershell
# ファイル一覧をクリップボードへ
ls -Recurse -File *.textile | ? {
    (cat $_.FullName | ? {$_ -match '(^\*\s[^\*]|^#\s[^\*])'}) -join "`n" |
        sls -Pattern '(\*.+\n#|#.+\n\*)'
} | select -ExpandProperty FullName | Set-Clipboard
```

### Textile -> Markdown の変換

先述の通りのオプションで Pandoc を実行する。

```powershell
ls -Recurse -File *.textile | %{
    pandoc `
        --from=textile --to=gfm+east_asian_line_breaks `
        --shift-heading-level-by=-4 --eol=lf `
        $_.FullName --output="$($_.Directory)\$($_.Name -replace 'textile','md')"
}
```

### 3 の解消

変換後、まず最初に `<!-- -->` を取り除く。最後に Prettier を実行してきれいな状態にしたいからだ。

置換対象が Pandoc によって追加されたコメントだけなのか調べる。

```powershell
PS> ls -Recurse -File *.md | % {cat $_.FullName | sls -Pattern '^<!--.+-->'} | group

Count Name                      Group
----- ----                      -----
 2275 <!-- -->                  {<!-- -->, <!-- -->, <!-- -->, <!-- -->…}
```

問題ないようなので一括置換する。

```powershell
ls -Recurse -File *.md | ? {sls -Path $_.FullName -Pattern '^<!--.+-->'} | %{
    $file = $_.FullName
    (cat $file | ? {$_ -notmatch '^<!--.+-->'}) -join "`n" |
        Set-Content $file -NoNewline
}
```

この一括変換結果、1000 超のファイルの差分に問題ないか `git diff --word-diff` で見る。流石に多いので変更をグルーピングして見た。全部同じ変換結果なら1つにまとまる。

```powershell
git diff --word-diff | ? {$_ -match '\[(\+|-)'} | group
```

この置換により HTML コメント削除後は空行が 2 行になり、かつ文末の改行が消えてしまうが、後に行う Prettier で補修される(はず)。

### 5 の解消

幸い、`class` 属性なしの `code` タグはなかった。

`<code class="${language}"` → ` ```${language} ` 、 `</code>` → ` ``` ` へ変換する。一緒にやるイメージがなかったので 2 回置換する。
同時に、`pre` タグが変換されたことによる半角スペース 4 個 があると文章のインデントと合わず正しくレンダリングできないので、取り除く。

````powershell
ls -Recurse -File *.md | ? {sls -Path $_.FullName -Pattern '<code class='} | % {
    $file = $_.FullName
    (cat $file | % {
        $_ -replace '\s{0,4}<code class="(\w+)">','```$1' `
           -replace '\s{0,4}</code>','```'
    }) -join "`n" | Set-Content $file -NoNewline
}
````

### 2 の解消

[Prettier](https://prettier.io/) を使う。数年前から Markdown の整形に使っているのでそれに合わせる。

`ForEach-Object` して 1 ファイルずつ `prettier` を実行するとそこそこに遅い。 [glob](https://en.wikipedia.org/wiki/Glob_(programming)) にまとめた方が速く実行できるので、ちょっとパターンが雑だがそのようにする。

```powershell
prettier --write "**/{$((
    ls -Recurse -File *.textile | select -ExpandProperty DirectoryName |
    Split-Path -Leaf | group | select -ExpandProperty Name ) -join ',')}/*.md"
```

が、先述のコマンドを実行したあとでPrettier 導入前から Markdown で書いていた古い日記もあまり綺麗でないことが判明した。
なので、全体的に整形してしまう。

```powershell
prettier --write .
```

このとき箇条書きの書き忘れが見つかった。後述するような箇条書きがあると、 `prettier` に見出しと判断されてレイアウトが崩れるので、事前に手で取り除く。

```txt
- Bad な bullet list
  -
```

### 最終チェック

ココまで来たらもう大丈夫やろう、という感じで最終チェックしてたら、ココに来て痛恨のミス。
どうも箇条書き/順序付きリストの階層を誤っていると変換に失敗するようだった。 
1 の解消のときに回収しきれていないかった。

before.

```plaintext
** 壊れる箇条書きリスト
#### 壊れる順序付きリスト
```

after.

```plaintext
壊れる箇条書きリスト

\#### 壊れる順序付きリスト
```

調べてみたら 50 ファイルくらい結構派手にぶち壊れている部分があった。

```powershell
ls -Recurse -File *.md | ? {sls -Path $_.FullName -Pattern '\\(\*|#)\s'} |
    select -ExpandProperty FullName
```

これらは textile の段階から手直しし、 Textile → Markdown の変換からやり直す。

```powershell
ls -Recurse -File *.md | ? {sls -Path $_.FullName -Pattern '\\(\*|#)\s'} | % {
    pandoc `
        --from=textile --to=gfm+east_asian_line_breaks `
        --shift-heading-level-by=-4 --eol=lf `
        ($_.FullName -replace '.md','.textile') --output="$($_.FullName)"
}
```

この後で先に行っていた 2,3,5 の変換をすれば、期待の変換結果が得られた。

### 後始末

最後に、残しておいた Textile を全て削除する。当然、削除対象が正しいことを確認した後に消す。

```powershell
# Dry run.
rm * -Include *.textile -Recurse -WhatIf
# Execution.
rm * -Include *.textile -Recurse
```

### まとめ

なかなか手間がかかったが、うまくできた。
今回の移行作業の中で、検知していない見落としもあるはずなので、そういうものは見つけたときに対処するものとする。

以下気づき。

- Pandoc のオプションが大量にあり選ぶのが大変だが、おかげで期待する変換がしやすい
  - 2013 年位からちょくちょく使うがこんなにゴテゴテとオプションをつけたのは初めて
- 「息の長くなりそうなドキュメント」は事前にわからないので、普段からきれいなフォーマットにすれば、後々変換する機会があっても手間が省ける
- プレーンテキストは正義
- 複数行またがるタイプの正規表現のパターン記述力が足りない
- 日記に事象と感情の変化をセットで書いていたので、あとから見ても面白い。自己分析に使えそう
