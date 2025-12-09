# Serilog Logging Configuration

## Overview

Aplikacja 10xCards uøywa **Serilog** do strukturalnego logowania z zapisem do plikÛw oraz konsoli.

## Konfiguracja

### Pakiety NuGet
- `Serilog.AspNetCore` (8.0.3)
- `Serilog.Sinks.File` (6.0.0)

### Lokalizacja logÛw
Logi sπ zapisywane w katalogu `logs/` w g≥Ûwnym folderze aplikacji:
```
logs/
  10xcards-20240101.log
  10xcards-20240102.log
  ...
```

### Rotacja plikÛw
- **Rolling Interval**: Dziennie (RollingInterval.Day)
- **Retention**: 30 dni (retainedFileCountLimit: 30)
- **Format nazwy**: `10xcards-YYYYMMDD.log`

### Format logÛw
```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

**Przyk≥ad:**
```
2024-12-08 14:23:45.123 +01:00 [INF] Starting 10xCards application...
2024-12-08 14:23:46.456 +01:00 [ERR] Failed to create card: Invalid input
System.InvalidOperationException: Invalid input
   at CardService.CreateManualCardAsync(...)
```

## Poziomy logowania

### Minimum Levels
- **Default**: Information
- **Microsoft**: Warning (filtruje verbose EF Core i ASP.NET logs)
- **Microsoft.AspNetCore**: Warning
- **Microsoft.EntityFrameworkCore**: Warning

### DostÍpne poziomy
1. **Verbose** - Bardzo szczegÛ≥owe informacje (nie uøywane domyúlnie)
2. **Debug** - Informacje debug (development only)
3. **Information** - Normalne operacje aplikacji
4. **Warning** - Ostrzeøenia, nieoczekiwane sytuacje
5. **Error** - B≥Ídy, exceptions
6. **Fatal** - Krytyczne b≥Ídy powodujπce zamkniÍcie aplikacji

## Co jest logowane?

### 1. Startup/Shutdown
```csharp
Log.Information("Starting 10xCards application...");
Log.Information("10xCards application stopped.");
```

### 2. Database Operations
```csharp
Log.Information("Database migrations applied successfully.");
Log.Information("Test user created: test@10xcards.local");
Log.Information("Admin user created: admin@10xcards.local");
```

### 3. HTTP Requests (via UseSerilogRequestLogging)
```
HTTP GET /cards responded 200 in 45.6789 ms
HTTP POST /api/proposals responded 201 in 123.4567 ms
```

### 4. Card Operations
```csharp
_logger.LogInformation("Manual card created: {CardId} by user {UserId}", cardId, userId);
_logger.LogInformation("Card deleted: {CardId} by user {UserId}", cardId, userId);
_logger.LogWarning("Delete card failed: Card {CardId} not found for user {UserId}", cardId, userId);
```

### 5. Unhandled Exceptions
```csharp
_logger.LogError(ex, 
    "Unhandled exception occurred. Path: {Path}, Method: {Method}, User: {User}, TraceId: {TraceId}",
    context.Request.Path,
    context.Request.Method,
    context.User?.Identity?.Name ?? "Anonymous",
    context.TraceIdentifier);
```

### 6. Fatal Errors
```csharp
Log.Fatal(ex, "Application terminated unexpectedly");
Log.Fatal(ex, "An error occurred while migrating or seeding the database.");
```

## Request Logging Enrichment

Kaødy HTTP request jest wzbogacony o:
- **RequestHost**: Host name
- **UserAgent**: User agent header
- **RequestPath**: Request URL path
- **StatusCode**: HTTP status code
- **Elapsed**: Czas wykonania w ms

## Error Handling

### ExceptionLoggingMiddleware
Middleware przechwytuje wszystkie nieobs≥uøone wyjπtki i loguje je z kontekstem:
- Request path
- HTTP method
- User identity
- TraceIdentifier

### Error.razor
Strona b≥Ídu wyúwietla Request ID i loguje event:
```csharp
Logger.LogError("Error page accessed. RequestId: {RequestId}, TraceIdentifier: {TraceIdentifier}", 
    RequestId, HttpContext?.TraceIdentifier);
```

## Uøycie w serwisach

### Dependency Injection
```csharp
public sealed class CardService : ICardService
{
    private readonly ILogger<CardService> _logger;
    
    public CardService(CardsDbContext db, ILogger<CardService> logger)
    {
        _db = db;
        _logger = logger;
    }
}
```

### Przyk≥ady logowania
```csharp
// Information
_logger.LogInformation("Manual card created: {CardId} by user {UserId}", card.Id, userId);

// Warning
_logger.LogWarning("Delete card failed: Card {CardId} not found for user {UserId}", cardId, userId);

// Error with exception
_logger.LogError(ex, "Failed to create manual card for user {UserId}", userId);
```

## Structured Logging

Serilog wspiera strukturalne logowanie - uøywaj placeholders zamiast interpolacji:

? **Dobrze:**
```csharp
_logger.LogInformation("Card {CardId} created by user {UserId}", cardId, userId);
```

? **èle:**
```csharp
_logger.LogInformation($"Card {cardId} created by user {userId}");
```

## Konsola vs Plik

### Console Sink
- Wyúwietla logi w czasie rzeczywistym podczas developmentu
- Kolorowe formatowanie poziomÛw
- Pomocne przy debugowaniu

### File Sink
- Persystentne przechowywanie logÛw
- Rotacja dzienna
- Przydatne do analizy post-mortem
- Audit trail

## Troubleshooting

### Brak katalogu logs/
Katalog jest tworzony automatycznie przy pierwszym uruchomieniu.

### Brak uprawnieÒ do zapisu
Upewnij siÍ, øe aplikacja ma uprawnienia do zapisu w katalogu roboczym:
```bash
chmod 755 logs/  # Linux/Mac
```

### Za duøo logÛw z EF Core/ASP.NET
Zmniejsz poziom w `Program.cs`:
```csharp
.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
```

### Sprawdzanie logÛw
```bash
# Linux/Mac
tail -f logs/10xcards-20240108.log

# Windows PowerShell
Get-Content logs\10xcards-20240108.log -Wait -Tail 50
```

## Best Practices

1. **Uøyj structured logging** - zawsze uøywaj placeholders
2. **Loguj kontekst** - UserId, CardId, RequestId itp.
3. **Nie loguj wraøliwych danych** - has≥a, tokeny, PII
4. **Uøywaj odpowiednich poziomÛw**:
   - Information: Normalne operacje
   - Warning: Nieoczekiwane sytuacje (nie b≥Ídy)
   - Error: B≥Ídy wymagajπce uwagi
   - Fatal: Krytyczne b≥Ídy
5. **Nie loguj w pÍtlach** - moøe zapchaÊ logi
6. **Uøywaj try-catch z logowaniem** w critical paths

## Production Considerations

W úrodowisku produkcyjnym rozwaø:
- ZwiÍkszenie retention do 90 dni
- Dodanie sink do centralnego systemu (np. Seq, Elasticsearch)
- Monitoring alertÛw na podstawie logÛw Error/Fatal
- KompresjÍ starszych plikÛw
- Automatyczne czyszczenie starych logÛw

## Dodatkowe Sinki (opcjonalne)

Moøesz dodaÊ wiÍcej sink'Ûw:

### Seq (centralized logging)
```bash
dotnet add package Serilog.Sinks.Seq
```
```csharp
.WriteTo.Seq("http://localhost:5341")
```

### Elasticsearch
```bash
dotnet add package Serilog.Sinks.Elasticsearch
```

### Application Insights
```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```
