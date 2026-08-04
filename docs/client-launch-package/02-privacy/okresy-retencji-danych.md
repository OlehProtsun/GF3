# Proponowane okresy retencji

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | DPA § 14; pakiet 09 |
| Klasyfikacja | WEWNĘTRZNY / RODO |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Kategoria | Proponowana aktywna retencja | Kopie | Wyzwalacz | Powód | Usunięcie | Weryfikacja |
| --- | ---: | ---: | --- | --- | --- | --- |
| Aktywne Konta | czas upoważnienia | rotacja kopii | odebranie dostępu | dostęp | dezaktywacja/usunięcie | RODO/techniczna |
| Konta nieaktywne | [DO WERYFIKACJI RODO: OKRES] | rotacja | dezaktywacja | roszczenia/audyt | anonimizacja/usunięcie | prawna |
| Grafiki, dostępność, zamiany | [DO WERYFIKACJI RODO: WYMÓG KLIENTA] | rotacja | koniec okresu/Umowy | organizacja i prawo pracy po stronie Klienta | CRUD/procedura | Klient/prawnik |
| Komunikaty i powiadomienia | termin widoczności plus [DO WERYFIKACJI RODO] | rotacja | deadline | komunikacja | CRUD/cleanup | techniczna |
| Logi operacyjne/bezpieczeństwa | [DO WERYFIKACJI RODO: OKRES LOGÓW] | rotacja | zapis | bezpieczeństwo/roszczenia | ustawienia i delete | RODO/techniczna |
| Reset Hasła | kod 15 minut; metadane [DO WERYFIKACJI RODO] | rotacja | użycie/wygaśnięcie | bezpieczeństwo | czyszczenie pól | techniczna |
| Wsparcie | [DO WERYFIKACJI RODO: OKRES] | wg poczty | zamknięcie | obsługa/roszczenia | skrzynka/rejestr | prawna |
| Umowy i rozliczenia | [DO WERYFIKACJI PRAWNEJ I PODATKOWEJ: OKRES] | archiwum | zakończenie/rok | obowiązki i roszczenia | kontrolowane | księgowa |
| Akceptacje dokumentów | czas używania plus [DO WERYFIKACJI PRAWNEJ] | docelowo | akceptacja | dowód | rejestr | brak implementacji |
| Kopie | 10 najnowszych, okres czasowy zmienny | ten sam zbiór | kolejna kopia | odporność | rotacja | test produkcji |
| Incydenty | [DO WERYFIKACJI RODO: OKRES] | wg archiwum | zamknięcie | wykazanie i lessons learned | kontrolowane | IOD |
| Eksporty | do potwierdzenia odbioru + krótki bufor | nie ustalono | dostawa | wyjście danych | bezpieczne usunięcie | techniczna |
| Nieudane logowania | brak potwierdzonego osobnego rejestru | nie dotyczy | próba | bezpieczeństwo | wdrożyć jeśli potrzebne | techniczna |

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 6, 12–14, 24, 28, 30 i 32](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO](https://uodo.gov.pl)
