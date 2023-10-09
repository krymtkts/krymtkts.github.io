{:title "はじめてのPowerShell Galleryへの公開"
 :layout :post
 :tags  ["powershell", "powershellgallery", "maven"]}

先日、自前のモジュールをPowerShell Galleryに公開したので、その時のメモを記す。

[PowerShell Gallery | MavenAutoCompletion 0.1](https://www.powershellgallery.com/packages/MavenAutoCompletion/0.1)

PowerShell Galleryのアカウントを作るだとか、モジュールのAnalysisだとかはまた別で書こう。ここではモジュールの公開の部分だけ。

### TL;DR

`Poblish-Module`は除外ファイル設定とかないから公開時には注意しましょう😭

### モジュールの公開

以下の手順に従う。

[アイテムの作成と公開 | Microsoft Docs](https://docs.microsoft.com/ja-jp/powershell/gallery/how-to/publishing-packages/publishing-a-package)

事前にチェックしろよな！！！と書いてるのでそれに従い以下のコマンドを実行する

```powershell
Publish-Module -Path ".\MavenAutoCompletion" -NugetAPIKey "キーは見せられないよ" -WhatIf -Verbose
```

途中で最新のNuGet入れるかい？と聞かれるのでそれはYesで。

```powershell
$ Publish-Module -Path ".\MavenAutoCompletion" -NugetAPIKey "キーは見せられないよ" -WhatIf -Verbose
VERBOSE: Acquiring providers for assembly: C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\PackageManagement\1.3.1\coreclr\netstandard2.0\Microsoft.PackageManagement.NuGetProvider.dll
VERBOSE: Acquiring providers for assembly: C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\PackageManagement\1.3.1\coreclr\netstandard2.0\Microsoft.PackageManagement.MetaProvider.PowerShell.dll
VERBOSE: Acquiring providers for assembly: C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\PackageManagement\1.3.1\coreclr\netstandard2.0\Microsoft.PackageManagement.ArchiverProviders.dll
VERBOSE: Acquiring providers for assembly: C:\Users\takatoshi\OneDrive\Documents\PowerShell\Modules\PackageManagement\1.3.1\coreclr\netstandard2.0\Microsoft.PackageManagement.CoreProviders.dll

NuGet.exe is required to continue
This version of PowerShellGet requires minimum version '4.1.0' of NuGet.exe to publish an item to the
NuGet-based repositories. NuGet.exe must be available in
'C:\ProgramData\Microsoft\Windows\PowerShell\PowerShellGet\' or
'C:\Users\takatoshi\AppData\Local\Microsoft\Windows\PowerShell\PowerShellGet\', or under one of the
paths specified in PATH environment variable value. NuGet.exe can be downloaded from
https://aka.ms/psget-nugetexe. For more information, see https://aka.ms/installing-powershellget . Do
you want PowerShellGet to install the latest version of NuGet.exe now?
[Y] Yes  [N] No  [S] Suspend  [?] Help (default is "Y"): y
VERBOSE: Installing NuGet.exe.
VERBOSE: GET https://aka.ms/psget-nugetexe with 0-byte payload
VERBOSE: received 5690456-byte response of content type application/x-msdownload
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Publish Location:'https://www.powershellgallery.com/api/v2/package/'.
VERBOSE: Module 'MavenAutoCompletion' was found in 'C:\Users\takatoshi\dev\powershell\MavenAutoCompletion'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Using the provider 'PowerShellGet' for searching packages.
VERBOSE: Using the specified source names : 'PSGallery'.
VERBOSE: Getting the provider object for the PackageManagement Provider 'NuGet'.
VERBOSE: The specified Location is 'https://www.powershellgallery.com/api/v2/items/psscript/' and PackageManagementProvider is 'NuGet'.
VERBOSE: Searching repository 'https://www.powershellgallery.com/api/v2/items/psscript/FindPackagesById()?id='MavenAutoCompletion'' for ''.
VERBOSE: Total package yield:'0' for the specified package 'MavenAutoCompletion'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Using the provider 'PowerShellGet' for searching packages.
VERBOSE: Using the specified source names : 'PSGallery'.
VERBOSE: Getting the provider object for the PackageManagement Provider 'NuGet'.
VERBOSE: The specified Location is 'https://www.powershellgallery.com/api/v2/' and PackageManagementProvider is 'NuGet'.
VERBOSE: Searching repository 'https://www.powershellgallery.com/api/v2/FindPackagesById()?id='MavenAutoCompletion'' for ''.
VERBOSE: Total package yield:'0' for the specified package 'MavenAutoCompletion'.
What if: Performing the operation "Publish-Module" on target "Version '0.0.1' of module 'MavenAutoCompletion'".
```

それらしいエラーも警告も出ないので、実行に移す。

```powershell
$ Publish-Module -Path ".\MavenAutoCompletion" -NugetAPIKey "キーは見せられないよ" -Verbose
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Publish Location:'https://www.powershellgallery.com/api/v2/package/'.
VERBOSE: Module 'MavenAutoCompletion' was found in 'C:\Users\takatoshi\dev\powershell\MavenAutoCompletion'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Using the provider 'PowerShellGet' for searching packages.
VERBOSE: Using the specified source names : 'PSGallery'.
VERBOSE: Getting the provider object for the PackageManagement Provider 'NuGet'.
VERBOSE: The specified Location is 'https://www.powershellgallery.com/api/v2/items/psscript/' and PackageManagementProvider is 'NuGet'.
VERBOSE: Searching repository 'https://www.powershellgallery.com/api/v2/items/psscript/FindPackagesById()?id='MavenAutoCompletion'' for ''.
VERBOSE: Total package yield:'0' for the specified package 'MavenAutoCompletion'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Using the provider 'PowerShellGet' for searching packages.
VERBOSE: Using the specified source names : 'PSGallery'.
VERBOSE: Getting the provider object for the PackageManagement Provider 'NuGet'.
VERBOSE: The specified Location is 'https://www.powershellgallery.com/api/v2/' and PackageManagementProvider is 'NuGet'.
VERBOSE: Searching repository 'https://www.powershellgallery.com/api/v2/FindPackagesById()?id='MavenAutoCompletion'' for ''.
VERBOSE: Total package yield:'0' for the specified package 'MavenAutoCompletion'.
VERBOSE: Performing the operation "Publish-Module" on target "Version '0.0.1' of module 'MavenAutoCompletion'".
VERBOSE: Pushing MavenAutoCompletion.0.0.1.nupkg to 'https://www.powershellgallery.com/api/v2/package/'...
  PUT https://www.powershellgallery.com/api/v2/package/
�x��: <licenseUrl> element will be deprecated, please consider switching to specifying the license in
the package. Learn more: https://aka.ms/deprecateLicenseUrl.
  Created https://www.powershellgallery.com/api/v2/package/ 3489ms
Your package was pushed.

VERBOSE: Successfully published module 'MavenAutoCompletion' to the module publish location 'https://www.powershellgallery.com/api/v2/package/'. Please allow few minutes for 'MavenAutoCompletion' to show up in the search results.
```

> �x��: <licenseUrl> element will be deprecated, please consider switching to specifying the license in
> the package. Learn more: https://aka.ms/deprecateLicenseUrl.

う、モジュールのマニフェストでdeprecatedな属性があるが...いけたっぽい。この文字化けはemojiかな(Win10 1809ではterminalでemoji化けがあるのは既知)

### いけて...ない！

困ったことにイケてなかった...gitのオブジェクトとかそういうのまで全部publishedなかんじでまじで笑えねえ...とりあえずPowerShell Gallery上でリスト表示しないリクエストを出しておいたが笑えねえ😭

どうもこれはPowerShell Getの既知の問題？みたい...事前調査が足りてなかったぜえ...😭

[Ignore files when using `Publish-Module` · Issue #191 · PowerShell/PowerShellGet](https://github.com/PowerShell/PowerShellGet/issues/191)

現状できることとしては、別にモジュールと同名のフォルダを作って、その中にpublishしたいファイルをコピーし、`Publish-Module`を実行するしかないのではないかなと思う🤔

この際なので↑の取りなしでvr0.1として公開することにする。

```powershell
$ Publish-Module -Path ".\MavenAutoCompletion" -NugetAPIKey "キーは見せられないよ" -Verbose
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Publish Location:'https://www.powershellgallery.com/api/v2/package/'.
VERBOSE: Module 'MavenAutoCompletion' was found in 'C:\Users\takatoshi\dev\powershell\MavenAutoCompletion\publish\MavenAutoCompletion'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Using the provider 'PowerShellGet' for searching packages.
VERBOSE: Using the specified source names : 'PSGallery'.
VERBOSE: Getting the provider object for the PackageManagement Provider 'NuGet'.
VERBOSE: The specified Location is 'https://www.powershellgallery.com/api/v2/items/psscript/' and PackageManagementProvider is 'NuGet'.
VERBOSE: Searching repository 'https://www.powershellgallery.com/api/v2/items/psscript/FindPackagesById()?id='MavenAutoCompletion'' for ''.
VERBOSE: Total package yield:'0' for the specified package 'MavenAutoCompletion'.
VERBOSE: Repository details, Name = 'PSGallery', Location = 'https://www.powershellgallery.com/api/v2/'; IsTrusted = 'True'; IsRegistered = 'True'.
VERBOSE: Using the provider 'PowerShellGet' for searching packages.
VERBOSE: Using the specified source names : 'PSGallery'.
VERBOSE: Getting the provider object for the PackageManagement Provider 'NuGet'.
VERBOSE: The specified Location is 'https://www.powershellgallery.com/api/v2/' and PackageManagementProvider is 'NuGet'.
VERBOSE: Searching repository 'https://www.powershellgallery.com/api/v2/FindPackagesById()?id='MavenAutoCompletion'' for ''.
VERBOSE: Total package yield:'1' for the specified package 'MavenAutoCompletion'.
VERBOSE: Performing the operation "Publish-Module" on target "Version '0.1' of module 'MavenAutoCompletion'".
VERBOSE: Pushing MavenAutoCompletion.0.1.0.nupkg to 'https://www.powershellgallery.com/api/v2/package/'...
  PUT https://www.powershellgallery.com/api/v2/package/
�x��: <licenseUrl> element will be deprecated, please consider switching to specifying the license in
the package. Learn more: https://aka.ms/deprecateLicenseUrl.
  Created https://www.powershellgallery.com/api/v2/package/ 4558ms
Your package was pushed.

VERBOSE: Successfully published module 'MavenAutoCompletion' to the module publish location 'https://www.powershellgallery.com/api/v2/package/'. Please allow few minutes for 'MavenAutoCompletion' to show up in the search results.
```

これによって一応無事にPowerShell Getでの公開はできたし、一旦コレでええか😅

今度非推奨になってる`<licenseUrl>`を変えなあかんな。

ファイルコピって公開するためのスクリプトを起こしたので、次回はそれでやろうと思う。
