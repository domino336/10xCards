<db-plan>
{{db-plan}} <- zamień na referencję do @db-plan.md
</db-plan>

<prd>
{{prd}} <- zamień na referencję do @prd.md
</prd>

<tech-stack>
{{tech-stack}} <- zamień na referencję do @tech-stack.md
</tech-stack>

<copilot-instructions>
{{copilot-instructions}} <- zamień na referencję do @copilot-instructions.md
</copilot-instructions>

Jesteś doświadczonym architektem aplikacji Blazor Server, którego zadaniem jest stworzenie kompleksowego planu komponentów i serwisów aplikacyjnych. Twój plan będzie oparty na podanym schemacie bazy danych, dokumencie wymagań produktu (PRD), stacku technologicznym i instrukcjach kodowania podanych powyżej. Uważnie przejrzyj dane wejściowe i wykonaj następujące kroki:

1. Przeanalizuj schemat bazy danych:
   - Zidentyfikuj główne encje (tabele)
   - Zanotuj relacje między jednostkami
   - Rozważ wszelkie indeksy, które mogą mieć wpływ na projekt serwisów
   - Zwróć uwagę na warunki walidacji określone w schemacie

2. Przeanalizuj PRD:
   - Zidentyfikuj kluczowe cechy i funkcjonalności
   - Zwróć uwagę na konkretne wymagania dotyczące operacji na danych (pobieranie, tworzenie, aktualizacja, usuwanie)
   - Zidentyfikuj wymagania logiki biznesowej, które wykraczają poza operacje CRUD
   - Określ interakcje użytkownika wymagające komponentów Blazor

3. Rozważ stack technologiczny i instrukcje kodowania:
   - Upewnij się, że plan jest zgodny z Blazor Server + .NET 9
   - Zastosuj wzorce: Domain → Application Services → Blazor Components
   - Bez MediatR – bezpośrednie wstrzykiwanie serwisów
   - Bootstrap do stylowania (nie Tailwind)
   - Code-behind (*.razor.cs) gdy logika komponentu > ~40 linii

4. Tworzenie kompleksowego planu komponentów i serwisów:
   - Zdefiniuj główne komponenty Blazor (strony i komponenty współdzielone)
   - Zaprojektuj interfejsy serwisów aplikacyjnych (ICardService, IProposalService, etc.)
   - Określ metody serwisowe dla każdego use-case z PRD
   - Zdefiniuj struktury DTO dla przekazywania danych między warstwami
   - Uwzględnij walidację (DataAnnotations), obsługę błędów i stan UI
   - Zaplanuj nawigację i routing (@page)
   - Rozważ mechanizmy uwierzytelniania i autoryzacji (ASP.NET Core Identity)

Przed dostarczeniem ostatecznego planu, pracuj wewnątrz tagów <blazor_analysis> w swoim bloku myślenia, aby rozbić swój proces myślowy i upewnić się, że uwzględniłeś wszystkie niezbędne aspekty. W tej sekcji:

1. Wymień główne encje ze schematu bazy danych. Ponumeruj każdą encję i zacytuj odpowiednią część schematu.
2. Wymień kluczowe funkcje logiki biznesowej z PRD. Ponumeruj każdą funkcję i zacytuj odpowiednią część PRD.
3. Zmapuj funkcje z PRD do komponentów Blazor i serwisów. Dla każdej funkcji rozważ:
   - Jaki komponent (strona/komponent) obsłuży UI?
   - Jaki serwis aplikacyjny dostarczy logikę?
   - Czy potrzebny jest code-behind (*.razor.cs)?
4. Rozważ i wymień wszelkie wymagania dotyczące bezpieczeństwa i wydajności. Dla każdego wymagania zacytuj część dokumentów wejściowych.
5. Wyraźnie mapuj logikę biznesową z PRD na metody serwisów i komponenty.
6. Uwzględnij warunki walidacji ze schematu bazy danych w planie komponentów (EditForm + ValidationSummary).
7. Określ wzorce code-behind: kiedy używać *.razor.cs vs @code w komponencie.

Ta sekcja może być dość długa.

Ostateczny plan powinien być sformatowany w markdown i zawierać następujące sekcje:

Blazor Components & Services Plan
1. Przegląd architektury
•	Krótki opis przepływu: UI (Blazor) → Application Services → Domain → Persistence
•	Wzorzec wstrzykiwania zależności (DI)
2. Serwisy aplikacyjne (10xCards.Application)
Dla każdego serwisu podaj:
•	Nazwa interfejsu (np. ICardService)
•	Odpowiedzialność (use-cases)
•	Lista metod:
•	Sygnatura metody (async, parametry, typ zwracany)
•	Krótki opis
•	Przykład DTO zwracanego/akceptowanego
•	Logika biznesowa (mapowanie do domeny)
3. Komponenty Blazor (10xCards/Components)
Dla każdego komponentu podaj:
•	Nazwa komponentu (np. CardList.razor)
•	Routing (@page "/cards")
•	Rendermode (@rendermode InteractiveServer)
•	Odpowiedzialność (co wyświetla/robi)
•	Wstrzykiwane serwisy ([Inject])
•	Struktura markup (uproszczona, Bootstrap)
•	Czy wymaga code-behind (*.razor.cs)? Dlaczego?
•	Parametry komponentu ([Parameter])
•	Obsługa zdarzeń (@onclick, EditForm)
•	Walidacja (DataAnnotations + ValidationSummary)
4. DTOs (Data Transfer Objects)
•	Lista DTO dla każdego serwisu
•	Pola, typy, walidacja (atrybuty DataAnnotations)
5. Nawigacja i routing
•	Mapa ścieżek URL → komponenty
•	Struktura NavMenu
•	Parametry routingu (np. /cards/{id:guid})
6. Uwierzytelnianie i autoryzacja
•	Mechanizm (ASP.NET Core Identity)
•	Komponenty wymagające autoryzacji (@attribute [Authorize])
•	Role (jeśli dotyczy)
7. Walidacja i logika biznesowa
•	Warunki walidacji dla każdego formularza (z schematu DB)
•	Mapowanie walidacji: DTO → Domain
•	Obsługa błędów w komponentach (try/catch, komunikaty użytkownika)
8. Rejestracja DI (Program.cs)
•	Lista serwisów do zarejestrowania (AddScoped, AddSingleton)
•	Kolejność rejestracji
9. Wzorce code-behind
•	Kiedy używać *.razor.cs?
•	Logika > ~40 linii
•	Wiele wstrzykiwanych serwisów
•	Skomplikowana logika cyklu życia (OnInitializedAsync, OnParametersSetAsync)
•	Przykład struktury partial class
10. Checklist implementacji
•	Kolejność kroków do zaimplementowania każdej funkcji z PRD
•	Mapowanie: Feature → Domain → Service → Component → Test

Upewnij się, że Twój plan jest:
- Kompleksowy i odnosi się do wszystkich aspektów materiałów wejściowych
- Zgodny z instrukcjami kodowania (no MediatR, Bootstrap, code-behind pattern)
- Zawiera przykłady kodu C# dla kluczowych elementów
- Określa wyraźnie file-scoped namespaces i sealed classes gdzie stosowne

Jeśli musisz przyjąć jakieś założenia z powodu niejasnych informacji wejściowych, określ je wyraźnie w swojej analizie.

Końcowy wynik powinien składać się wyłącznie z planu komponentów i serwisów w formacie markdown w języku polskim, który zapiszesz w .ai/blazor-plan.md i nie powinien powielać ani powtarzać żadnej pracy wykonanej w bloku myślenia <blazor_analysis>.
