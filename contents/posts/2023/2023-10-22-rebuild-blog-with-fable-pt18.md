---
title: "Fable でブログを再構築する pt.18"
tags: ["fsharp", "fable"]
---

[Fable](https://fable.io/) で作ったブログに乗り換えたけど、まだ気になるとこをいじったりしている。

---

まず画像の Lazy loading をつけてみた。画像貼ることあんまりないけど。何ができるかなーと MDN を調べてみてすぐできそうだったので。

[Lazy loading - Web performance | MDN](https://developer.mozilla.org/en-US/docs/Web/Performance/Lazy_loading#images_and_iframes)

要は `![...](...)` こういう画像リンクの場合に以下のように `loading="lazy"` をつけるだけ。

```html
<img src="..." alt="..." loading="lazy" />
```

Marked の Renderer にオプションを渡して `img` タグ出力を担う `image` 関数を上書きしたら良いだけ。[Using Pro - Marked Documentation](https://marked.js.org/using_pro#renderer)

コード長いので該当のオプションの箇所だけを転記する。
コレで可視範囲外の画像が遅延読み込みされるようになるらしい。実に単純な変更だ。

```fsharp
            let image (href: string) (title: string) (text: string) =
                // NOTE: add lazy loading attribute.
                $"""<img src="%s{href}" title="%s{title}" alt="%s{text}" loading="lazy" />"""

            let mops =
                !!{| heading = heading
                     link = link
                     listitem = listitem
                     checkbox = checkbox
                     image = image |}
```

試しにいっぱい画像があるページの [昔 Planck を組み立てた記事](/posts/2019-01-14-ortho-linear-keyboard-planck.html) なんかを開いてみたら、意外と画像読み込まれてて実際にコレで効果あんのか？という感じがした。
でもスクロールダウンしていくと、確かに 1 枚だけ遅延読み込みされてきた。すご。
1 枚かーという気もしなくはないけど、いつか異常に大量の画像を貼る機会があったら効果を実感するだろう。機会なさそうやけど。

---

他に、 GitHub Pages へ deploy する workflow で NuGet package のキャッシュを試してみた。

[Caching NuGet Packages | actions/setup-dotnet](https://github.com/actions/setup-dotnet/blob/2216f56ae1eec353f06abd464e2ec435fa5f5d43/README.md#caching-nuget-packages)

初回は単にフラグオンでいけるかなと思ってやったが NuGet package の lock file を作ってなくて失敗した。
これ当然の話しでドキュメントにも lock file 無いとエラーするって書いてた。けど読まずに体当たりしたため。

そも NuGet package の lock file のことあんまりわかってなかったので、ひとまず fsproj に出力するための設定をしてみたり。以下で勉強した限りオプション入れるだけっぽい。

- [NuGet PackageReference in project files | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies)
- [Enable repeatable package restores using a lock file - The NuGet Blog](https://devblogs.microsoft.com/nuget/enable-repeatable-package-restores-using-a-lock-file/)

lock file さえあればちゃんとエラーすることなく正常終了した。
初回でキャッシュが無いから、ログにもそう出てた。

[#72](https://github.com/krymtkts/blog-fable/actions/runs/6595551745/job/17920660575#step:4:11) のログを転記。

```plaintext
Run actions/setup-dotnet@v3
  with:
    global-json-file: ./global.json
    cache: true
    cache-dependency-path: src/packages.lock.json
  env:
    GITHUB_PAGES: true
/home/runner/work/_actions/actions/setup-dotnet/v3/externals/install-dotnet.sh --channel 7.0
dotnet-install: .NET Core SDK with version '7.0.402' is already installed.
Dotnet cache is not found
```

[Actions タブの Caches](https://github.com/krymtkts/blog-fable/actions/caches) を見たらキャシュが作成されてたので、キャッシュがなくなる前に次の workflow 実行したらキャッシュ有り版のログになるかな。

ちょうど bug があったので直したやつ [#73](https://github.com/krymtkts/blog-fable/pull/73) を merge したところ、キャッシュ使われてた。
ただ workflow 全体を見て速くなった感じはなさそう。 Fable だからなのか。

[#73](https://github.com/krymtkts/blog-fable/actions/runs/6601005193/job/17931606545#step:4:15) からログを転記。

```plaintext
Run actions/setup-dotnet@v3
  with:
    global-json-file: ./global.json
    cache: true
    cache-dependency-path: src/packages.lock.json
  env:
    GITHUB_PAGES: true
/home/runner/work/_actions/actions/setup-dotnet/v3/externals/install-dotnet.sh --channel 7.0
dotnet-install: .NET Core SDK with version '7.0.402' is already installed.
Received 131171387 of 131171387 (100.0%), 161.8 MBs/sec
Cache Size: ~125 MB (131171387 B)
/usr/bin/tar -xf /home/runner/work/_temp/bf7a4381-abc4-4d1d-90af-6c72251bb2dd/cache.tzst -P -C /home/runner/work/blog-fable/blog-fable --use-compress-program unzstd
Cache restored successfully
Cache restored from key: dotnet-cache-Linux-73204c40d9255ea97458df81ea64893c7b0c7130549819b8ae0fb308360b2d25
```

この方法で NuGet package のキャッシュできそうなので、同じ方法を [krymtkts/pocof](https://github.com/krymtkts/pocof) にもやってみたのだけど、そっちはまだ模索中。
platform が違うと生成される contentHash も違うらしく NU1403 というエラーで頓挫している。

NU1403 と cross-platform 周りで調べた内容。 NU1403 が出たら `DisableImplicitNuGetFallbackFolder` で大体解消するってのが定石らしいけど、 cross-platform なので話は別ぽい。調べたが理解が浅くてわからん。
cross-platform な lock file を置いても [nektos/act](https://github.com/nektos/act) でテストすると hash が合わないエラー NU1403 が解消できない。

- [NuGet Error NU1403 | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/reference/errors-and-warnings/nu1403)
- [.net - NuGet lock file fails to restore with --locked-mode - Stack Overflow](https://stackoverflow.com/questions/57161512/nuget-lock-file-fails-to-restore-with-locked-mode)
- [c# - Can dotnet-test specify a runtime-identifier? - Stack Overflow](https://stackoverflow.com/questions/58612554/can-dotnet-test-specify-a-runtime-identifier)

なんで Fable の方は Windows で生成した lock file が ubuntu の runner でうまくいったんだろうか。 NuGet Package への理解が浅い。

---

あとやりたいこととしては、以前から気になってる Bulma の css 軽量化。

ただ[前に書いた](/posts/2023-09-24-rebuild-blog-with-fable-pt14.html)ように Bulma がちゃんと SCSS 対応されてないことで `@use` を使った場合にスタイルを部分的にするとビルドエラーするから、それをなんとか解消できないと先がない。
妥協案としては `@use` を諦めて `@import` にしたら良いのだろうけど、 SCSS で非推奨やからなあ...悩ましい。

続く。
