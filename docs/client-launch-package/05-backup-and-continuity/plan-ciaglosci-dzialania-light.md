# Plan ciągłości działania — light

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | incydenty; backup; wsparcie |
| Klasyfikacja | POUFNY WEWNĘTRZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Usługi krytyczne

Logowanie, odczyt/opublikowanie harmonogramu, dostępność, zamiany i komunikacja. Zależności: hosting **[DO WERYFIKACJI TECHNICZNEJ]**, DNS/TLS **[DO WERYFIKACJI TECHNICZNEJ]**, baza/backup, dostawca e-mail **[DO WERYFIKACJI TECHNICZNEJ]**.

## Cele i scenariusze

Docelowe RTO **[DO WYPEŁNIENIA]**, RPO **[DO WYPEŁNIENIA]**, godziny obsady **[DO WYPEŁNIENIA]**. Scenariusze: awaria aplikacji/hosta, uszkodzenie bazy, utrata DNS/TLS, niedostępność poczty, przejęcie konta, brak osoby kluczowej. Dla każdego: właściciel, detekcja, obejście, komunikacja, kryterium odtworzenia i powrotu.

## Tryb awaryjny

Klient utrzymuje zatwierdzony alternatywny kanał publikacji grafików **[DO WYPEŁNIENIA]**. GF komunikuje status przez **[DO WYPEŁNIENIA]** z częstotliwością zależną od SEV. Nie wysyłać danych harmonogramu niezatwierdzonym kanałem.

## Ćwiczenia

Tabletop i test backupu przed startem oraz co **[DO WYPEŁNIENIA]**; po zdarzeniu aktualizacja planu, kontaktów i ryzyka.
