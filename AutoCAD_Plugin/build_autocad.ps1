# File: build_autocad.ps1
$ErrorActionPreference = "Stop"

Write-Host "========================================"
Write-Host "  VinTed AutoCAD Plugin - Build"
Write-Host "========================================"

# Tìm thư mục AutoCAD 2024
$autocadPath = "C:\Program Files\Autodesk\AutoCAD 2024"
if (-not (Test-Path "$autocadPath\accoremgd.dll")) {
    Write-Host "ERROR: Khong tim thay AutoCAD 2024 tai $autocadPath" -ForegroundColor Red
    exit 1
}

Write-Host "[1/2] Dang build VinTed.AutoCAD.csproj..."
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

& $msbuild ".\VinTed.AutoCAD.csproj" /t:Rebuild /p:Configuration=Release "/p:AutoCADPath=$autocadPath"

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build that bai!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "[2/2] Build thanh cong!" -ForegroundColor Green
Write-Host "Output: AutoCAD_Plugin\bin\Release\VinTed.AutoCAD.dll"

Write-Host "========================================"
Write-Host "  HOAN TAT!"
Write-Host "========================================"
