# ============================================================
# VinTed Add-in - Build and Deploy Script
# ============================================================

$projectName = "VinTed"
$appDataFolder = [System.IO.Path]::Combine($env:AppData, "Autodesk", "ApplicationPlugins", $projectName)
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  VinTed Add-in - Build and Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- STEP 0: Read version from version.json ---
Write-Host "[0/7] Doc version tu version.json..." -ForegroundColor Yellow
$versionFile = ".\version.json"
if (Test-Path $versionFile) {
    $versionJson = Get-Content $versionFile -Raw | ConvertFrom-Json
    $version = $versionJson.version
    Write-Host "  -> Version: $version" -ForegroundColor Green

    # Inject version vao AssemblyInfo.cs
    $assemblyInfoPath = ".\Properties\AssemblyInfo.cs"
    if (Test-Path $assemblyInfoPath) {
        $assemblyContent = Get-Content $assemblyInfoPath -Raw
        $assemblyContent = $assemblyContent -replace 'AssemblyVersion\("[^"]*"\)', "AssemblyVersion(`"$version.0`")"
        $assemblyContent = $assemblyContent -replace 'AssemblyFileVersion\("[^"]*"\)', "AssemblyFileVersion(`"$version.0`")"
        Set-Content $assemblyInfoPath $assemblyContent -Encoding UTF8
        Write-Host "  -> Da cap nhat AssemblyInfo.cs: $version.0" -ForegroundColor Green
    }
} else {
    Write-Host "  -> Khong tim thay version.json, dung version mac dinh." -ForegroundColor Gray
}

# --- STEP 1: Kill Inventor ---
Write-Host "[1/7] Kiem tra Inventor..." -ForegroundColor Yellow
$invProcess = Get-Process -Name "Inventor" -ErrorAction SilentlyContinue
if ($invProcess) {
    Write-Host "  -> Dang dong Inventor..." -ForegroundColor Red
    Stop-Process -Name "Inventor" -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "  -> Da dong Inventor." -ForegroundColor Green
} else {
    Write-Host "  -> Inventor khong chay." -ForegroundColor Gray
}

# --- STEP 2: Clean Build ---
Write-Host "[2/7] Don dep thu muc Build..." -ForegroundColor Yellow
$buildDir = ".\bin\Release"
if (Test-Path $buildDir) {
    Remove-Item -Path $buildDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  -> Da xoa bin\Release" -ForegroundColor Green
}

# --- STEP 3: MSBuild ---
Write-Host "[3/7] Dang build VinTed.csproj..." -ForegroundColor Yellow
$buildArgs = @("VinTed.csproj", "/t:Rebuild", "/p:Configuration=Release", "/verbosity:minimal")
& $msbuild $buildArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "*** BUILD THAT BAI! ***" -ForegroundColor Red
    exit 1
}
Write-Host "  -> Build thanh cong!" -ForegroundColor Green

# --- STEP 4: Remove System DLLs ---
Write-Host "[4/7] Don dep DLL he thong..." -ForegroundColor Yellow
$dangerousPatterns = @("mscorlib.dll", "System.dll", "System.Core.dll")
foreach ($pattern in $dangerousPatterns) {
    $filePath = [System.IO.Path]::Combine($buildDir, $pattern)
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
        Write-Host "  -> Da xoa: $pattern" -ForegroundColor Red
    }
}
Get-ChildItem -Path $buildDir -Filter "*.nlp" -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "  -> Da xoa: $($_.Name)" -ForegroundColor Red
}
Write-Host "  -> Hoan tat." -ForegroundColor Green

# Copy ModernWpf DLLs vao build output
Write-Host "  -> Copy ModernWpf DLLs..." -ForegroundColor Yellow
Copy-Item ".\packages\ModernWpfUI\lib\net45\ModernWpf.dll" -Destination $buildDir -Force -ErrorAction SilentlyContinue
Copy-Item ".\packages\ModernWpfUI\lib\net45\ModernWpf.Controls.dll" -Destination $buildDir -Force -ErrorAction SilentlyContinue
Write-Host "  -> Da copy ModernWpf." -ForegroundColor Green

# --- STEP 5: Remove localization folders (không cần) ---
Write-Host "[5/7] Don dep thu muc ngon ngu..." -ForegroundColor Yellow
$localeDirs = Get-ChildItem -Path $buildDir -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match "^[a-z]{2}-[A-Z]{2}$|^[a-z]{2}-[A-Za-z]+-[A-Z]{2}$" }
foreach ($dir in $localeDirs) {
    Remove-Item $dir.FullName -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  -> Da xoa: $($dir.Name)/" -ForegroundColor Red
}
Write-Host "  -> Hoan tat." -ForegroundColor Green

# --- STEP 6: Create .addin manifest ---
Write-Host "[6/7] Tao file manifest .addin..." -ForegroundColor Yellow
$addinXml = '<?xml version="1.0" encoding="utf-8"?>'
$addinXml += "`r`n" + '<Addin Type="Standard">'
$addinXml += "`r`n" + '  <ClassId>{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}</ClassId>'
$addinXml += "`r`n" + '  <ClientId>{D4E5F6A7-B8C9-0D1E-2F3A-4B5C6D7E8F90}</ClientId>'
$addinXml += "`r`n" + '  <DisplayName>VinTed</DisplayName>'
$addinXml += "`r`n" + '  <Description>VinTed Add-in for Autodesk Inventor</Description>'
$addinXml += "`r`n" + "  <Assembly>$appDataFolder\VinTed.dll</Assembly>"
$addinXml += "`r`n" + '  <AddinType>Standard</AddinType>'
$addinXml += "`r`n" + '  <LoadOnStartUp>1</LoadOnStartUp>'
$addinXml += "`r`n" + '  <UserUnloadable>1</UserUnloadable>'
$addinXml += "`r`n" + '  <Hidden>0</Hidden>'
$addinXml += "`r`n" + '  <SupportedSoftwareVersionGreaterThan>16..</SupportedSoftwareVersionGreaterThan>'
$addinXml += "`r`n" + '</Addin>'
[System.IO.File]::WriteAllText("VinTed.addin", $addinXml, [System.Text.Encoding]::UTF8)
Write-Host "  -> Da tao VinTed.addin" -ForegroundColor Green

# --- STEP 7: Deploy ---
Write-Host "[7/7] Deploy vao AppData..." -ForegroundColor Yellow
if (!(Test-Path $appDataFolder)) {
    New-Item -ItemType Directory -Path $appDataFolder -Force | Out-Null
}
Copy-Item "$buildDir\*.*" -Destination $appDataFolder -Force -ErrorAction SilentlyContinue
Copy-Item "VinTed.addin" -Destination $appDataFolder -Force

Write-Host "  -> Da deploy vao: $appDataFolder" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  HOAN TAT! Khoi dong lai Inventor." -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
