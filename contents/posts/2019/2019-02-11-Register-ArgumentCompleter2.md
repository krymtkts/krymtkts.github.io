---
title: "Register-ArgumentCompleterのScriptBlockの引数"
tags:  ["powershell"]
---

[前回](./2019-02-04-Register-ArgumentCompleter.md)の続き。

```powershell
Register-ArgumentCompleter -Native -CommandName mvn -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)
```

↑こんなんいるやん？

`wordToComplete`には入力中の文字列が入ってくる。

`$commandAst`にはコマンドラインが全部載ってくる。

でも実際詳しく知らんので、たとえば`maven clean install -pl :module1 -`ってとこまで入力してCtrl+Spaceやったときにはどういうパラメータになるかわからんので調べた。

結論から言うと`$commandAst`はASTが乗ってくる。`mvn clean install --d`でtabしたときのデバッガでの出力は以下の通り(デバッグ実行にはISEを利用した)。 [Windows PowerShell ISE の操作 | Microsoft Docs](https://docs.microsoft.com/ja-jp/powershell/scripting/components/ise/exploring-the-windows-powershell-ise?view=powershell-6)

```powershell
PS C:\Users\takatoshi\Desktop\maven> mvn clean install --d
ヒット 'C:\Users\takatoshi\dev\powershell\MavenAutoCompletion\MavenAutoCompletion.ps1:152' の行のブレークポイント
[DBG]: PS C:\Users\takatoshi\Desktop\maven>> $commandAst


CommandElements    : {mvn, clean, install, --d}
InvocationOperator : Unknown
DefiningKeyword    :
Redirections       : {}
Extent             : mvn clean install --d
Parent             : mvn clean install --d




[DBG]: PS C:\Users\takatoshi\Desktop\maven>> $wordToComplete
--d
```

`mvn clean install --projects :`でtabした場合↓

```powershell
[DBG]: PS C:\Users\takatoshi\Desktop\maven>>
mvn clean install --projects :
ヒット 'C:\Users\takatoshi\dev\powershell\MavenAutoCompletion\MavenAutoCompletion.ps1:152' の行のブレークポイント
[DBG]: PS C:\Users\takatoshi\Desktop\maven>> $commandAst


CommandElements    : {mvn, clean, install, --projects...}
InvocationOperator : Unknown
DefiningKeyword    :
Redirections       : {}
Extent             : mvn clean install --projects :
Parent             : mvn clean install --projects :




[DBG]: PS C:\Users\takatoshi\Desktop\maven>> $commandAst.CommandElements


StringConstantType : BareWord
Value              : mvn
StaticType         : System.String
Extent             : mvn
Parent             : mvn clean install --projects :

StringConstantType : BareWord
Value              : clean
StaticType         : System.String
Extent             : clean
Parent             : mvn clean install --projects :

StringConstantType : BareWord
Value              : install
StaticType         : System.String
Extent             : install
Parent             : mvn clean install --projects :

StringConstantType : BareWord
Value              : --projects
StaticType         : System.String
Extent             : --projects
Parent             : mvn clean install --projects :

StringConstantType : BareWord
Value              : :
StaticType         : System.String
Extent             : :
Parent             : mvn clean install --projects :
```

`wordToComplete` こいつはまじでただの文字列。`commandAst.CommandElements`の最後の要素の`value`が出てる。

という感じだったので、MavenAutoCompletion的には`commandAst.CommandElements`の最後から2要素を対象に正規表現してやれば、だいたい望みのことができるのがわかったのであった。

### 余談

PowerShellで配列の任意の連続した要素を抜き出すのに、slice的なんがないんかと調べたところ、以下のようにするようだ。

```powershell
> $a = @('mvn', 'clean', 'install', '-pl', ':')
> $a[($a.Length -2)..$a.Length]
-pl
:
```

cool😉
