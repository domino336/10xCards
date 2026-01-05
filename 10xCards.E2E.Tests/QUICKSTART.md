# Szybki start - Testy E2E

## 1. Instalacja Playwright

Po pierwszym buildzie projektu:

```bash
# Windows
cd 10xCards.E2E.Tests
.\install-playwright.bat
```

Lub rêcznie:
```bash
pwsh 10xCards.E2E.Tests\bin\Debug\net9.0\playwright.ps1 install
```

## 2. Uruchomienie aplikacji

W osobnym terminalu:
```bash
dotnet run --project 10xCards
```

Poczekaj a¿ aplikacja wystartuje na `http://localhost:5077`

## 3. Uruchomienie testów

```bash
dotnet test 10xCards.E2E.Tests
```

## Gotowe testy logowania

? Login z kontem testowym
? Login z kontem administratora  
? Login z b³êdnym has³em (weryfikacja b³êdu)
? Login z parametrem returnUrl
? Login z opcj¹ "Remember me"
? Przekierowanie niezalogowanych u¿ytkowników
? Komunikat sukcesu po rejestracji

**7 testów E2E gotowych do uruchomienia!**

## Szczegó³y

Zobacz pe³n¹ dokumentacjê w `10xCards.E2E.Tests/README.md`
