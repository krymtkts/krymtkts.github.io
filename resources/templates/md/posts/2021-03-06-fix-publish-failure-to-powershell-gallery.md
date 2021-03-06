{:title "PSGallery への公開つまづき 2021"
:layout :post
:tags ["powershell"]}

今年のはじめに[krymtkts/Get-GzipContent: Get-Content for gzip files.](https://github.com/krymtkts/Get-GzipContent)の更新を行った際に、PoweShell Gallery への公開で手間取った。その 2 ヶ月遅れの記録である。

[Publish-Module throws error "Failed to generate the compressed file for module 'Microsoft (R) Build Engine version 15.7.179.6572 for .NET Core'" · Issue #303 · PowerShell/PowerShellGetv2](https://github.com/PowerShell/PowerShellGetv2/issues/303#issuecomment-433139506)

```powershell
Invoke-WebRequest -Uri https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile "$env:LOCALAPPDATA\Microsoft\Windows\PowerShell\PowerShellGet\NuGet.exe"
```

いやまあこれで直ったんやけど、わからなさすぎてこの記事も見た。

[Fixing the ‘Failed to generate the compressed file for module ‘C:\Program Files\dotnet\dotnet.exe’ error when deploying to the PowerShell Gallery using Azure DevOps | SQL DBA with A Beard](https://sqldbawithabeard.com/2019/11/26/fixing-the-failed-to-generate-the-compressed-file-for-module-cprogram-filesdotnetdotnet-exe-error-when-deploying-to-the-powershell-gallery-using-azure-devops/)

これは一時しのぎなので最終的に目指すのは配置している`nuget.exe`を消し去っても動くようになることだ。とはいえまた次公開するときまで使うときがないので試すのめんどくせええ＆次試そうと思っても忘れる...ということで一旦 Chocolatey で NuGet を入れておいて保険とした。でもこれ自体も本来バイナリ不要で動いてたことからしたら蛇足のはずやねんけどな、Issue に進捗なく Close されてるからわからん。まあ日記にも書いたからエラーしたときにきっと振り返れる、未来のワイ。

あと年に数回しか PowerShell Gallery に公開しないとやり方とか色々忘れるのだけど、一番忘れるのが API キーの寿命が短く設定してあって切れてるということ。今回期限切れの API キーを再有効化できるってのを知ったので、それはそれで良。

[API キーの管理 - PowerShell | Microsoft Docs](https://docs.microsoft.com/ja-jp/powershell/scripting/gallery/how-to/managing-profile/creating-apikeys?view=powershell-7.1#editing-and-deleting-existing-api-keys)
