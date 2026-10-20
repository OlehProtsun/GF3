# Załącznik D – podmioty podprzetwarzające

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA § 8–9 |
| Klasyfikacja | UMOWNY / POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Dostawca | Usługa / rola | Zakres | Kraj/region | Poza EOG | Mechanizm | Link DPA/prywatności | Dodano | Uwagi |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [DO WYPEŁNIENIA: DOSTAWCA HOSTINGU] | hosting produkcyjny | baza i aplikacja | [DO WYPEŁNIENIA: REGION HOSTINGU] | [DO WERYFIKACJI RODO: TRANSFER] | [DO WYPEŁNIENIA: MECHANIZM] | [DO WYPEŁNIENIA: LINK] | [DO WYPEŁNIENIA: DATA] | Dostawcy nie potwierdzono w repozytorium |
| [DO WYPEŁNIENIA: DOSTAWCA SMTP] | wysyłka kodów odzyskiwania | e-mail, nazwa, kod w wiadomości | [DO WYPEŁNIENIA: REGION SMTP] | [DO WERYFIKACJI RODO: TRANSFER] | [DO WYPEŁNIENIA: MECHANIZM] | [DO WYPEŁNIENIA: LINK] | [DO WYPEŁNIENIA: DATA] | Kod używany do resetu Hasła |
| [DO WYPEŁNIENIA: DOSTAWCA ZEWNĘTRZNYCH KOPII] | docelowa kopia off-site | zaszyfrowana baza | [DO WYPEŁNIENIA: REGION] | [DO WERYFIKACJI RODO: TRANSFER] | [DO WYPEŁNIENIA: MECHANIZM] | [DO WYPEŁNIENIA: LINK] | [DO WYPEŁNIENIA: DATA] | Brak potwierdzonej kopii off-site |

Nginx, Docker i SQLite są komponentami technicznymi, a nie automatycznie Podmiotami podprzetwarzającymi. YouTube/Google może być odrębnym odbiorcą przy ładowaniu zewnętrznego osadzenia; jego rola i aktywacja wymagają oceny. **[DO WERYFIKACJI RODO: KWALIFIKACJA ZEWNĘTRZNYCH MEDIÓW]**

## Podstawy i materiały do weryfikacji

- [RODO – art. 28, 29, 32–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020 – administrator i podmiot przetwarzający](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)

## Bramki push i aktywne zawiadomienia

PushOptions.AllowedEndpointHosts przewiduje techniczne domeny FCM, Mozilla, Apple i Windows. Lista nie potwierdza wykorzystania każdej bramy, podmiotu prawnego ani umowy powierzenia. Dla każdego rzeczywistego pośrednika ustalić nazwę prawną, rolę (podprocesor lub inny odbiorca), region, metadane, warunki, dalszych odbiorców oraz transfery: [INFRASTRUCTURE_REQUIRED: kwalifikacja i dowody dostawców push]. Do czasu tej oceny nie deklarować EOG ani podpisanych SCC.

Do rejestru zmian należy wpisać planowaną datę, zakres, odbiorcę aktywnego zawiadomienia, dowód doręczenia i wynik sprzeciwu. Projekt DPA § 8 przewiduje 14 dni uprzedzenia i zakaz rozpoczęcia spornego przetwarzania do uzgodnienia rozwiązania. Pilny tryb wymaga uprzedniego szczególnego upoważnienia, nie samego powiadomienia po fakcie.
