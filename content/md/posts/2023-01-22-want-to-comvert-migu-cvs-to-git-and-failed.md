{:title "Migu の CVS repo を Git repo に変換したい(そして失敗)"
:layout :post
:tags ["font"]}

### 2023-01-29 追記

これ上手くいかないの当然で、サーバにある repo 本体に対してこれを実行しないといけない。
centralized VCS 久しぶりだったので、仕組みをすっかり忘れていた。
この日記は間違いの記録となった。

---

Migu フォントが好きでずっと使っている。
それこそ [Migu で PowerLine を使うためにパッチスクリプトを改修する](/posts/2021-05-30-maybe-completed-refining-migu-nerd-font)くらいには気に入っている。

[Migu フォント : M+と IPA の合成フォント](https://mix-mplus-ipa.osdn.jp/migu/)

今後も使い続けるにあたって、この度 Migu のグリフやコードを読んでみたいと思ったのだけど、 Migu は OSDN の CVS で管理されているので、なかなか読み辛い。
自分が VCS を使い始めたのは SVN からだったりするので、 CVS わからない事が多い。
なのでこの度 Git の repo に変換しようと考えている。

方法を調べているが、もう時代的に cvs → git の変換なんてあまり行われてないようだ。
情報を調べてみても流石に最近のものはなく、いまんところ 2020 頃の記事が参考になるか。

[CVS リポジトリを Git リポジトリに変換する | アルファのブログ](https://alpha3166.github.io/blog/20200523.html)

ひとまず [Index of /mix-mplus-ipa - mixfont-mplus-ipa - OSDN](https://osdn.net/cvs/view/mix-mplus-ipa/) にアクセスし repo の tarball を download してある。

これに対して、 WSL の Ubuntu 20 で [cvs-fast-export](https://gitlab.com/esr/cvs-fast-export) をインストールして `cvsconvert` かけてみたのだけどエラーになってしまった。

```shell
$ cvsconvert -pvn mix-mplus-ipa/mixfont-mplus-ipa
2023-01-22T07:25:17Z: Reading file list...done, 0.000KB in 0 files (0.581sec)
2023-01-22T07:25:17Z: Analyzing masters with 16 threads...done, 0 revisions (0.002sec)
2023-01-22T07:25:17Z: Make DAG branch heads...done  (0.000sec)
2023-01-22T07:25:17Z: Sorting...done        after parsing:      0.583   1456KB
after branch collation: 0.583   1456KB
               total:   0.583   1456KB
0 commits/0.000M text, 0 atoms at 0 commits/sec.
fatal: stream ends early
fast-import: dumping crash report to .git/fast_import_crash_50
cvsconvert: cat mix-mplus-ipa-mixfont-mplus-ipa.git.fi | (cd mix-mplus-ipa-mixfont-mplus-ipa-git >/dev/null; git fast-import --quiet --done && git checkout) returned 128.
```

さっぱりわからん。

他にもツールがあるのでを試してみたいが、 `cvs2git` が同梱される [cvs2svn](https://github.com/mhagger/cvs2svn) は Ubuntu 20 で提供されておらず。
[Ubuntu – Package Search Results -- cvs2svn](https://packages.ubuntu.com/search?keywords=cvs2svn)

また `git cvsimport` はインストール済みの `git` とバージョン競合してしまった。

こうなったら Dockerfile 書いて Ubuntu 18 で `cvs2git` インストールする方が良いか？
ちゃっちゃと終えられるかと思ったけど全然そうじゃなかった。
どうでもいい系のタスクをまた積んでしまったけど、一度手を出したからには最後までやらないと気が済まない。
