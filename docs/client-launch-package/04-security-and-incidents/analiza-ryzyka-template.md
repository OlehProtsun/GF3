# Analiza ryzyka incydentu — szablon

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | polityka bezpieczeństwa; incydenty |
| Klasyfikacja | POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Kontekst

ID, rola GF, aktywa, proces, osoby i kategorie danych: **[DO WYPEŁNIENIA]**.

## Ocena

| Scenariusz szkody | Źródło/zagrożenie | Istniejące zabezpieczenia | Prawdopodobieństwo 1–5 | Skutek 1–5 | Wynik | Ryzyko dla praw i wolności |
|---|---|---|---:|---:|---:|---|
| **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |

Uwzględnić możliwość identyfikacji, skalę, wrażliwość kontekstu pracy, dzieci/osoby podatne, odwracalność, poufność, integralność i dostępność. Progi i apetyt na ryzyko: **[DO WYPEŁNIENIA]**. Decyzja, środki, właściciel, termin, ryzyko rezydualne i akceptujący: **[DO WYPEŁNIENIA]**.

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 24–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)

## Ocena ryzyka i DPIA — stan przeglądu 2026-10-19

Przetwarzanie dotyczy pracowników pozostających w relacji zależności; generator przydziałów, szczegółowe historie i notatki mogą wpływać na prawa pracownicze. Nie potwierdzono dedykowanego modelu ML ani zewnętrznego API AI w generatorze, ale sama nazwa „algorytm” nie rozstrzyga definicji AI lub profilowania.

| Scenariusz | Skutek dla osób | Stan i postępowanie przed dopuszczeniem |
|---|---|---|
| Zgadywanie hasła lub XSS/token | Przejęcie dostępu do grafików i kontaktów | 6 cyfr i localStorage zwiększają ryzyko; hardening auth i test zagrożeń; krytyczna decyzja security |
| Błędna izolacja lub eksport | Ujawnienie personelu innego Klienta lub nieuprawnionemu odbiorcy | Role istnieją; dowód oddzielnej instancji i testów zakresu obiektowego wymagany |
| Powiadomienie na ekranie blokady, stare urządzenie | Ujawnienie miejsca, godzin lub charakteru pracy | Treść ograniczona, lecz zawiera kontekst; instrukcja użytkownika, wyłączenie urządzenia, test zmiany konta i offline |
| Treści wrażliwe w notatkach | Ujawnienie zdrowia lub innych szczególnych kategorii | Minimalizacja, instrukcje, ograniczenie dostępu, kontrolowane usunięcie; odrębna przesłanka art. 9 jeśli potrzebna |
| Bezrefleksyjna publikacja generatora | Nierówny przydział, błędny odpoczynek i wpływ na wynagrodzenie/życie prywatne | Rzeczywista kontrola menedżera, możliwość korekty i skargi; test art. 22 oraz prawa pracy |
| Utrata wolumenu, nieudany restore | Utrata planu pracy i dowodów | Lokalna rotacja nie zastępuje off-site i ćwiczenia; alternatywny kanał grafików |
| Soft-delete uznany za wymazanie | Nieuprawniona retencja i ponowne ujawnienie po restore | Przegląd profilu, relacji, historii/PDF, eksportów i kopii; rejestr usunięć |
| Zewnętrzne media i bramy push | Metadane u dostawcy, transfer poza EOG | Ustalenie roli i podstawy, blokowanie opcjonalnych mediów oraz ocena rozdziału V |

Nie nadano fikcyjnych wyników prawdopodobieństwa ani akceptacji ryzyka. Właściciel i Klient muszą uzupełnić skalę, liczbę osób, rzeczywisty zakres monitorowania, podatność osób, odbiorców, incydenty, skuteczność środków oraz prawdopodobieństwo/skutek i ryzyko pozostałe: [CLIENT_REQUIRED: kontekst zatrudnienia i skala; PRODUCTION_TEST_REQUIRED: skuteczność kontroli].

Administrator ocenia art. 35 RODO i wykaz Prezesa UODO z uwzględnieniem systematycznej oceny osób, istotności skutków, skali, danych wrażliwych i łączenia danych. Brak automatycznego obowiązku DPIA dla każdego grafiku nie oznacza zwolnienia dla danego wdrożenia. Przy prawdopodobnym wysokim ryzyku DPIA należy wykonać przed przetwarzaniem; zawiera opis, konieczność/proporcjonalność, ryzyka i środki (art. 35 ust. 7), opinię IOD gdy wyznaczony i, gdy właściwe, perspektywę osób. Wysokie ryzyko pozostałe wymaga uprzedniej konsultacji z organem według art. 36. Procesor zapewnia pomoc i dostępne informacje. Decyzja DPIA tak/nie z uzasadnieniem, datą i uprawnionym zatwierdzającym: [LAWYER_REQUIRED: ocena DPIA z Klientem].
