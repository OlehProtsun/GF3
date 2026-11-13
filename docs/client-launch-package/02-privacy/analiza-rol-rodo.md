# Analiza ról RODO

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA; polityka prywatności |
| Klasyfikacja | WEWNĘTRZNY / RODO |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Cel | Dane | Osoby | Rola Usługodawcy | Proponowana podstawa | Retencja | Odbiorcy | Weryfikacja |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Grafiki, profile, dostępność, zamiany, komunikaty | dane pracowników | Użytkownicy | Podmiot przetwarzający | Polecenie i DPA | wg Klienta/DPA | hosting, SMTP wg zakresu | [DO WERYFIKACJI RODO: ROLE] |
| Umowa i kontakt B2B | dane reprezentantów | kontakty Klienta | odrębny Administrator | art. 6 ust. 1 lit. b/f – propozycja | [DO WERYFIKACJI RODO: RETENCJA] | księgowość, doradcy | podstawa i balans |
| Rozliczenia i dokumentacja | dane kontaktowe i podatkowe | Strony | odrębny Administrator | art. 6 ust. 1 lit. c – propozycja | okres ustawowy | księgowość, organy | obowiązki krajowe |
| Roszczenia | umowa i komunikacja | Strony | odrębny Administrator | art. 6 ust. 1 lit. f – propozycja | przedawnienie plus bufor | prawnicy, sądy | balans |
| Bezpieczeństwo własnej infrastruktury | Konto, logi, Incydenty | Użytkownicy | do analizy: Administrator lub procesor | art. 6 ust. 1 lit. f / DPA | [DO WERYFIKACJI RODO: RETENCJA LOGÓW] | hosting | rozdzielić cele |
| Wsparcie | kontakt i treść zgłoszenia | zgłaszający | zależnie od treści | Umowa/DPA lub lit. f | [DO WERYFIKACJI RODO: RETENCJA WSPARCIA] | osoby wsparcia | procedura kwalifikacji |

Nie należy przyjmować jednej roli dla wszystkich operacji. Dane pracownicze pozostają co do zasady pod kontrolą Klienta; własne rozliczenia, kontrakt i roszczenia są odrębnymi celami Usługodawcy.
## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 6, 12–14, 24, 28, 30 i 32](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO](https://uodo.gov.pl)

## Kwalifikacja operacji i kryteria

Podstawy administratora, źródła, odbiorcy, prawa i kryteria retencji są opisane per cel w § 2–8 polityki prywatności; nie są podstawą samodzielnego wykorzystywania danych przez procesora. Czynności techniczne kont, push, historii i dowodów regulaminu na polecenie Klienta mieszczą się w DPA. Dowód potwierdzenia zasad nie zamienia relacji powierzenia w niezależne administrowanie.

Log organizacji pracy służący Klientowi jest powierzeniem. Zabezpieczenie infrastruktury dla samodzielnie określonego celu Operatora może stanowić odrębną czynność administratora po ocenie celu, niezbędności i równowagi. Ten sam zapis użyty w obu celach wymaga rozdzielenia dostępu, retencji i informacji. Zgłoszenie wsparcia zawiera kontakt obsługiwany we własnym celu oraz możliwe dane pracowników dostępne tylko na instrukcję; nie kwalifikuje się całej skrzynki jedną rolą.

Art. 9 wymaga odrębnej przesłanki, jeżeli wolny tekst zawiera dane szczególne; nie zastępuje jej zakaz w regulaminie. Podstawy dla pracowników, zleceniobiorców i menedżerów mogą się różnić. Klient potwierdza podstawy i realną kontrolę generatora. Push jest wyborem kanału na urządzeniu, a nie zgodą na wszystkie cele. Wytyczne EROD 07/2020 wymagają oceny rzeczywistych celów i zasadniczych sposobów, niezależnie od nazwy strony w umowie.

Status kwalifikacji: REQUIRES LEGAL REVIEW; konfiguracja dostawców: UNVERIFIED EXTERNAL FACT. Brak dowodów VERIFIED IN DEPLOYMENT w tym przeglądzie.
