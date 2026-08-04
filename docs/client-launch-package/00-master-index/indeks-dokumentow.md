# Indeks dokumentów pakietu pierwszego Klienta

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | cały pakiet; istniejące dokumenty |
| Klasyfikacja | WEWNĘTRZNY / DO UZGODNIENIA |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Zakres

Pakiet zawiera 81 wymaganych artefaktów w 11 sekcjach. Plik techniczny `generate-package.mjs` służy wyłącznie deterministycznemu odtworzeniu pakietu i nie jest dokumentem klienta. Wszystkie artefakty mają status DRAFT.

| Lp. | Ścieżka | Tytuł | Cel | Odbiorca | Właściciel | Przed demo | Przed podpisaniem | Przed produkcją | Status |
|---:|---|---|---|---|---|---|---|---|---|
| 1 | `../commercial-offer/` | Oferta handlowa — wersja autorytatywna | Prezentacja handlowa | Klient | Właściciel SaaS | Tak | Tak | Nie | Istniejący DRAFT — niezmieniony |
| 2 | `../legal/electronic-services-regulations/` | Regulamin i informacja o zagrożeniach — wersja autorytatywna | UŚUDE i zasady Użytkownika | Użytkownicy | Usługodawca / prawnik | Tak | Tak | Tak | Istniejący DRAFT — niezmieniony |
| 3 | `../legal/saas-agreement/` | Umowa SaaS i Załącznik nr 1 — wersja autorytatywna | Kontrakt główny | Strony | Usługodawca / prawnik | Nie | Tak | Tak | Istniejący DRAFT — niezmieniony |
| 4 | `00-master-index/changelog.md` | changelog.md | Nawigacja i kontrola | Właściciel pakietu / recenzenci | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 5 | `00-master-index/indeks-dokumentow.md` | indeks-dokumentow.md | Nawigacja i kontrola | Właściciel pakietu / recenzenci | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 6 | `00-master-index/macierz-spojnosci-dokumentow.md` | macierz-spojnosci-dokumentow.md | Nawigacja i kontrola | Właściciel pakietu / recenzenci | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 7 | `00-master-index/raport-brakow-i-ryzyk.md` | raport-brakow-i-ryzyk.md | Nawigacja i kontrola | Właściciel pakietu / recenzenci | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 8 | `00-master-index/README.md` | README.md | Nawigacja i kontrola | Właściciel pakietu / recenzenci | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 9 | `00-master-index/rejestr-placeholderow.md` | rejestr-placeholderow.md | Nawigacja i kontrola | Właściciel pakietu / recenzenci | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 10 | `01-data-processing-agreement/checklista-dpa.md` | Checklista DPA | Kontrola przed podpisaniem | Usługodawca / Klient | Prawnik / IOD | Nie | Tak | Tak | DRAFT |
| 11 | `01-data-processing-agreement/README.md` | README – DPA | Instrukcja pakietu powierzenia | Właściciel pakietu | IOD | Nie | Tak | Tak | DRAFT |
| 12 | `01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.html` | UMOWA POWIERZENIA PRZETWARZANIA DANYCH OSOBOWYCH — HTML | Wersja do druku A4 | Strony / Użytkownicy | Właściciel dokumentu | Tak | Tak | Tak | DRAFT |
| 13 | `01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md` | UMOWA POWIERZENIA PRZETWARZANIA DANYCH OSOBOWYCH | Umowa z art. 28 RODO | Administrator i Podmiot przetwarzający | Usługodawca / prawnik / IOD | Nie | Tak | Tak | DRAFT |
| 14 | `01-data-processing-agreement/zalacznik-a-opis-przetwarzania.md` | Załącznik A – opis przetwarzania | Opis art. 28 RODO | Strony DPA | IOD / właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 15 | `01-data-processing-agreement/zalacznik-b-kategorie-danych-i-osob.md` | Załącznik B – kategorie danych i osób | Inwentarz DPA | Strony DPA | IOD | Nie | Tak | Tak | DRAFT |
| 16 | `01-data-processing-agreement/zalacznik-c-srodki-techniczne-i-organizacyjne.md` | Załącznik C – środki techniczne i organizacyjne | Macierz TOM | Strony DPA / audytor | Właściciel techniczny / IOD | Nie | Tak | Tak | DRAFT |
| 17 | `01-data-processing-agreement/zalacznik-d-podmioty-podprzetwarzajace.md` | Załącznik D – podmioty podprzetwarzające | Lista dostawców danych | Administrator | Usługodawca / IOD | Nie | Tak | Tak | DRAFT |
| 18 | `01-data-processing-agreement/zalacznik-e-procedura-usuniecia-lub-zwrotu-danych.md` | Załącznik E – zwrot lub usunięcie danych | Procedura zakończenia DPA | Strony DPA | Właściciel techniczny / IOD | Nie | Tak | Tak | DRAFT |
| 19 | `02-privacy/analiza-rol-rodo.md` | Analiza ról RODO | Rozdział ról administrator/procesor | Usługodawca / Klient / IOD | IOD | Nie | Tak | Tak | DRAFT |
| 20 | `02-privacy/klauzula-informacyjna-kontakty-b2b-pl.md` | Klauzula informacyjna – kontakty B2B | Informacja art. 13/14 | Reprezentanci i kontakty Klienta | Usługodawca / IOD | Nie | Tak | Tak | DRAFT |
| 21 | `02-privacy/klauzula-informacyjna-uzytkownicy-platformy-pl.md` | Klauzula informacyjna – Użytkownicy Platformy | Informacja dla pracowników | Menedżerowie i pracownicy | Klient / IOD | Nie | Nie | Tak | DRAFT |
| 22 | `02-privacy/mapa-danych-osobowych.md` | Mapa danych osobowych | Inwentarz danych w kodzie | IOD / techniczny właściciel | IOD | Nie | Nie | Tak | DRAFT |
| 23 | `02-privacy/okresy-retencji-danych.md` | Proponowane okresy retencji | Harmonogram retencji | IOD / Klient | IOD | Nie | Nie | Tak | DRAFT |
| 24 | `02-privacy/polityka-prywatnosci-pl.html` | POLITYKA PRYWATNOŚCI PLATFORMY GF — HTML | Wersja do druku A4 | Strony / Użytkownicy | Właściciel dokumentu | Tak | Nie | Tak | DRAFT |
| 25 | `02-privacy/polityka-prywatnosci-pl.md` | POLITYKA PRYWATNOŚCI PLATFORMY GF | Publiczna informacja prywatności | Odwiedzający, kontakty i Użytkownicy | Usługodawca / IOD | Tak | Nie | Tak | DRAFT |
| 26 | `02-privacy/README.md` | README – prywatność | Instrukcja dokumentów prywatności | Właściciel pakietu | IOD | Nie | Nie | Tak | DRAFT |
| 27 | `02-privacy/rejestr-kategorii-czynnosci-przetwarzania.md` | Rejestr kategorii czynności przetwarzania – procesor | Projekt art. 30 ust. 2 RODO | Podmiot przetwarzający / IOD | IOD | Nie | Nie | Tak | DRAFT |
| 28 | `03-cookies-and-storage/audyt-cookies-i-storage.md` | Audyt cookies i pamięci przeglądarki | Audyt kodu storage i stron trzecich | Techniczny / IOD | Właściciel techniczny | Tak | Nie | Tak | DRAFT |
| 29 | `03-cookies-and-storage/polityka-cookies-pl.html` | POLITYKA COOKIES I PAMIĘCI PRZEGLĄDARKI GF — HTML | Wersja do druku A4 | Strony / Użytkownicy | Właściciel dokumentu | Tak | Nie | Tak | DRAFT |
| 30 | `03-cookies-and-storage/polityka-cookies-pl.md` | POLITYKA COOKIES I PAMIĘCI PRZEGLĄDARKI GF | Publiczna polityka technologii | Użytkownicy | Usługodawca / IOD | Tak | Nie | Tak | DRAFT |
| 31 | `03-cookies-and-storage/README.md` | README – cookies i storage | Instrukcja audytu | Właściciel pakietu | IOD / techniczny | Tak | Nie | Tak | DRAFT |
| 32 | `03-cookies-and-storage/rekomendacja-mechanizmu-zgody.md` | Rekomendacja mechanizmu zgody | Decyzja cookies/storage | Właściciel SaaS / IOD | IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 33 | `03-cookies-and-storage/wykaz-cookies-i-storage.md` | Wykaz cookies i storage | Inwentarz technologii przeglądarki | Użytkownicy / IOD | Techniczny właściciel | Tak | Nie | Tak | DRAFT |
| 34 | `04-security-and-incidents/analiza-ryzyka-template.md` | Analiza ryzyka incydentu — szablon | Szablon oceny | IOD / właściciel ryzyka | Prawnik / techniczny | Nie | Nie | Tak | DRAFT |
| 35 | `04-security-and-incidents/checklista-72-godziny.md` | Checklista 72 godziny | Checklista naruszenia | Koordynator incydentu / IOD | IOD / prawnik | Nie | Nie | Tak | DRAFT |
| 36 | `04-security-and-incidents/formularz-zgloszenia-incydentu.md` | Formularz zgłoszenia incydentu | Formularz operacyjny | Zgłaszający / koordynator | IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 37 | `04-security-and-incidents/polityka-bezpieczenstwa-danych.md` | Polityka bezpieczeństwa danych GF | Polityka wewnętrzna | Właściciel SaaS | IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 38 | `04-security-and-incidents/procedura-naruszenia-ochrony-danych.md` | Procedura naruszenia ochrony danych osobowych | Procedura RODO | Właściciel SaaS / IOD | IOD / prawnik | Nie | Nie | Tak | DRAFT |
| 39 | `04-security-and-incidents/procedura-reagowania-na-incydenty.md` | Procedura reagowania na incydenty | Procedura operacyjna | Właściciel SaaS | IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 40 | `04-security-and-incidents/README.md` | README — bezpieczeństwo i incydenty | Instrukcja użycia | Właściciel pakietu | IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 41 | `04-security-and-incidents/rejestr-incydentow-template.csv` | rejestr-incydentow-template.csv | Szablon rejestru | Właściciel procesu | Właściciel SaaS | Nie | Nie | Nie | DRAFT |
| 42 | `05-backup-and-continuity/checklista-testu-odtworzeniowego.md` | Checklista testu odtworzeniowego | Checklista kontrolna | Techniczny wykonawca | Właściciel SaaS | Nie | Nie | Tak | DRAFT |
| 43 | `05-backup-and-continuity/plan-ciaglosci-dzialania-light.md` | Plan ciągłości działania — light | Plan operacyjny | Właściciel SaaS | Techniczny / IOD | Nie | Nie | Tak | DRAFT |
| 44 | `05-backup-and-continuity/procedura-kopii-zapasowych.md` | Procedura kopii zapasowych | Procedura operacyjna | Techniczny właściciel usługi | Właściciel SaaS / IOD | Nie | Nie | Tak | DRAFT |
| 45 | `05-backup-and-continuity/procedura-odtwarzania-danych.md` | Procedura odtwarzania danych | Runbook wysokiego poziomu | Techniczny właściciel usługi | Właściciel SaaS / IOD | Nie | Nie | Tak | DRAFT |
| 46 | `05-backup-and-continuity/README.md` | README — backup i ciągłość | Instrukcja użycia | Właściciel pakietu | Techniczny / IOD | Nie | Nie | Tak | DRAFT |
| 47 | `05-backup-and-continuity/rejestr-testow-backup-template.csv` | rejestr-testow-backup-template.csv | Szablon rejestru | Właściciel procesu | Właściciel SaaS | Nie | Nie | Nie | DRAFT |
| 48 | `06-client-launch/checklista-onboardingu-klienta.md` | Checklista onboardingu Klienta | Checklista wdrożenia | Koordynator wdrożenia | Klient / techniczny | Nie | Nie | Tak | DRAFT |
| 49 | `06-client-launch/checklista-onboardingu-uzytkownikow.md` | Checklista onboardingu Użytkowników | Checklista dla Klienta | Manager Klienta | Koordynator wdrożenia | Nie | Nie | Tak | DRAFT |
| 50 | `06-client-launch/formularz-danych-wdrozeniowych.md` | Formularz danych wdrożeniowych | Formularz Klienta | Klient | Koordynator wdrożenia | Nie | Nie | Tak | DRAFT |
| 51 | `06-client-launch/formularz-osob-kontaktowych.md` | Formularz osób kontaktowych | Formularz stron | Klient / Usługodawca | Koordynator | Nie | Nie | Tak | DRAFT |
| 52 | `06-client-launch/potwierdzenie-akceptacji-dokumentow.md` | Potwierdzenie udostępnienia i akceptacji dokumentów | Dowód organizacyjny | Klient / Usługodawca | Prawnik / IOD | Nie | Nie | Tak | DRAFT |
| 53 | `06-client-launch/protokol-przekazania-dostepu-administratora.md` | Protokół przekazania dostępu managera | Protokół bezpieczeństwa | Usługodawca / Klient | Techniczny | Nie | Nie | Tak | DRAFT |
| 54 | `06-client-launch/protokol-uruchomienia-platformy.html` | Protokół uruchomienia Platformy GF — HTML | Wersja do druku A4 | Strony / Użytkownicy | Właściciel dokumentu | Tak | Tak | Tak | DRAFT |
| 55 | `06-client-launch/protokol-uruchomienia-platformy.md` | Protokół uruchomienia Platformy GF | Protokół dwustronny | Usługodawca / Klient | Prawnik / techniczny | Tak | Nie | Tak | DRAFT |
| 56 | `06-client-launch/README.md` | README — uruchomienie Klienta | Instrukcja kolejności | Koordynator wdrożenia | Klient / prawnik / techniczny | Nie | Nie | Tak | DRAFT |
| 57 | `07-support/formularz-zgloszenia-bledu.md` | Formularz zgłoszenia błędu | Formularz Klienta | Zgłaszający | Wsparcie | Nie | Nie | Tak | DRAFT |
| 58 | `07-support/formularz-zgloszenia-zmiany.md` | Formularz zgłoszenia zmiany | Formularz biznesowy | Klient | Właściciel produktu | Nie | Nie | Tak | DRAFT |
| 59 | `07-support/instrukcja-zglaszania-problemow-dla-klienta.md` | Instrukcja zgłaszania problemów dla Klienta | Instrukcja użytkowa | Usługodawca | Klient | Nie | Nie | Tak | DRAFT |
| 60 | `07-support/klasyfikacja-zgloszen.md` | Klasyfikacja zgłoszeń | Macierz operacyjna | Wsparcie | Klient | Nie | Nie | Tak | DRAFT |
| 61 | `07-support/README.md` | README — wsparcie | Instrukcja użycia | Właściciel pakietu | Klient / prawnik | Nie | Nie | Tak | DRAFT |
| 62 | `07-support/zasady-wsparcia-technicznego.md` | Zasady wsparcia technicznego | Załącznik operacyjny | Usługodawca | Klient / prawnik | Nie | Nie | Tak | DRAFT |
| 63 | `08-billing-and-records/checklista-rozliczen-miesiecznych.md` | Checklista rozliczeń miesięcznych | Checklista księgowa | Usługodawca | Księgowy | Nie | Nie | Tak | DRAFT |
| 64 | `08-billing-and-records/ewidencja-kosztow-template.csv` | ewidencja-kosztow-template.csv | Szablon rejestru | Właściciel procesu | Właściciel SaaS | Nie | Nie | Nie | DRAFT |
| 65 | `08-billing-and-records/ewidencja-sprzedazy-template.csv` | ewidencja-sprzedazy-template.csv | Szablon rejestru | Właściciel procesu | Właściciel SaaS | Nie | Nie | Nie | DRAFT |
| 66 | `08-billing-and-records/instrukcja-wystawiania-dokumentu-sprzedazy.md` | Instrukcja wystawiania dokumentu sprzedaży | Projekt księgowy | Usługodawca | Księgowy / doradca podatkowy | Nie | Nie | Tak | DRAFT |
| 67 | `08-billing-and-records/README.md` | README — rozliczenia i ewidencje | Instrukcja użycia | Usługodawca | Księgowy | Nie | Nie | Tak | DRAFT |
| 68 | `08-billing-and-records/rejestr-platnosci-template.csv` | rejestr-platnosci-template.csv | Szablon rejestru | Właściciel procesu | Właściciel SaaS | Nie | Nie | Nie | DRAFT |
| 69 | `08-billing-and-records/warianty-rozliczenia-do-konsultacji.md` | Warianty rozliczenia do konsultacji | Macierz decyzyjna | Usługodawca | Księgowy / prawnik | Nie | Nie | Tak | DRAFT |
| 70 | `08-billing-and-records/wzor-faktury-pl.html` | Wzór faktury PL | Alternatywny szablon sprzedaży | Usługodawca / Klient | Księgowy | Nie | Tak | Tak | DRAFT |
| 71 | `08-billing-and-records/wzor-rachunku-pl.html` | Wzór rachunku PL | Alternatywny szablon sprzedaży | Usługodawca / Klient | Księgowy | Nie | Tak | Tak | DRAFT |
| 72 | `08-billing-and-records/zasady-numeracji-dokumentow.md` | Zasady numeracji dokumentów | Projekt księgowy | Usługodawca | Księgowy | Nie | Nie | Tak | DRAFT |
| 73 | `09-data-exit/checklista-dezaktywacji-klienta.md` | Checklista dezaktywacji Klienta | Checklista kontrolna | Koordynator exit | Klient / IOD | Nie | Nie | Tak | DRAFT |
| 74 | `09-data-exit/procedura-zakonczenia-wspolpracy.md` | Procedura zakończenia współpracy | Procedura offboardingu | Usługodawca / Klient | Prawnik / IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 75 | `09-data-exit/protokol-eksportu-danych.md` | Protokół eksportu danych | Protokół przekazania | Usługodawca / Klient | IOD / techniczny | Nie | Nie | Tak | DRAFT |
| 76 | `09-data-exit/protokol-usuniecia-danych.md` | Protokół usunięcia danych | Protokół wykonania | Usługodawca | Klient / IOD | Nie | Nie | Tak | DRAFT |
| 77 | `09-data-exit/README.md` | README — data exit | Instrukcja użycia | Właściciel pakietu | Prawnik / IOD | Nie | Nie | Tak | DRAFT |
| 78 | `10-final-review/checklista-gotowosci-do-podpisania.md` | Checklista gotowości do podpisania | Checklista przeglądu | Prawnik | Właściciel SaaS | Nie | Tak | Tak | DRAFT |
| 79 | `10-final-review/checklista-gotowosci-do-produkcji.md` | Checklista gotowości do produkcji | Checklista IOD i bezpieczeństwa | IOD / techniczny | Klient / właściciel SaaS | Nie | Nie | Tak | DRAFT |
| 80 | `10-final-review/finalny-raport-gotowosci.md` | Finalny raport gotowości | Raport bramkowy | Właściciel pakietu | Prawnik / IOD / księgowy / techniczny | Nie | Tak | Tak | DRAFT |
| 81 | `10-final-review/lista-decyzji-wlasciciela-saas.md` | Lista decyzji właściciela SaaS | Rejestr zarządczy | Właściciel SaaS | Doradcy domenowi | Nie | Tak | Tak | DRAFT |
| 82 | `10-final-review/pytania-do-klienta.md` | Pytania do Klienta przed startem | Lista uzgodnień | Koordynator wdrożenia | Klient | Nie | Nie | Tak | DRAFT |
| 83 | `10-final-review/pytania-do-ksiegowego.md` | Pytania do księgowego i doradcy podatkowego | Lista konsultacyjna | Usługodawca | Księgowy / doradca podatkowy | Nie | Tak | Tak | DRAFT |
| 84 | `10-final-review/pytania-do-prawnika.md` | Pytania do prawnika | Lista konsultacyjna | Właściciel SaaS | Prawnik | Nie | Tak | Tak | DRAFT |

## Istniejące wersje autorytatywne — niepowielone

- `docs/commercial-offer/` — oferta handlowa;
- `docs/legal/saas-agreement/` — Umowa SaaS i Załącznik nr 1;
- `docs/legal/electronic-services-regulations/` — Regulamin i informacja o zagrożeniach.

Przed użyciem sprawdzić rejestr placeholderów, raport ryzyk i finalny raport gotowości.
