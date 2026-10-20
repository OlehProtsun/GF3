# Załącznik A – opis przetwarzania

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA; Załącznik E |
| Klasyfikacja | UMOWNY / POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Element | Opis |
| --- | --- |
| Przedmiot | Obsługa Platformy GF dla Klienta |
| Czas | Okres Umowy SaaS plus eksport, usuwanie i rotacja kopii |
| Charakter | Zapis, organizowanie, odczyt, zmiana, udostępnienie uprawnionym, eksport, kopia, usunięcie |
| Cel | Grafiki, dostępność, zamiany, komunikacja, Konta, bezpieczeństwo i wsparcie |
| Moduły | Pracownicy, lokalizacje, dostępność, grafiki, zamiany, komunikaty, powiadomienia, logi, eksport |
| Częstotliwość | Ciągła podczas korzystania; kopia w kodzie co godzinę podczas działania procesu |
| Region hostingu | [DO WERYFIKACJI TECHNICZNEJ: REGION PRODUKCYJNY] |
| Kopie | SQLite; 10 najnowszych automatycznych kopii w tym samym obszarze danych – stan kodu |
| Dostęp wsparcia | [DO WYPEŁNIENIA: ROLE I PROCEDURA DOSTĘPU PRODUKCYJNEGO] |
| Eksport | Wybrane XLSX i SQL; pełny eksport do ustalenia |
| Usunięcie | Zgodnie z Załącznikiem E i instrukcją Administratora |

## Podstawy i materiały do weryfikacji

- [RODO – art. 28, 29, 32–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020 – administrator i podmiot przetwarzający](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)

## Web Push i PWA — zakres przetwarzania

Przetwarzanie obejmuje service worker /sw.js i subskrypcję danego urządzenia, endpoint, p256dh/auth, ID pracownika/subskrypcji, daty utworzenia i aktualizacji oraz rekordy doręczeń: ID powiadomienia, liczbę prób, termin kolejnej próby, dzierżawę zadania, wynik i datę finalizacji. Kody i endpointy traktuje się jako poufne identyfikatory i nie publikuje w dowodach audytu. Powiadomienia obejmują grafiki, dostępność, zamiany, wolne zmiany i przyjęcie zamiany; treść może ujawniać nazwę, okres, termin lub godziny pracy na ekranie blokady. Język PL/EN wynika z konta.

Włączenie jest działaniem pracownika i wymaga permission przeglądarki; jest ono odrębne od podstawy RODO i od oceny art. 399 PKE. Wyłączenie dla urządzenia podejmuje usunięcie subskrypcji serwera i unsubscribe w przeglądarce; błąd sieci może pozostawić rekord serwera. Cofnięcie samego permission nie gwarantuje usunięcia. Odpowiedź bramy 404/410 powoduje usunięcie subskrypcji; usunięcie rekordu subskrypcji usuwa zależne doręczenia. Brak potwierdzonego TTL nieaktywnej subskrypcji wymaga decyzji retencyjnej.

Skaner działa co około 30 sekund przy włączonej usłudze; pomija zdarzenia sprzed opt-in i odczytane. Przejściowe błędy są ponawiane do 8 prób; ostateczny wynik „sent” oznacza przyjęcie wysyłki przez usługę, nie odczyt na urządzeniu. Zakończone doręczenia są usuwane po 5 dniach od finalizacji podczas skanów; zadań nieukończonych i subskrypcji nie obejmuje ten warunek. TTL wysyłki jest ograniczony do godziny dla ofert zamian/wolnych zmian i do 5 dni dla pozostałych zdarzeń, zależnie od wieku zdarzenia.

Web Push szyfruje treść i używa HTTPS, ale dostawcy bram przeglądarki/systemu mogą otrzymywać metadane transmisji. Konfiguracja dopuszcza hosty FCM, Mozilla, Apple i Windows; nie jest to dowód zawartej umowy, konkretnego operatora prawnego, regionu lub transferu. Należy ustalić rolę każdego rzeczywistego dostawcy i podstawę rozdziału V przed produkcją. Brak gwarancji dostarczenia; nie używać jako jedynego kanału skutecznego zawiadomienia pracownika.

## Potwierdzenia i archiwizacja

Platforma rejestruje osobiste potwierdzenie opublikowanej wersji PDF przez konto menedżera lub pracownika: identyfikator konta, rolę, migawkę loginu i nazwy oraz czas UTC. Dokument ma wersję, SHA-256 i czas publikacji; opublikowany dokument jest niezmienny, a przyjęte lub opublikowane dokumenty nie podlegają zwykłemu usunięciu. Dostępna jest historia własna i historia pracownika dla menedżera. Middleware zwraca HTTP 428 dla chronionych operacji API przy oczekujących dokumentach, z wyjątkami uwierzytelnienia, pobrania i potwierdzenia dokumentów oraz wskazanych tras. Gate prezentuje PDF i pole domyślnie niezaznaczone. Nie dowodzi to przeczytania każdej strony ani kwalifikowanego podpisu; nie podpisuje Umowy SaaS lub DPA w imieniu Klienta i nie jest ogólną zgodą RODO. Tylko prawnie zweryfikowany PDF może trafić do publikacji produkcyjnej; ten przegląd niczego nie publikuje.

Usunięcie pracownika oznacza archiwizację profilu przez IsDeleted oraz usunięcie konta pracownika. Nie oznacza pełnego wymazania profilu, kontaktów, historycznych grafików, dostępności, zamian, korekt, dowodów akceptacji, eksportów lub kopii. Wykonanie prawa do usunięcia wymaga ustalenia podstaw retencji, instrukcji Klienta i odrębnej kontroli powiązań; nie należy zastępować archiwizacji deklaracją anonimizacji.
