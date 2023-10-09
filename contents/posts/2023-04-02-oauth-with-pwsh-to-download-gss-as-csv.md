---
title: "PowerShell で OAuth して Google Sheet を CSV で DL する"
tags: ["powershell"]
---

最近役目を終えたスクリプトに、 Google Sheet を CSV でダウンロードするやつがいた。考慮不足等、諸々至らぬ点あろうが、スクリプトを供養するため日記にする。

モジュール導入したくないなーという事情があったので、下記を見てヒイヒイ言いながら実装した記憶がある。
「Google Sheet から CSV をダウンロードする」には `https://www.googleapis.com/auth/drive` のスコープがいるみたい。てっきりスプシのスコープと勘違いして、無駄に時間かかった。

- [How to connect API from PowerShell with OAUTH 2.0? - Stack Overflow](https://stackoverflow.com/questions/63125283/how-to-connect-api-from-powershell-with-oauth-2-0)
- [Connect to Google API with Powershell — LazyAdmin](https://lazyadmin.nl/it/connect-to-google-api-with-powershell/) して - [r googlesheets - How to export a csv from Google Sheet API? - Stack Overflow](https://stackoverflow.com/questions/37705553/how-to-export-a-csv-from-google-sheet-api/61107170#61107170) する

スクリプトは Gist に登録しておいた。
それに伴い Comment-Based Help を書いたり、関数の名前を変えたりした。Google Sheets ってサービス名に対してスプシ自体は spreadsheet だったりしてマジでややこしい。

[GoogleSpreadSheetOAuth.ps1](https://gist.github.com/krymtkts/0618fe495df0d3e567e9fb18ca2e308b)

```powershell
<#
.SYNOPSIS
Download Google Sheet as CSV.
.DESCRIPTION
Download Google Sheet as CSV using GCP OAuth Client.
.PARAMETER OAuthClientSecretsPath
The path to the JSON file for the OAuth 2.0 Client Secrets created on GCP.
.PARAMETER OAuthStorePath
The path to save authentication tokens.
.EXAMPLE
. ./GoogleSheetsOAuth.ps1 `
    -OAuthClientSecretsPath './client-secrets.json' `
    -OAuthStorePath './gss-credential'
Get-GoogleSpreadSheetAsCsv `
    -SpreadSheet 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx' `
    -SheetId '000000000' `
    -OutFile './test.csv'
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $OAuthClientSecretsPath,
    [Parameter(Mandatory)]
    [string]
    $OAuthStorePath
)

class OAuthCredentialStore {
    [SecureString] $AccessToken
    [SecureString] $RefreshToken
    OAuthCredentialStore(
        [SecureString] $AccessToken,
        [SecureString] $RefreshToken
    ) {
        $this.AccessToken = $AccessToken
        $this.RefreshToken = $RefreshToken
    }
}

$script:OAuthCredential = $null

function Get-GoogleSheetsAuthInitToken {
    [CmdletBinding(SupportsShouldProcess)]
    param ()

    if (-not (Split-Path $OAuthStorePath -Parent | Test-Path)) {
        throw "${OAuthStorePath} not found."
    }

    if (-not (Test-Path $OAuthClientSecretsPath)) {
        throw "${OAuthClientSecretsPath} not found."
    }
    $secrets = Get-Content $OAuthClientSecretsPath | ConvertFrom-Json | Select-Object -ExpandProperty installed

    $Scope = [System.Web.HttpUtility]::UrlEncode('https://www.googleapis.com/auth/drive')
    $ClientID = $secrets.client_id
    $ClientSecret = $secrets.client_secret
    $RedirectUri = $secrets.redirect_uris[0]
    $Uri = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&client_id=${ClientID}&redirect_uri=${RedirectUri}&scope=${Scope}&access_type=offline"

    Start-Process $Uri
    $AuthorizationCode = Read-Host 'paste auth code here!'

    $AuthData = @{
        code = $AuthorizationCode;
        client_id = $ClientID;
        client_secret = $ClientSecret;
        redirect_uri = $RedirectUri;
        grant_type = 'authorization_code';
        access_type = 'offline';
    }

    Write-Host 'try to get access token...'
    $TokenResponse = Invoke-RestMethod -Method Post -Uri 'https://www.googleapis.com/oauth2/v4/token' -Body $AuthData
    if (-not $?) {
        throw 'request failed.'
    }

    Write-Host 'done.'

    $script:OAuthCredential = [OAuthCredentialStore]::new(
        ($TokenResponse.access_token | ConvertTo-SecureString -AsPlainText),
        ($TokenResponse.refresh_token | ConvertTo-SecureString -AsPlainText)
    )

    $store = @{
        AccessToken = $OAuthCredential.AccessToken | ConvertFrom-SecureString;
        RefreshToken = $OAuthCredential.RefreshToken | ConvertFrom-SecureString;
    }

    if ($PSCmdlet.ShouldProcess($OAuthStorePath)) {
        New-Item -Path $OAuthStorePath -Force | Out-Null
        $store | ConvertTo-Json -Compress | Set-Content -Path $OAuthStorePath -Force
    }

    $OAuthCredential.AccessToken, $OAuthCredential.RefreshToken
}

function Get-GoogleSheetsAuthToken {
    [CmdletBinding(SupportsShouldProcess)]
    param ()
    Write-Verbose "$OAuthStorePath"
    $content = Get-Content -Path $OAuthStorePath -ErrorAction Ignore
    if ([String]::IsNullOrEmpty($content)) {
        $token, $_ = Get-GoogleSheetsAuthInitToken
        return $token
    }
    try {
        $cred = $content | ConvertFrom-Json
        $script:OAuthCredential = [OAuthCredentialStore]::new(
            ($cred.AccessToken | ConvertTo-SecureString),
            ($cred.RefreshToken | ConvertTo-SecureString)
        )
    }
    catch {
        throw 'Invalid SecureString stored for this module. Remove gs-credential and try again.'
    }
    $RefreshToken = $script:OAuthCredential.RefreshToken | ConvertFrom-SecureString -AsPlainText
    if (-not $RefreshToken) {
        $token, $_ = Get-GoogleSheetsAuthInitToken
        return $token
    }

    $secrets = Get-Content $OAuthClientSecretsPath | ConvertFrom-Json | Select-Object -ExpandProperty installed
    $RefreshData = @{
        refresh_token = $RefreshToken
        client_id = $secrets.client_id
        client_secret = $secrets.client_secret
        grant_type = 'refresh_token'
        access_type = 'offline'
    }

    $Response = Invoke-RestMethod -Method Post -Uri 'https://www.googleapis.com/oauth2/v4/token' -Body $RefreshData
    if (-not $?) {
        throw 'request failed.'
    }

    return $Response.access_token | ConvertTo-SecureString -AsPlainText
}

function Get-GoogleSpreadSheetAsCsv {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [String]
        $SpreadSheet,
        [Parameter(Mandatory)]
        [String]
        $SheetId,
        [Parameter(Mandatory)]
        [String]
        $OutFile
    )

    $accessToken, $refreshToken = Get-GoogleSheetsAuthToken
    if (-not $?) {
        return
    }

    $Params = @{
        Uri = "https://docs.google.com/spreadsheets/d/$SpreadSheet/gviz/tq?tqx=out:csv&gid=$($_.SheetId)"
        Method = 'GET'
        Authentication = 'Bearer'
        Token = $accessToken
        OutFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile)
    }
    Invoke-WebRequest @Params | Out-Null
}
```

このスクリプトでは認証コードを得るのに手でコピってる。
これを `System.Net.HttpListener` で自鯖を立てて取る方式にしたかったけど、コピペするのは初回だけだし放置してたらそのままスクリプトの寿命が来てしまった。
自動で認証コードを得る方式は以前ググってそれっぽいの見つけてたはずだが、 URL を失念してしまった。
今なら ChatGPT サンにお願いしたら書いてくれそうな雰囲気するなと思い試したらそれっぽいのが出たが、 OAuth client の Redirect URL を変えるのが手間で検証しなかった。怠けてる。
次にやる機会が来るその日まで、自鯖で認証コード取る方式は寝かせておく(やる日は来るのか)。
