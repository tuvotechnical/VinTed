$ErrorActionPreference = 'Stop'

$repo = 'tuvotechnical/VinTed'
$tagName = 'v1.2.1'
$zipPath = Join-Path (Get-Location) 'VinTed-v1.2.1.zip'
$ghExe = Join-Path (Get-Location) '.tools\gh\bin\gh.exe'

if (!(Test-Path $ghExe)) {
    throw 'GitHub CLI portable not found: ' + $ghExe
}

if (!(Test-Path $zipPath)) {
    throw 'Release ZIP not found: ' + $zipPath
}

Write-Host 'Paste GitHub token, then press Enter:' -ForegroundColor Yellow
$token = [Console]::ReadLine()
if ([string]::IsNullOrWhiteSpace($token)) {
    throw 'GitHub token is empty.'
}

$env:GH_TOKEN = $token.Trim()
$env:GITHUB_TOKEN = $env:GH_TOKEN

Write-Host 'Checking GitHub authentication...' -ForegroundColor Cyan
& $ghExe auth status --hostname github.com
if ($LASTEXITCODE -ne 0) {
    throw 'GitHub authentication failed.'
}

$releaseNotes = @"
## VinTed v1.2.1

Fix auto-update version normalization.

### Changes
- Normalize GitHub release tags and assembly versions to SemVer x.y.z before display/comparison.
- Compare versions with System.Version to avoid incorrect update prompts such as local 1.1.5 being treated as older than malformed 1.1.15 values.
- Update README and internal error log for release tooling issues.

### Update
1. Download VinTed-v1.2.1.zip below.
2. Extract to %AppData%\Autodesk\ApplicationPlugins\VinTed\.
3. Restart Autodesk Inventor.
"@

Write-Host ('Creating GitHub Release ' + $tagName + '...') -ForegroundColor Cyan
& $ghExe release create $tagName $zipPath --repo $repo --title 'VinTed v1.2.1' --notes $releaseNotes --verify-tag
if ($LASTEXITCODE -ne 0) {
    throw 'GitHub release creation failed.'
}

Write-Host 'Release created successfully.' -ForegroundColor Green
& $ghExe release view $tagName --repo $repo --json tagName,name,url,assets
