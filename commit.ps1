# ============================================================
# VinTed Add-in - Commit, Build & Release Pipeline
# ============================================================
# Usage:
#   .\commit.ps1 -Message "Mo ta commit" -BumpType patch
#   .\commit.ps1 -Message "Tinh nang moi" -BumpType minor
#   .\commit.ps1 -Message "Version lon" -BumpType major
# ============================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$Message = "",

    [Parameter(Mandatory=$false)]
    [ValidateSet("patch", "minor", "major")]
    [string]$BumpType = "patch"
)

# --- Cau hinh ---
$projectName = "VinTed"
$repoOwner = "tuvotechnical"
$repoName = "VinTed"
$versionFile = ".\version.json"
$buildScript = ".\build.ps1"
$buildDir = ".\bin\Release"

Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host "  VinTed - Commit, Build and Release" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""

# ============================================================
# STEP 1: Doc version hien tai
# ============================================================
Write-Host "[1/8] Doc version hien tai..." -ForegroundColor Yellow
if (!(Test-Path $versionFile)) {
    Write-Host "  Tao version.json voi version 1.0.0" -ForegroundColor Gray
    @{ version = "1.0.0" } | ConvertTo-Json | Set-Content $versionFile -Encoding UTF8
}
$versionJson = Get-Content $versionFile -Raw | ConvertFrom-Json
$currentVersion = $versionJson.version
Write-Host "  Version hien tai: $currentVersion" -ForegroundColor Cyan

# ============================================================
# STEP 2: Bump version
# ============================================================
Write-Host "[2/8] Bump version ($BumpType)..." -ForegroundColor Yellow
$parts = $currentVersion.Split(".")
$major = [int]$parts[0]
$minor = [int]$parts[1]
$patch = [int]$parts[2]

switch ($BumpType) {
    "major" {
        $major++
        $minor = 0
        $patch = 0
    }
    "minor" {
        $minor++
        $patch = 0
    }
    "patch" {
        $patch++
    }
}

$newVersion = "$major.$minor.$patch"
Write-Host "  Version moi: $currentVersion --> $newVersion" -ForegroundColor Green

# ============================================================
# STEP 3: Ghi version moi
# ============================================================
Write-Host "[3/8] Ghi version moi vao version.json..." -ForegroundColor Yellow
@{ version = $newVersion } | ConvertTo-Json | Set-Content $versionFile -Encoding UTF8
Write-Host "  Da cap nhat version.json" -ForegroundColor Green

# ============================================================
# STEP 4: Build (goi build.ps1)
# ============================================================
Write-Host "[4/8] Bat dau build..." -ForegroundColor Yellow
Write-Host ""
& $buildScript
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "*** BUILD THAT BAI - Dung pipeline. ***" -ForegroundColor Red
    # Rollback version
    @{ version = $currentVersion } | ConvertTo-Json | Set-Content $versionFile -Encoding UTF8
    exit 1
}
Write-Host ""

# ============================================================
# STEP 5: Tao ZIP release asset
# ============================================================
Write-Host "[5/8] Tao ZIP release..." -ForegroundColor Yellow
$zipName = "VinTed-v$newVersion.zip"
$zipPath = ".\$zipName"

# Xoa zip cu neu co
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Danh sach file can nen
$filesToZip = @(
    "$buildDir\VinTed.dll",
    "$buildDir\ModernWpf.dll",
    "$buildDir\ModernWpf.Controls.dll",
    ".\VinTed.addin"
)

# Tao thu muc tam de chua cac file
$tempZipDir = ".\__release_temp__"
if (Test-Path $tempZipDir) { Remove-Item $tempZipDir -Recurse -Force }
New-Item -ItemType Directory -Path $tempZipDir -Force | Out-Null

foreach ($f in $filesToZip) {
    if (Test-Path $f) {
        Copy-Item $f -Destination $tempZipDir -Force
    } else {
        Write-Host "  WARN: Khong tim thay: $f" -ForegroundColor Red
    }
}

# Tao file INSTALL.txt huong dan cai dat
$installLines = @(
    "==============================================",
    "  VinTed v$newVersion - Huong dan cai dat",
    "==============================================",
    "",
    "1. Giai nen tat ca file vao thu muc:",
    "   %AppData%\Autodesk\ApplicationPlugins\VinTed\",
    "",
    "2. Khoi dong lai Autodesk Inventor.",
    "",
    "3. Tab VinTed se xuat hien trong Ribbon khi mo ban ve Drawing.",
    "",
    "Lien he: https://github.com/$repoOwner/$repoName/issues"
)
$installLines | Set-Content "$tempZipDir\INSTALL.txt" -Encoding UTF8

# Nen thanh ZIP
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    (Resolve-Path $tempZipDir).Path,
    (Join-Path (Get-Location) $zipName),
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false
)

# Xoa thu muc tam
Remove-Item $tempZipDir -Recurse -Force

if (Test-Path $zipPath) {
    $zipSizeKB = [math]::Round((Get-Item $zipPath).Length / 1KB, 1)
    Write-Host "  Da tao: $zipName ($zipSizeKB KB)" -ForegroundColor Green
} else {
    Write-Host "  Khong tao duoc ZIP" -ForegroundColor Red
    exit 1
}

# ============================================================
# STEP 6: Git commit
# ============================================================
Write-Host "[6/8] Git commit..." -ForegroundColor Yellow
if ([string]::IsNullOrWhiteSpace($Message)) {
    $Message = "Release v$newVersion"
}
$commitMsg = "v$newVersion - $Message"

git add .
if ($LASTEXITCODE -ne 0) {
    Write-Host "  git add that bai" -ForegroundColor Red
    exit 1
}

git commit -m $commitMsg
if ($LASTEXITCODE -ne 0) {
    Write-Host "  git commit that bai (co the khong co thay doi)." -ForegroundColor Gray
}
Write-Host "  Commit: $commitMsg" -ForegroundColor Green

# ============================================================
# STEP 7: Git push
# ============================================================
Write-Host "[7/8] Git push..." -ForegroundColor Yellow
git push origin HEAD
if ($LASTEXITCODE -ne 0) {
    Write-Host "  git push that bai" -ForegroundColor Red
    Write-Host "  Thu git push thu cong sau." -ForegroundColor Gray
}
Write-Host "  Da push len GitHub." -ForegroundColor Green

# ============================================================
# STEP 8: Tao GitHub Release
# ============================================================
Write-Host "[8/8] Tao GitHub Release..." -ForegroundColor Yellow

# Kiem tra gh CLI
$ghPath = Get-Command "gh" -ErrorAction SilentlyContinue
if ($ghPath) {
    # Dung gh CLI (cach tot nhat)
    $tagName = "v$newVersion"
    $releaseTitle = "VinTed v$newVersion"
    $releaseNotes = "## VinTed v$newVersion`n`n$Message`n`n### Huong dan cap nhat:`n1. Tai file VinTed-v$newVersion.zip ben duoi.`n2. Giai nen vao %AppData%\Autodesk\ApplicationPlugins\VinTed\`n3. Khoi dong lai Inventor."

    gh release create $tagName $zipPath --title $releaseTitle --notes $releaseNotes
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  Da tao Release: $tagName" -ForegroundColor Green
    } else {
        Write-Host "  Tao release that bai qua gh CLI." -ForegroundColor Red
        Write-Host "  Thu tao release thu cong tren GitHub." -ForegroundColor Gray
    }
} else {
    # Fallback: dung GitHub REST API voi GITHUB_TOKEN
    Write-Host "  gh CLI khong co, su dung GitHub API..." -ForegroundColor Gray

    # Kiem tra GITHUB_TOKEN
    $token = $env:GITHUB_TOKEN
    if ([string]::IsNullOrWhiteSpace($token)) {
        Write-Host ""
        Write-Host "  ============================================" -ForegroundColor Yellow
        Write-Host "  KHONG TIM THAY GITHUB_TOKEN" -ForegroundColor Yellow
        Write-Host "  ============================================" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  De tu dong tao Release, ban can 1 trong 2 cach:" -ForegroundColor White
        Write-Host ""
        Write-Host "  Cach 1: Cai gh CLI (khuyen nghi)" -ForegroundColor Cyan
        Write-Host "    Tai tu: https://cli.github.com/" -ForegroundColor Gray
        Write-Host "    Sau do chay: gh auth login" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  Cach 2: Tao Personal Access Token" -ForegroundColor Cyan
        Write-Host "    GitHub - Settings - Developer settings - Personal access tokens" -ForegroundColor Gray
        Write-Host "    Tao token voi quyen repo" -ForegroundColor Gray
        Write-Host "    Set bien moi truong: GITHUB_TOKEN = ghp_xxxxx" -ForegroundColor Gray
        Write-Host ""
        Write-Host "  File ZIP da duoc tao tai: $zipPath" -ForegroundColor Green
        Write-Host "  Ban co the upload thu cong len:" -ForegroundColor White
        Write-Host "  https://github.com/$repoOwner/$repoName/releases/new" -ForegroundColor Cyan
        Write-Host ""
    } else {
        # Tao release qua GitHub REST API
        $tagName = "v$newVersion"
        $releaseBody = "## VinTed v$newVersion - $Message"

        $createReleaseJson = @{
            tag_name = $tagName
            name = "VinTed v$newVersion"
            body = $releaseBody
            draft = $false
            prerelease = $false
        } | ConvertTo-Json -Compress

        try {
            $apiHeaders = @{
                "Authorization" = "Bearer $token"
                "Accept" = "application/vnd.github.v3+json"
                "User-Agent" = "VinTed-Release-Script"
            }

            # Tao release
            $releaseResponse = Invoke-RestMethod `
                -Uri "https://api.github.com/repos/$repoOwner/$repoName/releases" `
                -Method Post `
                -Headers $apiHeaders `
                -Body $createReleaseJson `
                -ContentType "application/json"

            $releaseId = $releaseResponse.id
            $uploadUrl = $releaseResponse.upload_url -replace '\{.*\}', ''
            Write-Host "  Da tao Release: $tagName (ID: $releaseId)" -ForegroundColor Green

            # Upload ZIP asset
            Write-Host "  Dang upload $zipName..." -ForegroundColor Yellow
            $uploadHeaders = @{
                "Authorization" = "Bearer $token"
                "Accept" = "application/vnd.github.v3+json"
                "User-Agent" = "VinTed-Release-Script"
                "Content-Type" = "application/zip"
            }

            $zipBytes = [System.IO.File]::ReadAllBytes((Resolve-Path $zipPath).Path)
            $uploadUri = "$($uploadUrl)?name=$zipName"

            Invoke-RestMethod `
                -Uri $uploadUri `
                -Method Post `
                -Headers $uploadHeaders `
                -Body $zipBytes

            Write-Host "  Da upload: $zipName" -ForegroundColor Green
        }
        catch {
            Write-Host ("  Loi khi tao release: " + $_.Exception.Message) -ForegroundColor Red
            Write-Host "  File ZIP van con tai: $zipPath" -ForegroundColor Gray
            Write-Host "  Upload thu cong tai: https://github.com/$repoOwner/$repoName/releases/new" -ForegroundColor Cyan
        }
    }
}

# ============================================================
# HOAN TAT
# ============================================================
Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "  HOAN TAT PIPELINE" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "  Version:  $newVersion" -ForegroundColor White
Write-Host "  Tag:      v$newVersion" -ForegroundColor White
Write-Host "  Commit:   $commitMsg" -ForegroundColor White
Write-Host "  Release:  https://github.com/$repoOwner/$repoName/releases/tag/v$newVersion" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
