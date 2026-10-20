# Checklista gotowości do podpisania

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | cały pakiet; istniejące dokumenty |
| Klasyfikacja | POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

- [ ] zdolność i warunki działalności nierejestrowanej, reprezentacja i dane Stron;
- [ ] Umowa, Załącznik 1, Regulamin, informacja o zagrożeniach i DPA A–E spójne;
- [ ] cena 500,00 zł, okres, płatność 7 dni, wypowiedzenie, odpowiedzialność i wsparcie uzgodnione;
- [ ] role RODO, instrukcje, audyt, podprocesorzy, transfery, retencja i exit prawidłowe;
- [ ] podstawy przetwarzania własnego i klauzule informacyjne zatwierdzone;
- [ ] model UŚUDE, udostępnienie/akceptacja dokumentów i publiczne linki rozstrzygnięte;
- [ ] VAT, KSeF, PIT/ZUS pozostają decyzją właściwego specjalisty;
- [ ] wszystkie blokujące placeholdery zamknięte; wersje/datowanie/podpisy finalne.

Wynik: **[DO WERYFIKACJI PRAWNEJ: GOTOWE / WARUNKOWO / NIEGOTOWE]**; zastrzeżenia **[DO WYPEŁNIENIA]**.

## Podstawy i materiały do weryfikacji

- [RODO](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Ustawa o świadczeniu usług drogą elektroniczną – ELI](https://eli.gov.pl/eli/DU/2024/1513/ogl)
- [biznes.gov.pl – działalność nierejestrowana](https://biznes.gov.pl/pl/firma/zakladanie-firmy/chce-wiedziec-jak-zalozyc-wlasna-firme/dzialalnosc-nierejestrowa-oraz-inne-sytuacje-w-ktorych-nie-trzeba-rejestrowac-firmy)

## Weryfikacja rozszerzonego zakresu

Platforma rejestruje osobiste potwierdzenie opublikowanej wersji PDF przez konto menedżera lub pracownika: identyfikator konta, rolę, migawkę loginu i nazwy oraz czas UTC. Dokument ma wersję, SHA-256 i czas publikacji; opublikowany dokument jest niezmienny, a przyjęte lub opublikowane dokumenty nie podlegają zwykłemu usunięciu. Dostępna jest historia własna i historia pracownika dla menedżera. Middleware zwraca HTTP 428 dla chronionych operacji API przy oczekujących dokumentach, z wyjątkami uwierzytelnienia, pobrania i potwierdzenia dokumentów oraz wskazanych tras. Gate prezentuje PDF i pole domyślnie niezaznaczone. Nie dowodzi to przeczytania każdej strony ani kwalifikowanego podpisu; nie podpisuje Umowy SaaS lub DPA w imieniu Klienta i nie jest ogólną zgodą RODO. Tylko prawnie zweryfikowany PDF może trafić do publikacji produkcyjnej; ten przegląd niczego nie publikuje.

- [ ] Sprawdzono push opt-in/opt-out dla urządzenia, ekran blokady i zmianę konta, nie utożsamiając permission ze zgodą RODO.
- [ ] Zamknięto ocenę opcjonalnego storage i mediów oraz zidentyfikowano dostawców/transfery.
- [ ] Archiwizacja pracownika nie została poświadczona jako wymazanie danych.
