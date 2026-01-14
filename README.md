# 10xCards

[![codecov](https://codecov.io/gh/domino336/10xCards/branch/main/graph/badge.svg)](https://codecov.io/gh/domino336/10xCards)
[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)

## ?? O projekcie

10xCards to aplikacja webowa do szybkiego tworzenia i nauki z wykorzystaniem fiszek (Q&A) wspieranych algorytmem powtórek roz³o¿onych w czasie (spaced repetition). Rozwi¹zuje problem czasoch³onnego przygotowywania wysokiej jakoœci materia³ów do nauki poprzez automatyczne generowanie propozycji fiszek z wklejonych notatek u¿ytkownika.

**G³ówna wartoœæ:** Oszczêdnoœæ czasu i zwiêkszona adopcja metody aktywnej nauki.

**Docelowi u¿ytkownicy:**
- Studenci
- Profesjonaliœci ucz¹cy siê do certyfikacji
- Osoby przygotowuj¹ce siê do egzaminów

## ? Kluczowe funkcjonalnoœci MVP

- **Generowanie fiszek AI** – wklej notatki, otrzymaj propozycje fiszek Q&A
- **Edycja i akceptacja** – przegl¹daj, edytuj i akceptuj/odrzucaj propozycje przed zapisaniem
- **Manualne tworzenie** – dodawaj w³asne fiszki bezpoœrednio
- **Spaced Repetition** – algorytm powtórek optymalizuj¹cy harmonogram nauki
- **Sesje powtórek** – przegl¹d fiszek z ocen¹ (Again/Good/Easy) i automatyczn¹ aktualizacj¹ harmonogramu
- **Kolekcje** – organizacja fiszek w zestawy z mo¿liwoœci¹ wersjonowania
- **System kont** – rejestracja, logowanie, izolacja danych u¿ytkowników
- **Panel admina** – metryki adopcji, wspó³czynnik akceptacji AI, aktywni u¿ytkownicy

## ?? Technologie

- **Backend:** .NET 9, ASP.NET Core, Blazor Server
- **Frontend:** Razor Components, Bootstrap 5
- **Baza danych:** SQLite (EF Core) z mo¿liwoœci¹ migracji do PostgreSQL
- **Uwierzytelnianie:** ASP.NET Core Identity
- **AI:** Planowana integracja z OpenRouter
- **Testy:** xUnit, Playwright (E2E), bUnit (komponenty)
- **CI/CD:** GitHub Actions

## ?? Wymagania

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Opcjonalnie) Visual Studio 2022 lub Visual Studio Code
- (Opcjonalnie dla testów E2E) Playwright browsers

## ?? Szybki start

### 1. Sklonuj repozytorium

```bash
git clone https://github.com/domino336/10xCards.git
cd 10xCards
```

### 2. Zbuduj projekt

```bash
dotnet build
```

### 3. Uruchom migracje bazy danych

```bash
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards
```

### 4. Uruchom aplikacjê

```bash
dotnet run --project 10xCards
```

Aplikacja bêdzie dostêpna pod adresem: `https://localhost:5001` (lub `http://localhost:5000`)

### 5. (Opcjonalnie) Domyœlne konta testowe

Po uruchomieniu aplikacji mo¿esz zarejestrowaæ nowe konto lub u¿yæ domyœlnych kont testowych (jeœli zosta³y skonfigurowane):

- **U¿ytkownik testowy:**
  - Email: `test@10xcards.lokal`
  - Has³o: `Test123!`

- **Administrator:**
  - Email: `admin@10xcards.lokal`
  - Has³o: `Admin123!`

**Uwaga:** Domyœlne konta s¹ tworzone automatycznie przy pierwszym uruchomieniu aplikacji w œrodowisku deweloperskim.

## ?? Struktura projektu

Projekt zorganizowany jest zgodnie z architektur¹ warstwow¹:

```
10xCards/
??? 10xCards.Domain/              # Encje, value objects, enums, logika domenowa
??? 10xCards.Application/         # Serwisy aplikacyjne, interfejsy, DTOs
??? 10xCards.Persistance/         # EF Core DbContext, konfiguracje, migracje
??? 10xCards/                     # Blazor Server host, komponenty Razor, Program.cs
??? 10xCards.Unit.Tests/          # Testy jednostkowe (domain/application)
??? 10xCards.Integration.Tests/  # Testy integracyjne (z DbContext)
??? 10xCards.E2E.Tests/           # Testy End-to-End (Playwright)
??? .ai/                          # Dokumentacja techniczna projektu
```

### Warstwy aplikacji

- **Domain** – Czysta logika biznesowa, encje, walidacja w konstruktorach
- **Application** – Use-cases, serwisy (ICardService, IProposalService), DTOs
- **Persistence** – Dostêp do danych, EF Core, migracje SQLite
- **Web (10xCards)** – Blazor Server, komponenty Razor, DI, routing

## ?? Testy

Projekt posiada kompleksowy zestaw testów automatycznych:

### Testy jednostkowe (Unit Tests)

Testowanie logiki biznesowej i serwisów aplikacyjnych bez zale¿noœci zewnêtrznych.

```bash
dotnet test 10xCards.Unit.Tests
```

**Zakres:**
- Serwisy aplikacyjne (CardService, ProposalService, CollectionService, AdminService, SrService)
- Logika domenowa
- Algorytm Spaced Repetition
- Walidacje i regu³y biznesowe

### Testy integracyjne (Integration Tests)

Testowanie wspó³pracy komponentów z prawdziw¹ baz¹ danych SQLite.

```bash
dotnet test 10xCards.Integration.Tests
```

**Zakres:**
- Operacje CRUD z EF Core
- Migracje bazy danych
- Transakcje i integralnoœæ danych
- Relacje miêdzy encjami

### Testy E2E (End-to-End - Playwright)

Testowanie pe³nych przep³ywów u¿ytkownika w przegl¹darce.

**Windows:**
```bash
cd 10xCards.E2E.Tests
.\run-tests.bat
```

**Linux/Mac:**
```bash
cd 10xCards.E2E.Tests
dotnet test
```

**Zakres:**
- Rejestracja i logowanie
- Generacja i akceptacja propozycji fiszek
- Zarz¹dzanie kartami i kolekcjami
- Sesje powtórek
- Panel administracyjny

Szczegó³owe instrukcje: [10xCards.E2E.Tests/README.md](10xCards.E2E.Tests/README.md)

### Uruchom wszystkie testy

```bash
dotnet test
```

### Code Coverage

Pokrycie kodu zbierane jest automatycznie przez CI/CD i raportowane do Codecov:

```bash
# Generowanie lokalnego raportu pokrycia
dotnet test --collect:"XPlat Code Coverage"
```

Plan testów i strategia: [.ai/test-plan.md](.ai/test-plan.md)

## ?? CI/CD

Projekt wykorzystuje GitHub Actions do automatycznej integracji i wdra¿ania:

- **Build** – Kompilacja rozwi¹zania w konfiguracji Release
- **Testy jednostkowe** – Automatyczne uruchamianie testów z `10xCards.Unit.Tests`
- **Testy E2E** – Automatyczne uruchamianie testów Playwright z `10xCards.E2E.Tests`
- **Code Coverage** – Zbieranie pokrycia kodu i przesy³anie do Codecov
- **Status PR** – Automatyczne komentarze z wynikami testów w pull requestach

### Konfiguracja

- **Workflow:** `.github/workflows/pull-request.yml`
- **Codecov:** `codecov.yml` (konfiguracja pokrycia kodu)
- **Uruchomienie:** Automatycznie przy ka¿dym pull requeœcie do `main`

### Struktura workflow

```
???????????
?  BUILD  ?
???????????
     ?
     ???????????????????????????????
     ?              ?              ?
????????????  ????????????  ????????????
?   UNIT   ?  ?   E2E    ?  ? COVERAGE ?
?  TESTS   ?  ?  TESTS   ?  ? (Codecov)?
????????????  ????????????  ????????????
     ?              ?              ?
     ???????????????????????????????
                    ?
                    ?
            ????????????????
            ?   STATUS     ?
            ?  COMMENT     ?
            ????????????????
```

### Pokrycie kodu

Raporty pokrycia kodu dostêpne na [Codecov](https://codecov.io/gh/domino336/10xCards):

- **Unit Tests** – pokrycie warstw Application, Domain, Persistence
- **E2E Tests** – pokrycie warstwy Web i integracji
- **Cel:** >70% pokrycia kodu

**Badge pokrycia:**
```markdown
[![codecov](https://codecov.io/gh/domino336/10xCards/branch/main/graph/badge.svg)](https://codecov.io/gh/domino336/10xCards)
```

Wiêcej informacji: [.github/workflows/README.md](.github/workflows/README.md)

## ?? Dokumentacja

Szczegó³owa dokumentacja techniczna znajduje siê w katalogu `.ai/`:

- **[prd.md](.ai/prd.md)** – Product Requirements Document (wymagania produktowe, user stories, metryki)
- **[tech-stack.md](.ai/tech-stack.md)** – Stack technologiczny i decyzje architektoniczne
- **[db-plan.md](.ai/db-plan.md)** – Schemat bazy danych, encje, relacje, indeksy
- **[test-plan.md](.ai/test-plan.md)** – Strategia testowania
- **[copilot-instructions.md](.ai/copilot-instructions.md)** – Regu³y i konwencje kodowania dla AI

## ?? Architektura

Aplikacja wykorzystuje wzorce:

- **Clean Architecture** – separacja warstw (Domain, Application, Persistence, Web)
- **Dependency Injection** – bezpoœrednie wstrzykiwanie serwisów (bez MediatR)
- **Repository Pattern** – abstrakcja dostêpu do danych (EF Core)
- **DTO Pattern** – separacja modeli domenowych od modeli widoku
- **Code-behind Pattern** – `*.razor.cs` dla komponentów z logik¹ > 40 linii

## ?? Bezpieczeñstwo

- HTTPS wymuszony
- ASP.NET Core Identity do uwierzytelniania
- Ochrona przed CSRF (`UseAntiforgery`)
- Walidacja danych wejœciowych (DataAnnotations)
- Sanityzacja HTML/XSS
- Izolacja danych per u¿ytkownik (UserId filtering)

## ?? Metryki sukcesu

- **Wspó³czynnik akceptacji AI:** ?75% (acceptedAI / generatedAI)
- **Udzia³ AI w tworzeniu:** ?75% fiszek z AI
- **Aktywni u¿ytkownicy:** Wzrost stabilny w 7/30 dni
- **Czas generacji:** <10s dla batchu 100 segmentów

## ?? Wk³ad

Projekt certyfikacyjny w ramach kursu [10xDevs](https://10xdevs.io/).

## ?? Licencja

Projekt edukacyjny.

## ?? Autor

[GitHub](https://github.com/domino336)

---

**Status projektu:** ?? MVP w aktywnym rozwoju

Dla developerów: Przed rozpoczêciem pracy zapoznaj siê z [.ai/copilot-instructions.md](.ai/copilot-instructions.md)
