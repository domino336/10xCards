Twoim zadaniem jest wdro¿enie komponentów Blazor Server w oparciu o podany plan wdro¿enia. Twoim celem jest stworzenie solidnej i dobrze zorganizowanej implementacji, która zawiera odpowiedni¹ walidacjê, obs³ugê b³êdów i pod¹¿a za wszystkimi logicznymi krokami opisanymi w planie.

Najpierw dok³adnie przejrzyj dostarczony plan wdro¿enia:

<implementation_plan>
{{blazor-implementation-plan}} <- dodaj referencjê do planu implementacji (np. @view-implementation-plan.md)
</implementation_plan>

<types>
{{types}} <- dodaj referencje do definicji typów domenowych i DTOs (np. @10xCards.Domain, @DTOs)
</types>

<implementation_rules>
{{blazor-rules}} <- dodaj referencje do regu³ frontendu (np. @.editorconfig, @blazor-conventions.md)
</implementation_rules>

<implementation_approach>
Realizuj maksymalnie 3 kroki planu implementacji, podsumuj krótko co zrobi³eœ i opisz plan na 3 kolejne dzia³ania - zatrzymaj w tym momencie pracê i czekaj na mój feedback.
</implementation_approach>

Teraz wykonaj nastêpuj¹ce kroki, aby zaimplementowaæ komponenty Blazor:

1. Przeanalizuj plan wdro¿enia:
   - Okreœl typ komponentu (Page, Shared, Layout)
   - Zidentyfikuj routy i parametry dla komponentów Page
   - Okreœl wymagane serwisy i zale¿noœci do wstrzykniêcia
   - Zrozum wymagany stan komponentu i cykl ¿ycia
   - Zidentyfikuj modele danych (DTOs, requests, responses)
   - Zwróæ uwagê na wymagania dotycz¹ce autoryzacji i ról
   - Okreœl wzorce interakcji u¿ytkownika (formularze, modalne, nawigacja)

2. Rozpocznij implementacjê:
   **Dla komponentów Razor (.razor):**
   - Zdefiniuj dyrektywê `@page` z odpowiednim routem (jeœli Page)
   - Dodaj atrybuty autoryzacji (`@attribute [Authorize]`, `@attribute [Authorize(Roles = "Admin")]`)
   - Wstrzyknij wymagane serwisy (`@inject IServiceName ServiceName`)
   - Zaimplementuj markup Razor z Bootstrap 5 (grid, utilities, komponenty)
   - U¿yj `EditForm` z `DataAnnotationsValidator` dla walidacji formularzy
   - Obs³u¿ stany loading, error, empty z odpowiednimi komunikatami
   - Zapewnij responsywnoœæ i accessibility (aria-label, role)

   **Dla code-behind (.razor.cs):**
   - Utwórz klasê `partial` odpowiadaj¹c¹ nazwie komponentu
   - Zdefiniuj parametry komponentu (`[Parameter]`)
   - Zaimplementuj lifecycle hooks (`OnInitializedAsync`, `OnParametersSet`)
   - Dodaj pola stanu komponentu (private)
   - Implementuj event handlers (async Task dla operacji asynchronicznych)
   - Obs³u¿ Result pattern dla odpowiedzi z serwisów
   - Zarz¹dzaj nawigacj¹ (`NavigationManager.NavigateTo`)

3. Walidacja i obs³uga b³êdów:
   - U¿yj Data Annotations w modelach dla walidacji po stronie klienta
   - Wyœwietlaj `ValidationSummary` i `ValidationMessage` w formularzach
   - Obs³uguj b³êdy z Result pattern (sprawdzaj `IsSuccess`)
   - Pokazuj komunikaty b³êdów w alertach Bootstrap (`alert-danger`)
   - Dodaj przyciski retry dla operacji, które mog¹ zawieœæ
   - Zablokuj interfejs podczas operacji asynchronicznych (disabled buttons, spinners)
   - Waliduj autoryzacjê (sprawdzaj UserId z Claims)

4. State Management i UX:
   - Zarz¹dzaj stanem ³adowania (`isLoading`, `isGenerating`)
   - Implementuj optymistyczne UI updates gdzie mo¿liwe
   - Dodaj feedback wizualny (spinners, progress indicators)
   - Wykorzystaj toast notifications dla powiadomieñ sukcesu/b³êdu
   - Obs³u¿ paginacjê z odpowiednim stanem (currentPage, totalPages)
   - Zarz¹dzaj selection state (HashSet<Guid> dla multi-select)
   - Implementuj modal confirmations dla destructive actions

5. Rozwa¿ania dotycz¹ce testowania:
   - SprawdŸ edge cases (empty lists, brak due cards, timeout)
   - Przetestuj flow autoryzacji (unauthorized access)
   - Zweryfikuj walidacjê formularzy (min/max length, required fields)
   - Testuj responsywnoœæ na ró¿nych rozdzielczoœciach (min 360px)
   - SprawdŸ accessibility (keyboard navigation, screen readers)

6. Dokumentacja i best practices:
   - Dodaj komentarze XML dla parametrów komponentów
   - U¿yj znacz¹cych nazw dla event callbacks
   - Separuj concerns (presentation w .razor, logic w .razor.cs)
   - Stosuj siê do konwencji nazewnictwa (.editorconfig)
   - Wykorzystuj reusable components dla powtarzalnych elementów
   - Zachowaj spójnoœæ z Bootstrap theme i design system

Po zakoñczeniu implementacji upewnij siê, ¿e:
- Komponent zawiera wszystkie wymagane dyrektywy i atrybuty
- Dependency injection jest poprawnie skonfigurowane
- Markup jest semantyczny i accessible
- Walidacja dzia³a po stronie klienta i serwera
- Obs³uga b³êdów pokrywa wszystkie scenariusze
- Stan komponentu jest zarz¹dzany efektywnie
- UX jest intuicyjny z odpowiednim feedback

Jeœli musisz przyj¹æ jakieœ za³o¿enia lub masz jakiekolwiek pytania dotycz¹ce planu implementacji, przedstaw je przed pisaniem kodu.

Pamiêtaj, aby przestrzegaæ:
- Wzorców Blazor Server (Server-side rendering, SignalR)
- Konwencji .NET 9 i C# 12
- Bootstrap 5 utilities i komponentów
- Accessibility guidelines (WCAG)
- Zasad clean code i separation of concerns
