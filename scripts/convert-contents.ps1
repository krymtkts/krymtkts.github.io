Get-ChildItem ./contents/posts/*.md | ForEach-Object {
    $match = Get-Content $_.FullName -Raw | Select-String '(?s)^\{\:.+?\}'
    if (-not $match.Matches) {
        return
    }
    $fm = $match.Matches[0].Value
    $article = $match.Line.Remove(0, $fm.Length)
    $newFm = $fm -replace '(\{|\})', '' -replace ':title', 'title:' -replace ' ?:tags', 'tags:' -replace ' ?:layout.+\n', ''
    @"
---
$newFm
---$article
"@ | Set-Content $_.FullName -Encoding UTF8 -NoNewline
}
