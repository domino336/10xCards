@echo off
REM Quick Setup Script dla Windows - pierwsze uruchomienie

echo ===================================
echo   10xCards - Quick Setup
echo ===================================
echo.

echo [1/4] Sprawdzanie narzedzi...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 9 SDK nie jest zainstalowany!
    echo Pobierz z: https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

echo [2/4] Instalacja dotnet-ef...
dotnet tool install --global dotnet-ef 2>nul
if errorlevel 1 (
    echo dotnet-ef juz zainstalowane, aktualizuje...
    dotnet tool update --global dotnet-ef
)

echo [3/4] Tworzenie migracji bazy danych...
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards

echo [4/4] Budowanie projektu...
dotnet build

echo.
echo ===================================
echo   Setup zakonczony pomyslnie!
echo ===================================
echo.
echo Aby uruchomic aplikacje:
echo   dotnet run --project 10xCards
echo.
echo Domyslny uzytkownik testowy:
echo   Email: test@10xcards.local
echo   Haslo: Test123!
echo.
pause
