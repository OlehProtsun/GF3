# Procedura kopii zapasowych

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | odtwarzanie; BCP; DPA C |
| Klasyfikacja | POUFNY WEWNĘTRZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## 1. Stan zastany

Kod potwierdza automatyczną kopię bazy SQLite mniej więcej raz na godzinę podczas działania aplikacji, wykonaną mechanizmem backup SQLite, oraz zachowanie 10 najnowszych kopii automatycznych. Baza i kopie znajdują się w przestrzeni danych aplikacji/nazwanym wolumenie. Nie potwierdzono kopii poza tym wolumenem, szyfrowania at-rest, alarmowania ani produkcyjnego testu odtworzenia.

## 2. Stan wymagany

| Kontrola | Parametr do decyzji |
|---|---|
| Zakres | baza, konfiguracja odtwarzalna i niezbędne pliki; bez sekretów w jawnej kopii |
| RPO i częstotliwość | **[DO WYPEŁNIENIA: RPO]** / **[DO WYPEŁNIENIA: HARMONOGRAM]** |
| Retencja wielopoziomowa | **[DO WYPEŁNIENIA: DZIENNA/TYGODNIOWA/MIESIĘCZNA]** |
| Druga lokalizacja | **[DO WERYFIKACJI TECHNICZNEJ: DOSTAWCA I REGION]** |
| Szyfrowanie i klucze | **[DO WERYFIKACJI TECHNICZNEJ]** |
| Monitoring i właściciel alarmu | **[DO WYPEŁNIENIA]** |

## 3. Wykonanie i kontrola

Kopia automatyczna nie zastępuje kopii odseparowanej. Każde zadanie zapisuje czas, wynik, rozmiar, identyfikator, retencję i wynik kontroli integralności. Dostęp minimalny, transfer szyfrowany, klucze oddzielone. Po błędzie eskalacja według SEV i ponowienie po usunięciu przyczyny. Usuwanie po retencji jest kontrolowane i udokumentowane.

## 4. Testy

Test odtworzeniowy co **[DO WYPEŁNIENIA: CZĘSTOTLIWOŚĆ]**, po zmianie mechanizmu i przed pierwszą produkcją. Wynik w rejestrze; nie testować na jedynej kopii ani nadpisywać produkcji.

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 24–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
