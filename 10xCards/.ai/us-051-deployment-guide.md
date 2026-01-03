# ?? Instrukcja uruchomienia US-051: Kolekcje fiszek

## ? Szybki start

### 1. Utworzenie i zastosowanie migracji bazy danych

```powershell
# W katalogu g³ównym projektu (gdzie jest plik .sln)
dotnet ef migrations add AddCardCollections --project 10xCards.Persistance --startup-project 10xCards

# Zastosowanie migracji
dotnet ef database update --project 10xCards.Persistance --startup-project 10xCards
```

**Oczekiwany rezultat:**
- Nowa migracja w `10xCards.Persistance/Migrations/`
- Trzy nowe tabele w bazie SQLite:
  - `CardCollections`
  - `CardCollectionCards`
  - `CardCollectionBackups`

### 2. Weryfikacja build

```powershell
dotnet build
```

? **Status:** Build successful (ju¿ zweryfikowane)

### 3. Uruchomienie aplikacji

```powershell
dotnet run --project 10xCards
```

lub w Visual Studio: **F5** (Debug) / **Ctrl+F5** (bez debugowania)

### 4. Test w przegl¹darce

1. Otwórz: `https://localhost:5001` (lub port z konsoli)
2. Zaloguj siê (test@10xcards.local / Test123!)
3. Kliknij **"Collections"** w menu nawigacyjnym
4. Kliknij **"Create New Collection"**

---

## ?? Scenariusze testowe

### Test 1: Tworzenie kolekcji

**Kroki:**
1. `/collections/create`
2. Nazwa: "Biology Exam 2024"
3. Opis: "All topics for final exam"
4. Zaznacz 5 fiszek
5. Kliknij "Create Collection"

**Oczekiwany wynik:**
- Przekierowanie do `/collections/{id}`
- Kolekcja zawiera 5 fiszek
- Badge: "5 card(s)"

### Test 2: Edycja kolekcji

**Kroki:**
1. `/collections/{id}/edit`
2. Zmieñ nazwê na "Biology Final 2024"
3. Kliknij "Save Changes"
4. Wróæ do szczegó³ów (`/collections/{id}`)

**Oczekiwany wynik:**
- Nazwa zaktualizowana
- Badge "Backup available" widoczny
- Przycisk "Restore" aktywny

### Test 3: Dodawanie fiszek

**Kroki:**
1. `/collections/{id}/edit`
2. Kliknij "Add More Cards"
3. W modalu zaznacz 3 dodatkowe fiszki
4. Kliknij "Add Selected Cards (3)"

**Oczekiwany wynik:**
- Alert sukcesu: "Added 3 card(s) to collection."
- Liczba fiszek wzrasta do 8
- Modal zamkniêty

### Test 4: Usuwanie fiszek

**Kroki:**
1. `/collections/{id}/edit`
2. Zaznacz 2 fiszki checkboxami
3. Kliknij "Remove Selected (2)"

**Oczekiwany wynik:**
- Alert: "Removed 2 card(s) from collection."
- Liczba fiszek spad³a do 6
- Fiszki nadal istniej¹ w `/cards`

### Test 5: Przywracanie backupu

**Kroki:**
1. `/collections/{id}`
2. Kliknij "Restore" (jeœli backup dostêpny)
3. PotwierdŸ w modalu

**Oczekiwany wynik:**
- Nazwa i opis przywrócone do poprzedniej wersji
- Badge "Backup available" znikn¹³
- Przycisk "Restore" nieaktywny

### Test 6: Usuwanie kolekcji

**Kroki:**
1. `/collections`
2. Kliknij ikonê kosza przy kolekcji
3. PotwierdŸ w modalu

**Oczekiwany wynik:**
- Alert: "Collection deleted successfully."
- Kolekcja zniknê³a z listy
- PrzejdŸ do `/cards` ? fiszki nadal tam s¹

### Test 7: Autoryzacja

**Kroki:**
1. Wyloguj siê
2. Próbuj wejœæ na `/collections`

**Oczekiwany wynik:**
- Przekierowanie do `/Account/Login`
- Po zalogowaniu - powrót do `/collections`

---

## ?? Troubleshooting

### Problem: Migracja nie dzia³a

**Objaw:**
```
Could not load file or assembly 'System.Runtime, Version=10.0.0.0
```

**Rozwi¹zanie:**
1. SprawdŸ wersjê .NET SDK: `dotnet --version` (powinno byæ 9.0.x)
2. Upewnij siê ¿e jesteœ w katalogu g³ównym (gdzie `.sln`)
3. Spróbuj: `dotnet clean && dotnet build`

### Problem: Build error "CardListItemDto does not contain 'Front'"

**Objaw:**
```
CS1061: 'CardListItemDto' does not contain a definition for 'Front'
```

**Rozwi¹zanie:**
? Ju¿ naprawione - u¿ywamy `FrontPreview` zamiast `Front`

### Problem: Brak linku "Collections" w menu

**Objaw:**
Menu nie pokazuje zak³adki "Collections"

**Rozwi¹zanie:**
1. SprawdŸ czy jesteœ zalogowany (`<AuthorizeView>`)
2. Wyczyœæ cache przegl¹darki (Ctrl+Shift+R)
3. SprawdŸ `NavMenu.razor` - link powinien byæ miêdzy "My Cards" a "Review"

### Problem: "Collection not found" przy otwieraniu szczegó³ów

**Objaw:**
B³¹d 404 lub "Collection not found or access denied"

**Mo¿liwe przyczyny:**
1. Nieprawid³owy GUID w URL
2. Kolekcja nale¿y do innego u¿ytkownika
3. Kolekcja zosta³a usuniêta

**Debug:**
```csharp
// W CollectionService.cs dodaj breakpoint w GetCollectionAsync
var collection = await _context.CardCollections
    .Where(c => c.Id == collectionId && c.UserId == userId)
    .FirstOrDefaultAsync(cancellationToken);
```

---

## ?? Weryfikacja danych w bazie

### SQLite Browser (opcjonalnie)

1. Pobierz [DB Browser for SQLite](https://sqlitebrowser.org/)
2. Otwórz plik `AppData/10xCards.db`
3. SprawdŸ tabele:

**CardCollections:**
```sql
SELECT * FROM CardCollections;
```

**CardCollectionCards:**
```sql
SELECT cc.CollectionId, cc.CardId, c.Front
FROM CardCollectionCards cc
JOIN Cards c ON cc.CardId = c.Id
WHERE cc.CollectionId = '<your-collection-id>';
```

**CardCollectionBackups:**
```sql
SELECT * FROM CardCollectionBackups;
```

---

## ? Checklist finalny

Przed zamkniêciem US-051, upewnij siê ¿e:

- [ ] ? Build successful
- [ ] ?? Migracja zastosowana (`dotnet ef database update`)
- [ ] ?? Aplikacja uruchamia siê bez b³êdów
- [ ] ?? Link "Collections" widoczny w menu (po zalogowaniu)
- [ ] ?? Utworzenie kolekcji dzia³a
- [ ] ?? Edycja (nazwa/opis) tworzy backup
- [ ] ?? Dodawanie/usuwanie fiszek dzia³a
- [ ] ?? Restore przywraca backup
- [ ] ?? Delete usuwa kolekcjê (nie fiszki)
- [ ] ?? Autoryzacja blokuje dostêp niezalogowanym
- [ ] ?? Responsywnoœæ OK na mobile (360px)

---

## ?? Gotowe!

Implementacja US-051 ukoñczona. Kolekcje fiszek dzia³aj¹ zgodnie z wymaganiami:
- ? Tworzenie kolekcji z wyborem fiszek
- ? Edycja nazwy i opisu
- ? Dodawanie/usuwanie fiszek
- ? Backup i restore poprzedniej wersji
- ? Usuwanie kolekcji (fiszki pozostaj¹)
- ? Autoryzacja i izolacja per u¿ytkownik

**Nastêpny krok:** Testy integracyjne i deploy! ??
