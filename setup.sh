#!/bin/bash
# Quick Setup Script dla Linux/Mac - pierwsze uruchomienie

echo "==================================="
echo "  10xCards - Quick Setup"
echo "==================================="
echo ""

echo "[1/4] Sprawdzanie narzêdzi..."
if ! command -v dotnet &> /dev/null; then
    echo "? ERROR: .NET 9 SDK nie jest zainstalowany!"
    echo "Pobierz z: https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

echo "[2/4] Instalacja dotnet-ef..."
dotnet tool install --global dotnet-ef 2>/dev/null || {
    echo "dotnet-ef ju¿ zainstalowane, aktualizujê..."
    dotnet tool update --global dotnet-ef
}

echo "[3/4] Tworzenie migracji bazy danych..."
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards

echo "[4/4] Budowanie projektu..."
dotnet build

echo ""
echo "==================================="
echo "  Setup zakoñczony pomyœlnie!"
echo "==================================="
echo ""
echo "Aby uruchomiæ aplikacjê:"
echo "  dotnet run --project 10xCards"
echo ""
echo "Domyœlny u¿ytkownik testowy:"
echo "  Email: test@10xcards.local"
echo "  Has³o: Test123!"
echo ""
