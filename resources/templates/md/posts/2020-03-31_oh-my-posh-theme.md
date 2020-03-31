{:title "My own oh-my-posh theme"
:layout :post
:tags ["powershell", "oh-my-posh"]}

oh-my-posh の話。
ご存じない？ [JanDeDobbeleer/oh-my-posh: A prompt theming engine for Powershell](https://github.com/JanDeDobbeleer/oh-my-posh)

元々、Agnoster のシンボルを変えるだけで使ってた。

が、最近になって諸々の不満点 ↓ を解決したくなって、自分用のテーマを作ろうと思ったのがきっかけ。

- コマンドを打った時間を出したくなった
  - 分割して Terminal を使うことが多いので仕事で CLI を使ったときにどこがいちばん最後に使ったかわかりやすくしたい
- 階層が深いディレクトリで仕事することが多く working directory の短縮表示をもっと短くしたかった
  - デフォルトの`..`を`.`にする(ほんとは頭文字にしたいが)
- Windows10 のいつの Version からか忘れたが一部の絵文字の表示が化ける(管理者権限には 💪 を使ってた)
  - ⚡ に戻す

作ったのはこれ。[My own oh-my-posh theme.](https://gist.github.com/krymtkts/6f7e365fd1683d6edeb7e531f725d280)

oh-my-posh のテーマを作るのはかんたん。その作り方を以下に記す。

## How to make it

テンプレを生成してくれる機能とかはない。そのため theme の PS モジュールは手で配置することになる。ちなみに export されている function たちは以下の通りである。

[oh-my-posh/oh-my-posh.psd1 at master · JanDeDobbeleer/oh-my-posh](https://github.com/JanDeDobbeleer/oh-my-posh/blob/master/oh-my-posh.psd1#L48)

ユーザ定義のテーマは `$ThemeSettings.MyThemesLocation` に配置する(`Get-ThemesLocation`でも同じ様子)。フォルダがなければ合わせて作成する。

元ファイルはいまあるテーマからコピって作るのが手っ取り早い。わたしは[Agnoster.psm1](https://github.com/JanDeDobbeleer/oh-my-posh/blob/master/Themes/Agnoster.psm1)から作成した。
今あるテーマの配置フォルダを知るには`Get-Theme`を実行すれば良い。

```powershell
PS> Get-Theme

Name                  Type     Location
----                  ----     --------
krymtkts              User     C:\Users\takatoshi\OneDrive\Documents\PowerShell\PoshThemes\krymtkts.psm1
Agnoster              Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Agnoster.psm1
Avit                  Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Avit.psm1
Darkblood             Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Darkblood.psm1
Fish                  Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Fish.psm1
Honukai               Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Honukai.psm1
Paradox               Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Paradox.psm1
Powerlevel10k-Classic Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Powerlevel10k-Classic.psm1
Powerlevel10k-Lean    Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Powerlevel10k-Lean.psm1
PowerLine             Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\PowerLine.psm1
pure                  Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\pure.psm1
robbyrussell          Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\robbyrussell.psm1
Sorin                 Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\Sorin.psm1
tehrob                Defaults C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\oh-my-posh\2.0.399\Themes\tehrob.psm1
```

Agnoster とわたしのテーマの差分は次の通り。`Compare-Object`だと差分が見にくいことこの上なし！誰得 😂 なので wsl の bash から diff した。PowerSheller 失格だね(`Compare-Object`がひどいのよ)。

Unified diff でみたらこの通り。

```sh
$ diff ./Modules/oh-my-posh/2.0.399/Themes/Agnoster.psm1 ./PoshThemes/krymtkts.psm1 -u
```

```diff
--- ./Modules/oh-my-posh/2.0.399/Themes/Agnoster.psm1   2020-03-13 13:22:52.000000000 +0900
+++ ./PoshThemes/krymtkts.psm1  2020-02-24 16:03:51.912063800 +0900
@@ -11,12 +11,27 @@

     $lastColor = $sl.Colors.PromptBackgroundColor

-    $prompt = Write-Prompt -Object $sl.PromptSymbols.StartSymbol -ForegroundColor $sl.Colors.SessionInfoForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
-
+    $now = Get-Date -UFormat '%Y-%m-%d %R'
+    $backwardSymbol = "$($sl.PromptSymbols.SegmentBackwardSymbol)"
     #check the last command state and indicate if failed
     If ($lastCommandFailed) {
-        $prompt += Write-Prompt -Object "$($sl.PromptSymbols.FailedCommandSymbol) " -ForegroundColor $sl.Colors.CommandFailedIconForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
+        $rightText = " $($sl.PromptSymbols.FailedCommandSymbol) $now "
+        $rightLength = $rightText.Length + $backwardSymbol.Length + 1 # care the symbol size blur.
+        $foregroundColor = $ThemeSettings.Colors.CommandFailedIconForegroundColor
     }
+    else {
+        $rightText = " $now "
+        $rightLength = $rightText.Length + $backwardSymbol.Length
+        $foregroundColor = $ThemeSettings.Colors.PromptForegroundColor
+    }
+    $prompt += Set-CursorUp -lines 1
+    $prompt += Set-CursorForRightBlockWrite -textLength $rightLength
+    $prompt += Write-Prompt $backwardSymbol -ForegroundColor $sl.Colors.PromptBackgroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
+    $prompt += Write-Prompt $rightText -ForegroundColor $foregroundColor -BackgroundColor $sl.Colors.PromptBackgroundColor
+
+    # Write the prompt
+    $prompt += Set-Newline
+    $prompt += Write-Prompt -Object $sl.PromptSymbols.StartSymbol -ForegroundColor $sl.Colors.SessionInfoForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor

     #check for elevated prompt
     If (Test-Administrator) {
@@ -24,9 +39,8 @@
     }

     $user = $sl.CurrentUser
-    $computer = $sl.CurrentHostname
     if (Test-NotDefaultUser($user)) {
-        $prompt += Write-Prompt -Object "$user@$computer " -ForegroundColor $sl.Colors.SessionInfoForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
+        $prompt += Write-Prompt -Object "$user " -ForegroundColor $sl.Colors.SessionInfoForegroundColor -BackgroundColor $sl.Colors.SessionInfoBackgroundColor
     }

     if (Test-VirtualEnv) {
@@ -63,7 +77,11 @@
 }

 $sl = $global:ThemeSettings #local settings
+$sl.PromptSymbols.ElevatedSymbol = [char]::ConvertFromUtf32(0x26A1)
+$sl.PromptSymbols.FailedCommandSymbol = [char]::ConvertFromUtf32(0x274C)
 $sl.PromptSymbols.SegmentForwardSymbol = [char]::ConvertFromUtf32(0xE0B0)
+$sl.PromptSymbols.SegmentBackwardSymbol = [char]::ConvertFromUtf32(0xe0b2)
+$sl.PromptSymbols.TruncatedFolderSymbol = '.'
 $sl.Colors.PromptForegroundColor = [ConsoleColor]::White
 $sl.Colors.PromptSymbolColor = [ConsoleColor]::White
 $sl.Colors.PromptHighlightColor = [ConsoleColor]::DarkBlue
```

ちょっと実行時間のあたりが Cmder で出した場合にずれてしまうことがあって、微妙に余白をとってたりする。
でもこれでだいたい Cmder で見ても Windows Terminal でみても美しく出力される様になっている。
Windows Terminal ではシンボルフォントがめちゃくちゃ小さくなってしまうバグが有るのでまだ 100%最高とはいけないけど([Certain "emoji" are still half-sized · Issue #900 · microsoft/terminal](https://github.com/microsoft/terminal/issues/900))

余談だが、こんかい自作テーマの作成にあたり、既存テーマの PS モジュールにフォーマットの崩れを見つけたので PR 送ったら受け入れてもらえた。

[Format some themes. by krymtkts · Pull Request #211 · JanDeDobbeleer/oh-my-posh](https://github.com/JanDeDobbeleer/oh-my-posh/pull/211)

自分の気に入っている OSS に PR を受けれてもらえるのはちょっとした感動があるな 😚
