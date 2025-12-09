# Plan bazy danych – 10xCards MVP (SQLite + EF Core)

## 1. Przegląd
Baza: SQLite (plik `AppData/10xCards.db`). Dostęp: Entity Framework Core.
Cel: minimalny, prosty schemat wspierający generację i naukę fiszek (Spaced Repetition) oraz logowanie akcji.

## 2. Typy domenowe (C# enums)
```csharp
public enum GenerationMethod { Ai = 0, Manual = 1 }
public enum AcceptanceAction { Accepted = 0, Rejected = 1 }
public enum ReviewResult { Again = 0, Good = 1, Easy = 2 }
```
Konwersja w EF: `.HasConversion<string>()` lub domyślny int (preferowany dla prostoty). Export do UI jako string przez `.ToString()`.

## 3. Encje (klasy EF Core)
```csharp
public class SourceText
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty; // SHA256
    public DateTime CreatedUtc { get; set; }
}

public class CardProposal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SourceTextId { get; set; }
    public SourceText? SourceText { get; set; }
    public string Front { get; set; } = string.Empty; // question
    public string Back { get; set; } = string.Empty;  // answer
    public GenerationMethod GenerationMethod { get; set; } = GenerationMethod.Ai;
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; } // cleanup window (np. +7 dni)
}

public class Card
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SourceTextId { get; set; }
    public SourceText? SourceText { get; set; }
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
    public GenerationMethod GenerationMethod { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public CardProgress? Progress { get; set; } // 1:1
}

public class CardProgress
{
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTime NextReviewUtc { get; set; }
    public int ReviewCount { get; set; }
    public DateTime? LastReviewUtc { get; set; }
    public ReviewResult? LastReviewResult { get; set; }
    public string SrState { get; set; } = "{}"; // JSON (TEXT)
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class AcceptanceEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ProposalId { get; set; }
    public Guid? CardId { get; set; }
    public AcceptanceAction Action { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public class ActiveGeneration
{
    public Guid UserId { get; set; }
    public DateTime StartedUtc { get; set; }
}

public class GenerationError
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? SourceTextId { get; set; }
    public string ErrorType { get; set; } = string.Empty; // timeout, api_error, parse_error
    public string? ErrorMessage { get; set; }
    public DateTime CreatedUtc { get; set; }
}
```

## 4. Konfiguracja Fluent API (wybrane fragmenty)
```csharp
modelBuilder.Entity<SourceText>()
    .HasIndex(x => new { x.UserId, x.ContentHash });

modelBuilder.Entity<CardProposal>()
    .HasIndex(x => x.UserId);
modelBuilder.Entity<CardProposal>()
    .HasIndex(x => x.ExpiresUtc);

modelBuilder.Entity<Card>()
    .HasIndex(x => x.UserId);
modelBuilder.Entity<Card>()
    .HasIndex(x => new { x.GenerationMethod, x.CreatedUtc });

modelBuilder.Entity<CardProgress>()
    .HasKey(x => x.CardId);
modelBuilder.Entity<CardProgress>()
    .HasIndex(x => new { x.UserId, x.NextReviewUtc });

modelBuilder.Entity<AcceptanceEvent>()
    .HasIndex(x => new { x.UserId, x.CreatedUtc });

modelBuilder.Entity<ActiveGeneration>()
    .HasKey(x => x.UserId);

modelBuilder.Entity<GenerationError>()
    .HasIndex(x => new { x.UserId, x.CreatedUtc });
```
Walidacje długości: użyć DataAnnotations `[MinLength]`, `[MaxLength]` lub FluentValidation. Przykład: Front/Back 50–500 znaków.

## 5. Reguły biznesowe
- Propozycja wygasa po 7 dniach (Hosted Service usuwa rekordy `ExpiresUtc < now`).
- Akceptacja tworzy `Card` + `CardProgress` + `AcceptanceEvent` i usuwa `CardProposal`.
- Odrzucenie tworzy `AcceptanceEvent` i usuwa `CardProposal`.
- `ActiveGeneration` blokuje równoczesną generację dla użytkownika (sprawdzenie czy istnieje rekord młodszy niż X minut).
- Spaced Repetition: przy zapisie wyniku aktualizacja `ReviewCount`, `LastReviewUtc`, `LastReviewResult`, wyliczenie nowego `NextReviewUtc`, aktualizacja `SrState`.

## 6. Spaced Repetition – SrState (przykład JSON)
```json
{
  "ease": 2.5,
  "interval": 3,
  "stability": 0.4,
  "difficulty": 0.3
}
```
Serializacja: `SrState = JsonSerializer.Serialize(obj)`; deserializacja przy kalkulacji następnej powtórki.

## 7. Migracje EF Core (proponowana kolejność)
1. InitialCreate – SourceTexts, CardProposals, Cards, CardProgress, AcceptanceEvents, ActiveGenerations, GenerationErrors.
2. AddIndexes – dodatkowe indeksy kompozytowe.
3. AddSrStateDefaults – ewentualne wartości początkowe.
4. Future: AddUserQuota (rate limiting).

## 8. Operacje serwisowe (serwisy aplikacyjne)
- `IProposalService`: tworzenie/zamykanie/akceptacja/odrzucanie.
- `ICardService`: edycja fiszek, pobieranie do nauki (`NextReviewUtc <= now`).
- `IReviewService`: zapis wyniku powtórki (algorytm SR).
- `IGenerationThrottleService`: kontrola `ActiveGeneration`.
- `ICleanupService` (Hosted Service): usuwa wygasłe propozycje i stare wpisy `ActiveGeneration`.

## 9. Algorytm SR (MVP)
Prosty SM-2 lub uproszczony FSRS:
- Ocena: Again/Good/Easy.
- Interval: start 1d / 3d / 5d zależnie od wyniku.
- Ease modyfikowana: `ease = ease + delta` (delta zależna od wyniku).
Przechowywane w `SrState` bez migracji schematu.

## 10. Bezpieczeństwo
- Filtrowanie po `UserId` w każdej kwerendzie LINQ.
- ASP.NET Core Identity kontroluje użytkowników; dodać role (`Admin`).
- Brak RLS (niewspierane w SQLite) – testy serwisów zapewniają izolację.

## 11. Deduplikacja SourceText
1. Oblicz SHA256 `Content`.
2. Sprawdź istniejący `SourceText` dla `(UserId, ContentHash)`.
3. Jeśli istnieje – użyj jego `Id`.
4. Inaczej – utwórz nowy rekord.

## 12. Monitoring i utrzymanie
- Logi EF + czas wykonania zapytań.
- Rozmiar pliku DB > określony próg – komunikat do logów.
- Backup: kopiowanie pliku `10xCards.db` do folderu `Backups/` z datą (skrypt lub job GitHub Actions przed deploymentem).

## 13. Migracja przyszła do PostgreSQL (opcjonalna)
- Zamiana TEXT JSON → JSONB.
- Dodanie partial indexes (due reviews, new cards).
- Implementacja RLS.
- Trigger aktualizacji `UpdatedUtc`.

## 14. Scenariusze zapytań (przykłady LINQ)
- Fiszki do powtórki:
```csharp
var due = _db.CardProgress
    .Where(x => x.UserId == userId && x.NextReviewUtc <= nowUtc)
    .OrderBy(x => x.NextReviewUtc)
    .Take(50)
    .Select(x => x.Card)
    .ToList();
```
- Propozycje aktywne:
```csharp
var proposals = _db.CardProposals
    .Where(p => p.UserId == userId && p.ExpiresUtc > nowUtc)
    .OrderByDescending(p => p.CreatedUtc)
    .ToList();
```

## 15. Testy (zakres)
- Akceptacja propozycji: tworzy Card + CardProgress + AcceptanceEvent i usuwa CardProposal.
- Odrzucenie: tworzy AcceptanceEvent i usuwa CardProposal.
- Review: poprawna aktualizacja pól + przesunięcie `NextReviewUtc`.
- Cleanup: propozycje po dacie wygaszenia znikają.
- Throttle: druga generacja blokowana w oknie czasowym.

## 16. Otwarte kwestie
- Wybór docelowego algorytmu SR (FSRS vs SM-2).
- Implementacja rate limiting (UserQuota).
- Format migracji ease/interval gdy zmieni się algorytm (wersjonowanie w `SrState`: `{ "version": 2, ... }`).

## 17. Podsumowanie
Schemat uproszczony pod kątem szybkiej implementacji w Blazor Server + EF Core + SQLite. Elastyczność przez tekstowe pole JSON (`SrState`), brak zaawansowanych konstrukcji PostgreSQL. Gotowe do rozpoczęcia kodowania DbContext, migracji inicjalnej i serwisów domenowych.
