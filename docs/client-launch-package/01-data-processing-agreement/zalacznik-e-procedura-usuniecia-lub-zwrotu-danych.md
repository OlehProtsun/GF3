# Załącznik E – zwrot lub usunięcie danych

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA § 14; pakiet 09 |
| Klasyfikacja | UMOWNY / POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

1. Administrator składa udokumentowane Polecenie eksportu, zwrotu, usunięcia albo ograniczenia.
2. Strony potwierdzają zakres: profile, Konta, grafiki, dostępność, zamiany, komunikaty, logi, eksporty i kopie.
3. Eksport wykorzystuje wyłącznie dostępny format; pełny eksport wymaga **[DO WERYFIKACJI TECHNICZNEJ: ZAKRES I FORMAT]**.
4. Odbiorca i kanał dostawy są weryfikowani; plik jest usuwany po potwierdzeniu odbioru.
5. Dostępy i sesje są cofane, a system aktywny usuwany lub anonimizowany według instrukcji niezależnie od sporu rozliczeniowego.
6. Kopie wygasają przez rotację; opóźnione usunięcie i wyjątki prawne trafiają do protokołu.
7. Podmioty podprzetwarzające wykonują zgodne instrukcje.
8. Potwierdzenie opisuje faktycznie wykonane operacje i nie certyfikuje nieweryfikowalnego fizycznego nadpisania nośników.

## Podstawy i materiały do weryfikacji

- [RODO – art. 28, 29, 32–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020 – administrator i podmiot przetwarzający](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)

## Wybór Administratora i zakres kontroli

Administrator wybiera zwrot i usunięcie pozostających kopii albo samo usunięcie zgodnie z art. 28 ust. 3 lit. g. Usunięcia danych powierzonych nie wolno uzależniać od zapłaty spornej faktury. Własne dokumenty rozliczeniowe przechowuje się odrębnie na właściwej podstawie; spór nie uzasadnia zatrzymania całej bazy pracowników.

Usunięcie pracownika oznacza archiwizację profilu przez IsDeleted oraz usunięcie konta pracownika. Nie oznacza pełnego wymazania profilu, kontaktów, historycznych grafików, dostępności, zamian, korekt, dowodów akceptacji, eksportów lub kopii. Wykonanie prawa do usunięcia wymaga ustalenia podstaw retencji, instrukcji Klienta i odrębnej kontroli powiązań; nie należy zastępować archiwizacji deklaracją anonimizacji.

Kontrola obejmuje także PDF i potwierdzenia, historie korekt/zamian, wersje grafików, odczyty, push subscriptions i delivery, pliki wsparcia, eksporty i rekordy podprocesorów. Nie należy obiecywać twardego usunięcia dowodów blokowanego przez aktualny interfejs: potrzebna jest kontrolowana procedura techniczna i test. Klient otrzymuje wykaz wykonanych działań, wyjątków prawnych z terminem oraz plan wygaśnięcia kopii.

W fazie exit zatrzymać skaner push przed odtwarzaniem lub migracją, usunąć subskrypcje dla instancji według instrukcji i ocenić dane w bramach. Cofnięcie tokenu nie zastępuje unsubscribe. Po restore ponowić usunięcia i nie wznowić powiadomień na nieaktualne urządzenia. Terminy 14/45 dni pozostają propozycją uzależnioną od potwierdzonej wykonalności; kopie mają uzgodniony maksymalny czas, nie nieograniczone „do rotacji”.
