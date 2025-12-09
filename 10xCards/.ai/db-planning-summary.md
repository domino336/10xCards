# Podsumowanie planowania bazy danych dla 10xCards MVP (aktualizacja: SQLite + EF Core)

## Podjęte decyzje (zaktualizowane)

1. Silnik bazy: SQLite (plik aplikacji `AppData/10xCards.db`) zarządzany przez Entity Framework Core.
2. Model propozycji: Osobna tabela `CardProposals` dla tymczasowych propozycji AI.
3. Źródła tekstowe: Osobna tabela `SourceTexts` dla wklejanych notatek użytkownika (opcjonalnie dołączanych do generacji AI).
4. Spaced Repetition: Minimalna implementacja w tabeli `CardProgress` z polem tekstowym `SrState` (JSON serializowany jako TEXT) + podstawowe pola (`NextReviewUtc`, `ReviewCount`, `LastReviewResult`).
5. Logowanie akcji: Tabela `AcceptanceEvents` rejestrująca akceptację / odrzucenie propozycji (z referencją do `ProposalId` oraz docelowego `CardId` po akceptacji).
6. Indeksowanie: Indeksy EF Core (HasIndex) na kolumnach filtrów powtórek (`UserId`, `NextReviewUtc`) oraz statystyk tworzenia (`GenerationMethod`, `CreatedUtc`).
7. Usuwanie danych: Hard delete + relacje z kaskadowym usuwaniem w EF (OnDelete Cascade) gdzie potrzebne.
8. Ograniczenie równoczesnych generacji: Prosta tabela `ActiveGenerations` (klucz `UserId`, znacznik czasu). WALIDACJA w kodzie (nie advisory locks – brak wsparcia w SQLite).
9. Walidacja danych: Logika walidacji po stronie aplikacji (FluentValidation lub atrybuty DataAnnotations); brak constraintów CHECK specyficznych dla PostgreSQL.
10. Skalowalność: Jeden plik DB dla MVP; możliwość migracji do PostgreSQL / MSSQL później bez zmiany warstwy domenowej.

## Uzasadnienia zmian względem wcześniejszego planu PostgreSQL
- Usunięto RLS (Row Level Security) – SQLite nie obsługuje tego mechanizmu; bezpieczeństwo realizowane przez ASP.NET Core Identity + filtrowanie po `UserId` w zapytaniach.
- JSONB zastąpiono TEXT + serializacja (System.Text.Json) – wystarczające dla elastycznego stanu algorytmu SR.
- ENUM typy (PostgreSQL) zastąpione przez `string` lub `int` + mapowanie w kodzie (np. enum C# + konwersja EF).
- Brak funkcji/triggerów PL/pgSQL – ewentualne automatyzacje (cleanup starych propozycji) wykonywane przez zadanie aplikacyjne (Hosted Service) lub job w CI/CD.
- Indeksy częściowe / materialized views – niedostępne; proste indeksy B-Tree przez EF wystarczą przy skali MVP.

## Schemat logiczny (nazwy klas / tabel EF)
```
User (ASP.NET Core Identity)
    ↓ 1:N
SourceText (Id, UserId, Content, CreatedUtc)
    ↓ 1:N
CardProposal (Id, UserId, SourceTextId?, Front, Back, CreatedUtc, ExpiresUtc, GenerationMethod)
    ↓ (akceptacja) tworzy ↓
Card (Id, UserId, SourceTextId?, Front, Back, CreatedUtc, GenerationMethod)
    ↓ 1:1
CardProgress (CardId PK, UserId, NextReviewUtc, ReviewCount, LastReviewResult, SrState TEXT)

AcceptanceEvent (Id, UserId, ProposalId?, CardId?, Action, CreatedUtc)
ActiveGeneration (UserId PK, StartedUtc)
```

## Typy danych (SQLite + EF Core)
- Klucze główne: GUID (EF `Guid`) – przechowywane jako TEXT (domyślnie) lub BLOB; wygodne do generowania po stronie aplikacji.
- Daty: `DateTime` (UTC) – konwencja `*Utc` w nazwie pola.
- JSON state: `SrState` przechowuje serializowany obiekt (easeFactor, interval, difficulty itp.).
- Enums (C#): `GenerationMethod`, `AcceptanceAction`, `ReviewResult` – konwertowane do string lub int przez `HasConversion`.

## Indeksowanie (przykłady EF Fluent API)
- CardProgress: HasIndex(x => new { x.UserId, x.NextReviewUtc }) – pobieranie fiszek do powtórki.
- Cards: HasIndex(x => new { x.GenerationMethod, x.CreatedUtc }).
- AcceptanceEvents: HasIndex(x => new { x.UserId, x.CreatedUtc }).

## Spaced Repetition – minimalny model
Pola:
- `NextReviewUtc` – wyliczane algorytmem.
- `ReviewCount` – inkrementowane przy każdej powtórce.
- `LastReviewResult` – enum (again/good/easy).
- `SrState` – struktura np. `{ "ease": 2.5, "interval": 3, "stability": 0.4 }`.
Migracja algorytmu: dodanie nowych parametrów w JSON bez migracji tabel.

## Proces akceptacji (flow)
1. Pobierz propozycje użytkownika.
2. Użytkownik akceptuje → tworzony `Card`, `CardProgress`, wpis w `AcceptanceEvents`, usunięcie `CardProposal` lub oznaczenie jako przetworzone.
3. Odrzucenie → wpis w `AcceptanceEvents` (Action=Rejected), opcjonalne usunięcie propozycji.

## Ograniczenie równoczesnych generacji
- Wstawienie rekordu do `ActiveGenerations` przy starcie.
- Sprawdzenie czy istnieje aktywny wpis młodszy niż N minut.
- Usunięcie wpisu po zakończeniu generacji.
(Możliwe zastąpienie prostym semaforem w pamięci przy single-instance hostingu.)

## Strategia czyszczenia
Hosted Service (np. `IHostedService`) uruchamiany co X godzin:
- Usuwa `CardProposals` z `ExpiresUtc < now`.
- Czyści stare `ActiveGenerations` (>5 min).

## Bezpieczeństwo
- Dostęp filtrowany po `UserId` (LINQ Where).
- ASP.NET Core Identity obsługujący użytkowników i role; rola `Admin` opcjonalnie.
- Brak RLS – kontrola w warstwie aplikacji + testy jednostkowe serwisów.

## Migracje i ewolucja
- EF Core Migrations generowane komendą `dotnet ef migrations add <Name>`.
- Przy przyszłej migracji do PostgreSQL: mapowanie JSON → JSONB, dodanie indeksów partial, RLS.

## Monitoring
- Na etapie MVP: logowanie EF (Category: Microsoft.EntityFrameworkCore.Database.Command) do konsoli / pliku.
- Później: Serilog + metryki (Prometheus / OpenTelemetry) jeśli host produkcyjny.

## Rzeczy usunięte / zastąpione
- Supabase Auth / RLS → ASP.NET Core Identity.
- PostgreSQL JSONB → TEXT JSON.
- Advisory locks → Prosty wpis w tabeli + walidacja czasu.
- pg_cron / funkcje PL/pgSQL → Hosted Service w .NET.
- Materialized views / zaawansowane indeksy → zwykłe indeksy EF.

## Nierozwiązane kwestie (dostosowane)
1. Wybór algorytmu SR (FSRS vs SM2) – serializacja w `SrState` gotowa.
2. Rate limiting generacji AI – potrzebna tabela `UserQuota` (PLAN).
3. Backup pliku SQLite – skrypt kopii (`AppData/10xCards.db` + timestamp) w GitHub Actions / cron systemowy.
4. Testy konkurencji zapisu – do zweryfikowania (SQLite ma blokady pliku; przy małej skali akceptowalne).
5. Migracja do silnika SQL przy wzroście – przygotować abstrakcję repozytoriów.

## Podsumowanie
Aktualny plan uwzględnia prostotę i szybkość implementacji z lokalnym plikiem SQLite oraz EF Core. Architektura tabel pozostała podobna koncepcyjnie do wcześniejszego projektu, ale uproszczono elementy specyficzne dla PostgreSQL i Supabase. Dokument gotowy jako baza do implementacji modeli EF i migracji początkowej.
