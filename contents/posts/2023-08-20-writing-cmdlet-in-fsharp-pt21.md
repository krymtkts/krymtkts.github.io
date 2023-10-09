{:title "F# でコマンドレットを書いてる pt.21"
:layout :post
:tags ["fsharp","powershell"]}

[pocof](https://github.com/krymtkts/pocof) の開発をした。
[pocof のコンパイルを .NET 7 にした](/posts/2023-07-16-writing-cmdlet-in-fsharp-pt20) こともあってなんかテンション的に触りやすくなった(逆に Fable でブログ書き直す方を放置)。
[PSResourceGet への乗り換え](/posts/2023-08-13-test-publish-psresource) もできたのだけど、その時気になることがあった。

いつかはやらないといけないと考えていたが、配布 DLL が多すぎる。
[PowerShell Gallery | pocof 0.5.0-alpha](https://www.powershellgallery.com/packages/pocof/0.5.0-alpha) の FileList 見たらわかるけどめちゃくちゃ DLL がある。
pocof で使ってない DLL もいっぱい含まれている。

これは...解決したい。

.NET では実行可能ファイルであれば自己完結型と言われる単一バイナリの生成ができるのだけど、 DLL ではそういうのはない様子。
PowerShell モジュールの場合どういう手段が取れるのかちゃんと理解してなかったが、こういうこと ↓ で大量の DLL 全部ぶちまけられてた。

- [Microsoft.PowerShell.SDK](https://www.nuget.org/packages/Microsoft.PowerShell.SDK/#versions-body-tab) を参照してる
- F# の各言語に localize されたリソースの DLL が出力される

これらに対処してバンドルに含まれる DLL を減らしてみた。 [#57](https://github.com/krymtkts/pocof/pull/57)

### Microsoft.PowerShell.SDK

pocof のような PowerShell モジュールは PowerShell のランタイム内で動く。
また pocof では PowerShell の機能をモリモリ使って出力結果を生成するようなことがない。
このようなケースでは、 [PowerShell/PowerShellStandard](https://github.com/PowerShell/PowerShellStandard) のリファレンスライブラリだけでいいみたい。

pocof で `Microsoft.PowerShell.SDK` を `PowerShellStandard.Library` に入れ替えて全テスト実行してみた。
テストデータ作成のために `PSObject` を作成しているところだけ影響があった。
つまり pocof 本体に影響はないので、テストプロジェクトで `Microsoft.PowerShell.SDK` を参照してれば OK ということ。

[一番初めに pocof を書き始めた](/posts/2022-05-07-start-to-write-cmdlet-by-fsharp)ころの記事では `PowerShellStandard.Library` を参照してたのだけどなんか途中で変えてみたい。
初回リリースでは既に `Microsoft.PowerShell.SDK` に変わってたのだけど、この頃は [FsUnit](https://fsprojects.github.io/FsUnit/) 使ったテストなかった気がするし、違う理由かなんか分からず変えてそう。コミットログ追えばわかるかな。

### F# の localize されたリソースの DLLs

pocof では localize されたリソースの DLL 要らない。なんか F# のリソースが使われるとしても英語でいいし。
これらの各言語リソースの DLL を取り除くには `SatelliteResourceLanguages` という MSBuild プロパティを指定すれば良いようだ。言語を指定せずに空(`null`)だと全言語対象のママになるので、英語を指定しないといけない。

- [What is the purpose of the FSharp.Core.resource.dll files? - General - F# Software Foundation Community Forums](https://forums.fsharp.org/t/what-is-the-purpose-of-the-fsharp-core-resource-dll-files/1402)
- [SatelliteResourceLanguages | Microsoft.NET.Sdk の MSBuild プロパティ - .NET | Microsoft Learn](https://learn.microsoft.com/ja-jp/dotnet/core/project-sdk/msbuild-props#satelliteresourcelanguages)

### 終わり

MUSBuild の世界は難しいなー。でもこれでかなり DDL が減った。
ただ最後に `System.Management.Automation.dll` が残ってる。

これって PowerShell 環境ならかならずあるのでは...という気がするのだけど。
open してる名前空間の関係で消えないのかな？もうちょい検証してみないとわからん。
