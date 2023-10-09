# NOTE: convert pages/*.md manually.
Get-ChildItem ./contents/posts/*.md | ForEach-Object {
    $match = Get-Content $_.FullName -Raw | Select-String '(?s)^\{\:.+?\}'
    if (-not $match.Matches) {
        return
    }
    $fm = $match.Matches[0].Value
    $article = $match.Line.Remove(0, $fm.Length)
    $newArticle = $article -replace '\((/(posts|tags)/.+?)\)', '($1.html)' -replace 'poershell', 'powershell' -replace '```(log|textile)', '```plaintext'
    $newFm = $fm -replace '(\{|\})', '' -replace ':title', 'title:' -replace ' ?:tags', 'tags:' -replace ' ?:layout.+\n', ''
    # NOTE: replace '(tags:.+?") "' "\n```\n[^\n]" and "../pages/about" manually.
    @"
---
$newFm
---$newArticle
"@ | Set-Content $_.FullName -Encoding UTF8 -NoNewline
}

<#
# NOTE: for organization posts into subdirectories.
2019..2023 | ForEach-Object {
    mkdir ./contents/posts/$_ -Force | Out-Null
    $Year = $_
    Get-ChildItem "./contents/posts/${Year}*.md" -File | ForEach-Object { git mv (Resolve-Path $_.FullName -Relative) "./contents/posts/${Year}/$($_.Name)" }
}
#>