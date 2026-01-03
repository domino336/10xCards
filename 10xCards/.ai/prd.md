# Dokument wymagań produktu (PRD) - 10xCards

## 1. Przegląd produktu
Aplikacja webowa 10xCards służy do szybkiego tworzenia i nauki z wykorzystaniem fiszek (Q&A) wspieranych algorytmem powtórek (spaced repetition). Rozwiązuje problem czasochłonnego przygotowywania wysokiej jakości materiałów do nauki poprzez automatyczne generowanie propozycji fiszek z wklejonych notatek użytkownika. Użytkownik może akceptować, odrzucać lub edytować propozycje przed zapisaniem. Produkt w wersji MVP skupia się na prostym, skutecznym przepływie: generacja → selekcja → nauka powtórkowa. Docelowi użytkownicy: studenci, profesjonaliści uczący się do certyfikacji, osoby przygotowujące się do egzaminów. Główna wartość: oszczędność czasu i zwiększona adopcja metody aktywnej nauki.

## 2. Problem użytkownika
Manualne tworzenie fiszek jest czasochłonne i nużące, co osłabia motywację do regularnego stosowania technik spaced repetition. Brak szybkiego sposobu konwersji dużych fragmentów notatek w struktury Q&A powoduje niską adopcję narzędzi opartych na fiszkach. Użytkownicy chcą:
- Minimalizować czas od zebrania materiału do posiadania gotowego zestawu fiszek.
- Zachować kontrolę nad jakością (możliwość edycji, odrzucenia).
- Mieć prosty wgląd w harmonogram powtórek bez konieczności konfiguracji.
- Unikać przeładowania funkcjami nieistotnymi na początku (importy, współdzielenie).
Problemem admina jest potrzeba obserwacji adopcji i jakości generacji (współczynnik akceptacji) aby podejmować decyzje rozwojowe.

## 3. Wymagania funkcjonalne
F1. Wklejenie źródłowego tekstu i uruchomienie generacji zestawu propozycji fiszek Q&A (segmentacja po nagłówkach lub akapitach).
F2. Prezentacja listy wygenerowanych propozycji z możliwością edycji pytania i odpowiedzi przed akceptacją.
F3. Akceptacja pojedyncza (on-card) oraz masowa (bulk select → accept) wygenerowanych fiszek.
F4. Odrzucenie pojedynczej lub masowe propozycji (fiszek nie zapisujemy do biblioteki użytkownika).
F5. Manualne tworzenie pojedynczej fiszki poprzez formularz (pytanie, odpowiedź) z walidacją długości 50–500 znaków na każdą stronę.
F6. Przechowywanie zaakceptowanych fiszek w koncie użytkownika wraz ze statusem i metadanymi (timestamps, generationMethod, acceptStatus).
F7. Wyświetlanie listy fiszek użytkownika z filtrami podstawowymi (status: do powtórki / oczekuje / nowa) oraz informacją o dacie kolejnej powtórki.
F8. Uruchomienie sesji powtórek: prezentacja pytania, odsłonięcie odpowiedzi, rejestracja wyniku (np. ocena Good / Again) i aktualizacja parametrów w CardProgress.
F9. Integracja z gotowym, zewnętrznym lub open-source algorytmem powtórek (biblioteka) – aktualizacja daty kolejnej powtórki i ewentualnych parametrów trudności.
F10. System kont użytkowników: rejestracja, logowanie, wylogowanie, reset hasła (email) – minimalny model auth.
F11. Autoryzacja dostępu: fiszki i dane powtórek izolowane per użytkownik, admin ma dostęp do zagregowanych metryk.
F12. Panel admina: agregaty (liczba fiszek całkowita, liczba aktywnych użytkowników w ostatnich 7/30 dniach, współczynnik akceptacji AI globalnie i per percentyle, udział AI vs manual).
F13. Logowanie zdarzeń akceptacji/odrzucenia (acceptStatus change) z timestampem do późniejszej analizy KPI.
F14. Edycja istniejącej zaakceptowanej fiszki (pytanie, odpowiedź) z aktualizacją pola updatedAt (bez wersjonizacji historii).
F15. Usuwanie fiszki przez użytkownika (soft delete lub hard delete – decyzja implementacyjna; w MVP wystarczy hard delete jeśli brak zależności w SR bibliotece).
F16. Podstawowe zabezpieczenia wejścia: ograniczenie długości pól, sanityzacja HTML/JS (zapobieganie XSS) – minimalny poziom bezpieczeństwa.
F17. Podstawowa paginacja list gdy liczba fiszek > N (np. 50 na stronę) – zapewnienie wydajności UI.

## 4. Granice produktu
Zakres MVP obejmuje: generowanie AI z wklejonego tekstu, manualne tworzenie, edycję, usuwanie, sesje powtórek z integracją istniejącej biblioteki SR, dashboard admina z metrykami agregowanymi, prosty system użytkowników. Poza zakresem: własny zaawansowany algorytm SR (używamy istniejącego), import plików (PDF, DOCX, itd.), współdzielenie zestawów, integracje z platformami zewnętrznymi, aplikacje mobilne (tylko web), wersjonizacja treści, zapisy powodów odrzucenia, formaty inne niż Q&A, dodatkowe typy fiszek (obrazkowe, cloze), filtry jakości/PII. Techniczne ograniczenia: długość strony fiszki 50–500 znaków; brak przechowywania przyczyn odrzucenia; brak zaawansowanego systemu uprawnień poza rolą admin/user. Skalowalność: początkowo monolityczna aplikacja web (np. Node + React / Rails / Django) z prostą relacyjną bazą danych (PostgreSQL). Prywatność: brak specjalnych filtrów PII – użytkownik odpowiedzialny za treść. Bezpieczeństwo: standardowe uwierzytelnianie hasłem, podstawowa sanitacja wejścia. Wydajność: generacja fiszek batch < ~200 propozycji na żądanie (limit aby uniknąć zbyt długiego czasu odpowiedzi). 

## 5. Historyjki użytkowników
US-001
Tytuł: Rejestracja konta
Opis: Jako nieautentykowany użytkownik chcę stworzyć konto podając email i hasło aby móc zapisywać fiszki i śledzić powtórki.
Kryteria akceptacji:
- Formularz email (format valid), hasło (min 8 znaków) i przycisk rejestruj.
- Po sukcesie konto utworzone w bazie i użytkownik zalogowany (sesja / token).
- Błędny email lub słabe hasło pokazuje komunikat walidacji.
- Próba utworzenia konta z istniejącym emailem zwraca błąd.

US-002
Tytuł: Logowanie
Opis: Jako zarejestrowany użytkownik chcę się zalogować aby uzyskać dostęp do moich fiszek.
Kryteria akceptacji:
- Formularz email + hasło.
- Poprawne dane: przejście do dashboardu fiszek.
- Niepoprawne: komunikat błędu.
- Sesja utrzymywana do wylogowania lub wygaśnięcia (timeout konfigurowalny, np. 24h).

US-003
Tytuł: Reset hasła
Opis: Jako użytkownik który zapomniał hasła chcę otrzymać link resetujący aby odzyskać dostęp.
Kryteria akceptacji:
- Formularz wprowadzenia email.
- Jeśli email istnieje wysyłany jest link resetujący (token ważny X min, np. 60).
- Po kliknięciu link prowadzi do formularza ustawienia nowego hasła.
- Nowe hasło spełnia minimalne zasady i aktywuje konto.

US-004
Tytuł: Wylogowanie
Opis: Jako zalogowany użytkownik chcę się wylogować aby zakończyć sesję na współdzielonym urządzeniu.
Kryteria akceptacji:
- Przy kliknięciu wyloguj sesja/token unieważniony.
- Użytkownik przekierowany do strony logowania.

US-005
Tytuł: Wklejenie źródła i generacja
Opis: Jako użytkownik chcę wkleić moje notatki tekstowe i otrzymać propozycje fiszek Q&A aby szybko zbudować zestaw.
Kryteria akceptacji:
- Pole tekstowe przyjmuje min 50 znaków, max np. 20k znaków.
- Kliknięcie generuj uruchamia proces, pokazuje stan ładowania.
- Wynik zawiera listę propozycji (co najmniej 1 jeśli model wygeneruje) z pytaniem i odpowiedzią.
- Błąd AI lub pusty wynik daje komunikat i możliwość ponowienia.

US-006
Tytuł: Limit wielkości batch generacji
Opis: Jako użytkownik nie chcę czekać zbyt długo na generację z bardzo dużego tekstu aby zachować płynność.
Kryteria akceptacji:
- Jeśli tekst przekracza ustalony limit segmentów system informuje o przycięciu lub proponuje podział.
- Generacja nie trwa dłużej niż ustalony SLA (np. 10 s dla 100 segmentów) w testach.

US-007
Tytuł: Przegląd propozycji
Opis: Jako użytkownik chcę zobaczyć wszystkie wygenerowane propozycje z możliwością edycji przed akceptacją.
Kryteria akceptacji:
- Lista kart z polem edycji pytania i odpowiedzi (inline lub modal).
- Zmiany lokalne widoczne natychmiast przed zapisaniem.
- Walidacja długości 50–500 znaków na każdą stronę.

US-008
Tytuł: Akceptacja pojedyncza
Opis: Jako użytkownik chcę zaakceptować pojedynczą propozycję aby dodać ją do mojej biblioteki.
Kryteria akceptacji:
- Kliknięcie akceptuj zapisuje fiszkę do Cards (status accepted, generationMethod=AI).
- Fiszka znika z listy propozycji lub oznaczona jako zaakceptowana.
- Log akceptacji zapisany.

US-009
Tytuł: Masowa akceptacja
Opis: Jako użytkownik chcę zaznaczyć wiele propozycji i zaakceptować je jednym kliknięciem aby przyspieszyć proces.
Kryteria akceptacji:
- Checkboxy lub zaznaczanie wielokrotne dostępne.
- Kliknięcie masowa akceptacja zapisuje wszystkie wybrane.
- Błędy pojedynczych zapisów raportowane selektywnie (np. toast z listą ID).

US-010
Tytuł: Odrzucenie pojedyncze
Opis: Jako użytkownik chcę odrzucić propozycję aby nie zaśmiecała mojej listy.
Kryteria akceptacji:
- Kliknięcie odrzuć usuwa propozycję z interfejsu (bez zapisu do Cards).
- Log odrzucenia zapisany (acceptStatus=rejected).

US-011
Tytuł: Masowe odrzucenie
Opis: Jako użytkownik chcę odrzucić wiele propozycji naraz aby oczyścić listę szybko.
Kryteria akceptacji:
- Wielokrotne zaznaczanie.
- Kliknięcie masowe odrzuć usuwa wszystkie wybrane propozycje.
- Logi odrzuceń zapisane zbiorczo.

US-012
Tytuł: Edycja przed akceptacją
Opis: Jako użytkownik chcę poprawić treść pytania lub odpowiedzi wygenerowanej zanim ją zaakceptuję.
Kryteria akceptacji:
- Edycja możliwa przed kliknięciem akceptuj.
- Walidacja długości i znaków specjalnych odbywa się przed zapisem.
- Zapis po akceptacji zawiera zedytowaną treść.

US-013
Tytuł: Manualne tworzenie fiszki
Opis: Jako użytkownik chcę ręcznie utworzyć fiszkę gdy AI nie wygenerowało potrzebnych treści.
Kryteria akceptacji:
- Formularz pytanie + odpowiedź.
- Walidacja długości 50–500 znaków.
- Po zapisie fiszka pojawia się w mojej liście (generationMethod=manual, acceptStatus=accepted).

US-014
Tytuł: Lista moich fiszek
Opis: Jako użytkownik chcę zobaczyć wszystkie moje fiszki z informacją o dacie następnej powtórki aby planować naukę.
Kryteria akceptacji:
- Widok listy z kolumnami: pytanie (skrót), status powtórki, nextReviewDate.
- Paginacja gdy >50.
- Sortowanie po nextReviewDate domyślnie rosnąco.

US-015
Tytuł: Filtrowanie fiszek wg statusu powtórki
Opis: Jako użytkownik chcę odfiltrować fiszki do powtórki dzisiaj aby skupić się na priorytetach.
Kryteria akceptacji:
- Filtr "Do powtórki" pokazuje tylko fiszki z nextReviewDate <= dzisiaj.
- Filtr "Nowe" pokazuje fiszki jeszcze bez pierwszej sesji.
- Reset filtrów przywraca pełną listę.

US-016
Tytuł: Rozpoczęcie sesji powtórek
Opis: Jako użytkownik chcę uruchomić sesję powtórek aktualnych fiszek aby utrwalić wiedzę.
Kryteria akceptacji:
- Kliknięcie rozpocznij sesję pobiera zestaw fiszek z nextReviewDate <= dzisiaj.
- Jeśli brak fiszek wyświetlany komunikat.
- Sesja pokazuje kolejno pytania.

US-017
Tytuł: Odsłonięcie odpowiedzi
Opis: Jako użytkownik chcę odsłonić odpowiedź po próbie przypomnienia aby ocenić swoją pamięć.
Kryteria akceptacji:
- Przycisk pokaż odpowiedź odsłania treść.
- UI blokuje ponowne odsłonięcie (idempotent).

US-018
Tytuł: Ocena odpowiedzi
Opis: Jako użytkownik chcę ocenić czy znałem odpowiedź aby algorytm ustalił kolejny termin.
Kryteria akceptacji:
- Przynajmniej dwie opcje: Again / Good (lub skala rozszerzalna).
- Wybranie opcji wysyła aktualizację do CardProgress.
- nextReviewDate aktualizowane według algorytmu.

US-019
Tytuł: Zakończenie sesji
Opis: Jako użytkownik chcę zakończyć sesję aby wrócić do listy i zobaczyć postępy.
Kryteria akceptacji:
- Po ostatniej fiszce lub kliknięciu zakończ wyświetlane podsumowanie liczby powtórek i skuteczności.
- Powrót do listy fiszek.

US-020
Tytuł: Edycja zaakceptowanej fiszki
Opis: Jako użytkownik chcę edytować treść fiszki po zaakceptowaniu aby poprawić jej jakość.
Kryteria akceptacji:
- Opcja edycji dostępna z widoku listy.
- Walidacja długości przy zapisie.
- updatedAt zmienione.

US-021
Tytuł: Usunięcie fiszki
Opis: Jako użytkownik chcę usunąć fiszkę która jest niepotrzebna aby utrzymać porządek.
Kryteria akceptacji:
- Akcja usuń dostępna na liście lub w detalu.
- Potwierdzenie (modal) przed finalnym usunięciem.
- Po usunięciu fiszka znika z listy.

US-022
Tytuł: Bezpieczeństwo dostępu do danych
Opis: Jako użytkownik oczekuję że nikt inny nie zobaczy moich fiszek aby zachować prywatność nauki.
Kryteria akceptacji:
- Próba dostępu do fiszek innego użytkownika zwraca błąd 403.
- Każde zapytanie wymaga uwierzytelnienia (token / sesja).

US-023
Tytuł: Dashboard admina – agregaty
Opis: Jako admin chcę zobaczyć kluczowe metryki adopcji aby ocenić jakość generacji i zaangażowanie.
Kryteria akceptacji:
- Widok liczby wszystkich fiszek (accepted AI + manual).
- Współczynnik akceptacji AI globalny (acceptedAI / generatedAI).
- Percentyle akceptacji (p50, p75, p90) z filtrem wykluczającym użytkowników <5 fiszek.
- Udział AI vs manual (procent).
- Liczba aktywnych użytkowników (logowanie lub powtórka) w 7 i 30 dni.

US-024
Tytuł: Logowanie zdarzeń akceptacji
Opis: Jako system chcę rejestrować akceptacje/odrzucenia aby umożliwić analizę KPI.
Kryteria akceptacji:
- Każda akcja accept/reject zapisuje rekord z userId, cardTempId, status, timestamp.
- Dane dostępne do agregacji.

US-025
Tytuł: Paginacja listy fiszek
Opis: Jako użytkownik chcę wygodnie nawigować po dużej liczbie fiszek aby nie obciążać UI.
Kryteria akceptacji:
- Domyślnie pokazuje pierwsze 50.
- Przyciski następna / poprzednia strona.
- Liczba wszystkich stron wyliczana.

US-026
Tytuł: Walidacja długości treści
Opis: Jako użytkownik chcę aby system odrzucał zbyt krótkie lub zbyt długie wpisy aby utrzymać jakość.
Kryteria akceptacji:
- Próba zapisu <50 znaków lub >500 znaków powoduje komunikat błędu.
- Zapis blokowany do poprawy.

US-027
Tytuł: Ograniczenie dużych wklejonych notatek
Opis: Jako użytkownik chcę wiedzieć gdy moje notatki są za duże aby zoptymalizować generację.
Kryteria akceptacji:
- Wklejenie >20k znaków wyświetla komunikat i blokuje generację lub proponuje podział.
- Użytkownik może zaakceptować przycięcie do limitu.

US-028
Tytuł: Stabilność sesji powtórek
Opis: Jako użytkownik chcę aby sesja nie traciła stanu w przypadku odświeżenia strony aby uniknąć frustracji.
Kryteria akceptacji:
- Odświeżenie strony podczas sesji zachowuje listę pozostałych fiszek i postęp (localStorage / backend).
- Jeśli sesja wygasła pokazuje komunikat i możliwość restartu.

US-029
Tytuł: Obsługa błędów generacji AI
Opis: Jako użytkownik chcę jasny komunikat gdy generacja się nie powiedzie aby móc spróbować ponownie.
Kryteria akceptacji:
- Timeout generacji > ustalony limit pokazuje wskazówkę o podziale tekstu.
- Błędy sieci / API pokazują distinct komunikat i przycisk ponów.

US-030
Tytuł: Ochrona przed XSS
Opis: Jako system chcę sanitizować treści wejściowe aby zapobiec wstrzyknięciom skryptów.
Kryteria akceptacji:
- Wklejenie lub zapis treści zawierającej tag script powoduje jego usunięcie lub zneutralizowanie.
- Testy bezpieczeństwa potwierdzają brak wykonywania wstrzykniętego JS.

US-031
Tytuł: Ręczne odświeżenie metryk admina
Opis: Jako admin chcę odświeżyć dane dashboardu aby zobaczyć najnowsze wartości.
Kryteria akceptacji:
- Przycisk odśwież generuje nowe zapytanie do agregacji.
- Widok aktualizuje się w <2 s dla bazy <100k fiszek.

US-032
Tytuł: Widok szczegółowy fiszki
Opis: Jako użytkownik chcę zobaczyć pełne pytanie i odpowiedź oraz historię powtórek (jeśli dostępna) aby ocenić jej trudność.
Kryteria akceptacji:
- Kliknięcie na skrót w liście otwiera detal z pełną treścią.
- Pokazane podstawowe parametry SR (ostatnia data powtórki, liczba powtórek).

US-033
Tytuł: Wznowienie przerwanej sesji
Opis: Jako użytkownik chcę wznowić sesję przerwaną wcześniej aby kontynuować naukę bez utraty stanu.
Kryteria akceptacji:
- Jeśli istnieje aktywna niedokończona sesja, przy wejściu na stronę powtórek pojawia się opcja wznowienia.
- Wznowienie ładuje pozostałe fiszki w prawidłowej kolejności.

US-034
Tytuł: Konwersja generacja→akceptacja (metryka)
Opis: Jako admin chcę mierzyć konwersję od wygenerowanych do zaakceptowanych fiszek od tygodnia 8 aby monitorować poprawę jakości.
Kryteria akceptacji:
- Dashboard pokazuje współczynnik (acceptedAI / generatedAI) z możliwością wyboru zakresu czasu >= week8.
- Dane filtrują użytkowników <5 fiszek.

US-035
Tytuł: Widoczność źródła
Opis: Jako użytkownik chcę mieć dostęp do oryginalnego wklejonego fragmentu aby móc zweryfikować kontekst fiszki.
Kryteria akceptacji:
- Link lub tooltip przy fiszce wskazuje fragment źródłowy (SourceText) jeśli istnieje.
- Jeśli brak powiązania (manual) element nie jest wyświetlany.

US-036
Tytuł: Minimalna dostępność funkcji
Opis: Jako użytkownik chcę korzystać z podstawowych funkcji w przeglądarce mobilnej mimo braku dedykowanej aplikacji aby mieć elastyczność.
Kryteria akceptacji:
- Interfejs responsywny (lista fiszek i sesja powtórek używalne na ekranie ~360px szerokości).
- Brak krytycznych błędów layoutu.

US-037
Tytuł: Ograniczenie liczby jednoczesnych generacji
Opis: Jako system chcę ograniczyć równoczesne generacje jednego użytkownika aby chronić zasoby.
Kryteria akceptacji:
- Próba uruchomienia nowej generacji gdy poprzednia w toku skutkuje komunikatem.
- Po zakończeniu generacji użytkownik może rozpocząć kolejną.

US-038
Tytuł: Spójność identyfikacji fiszek
Opis: Jako system chcę nadawać unikalne identyfikatory fiszkom aby zapewnić śledzenie i referencje.
Kryteria akceptacji:
- Każda zaakceptowana fiszka posiada unikalny ID w Cards.
- ID używany w logach akceptacji i postępie (CardProgress).

US-039
Tytuł: Odporność na duplikaty
Opis: Jako użytkownik chcę uniknąć zapisu identycznych fiszek aby nie marnować miejsca.
Kryteria akceptacji:
- Próba stworzenia nowej fiszki (manualnej lub z edytowanej propozycji) identycznej (pytanie + odpowiedź) jak istniejąca generuje ostrzeżenie.
- Użytkownik może potwierdzić zapis mimo ostrzeżenia (edge case decyzja: soft block).

US-040
Tytuł: Monitorowanie błędów
Opis: Jako admin chcę mieć informację o błędach generacji aby diagnozować problemy jakości.
Kryteria akceptacji:
- Dashboard pokazuje liczbę błędów generacji (timeout, API error) w wybranym okresie.
- Możliwe sortowanie po typie błędu.

US-041
Tytuł: Sesja powtórek – kolejność
Opis: Jako użytkownik chcę aby kolejność fiszek w sesji była deterministyczna aby móc kontynuować wznowienie.
Kryteria akceptacji:
- Kolejność ustalana np. po nextReviewDate, a przy remisie po ID rosnąco.
- Wznowienie zachowuje identyczną kolejność.

US-042
Tytuł: Dostępność podstawowa (a11y)
Opis: Jako użytkownik z ograniczeniami chcę korzystać z aplikacji z czytnikiem ekranu.
Kryteria akceptacji:
- Kluczowe elementy (przyciski akceptuj, odrzuć, generuj) posiadają etykiety aria-label.
- Focus management poprawny w sesji powtórek.

US-043
Tytuł: Ochrona przed nadmiernym rozmiarem odpowiedzi
Opis: Jako użytkownik chcę aby odpowiedzi nie były zbyt długie w UI.
Kryteria akceptacji:
- Odpowiedzi >500 znaków są przycięte z opcją "Pokaż całość".
- Przycięcie nie zmienia zapisanej wartości w bazie.

US-044
Tytuł: Śledzenie metody utworzenia
Opis: Jako admin chcę wiedzieć ile fiszek powstaje manualnie vs AI.
Kryteria akceptacji:
- Każda fiszka posiada generationMethod (AI|manual).
- Dashboard pokazuje procentowy udział.

US-045
Tytuł: Ograniczenie powtórnej edycji przed akceptacją
Opis: Jako użytkownik chcę uniknąć utraty zmian gdy edytuję wiele propozycji.
Kryteria akceptacji:
- Edycje wielu propozycji przed masową akceptacją nie są resetowane przez pojedyncze akcje.
- Zapis każdej zaakceptowanej zawiera ostatnią wersję edytowanej treści.

US-046
Tytuł: Bezpieczne API powtórek
Opis: Jako system chcę zapewnić że aktualizacje postępu powtórek są autoryzowane.
Kryteria akceptacji:
- Endpoint updateProgress wymaga uwierzytelnienia i poprawnego cardId należącego do użytkownika.
- Próba aktualizacji cudzego cardId zwraca 403.

US-047
Tytuł: Minimalne raportowanie konwersji po tygodniu 8
Opis: Jako admin chcę mieć dostęp do wskaźnika konwersji od tygodnia 8 aby mierzyć poprawę.
Kryteria akceptacji:
- Dashboard ukrywa sekcję konwersji dla dat < week8.
- Dla dat >= week8 sekcja widoczna z aktualnym współczynnikiem.

US-048
Tytuł: Stabilność identyfikatorów SourceText
Opis: Jako system chcę powiązać fiszki z fragmentem źródłowym.
Kryteria akceptacji:
- Każde wklejone źródło otrzymuje unikalny sourceId.
- Fiszki wygenerowane z tego źródła referencjonują sourceId.

US-049
Tytuł: Czyszczenie propozycji po zakończeniu
Opis: Jako użytkownik chcę aby zaakceptowane/odrzucone propozycje znikały aby lista była aktualna.
Kryteria akceptacji:
- Po masowej akceptacji/odrzuceniu pozostałe nieprzetworzone nadal widoczne.
- Odświeżenie strony nie przywraca przetworzonych.

US-050
Tytuł: Ograniczenie równoczesnych sesji
Opis: Jako system chcę zapobiec wielokrotnemu równoczesnemu odpaleniu sesji przez jednego użytkownika.
Kryteria akceptacji:
- Próba startu drugiej aktywnej sesji gdy jedna trwa pokazuje komunikat.
- Po zakończeniu pierwszej można rozpocząć następną.

US-051
Tytuł: Kolekcje fiszek
Opis: Jako użytkownik chcę móc zapisywać i edytować zestawy fiszek
Kryteria akceptacji:
- Użytkownik może zapisać aktualny zestaw fizek jako kolekcję 
- Użytkownik może aktualizować kolekcję.
- Użytkownik może usunąć kolekcję.
- Użytkownik może przywrócić kolekcję do poprzedniej wersji (pending changes).
- Funkcjonalność kolekcji nie jest dostępna bez logowania się do systemu

US-052
Tytuł: Bezpieczny dostęp
Opis: Jako użytkownik chcę mieć możliwość rejestracji i logowania się do systemu w sposób zapewniający bezpieczeństwo moich danych.
Kryteria akceptacji:
- Logowanie i rejestracja odbywają się na tej stronie wykorzystaniu Identity.EntityFrameworkCore
- Sposób dodania nowego usera - sposób dowolny


## 6. Metryki sukcesu
M1. Współczynnik akceptacji AI: acceptedAI / generatedAI powinien osiągnąć >=75% (globalnie). Monitorowany dziennie i tygodniowo; raport percentylowy (p50, p75, p90) z wykluczeniem użytkowników <5 fiszek.
M2. Udział AI w tworzeniu fiszek: (count fiszek AI accepted) / (count fiszek AI accepted + manual accepted) >=75%. Monitorowany tygodniowo.
M3. Konwersja generacja→akceptacja od tygodnia 8: acceptedAI / generatedAI (dla okresu od startu week8 do bieżącej daty) – dążenie do trendu rosnącego; sekcja dostępna tylko dla >= week8.
M4. Aktywni użytkownicy: liczba użytkowników z >=1 zalogowaniem lub sesją powtórek w ostatnich 7 i 30 dniach – cel wzrostu stabilnego (baseline ustalony po MVP release).
M5. Czas generacji: średni czas od kliknięcia generuj do otrzymania wyników <10 s dla batchu 100 segmentów (SLA). Rejestrowany w logach.
M6. Jakość nauki (opcjonalne przyszłe): retencja D1/D7 – metryka eksploracyjna, poza rdzeniem MVP.
Dane źródłowe: acceptStatus (enum: proposed|accepted|rejected), generationMethod (AI|manual), timestamps (createdAt, updatedAt, acceptedAt, nextReviewDate), cardProgress (liczba powtórek, lastReviewResult), sourceId, errorLogs (typ, timestamp). Mechanizm pomiaru: okresowe zapytania agregujące + ewentualna warstwa ETL (nie w MVP).