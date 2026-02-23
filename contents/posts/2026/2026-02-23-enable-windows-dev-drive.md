---
title: "Windows の Dev Drive を有効にする"
tags: ["windows", "dotnet"]
---

storage 貧者だったので使ってなかったが、 laptop 変えて増えたので Dev Drive を設定してみた。

- [Set up a Dev Drive on Windows 11 | Microsoft Learn](https://learn.microsoft.com/en-us/windows/dev-drive/)
    - よくわかってないが Dev Drive は ReFS を採用している。 [Resilient File System (ReFS) overview | Microsoft Learn](https://learn.microsoft.com/en-us/windows-server/storage/refs/refs-overview)
- Dev Drive のところにも載ってるが、 NuGet の cache などの directory について
    - [How to manage the global packages, HTTP cache, temp folders in NuGet | Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders)

Windows の UI から設定する。サイズは 256 GB にした。

Dev Drive の文書の NuGet の箇所をもとに cache などを Dev Drive に設定する。

```plaintext
> dotnet nuget locals all --list
http-cache: C:\Users\takatoshi\AppData\Local\NuGet\v3-cache
global-packages: C:\Users\takatoshi\.nuget\packages\
temp: C:\Users\takatoshi\AppData\Local\Temp\NuGetScratch
plugins-cache: C:\Users\takatoshi\AppData\Local\NuGet\plugins-cache
```

環境変数で設定できる様子。

```powershell
setx NUGET_PACKAGES D:\nuget\packages
setx NUGET_HTTP_CACHE_PATH D:\nuget\http-cache\v3-cache
```

npm 等も Dev Drive の文書をもとに設定する。

```plaintext
> npm config get cache
C:\Users\takatoshi\AppData\Local\npm-cache
> npm config set cache D:\npm-cache --global
```

ついでに仕事用 laptop も Dev Drive 設定してみた。
私物では使ってないが仕事で使う [Yarn](https://classic.yarnpkg.com/lang/en/docs/cli/cache/#toc-change-the-cache-path-for-yarn) も Dev Drive を使わせてみた。

```plaintext
> setx YARN_CACHE_FOLDER D:\yarn-cache
```

仕事でよく利用する [nektos/act](https://github.com/nektos/act), [AWS CDK](https://github.com/aws/aws-cdk), [OpenAI Codex](https://openai.com/codex/) とかも cache があるのだけど、今は設定してない。
今回は cache だけ Dev Drive を使わせたが、 `node_modules` のような小さいファイルが大量にあるものは、本当は repo 丸ごと Dev Drive 配置がベストっぽい。
だけど今はなんとなくやらなかった。

仕事で使う場合だと、 PHP と TypeScript を Docker で動かすため、 bind mount では ReFS の良くないところが目立つようなので中々手出しできなと思った。
一般的にこのようなユースケースでは WSL2 側で `git clone` するのが良いようだが、 `ghq` で管理してるのもありやってない。

ここまでの Dev Drive の設定自体は何も困らなかった。
ただ設定後に `dotnet tool restore` しても tool が見つからない状態になって、非常に困った。
原因は以下のように `dotnet tool` の問題みたい。

['Dotnet tool restore' does not work correctly when NUGET_PACKAGES (global-packages) is set to an alternative location · Issue #11432 · dotnet/sdk](https://github.com/dotnet/sdk/issues/11432#issuecomment-1020543559)

`$env:USERPROFILE\.dotnet\toolResolverCache` にある dotnet tool の path が cache されてるらしい。
cache を消去しないと dotnet tool が Dev Drive の path を見ず、正しい restore が行われない。やば。

確認してみたら中にはこういう cache があって path が古いままだった。

```json
[{"Version":"5.0.0-alpha.22","TargetFramework":"net10.0","RuntimeIdentifier":"any","Name":"fable","Runner":"dotnet","PathToExecutable":"C:\\Users\\takatoshi\\.nuget\\packages\\fable\\5.0.0-alpha.22\\tools/net10.0/any/fable.dll"},{"Version":"5.0.0-alpha.23","TargetFramework":"net10.0","RuntimeIdentifier":"any","Name":"fable","Runner":"dotnet","PathToExecutable":"C:\\Users\\takatoshi\\.nuget\\packages\\fable\\5.0.0-alpha.23\\tools/net10.0/any/fable.dll"}]
```

cache を消す。

```powershell
rm $env:USERPROFILE\.dotnet\toolResolverCache
```

この後 `dotnet tool restore` したらきれいに cache が再生成され、 Dev Drive を見るようになる。

これを自力で解決するのは難しいな。 AI は素っ頓狂なことを言うし、自力でググってよかった。
[`dnx` ならこれが起こらんらしい](https://github.com/dotnet/sdk/issues/11432#issuecomment-3118705720)が仕組みが違うっぽい。試してないのでわからん。

Dev Drive に変えてみて特に性能面で優れた点をまだ感じてないが、今後のデフォみたいだし徐々に利用範囲を広めたい所存。
