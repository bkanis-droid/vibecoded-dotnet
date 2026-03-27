@echo off
REM Setup script for running tests - Windows batch version

echo.
echo ========================================
echo Medical Customer Management - Test Setup
echo ========================================
echo.

REM Step 1: Install Playwright browsers if not already installed
echo [1/3] Checking Playwright browsers...
if exist "%USERPROFILE%\AppData\Local\ms-playwright\chromium-*" (
    echo. Playwright browsers already installed
) else (
    echo Installing Playwright browsers...
    call playwright install chromium
    if errorlevel 1 (
        echo Failed to install Playwright browsers
        exit /b 1
    )
)
echo.

REM Step 2: Build the project
echo [2/4] Building project...
call dotnet build "Mijn applicatie.sln"
if errorlevel 1 (
    echo Failed to build project
    exit /b 1
)
echo.

REM Step 3: Start the application in background
echo [3/4] Starting application server...
start "MedicalApp" /B dotnet run
echo Application server started
echo Waiting 3 seconds for server to be ready...
timeout /t 3 /nobreak
echo.

REM Step 4: Run the tests
echo [4/4] Running tests...
echo Running: dotnet test
call dotnet test "Mijn applicatie.sln" --verbosity normal

set TEST_RESULT=%ERRORLEVEL%

REM Clean up: Kill the application process
echo.
echo [Cleanup] Stopping application server...
taskkill /FI "WINDOWTITLE eq MedicalApp" /T /F >nul 2>&1
echo Application server stopped

echo.
echo ========================================
if %TEST_RESULT% equ 0 (
    echo All tests passed!
) else (
    echo Some tests failed or were skipped
)
echo ========================================
echo.

exit /b %TEST_RESULT%
