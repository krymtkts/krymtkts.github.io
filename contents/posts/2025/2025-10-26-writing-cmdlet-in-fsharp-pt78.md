---
title: "F# で Cmdlet を書いてる pt.78"
tags: ["fsharp", "powershell", "dotnet"]
---

[krymtkts/pocof](https://github.com/krymtkts/pocof) を開発した。

前回に続き query の構文解析と実行部分に手を入れた。 [#373](https://github.com/krymtkts/pocof/pull/373)

query の構文解析は、従来の正規表現の利用をやめ自前の parser で組み立てるようにした。[前の日記](/posts/2025-10-19-writing-cmdlet-in-fsharp-pt77.html)のときとさほど変わってないかと。

before

| Method               | QueryCount |     Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------- | ---------- | -------: | --------: | --------: | ----: | ------: | -----: | --------: | ----------: |
| prepareNormalQuery   | 1          | 1.353 us | 0.0343 us | 0.1007 us |  1.01 |    0.10 | 0.6523 |   2.66 KB |        1.00 |
| preparePropertyQuery | 1          | 1.428 us | 0.0277 us | 0.0330 us |  1.06 |    0.08 | 0.6866 |    2.8 KB |        1.05 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 3          | 2.672 us | 0.0522 us | 0.0813 us |  1.00 |    0.04 | 1.2970 |    5.3 KB |        1.00 |
| preparePropertyQuery | 3          | 2.845 us | 0.0560 us | 0.0803 us |  1.07 |    0.04 | 1.3657 |   5.58 KB |        1.05 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 5          | 4.018 us | 0.0741 us | 0.1317 us |  1.00 |    0.05 | 1.9379 |   7.95 KB |        1.00 |
| preparePropertyQuery | 5          | 4.299 us | 0.0852 us | 0.2229 us |  1.07 |    0.07 | 2.0370 |   8.34 KB |        1.05 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 7          | 5.332 us | 0.1034 us | 0.1669 us |  1.00 |    0.04 | 2.5864 |  10.59 KB |        1.00 |
| preparePropertyQuery | 7          | 5.651 us | 0.1125 us | 0.1156 us |  1.06 |    0.04 | 2.7237 |  11.13 KB |        1.05 |

after

| Method               | QueryCount |     Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
| -------------------- | ---------- | -------: | --------: | --------: | ----: | ------: | -----: | --------: | ----------: |
| prepareNormalQuery   | 1          | 1.307 us | 0.0308 us | 0.0903 us |  1.00 |    0.10 | 0.6638 |   2.71 KB |        1.00 |
| preparePropertyQuery | 1          | 1.278 us | 0.0254 us | 0.0465 us |  0.98 |    0.08 | 0.6809 |   2.78 KB |        1.03 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 3          | 2.447 us | 0.0487 us | 0.0813 us |  1.00 |    0.05 | 1.3199 |    5.4 KB |        1.00 |
| preparePropertyQuery | 3          | 2.532 us | 0.0501 us | 0.0634 us |  1.04 |    0.04 | 1.3542 |   5.53 KB |        1.02 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 5          | 4.473 us | 0.2554 us | 0.7449 us |  1.03 |    0.24 | 1.9760 |   8.09 KB |        1.00 |
| preparePropertyQuery | 5          | 3.852 us | 0.0760 us | 0.1519 us |  0.88 |    0.15 | 2.0218 |   8.28 KB |        1.02 |
|                      |            |          |           |           |       |         |        |           |             |
| prepareNormalQuery   | 7          | 4.879 us | 0.0973 us | 0.2073 us |  1.00 |    0.06 | 2.6321 |  10.77 KB |        1.00 |
| preparePropertyQuery | 7          | 5.087 us | 0.1007 us | 0.1655 us |  1.04 |    0.05 | 2.7008 |  11.04 KB |        1.02 |

次に手を加えたのは、絞り込みのための述語を生成していた部分。
わかりやすさのために関数を分けていたのを統合し、再帰による繰り返しを減らした。
そして色々試してより良くする方法を探した結果、[コードクォート](https://learn.microsoft.com/ja-jp/dotnet/fsharp/language-reference/code-quotations)の利用をやめて lambda expression と loop に書き直した。
クエリの数が増えるほど効果があるのでコードクォートを使っていたが、実際のところ 10 クエリも組み合わせることがなく、 overhead が目立つと考えた。
また loop を使う等他の再帰化も合わせれば断然 lambda expression の方が速くてメモリも軽くできるので、コードクォートをやめ結局元に戻った。

before

| Method            | EntryCount | QueryCount |       Mean |    Error |   StdDev |     Median |    Gen0 |   Gen1 | Allocated |
| ----------------- | ---------- | ---------- | ---------: | -------: | -------: | ---------: | ------: | -----: | --------: |
| run_obj_normal    | 100        | 1          |   542.6 us | 10.11 us | 18.99 us |   535.4 us | 15.6250 | 0.9766 |  63.91 KB |
| run_dict_normal   | 100        | 1          |   541.9 us | 10.68 us | 16.31 us |   539.3 us | 15.6250 | 0.9766 |  64.04 KB |
| run_obj_property  | 100        | 1          |   257.3 us |  4.62 us |  5.50 us |   256.6 us |  4.8828 | 4.3945 |  21.38 KB |
| run_dict_property | 100        | 1          |   260.2 us |  5.10 us |  7.31 us |   261.3 us |  4.8828 | 4.3945 |  21.38 KB |
| run_obj_normal    | 100        | 5          |   792.0 us | 22.19 us | 61.85 us |   771.3 us | 35.1563 |      - | 148.65 KB |
| run_dict_normal   | 100        | 5          |   771.8 us | 15.32 us | 29.15 us |   775.6 us | 35.1563 |      - | 148.64 KB |
| run_obj_property  | 100        | 5          |   262.8 us |  5.24 us |  8.75 us |   262.5 us |  4.8828 | 4.3945 |  21.48 KB |
| run_dict_property | 100        | 5          |   258.5 us |  4.76 us |  6.35 us |   258.5 us |  4.8828 | 4.3945 |  21.48 KB |
| run_obj_normal    | 100        | 10         | 1,040.2 us | 20.52 us | 34.29 us | 1,037.0 us | 62.5000 | 1.9531 | 255.39 KB |
| run_dict_normal   | 100        | 10         | 1,039.5 us | 20.41 us | 36.80 us | 1,033.0 us | 62.5000 | 1.9531 | 255.39 KB |
| run_obj_property  | 100        | 10         |   259.9 us |  5.14 us |  8.01 us |   259.5 us |  4.8828 | 4.3945 |   21.6 KB |
| run_dict_property | 100        | 10         |   261.1 us |  5.11 us |  7.64 us |   259.8 us |  4.8828 | 4.3945 |   21.6 KB |
| run_obj_normal    | 1000       | 1          |   539.5 us | 10.77 us | 12.82 us |   538.3 us | 15.6250 | 0.9766 |   63.9 KB |
| run_dict_normal   | 1000       | 1          |   539.7 us | 10.67 us | 15.98 us |   539.0 us | 15.6250 | 0.9766 |   63.9 KB |
| run_obj_property  | 1000       | 1          |   256.6 us |  5.11 us |  5.89 us |   256.0 us |  4.8828 | 4.3945 |  21.38 KB |
| run_dict_property | 1000       | 1          |   265.8 us |  5.29 us | 14.11 us |   261.6 us |  4.8828 | 4.3945 |  21.38 KB |
| run_obj_normal    | 1000       | 5          |   769.6 us | 15.05 us | 24.30 us |   766.6 us | 35.1563 |      - | 148.64 KB |
| run_dict_normal   | 1000       | 5          |   775.4 us | 14.84 us | 19.82 us |   765.8 us | 35.1563 |      - | 148.64 KB |
| run_obj_property  | 1000       | 5          |   262.3 us |  5.20 us |  8.39 us |   263.0 us |  4.8828 | 4.3945 |  21.48 KB |
| run_dict_property | 1000       | 5          |   260.7 us |  5.10 us |  7.48 us |   259.6 us |  4.8828 | 4.3945 |  21.48 KB |
| run_obj_normal    | 1000       | 10         | 1,054.5 us | 19.16 us | 33.05 us | 1,052.0 us | 62.5000 | 1.9531 | 255.39 KB |
| run_dict_normal   | 1000       | 10         | 1,031.6 us | 20.54 us | 30.74 us | 1,031.5 us | 62.5000 | 1.9531 | 255.39 KB |
| run_obj_property  | 1000       | 10         |   261.9 us |  4.59 us |  6.59 us |   259.7 us |  4.8828 | 4.3945 |   21.6 KB |
| run_dict_property | 1000       | 10         |   259.9 us |  5.10 us |  6.98 us |   258.9 us |  4.8828 | 4.3945 |   21.6 KB |

after

| Method            | EntryCount | QueryCount |      Mean |    Error |    StdDev |    Median |     Gen0 |   Gen1 | Allocated |
| ----------------- | ---------- | ---------- | --------: | -------: | --------: | --------: | -------: | -----: | --------: |
| run_obj_normal    | 100        | 1          |  71.51 us | 1.311 us |  1.227 us |  71.35 us |  12.4512 |      - |  50.85 KB |
| run_dict_normal   | 100        | 1          |  61.02 us | 0.835 us |  0.740 us |  60.98 us |  18.0664 | 0.2441 |  73.76 KB |
| run_obj_property  | 100        | 1          |  45.92 us | 1.737 us |  4.928 us |  44.66 us |   6.8359 |      - |  27.75 KB |
| run_dict_property | 100        | 1          |  39.69 us | 0.785 us |  1.880 us |  39.13 us |   3.9063 |      - |  16.06 KB |
| run_obj_normal    | 100        | 5          |  68.25 us | 1.129 us |  1.056 us |  67.89 us |  12.9395 |      - |  52.46 KB |
| run_dict_normal   | 100        | 5          |  59.26 us | 0.780 us |  0.729 us |  59.08 us |  18.0664 |      - |  73.28 KB |
| run_obj_property  | 100        | 5          |  44.90 us | 1.366 us |  3.874 us |  44.39 us |   6.8359 |      - |  27.84 KB |
| run_dict_property | 100        | 5          |  39.88 us | 0.797 us |  1.815 us |  40.17 us |   3.9063 |      - |  16.13 KB |
| run_obj_normal    | 100        | 10         |  65.97 us | 0.596 us |  0.528 us |  65.81 us |  12.4512 | 0.2441 |  50.87 KB |
| run_dict_normal   | 100        | 10         |  61.32 us | 0.718 us |  0.671 us |  61.38 us |  16.8457 |      - |  68.59 KB |
| run_obj_property  | 100        | 10         |  43.40 us | 0.867 us |  2.010 us |  43.09 us |   6.8359 |      - |  27.96 KB |
| run_dict_property | 100        | 10         |  38.44 us | 0.767 us |  0.788 us |  38.41 us |   4.0283 |      - |  16.26 KB |
| run_obj_normal    | 1000       | 1          | 440.59 us | 8.743 us | 19.734 us | 435.38 us | 109.3750 | 3.9063 | 444.76 KB |
| run_dict_normal   | 1000       | 1          | 368.63 us | 7.227 us | 11.463 us | 368.76 us | 215.8203 | 0.9766 | 874.71 KB |
| run_obj_property  | 1000       | 1          | 254.84 us | 6.116 us | 17.647 us | 250.79 us |  50.2930 |      - |  204.5 KB |
| run_dict_property | 1000       | 1          | 159.27 us | 3.195 us |  9.010 us | 159.45 us |  16.1133 |      - |  65.35 KB |
| run_obj_normal    | 1000       | 5          | 421.16 us | 8.228 us | 13.972 us | 421.89 us | 112.3047 |      - | 456.53 KB |
| run_dict_normal   | 1000       | 5          | 360.14 us | 7.111 us | 16.481 us | 359.22 us | 216.7969 |      - | 878.04 KB |
| run_obj_property  | 1000       | 5          | 252.63 us | 6.263 us | 18.170 us | 248.13 us |  50.2930 |      - | 204.58 KB |
| run_dict_property | 1000       | 5          | 163.12 us | 4.105 us | 11.974 us | 161.47 us |  16.1133 |      - |  65.45 KB |
| run_obj_normal    | 1000       | 10         | 369.26 us | 7.483 us | 21.708 us | 364.90 us | 111.3281 |      - | 457.44 KB |
| run_dict_normal   | 1000       | 10         | 343.91 us | 6.037 us | 12.468 us | 344.69 us | 199.2188 |      - | 809.26 KB |
| run_obj_property  | 1000       | 10         | 257.50 us | 6.539 us | 19.073 us | 251.57 us |  50.2930 |      - | 204.71 KB |
| run_dict_property | 1000       | 10         | 156.66 us | 3.260 us |  9.247 us | 155.36 us |  16.1133 |      - |  65.56 KB |

コードクォートは非常に魅力的だが、式の複雑化で node が増えると重くなる・ IL の最適化が効かないといったところで活かしきれなかった。
今後速さを求められるメタプロが必要な場面なら活かせるかなと。 pocof ではなさそうだが。

修正して benchmark を取って... の繰り返しだととにかく benchmark 取得のための時間が必要なので、なかなか実装進められず疲れた。
改善アイディアを実装するたびに 5 ~ 10 分の検証時間がかかるのは結構ストレスだし、限られた時間しかない状態だと中々許容しにくい。
でも家事の隙間に benchmark を回したりで工夫したおかげでそこそこ満足がいく最適化にはなったかなという感触。
これで pocof の 次の version を出して使い始められる。
benchmark はなんかもう少し気軽に回せるような改善ができないか考えたい。

この先もちまちま最適化していく。
クエリ関連では、述語を構築する際に差分コンパイルができたらいいなと考えているので、実験してみたい。
以前も触れたが、最適化において [.NET Standard 2.0](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0) で使えない機能を使いたい場面が増えた。
そろそろ Windows PowerShell だけ .NET Standard 2.0 にして他はより新しい target framework にするとか考えた方が良いかも知れない。
多少なりとも複雑性をもたらすからおいそれと始められないけど、いい経験にはなりそうだ。

あと 2025-11 は .NET 10 が来るので、乗り換えて高速化されるのを期待したい。
