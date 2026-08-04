# Wykaz cookies i storage

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | audyt; polityka cookies |
| Klasyfikacja | PUBLICZNY PO ZATWIERDZENIU |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Nazwa/klucz | Typ | Dostawca | Cel | Niezbędne | Czas | Strona | Dane | Rekomendacja | Dowód |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| gf3.auth.access-token | localStorage | GF | sesja JWT | Tak dla obecnego modelu | do logout/wygaśnięcia/wyczyszczenia | first | token | informacja; ocenić bezpieczniejszy model | AuthProvider |
| gf3.auth.last-username | localStorage | GF | wygoda logowania | Nieściśle | do wyczyszczenia | first | username | opcja i informacja | LoginPage |
| gf3.auth.password-mode | localStorage | GF | PC/Phone | Nieściśle | do wyczyszczenia | first | preferencja | informacja | LoginPage |
| gf3.employee-notifications.read.* | localStorage | GF | odczyty powiadomień | Funkcjonalne | część 7 dni, część trwała; maks. 300 | first | ID, czas, employee ID/username w kluczu | minimalizacja i informacja | helper notifications |
| gf3:employee-schedule-column-order:* | localStorage | GF | kolejność kolumn | Funkcjonalne | do wyczyszczenia | first | employee ID/username, schedule ID | informacja | schedule page |
| *:list:pinned | localStorage | GF | przypięte rekordy | Nie | do wyczyszczenia | first | ID rekordów | informacja | list cards |
| employee UI state | baza serwera | GF | synchronizacja odczytów/pinów | Funkcjonalne | [DO WERYFIKACJI RODO] | server | employee ID, IDs, czas | retencja | EmployeeUiState |
| youtube-nocookie.com | iframe / możliwe cookies/storage | Google/YouTube | wideo aktualności | Nie | wg dostawcy | third | IP, nagłówki, interakcje – do testu | uprzednia zgoda lub click-to-load | system news |
| zewnętrzny imageUrl | żądanie HTTP / cache | wskazany host | obraz aktualności | Nie | wg przeglądarki/hosta | third | IP, nagłówki | proxy/allowlist/zgoda | system news |
| cookies produkcyjne | HTTP cookie | [DO WERYFIKACJI TECHNICZNEJ] | [DO WERYFIKACJI TECHNICZNEJ] | nieustalone | nieustalone | nieustalone | nieustalone | audyt DevTools | środowisko niebadane |
