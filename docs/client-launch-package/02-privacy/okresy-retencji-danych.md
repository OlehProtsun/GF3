# Proponowane okresy retencji

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
| Akceptacje dokumentów | czas obowiązywania i konieczny okres dowodowy ustalony prawnie | cykl kopii | potwierdzenie UTC | dowód wersji PDF | kontrolowany exit, zwykłe usunięcie blokowane | LAWYER_REQUIRED / PRODUCTION_TEST_REQUIRED |
| Kopie | 10 najnowszych, okres czasowy zmienny | ten sam zbiór | kolejna kopia | odporność | rotacja | test produkcji |
| Incydenty | [DO WERYFIKACJI RODO: OKRES] | wg archiwum | zamknięcie | wykazanie i lessons learned | kontrolowane | IOD |
| Eksporty | do potwierdzenia odbioru + krótki bufor | nie ustalono | dostawa | wyjście danych | bezpieczne usunięcie | techniczna |
| Nieudane logowania | brak potwierdzonego osobnego rejestru | nie dotyczy | próba | bezpieczeństwo | wdrożyć jeśli potrzebne | techniczna |

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 6, 12–14, 24, 28, 30 i 32](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO](https://uodo.gov.pl)

## Uzupełnienie i kryteria operacyjne

| Kategoria | Potwierdzony mechanizm / kryterium | Otwarta decyzja |
|---|---|---|
| Odczyty powiadomień | Serwer usuwa odczyty co najmniej 5-dniowe podczas cleanup; klient filtruje nowe datowane wpisy po 5 dniach; starsze formaty nie gwarantują wygaśnięcia | Test uruchamiania cleanup i migracji pozostałych wpisów |
| Doręczenia push | Usuwanie finalizacji starszych niż 5 dni przez aktywny skaner; retry do 8 prób | Limit i kontrola nieukończonych zadań oraz cyklu skanera |
| Subskrypcje push | Wyłączenie/DELETE lub 404/410; brak TTL nieaktywności | INFRASTRUCTURE_REQUIRED: maksymalny czas nieaktywności i metoda kontroli |
| Profil archiwalny | IsDeleted zachowuje profil i historię; konto usuwane | CLIENT_REQUIRED: konieczność każdej kategorii, termin, instrukcja wymazania/ograniczenia |
| Dane umowy, wsparcia i roszczeń | Relacja lub sprawa, następnie tylko niezbędny dowód do przedawnienia właściwego roszczenia | OWNER_REQUIRED / LAWYER_REQUIRED: harmonogram i przepis, bez ogólnego bezterminowego bufora |
| Dokumenty rozliczeniowe | Termin ustawowy właściwy dla statusu i dokumentu; początek biegu nie zawsze data wystawienia | ACCOUNTANT_REQUIRED: przepis, zawieszenia, KSeF i archiwum |

Retencja w UI, ważność tokenu, TTL przesyłki i fizyczne kasowanie to różne mechanizmy. Wygaśnięcie terminu widoczności komunikatu nie dowodzi wymazania. Okno eksportu 14 dni i aktywnego usunięcia 45 dni są PROPOSED CONTRACTUAL TERM; nie stosować do każdej kategorii jako istniejącego automatycznego cleanup. Maksymalny czas kopii po exit musi być ustalony, także gdy nowe kopie przestają powstawać. Dla każdego wyjątku zapisać podstawę, zakres, ograniczenie dostępu i datę ponownej oceny.
