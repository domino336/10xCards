# ? Czêste problemy i rozwi¹zania

## Problem: `SQLite Error 1: 'no such table: AspNetUsers'`

### Przyczyna:
Baza danych nie zosta³a utworzona lub migracje nie zosta³y zastosowane.

### Rozwi¹zanie:

#### Krok 1: SprawdŸ czy migracja istnieje
```bash
dotnet ef migrations list --project 10xCards.Persistance --startup-project 10xCards
```

#### Krok 2A: Jeœli NIE MA migracji - utwórz j¹
```bash
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards
```

#### Krok 2B: Jeœli migracja ISTNIEJE - zastosuj j¹
```bash
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards
```

#### Krok 3: Uruchom aplikacjê
```bash
dotnet run --project 10xCards
```

### Szybkie rozwi¹zanie (reset wszystkiego):
```bash
# Windows
reset-db.bat

# Linux/Mac
chmod +x reset-db.sh
./reset-db.sh
```

---

## Problem: `dotnet-ef: command not found`

### Rozwi¹zanie:
```bash
dotnet tool install --global dotnet-ef
```

Jeœli nadal nie dzia³a, dodaj œcie¿kê do PATH:
```bash
# Windows (PowerShell)
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# Linux/Mac
export PATH="$PATH:$HOME/.dotnet/tools"
```

---

## Problem: B³¹d 404 na `/Account/Login`

### Przyczyna:
Aplikacja nie zosta³a zbudowana poprawnie lub routing nie dzia³a.

### Rozwi¹zanie:
```bash
# Wyczyœæ i przebuduj
dotnet clean
dotnet build
dotnet run --project 10xCards
```

---

## Problem: Nie mogê siê zalogowaæ

### SprawdŸ:
1. Czy u¿ytkownik zosta³ utworzony? (sprawdŸ logi w konsoli przy starcie)
2. Czy has³o spe³nia wymagania?
   - Min. 8 znaków
   - Wielka litera
   - Ma³a litera
   - Cyfra

### Rozwi¹zanie:
Zarejestruj nowe konto zamiast u¿ywaæ testowego:
- PrzejdŸ do `/Account/Register`
- U¿yj silnego has³a, np. `MyPassword123!`

---

## Problem: Aplikacja siê nie uruchamia

### SprawdŸ wersjê .NET:
```bash
dotnet --version
```

Wymagana: **9.0.x**

Jeœli masz starsz¹ wersjê, pobierz .NET 9 SDK z:
https://dotnet.microsoft.com/download/dotnet/9.0

---

## Problem: Chcê zacz¹æ od nowa (reset bazy danych)

### Windows:
```bash
del /f /q 10xCards\10xCards.db
del /f /q 10xCards\10xCards.db-shm
del /f /q 10xCards\10xCards.db-wal
dotnet run --project 10xCards
```

### Linux/Mac:
```bash
rm -f 10xCards/10xCards.db*
dotnet run --project 10xCards
```

---

## Problem: Migracja ju¿ istnieje, nie mogê utworzyæ nowej

### Rozwi¹zanie 1: Usuñ stare migracje i utwórz now¹
```bash
# Usuñ folder Migrations
rm -rf 10xCards.Persistance/Migrations  # Linux/Mac
rmdir /s 10xCards.Persistance\Migrations  # Windows

# Utwórz now¹ migracjê
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards
```

### Rozwi¹zanie 2: Po prostu zastosuj istniej¹c¹ migracjê
```bash
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards
```

---

## Nadal masz problemy?

1. SprawdŸ logi w konsoli podczas uruchamiania
2. Upewnij siê, ¿e wszystkie projekty siê buduj¹: `dotnet build`
3. SprawdŸ czy plik `10xCards/10xCards.db` istnieje po uruchomieniu
4. Zobacz `SETUP.md` dla pe³nej instrukcji
