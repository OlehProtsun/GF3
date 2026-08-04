# Mapa danych osobowych

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | analiza ról; DPA B |
| Klasyfikacja | WEWNĘTRZNY / RODO |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Moduł/encja | Pola | Osoba | Cel | Rola | Dostęp | Odbiorca | Retencja | Eksport | Usunięcie | Ryzyko |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Employee | imię, nazwisko, telefon, e-mail, ID | pracownik | profil i grafik | procesor | menedżer; własny profil częściowo | hosting/SMTP | [DO WERYFIKACJI RODO] | XLSX pośrednio | CRUD z ograniczeniami | kontakt i historia |
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
