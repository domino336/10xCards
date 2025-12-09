# Naprawiono problem z autoryzacj¹ w 10xCards

## ?? Co zosta³o naprawione:

### Problem:
Przy wejœciu na `/generate` aplikacja przekierowywa³a na `/login`, który zwraca³ b³¹d 404.

### Rozwi¹zanie:

1. **Dodano obs³ugê autoryzacji w routerze** (`Routes.razor`)
   - Zamieniono `RouteView` na `AuthorizeRouteView`
   - Dodano komponent `RedirectToLogin` dla nieautoryzowanych u¿ytkowników
   - Dodano obs³ugê `CascadingAuthenticationState` w `App.razor`

2. **Utworzono strony uwierzytelniania:**
   - `/Account/Login` - strona logowania
   - `/Account/Register` - strona rejestracji
   - `/Account/Logout` - automatyczne wylogowanie
   - `/Account/AccessDenied` - strona odmowy dostêpu

3. **Zaktualizowano nawigacjê** (`NavMenu.razor`)
   - Dodano `AuthorizeView` do warunkowego wyœwietlania menu
   - Dla zalogowanych u¿ytkowników: linki do funkcji + wylogowanie
   - Dla niezalogowanych: linki do logowania i rejestracji

4. **Poprawiono œcie¿ki w `Program.cs`:**
   - `LoginPath` zmieniono z `/login` na `/Account/Login`
   - `LogoutPath` zmieniono z `/logout` na `/Account/Logout`
   - `AccessDeniedPath` zmieniono z `/access-denied` na `/Account/AccessDenied`

5. **Dodano automatyczne tworzenie u¿ytkownika testowego** (tylko w Development):
   - Email: `test@10xcards.local`
   - Has³o: `Test123!`

## ?? Jak uruchomiæ aplikacjê:

### ?? WA¯NE: Pierwsze uruchomienie wymagane!

**Przed pierwszym uruchomieniem musisz utworzyæ migracjê bazy danych!**

#### Opcja A: U¿yj skryptu setup (naj³atwiejsze)
```bash
# Windows
setup.bat

# Linux/Mac
chmod +x setup.sh
./setup.sh
```

#### Opcja B: Rêczne kroki
```bash
# 1. Zainstaluj narzêdzie EF Core
dotnet tool install --global dotnet-ef

# 2. Utwórz migracjê
dotnet ef migrations add InitialCreate --project 10xCards.Persistance --startup-project 10xCards

# 3. Uruchom aplikacjê (automatycznie zastosuje migracje i utworzy u¿ytkownika testowego)
dotnet run --project 10xCards
```

### Po uruchomieniu aplikacji:

### Opcja 1: U¿yj automatycznie utworzonego u¿ytkownika testowego
1. Zaloguj siê u¿ywaj¹c:
   - Email: `test@10xcards.local`
   - Has³o: `Test123!`

### Opcja 2: Utwórz w³asne konto
1. PrzejdŸ do `/Account/Register`
2. Zarejestruj nowe konto

## ?? Wymagania dla has³a:
- Minimum 8 znaków
- Przynajmniej jedna wielka litera
- Przynajmniej jedna ma³a litera
- Przynajmniej jedna cyfra

## ? Strukturra utworzonych plików:

```
10xCards/Components/Pages/Account/
??? Login.razor
??? Login.razor.cs
??? Register.razor
??? Register.razor.cs
??? Logout.razor
??? AccessDenied.razor

10xCards/Components/Shared/
??? RedirectToLogin.razor
```

## ?? Zmiany w istniej¹cych plikach:
- `10xCards/Components/Routes.razor` - dodano `AuthorizeRouteView`
- `10xCards/Components/App.razor` - ju¿ mia³o `CascadingAuthenticationState`
- `10xCards/Components/_Imports.razor` - dodano `@using Microsoft.AspNetCore.Identity`
- `10xCards/Components/Layout/NavMenu.razor` - dodano `AuthorizeView`
- `10xCards/Program.cs` - poprawiono œcie¿ki autoryzacji + seed u¿ytkownika

## ?? Dokumentacja:
Zobacz `SETUP.md` dla szczegó³owych instrukcji konfiguracji bazy danych i pierwszego uruchomienia.
