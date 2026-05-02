$ErrorActionPreference = 'Stop'

$version = '2.82.1'
$url = 'https://github.com/cli/cli/releases/download/v' + $version + '/gh_' + $version + '_windows_amd64.zip'
$toolsDir = Join-Path (Get-Location) '.tools'
$zipPath = Join-Path $toolsDir ('gh_' + $version + '_windows_amd64.zip')
$extractDir = Join-Path $toolsDir 'gh'

if (!(Test-Path $toolsDir)) {
    New-Item -ItemType Directory -Path $toolsDir | Out-Null
}

if (Test-Path $extractDir) {
    Remove-Item $extractDir -Recurse -Force
}

Write-Host ('Downloading GitHub CLI ' + $version + '...') -ForegroundColor Cyan
Invoke-WebRequest -Uri $url -OutFile $zipPath

Write-Host 'Extracting...' -ForegroundColor Cyan
Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

$ghExe = Get-ChildItem -Path $extractDir -Filter gh.exe -Recurse | Select-Object -First 1 -ExpandProperty FullName
if ([string]::IsNullOrWhiteSpace($ghExe)) {
    throw 'gh.exe not found after extraction'
}

$ghDir = Split-Path $ghExe -Parent
$envFile = Join-Path $toolsDir 'gh-path.txt'
Set-Content -Path $envFile -Value $ghExe -Encoding UTF8

Write-Host ('GH_EXE=' + $ghExe) -ForegroundColor Green
& $ghExe --version
