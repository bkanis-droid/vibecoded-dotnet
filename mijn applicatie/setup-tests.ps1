# Setup script for running tests
# This script installs Playwright browsers and runs the tests

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Medical Customer Management - Test Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Step 1: Install Playwright browsers if not already installed
Write-Host "`n[1/3] Checking Playwright browsers..." -ForegroundColor Yellow
$playwrightDir = "$env:USERPROFILE\.nuget\packages\microsoft.playwright\*\tools"
$browserDir = "$env:USERPROFILE\AppData\Local\ms-playwright\chromium-*"

if (Test-Path $browserDir) {
    Write-Host "✓ Playwright browsers already installed" -ForegroundColor Green
} else {
    Write-Host "Installing Playwright browsers..." -ForegroundColor Yellow
    playwright install chromium
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Playwright browsers installed successfully" -ForegroundColor Green
    } else {
        Write-Host "✗ Failed to install Playwright browsers" -ForegroundColor Red
        exit 1
    }
}

# Step 2: Build the project
Write-Host "`n[2/4] Building project..." -ForegroundColor Yellow
dotnet build "Mijn applicatie.sln"
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Project built successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to build project" -ForegroundColor Red
    exit 1
}

# Step 3: Start the application in background
Write-Host "`n[3/4] Starting application server..." -ForegroundColor Yellow
$appProcess = Start-Process -FilePath "dotnet" -ArgumentList "run" -PassThru -WindowStyle Hidden
Write-Host "✓ Application server started (PID: $($appProcess.Id))" -ForegroundColor Green
Write-Host "Waiting 3 seconds for server to be ready..." -ForegroundColor Gray
Start-Sleep -Seconds 3

# Step 4: Run the tests
Write-Host "`n[4/4] Running tests..." -ForegroundColor Yellow
Write-Host "Running: dotnet test" -ForegroundColor Gray
dotnet test "Mijn applicatie.sln" --verbosity normal

$testResult = $LASTEXITCODE

# Clean up: Stop the application server
Write-Host "`n[Cleanup] Stopping application server..." -ForegroundColor Yellow
try {
    Stop-Process -Id $appProcess.Id -Force -ErrorAction Stop
    Write-Host "✓ Application server stopped" -ForegroundColor Green
} catch {
    Write-Host "⚠ Could not stop application server (may have already exited)" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan
if ($testResult -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "⚠ Some tests failed or were skipped" -ForegroundColor Yellow
}
Write-Host "========================================" -ForegroundColor Cyan

exit $testResult
