# Projekt Umowy SaaS B2B – GF

## Status dokumentów

Pakiet jest roboczym projektem negocjacyjnym w języku polskim. Nie stanowi finalnej porady prawnej, podatkowej, księgowej, ubezpieczeniowej ani RODO. Nie potwierdza automatycznie prawa do działalności nierejestrowanej, braku ZUS, zwolnienia z VAT ani określonej klasyfikacji współpracy.

## Utworzone pliki

- `umowa-saas-b2b-pl.md` – edytowalne źródło Umowy.
- `umowa-saas-b2b-pl.html` – wersja do przeglądania, drukowania i eksportu do PDF.
- `zalacznik-1-zakres-uslug.md` – potwierdzony zakres Platformy GF.
- `checklista-przed-podpisaniem.md` – lista decyzji, weryfikacji i alternatywnych klauzul.
- `README.md` – niniejsza instrukcja.
- `generate-html.mjs` – lokalny generator HTML bez zewnętrznych pakietów.

## Edycja i generowanie HTML

1. Treść główną edytuj w `umowa-saas-b2b-pl.md`.
2. Po zmianie treści uruchom w tym katalogu `node generate-html.mjs`. Skrypt korzysta wyłącznie z modułów wbudowanych Node.js i ponownie tworzy `umowa-saas-b2b-pl.html` z osadzonym stylem wydruku.
3. Przed podpisaniem porównaj nagłówki i ponumerowane ustępy obu wersji. HTML powinien zawierać tę samą treść merytoryczną co Markdown.
4. Nie edytuj wyłącznie HTML bez przeniesienia tej samej zmiany do źródła Markdown.

W bieżącej wersji HTML został wygenerowany mechanicznie z pliku Markdown; nie wymaga zewnętrznych bibliotek, skryptów ani połączenia z internetem.

## Drukowanie i eksport do PDF

1. Otwórz `umowa-saas-b2b-pl.html` w Chrome lub Edge.
2. Wybierz `Ctrl+P` i **Zapisz jako PDF**.
3. Ustaw format A4, skalę 100% i orientację pionową.
4. Wyłącz nagłówki i stopki przeglądarki. Marginesy są zdefiniowane w stylach dokumentu.
5. Włącz grafikę tła, jeżeli podgląd jej nie drukuje.
6. Sprawdź podgląd wszystkich stron, zwłaszcza tabele, podpisy i podziały między paragrafami.

Numeracja stron wykorzystuje reguły CSS dla mediów stronicowanych. Jej obsługa zależy od silnika PDF; jeśli przeglądarka jej nie wyświetli, można włączyć numerację stron w narzędziu używanym do finalnego składu.

## Potwierdzona funkcjonalność GF

Na podstawie aktualnych tras, interfejsów i usług projektu potwierdzono:

- rozdzielenie Kont na role menedżera i pracownika;
- zarządzanie pracownikami i lokalizacjami;
- tworzenie, wspomagane generowanie, edycję i publikację grafików;
- planowanie zmian i podsumowanie czasu pracy;
- zarządzanie dostępnością i jej uzupełnianie przez pracowników;
- widok opublikowanego grafiku po stronie pracownika;
- komunikaty menedżera oraz powiadomienia o grafikach, dostępności i zamianach;
- zgłaszanie, przyjmowanie, anulowanie i historię zamian zmian;
- rejestr wybranych działań operacyjnych;
- eksport wybranych danych grafików do XLSX;
- mechanizm automatycznej kopii co godzinę podczas działania usługi i retencję 10 najnowszych kopii w kodzie aplikacji.

## Celowo wyłączone lub niepotwierdzone

- naliczanie płac, obsługa kadr, fakturowanie i rozliczenia podatkowe;
- automatyczna, kompletna kontrola zgodności grafiku z prawem pracy;
- integracje z systemami zewnętrznymi;
- standardowa migracja danych;
- natywne aplikacje mobilne i tryb offline;
- eksport PDF z aktualnego interfejsu;
- publiczny interfejs integracyjny dla Klienta;
- gwarantowane SLA, 24/7, czas naprawy lub 100% dostępności;
- samodzielna rejestracja i rozliczanie wielu niezależnych firm;
- pełna zgodność z RODO bez odrębnego audytu i DPA.

## Klauzule wymagające przeglądu prawnego

- status i oznaczenie osoby wykonującej działalność nierejestrowaną;
- prawo pobytu oraz uprawnienie do wykonywania działalności, jeżeli dotyczy;
- zakres licencji i zakazów dotyczących oprogramowania;
- model odpowiedzialności i limit równy 6 miesiącom opłat;
- zasady zawieszenia, rozwiązania i formy oświadczeń;
- pięcioletnia poufność;
- właściwość sądu;
- forma aneksów;
- okres pobrania, usunięcia i anonimizacji danych;
- role RODO, umowa powierzenia, subprocesorzy i lokalizacja przetwarzania;
- zgodność przyszłego Regulaminu z ustawą o świadczeniu usług drogą elektroniczną.

## Klauzule wymagające przeglądu księgowego lub podatkowego

- dopuszczalność i warunki działalności nierejestrowanej;
- NIP lub PESEL używany do rozliczeń;
- status VAT i ewentualna podstawa zwolnienia;
- dokument księgowy wystawiany Klientowi;
- podatek dochodowy, ewidencja sprzedaży i rachunek bankowy;
- obowiązki Klienta jako płatnika podatku lub składek;
- klasyfikacja współpracy dla ZUS, także po utracie statusu studenta;
- sposób rozliczenia niepełnego miesiąca i ewentualnego wejścia w VAT.

## Pozycje wymagające potwierdzenia technicznego

- produkcyjny adres Platformy;
- liczba Użytkowników i zakres konfiguracji początkowej;
- wspierane przeglądarki, wersje i urządzenia;
- produkcyjne działanie, monitoring, przechowanie i odtwarzanie kopii;
- dostępny zakres eksportu XLSX;
- okres pobrania danych po zakończeniu i termin usunięcia;
- lokalizacja przetwarzania i lista dostawców pomocniczych;
- adresy wsparcia oraz zgłoszeń Incydentów;
- proces utworzenia Administratora Klienta i separacja danych środowisk.

## Odrębne dokumenty do przygotowania

- Regulamin świadczenia usług drogą elektroniczną;
- Umowa powierzenia przetwarzania danych osobowych;
- Polityka prywatności;
- Polityka cookies;
- Protokół uruchomienia Platformy;
- opcjonalne SLA;
- opcjonalny formularz akceptacji oferty.

## Wyszukiwanie pól do uzupełnienia

W całym katalogu wyszukaj wyrażenie `[` lub dokładniejsze prefiksy:

- `[DO WYPEŁNIENIA:`
- `[DO WERYFIKACJI PRAWNEJ:`
- `[DO WERYFIKACJI KSIĘGOWEJ:`
- `[DO WERYFIKACJI TECHNICZNEJ:`
- `[DO WERYFIKACJI Z KSIĘGOWYM LUB PRAWNIKIEM]`

Aktualny, pełny inwentarz pozostałych placeholderów znajduje się w sekcji „Inwentarz placeholderów” poniżej i powinien zostać odświeżony po każdej zmianie dokumentów.

## Inwentarz placeholderów

<!-- PLACEHOLDER_INVENTORY_START -->

- `[DO WERYFIKACJI KSIĘGOWEJ I PRAWNEJ]`
- `[DO WERYFIKACJI KSIĘGOWEJ: RODZAJ DOKUMENTU KSIĘGOWEGO]`
- `[DO WERYFIKACJI KSIĘGOWEJ: SPOSÓB ROZLICZENIA, VAT, PODATEK, ZUS I EWENTUALNE OBOWIĄZKI PŁATNIKA]`
- `[DO WERYFIKACJI KSIĘGOWEJ]`
- `[DO WERYFIKACJI PRAWNEJ: CZY DLA ANEKSÓW WYMAGAĆ FORMY PISEMNEJ, ELEKTRONICZNEJ Z PODPISEM KWALIFIKOWANYM CZY DOKUMENTOWEJ]`
- `[DO WERYFIKACJI PRAWNEJ: LIMIT ODPOWIEDZIALNOŚCI – WERSJA GŁÓWNA 6 MIESIĘCY; ROZWAŻYĆ 3, 6 LUB 12 MIESIĘCY]`
- `[DO WERYFIKACJI PRAWNEJ: STATUS I SPOSÓB IDENTYFIKACJI USŁUGODAWCY]`
- `[DO WERYFIKACJI PRAWNEJ: WŁAŚCIWOŚĆ SĄDU]`
- `[DO WERYFIKACJI PRAWNEJ]`
- `[DO WERYFIKACJI TECHNICZNEJ I PRAWNEJ: LOKALIZACJA PRZETWARZANIA DANYCH]`
- `[DO WERYFIKACJI TECHNICZNEJ I PRAWNEJ: TERMIN USUNIĘCIA DANYCH – PROPOZYCJA 45 DNI]`
- `[DO WERYFIKACJI TECHNICZNEJ I PRAWNEJ]`
- `[DO WERYFIKACJI TECHNICZNEJ: CZĘSTOTLIWOŚĆ KOPII ZAPASOWYCH – W KODZIE CO GODZINĘ PODCZAS DZIAŁANIA USŁUGI]`
- `[DO WERYFIKACJI TECHNICZNEJ: DOSTĘPNY FORMAT EKSPORTU – OBECNIE POTWIERDZONO ARKUSZ XLSX DLA WYBRANYCH DANYCH GRAFIKU]`
- `[DO WERYFIKACJI TECHNICZNEJ: LISTA OBSŁUGIWANYCH PRZEGLĄDAREK I WERSJI]`
- `[DO WERYFIKACJI TECHNICZNEJ: OKRES NA POBRANIE DANYCH PO ZAKOŃCZENIU UMOWY – PROPOZYCJA 14 DNI]`
- `[DO WERYFIKACJI TECHNICZNEJ: OKRES RETENCJI KOPII – W KODZIE LIMIT 10 NAJNOWSZYCH KOPII]`
- `[DO WERYFIKACJI TECHNICZNEJ: SPOSÓB ODTWARZANIA, TESTY ODTWORZENIOWE I ODPOWIEDZIALNA OSOBA]`
- `[DO WERYFIKACJI TECHNICZNEJ: WYMAGANIA DLA URZĄDZEŃ]`
- `[DO WERYFIKACJI Z KSIĘGOWYM LUB PRAWNIKIEM]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL DO FAKTUR]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL DO INCYDENTÓW DANYCH OSOBOWYCH]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL DO KONTAKTU]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL KONTAKTU OPERACYJNEGO KLIENTA]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL KONTAKTU PRAWNEGO KLIENTA]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL USŁUGODAWCY]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL WSPARCIA]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL]`
- `[DO WYPEŁNIENIA: ADRES PLATFORMY]`
- `[DO WYPEŁNIENIA: ADRES SIEDZIBY]`
- `[DO WYPEŁNIENIA: ADRES ZAMIESZKANIA]`
- `[DO WYPEŁNIENIA: CENNIK USŁUG DODATKOWYCH / WYCENA INDYWIDUALNA]`
- `[DO WYPEŁNIENIA: DATA DPA]`
- `[DO WYPEŁNIENIA: DATA ROZPOCZĘCIA OBOWIĄZYWANIA]`
- `[DO WYPEŁNIENIA: DATA UMOWY]`
- `[DO WYPEŁNIENIA: DATA URUCHOMIENIA]`
- `[DO WYPEŁNIENIA: DATA]`
- `[DO WYPEŁNIENIA: FORMA PRAWNA]`
- `[DO WYPEŁNIENIA: IMIĘ I NAZWISKO]`
- `[DO WYPEŁNIENIA: KRS]`
- `[DO WYPEŁNIENIA: LICZBA EGZEMPLARZY]`
- `[DO WYPEŁNIENIA: LICZBA UŻYTKOWNIKÓW LUB ZASADA LIMITU]`
- `[DO WYPEŁNIENIA: LIMIT POCZĄTKOWYCH DANYCH LUB UŻYTKOWNIKÓW]`
- `[DO WYPEŁNIENIA: LISTA SUBPROCESORÓW LUB MIEJSCE JEJ UDOSTĘPNIENIA]`
- `[DO WYPEŁNIENIA: MAKSYMALNA LICZBA POCZĄTKOWYCH UŻYTKOWNIKÓW]`
- `[DO WYPEŁNIENIA: MIEJSCOWOŚĆ]`
- `[DO WYPEŁNIENIA: NIP – JEŻELI NADANY]`
- `[DO WYPEŁNIENIA: NIP]`
- `[DO WYPEŁNIENIA: NUMER RACHUNKU BANKOWEGO]`
- `[DO WYPEŁNIENIA: NUMER TELEFONU]`
- `[DO WYPEŁNIENIA: OSOBA KONTAKTOWA KLIENTA]`
- `[DO WYPEŁNIENIA: OSOBA KONTAKTOWA USŁUGODAWCY]`
- `[DO WYPEŁNIENIA: OSOBA REPREZENTUJĄCA KLIENTA]`
- `[DO WYPEŁNIENIA: OSOBA REPREZENTUJĄCA]`
- `[DO WYPEŁNIENIA: PEŁNA NAZWA SPÓŁKI]`
- `[DO WYPEŁNIENIA: PEŁNA OPŁATA / PROPORCJONALNIE / INNA ZASADA]`
- `[DO WYPEŁNIENIA: PESEL – TYLKO JEŻELI KONIECZNY]`
- `[DO WYPEŁNIENIA: PIERWSZY DZIEŃ PIERWSZEGO OKRESU ROZLICZENIOWEGO]`
- `[DO WYPEŁNIENIA: PLANOWANA DATA URUCHOMIENIA]`
- `[DO WYPEŁNIENIA: PODSTAWA REPREZENTACJI]`
- `[DO WYPEŁNIENIA: REGON]`
- `[DO WYPEŁNIENIA: WŁAŚCIWA PODSTAWA PRAWNA]`
- `[DO WYPEŁNIENIA: ZAKRES KONFIGURACJI POCZĄTKOWEJ]`
- `[DO WYPEŁNIENIA: ZAKRES KONFIGURACJI]`
- `[DO WYPEŁNIENIA: ZDALNIE / MATERIAŁ / SPOTKANIE]`
- `[DO WYPEŁNIENIA]`

<!-- PLACEHOLDER_INVENTORY_END -->

## Punkty odniesienia prawnego

Przy przygotowaniu projektu sprawdzono oficjalne teksty: Prawo przedsiębiorców (w szczególności art. 5), ustawę o świadczeniu usług drogą elektroniczną (w szczególności art. 8), Kodeks cywilny w zakresie formy dokumentowej oraz art. 28 RODO. Przed podpisaniem należy ponownie sprawdzić aktualny stan prawny i zastosowanie przepisów do konkretnych Stron.

- [Prawo przedsiębiorców – ELI](https://eli.gov.pl/eli/DU/2018/646/ogl)
- [Ustawa o świadczeniu usług drogą elektroniczną – tekst jednolity, ELI](https://eli.gov.pl/eli/DU/2024/1513/ogl)
- [Kodeks cywilny – ELI](https://eli.gov.pl/eli/DU/1964/93/ogl)
- [RODO – EUR-Lex](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
