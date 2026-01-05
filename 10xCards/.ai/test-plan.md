
# Plan testów dla projektu 10xCards

## 1. Wprowadzenie i cele testowania

Celem testowania w projekcie **10xCards** jest potwierdzenie, że aplikacja:

- Zapewnia poprawny i stabilny przebieg kluczowego przepływu użytkownika:
  - Rejestracja i logowanie
  - Generacja propozycji kart z tekstu
  - Przegląd i akceptacja lub odrzucanie propozycji
  - Zarządzanie biblioteką kart oraz kolekcjami
  - Sesje powtórek z algorytmem SR
  - Panel administratora z metrykami
- Spełnia wymagania funkcjonalne z PRD, ze szczególnym naciskiem na historyjki użytkownika od US 5 do US 52
- Jest bezpieczna pod względem uwierzytelniania i autoryzacji, a dane użytkowników są izolowane
- Jest wystarczająco wydajna i odporna na błędy dla zakładanej skali MVP
- Jest pokryta adekwatnym zestawem testów automatycznych, uruchamianych w CI

Docelowo testy mają minimalizować ryzyko regresji przy dalszym rozwoju funkcjonalności i ułatwiać integrację nowych elementów (np. prawdziwej integracji z OpenRouter zamiast obecnego mocka w `ProposalService`).

## 2. Zakres testów

### 2.1 Zakres funkcjonalny objęty testami

- **Uwierzytelnianie i autoryzacja**
  - Rejestracja
  - Logowanie formularzowe przez endpoint api auth login form
  - Wylogowanie
  - Wymuszanie zalogowania dla stron oznaczonych atrybutem `Authorize`
  - Dostęp do panelu admina wyłącznie dla roli Admin

- **Generacja kart z tekstu**
  - Walidacja długości tekstu wejściowego w `GenerateCards`
  - Mockowa generacja propozycji w `ProposalService.GenerateProposalsAsync`
  - Przegląd propozycji w `ReviewProposals`
  - Edycja, pojedyncza i masowa akceptacja, pojedyncze i masowe odrzucanie

- **Biblioteka kart**
  - Wyświetlanie listy kart z filtrami All, DueToday, New oraz paginacją
  - Ręczne tworzenie karty `CreateCard`
  - Edycja karty `EditCard`
  - Usuwanie karty z potwierdzeniem `ConfirmModal`
  - Wyliczanie statusów New, Due, Learned i dat następnej powtórki na podstawie `CardProgress`

- **Spaced repetition**
  - Pobieranie kart do powtórki `CardService.GetDueCardsAsync`
  - Sesja powtórek `ReviewSession` od stanu Loading do Finished
  - Aktualizacja `CardProgress` przez `SrService.RecordReviewAsync` dla wyników Again, Good, Easy
  - Wyliczanie kolejnych odstępów czasowych zgodnie z uproszczonym SM-2

- **Kolekcje kart (US 51)**
  - Tworzenie kolekcji z wybranych kart `CreateCollection`
  - Lista kolekcji z paginacją `Collections`
  - Szczegóły kolekcji `CollectionDetail`
  - Edycja nazwy, opisu oraz zawartości kolekcji `EditCollection`
  - Backup poprzedniej wersji `CardCollectionBackup` i odtwarzanie kolekcji
  - Usuwanie kolekcji bez usuwania kart

- **Panel admina**
  - Agregacja metryk w `AdminService` i prezentacja w Admin Dashboard
  - Cache metryk przez `IMemoryCache`
  - Statystyki kart, aktywni użytkownicy, błędy generacji, percentyle akceptacji

- **Obsługa błędów i logowanie**
  - Middleware `ExceptionLoggingMiddleware`
  - Strona błędu Error
  - TestErrorLogging w środowisku deweloperskim
  - Konfiguracja Seriloga w Program (logi do konsoli i pliku)

### 2.2 Poza zakresem na poziomie MVP

- Prawdziwe wywołania OpenRouter zamiast mocka (będą testowane dopiero po implementacji integracji HTTP)
- Zaawansowane testy wydajnościowe pod dużą skalą (na etapie MVP tylko proste smoke testy wydajnościowe)
- Zaawansowane testy bezpieczeństwa (np. testy penetracyjne) – na razie podstawowe scenariusze bezpieczeństwa logowania i autoryzacji



## 3. Typy testów do przeprowadzenia

### 3.1 Testy jednostkowe

**Cel:** Odizolowana weryfikacja logiki domenowej i logiki serwisów aplikacyjnych, bez prawdziwej bazy danych i bez Blazora.

**Zakres:**

- `SrService.RecordReviewAsync`
  - Wyliczanie nowego odstępu, zmiany ease factor i liczby powtórek dla wyników Again, Good, Easy
  - Prawidłowe ustawienie LastReviewUtc, NextReviewUtc, ReviewCount
- `CardService`
  - Walidacja długości front oraz back przy tworzeniu i edycji
  - Wyliczanie statusu New, Due, Learned
  - Prawidłowe filtrowanie i paginacja w GetCardsAsync (logika filtrowania w oderwaniu od EF)
- `ProposalService`
  - Walidacja długości tekstu wejściowego
  - Mockowe generowanie propozycji (`GenerateMockProposals` i `GenerateQuestion`)
  - Logika akceptacji i odrzucania propozycji (bez realnego EF, przy użyciu stubów lub mocków)
- `CollectionService`
  - Walidacja nazwy i opisu kolekcji
  - Tworzenie backupu przy UpdateCollection
  - Filtrowanie kart użytkownika przy tworzeniu i edycji kolekcji
  - Wykluczanie duplikatów w AddCardsToCollection
  - Logika RestorePreviousVersion
- `AdminService`
  - Obliczanie percentyli i współczynnika akceptacji z danych wejściowych
  - Zachowanie przy braku danych wejściowych

**Biblioteka:** xUnit, Moq.

### 3.2 Testy integracyjne

**Cel:** Zweryfikowanie poprawnej współpracy serwisów aplikacyjnych z prawdziwym CardsDbContext i SQLite.

**Zakres:**

- Scenariusz generacji propozycji z użyciem prawdziwego CardsDbContext
  - Zapis SourceText
  - Utworzenie CardProposal
  - Odzyskanie propozycji przez GetProposalsBySourceAsync
- Akceptacja i odrzucanie propozycji
  - Utworzenie Card, CardProgress, AcceptanceEvent, usunięcie CardProposal
  - Utworzenie tylko AcceptanceEvent i usunięcie CardProposal przy odrzuceniu
- Spaced repetition
  - Zapis CardProgress w bazie po wywołaniu RecordReviewAsync
  - Filtrowanie kart do powtórki w GetDueCardsAsync
- Kolekcje
  - Tworzenie, aktualizacja, usuwanie kolekcji oraz dodawanie i usuwanie kart z kolekcji z użyciem prawdziwej bazy

**Narzędzia:** Projekt `10xCards.Integration.Tests` z xUnit i SQLite, konfiguracja DbContext w trybie testowym.

### 3.3 Testy end to end z Playwright

**Cel:** Zweryfikowanie pełnych przepływów użytkownika w przeglądarce, włączając Blazor, API, bazę danych oraz identity.

**Kluczowe przepływy E2E:**

- Rejestracja
  - Wejście na stronę Register
  - Poprawne wypełnienie formularza
  - Przekierowanie na Login z komunikatem
  - Zalogowanie i weryfikacja dostępu do kart
- Logowanie standardowymi kontami testowymi
  - test 10xcards lokal
  - admin 10xcards lokal
  - Sprawdzenie poprawności przekierowań po logowaniu
- Generacja i przegląd propozycji
  - Wklejenie tekstu w Generate
  - Walidacje długości tekstu
  - Wyświetlenie propozycji w ReviewProposals
  - Edycja front i back dla jednej pozycji
  - Masowa akceptacja zaznaczonych propozycji
  - Weryfikacja obecności zaakceptowanych kart w My Cards
- Zarządzanie kartami
  - Ręczne utworzenie karty
  - Edycja karty
  - Usunięcie karty z potwierdzeniem
  - Sprawdzenie filtrów i paginacji
- Sesja powtórek
  - Przygotowanie kart z NextReviewUtc ustawionym na przeszłość
  - Rozpoczęcie sesji, pokazanie pytania, odpowiedzi i udzielenie odpowiedzi Again, Good, Easy
  - Zakończenie sesji i wyświetlenie podsumowania
- Kolekcje
  - Utworzenie kolekcji na podstawie istniejących kart
  - Edycja nazwy i opisu
  - Dodanie nowych kart do kolekcji
  - Usunięcie kart z kolekcji
  - Odtworzenie z backupu poprzedniej nazwy i opisu
  - Usunięcie kolekcji
- Panel admina
  - Logowanie jako admin
  - Otwarcie Dashboard
  - Sprawdzenie wyświetlania metryk, w tym AI Acceptance Rate i percentyli
  - Odświeżenie metryk przyciskiem Refresh

**Narzędzie:** Playwright dla C# lub Playwright w TypeScript z uruchomieniem aplikacji lokalnie na adresie z launchSettings.

### 3.4 Testy bezpieczeństwa podstawowego

- **Autoryzacja**
  - Próby wejścia bez logowania na strony z atrybutem `Authorize` kończą się przekierowaniem na Login
  - Próba wejścia na Admin Dashboard bez roli Admin skutkuje AccessDenied
- **Sesje**
  - Sprawdzenie, że po wylogowaniu nie można wrócić do chronionych stron przy użyciu wstecz w przeglądarce bez ponownego logowania
- **XSS**
  - Próby wprowadzenia treści z tagami skryptu w front i back karty oraz w opisach kolekcji
  - Weryfikacja, że nie dochodzi do wykonania skryptów po stronie klienta (w wersji MVP test manualny, w docelowej wersji testy automatyczne)

### 3.5 Testy wydajności smoke

- Czas generacji propozycji dla tekstu o maksymalnej długości w mocku
- Czas ładowania listy kart przy górnej granicy paginacji
- Czas otwarcia Dashboard admina przy większej liczbie kart i zdarzeń
- Realizacja początkowo manualna z użyciem logów i konsoli przeglądarki.

### 3.6 Testy regresji

- Automatyczne uruchamianie pakietu testów jednostkowych, integracyjnych i kluczowych scenariuszy Playwright po każdym merge do gałęzi głównej
- Zestaw scenariuszy regresyjnych obejmujący wszystkie główne funkcje

## 4. Scenariusze testowe dla kluczowych funkcjonalności

Poniżej wybrane scenariusze wysokiego poziomu, które będą rozwijane na szczegółowe przypadki testowe.

### 4.1 Uwierzytelnianie i autoryzacja

- **Rejestracja**
  - Poprawna rejestracja nowego użytkownika
  - Próba rejestracji z istniejącym adresem e-mail
  - Walidacja haseł za krótkich i nie spełniających wymogów
- **Logowanie**
  - Poprawne logowanie dla test 10xcards lokal
  - Poprawne logowanie dla admin 10xcards lokal
  - Niepoprawne hasło powoduje właściwy komunikat błędu
- **Autoryzacja**
  - Wejście na Cards, Collections, Review, Generate bez zalogowania
  - Wejście na Admin Dashboard bez roli Admin
  - Sprawdzenie przekierowań i komunikatów

### 4.2 Generacja i przegląd propozycji

- **Walidacja długości tekstu w GenerateCards**
  - Tekst krótszy niż 50 znaków
  - Tekst dłuższy niż limit
  - Poprawny tekst
- **Mockowa generacja**
  - Sprawdzenie liczby wygenerowanych propozycji w zależności od liczby zdań
  - Sprawdzenie, że front oraz back spełniają ograniczenia długości po stronie serwisu
- **ReviewProposals**
  - Załadowanie propozycji dla danego SourceTextId
  - Zaznaczenie pojedynczej i wielu propozycji
  - Edycja front oraz back i zapisanie zmian w edycji lokalnej
  - Akceptacja zaznaczonych propozycji i weryfikacja w bazie kart
  - Odrzucenie zaznaczonych propozycji i weryfikacja braku kart

### 4.3 Biblioteka kart

- **Lista kart**
  - Wyświetlenie kart z filtrami All, DueToday, New
  - Sprawdzenie paginacji na granicy stron
- **Tworzenie karty manualnie**
  - Walidacje długości front i back
  - Poprawne utworzenie karty oraz CardProgress
  - Pojawienie się karty na liście
- **Edycja karty**
  - Edycja front i back z walidacją
  - Aktualizacja UpdatedUtc w bazie
  - Odtwarzanie danych karty w formularzu
- **Usuwanie karty**
  - Potwierdzenie usunięcia w ConfirmModal
  - Usunięcie karty i powiązanego CardProgress

### 4.4 Spaced repetition

- **Pobieranie kart do powtórki**
  - Brak kart z NextReviewUtc nieprzekroczonym
  - Istnieją karty z NextReviewUtc w przeszłości, zwrócone w prawidłowej kolejności
- **Sesja w ReviewSession**
  - Przejście stanów Loading, ShowFront, ShowBack, Finished
  - Przypadki:
    - Wszystkie odpowiedzi Again
    - Mieszanka Again, Good, Easy
    - Wszystkie odpowiedzi Easy
  - Sprawdzenie poprawności podsumowania sesji
- **Aktualizacja CardProgress**
  - Zwiększanie ReviewCount
  - Ustawianie NextReviewUtc zgodnie z algorytmem
  - Utrwalenie zmian w bazie

### 4.5 Kolekcje kart

- **Tworzenie kolekcji**
  - Walidacja nazwy i opisu
  - Próba użycia kart, które nie należą do użytkownika
  - Sprawdzenie, że kolekcja zawiera wybrane karty
- **Lista kolekcji**
  - Paginacja i sortowanie po dacie aktualizacji
  - Informacje o liczbie kart i czy istnieje backup
- **Szczegóły kolekcji**
  - Wyświetlenie kart należących do kolekcji
  - Linki do edycji kart
- **Edycja kolekcji**
  - Zmiana nazwy i opisu
  - Utworzenie backupu i możliwość odtworzenia poprzedniej wersji
  - Dodawanie kart, pomijanie duplikatów
  - Usuwanie wybranych kart z kolekcji
- **Usuwanie kolekcji**
  - Usunięcie kolekcji bez kasowania kart
  - Przekierowanie z CollectionDetail do listy kolekcji

### 4.6 Panel admina

- **Wyliczanie metryk**
  - Sprawdzenie poprawności liczby kart ogółem, manualnych i AI
  - Sprawdzenie pola AiAcceptanceRate przy różnych proporcjach zaakceptowanych zdarzeń
  - Sprawdzenie percentyli P50, P75, P90 dla różnych rozkładów danych
  - Sprawdzenie liczby aktywnych użytkowników w ostatnich 7 i 30 dniach na podstawie CreatedUtc kart
  - Sprawdzenie liczby błędów generacji
- **Cache metryk**
  - Dwa kolejne wywołania GetDashboardMetricsAsync w krótkim odstępie powinny korzystać z cache
  - Aktualizacja metryk po upłynięciu czasu cache

### 4.7 Obsługa błędów i logowanie

- **Middleware ExceptionLoggingMiddleware**
  - Zasymulowanie wyjątku w jednej z tras i weryfikacja wpisu w logach
  - Sprawdzenie, że użytkownik widzi stronę Error
- **Strona Error**
  - Wyświetlanie RequestId oraz komunikatu w trybie deweloperskim
  - Dostępne linki powrotne na stronę główną i do kart

## 5. Środowisko testowe

### 5.1 Konfiguracja środowiska lokalnego

- **System operacyjny:** Windows lub Linux z zainstalowanym SDK .NET 9
- **Baza danych:** SQLite plik `cards.db` w katalogu projektu
- **Skrypty:** `setup.bat`, `reset db.bat` do szybkiego przygotowania bazy
- **Config:** `appsettings.Development.json` z ustawieniami logowania
- **ASPNETCORE_ENVIRONMENT** ustawiony na Development
- **Uruchamianie aplikacji:**
  - `dotnet run --project 10xCards`
  - Lub z poziomu Visual Studio

### 5.2 Środowisko CI

- **GitHub Actions**
  - Job budujący i uruchamiający `dotnet test` dla projektów unit i integration
  - Dodatkowy job uruchamiający testy Playwright po wystartowaniu aplikacji testowej

### 5.3 Dane testowe

- **Konta:**
  - test 10xcards lokal hasło `Test123!`
  - admin 10xcards lokal hasło `Admin123!`
- **Dane przykładowe:**
  - Kilka zestawów kart i kolekcji tworzonych skryptem seed lub w testach integracyjnych
  - Teksty wejściowe do generacji o różnej długości i strukturze

## 6. Narzędzia do testowania

- **xUnit** – testy jednostkowe i integracyjne
- **Moq** – mockowanie interfejsów i zależności w testach jednostkowych
- **Microsoft NET Test Sdk plus coverlet** – uruchamianie testów i zbieranie pokrycia
- **SQLite in memory lub na pliku tymczasowym** – dla testów integracyjnych
- **Playwright** – testy end to end w przeglądarce
- **GitHub Actions** – CI zautomatyzowany dla testów

## 7. Harmonogram testów

Przykładowy iteracyjny harmonogram dla MVP:

- **Etap przygotowania**
  - Konfiguracja projektów testowych unit oraz integration
  - Konfiguracja podstawowego pipeline w GitHub Actions
- **Etap funkcji podstawowych**
  - Testy jednostkowe i integracyjne dla CardService, SrService, ProposalService
  - Pierwsze scenariusze E2E dla rejestracji, logowania i tworzenia kart
- **Etap funkcji generacji i propozycji**
  - Rozszerzenie testów ProposalService
  - Scenariusze E2E dla GenerateCards i ReviewProposals
- **Etap spaced repetition**
  - Rozbudowa testów SrService
  - E2E dla sesji ReviewSession
- **Etap kolekcji kart**
  - Testy jednostkowe i integracyjne CollectionService
  - E2E dla tworzenia i edycji kolekcji
- **Etap panelu admina oraz stabilizacji**
  - Testy AdminService
  - E2E sprawdzające Dashboard
  - Testy regresyjne pełnego przepływu użytkownika
- **Etap przed wydaniem**
  - Pełny przebieg pakietu testów
  - Naprawa krytycznych defektów
  - Ostateczne potwierdzenie kryteriów akceptacji

## 8. Kryteria akceptacji testów

- **Funkcjonalne**
  - Wszystkie kluczowe przepływy E2E przechodzą bez błędów
  - Brak otwartych defektów o wysokiej ważności blokujących scenariusze z PRD
  - Wszystkie wymagania funkcjonalne z PRD oznaczone jako MVP są pokryte przynajmniej jednym testem automatycznym lub manualnym
- **Jakościowe**
  - Co najmniej podstawowe pokrycie logiki serwisów aplikacyjnych i algorytmu SR testami jednostkowymi
  - Wszystkie projekty testowe przechodzą pozytywnie w CI
- **Bezpieczeństwo i autoryzacja**
  - Brak scenariusza, w którym użytkownik bez uprawnień ma dostęp do zasobów innego użytkownika lub panelu admina
  - Logowanie i wylogowanie działają poprawnie
- **Stabilność**
  - Brak systematycznych błędów nieobsłużonych wyjątków w logach przy podstawowych przepływach

## 9. Role i odpowiedzialności

| Rola | Główne odpowiedzialności |
|---|---|
| Inżynier QA | Przygotowanie planu testów, projektowanie scenariuszy, automatyzacja testów E2E, koordynacja regresji, raportowanie defektów |
| Programista backend | Implementacja testów jednostkowych i integracyjnych, poprawa defektów, utrzymanie jakości kodu, przygotowanie danych testowych |
| Programista frontend Blazor | Testy komponentów, wsparcie dla testów E2E Playwright, poprawa defektów interfejsu i logiki klienta |
| DevOps | Konfiguracja pipeline CI, integracja testów w GitHub Actions, utrzymanie środowisk testowych |
| Product owner lub lider techniczny | Priorytetyzacja defektów, akceptacja wyników testów przed wydaniem, decyzje o zakresie testów |

## 10. Procedury raportowania błędów

### 10.1 Kanał raportowania

- System zgłoszeń w repozytorium projektu
- Zakładanie zgłoszeń w sekcji Issues

### 10.2 Zawartość zgłoszenia defektu

Każdy błąd powinien zawierać co najmniej:

- Krótki tytuł opisujący problem
- Środowisko w którym wystąpił błąd:
  - wersja aplikacji
  - przeglądarka
  - system operacyjny
- Kroki do odtworzenia
  - Dokładna sekwencja działań od wejścia na stronę do wystąpienia błędu
- Oczekiwane zachowanie
- Rzeczywiste zachowanie
- Załączniki:
  - zrzuty ekranu
  - fragmenty logów Seriloga jeśli dostępne
- Klasyfikacja:
  - Priorytet oraz poziom ważności
  - komponent którego dotyczy błąd
  - rodzaj testu który go wykrył (unit, integration, E2E, test manualny)

### 10.3 Cykl życia defektu

- **New** – Defekt zgłoszony przez QA, dewelopera lub osobę testującą
- **Triaged** – Priorytet i ważność ustalone przez zespół lub product ownera
- **In progress** – Defekt przypisany do dewelopera i aktualnie naprawiany
- **Resolved** – Poprawka wdrożona w kodzie, testy jednostkowe oraz integracyjne przechodzą, QA otrzymuje informację o gotowości do retestu
- **Verified** – QA potwierdza, że błąd nie występuje w aktualnej wersji, a istniejące testy obejmują scenariusz powodujący błąd
- **Closed** – Defekt zamknięty

W przypadku regresji defekt może zostać ponownie otwarty.

### 10.4 Powiązanie defektów z testami

Każdy istotny defekt powinien skutkować:

- Dodaniem lub rozszerzeniem testu automatycznego w odpowiednim projekcie testowym
- Aktualizacją dokumentacji scenariuszy testowych jeśli wymaga tego zmiana zachowania aplikacji

W opisie pull requestu z poprawką należy wskazać identyfikator zgłoszenia oraz krótko opisać dodane testy automatyczne

---

Ten plan stanowi bazę do przygotowania szczegółowych przypadków testowych, implementacji testów automatycznych oraz konfiguracji pipeline CI dla projektu 10xCards.

4.1 Uwierzytelnianie i autoryzacja
Rejestracja
Poprawna rejestracja nowego użytkownika
Próba rejestracji z istniejącym adresem e mail
Walidacja haseł za krótkich i nie spełniających wymogów

Logowanie
Poprawne logowanie dla test 10xcards lokal
Poprawne logowanie dla admin 10xcards lokal
Niepoprawne hasło powoduje właściwy komunikat błędu

Autoryzacja
Wejście na Cards, Collections, Review, Generate bez zalogowania
Wejście na Admin Dashboard bez roli Admin
Sprawdzenie przekierowań i komunikatów

4.2 Generacja i przegląd propozycji
Walidacja długości tekstu w GenerateCards
Tekst krótszy niż 50 znaków
Tekst dłuższy niż limit
Poprawny tekst

Mockowa generacja
Sprawdzenie liczby wygenerowanych propozycji w zależności od liczby zdań
Sprawdzenie, że front oraz back spełniają ograniczenia długości po stronie serwisu

ReviewProposals
Załadowanie propozycji dla danego SourceTextId
Zaznaczenie pojedynczej i wielu propozycji
Edycja front oraz back i zapisanie zmian w edycji lokalnej
Akceptacja zaznaczonych propozycji i weryfikacja w bazie kart
Odrzucenie zaznaczonych propozycji i weryfikacja braku kart

4.3 Biblioteka kart
Lista kart
Wyświetlenie kart z filtrami All, DueToday, New
Sprawdzenie paginacji na granicy stron

Tworzenie karty manualnie
Walidacje długości front i back
Poprawne utworzenie karty oraz CardProgress
Pojawienie się karty na liście

Edycja karty
Edycja front i back z walidacją
Aktualizacja UpdatedUtc w bazie
Odtwarzanie danych karty w formularzu

Usuwanie karty
Potwierdzenie usunięcia w ConfirmModal
Usunięcie karty i powiązanego CardProgress

4.4 Spaced repetition
Pobieranie kart do powtórki
Brak kart z NextReviewUtc nieprzekroczonym
Istnieją karty z NextReviewUtc w przeszłości, zwrócone w prawidłowej kolejności

Sesja w ReviewSession
Przejście stanów Loading, ShowFront, ShowBack, Finished
Przypadki
Wszystkie odpowiedzi Again
Mieszanka Again, Good, Easy
Wszystkie odpowiedzi Easy
Sprawdzenie poprawności podsumowania sesji

Aktualizacja CardProgress
Zwiększanie ReviewCount
Ustawianie NextReviewUtc zgodnie z algorytmem
Utrwalenie zmian w bazie

4.5 Kolekcje kart
Tworzenie kolekcji
Walidacja nazwy i opisu
Próba użycia kart, które nie należą do użytkownika
Sprawdzenie, że kolekcja zawiera wybrane karty

Lista kolekcji
Paginacja i sortowanie po dacie aktualizacji
Informacje o liczbie kart i czy istnieje backup

Szczegóły kolekcji
Wyświetlenie kart należących do kolekcji
Linki do edycji kart

Edycja kolekcji
Zmiana nazwy i opisu
Utworzenie backupu i możliwość odtworzenia poprzedniej wersji
Dodawanie kart, pomijanie duplikatów
Usuwanie wybranych kart z kolekcji

Usuwanie kolekcji
Usunięcie kolekcji bez kasowania kart
Przekierowanie z CollectionDetail do listy kolekcji

4.6 Panel admina
Wyliczanie metryk
Sprawdzenie poprawności liczby kart ogółem, manualnych i AI
Sprawdzenie pola AiAcceptanceRate przy różnych proporcjach zaakceptowanych zdarzeń
Sprawdzenie percentyli P50, P75, P90 dla różnych rozkładów danych
Sprawdzenie liczby aktywnych użytkowników w ostatnich 7 i 30 dniach na podstawie CreatedUtc kart
Sprawdzenie liczby błędów generacji

Cache metryk
Dwa kolejne wywołania GetDashboardMetricsAsync w krótkim odstępie powinny korzystać z cache
Aktualizacja metryk po upłynięciu czasu cache

4.7 Obsługa błędów i logowanie
Middleware ExceptionLoggingMiddleware
Zasymulowanie wyjątku w jednej z tras i weryfikacja wpisu w logach
Sprawdzenie, że użytkownik widzi stronę Error

Strona Error
Wyświetlanie RequestId oraz komunikatu w trybie deweloperskim
Dostępne linki powrotne na stronę główną i do kart

5. Środowisko testowe
5.1 Konfiguracja środowiska lokalnego
System operacyjny
Windows lub Linux z zainstalowanym SDK .NET 9

Baza danych
SQLite plik cards.db w katalogu projektu
Skrypty setup.bat, reset db.bat do szybkiego przygotowania bazy

Config
appsettings.Development.json z ustawieniami logowania
ASPNETCORE ENVIRONMENT ustawiony na Development

Uruchamianie aplikacji
dotnet run project 10xCards
Lub z poziomu Visual Studio

5.2 Środowisko CI
GitHub Actions
Job budujący i uruchamiający dotnet test dla projektów unit i integration
Dodatkowy job uruchamiający testy Playwright po wystartowaniu aplikacji testowej
5.3 Dane testowe
Konta
test 10xcards lokal hasło Test123!
admin 10xcards lokal hasło Admin123!

Dane przykładowe
Kilka zestawów kart i kolekcji tworzonych skryptem seed lub w testach integracyjnych
Teksty wejściowe do generacji o różnej długości i strukturze

6. Narzędzia do testowania
xUnit
Testy jednostkowe i integracyjne

Moq
Mockowanie interfejsów i zależności w testach jednostkowych

Microsoft NET Test Sdk plus coverlet
Uruchamianie testów i zbieranie pokrycia

SQLite in memory lub na pliku tymczasowym
Dla testów integracyjnych

Playwright
Testy end to end w przeglądarce

GitHub Actions
CI zautomatyzowany dla testów

7. Harmonogram testów
Przykładowy iteracyjny harmonogram dla MVP

Etap przygotowania
Konfiguracja projektów testowych unit oraz integration
Konfiguracja podstawowego pipeline w GitHub Actions

Etap funkcji podstawowych
Testy jednostkowe i integracyjne dla CardService, SrService, ProposalService
Pierwsze scenariusze E2E dla rejestracji, logowania i tworzenia kart

Etap funkcji generacji i propozycji
Rozszerzenie testów ProposalService
Scenariusze E2E dla GenerateCards i ReviewProposals

Etap spaced repetition
Rozbudowa testów SrService
E2E dla sesji ReviewSession

Etap kolekcji kart
Testy jednostkowe i integracyjne CollectionService
E2E dla tworzenia i edycji kolekcji

Etap panelu admina oraz stabilizacji
Testy AdminService
E2E sprawdzające Dashboard
Testy regresyjne pełnego przepływu użytkownika

Etap przed wydaniem
Pełny przebieg pakietu testów
Naprawa krytycznych defektów
Ostateczne potwierdzenie kryteriów akceptacji

8. Kryteria akceptacji testów
Funkcjonalne
Wszystkie kluczowe przepływy E2E przechodzą bez błędów
Brak otwartych defektów o wysokiej ważności blokujących scenariusze z PRD
Wszystkie wymagania funkcjonalne z PRD oznaczone jako MVP są pokryte przynajmniej jednym testem automatycznym lub manualnym

Jakościowe
Co najmniej podstawowe pokrycie logiki serwisów aplikacyjnych i algorytmu SR testami jednostkowymi
Wszystkie projekty testowe przechodzą pozytywnie w CI

Bezpieczeństwo i autoryzacja
Brak scenariusza, w którym użytkownik bez uprawnień ma dostęp do zasobów innego użytkownika lub panelu admina
Logowanie i wylogowanie działają poprawnie

Stabilność
Brak systematycznych błędów nieobsłużonych wyjątków w logach przy podstawowych przepływach

9. Role i odpowiedzialności
Rola	Główne odpowiedzialności
Inżynier QA	Przygotowanie planu testów, projektowanie scenariuszy, automatyzacja testów E2E, koordynacja regresji, raportowanie defektów
Programista backend	Implementacja testów jednostkowych i integracyjnych, poprawa defektów, utrzymanie jakości kodu, przygotowanie danych testowych
Programista frontend Blazor	Testy komponentów, wsparcie dla testów E2E Playwright, poprawa defektów interfejsu i logiki klienta
DevOps	Konfiguracja pipeline CI, integracja testów w GitHub Actions, utrzymanie środowisk testowych
Product owner lub lider techniczny	Priorytetyzacja defektów, akceptacja wyników testów przed wydaniem, decyzje o zakresie testów
10. Procedury raportowania błędów
10.1 Kanał raportowania
System zgłoszeń w repozytorium projektu
Zakładanie zgłoszeń w sekcji Issues
10.2 Zawartość zgłoszenia defektu
Każdy błąd powinien zawierać co najmniej:

Krótki tytuł opisujący problem

Środowisko w którym wystąpił błąd
wersja aplikacji
przeglądarka
system operacyjny

Kroki do odtworzenia
Dokładna sekwencja działań od wejścia na stronę do wystąpienia błędu

Oczekiwane zachowanie

Rzeczywiste zachowanie

Załączniki
zrzuty ekranu
fragmenty logów Seriloga jeśli dostępne

Klasyfikacja
Priorytet oraz poziom ważności
komponent którego dotyczy błąd
rodzaj testu który go wykrył (unit, integration, E2E, test manualny)

10.3 Cykl życia defektu
New
Defekt zgłoszony przez QA, dewelopera lub osobę testującą

Triaged
Priorytet i ważność ustalone przez zespół lub product ownera

In progress
Defekt przypisany do dewelopera i aktualnie naprawiany

Resolved
Poprawka wdrożona w kodzie, testy jednostkowe oraz integracyjne przechodzą
QA otrzymuje informację o gotowości do retestu

Verified
QA potwierdza, że błąd nie występuje w aktualnej wersji, a istniejące testy obejmują scenariusz powodujący błąd

Closed
Defekt zamknięty

W przypadku regresji defekt może zostać ponownie otwarty.

10.4 Powiązanie defektów z testami
Każdy istotny defekt powinien skutkować
Dodaniem lub rozszerzeniem testu automatycznego w odpowiednim projekcie testowym
Aktualizacją dokumentacji scenariuszy testowych jeśli wymaga tego zmiana zachowania aplikacji

W opisie pull requestu z poprawką należy wskazać identyfikator zgłoszenia oraz krótko opisać dodane testy automatyczne

Ten plan stanowi bazę do przygotowania szczegółowych przypadków testowych, implementacji testów automatycznych oraz konfiguracji pipeline CI dla projektu 10xCards.