# Pull Request Checklist

Przed zatwierdzeniem PR upewnij siê, ¿e:

## Testy
- [ ] Wszystkie testy jednostkowe przechodz¹ (`dotnet test 10xCards.Unit.Tests`)
- [ ] Wszystkie testy E2E przechodz¹ (`dotnet test 10xCards.E2E.Tests`)
- [ ] Dodano nowe testy dla nowych funkcjonalnoœci
- [ ] Pokrycie kodu nie spad³o poni¿ej progu

## Build
- [ ] Projekt kompiluje siê bez ostrze¿eñ (`dotnet build`)
- [ ] Brak b³êdów w build na CI/CD

## Code Quality
- [ ] Kod jest zgodny z konwencjami projektu
- [ ] Zmienne i metody maj¹ sensowne nazwy
- [ ] Usuniêto nieu¿ywany kod
- [ ] Brak zduplikowanej logiki

## Dokumentacja
- [ ] Zaktualizowano README.md jeœli potrzeba
- [ ] Dodano komentarze do skomplikowanej logiki
- [ ] Zaktualizowano dokumentacjê API jeœli potrzeba

## Bezpieczeñstwo
- [ ] Brak wra¿liwych danych w kodzie (has³a, tokeny, klucze)
- [ ] Walidacja danych wejœciowych
- [ ] Sprawdzono uprawnienia u¿ytkowników

## GitHub Actions
- [ ] Workflow przeszed³ pomyœlnie
- [ ] Status comment zosta³ dodany do PR
- [ ] Artefakty zosta³y wygenerowane

## Baza danych
- [ ] Migracje zosta³y dodane i przetestowane
- [ ] Baza danych dzia³a lokalnie po zmianach

## Ready for Review
- [ ] Branch jest aktualny z main
- [ ] Konflikt zosta³y rozwi¹zane
- [ ] PR ma sensowny tytu³ i opis
- [ ] Przypisano reviewerów

---

## Dla Reviewera

Podczas review sprawdŸ:
- [ ] Czy zmiany s¹ zgodne z celem PR
- [ ] Czy testy s¹ wystarczaj¹ce
- [ ] Czy kod jest czytelny i zrozumia³y
- [ ] Czy nie wprowadzono regresji
- [ ] Czy dokumentacja jest aktualna
