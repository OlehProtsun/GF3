# Załącznik B – kategorie danych i osób

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA § 4 |
| Klasyfikacja | UMOWNY / POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Osoby
| Kategoria | Potwierdzenie | Uwagi |
| --- | --- | --- |
| Pracownicy i byli pracownicy | Tak | Historia może zachować nazwę po zmianach |
| Menedżerowie / Administrator Klienta | Tak | Konto, nazwa wyświetlana, e-mail odzyskiwania |
| Reprezentanci i kontakty Klienta | Poza główną bazą | Umowa, rozliczenia, wsparcie |
| Zaproszeni Użytkownicy | Nie | Brak systemu zaproszeń |

## Dane
| Kategoria | Przykłady | Moduł |
| --- | --- | --- |
| Identyfikacyjne i kontaktowe | imię, nazwisko, telefon, e-mail, nazwa Użytkownika, identyfikator | profile i Konta |
| Zatrudnieniowo-organizacyjne | lokalizacja, grafik, godziny, przydziały | grafiki |
| Dostępność i zamiany | dni, przedziały, strony zamiany, status, czas | dostępność i zamiany |
| Komunikacja | tytuł, treść, autor, potwierdzenie odczytu | komunikaty |
| Uwierzytelnianie | skrót Hasła i kodu, czasy resetu/logowania, wersja sesji | Konta |
| Operacyjne | aktor, rola, działanie, czas; ustawienia UI | logi i preferencje |

## Ryzyko danych szczególnych i skazań

Nie potwierdzono pól przeznaczonych dla zdrowia, niepełnosprawności, biometrii, religii, poglądów, związków zawodowych, orientacji, życia seksualnego ani skazań. Pola notatek, nazw i komunikatów mogą przyjąć dowolny tekst; należy zakazać danych nadmiarowych i wdrożyć instrukcje. **[DO WERYFIKACJI RODO: CZY KLIENT PLANUJE DANE SZCZEGÓLNYCH KATEGORII LUB SKAZAŃ]**

## Podstawy i materiały do weryfikacji

- [RODO – art. 28, 29, 32–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020 – administrator i podmiot przetwarzający](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)

## Dodatkowe wdrożone kategorie

| Kategoria | Dane i osoby | Cel |
|---|---|---|
| Korekty i historie | Wniosek, autor, status, czas, treść, zmiany i wersje grafiku; pracownicy/menedżerowie | Organizacja pracy i odtworzenie zmian |
| Regulamin | PDF, wersja, SHA-256, publikacja; account ID/rola, username/display-name snapshot, acceptedAt UTC | Udostępnienie i dowód osobistego potwierdzenia |
| Web Push | Employee ID, endpoint, p256dh/auth, subscription ID, daty; delivery ID, notification ID, próby, status i finalizacja | Opcjonalny kanał powiadomień urządzenia |
| Pamięć urządzenia | Token i login, preferencje, odczyty, employee ID dla push i termin ponowienia zaproszenia; subskrypcja i permission | Sesja, ustawienia i działanie push |

Kategorie dotyczą także byłych użytkowników, których historia lub profil nie zostały wymazane. Nie oznacza to zgody na bezterminowe przechowywanie. Szczegółowe nazwy kluczy i czasy wskazuje wykaz storage; nie dodaje on nowych celów do DPA.
