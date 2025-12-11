# Health Endpoint Test Script
# This script tests the /health endpoint of the SampleCkWebApp

Write-Host "Testing Health Endpoint..." -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"
$healthEndpoint = "$baseUrl/health"

# Try common ports if 5000 doesn't work
$ports = @(5000, 5001, 8080, 7000, 7001)

$success = $false

foreach ($port in $ports) {
    $url = "http://localhost:$port/health"
    try {
        Write-Host "`nTrying $url..." -ForegroundColor Yellow
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3
        Write-Host "✓ Success!" -ForegroundColor Green
        Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
        Write-Host "`nResponse Body:" -ForegroundColor Cyan
        $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10
        $success = $true
        break
    }
    catch {
        Write-Host "✗ Failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

if (-not $success) {
    Write-Host "`n⚠ Could not connect to health endpoint." -ForegroundColor Yellow
    Write-Host "Make sure the application is running:" -ForegroundColor Yellow
    Write-Host "  cd SampleCkWebApp\src\SampleCkWebApp.WebApi" -ForegroundColor White
    Write-Host "  dotnet run" -ForegroundColor White
}

