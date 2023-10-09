---
title: "Windows 11 の表示言語を完全に英語にする"
tags: ["windows"]
---

Windows 10 からデフォルト言語が日本の状態の PC でも表示言語を変えられる。
極力英語 UI を使うようにしているのもあって、今の Razer Blade Stealth を買ったころから英語表示で利用している。 Windows 11 にアップグレードしてからも同じだ。

設定自体は、ここに書いてる方法で表示言語を日本語から英語に変えられる。
[Manage the input and display language settings in Windows - Microsoft Support](https://support.microsoft.com/en-us/windows/manage-the-input-and-display-language-settings-in-windows-12a10cb4-8626-9b77-0ccb-5013e0c7c7a2#WindowsVersion=Windows_10)

あと重要なのが、 Time & Language → Language & region のところの Preferred languages の順番。ここでちゃんと先頭を英語にしておかないといけない。
例えば [PowerToys](https://github.com/microsoft/PowerToys) や [Windows Terminal](https://github.com/microsoft/terminal) といったツール。
これらは最優先される言語でコマンドパレットや UI を表示したりする仕組みになってるようで[^1]、日本語を最優先にしていると部分的に表示言語が日本語になり、使いにくいことこの上なくなる。
これに従うとデフォルトのキーボードが英語になるデメリットがあるが、インスタントに切り替えられない UI の方を尊重して、 起動後は毎回 Win + Space で英語 → 日本語に切り替えている。

こんな感じでちまちま英語 UI を使えるように調整していたのだけど、最近困った事が起こった。
ここ数ヶ月のどっかの Windows Update のタイミング以降、ログイン画面やシャットダウン・再起動時の表示言語が日本語になる事象が発生した。
先述の表示言語の設定で、英語 → 日本語としたあとで再起動後また英語にするとか、色々やっても解消しないので何なんや...と思っていた。

ついに先日、その解消方法を見つけた。
しかも身近な Time & Language → Language & region の中にある。たまたまポチっては画面を眺めてを繰り返していたときに見つけた。

Administrative language settings だ。

キーワードさえわかればこのように類似ケースを探すのも容易い。でも Microsoft の文書がヒットしないのはなんでや...
[How to change system language on Windows 11 | Windows Central](https://www.windowscentral.com/how-change-system-language-windows-10)

ここ 2 つのセクションがあって、両方を英語に変えた。

1. Welcome screen and new user accounts
2. Language for non-Unicode programs

1 では Current user から Welcome screen, New user accounts の 2 つの言語設定に設定をコピーできて、その内の Welcome screen の言語設定がログイン画面に効いた様子。
また英語表示が戻ってきて、うれしい。
更に、 Windows Update 後に足した覚えのないキーボードが復活する現象にも悩まされていた。
New user accounts にその足した覚えのないキーボードが割あたってたので、多分犯人はこいつ。
これも解消できそう。

2 の方は何に効いてるのかわからん。 non-Unicode なアプリがあったらわかるんかな。

手順がわかってしまえば、もしまた表示言語が日本語に戻ったとしても、この設定をやり直せば良いので安心できる。
理想はスクリプト化しておきたいが、まだ Registry の該当箇所を調べてない。

という具体で久しぶりのフル英語表示にできて満足した。

[^1]: 要出典。昔 Windows Terminal にコマンドパレットが導入されたときにそんな Issue を見たが忘れた。
