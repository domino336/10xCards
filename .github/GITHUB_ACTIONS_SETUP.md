# GitHub Actions - Quick Guide

## ?? Utworzone pliki

### Workflow
- ? `.github/workflows/pull-request.yml` - G³ówny workflow CI dla PR
- ? `.github/workflows/README.md` - Dokumentacja workflow

### Konfiguracja
- ? `codecov.yml` - Konfiguracja pokrycia kodu Codecov
- ? `.github/PULL_REQUEST_TEMPLATE.md` - Szablon checklist dla PR

### Lokalne skrypty (zaktualizowane)
- ? `10xCards.E2E.Tests/run-tests.bat` - Ulepszone uruchamianie testów E2E lokalnie

## ?? Workflow Pull Request

### Struktur a
```
BUILD ? [UNIT-TESTS + E2E-TESTS] ? STATUS-COMMENT
```

### Funkcje
1. **Build** - Kompilacja ca³ego rozwi¹zania
2. **Unit Tests** - Testy jednostkowe z coverage
3. **E2E Tests** - Testy end-to-end w przegl¹darce z coverage
4. **Status Comment** - Automatyczny komentarz w PR ze statusem

### Artefakty
- Build artifacts (1 dzieñ retencji)
- Unit test results (7 dni)
- E2E test results (7 dni)
- Playwright traces (7 dni, tylko przy b³êdach)

## ?? Setup

### 1. Uprawnienia GitHub
Settings ? Actions ? General ? Workflow permissions:
- ? Wybierz "Read and write permissions"

### 2. Codecov (Opcjonalnie)
Settings ? Secrets and variables ? Actions ? New secret:
- Name: `CODECOV_TOKEN`
- Value: [token z codecov.io]

Bez tokena workflow dzia³a, ale coverage nie jest wysy³any.

## ?? Coverage

### Unit Tests
- Flag: `unittests`
- Œcie¿ki: Application, Domain, Persistence

### E2E Tests
- Flag: `e2etests`
- Œcie¿ki: 10xCards, Application

## ?? U¿ywane Akcje (najnowsze wersje)

| Akcja | Wersja | Status |
|-------|--------|--------|
| actions/checkout | v6 | ? Active |
| actions/setup-dotnet | v5 | ? Active |
| actions/upload-artifact | v4 | ? Active |
| actions/download-artifact | v4 | ? Active |
| codecov/codecov-action | v5 | ? Active |
| actions/github-script | v7 | ? Active |

Wszystkie akcje zweryfikowane jako nieusuniête (not deprecated/archived).

## ?? Nastêpne kroki

1. ? Workflow jest gotowy do u¿ycia
2. ?? Dodaj secret CODECOV_TOKEN jeœli chcesz coverage reports
3. ?? Stwórz testowy PR aby sprawdziæ workflow
4. ?? Monitoruj rezultaty w zak³adce Actions

## ?? Tips

- Workflow ignoruje zmiany w plikach `*.md` (oprócz workflow files)
- E2E testy uruchamiaj¹ aplikacjê automatycznie
- Ka¿dy job zapisuje logi i artefakty
- Status comment pojawia siê zawsze (success lub failure)

---

? **Wszystko gotowe do u¿ycia!**
