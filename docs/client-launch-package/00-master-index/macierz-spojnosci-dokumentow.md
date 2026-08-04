# Macierz spójności dokumentów

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
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
| Funkcje | harmonogramy/organizacja | Zał. 1 | dostęp i zasady | tylko funkcje potwierdzone w repo | brak self-registration i logu akceptacji |
| Cookies/media | brak | brak | wymagania techniczne | localStorage + zewnętrzne obrazy/YouTube | wariant B lub wyłączenie |

Nie zmieniono dokumentów istniejących. Wszelkie korekty do nich należy wykonać osobno, z wersją i wpisem w changelogu.

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
