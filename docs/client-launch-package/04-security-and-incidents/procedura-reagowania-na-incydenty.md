# Procedura reagowania na incydenty

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
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

## Decyzja o naruszeniu i terminy ustawowe

Naruszenie danych obejmuje przypadkowe lub niezgodne z prawem zniszczenie, utratę, zmianę, ujawnienie albo dostęp; może dotyczyć także dostępności bez wycieku. Rejestruje się moment wykrycia oraz moment uzyskania wystarczającej pewności naruszenia. Nie każdy błąd techniczny jest naruszeniem danych.

Procesor informuje Administratora bez zbędnej zwłoki po stwierdzeniu, nie czeka 72 godzin ani na pełny raport. Pierwsze informacje i kolejne uzupełnienia zawierają charakter, przybliżoną liczbę osób i rekordów, konsekwencje, kontakt i środki. Administrator zawiadamia właściwy organ bez zbędnej zwłoki, w miarę możliwości do 72 godzin od stwierdzenia, chyba że jest mało prawdopodobne ryzyko naruszenia praw lub wolności. Przy opóźnieniu wyjaśnia przyczyny; informacje może przekazywać etapami. Każde naruszenie dokumentuje zgodnie z art. 33 ust. 5, również niezgłoszone.

Przy wysokim ryzyku Administrator zawiadamia osoby bez zbędnej zwłoki jasnym językiem, z informacją o skutkach, kontakcie i środkach, chyba że zachodzi i została udokumentowana przesłanka art. 34 ust. 3. Szyfrowanie chroni tylko w swoim rzeczywistym zakresie; nie zwalnia automatycznie przy przejętym koncie, ekranie blokady lub ujawnionych kluczach.

Triage GF obejmuje ujawnienie JWT, zgadywanie krótkich haseł, błędne uprawnienia i izolację, eksporty, utratę SQLite/kopii oraz endpoint/klucze push i powiadomienia na wspólnym urządzeniu. Przy push zatrzymać wysyłkę i ocenić rzeczywistego odbiorcę, a nie tylko wynik „sent”. Zabezpieczyć minimalne dowody bez publikowania payloadów i endpointów. Kanały stron, zastępstwa i potwierdzenie ćwiczenia: [OWNER_REQUIRED: kontakty incydentowe; PRODUCTION_TEST_REQUIRED: test eskalacji]. Podstawy: RODO art. 4 pkt 12, 33–34; materiały UODO https://uodo.gov.pl/pl/525/2584.
