@echo off
echo ========================================
echo 10xCards - Uruchamianie testow E2E
echo ========================================
echo.

REM Zapisz obecny katalog
set ORIGINAL_DIR=%cd%

REM Przejdz do katalogu solucji
cd /d "%~dp0.."

echo [1/4] Budowanie projektu...
dotnet build
if %ERRORLEVEL% NEQ 0 (
    echo Blad budowania projektu!
    pause
    exit /b 1
)
echo.

echo [2/4] Sprawdzanie instalacji Playwright...
if not exist "10xCards.E2E.Tests\bin\Debug\net9.0\.playwright" (
    echo Playwright nie jest zainstalowany. Instalowanie...
    cd 10xCards.E2E.Tests
    call install-playwright.bat
    cd ..
)
echo.

echo [3/4] Uruchamianie aplikacji w tle...
start "10xCards App" /B dotnet run --project 10xCards --no-build
echo Czekam 15 sekund na uruchomienie aplikacji...
timeout /t 15 /nobreak > nul
echo.

echo [4/4] Uruchamianie testow E2E...
echo.
dotnet test 10xCards.E2E.Tests --no-build --verbosity normal

REM Zapisz wynik testow
set TEST_RESULT=%ERRORLEVEL%

echo.
echo ========================================
echo Zatrzymywanie aplikacji...
echo ========================================

REM Zatrzymaj proces dotnet z tytu³em "10xCards App"
taskkill /F /FI "WINDOWTITLE eq 10xCards App*" 2>nul
REM Backup - zatrzymaj wszystkie procesy dotnet (opcjonalne, zakomentowane domyslnie)
REM taskkill /F /IM dotnet.exe 2>nul

REM Powrot do oryginalnego katalogu
cd /d "%ORIGINAL_DIR%"

echo.
if %TEST_RESULT% EQU 0 (
    echo ========================================
    echo Wszystkie testy przeszly pomyslnie!
    echo ========================================
) else (
    echo ========================================
    echo Niektore testy nie przeszly.
    echo ========================================
)

pause
exit /b %TEST_RESULT%
