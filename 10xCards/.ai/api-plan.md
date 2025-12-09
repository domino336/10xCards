# Blazor Feature Implementation Plan Generator

Jesteś doświadczonym architektem aplikacji Blazor Server, którego zadaniem jest stworzenie kompleksowego planu komponentów i serwisów aplikacyjnych dla pojedynczej funkcjonalności (feature). Twój plan będzie oparty na podanym schemacie bazy danych, dokumencie wymagań produktu (PRD), stacku technologicznym i instrukcjach kodowania.

---

## Dane wejściowe

Przed rozpoczęciem pracy, zapoznaj się z:

1. **Specyfikacja funkcji**:
<feature_specification>
{{feature-specification}} <- opis konkretnej funkcji z PRD (np. "Przeglądanie kart do powtórki", "Akceptacja propozycji karty")
</feature_specification>

2. **Powiązane zasoby bazy danych**:
<related_db_resources>
{{db-resources}} <- przekopiuj tabele i relacje z @db-plan.md
</related_db_resources>

3. **Definicje typów**:
<type_definitions>
{{types}} <- zamień na referencję do definicji typów (Domain entities, DTOs)
</type_definitions>

4. **Stack technologiczny**:
<tech_stack>
{{tech-stack}} <- zamień na referencję do @tech-stack.md
</tech_stack>

5. **Reguły implementacji**:
<implementation_rules>
{{copilot-instructions}} <- zamień na referencję do @copilot-instructions.md
</implementation_rules>

---

## Proces analizy

Przed dostarczeniem ostatecznego planu, pracuj wewnątrz tagów `<blazor_analysis>` w swoim bloku myślenia, aby rozbić swój proces myślowy. W tej sekcji **musisz**:

1. **Przeanalizuj schemat bazy danych**:
   - Zidentyfikuj główne encje (tabele) związane z funkcją
   - Zanotuj relacje między encjami
   - Rozważ indeksy wpływające na wydajność zapytań
   - Wypisz warunki walidacji określone w schemacie

2. **Przeanalizuj specyfikację funkcji (PRD)**:
   - Zidentyfikuj kluczowe interakcje użytkownika
   - Określ operacje na danych (pobieranie, tworzenie, aktualizacja, usuwanie)
   - Wypisz logikę biznesową wykraczającą poza proste CRUD
   - Zaplanuj przepływ nawigacji użytkownika

3. **Rozważ stack technologiczny i reguły kodowania**:
   - Upewnij się, że plan jest zgodny z Blazor Server + .NET 9
   - Zastosuj wzorzec: **Domain → Application Services → Blazor Components**
   - **Bez MediatR** – bezpośrednie wstrzykiwanie serwisów (`[Inject]`)
   - **Bootstrap** do stylowania (nie Tailwind)
   - **Code-behind** (`*.razor.cs`) gdy logika komponentu > ~40 linii

4. **Mapowanie funkcji na komponenty i serwisy**:
   - Dla każdego use-case:
     - Jaki komponent Blazor (strona/komponent współdzielony) obsłuży UI?
     - Jaki serwis aplikacyjny dostarczy logikę?
     - Czy potrzebny jest code-behind?
     - Jakie DTO są wymagane?

5. **Zaplanuj walidację i obsługę błędów**:
   - Walidacja front-end: `EditForm` + `DataAnnotations` + `ValidationSummary`
   - Walidacja back-end: serwisy aplikacyjne
   - Komunikaty użytkownika (Bootstrap alerts)

6. **Rozważ bezpieczeństwo i wydajność**:
   - Uwierzytelnianie/autoryzacja (`@attribute [Authorize]`, ASP.NET Core Identity)
   - Optymalizacja zapytań EF Core (`AsNoTracking`, `Include`)
   - Unikanie N+1 queries

Końcowe wyniki powinny składać się wyłącznie z planu wdrożenia w formacie markdown i nie powinny powielać ani powtarzać żadnej pracy wykonanej w sekcji analizy.

Pamiętaj, aby zapisać swój plan wdrożenia jako .ai/view-implementation-plan.md. Upewnij się, że plan jest szczegółowy, przejrzysty i zapewnia kompleksowe wskazówki dla zespołu programist