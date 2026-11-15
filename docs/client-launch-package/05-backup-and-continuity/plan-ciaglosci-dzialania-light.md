# Plan ciągłości działania — light

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
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

## PWA, push i retencja w odtwarzaniu

Kopia SQLite zawiera także dane subskrypcji i doręczeń push oraz potwierdzenia regulaminów. Przy odtwarzaniu testowym zablokować wysyłkę e-mail i push, używać środowiska izolowanego i danych syntetycznych, a przed wznowieniem produkcji ponowić wykonane usunięcia oraz zweryfikować aktywność endpointów i kont. Sam restore starego wolumenu nie może wznowić subskrypcji po ich wycofaniu. Sprawdzić niezmienność PDF i spójność historii, odczytów i wersji grafików.

Nie zmieniać GF3_DATA_VOLUME, nie usuwać gf3-data i nie stosować docker compose down -v w ramach aktualizacji. Najpierw bezpieczna kopia i potwierdzenie celu operacji. To wymóg proponowanej procedury, a nie dowód jej wykonania. RPO/RTO, separacja kopii, szyfrowanie i rzeczywisty test pozostają INFRASTRUCTURE_REQUIRED / PRODUCTION_TEST_REQUIRED. Cel ćwiczenia proponuje się co najmniej raz na kwartał i po istotnej zmianie; częstotliwość wymaga zatwierdzenia operatora.
