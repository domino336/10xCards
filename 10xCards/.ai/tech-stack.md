# Tech Stack

## Frontend
Blazor Server (.NET 9, Razor Components). Zapewnia renderowanie po stronie serwera i interaktywność przez sygnały (Circuit / SignalR pod spodem). Obecnie używamy Bootstrap (w repo jest `wwwroot/lib/bootstrap`).

Tailwind: możliwa integracja, ale wymaga dodania toolchainu Node (npm + `tailwindcss` CLI) oraz procesu budowania (np. skrypt w GitHub Actions / lokalnie). Ponieważ projekt nie posiada jeszcze konfiguracji Node ani pliku konfiguracyjnego Tailwind (`tailwind.config.js`) – usuwamy Tailwind ze stacku na ten moment. Możemy wrócić do tematu, gdy będzie potrzeba bardziej zaawansowanego utility-first stylowania.

Komponenty UI: natywne komponenty Blazor + własne lekkie komponenty. (Brak React / Astro / shadcn – usunięte jako nieadekwatne do aktualnej architektury.)

## Backend / API
ASP.NET Core (.NET 9) w jednym projekcie (ten sam co frontend) – model hostingu minimalnego. API (minimal APIs lub kontrolery) zostaną dodane stopniowo (na dziś brak dedykowanych endpointów poza komponentami Blazor).

## Dane
SQLite (plik w lokalnym systemie) jako lekka baza. Dostęp przez Entity Framework Core (plan: dodać pakiety `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Sqlite`, migracje, konfigurację DbContext). 

Strategia: 
- Start: jeden plik DB (np. `AppData/10xCards.db`).
- EF Core Migrations do ewolucji schematu.
- Możliwość późniejszego przejścia na PostgreSQL lub MSSQL jeśli zajdzie potrzeba skalowania.

## AI
Planowana integracja z OpenRouter (wywołania REST z HttpClient w .NET) – pozwoli testować różne modele (OpenAI / Anthropic / inne) przy kontroli kosztów. Dodamy warstwę abstrahującą (np. serwis `IAiCompletionService`). Na dziś brak implementacji – status: PLAN.

## Autentykacja / Autoryzacja
Do rozważenia: 
- Początkowo lokalne konta ASP.NET Core Identity + SQLite.
- Alternatywnie integracja z zewnętrznym IdP (np. Auth0 / Entra ID) w późniejszym etapie.

## CI/CD
GitHub Actions: 
- Build + test + publikacja artefaktu.
- (Plan) workflow kontenerowy / publikacja obrazu.

## Hosting
Faza początkowa: lokalny dev. 
Docelowo: dowolny hosting dla aplikacji .NET (np. DigitalOcean droplet z dockerem lub Azure App Service). DigitalOcean pozostaje opcją – z poprzedniego planu.

## Monitorowanie / Observability (PLAN)
- Logowanie: `ILogger` + persystencja (opcjonalnie Serilog) w przyszłości.
- Health checks endpoint.

## Bezpieczeństwo
- HTTPS wymuszone (już w konfiguracji).
- Walidacja wejścia w API gdy powstaną endpointy.
- Ochrona przed CSRF: `app.UseAntiforgery()` – już aktywne.

## Rzeczy usunięte z poprzedniego dokumentu
- Astro, React, TypeScript (niepotrzebne w Blazor Server C#).
- Supabase (zastępujemy własnym EF Core + SQLite).
- Tailwind (tymczasowo usunięty – brak toolchainu, można dodać później).
- shadcn/ui (biblioteka React, niezgodna z Blazor).

## Możliwe przyszłe rozszerzenia
- Tailwind (po dodaniu Node toolchain + skryptów budujących).
- Przejście z Bootstrap na Tailwind lub mieszane podejście.
- Dodanie warstwy API (minimal APIs) dla klientów zewnętrznych / integracji mobilnej.
- Caching (MemoryCache / Redis gdy potrzeba skali).

## Podsumowanie
Aktualny realny stack: Blazor Server (.NET 9), ASP.NET Core, Bootstrap, EF Core + SQLite, planowana integracja AI (OpenRouter), CI/CD przez GitHub Actions. Pozostałe elementy z poprzedniego pliku usunięto jako nieadekwatne.