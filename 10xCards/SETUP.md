# Instrukcje uruchomienia aplikacji 10xCards

## Wymagania
- .NET 9 SDK
- SQLite (wbudowane w .NET)

## ?? Szybkie uruchomienie (zalecane)

### Windows
```bash
setup.bat
```

### Linux/Mac
```bash
chmod +x setup.sh
./setup.sh
```

Skrypt automatycznie:
1. Sprawdzi wymagania
2. Zainstaluje narzêdzie `dotnet-ef`
3. Utworzy migracjê bazy danych
4. Zbuduje projekt

Po zakoñczeniu uruchom aplikacjê:
```bash
dotnet run --project 10xCards
```

---

## ?? Rêczne uruchomienie (krok po kroku)

## Krok 1: Zainstaluj narzêdzie EF Core

```bash
dotnet tool install --global dotnet-ef
```

Jeœli masz ju¿ zainstalowane, zaktualizuj do najnowszej wersji:

```bash
dotnet tool update --global dotnet-ef
```

## Krok 2: Utwórz migracjê pocz¹tkow¹

W g³ównym folderze rozwi¹zania (tam gdzie jest plik .sln) wykonaj:

```bash
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards
```

Ta komenda utworzy folder `Migrations` w projekcie `10xCards.Persistance` z plikami migracji.

## Krok 3: Uruchom aplikacjê

```bash
dotnet run --project 10xCards
```

**Przy pierwszym uruchomieniu** aplikacja automatycznie:
1. Zastosuje wszystkie migracje do bazy danych (SQLite)
2. Utworzy u¿ytkownika testowego (tylko w trybie Development)

Aplikacja bêdzie dostêpna pod adresem wyœwietlonym w konsoli (zwykle `https://localhost:5001`)

## Krok 4: Zaloguj siê

### Opcja A: U¿yj automatycznie utworzonego u¿ytkownika testowego
- Email: `test@10xcards.local`
- Has³o: `Test123!`

### Opcja B: Utwórz w³asne konto
1. W aplikacji przejdŸ do `/Account/Register`
2. Zarejestruj konto z wymaganymi parametrami has³a:
   - Minimum 8 znaków
   - Przynajmniej jedna wielka litera
   - Przynajmniej jedna ma³a litera
   - Przynajmniej jedna cyfra

## Rozwi¹zywanie problemów

### B³¹d: "No such table: AspNetUsers"
Oznacza to, ¿e migracje nie zosta³y zastosowane. Wykonaj:

```bash
# SprawdŸ czy migracja zosta³a utworzona
dotnet ef migrations list --project 10xCards.Persistance --startup-project 10xCards

# Jeœli migracja istnieje, zastosuj j¹ rêcznie
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards

# Jeœli migracja nie istnieje, utwórz j¹
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards
```

### B³¹d: "dotnet-ef: command not found"
Zainstaluj narzêdzie EF Core:

```bash
dotnet tool install --global dotnet-ef
```

### B³¹d 404 na /Account/Login
- Upewnij siê, ¿e aplikacja zosta³a zbudowana pomyœlnie: `dotnet build`
- SprawdŸ czy strony Account s¹ w folderze `10xCards/Components/Pages/Account/`

### B³¹d przy logowaniu
- SprawdŸ czy u¿ytkownik zosta³ utworzony (sprawdŸ logi w konsoli)
- SprawdŸ czy has³o spe³nia wymagania
- Spróbuj zarejestrowaæ nowe konto zamiast u¿ywaæ testowego

### Usuniêcie i ponowne utworzenie bazy danych

Jeœli chcesz zacz¹æ od nowa:

```bash
# Usuñ plik bazy danych (Windows)
del 10xCards\10xCards.db

# Usuñ plik bazy danych (Linux/Mac)
rm 10xCards/10xCards.db

# Zastosuj migracje ponownie (lub po prostu uruchom aplikacjê)
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards
```

## Struktura bazy danych

Baza danych SQLite znajduje siê w:
- Lokalizacja: `10xCards/10xCards.db`
- Mo¿na otworzyæ w narzêdziach takich jak DB Browser for SQLite

## Nastêpne kroki

Po zalogowaniu mo¿esz:
1. Generowaæ fiszki z AI (`/generate`) - *wymaga implementacji serwisów*
2. Zarz¹dzaæ swoimi fiszkami (`/cards`)
3. Przegl¹daæ sesje nauki (`/review`)
4. Tworzyæ fiszki manualnie (`/create`)
