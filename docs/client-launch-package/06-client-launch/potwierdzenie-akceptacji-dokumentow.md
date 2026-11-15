# Potwierdzenie udostępnienia i akceptacji dokumentów

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | regulamin; polityki; umowy |
| Klasyfikacja | POUFNY UMOWNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

Platforma rejestruje osobiste potwierdzenie opublikowanej wersji PDF przez konto menedżera lub pracownika: identyfikator konta, rolę, migawkę loginu i nazwy oraz czas UTC. Dokument ma wersję, SHA-256 i czas publikacji; opublikowany dokument jest niezmienny, a przyjęte lub opublikowane dokumenty nie podlegają zwykłemu usunięciu. Dostępna jest historia własna i historia pracownika dla menedżera. Middleware zwraca HTTP 428 dla chronionych operacji API przy oczekujących dokumentach, z wyjątkami uwierzytelnienia, pobrania i potwierdzenia dokumentów oraz wskazanych tras. Gate prezentuje PDF i pole domyślnie niezaznaczone. Nie dowodzi to przeczytania każdej strony ani kwalifikowanego podpisu; nie podpisuje Umowy SaaS lub DPA w imieniu Klienta i nie jest ogólną zgodą RODO. Tylko prawnie zweryfikowany PDF może trafić do publikacji produkcyjnej; ten przegląd niczego nie publikuje. Formularz zachowuje odrębny dowód umów i doręczenia informacji, których gate nie zastępuje.

| Dokument | Wersja | Data udostępnienia | Sposób/link | Akceptacja wymagana? | Osoba/umocowanie | Data/podpis |
|---|---|---|---|---|---|---|
| Umowa SaaS i Załącznik 1 | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | Tak | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| DPA A–E | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | Tak | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Regulamin / zagrożenia | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WERYFIKACJI PRAWNEJ]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Prywatność / cookies-storage | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | udostępnienie | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |

Klient zobowiązuje się przekazać Użytkownikom właściwe wersje i zachować dowód zgodnie z uzgodnionym procesem.

## Weryfikacja rozszerzonego zakresu

Platforma rejestruje osobiste potwierdzenie opublikowanej wersji PDF przez konto menedżera lub pracownika: identyfikator konta, rolę, migawkę loginu i nazwy oraz czas UTC. Dokument ma wersję, SHA-256 i czas publikacji; opublikowany dokument jest niezmienny, a przyjęte lub opublikowane dokumenty nie podlegają zwykłemu usunięciu. Dostępna jest historia własna i historia pracownika dla menedżera. Middleware zwraca HTTP 428 dla chronionych operacji API przy oczekujących dokumentach, z wyjątkami uwierzytelnienia, pobrania i potwierdzenia dokumentów oraz wskazanych tras. Gate prezentuje PDF i pole domyślnie niezaznaczone. Nie dowodzi to przeczytania każdej strony ani kwalifikowanego podpisu; nie podpisuje Umowy SaaS lub DPA w imieniu Klienta i nie jest ogólną zgodą RODO. Tylko prawnie zweryfikowany PDF może trafić do publikacji produkcyjnej; ten przegląd niczego nie publikuje.

- [ ] Sprawdzono push opt-in/opt-out dla urządzenia, ekran blokady i zmianę konta, nie utożsamiając permission ze zgodą RODO.
- [ ] Zamknięto ocenę opcjonalnego storage i mediów oraz zidentyfikowano dostawców/transfery.
- [ ] Archiwizacja pracownika nie została poświadczona jako wymazanie danych.
