# Wykaz cookies i storage

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | audyt; polityka cookies |
| Klasyfikacja | PUBLICZNY PO ZATWIERDZENIU |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Zakres i ocena prawna

Stan kodu: DEV2 171aec08a34dd48e43f1eca8c23d104a4ee7b750, przegląd 2026-10-19. VERIFIED IN CODE nie oznacza VERIFIED IN DEPLOYMENT. Inwentarz obejmuje wszystkie odnalezione miejsca zapisu w kodzie produktu; cookies i pamięć dostawców wymagają pomiaru domeny produkcyjnej.

Art. 399 PKE obejmuje zapis i odczyt informacji w urządzeniu, także localStorage, niezależnie od tego, czy informacja jest daną osobową lub HTTP cookie. Wyjątek ust. 3 wymaga konieczności dla transmisji albo usługi wyraźnie żądanej przez użytkownika. Sama wygoda lub brak reklamy nie wystarcza. Poniższa kwalifikacja jest REQUIRES LEGAL REVIEW, a opis implementacji VERIFIED IN CODE. Dostawcą własnych zapisów jest Operator GF; przeglądarka wykonuje zapis.

| Klucz / medium | Cel i dane osobowe | Czas i usunięcie | Niezbędność i ocena art. 399 | Dowód implementacji |
|---|---|---|---|---|
| gf3.auth.access-token / localStorage | Odtworzenie sesji bearer; identyfikacja konta w JWT, wysokie ryzyko XSS | Brak TTL samego wpisu; wylogowanie/unieważnienie usuwa token; ważność JWT jest odrębna | Niezbędna żądana sesja; trwałość i zakres wymagają minimalizacji | FrontEnd/src/app/providers/AuthProvider.tsx:24, persistToken |
| gf3.auth.last-username / localStorage | Zapamiętanie loginu; identyfikator osoby | Do nadpisania lub usunięcia danych witryny, bez TTL | Wygoda, brak potwierdzonego prior opt-in; domyślny trwały zapis nie jest automatycznie wyjątkiem | FrontEnd/src/pages/login/ui/LoginPage.tsx:14, persistValue |
| gf3.auth.password-mode / localStorage | Tryb PC/Phone; preferencja powiązana z urządzeniem | Bez TTL; zmiana lub czyszczenie | Preferencja; uzasadnić żądanie trwałego zapamiętania albo opt-in | LoginPage.tsx:15 |
| gf3.employee-notifications.read.* / localStorage | ID odczytów; employee ID/login w kluczach, znaczniki czasu | Nowe datowane wpisy filtrowane po 5 dniach; starsze formaty mogą pozostać, brak TTL klucza; czyszczenie witryny | Funkcja odczytów; serwer też synchronizuje stan, więc konieczność trwałej kopii wymaga oceny | FrontEnd/src/shared/lib/employeeNotificationReadState.ts:3, getEmployeeNotificationReadStorageKeys |
| gf3:employee-availability:time-presets:{employeeId} / localStorage | Godziny presetów, ID pracownika | Bez TTL; edycja/usunięcie danych witryny | Własny zapis presetów na działanie użytkownika; uzasadnić trwałe zapamiętanie żądanej funkcji albo zgoda | FrontEnd/src/pages/employee-availability/ui/EmployeeAvailabilityPage.tsx:44, readTimePresets |
| gf3:employee-schedule-column-order:v{version}:employee-{id} lub user-{name} / localStorage | Układ kolumn i ID grafików, powiązanie z osobą | Bez TTL; nadpisanie/czyszczenie | Wygoda układu; nie uznawać automatycznie za ściśle niezbędną | FrontEnd/src/pages/employee-schedule/ui/EmployeeSchedulePage.tsx:571, writeEmployeeScheduleColumnOrders |
| employees:list:pinned; shops:list:pinned; containers:list:pinned; availability-groups:list:pinned / localStorage | ID przypiętych rekordów, możliwy kontekst pracownika | Bez TTL; odpięcie nadpisuje listę, czyszczenie usuwa klucz | Wygoda; brak potwierdzonej zgody na pamięć opcjonalną | FrontEnd/src/shared/lib/records/usePinnedRecords.ts:23 oraz komponenty *ListCard |
| gf3.push.employeeId / localStorage | ID pracownika dla synchronizacji subskrypcji na urządzeniu | Do disableForDevice/czyszczenia; bez TTL | Obsługa wyraźnie włączonego push; wyjątek tylko w niezbędnym zakresie, po ocenie | FrontEnd/src/entities/employee-push/lib/pushSubscription.ts:3 |
| gf3.push.invitation.{employeeId} / localStorage | Odłożenie zaproszenia, ID osoby, czas następnej propozycji | Logiczny cooldown 7 dni; wpis fizycznie pozostaje do nadpisania/usunięcia | Zapamiętanie odmowy może służyć jej poszanowaniu; automatyczny zapis i trwałość wymagają oceny, nie zgoda na push | FrontEnd/src/entities/employee-push/ui/EmployeePushInvitation.tsx:11–18, isDismissed |
| /sw.js / rejestr service worker przeglądarki, scope / | Obsługa push i kliknięcia, bez tokenu JWT w skrypcie | Do wyrejestrowania lub usunięcia danych witryny; brak TTL aplikacyjnego | Niezbędny do żądanego push; sama rejestracja nie dowodzi zgody RODO | pushSubscription.ts:27, registerWorker; FrontEnd/public/sw.js |
| PushSubscription / pamięć przeglądarki i usługi push | Endpoint oraz klucze p256dh/auth, powiązanie z kontem | Do unsubscribe, wygaśnięcia przez dostawcę lub usunięcia; cofnięcie permission nie dowodzi usunięcia serwerowego | Wyłącznie po jawnym włączeniu i zgodzie systemowej; analiza PKE odrębna od podstawy RODO | pushSubscription.ts:49, ensureSubscription; lib/enablePush.ts |
| Notification.permission / ustawienie przeglądarki lub systemu | Decyzja granted/denied/default dla originu | Według przeglądarki, zmiana w jej ustawieniach; GF nie cofa go programowo | Uprawnienie urządzenia, odrębne od zgody cookies i podstawy RODO | lib/enablePush.ts, Notification.requestPermission |
| HTTP cache / pamięć przeglądarki, PWA manifest i ikony | Pobierane zasoby, możliwe lokalne ślady | Zależnie od nagłówków, przeglądarki i czyszczenia; produkcyjny proxy do testu | Zwykła transmisja/wyświetlenie; nie przyjmować automatycznie wyjątku dla dodatkowego offline cache | FrontEnd/public/manifest.json; GF3.WebApi/Program.cs, ApplyFrontendCacheHeaders |
| Cache API / aplikacyjna pamięć offline | Nie znaleziono fetch handlera ani caches.open w sw.js | Nie ustalono osobnego cache aplikacji | Obecny SW obsługuje powiadomienia; nie deklarować pełnej aplikacji offline | FrontEnd/public/sw.js |
| sessionStorage, IndexedDB i własne HTTP cookies | Nie znaleziono zapisów w kodzie produktu; nie dowodzi braku zapisów środowiska/dostawców | Do ustalenia w pomiarze | UNVERIFIED EXTERNAL FACT dla produkcji | Przeszukanie FrontEnd/src i public/sw.js |
| youtube-nocookie.com / iframe, możliwa pamięć i HTTP cache | Wideo, IP, nagłówki i interakcje; dostawca YouTube/Google | Według dostawcy; zmierzyć cookies/storage i czas w aktualnym runtime | Opcjonalne; obecny iframe może ładować się automatycznie. Wymagane blokowanie przed zgodą dla zapisów nieobjętych wyjątkiem | FrontEnd/src/features/manager-system-news/ui/ManagerSystemNews.tsx:32–51; pages/employee-notifications/ui/EmployeeNotificationsPage.tsx:139,248 |
| Zewnętrzny imageUrl / HTTP i cache | Obraz, IP, nagłówki; host wskazany w treści | Według hosta i przeglądarki; brak stałej listy w kodzie | Opcjonalne połączenie; ocena pamięci i RODO, blokada/prywatny hosting/proxy przed dopuszczeniem | Te same komponenty aktualności, newsImage |
| Cookies proxy, hostingu i zasoby produkcyjne | Nazwy/dostawcy nieustalone | [PRODUCTION_TEST_REQUIRED: nazwy, domeny, TTL, cele i sposób usuwania] | Nie klasyfikować zbiorczo jako niezbędne | DevTools i nagłówki rzeczywistej domeny, brak badania w tym przeglądzie |

Stan odczytów i pinów synchronizowany przez EmployeeUiStateController jest bazą serwera, nie dodatkowym kluczem pamięci urządzenia. Język konta jest zapisywany przez API; nie znaleziono dodatkowego klucza języka w localStorage. Nie znaleziono wywołania navigator.storage.persist; trwałość i usuwanie danych przez przeglądarkę nie są gwarantowane.

## Warunek wydania

Brak potwierdzonego mechanizmu prior opt-in dla opcjonalnych zapisów i mediów. Przed wydaniem należy dla każdego wiersza uzyskać udokumentowane uzasadnienie wyjątku albo wdrożyć rzeczywisty wybór przed zapisem/odczytem/połączeniem, równoważną odmowę i łatwe wycofanie. Test ma wykazać brak operacji opcjonalnych przed wyborem oraz po odmowie; sam baner nie spełnia tego wymagania. Odmowa nie ogranicza podstawowej sesji i grafiku. Permission push nie zastępuje tego testu. Nie zmieniono aplikacji w celu ukrycia luki.
