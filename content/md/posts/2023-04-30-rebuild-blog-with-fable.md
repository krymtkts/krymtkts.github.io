{:title "Fable でブログを再構築する pt.1"
:layout :post
:tags ["fsharp", "fable"]}

[Fable](https://fable.io/) の練習を始めた。

Fable にしたのはフロントエンドのエコシステムと統合するのに良さそうだったから。
いまの Clojure 製 Cryogen はフロントエンドとは切り離された感じで、一応 Bootstrap を使ってりはしてるが module 管理とかしてない。なのでフロントエンドの依存性の管理みたいのも一緒にしたいなーと考えていた。

今使ってる Clojure 製の [Cryogen](https://cryogenweb.org/) と同じ雰囲気を F# で再現するなら [ionide/Fornax](https://github.com/ionide/Fornax) が正にそんな感じっぽかった。
でも折角なので HTML, JavaScript, CSS 共に F# へ統合した感じにしたい。 Fable ならそれができそうに思えた。

実際のところ Fable は F# を他言語へ変換するトランスパイラなので、静的サイトジェネレーターとして使うにはオーバスペックな感じはある。
けど、こんな機会でもないと一生触らないテクノロジなので、やるならこっちやろ！と考えた。

実現したいことは以下の通り。

1. Markdown で書いたコンテンツを静的サイトに変換する(Cryogen と同じ使用感)
2. RSS feed の XML を出す(Cryogen で出したのと同じやつ)

ここ 2, 3 週間ほど、子供の寝かしつけ中とかに片手間で Fable やその周辺ツール・モジュールのドキュメントとか諸々の repo を見ていた。けどあんまドキュメント充実しておらず、テンプレプロジェクトを生成して壊してを繰り返してようやく雰囲気がわかってきた。見た目は同じでも中身は違うわ、ってかんじ。

色々試行錯誤した成果として先述の 1 を最小限に実現できたので、一旦日記にまとめる。
[krymtkts/blog-fable](https://github.com/krymtkts/blog-fable)

---

実現手段はいくつか調査した。
まず Fable 作者の Fable で静的サイトを作った例[^1]が既にある。
でも激古なためかわたしが理解不足なのか、全くもって動かすことができなかった。
また [Bulma](https://bulma.io/) という CSS Framework の Fable wrapper である [Fulma](https://fulma.github.io/Fulma/#home) のテンプレ[^2]もあったのだけど、これも同様に動かし方がわからなかった。

[^1]: [fable-compiler/static-web-generator: Simple Fable Node.js app to generate static pages](https://github.com/fable-compiler/static-web-generator)
[^2]: [Template - Fulma](https://fulma.github.io/Fulma/#template)

[Fable の site](https://github.com/fable-compiler/fable-compiler.github.io) で使われている [MangelMaxime/Nacara](https://github.com/MangelMaxime/Nacara) を試してみたけど、これ RSS 以外をほぼ自分で組り込むところがなくて F# で書く楽しさなさそうったので却下。

ここまで来たら、自力で練習がてら Fable の無垢のテンプレに付け足ししていくしかあるまいなとなった。というか周辺ツールの不理解もあって 1 から自分でやらないと無理ゲーと化してた。

まず [Fable · Start a new project](https://fable.io/docs/2-steps/your-first-fable-project.html) で素朴なプロジェクトを作成しする。

次に [fable-compiler/static-web-generator](https://github.com/fable-compiler/static-web-generator) でやってることを移植していく。
ここではエントリポイントで Markdown などのコンテンツを読み込んでテンプレに埋め込み、 HTML にレンダリングした結果をファイル出力している。
なので、まずは素朴な手書きの HTML でいいからファイル出力できる状態にし、その後 React の wrapper を追加する形で進める。

RSS は、 [Fable · .NET and F# compatibility](https://fable.io/docs/dotnet/compatibility.html) を見るに .NET の機能はまあ使えないので、 Node.js で xml を出力するやつを移植することになるハズ。
なんかハードな気配してきた。

---

何度も作っては消しを繰り返して得た確かな手順は以下の通り。

- Fable のプロジェクト作成は [Fable · Start a new project](https://fable.io/docs/2-steps/your-first-fable-project.html) を参照
- .NET 系ツールの取り回しは以下を参照
  - [How to manage .NET tools - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)
  - [global.json overview - .NET CLI | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
- (後述する) Femto は [Fable · Introducing Femto](https://fable.io/blog/2019/2019-06-29-Introducing-Femto.html) を参照

[Paket](https://github.com/fsprojects/Paket) を使うと依存関係の管理が楽になる(project ファイルから外に出せるから)のだけど、今回は導入しなかった。関係するツールが多すぎて腹いっぱいになってる。

```powershell
# 無垢の Fable project 作成する。
dotnet new install Fable.template
dotnet new fable --name blog-fable
cd blog-fable
git init
git commit --allow-empty -m 'Initial commit.'
git add .
git commit -m 'Add minimal Fable template.'

# dotnet tool を更新する。
dotnet tool update fable
dotnet tool update femto

# 動くことを確認する。
npm install
npm run start

# global.json の更新方法わからないので力づくで書き換えて更新する。
dotnet new globaljson --sdk-version 7.0.203 --roll-forward latestFeature --force
dotnet tool restore
```

build target が .Net Core 2.0 なので .Net 7 にする。
`src\App.fsproj` を編集する。

```patch
 <Project Sdk="Microsoft.NET.Sdk">
   <PropertyGroup>
-    <TargetFramework>netstandard2.0</TargetFramework>
+    <TargetFramework>net7</TargetFramework>
   </PropertyGroup>
   <ItemGroup>
     <Compile Include="App.fs" />
```

Fable も最新版の 4 にする。
これには [Femto](https://github.com/Zaid-Ajaj/Femto) という tool を使うみたい。 Femto は packet と npm の仲介役のようなやつで、 Nuget から Fable のモジュールを持ってきたらついでに npm してくれるようなやつっぽい。
さっぱりわからんが、コマンド打ったら確かに更新された。

```powershell
femto install Fable.Core .\src
femto install Fable.Browser.Dom .\src
```

↓ こうなった。

```patch
   <ItemGroup>
-    <PackageReference Include="Fable.Browser.Dom" Version="2.2.0" />
-    <PackageReference Include="Fable.Core" Version="3.2.3" />
+    <PackageReference Include="Fable.Browser.Dom" Version="2.14.0" />
+    <PackageReference Include="Fable.Core" Version="4.0.0" />
   </ItemGroup>
```

`npm run start` `npm run build` あたりが動いてたら大丈夫だろう。
(`dotnet tool restore` が `postinstall` で走る記述になってる)

webpack も古かったのでとりあえず上げておいたが、 SSG したいだけなのでこれらは後ほど消した。

ついでに solution も作っとく。

```powershell
dotnet new sln
dotnet sln add ./src
```

ここまでやってまだテンプレが最新化されただけ...

ここからが本番。
無味乾燥したテンプレに [fable-compiler/static-web-generator](https://github.com/fable-compiler/static-web-generator) の要素を足していく。

以下の Fable の package が使われており、そのうちいくつかは deprecated されてる。分割されている package を追うのは直に repo でコードを追うしかなく、なかなかハードな道のりだった。
そして調べた内容で書き換えをした。書き換えの内容は [krymtkts/blog-fable](https://github.com/krymtkts/blog-fable) を直に見るのが良い(書くのめんどい)。

- Fable.PowerPack ← [fable-compiler/fable-powerpack: Utilities for Fable apps](https://github.com/fable-compiler/fable-powerpack)
  - [Split Fable.PowerPack in multiple packages? · Issue #63 · fable-compiler/fable-powerpack](https://github.com/fable-compiler/fable-powerpack/issues/63) 分割された
  - そもそも使ってなくね？ということで移植しない
- Fable.React ← [Zaid-Ajaj/Feliz](https://github.com/Zaid-Ajaj/Feliz) 使えと
- Fable.Import 系はデカすぎて repo が分かれた [Track repo splits · Issue #80 · fable-compiler/fable-import](https://github.com/fable-compiler/fable-import/issues/80)
  - Fable.Import.Browser ← [fable-browser](https://github.com/fable-compiler/fable-browser)
  - Fable.Import.Node ← [fable-compiler/fable-node: Bindings for node.js native modules](https://github.com/fable-compiler/fable-node) めちゃ古だがまだ動くらしい？
- Fulma ← 今回は使わなかった

これら新しい package を移植し多少のコードを変更することで、やっと Markdown(今は `README.md` のみ) → HTML とパースして表示できるようになった。

---

ひとまずここまで。

まだたった 1 ページを出力できただけで先は長いが、 Fable の雰囲気わかってきてできそうな感触を得ている。

やっぱ Node.js の変更周り全然わからんことだらけなので、そこは [Nacara/Node.Extra.fs at master · MangelMaxime/Nacara](https://github.com/MangelMaxime/Nacara/blob/master/src/Nacara.Core/Node.Extra.fs#L8) のイケてる実装を参照するなどして組んでいけたら良さそう。

あとやっぱ F# は .NET へ重依存なので .NET を部分的にしか使わない Fable だとかなり書き味が違う。これに慣れるのはちょっとかかりそう。

続く。
