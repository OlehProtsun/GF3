# Procedura reagowania na incydenty

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | procedura naruszenia; formularz; rejestr |
| Klasyfikacja | POUFNY WEWNĘTRZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## 1. Zgłoszenie i triage

Każdy sygnał otrzymuje identyfikator, czas wykrycia, zgłaszającego i właściciela. Nie przesyłać haseł, tokenów ani pełnych danych osobowych. Zachować dowody w sposób kontrolowany; nie wykonywać destrukcyjnych działań bez kopii i decyzji.

| Klasa | Przykład | Reakcja docelowa |
|---|---|---|
| SEV1 krytyczny | aktywne ujawnienie danych, przejęcie uprzywilejowanego konta, całkowita utrata danych | natychmiastowa eskalacja **[DO WYPEŁNIENIA: CZAS]** |
| SEV2 wysoki | istotna niedostępność, podejrzenie naruszenia ograniczonego zakresu | **[DO WYPEŁNIENIA: CZAS]** |
| SEV3 średni | błąd z obejściem, ograniczony wpływ | w godzinach wsparcia |
| SEV4 niski | pytanie, kosmetyka | kolejka planowa |

## 2. Cykl

1. Zarejestruj i potwierdź odbiór.
2. Ogranicz skutki (np. unieważnij sesję, izoluj komponent) z zapisem decyzji.
3. Ustal zakres, oś czasu, dane i osoby dotknięte; odróżnij incydent bezpieczeństwa od naruszenia danych.
4. Powiadom IOD/prawnika i Klienta zgodnie z rolą procesora.
5. Usuń przyczynę, bezpiecznie przywróć i monitoruj.
6. Udokumentuj komunikację, dowody i decyzję o zamknięciu.
7. W ciągu **[DO WYPEŁNIENIA: TERMIN POST-MORTEM]** przeprowadź analizę przyczyn i plan działań.

## 3. Szablon przeglądu po incydencie

Oś czasu; wykrycie i co zadziałało/nie zadziałało; przyczyna bez obwiniania osób; wpływ; decyzje i komunikacja; zabezpieczenia doraźne; działania trwałe z właścicielem/terminem; dowód testu; ryzyko rezydualne i zatwierdzający: **[DO WYPEŁNIENIA]**.

## 4. Komunikacja

Kanał awaryjny: **[DO WYPEŁNIENIA: KANAŁ]**. Rzecznik: **[DO WYPEŁNIENIA: OSOBA]**. Komunikat do Klienta zawiera fakty, zakres, skutki, podjęte środki, zalecenia i kolejny termin aktualizacji; nie zawiera spekulacji. Publiczna komunikacja wymaga decyzji właściciela i konsultacji prawnej.

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 24–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
