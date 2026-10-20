# Macierz spójności dokumentów

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | oferta; Umowa; Regulamin; DPA; billing |
| Klasyfikacja | WEWNĘTRZNY / DO UZGODNIENIA |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

| Temat | Oferta | Umowa/Zał. 1 | Regulamin | DPA/pakiet | Stan i działanie |
|---|---|---|---|---|---|
| Produkt | GF | GF | GF | GF | spójne; repo historycznie GF3 |
| Strony | osoba / Klient B2B | Usługodawca / Klient | Usługodawca / Użytkownik | procesor / Administrator | uzupełnić dane i reprezentację |
| Cena | 500,00 zł/miesiąc | 500,00 zł z góry, 7 dni | odwołanie do Umowy | 500,00 zł bez założenia VAT | księgowy określa prezentację |
| Czas i wypowiedzenie | oferta | bezterminowo, 1 miesiąc na koniec miesiąca | do czasu konta/Umowy | DPA związana z Umową | prawnik zatwierdza |
| Wsparcie | opis ogólny | pierwsza odpowiedź 2 Dni Robocze | kanały Usługodawcy | cele SLA otwarte | nie obiecywać czasu naprawy |
| Odpowiedzialność | skrót | limit roboczy 6 miesięcy opłat | zgodnie z Umową | bez osobnego limitu | przegląd prawny |
| Poufność | skrót | 5 lat roboczo | bezpieczeństwo Użytkownika | dostęp need-to-know | przegląd prawny |
| Dane/role | ogólne | Klient odpowiada za dane | zasady Użytkownika | Klient=administrator, GF=procesor; własne cele osobno | IOD zatwierdza |
| Retencja/exit | brak szczegółu | eksport 14 dni, aktywne usunięcie 45 dni — projekty | usunięcie konta wg Umowy | DPA E + cykl backupu | zsynchronizować po teście |
| Funkcje | harmonogramy/organizacja | Zał. 1 | dostęp i zasady | funkcje potwierdzone w repo, w tym korekty/historia/push | brak self-registration; istnieją wersje PDF, potwierdzenia i historia |
| Cookies/media | brak | brak | wymagania techniczne | localStorage + zewnętrzne obrazy/YouTube | wariant B lub wyłączenie |

Przegląd 2026-10-19 harmonizuje istniejące dokumenty i ich pochodne; wersje i zakres zmian odnotowano w changelogu i raporcie zakończenia.

## Szczegółowa macierz kontroli

| Temat | Dokumenty | Obecne brzmienie | Konflikt | Rekomendowane brzmienie autorytatywne | Działanie |
|---|---|---|---|---|---|
| Nazwa/definicje | wszystkie | GF; Platforma/Usługa SaaS | historyczna nazwa repo GF3 | prawna nazwa produktu GF; terminy ze słownika pakietu | potwierdzić właściciel |
| Strony i zawiadomienia | Umowa, Regulamin, DPA, wsparcie | pola puste | brak danych publicznych i kanałów | Usługodawca/Klient oraz Administrator/procesor zależnie od roli | prawnik + właściciel |
| Cena i moment rozliczenia | oferta, Umowa, billing | 500,00 zł miesięcznie; z góry; 7 dni | nieustalone VAT/netto/brutto i data pierwszego okresu | „500,00 zł należne” do decyzji księgowej; start od protokołu | księgowy + Strony |
| E-mail/godziny/wsparcie | Umowa, Regulamin, support | 2 Dni Robocze; kanały/godziny puste | cele P1/P2 nieuzgodnione | pierwsza odpowiedź ≠ naprawa; bez obietnicy 24/7 | wpisać SLA i kontakty |
| Start/użytkownicy/pilot | oferta, Zał. 1, launch | liczby i data puste | brak limitu/pilota | wyłącznie parametry podpisane w formularzu i protokole | uzgodnić Klient |
| Funkcje/custom work | Zał. 1, DPA, support | potwierdzone funkcje GF; zmiany osobno | brak | abonament nie obejmuje custom development bez zamówienia | zachować |
| IP/kod/Dane Klienta | Umowa, DPA, exit | brak przeniesienia kodu; dane pozostają Klienta | brak | dostęp SaaS bez przeniesienia praw/kodu; eksport tylko danych | prawnik zatwierdza |
| Role RODO/podprocesorzy/region | DPA, privacy | Klient administrator, GF procesor; lista pusta | hosting/SMTP/transfer niepotwierdzone | wyłącznie zweryfikowani dostawcy, regiony i mechanizmy | techniczny + IOD |
| Backup/retencja/export/delete | Umowa, DPA E, backup, exit | ok. godzinowo/10 kopii; 14/45 dni roboczo | brak off-site i finalnego cyklu backupu | parametry po teście restore i decyzji Stron | techniczny + IOD + prawnik |
| Wypowiedzenie/odpowiedzialność | Umowa, DPA | 1 miesiąc na koniec miesiąca; limit roboczy 6 miesięcy | DPA nie tworzy nowego limitu | Umowa nadrzędna poza obowiązkami art. 28; bez wyłączenia winy umyślnej | prawnik |
| Hierarchia | Umowa, Zał. 1, Regulamin, DPA | częściowo opisana | wymaga finalnego uporządkowania | DPA pierwsza dla powierzenia; Umowa dla handlowych; Zał. 1 dla zakresu | prawnik |

## Mapa źródeł kanonicznych — przegląd 2026-10-19

Status całego zestawu: **DRAFT — REVIEW-READY redakcyjnie i technicznie po walidacji; bez zatwierdzenia prawnego i bez zgody produkcyjnej**. REVIEW-READY oznacza przygotowanie treści do pracy recenzenta, nie zamknięcie zewnętrznych faktów i STOP-SHIP. GF jest produktem, GF3 identyfikatorem technicznym.

| Warstwa | Źródło merytoryczne od katalogu repozytorium | Relacja i pierwszeństwo |
|---|---|---|
| Umowa B2B | docs/legal/saas-agreement/umowa-saas-b2b-pl.md i zalacznik-1-zakres-uslug.md | Jeden negocjowany SaaS; parametry handlowe są propozycją, nie faktem sprzedaży |
| Pełny regulamin | docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md | Publiczny /legal/regulamin.html jest pełną reprezentacją, nie konkurencyjnym skrótem |
| DPA | docs/client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md oraz A–E | Pierwszeństwo w sprawach ochrony danych; zgoda osoby korzystającej z konta nie zawiera DPA |
| Prywatność i urządzenie | docs/client-launch-package/02-privacy/polityka-prywatnosci-pl.md; 03-cookies-and-storage/polityka-cookies-pl.md | Publiczne HTML pod /legal/ są stabilną reprezentacją tej treści; informacja pracodawcy pozostaje odrębna |
| Publiczne strony uzupełniające | FrontEnd/public/legal/index.html, zasady-korzystania.html, podwykonawcy.html, bezpieczenstwo.html | Uzupełniające informacje; nie samodzielny zamiennik SaaS/DPA lub źródło prywatnych danych |
| Ewidencja wewnętrzna | Analiza ról, mapa, retencja, rejestry, COOKIE_AUDIT, ryzyko i procedury oraz raport 2026-10-19 | Fakty kodu, interpretacje i luki; nie publikować poufnych rejestrów w public assets |
| Wzory alternatywne | docs/legal/templates | Wskazane krótkie DPA/TOM/umowa/klauzula/SLA są historycznymi wzorami pomocniczymi; aktywne zobowiązania pochodzą z dokumentów kanonicznych |
| Operacje / oferta | docs/client-launch-package/04–10; docs/commercial-offer | Procedury, formularze i materiał handlowy; wypełnione umowy i dowody poza Git; nie zmieniają samodzielnie kontraktu |

Bezwzględnie obowiązujące prawo jest nadrzędne. DPA ma pierwszeństwo w ochronie danych; w pozostałym zakresie indywidualny aneks, SaaS, uzgodniony zakres, regulamin i pomocnicze zasady. Wybranie pustego szablonu nie tworzy równoległych zobowiązań.

## Edycja i generacja

Edytować Markdown dokumentu kanonicznego. Generatory saas-agreement i electronic-services-regulations aktualizują jego prywatny HTML oraz informacje o pliku w README. generate-package.mjs czyta istniejące źródła, aktualizuje cztery pochodne HTML, publiczny regulamin/privacy/cookies oraz rejestr pól; **nie odtwarza i nie nadpisuje Markdown starymi wzorami**. Pozostałe publiczne HTML są bezpośrednio redagowane. Zmiana publicznej treści unieważnia poprzednie zatwierdzenie dokładnych bajtów; nie wpisano nowego APPROVED ani zatwierdzonego SHA.

Odrębny PDF do potwierdzeń przygotować wyłącznie po przeglądzie: wydruk zatwierdzonej treści, kontrola każdej strony, numeru, daty i hasha, archiwum i akceptacja uprawnionych osób. Nie kopiować do PDF gate Umowy SaaS, DPA, TOMs, faktur, podpisów lub informacji prywatnej. W tym zadaniu nie wygenerowano i nie wgrano PDF, nie publikowano dokumentów i nie zmieniono infrastruktury.
