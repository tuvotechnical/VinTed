# VinTed - Deploy Supabase Edge Functions
# Su dung Supabase Management API (khong can Supabase CLI)

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$projectRef = "xjbpsucnldzktkahauix"
$accessToken = "sbp_437bfe8c0f991fa02bb35105f33d3eeda4de36c9"
$baseUrl = "https://api.supabase.com/v1/projects/$projectRef/functions"

$headers = @{
    "Authorization" = "Bearer $accessToken"
    "Content-Type"  = "application/json"
}

function Deploy-Function {
    param(
        [string]$Name,
        [string]$FilePath,
        [bool]$VerifyJwt = $true
    )

    Write-Host ""
    Write-Host "Deploying: $Name ..." -ForegroundColor Yellow

    if (!(Test-Path $FilePath)) {
        Write-Host "  ERROR: File not found: $FilePath" -ForegroundColor Red
        return
    }

    $code = Get-Content $FilePath -Raw -Encoding UTF8

    $bodyObj = @{
        slug       = $Name
        name       = $Name
        body       = $code
        verify_jwt = $VerifyJwt
    }
    $bodyJson = $bodyObj | ConvertTo-Json -Depth 5

    try {
        $response = Invoke-RestMethod -Uri $baseUrl -Method Post -Headers $headers -Body $bodyJson -ErrorAction Stop
        Write-Host "  -> Created: $Name" -ForegroundColor Green
    }
    catch {
        $statusCode = 0
        if ($_.Exception.Response -ne $null) {
            $statusCode = [int]$_.Exception.Response.StatusCode
        }
        if ($statusCode -eq 409) {
            Write-Host "  -> Function exists, updating..." -ForegroundColor Cyan
            try {
                $updateUrl = "$baseUrl/$Name"
                $updateBody = @{
                    body       = $code
                    verify_jwt = $VerifyJwt
                } | ConvertTo-Json -Depth 5
                $response = Invoke-RestMethod -Uri $updateUrl -Method Patch -Headers $headers -Body $updateBody -ErrorAction Stop
                Write-Host "  -> Updated: $Name" -ForegroundColor Green
            }
            catch {
                Write-Host "  -> UPDATE FAILED: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        else {
            Write-Host "  -> DEPLOY FAILED ($statusCode): $($_.Exception.Message)" -ForegroundColor Red
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errBody = $reader.ReadToEnd()
                Write-Host "  -> Detail: $errBody" -ForegroundColor Red
            } catch {}
        }
    }
}

function Set-Secrets {
    Write-Host ""
    Write-Host "Setting Edge Function secrets..." -ForegroundColor Yellow

    $secretsUrl = "https://api.supabase.com/v1/projects/$projectRef/secrets"
    $secrets = @(
        @{ name = "SEPAY_WEBHOOK_KEY"; value = "1YSN5ZMWVUGRVAF1NMLTK2P4ESIXAKPCBYLTD90OKRIIQAZSGYOXNJEECBPRXC9F" },
        @{ name = "SEPAY_ACCOUNT"; value = "0984056744" },
        @{ name = "SEPAY_BANK"; value = "VPBank" }
    )

    $body = $secrets | ConvertTo-Json -Depth 5

    try {
        Invoke-RestMethod -Uri $secretsUrl -Method Post -Headers $headers -Body $body -ErrorAction Stop
        Write-Host "  -> Secrets set successfully!" -ForegroundColor Green
    }
    catch {
        Write-Host "  -> SECRETS FAILED: $($_.Exception.Message)" -ForegroundColor Red
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errBody = $reader.ReadToEnd()
            Write-Host "  -> Detail: $errBody" -ForegroundColor Red
        } catch {}
    }
}

Write-Host ""
Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "  VinTed - Deploy Edge Functions"  -ForegroundColor Cyan
Write-Host "========================================"  -ForegroundColor Cyan

Set-Secrets

$funcDir = ".\supabase\functions"

Deploy-Function -Name "verify-license" -FilePath "$funcDir\verify-license\index.ts" -VerifyJwt $true
Deploy-Function -Name "create-order"   -FilePath "$funcDir\create-order\index.ts"   -VerifyJwt $true
Deploy-Function -Name "order-status"   -FilePath "$funcDir\order-status\index.ts"   -VerifyJwt $true
Deploy-Function -Name "sepay-webhook"  -FilePath "$funcDir\sepay-webhook\index.ts"  -VerifyJwt $false

Write-Host ""
Write-Host "========================================"  -ForegroundColor Green
Write-Host "  DEPLOY HOAN TAT!"  -ForegroundColor Green
Write-Host "========================================"  -ForegroundColor Green
Write-Host ""
