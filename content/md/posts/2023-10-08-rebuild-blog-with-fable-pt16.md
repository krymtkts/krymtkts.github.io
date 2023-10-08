{:title "Fable でブログを再構築する pt.16"
:layout :post
:tags ["fsharp", "fable"]}

[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) [Fable](https://fable.io/) でブログを再構築している。

こまい修正をして、その後現ブログの Markdown を元にどれくらいのパフォを望めるのか試してみた。

パフォ比較時点での記事数は今書いているこの記事を含めて、 posts が 151 pages が 1 画像が 25 。

```powershell
# krymtkts/krymtkts.github.io の root directory にて
PS> 'md/posts', 'md/pages', 'img' | ForEach-Object { Get-ChildItem ./content/$_ -Recurse -File | Measure-Object | Select-Object -ExpandProperty Count }
151
1
25
```

この状態で `lein run` だと 3 度試行して概ね 26.174 , 27.144, 28.722 秒に収まるくらい。

次に krymtkts/blog-fable の歴史を krymtkts/krymtkts.github.io に統合して測定する。
統合する手法は前に [Blog 用 Git repositories のマージ](/posts/2022-03-26-merge-blog-repo) でやったのと同じで慣れたもの。
生成元の Markdown 配置だけ構造変わってるのでそこの置き換えだけは必要な感じだが、今回は検証なので symbolic link でやる。
Markdown と画像以外は全部消す。

```powershell
# krymtkts/krymtkts.github.io の root directory にて
git switch --create feature/blog-fable

mkdir contents

# remove old resources
ll -Exclude content,contents | rm -Recurse

git add -u
git commit -m "Remove old resources."

# Merge blog-fable repo.
git remote add blog-fable ssh://git@github.com/krymtkts/blog-fable.git
git fetch blog-fable
git merge --allow-unrelated-histories blog-fable/main

# make symbolic links with Administrator access
rm contents/* -Recurse
'posts','pages' | %{New-Item -ItemType SymbolicLink -Path contents -Name $_ -Value "$(pwd)/content/md/$_"}
'img' | %{New-Item -ItemType SymbolicLink -Path contents -Name $_ -Value "$(pwd)/content/$_"}

# エラーしないように Markdown を書き換える ← 後述

npm install
npm run build
```

これで初回はビルドが発生するので 21.479 、若干速くなるけどキャッシュなしならあんま変わらんなという印象は否めない感じ。
以降はキャッシュがあったら 6.424 , 7.597 という感じだった。

[前回](/posts/2023-10-01-rebuild-blog-with-fable-pt15)このように記した。

> あと気になるのはパフォ面。
> Fable のビルドは遅いけどそれ以外は結構速いから期待できんじゃないかな。
> Cryogen での SSG は記事が多いともっさりしてて 151 記事で `lein run` の終了まで 25 秒くらかかる。
> 8 記事しかない状態で 4 秒未満のとこしか見てないから、 Cryogen と同じ記事数でどうなるか...

少なくともローカルで Markdown を書いて開発サーバで確認する分には速くなりそう。
GitHub Actions のワークフローで動かす分にはあんまり変わらないか。
何なら npm と dotnet 両方の依存関係があるし遅くなったりして。

GitHub Actions でキャッシュするようにしたら良さそうか。
でも GitHub Actions のキャッシュて 7 日間経過したら消えるらしいので、自分の使い方だと大体週一やからキャッシュの恩恵にあずかれなさげかな。
ひとまずやるだけやろう。

あと一部先述しているように本格的な移行を進めるにあたり、以下の最低限対応が必要。

- highlight.js 未対応言語の書き換え
  - わかりやすいように新ブログでは謎言語をエラーで許容しなくしたので、言語の識別子の typo や 空, `textile` `log` みたいな対応してないやつがエラーするようになった
- front matter を [edn](https://github.com/edn-format/edn) から YAML 形式に書き換え
- (念のため) サイト内 link に `.html` を足すようにする
  - GitHub Pages だといい感じに処理してくれるが、新ブログで生成する `*.html` に合わせる

他に見落としあるかも？という気はせんでもないが、最低限これだけ処置すればいつでも現ブログを移行できるようになるはず。

front matter の書き換えだけちょっとめんどくさいな...というやる気ゼロモードだが、あえて edn を維持する必要がなくやらざるを得ない。
量的に(150 あり)手作業はありえないので PowerShell でいい塩梅に処理できないか検討する。

ここまでくればあとは実施するのみだが、コレやってしまうと Feedly で見る限り少なくとも世界に 1 人は購読してくれている人がいるので、その人に影響あろうからちょっと気になるところではある。仕方ない点ではあるか。

あとアレな、 Fable と共に過ごすということは Node.js のエコシステムの上で生活するので、逐次脆弱性に対処するための依存関係の更新がこれから発生する。
と考えるメンテし続けないといけないから dependabot 有効化とか考えても良さそう。
