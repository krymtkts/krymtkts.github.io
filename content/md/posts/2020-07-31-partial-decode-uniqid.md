{:title "PHP の uniqid をデコードする"
:layout :post
:tags ["powershell", "php"]}

なんか一意っぽい値を PHP で生成する場合の楽な手段として、 `uniqid` がある。

- [php-src/uniqid.c at 91fbd12d5736b3cc9fc6bc2545e877dd65be1f6c · php/php-src](https://github.com/php/php-src/blob/91fbd12d5736b3cc9fc6bc2545e877dd65be1f6c/ext/standard/uniqid.c)

prefix を除いた先頭 8 桁が unixtime を 16 進数で出してるだけっぽいので、こいつが何時生成されたのかを知りたい時に、以下の手順が踏める。

- [PHP: uniqid - Manual](https://www.php.net/manual/ja/function.uniqid.php#95001)

ここのコメントのまま使える。`more_entropy`が有効な値で試す。

```php
// <?php
$s = "5ef4f46e0e40f9.59913527";
$d = date("r",hexdec(substr($s,0,8)));
echo($d . PHP_EOL);
// Thu, 25 Jun 2020 19:01:02 +0000
```

しかし手前は PHP をインストールしてなくて repl を持ってない(↑ の Repl.it でやった)ので、これを PowerShell でやる！(ついでに JST)

- [epoch - Convert Unix time with PowerShell - Stack Overflow](https://stackoverflow.com/questions/10781697/convert-unix-time-with-powershell)

```powershell
$s = "5ef4f46e0e40f9.59913527";
(Get-Date '1970-1-1').AddSeconds([System.Convert]::ToInt32($s.Substring(0, 8), 16)).ToLocalTime()
# 2020年6月26日 04:01:02
```

ちょっとした小技が必要だったので覚書しておく。
無駄に PHP のコードを読んでしまった...😂
