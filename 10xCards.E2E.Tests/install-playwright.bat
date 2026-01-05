@echo off
echo Installing Playwright browsers...
echo.

cd /d "%~dp0"

REM Build the project first if not already built
if not exist "bin\Debug\net9.0\playwright.ps1" (
    echo Building E2E test project...
    dotnet build
    echo.
)

REM Install Playwright browsers
if exist "bin\Debug\net9.0\playwright.ps1" (
    echo Running Playwright install script...
    pwsh bin\Debug\net9.0\playwright.ps1 install
    echo.
    echo Playwright browsers installed successfully!
) else (
    echo Error: Playwright install script not found.
    echo Please build the project first: dotnet build
    exit /b 1
)

echo.
echo Done! You can now run E2E tests.
pause
