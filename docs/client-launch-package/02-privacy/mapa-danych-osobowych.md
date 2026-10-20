# Mapa danych osobowych

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | analiza ról; DPA B |
| Klasyfikacja | WEWNĘTRZNY / RODO |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Moduł/encja | Pola | Osoba | Cel | Rola | Dostęp | Odbiorca | Retencja | Eksport | Usunięcie | Ryzyko |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Employee | imię, nazwisko, telefon, e-mail, ID | pracownik | profil i grafik | procesor | menedżer; własny profil częściowo | hosting/SMTP | [DO WERYFIKACJI RODO] | XLSX pośrednio | Soft-delete: zachowany profil/history, konto usuwane | kontakt i historia |
| EmployeeAccount | username, hash, czasy logowania/seen/reset, sessionVersion | pracownik | auth | procesor / bezpieczeństwo do analizy | system, pracownik | hosting/SMTP | [DO WERYFIKACJI RODO] | brak pełnego | przez zależność profilu | auth wysokie |
| ManagerAccount | username, nazwa, recovery e-mail, hash, czasy | menedżer | auth/admin | procesor / do analizy | menedżerowie | hosting/SMTP | [DO WERYFIKACJI RODO] | brak | manager CRUD | szerokie uprawnienia |
| Schedule/Slot | nazwa, miesiąc, godziny, pracownik, notatka | pracownik | grafik | procesor | role wg publikacji | hosting; XLSX/SQL odbiorca | wg Klienta | XLSX/SQL | CRUD | wolny tekst |
| Availability | dzień, rodzaj, przedział, modyfikacja | pracownik | dostępność | procesor | pracownik/menedżer | hosting | wg Klienta | pośrednio | CRUD grupy | może ujawnić wrażliwe przyczyny w nazwach |
| ShiftSwap | strony, czas, status, snapshot, nazwy historyczne | pracownik | zamiany i dowód | procesor | role | hosting | [DO WERYFIKACJI RODO] | brak pełnego | część historii trwała | duplikacja danych |
| Communication | tytuł, treść, autor, odczyt | Użytkownicy | ogłoszenia | procesor | role | hosting | do ustalenia | nie | manager CRUD | wolny tekst |
| WorkflowLog | aktor, rola, ID, działanie, czas | Użytkownik | audyt operacyjny | do analizy | menedżer | hosting | ustawialna, bez finalnej polityki | nie | manager delete | nadmiar treści |
| UI state/localStorage | token, username, role pośrednio, ID powiadomień, preferencje | Użytkownik | sesja i UX | do analizy | przeglądarka | urządzenie; YouTube przy mediach | różna | nie | logout/clear | XSS i urządzenie wspólne |
| ManagerNote | tytuł, treść, właściciel | menedżer/inne osoby w treści | prywatne notatki | procesor | właściciel | hosting | [DO WERYFIKACJI RODO] | nie | CRUD | dane nadmiarowe |
| Support/billing | kontakt, treść, dokument | kontakty B2B | wsparcie/rozliczenie | administrator | upoważnione osoby | e-mail/księgowość | ustawowa/roszczenia | na żądanie | procedura | poza bazą aplikacji |

## Uzupełnienia modelu wdrożonego na badanym commicie

| Encja | Kategorie i cel | Dostęp / odbiorcy | Retencja i usunięcie |
|---|---|---|---|
| RegulationDocument / RegulationAcceptance | PDF, wersja/hash/publikacja i konto/rola/migawki/czas UTC; dowód potwierdzenia | Użytkownik własne, menedżer historia pracownika, system manager publikacja | Chronione przed zwykłą edycją/usunięciem; polityka prawna i kontrolowany exit wymagają instrukcji |
| EmployeePushSubscription / Delivery | Endpoint i p256dh/auth, ID, daty; zdarzenie, próby, nextAttempt, lease, wynik/finalizacja | Konto pracownika dla własnego endpointu; skaner i brama push | Finalizacje >5 dni sprzątane podczas skanu; brak ogólnego TTL endpointu; delete subskrypcji kaskaduje delivery |
| Korekty i wersje grafików | Autor, treść/status/czas i migawki przed/po zmianach, dane pracowników | Autoryzowane role według funkcji | Instrukcja Klienta i analiza zależności; nie znika wraz z ukryciem profilu |

Usunięcie pracownika oznacza archiwizację profilu przez IsDeleted oraz usunięcie konta pracownika. Nie oznacza pełnego wymazania profilu, kontaktów, historycznych grafików, dostępności, zamian, korekt, dowodów akceptacji, eksportów lub kopii. Wykonanie prawa do usunięcia wymaga ustalenia podstaw retencji, instrukcji Klienta i odrębnej kontroli powiązań; nie należy zastępować archiwizacji deklaracją anonimizacji.

Pełny wykaz urządzenia zawiera 03-cookies-and-storage/wykaz-cookies-i-storage.md. Dla każdego celu stosuje się role i podstawy z analizy ról i polityki, retencję z macierzy oraz kontrolowane eksporty według DPA E. Repozytorium nie potwierdza nazw dostawców ani regionów; odbiorcę eksportu ustala Klient. Ryzyko wolnego tekstu obejmuje także przyczyny i notatki korekt.
