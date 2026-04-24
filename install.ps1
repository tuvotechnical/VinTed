# ============================================================
#  VinTed - One-Command Installer
# ============================================================
#  Cai dat:  powershell -c "irm https://raw.githubusercontent.com/tuvotechnical/VinTed/main/install.ps1 | iex"
# ============================================================

$ErrorActionPreference = "Stop"

# --- Cau hinh ---
$repoOwner = "tuvotechnical"
$repoName = "VinTed"
$installPath = "$env:AppData\Autodesk\ApplicationPlugins\VinTed"
$apiUrl = "https://api.github.com/repos/$repoOwner/$repoName/releases/latest"
$tempZip = "$env:TEMP\VinTed_install.zip"

# --- Banner ---
Write-Host ""
Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "  ║                                      ║" -ForegroundColor Cyan
Write-Host "  ║        VinTed — Installer             ║" -ForegroundColor Cyan
Write-Host "  ║   Autodesk Inventor Add-in            ║" -ForegroundColor Cyan
Write-Host "  ║                                      ║" -ForegroundColor Cyan
Write-Host "  ╚══════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

try {
    # --- STEP 1: Lay thong tin release moi nhat ---
    Write-Host "  [1/5] Kiem tra phien ban moi nhat..." -ForegroundColor Yellow

    $headers = @{ "User-Agent" = "VinTed-Installer" }
    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers

    $version = $release.tag_name
    $releaseName = $release.name
    $asset = $release.assets | Where-Object { $_.name -like "*.zip" } | Select-Object -First 1

    if (-not $asset) {
        Write-Host "  !! Khong tim thay file cai dat trong release." -ForegroundColor Red
        Write-Host "  -> Truy cap: https://github.com/$repoOwner/$repoName/releases" -ForegroundColor Gray
        return
    }

    $downloadUrl = $asset.browser_download_url
    $fileName = $asset.name
    $fileSize = [math]::Round($asset.size / 1KB, 1)

    Write-Host "  -> Phien ban: $releaseName ($version)" -ForegroundColor Green
    Write-Host "  -> File:      $fileName ($fileSize KB)" -ForegroundColor Gray

    # --- STEP 2: Dong Inventor (neu dang chay) ---
    Write-Host "  [2/5] Kiem tra Inventor..." -ForegroundColor Yellow
    $invProcess = Get-Process -Name "Inventor" -ErrorAction SilentlyContinue
    if ($invProcess) {
        Write-Host "  -> Inventor dang chay. Dong Inventor de cai dat..." -ForegroundColor Red
        Write-Host ""
        $confirm = Read-Host "     Nhap 'Y' de dong Inventor va tiep tuc, hoac 'N' de huy"
        if ($confirm -ne 'Y' -and $confirm -ne 'y') {
            Write-Host "  -> Da huy cai dat." -ForegroundColor Gray
            return
        }
        Stop-Process -Name "Inventor" -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-Host "  -> Da dong Inventor." -ForegroundColor Green
    } else {
        Write-Host "  -> OK (Inventor khong chay)." -ForegroundColor Gray
    }

    # --- STEP 3: Tai file ---
    Write-Host "  [3/5] Dang tai $fileName..." -ForegroundColor Yellow

    if (Test-Path $tempZip) { Remove-Item $tempZip -Force }

    # Dung BitsTransfer neu co, fallback sang WebClient
    try {
        Import-Module BitsTransfer -ErrorAction Stop
        Start-BitsTransfer -Source $downloadUrl -Destination $tempZip -DisplayName "VinTed"
    }
    catch {
        $wc = New-Object System.Net.WebClient
        $wc.Headers.Add("User-Agent", "VinTed-Installer")
        $wc.DownloadFile($downloadUrl, $tempZip)
    }

    if (!(Test-Path $tempZip)) {
        Write-Host "  !! Tai file that bai!" -ForegroundColor Red
        return
    }
    Write-Host "  -> Tai thanh cong." -ForegroundColor Green

    # --- STEP 4: Giai nen va cai dat ---
    Write-Host "  [4/5] Cai dat vao Inventor..." -ForegroundColor Yellow

    # Tao thu muc neu chua co
    if (!(Test-Path $installPath)) {
        New-Item -ItemType Directory -Path $installPath -Force | Out-Null
    }

    # Giai nen (ghi de file cu)
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($tempZip)
    foreach ($entry in $zip.Entries) {
        if ([string]::IsNullOrEmpty($entry.Name)) { continue }  # Bo qua thu muc
        $destPath = [System.IO.Path]::Combine($installPath, $entry.Name)
        $destDir = [System.IO.Path]::GetDirectoryName($destPath)
        if (!(Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destPath, $true)
    }
    $zip.Dispose()

    # Xoa file tam
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue

    Write-Host "  -> Da cai dat vao: $installPath" -ForegroundColor Green

    # --- STEP 5: Unblock files ---
    Write-Host "  [5/5] Mo khoa file (Unblock)..." -ForegroundColor Yellow
    Get-ChildItem -Path $installPath -Recurse | Unblock-File -ErrorAction SilentlyContinue
    Write-Host "  -> Hoan tat." -ForegroundColor Green

    # --- HOAN TAT ---
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "  ║                                      ║" -ForegroundColor Green
    Write-Host "  ║   CAI DAT THANH CONG!                ║" -ForegroundColor Green
    Write-Host "  ║                                      ║" -ForegroundColor Green
    Write-Host "  ║   Khoi dong Inventor de su dung.     ║" -ForegroundColor Green
    Write-Host "  ║   Tab 'VinTed' se xuat hien          ║" -ForegroundColor Green
    Write-Host "  ║   trong Ribbon khi mo ban ve.        ║" -ForegroundColor Green
    Write-Host "  ║                                      ║" -ForegroundColor Green
    Write-Host "  ╚══════════════════════════════════════╗" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Phien ban: $version" -ForegroundColor White
    Write-Host "  Thu muc:   $installPath" -ForegroundColor Gray
    Write-Host "  GitHub:    https://github.com/$repoOwner/$repoName" -ForegroundColor Cyan
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "  !! LOI: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Thu cai dat thu cong:" -ForegroundColor Yellow
    Write-Host "  1. Tai file ZIP tu: https://github.com/$repoOwner/$repoName/releases/latest" -ForegroundColor White
    Write-Host "  2. Giai nen vao: $installPath" -ForegroundColor White
    Write-Host "  3. Khoi dong lai Inventor." -ForegroundColor White
    Write-Host ""

    # Don dep file tam
    if (Test-Path $tempZip) { Remove-Item $tempZip -Force -ErrorAction SilentlyContinue }
}
