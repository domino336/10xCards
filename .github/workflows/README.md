# GitHub Actions Workflows

## Pull Request Workflow (`pull-request.yml`)

### Przegl¹d

Workflow uruchamiany automatycznie przy ka¿dym pull requeœcie do ga³êzi `main`. Przeprowadza pe³n¹ walidacjê kodu poprzez build, testy jednostkowe i E2E.

### Struktura

```
???????????????
?    BUILD    ?
???????????????
       ?
       ???????????????????????????????
       ?              ?              ?
       ?              ?              ?
??????????????? ???????????????    ?
? UNIT TESTS  ? ?  E2E TESTS  ?    ?
??????????????? ???????????????    ?
       ?              ?              ?
       ???????????????????????????????
                      ?
                      ?
              ???????????????
              ?   STATUS    ?
              ?  COMMENT    ?
              ???????????????
```

### Jobs

#### 1. **build** - Budowanie rozwi¹zania
- Checkout kodu
- Setup .NET 9
- Przywrócenie zale¿noœci
- Build w konfiguracji Release
- Upload artefaktów build

#### 2. **unit-tests** - Testy jednostkowe (równolegle)
- Wykorzystuje artefakty z build
- Uruchamia testy z projektu `10xCards.Unit.Tests`
- Zbiera pokrycie kodu (Code Coverage)
- Wysy³a pokrycie do Codecov
- Upload wyników testów jako artefakty

#### 3. **e2e-tests** - Testy end-to-end (równolegle)
- Wykorzystuje artefakty z build
- Instaluje przegl¹darki Playwright (chromium)
- Przygotowuje bazê SQLite
- Uruchamia aplikacjê w tle na `http://localhost:5077`
- Wykonuje testy E2E z projektu `10xCards.E2E.Tests`
- Zbiera pokrycie kodu
- Wysy³a pokrycie do Codecov
- Zatrzymuje aplikacjê
- Upload wyników testów i trace'ów Playwright

#### 4. **status-comment** - Komentarz statusu
- Uruchamia siê tylko gdy wszystkie poprzednie joby siê zakoñcz¹
- Pobiera wyniki testów
- Generuje podsumowanie w formie tabeli
- Publikuje komentarz w PR

### U¿ywane Akcje

| Akcja | Wersja | Zastosowanie |
|-------|--------|--------------|
| `actions/checkout` | v6 | Pobranie kodu |
| `actions/setup-dotnet` | v5 | Instalacja .NET SDK |
| `actions/upload-artifact` | v4 | Upload artefaktów |
| `actions/download-artifact` | v4 | Download artefaktów |
| `codecov/codecov-action` | v5 | Przesy³anie pokrycia |
| `actions/github-script` | v7 | Tworzenie komentarzy PR |

Wszystkie akcje s¹ w najnowszych wersjach i nie s¹ deprecated/archived.

### Wymagane Secrety (Opcjonalne)

- `CODECOV_TOKEN` - Token do przesy³ania pokrycia kodu do Codecov
  - Jeœli nie jest ustawiony, upload nie zakoñczy siê b³êdem

### Artefakty

| Nazwa | Retencja |
|-------|----------|
| `build-artifacts` | 1 dzieñ |
| `unit-test-results` | 7 dni |
| `e2e-test-results` | 7 dni |
| `playwright-traces` | 7 dni (tylko b³êdy) |

## Troubleshooting

### E2E testy nie mog¹ po³¹czyæ siê z aplikacj¹
Zwiêksz czas oczekiwania z 15 do 20 sekund w kroku startowania aplikacji.

### Playwright nie znajduje przegl¹darek
SprawdŸ flagê `--with-deps` w instalacji.

### Brak uprawnieñ do komentarzy
Settings ? Actions ? General ? Workflow permissions ? "Read and write permissions"
