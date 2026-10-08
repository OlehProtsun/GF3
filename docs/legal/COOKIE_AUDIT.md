# Audyt cookies i pamięci przeglądarki

WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ

Wersja: 0.1.0-draft | Obowiązuje od: [DO UZUPEŁNIENIA]
Operator: [DO UZUPEŁNIENIA]

## Zakres i dowody — 2026-10-08

Ograniczone wyszukiwanie wejścia `FrontEnd/index.html`, `src/main.tsx`, providerów, operacji browser storage oraz ich bezpośrednich producentów; szablon nginx HTTPS. Nie jest to audyt całego repo ani zewnętrznej produkcji.

Chromium, świeży profil: siedem stron prawnych przez Vite `127.0.0.1:5174` i skompilowany ASP.NET w Production `127.0.0.1:5185`, nowa syntetyczna SQLite. Każda odpowiedź 200/text/html, bez redirectu do logowania. `document.cookie === ""`, sessionStorage i localStorage puste, brak script na statycznych stronach. Sprawdzony nagłówek odpowiedzi legal index nie zawierał Set-Cookie. Na logowaniu po wpisaniu syntetycznej nazwy i wyborze Phone: localStorage zawierał tylko last-username/password-mode; cookies/sessionStorage puste. Nie zapisywano wartości tokenów lub rzeczywistych danych osobowych w dowodach.

Rzeczywista domena, reverse proxy, dostawcy i produkcyjne nagłówki: [DO UZUPEŁNIENIA]. Szablon nginx przekierowuje do HTTPS, ustawia HSTS i proxy; nie deklaruje Set-Cookie/trackera. To konfiguracja źródłowa, nie dowód aktywnego wdrożenia.

## Inwentarz

| Mechanizm / klucz | Cel | Czas / usunięcie | Strona trzecia | Klasyfikacja i droga prawna | Skuteczne blokowanie |
|---|---|---|---|---|---|
| document.cookie | Nie wykryto własnych cookies w badanym runtime | Brak obserwowanego zapisu | Nie wykryto | Brak kategorii do zgody w tym profilu; proxy do sprawdzenia | Nie wykryto skryptu ustawiającego |
| sessionStorage | Nie wykryto użycia w bounded scan/runtime | Brak | Nie wykryto | Brak obserwowanego zapisu | Nie wykryto producenta |
| gf3.auth.access-token (localStorage) | Żądana sesja bearer | Brak browser TTL; kod usuwa przy wylogowaniu/unieważnieniu; ważność JWT odrębna od fizycznego zapisu | Nie wykryto odbiorcy poza API instancji | Ściśle niezbędna sesja, art. 399 ust. 3 PKE do potwierdzenia praktyk | Logout usuwa token; blokada storage pozostawia sesję w pamięci, może uniemożliwić odtworzenie |
| gf3.auth.last-username | Żądane ułatwienie ponownego logowania | Do nadpisania lub usunięcia danych witryny; bez TTL | Nie wykryto | Preferencja funkcjonalna, ocena wyjątku art. 399 ust. 3 przez prawnika; nie reklama | Usunięcie w przeglądarce; nie ma osobnego consent toggla |
| gf3.auth.password-mode | Wybrany sposób wpisania hasła | Do zmiany/usunięcia; bez TTL | Nie wykryto | Preferencja funkcjonalna, wymaga potwierdzenia niezbędności do żądanej funkcji | Usunięcie w przeglądarce |
| gf3:employee-availability:time-presets:{employeeId} | Zapis edytowanych presetów dostępności | Do zmiany/usunięcia; bez TTL | Nie wykryto | Ustawienie żądane przez użytkownika; ocena art. 399 ust. 3 | Brak opt-in tracking; storage można usunąć |
| gf3:employee-schedule-column-order:v{version}:employee-{id} / user-{name} | Kolejność kolumn; lokalna zgodność z UI state | Do nadpisania/usunięcia; bez TTL | Nie wykryto | Funkcjonalne ustawienie żądane przez użytkownika, ocena wyjątku | Usunięcie lokalne nie usuwa konfiguracji serwerowej |
| gf3.employee-notifications.read.{identity} | Przeczytane powiadomienia, także klucze legacy i employee-{id} | Nowe wpisy filtrowane po 5 dniach przy odczycie; fizyczny klucz bez TTL; legacy string bez daty może pozostać | Nie wykryto | Stan funkcjonalny powiadomień, ocena wyjątku | Usunięcie localStorage; synchronizacja serwera może odtworzyć stan |
| employees:list:pinned, shops:list:pinned, containers:list:pinned, availability-groups:list:pinned | Przypięte rekordy managera | Do odpięcia/nadpisania/usunięcia; bez TTL | Nie wykryto | Funkcjonalny stan UI; potwierdzić żądaną funkcję i konieczność | Usunięcie browser storage |
| Analityka, reklamy, CDN, beacons, zewnętrzne fonty/skrypty | Nie wykryto w badanym wejściu/providerach/statycznych stronach | Nie zaobserwowano | Dostawcy wdrożenia nieznani | Nie dodano trackingu. To nie certyfikacja całej infrastruktury | Jeżeli opcjonalne trackery zostaną wykryte, wyłączyć przed produkcją do osobnego uprzedniego opt-in i withdrawal |
| Kontakt / marketing | Link wsparcia w logowaniu; reset hasła to wiadomość operacyjna | Retencja i dostawca SMTP do ustalenia | DO UZUPEŁNIENIA | Nie stwierdzono signup marketingowego w badanym wejściu. Kampanie wymagają osobnego sprawdzenia PKE art. 398 i RODO | Brak kampanii/formularza zgody dodanego tą zmianą |

## Warunki publikacji

Nie dodano bannera: nie stwierdzono opcjonalnego śledzenia w ograniczonym zakresie. Preferencje nie są reklamowymi cookies, ale ich niezbędność i faktyczna domena wymagają oceny. Jeśli wyjątek nie przysługuje lub pojawi się tracking, wstrzymać odpowiednie zapisy/skrypty przed produkcją aż do odrębnego przetestowanego mechanizmu zgody i cofnięcia. Sama polityka nie daje zgody.

Wewnętrzne ryzyko: token w localStorage jest dostępny dla skryptów tej samej origin; XSS i sześciocyfrowe dane logowania wymagają przeglądu bezpieczeństwa przed danymi pracowników. Nie zmieniono auth, TTL ani storage. Stan języka jest zarządzany istniejącym providerem/API, bez wykrytego własnego localStorage w tym providerze.
