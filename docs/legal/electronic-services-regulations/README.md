# Regulamin świadczenia usług drogą elektroniczną – pakiet roboczy GF

## Status

Pakiet jest polskim projektem do przeglądu prawnego, technicznego i operacyjnego. Nie jest finalną poradą prawną i nie potwierdza automatycznie zgodności Platformy ani procesu akceptacji z ustawą o świadczeniu usług drogą elektroniczną, RODO lub innymi przepisami.

## Utworzone pliki

- `regulamin-swiadczenia-uslug-elektronicznych-pl.md` – edytowalne źródło Regulaminu.
- `regulamin-swiadczenia-uslug-elektronicznych-pl.html` – samodzielna wersja do publikacji, przeglądania i druku.
- `informacja-o-zagrozeniach-pl.md` – zrozumiała informacja o ryzykach i środkach ostrożności.
- `checklista-wdrozenia-regulaminu.md` – lista prac publikacyjnych, prawnych i technicznych wraz z właścicielami placeholderów.
- `README.md` – instrukcja obsługi pakietu.
- `generate-html.mjs` – lokalny generator HTML bez zależności zewnętrznych.

## Regulamin a Umowa SaaS

Umowa o świadczenie usług SaaS reguluje stosunek handlowy między Usługodawcą a Klientem: abonament, okres współpracy, wdrożenie, zakres płatnych funkcji, odpowiedzialność i wyjście z danych. Regulamin opisuje korzystanie z Platformy przez Klienta i upoważnionych Użytkowników: Konta, wymagania techniczne, bezpieczeństwo, zakazane działania, reklamacje, zawieszenie i zakończenie dostępu.

Użytkownik będący pracownikiem lub menedżerem Klienta nie nabywa samodzielnie abonamentu i nie jest zobowiązany do zapłaty 500 zł. W sprawach handlowych pierwszeństwo ma indywidualnie uzgodniona Umowa SaaS; w sprawach powierzonego przetwarzania – Umowa powierzenia.

## Edycja placeholderów

1. Uzupełnić pola zaczynające się od `[DO WYPEŁNIENIA:`.
2. Przekazać pola `[DO WERYFIKACJI PRAWNEJ:` prawnikowi, a pola `[DO WERYFIKACJI TECHNICZNEJ:` właścicielowi aplikacji lub specjaliście bezpieczeństwa.
3. Użyć tabeli w sekcji 5 checklisty, która wskazuje plik, paragraf, wymaganą informację i osobę odpowiedzialną.
4. Po każdej zmianie uruchomić generator i ponownie przejrzeć inwentarz poniżej.
5. Nie publikować danych osobowych Usługodawcy bez świadomej decyzji co do zakresu wymaganego prawem.

## Generowanie i otwieranie HTML

1. Edytować wyłącznie źródło `regulamin-swiadczenia-uslug-elektronicznych-pl.md`.
2. W tym katalogu uruchomić `node generate-html.mjs`.
3. Otworzyć `regulamin-swiadczenia-uslug-elektronicznych-pl.html` bezpośrednio w przeglądarce. Plik nie wymaga serwera, JavaScriptu, CDN ani internetu.
4. Sprawdzić tabelę treści, odnośniki do wszystkich 24 paragrafów, wersję, daty i stopkę.

## Eksport do PDF

1. Otworzyć HTML w Chrome lub Edge.
2. Wybrać `Ctrl+P` i „Zapisz jako PDF”.
3. Ustawić A4, skalę 100% i orientację pionową.
4. Wyłączyć nagłówki i stopki przeglądarki, pozostawiając stopkę dokumentu.
5. Sprawdzić wszystkie strony, zwłaszcza podziały przed nagłówkami i listy.

## Publikacja na Platformie

1. Dodać publiczną trasę `/regulamin` oraz stabilny publiczny adres pliku HTML lub PDF.
2. Umieścić link „Regulamin” na ekranie logowania – jest to najważniejsze miejsce, ponieważ Regulamin ma być dostępny przed rozpoczęciem korzystania.
3. Powtórzyć link w stałej stopce lub menu pomocy po zalogowaniu.
4. Obok udostępnić „Informację o szczególnych zagrożeniach”, Politykę prywatności i – jeżeli dotyczy – Politykę cookies.
5. Zachować każdą opublikowaną wersję pod niezmiennym adresem archiwalnym; nie podmieniać po cichu wcześniej zaakceptowanej treści.

Nie wykonano zmian aplikacji, wdrożenia ani publikacji.

## Rejestrowanie akceptacji

Aktualny kod nie zapisuje akceptacji Regulaminu, numeru zaakceptowanej wersji ani daty akceptacji. Po przeglądzie prawnym rekomendowane wdrożenie obejmuje nieoznaczone z góry pole przy pierwszym logowaniu lub aktywacji, bezpośredni link do pełnej treści oraz zapis Użytkownika, Klienta, wersji i czasu akceptacji. Historyczne treści muszą pozostać odtwarzalne.

## Funkcjonalność potwierdzona w repozytorium

- dwie techniczne role: menedżer i pracownik;
- tworzenie Kont pracowników i menedżerów przez uprawnionego menedżera, bez samodzielnej rejestracji;
- logowanie oraz odzyskiwanie Hasła za pomocą kodu wysyłanego na zapisany adres e-mail;
- edycja przez pracownika własnego e-maila odzyskiwania i telefonu;
- zarządzanie pracownikami i lokalizacjami;
- zbieranie dostępności pracowników;
- tworzenie, wspomagane generowanie, edycja, publikowanie i wycofywanie grafików;
- podgląd grafików, zmian, godzin i obsady;
- zamiany zmian i historia zaakceptowanych zamian;
- komunikaty menedżera oraz ich odczyt lub zamknięcie przez pracownika;
- powiadomienia w interfejsie o grafikach, dostępności, zamianach i komunikatach;
- wybrane logi działań;
- eksport menedżerski XLSX i SQL;
- usuwanie pracownika, jeżeli pozwalają na to zależności, oraz unieważnianie jego aktywnych sesji.

Administrator Klienta jest pojęciem organizacyjnym. Aktualna Platforma nie ma odrębnej technicznej roli administratora – operacje administracyjne są wykonywane przez Konta menedżerskie.

## Funkcje celowo wyłączone lub niepotwierdzone

- samodzielna rejestracja Użytkownika;
- zaproszenia i obowiązkowa weryfikacja e-mail lub telefonu przed aktywacją;
- wymuszona zmiana Hasła przy pierwszym logowaniu;
- odrębny stan dezaktywacji Konta pracownika niezależny od jego usunięcia;
- samodzielne usunięcie Konta przez pracownika;
- zapis akceptacji Regulaminu i jego wersji;
- ogólny czat, komentarze pracownicze i przesyłanie plików;
- zgłaszanie ogólnej korekty grafiku inne niż potwierdzone zamiany zmian i dostępność;
- e-mail, SMS lub powiadomienia push jako ogólny kanał zdarzeń Platformy;
- oficjalna lista wspieranych wersji przeglądarek;
- automatyczna pełna kontrola zgodności grafiku z prawem pracy;
- obsługa konsumentów oraz indywidualna sprzedaż abonamentu pracownikom.

## Klauzule wymagające przeglądu prawnego

- oznaczenie i status Usługodawcy wykonującego działalność nierejestrowaną;
- moment i charakter relacji z Użytkownikiem upoważnionym przez Klienta;
- sposób udostępnienia, akceptacji i dowodzenia wersji Regulaminu;
- adekwatność uwierzytelniania do kategorii danych;
- procedura bezprawnych treści, zawieszenia i reklamacji;
- proponowany termin 14 dni na kompletną reklamację;
- role RODO, dezaktywacja Konta, żądania Użytkowników i retencja;
- obowiązki informacyjne i Polityka prywatności;
- termin zawiadamiania o zmianach i przesłanki ponownej akceptacji;
- kompletność Regulaminu względem aktualnego art. 8 ustawy o świadczeniu usług drogą elektroniczną.

## Pozycje wymagające weryfikacji technicznej

- docelowa macierz uprawnień Administratora Klienta i menedżerów;
- wspierane przeglądarki, wersje, systemy i urządzenia;
- użycie pamięci lokalnej oraz ewentualnych niezbędnych plików cookie w produkcji;
- ocena sześciocyfrowego Hasła i dodatkowych zabezpieczeń;
- kontrolowany proces dezaktywacji Konta bez utraty powiązanych danych;
- produkcyjne adresy wsparcia, reklamacji i Incydentów;
- proces klasyfikacji, eskalacji i monitorowania Incydentów;
- eksport, retencja, usuwanie danych, kopie zapasowe i separacja środowisk;
- wdrożenie publicznego Regulaminu i rejestru akceptacji.

## Odrębne dokumenty nadal wymagane

- Umowa o świadczenie usług SaaS i Załącznik nr 1 – już przygotowane jako osobny pakiet roboczy;
- Umowa powierzenia przetwarzania danych osobowych;
- Polityka prywatności;
- Polityka cookies, jeżeli wykorzystywane mechanizmy tego wymagają;
- produkcyjna procedura obsługi Incydentów;
- opcjonalne SLA;
- protokół uruchomienia Platformy.

## Inwentarz placeholderów

Inwentarz jest aktualizowany automatycznie przez `generate-html.mjs`. Szczegółowy przydział odpowiedzialności znajduje się w sekcji 5 checklisty.

<!-- PLACEHOLDER_INVENTORY_START -->

- `[DO WERYFIKACJI PRAWNEJ I OPERACYJNEJ: TERMIN ROZPATRZENIA KOMPLETNEJ REKLAMACJI – PROPOZYCJA 14 DNI]`
- `[DO WERYFIKACJI PRAWNEJ: CHARAKTER RELACJI USŁUGODAWCY Z UŻYTKOWNIKIEM UPOWAŻNIONYM PRZEZ KLIENTA]`
- `[DO WERYFIKACJI PRAWNEJ: CZY WYMAGANIA UWIERZYTELNIANIA SĄ ADEKWATNE DO KATEGORII PRZETWARZANYCH DANYCH]`
- `[DO WERYFIKACJI PRAWNEJ: MOMENT ZAWARCIA UMOWY O ŚWIADCZENIE USŁUG ELEKTRONICZNYCH Z UŻYTKOWNIKIEM I SPOSÓB DOKUMENTOWANIA AKCEPTACJI]`
- `[DO WERYFIKACJI PRAWNEJ: PODZIAŁ RÓL, OBOWIĄZKI INFORMACYJNE I OBSŁUGA PRAW OSÓB, KTÓRYCH DANE DOTYCZĄ]`
- `[DO WERYFIKACJI PRAWNEJ: RELACJA ADMINISTRATOR–PODMIOT PRZETWARZAJĄCY PRZY DEZAKTYWACJI KONTA I ŻĄDANIACH UŻYTKOWNIKA]`
- `[DO WERYFIKACJI PRAWNEJ: SPOSÓB OZNACZENIA USŁUGODAWCY ORAZ DOPUSZCZALNOŚĆ WSKAZANIA DZIAŁALNOŚCI NIEREJESTROWANEJ]`
- `[DO WERYFIKACJI PRAWNEJ: TERMIN POWIADOMIENIA I PRZESŁANKI PONOWNEJ AKCEPTACJI REGULAMINU]`
- `[DO WERYFIKACJI TECHNICZNEJ: ADEKWATNOŚĆ SZEŚCIOCYFROWEGO HASŁA DO RYZYKA I POTRZEBA DODATKOWYCH ZABEZPIECZEŃ]`
- `[DO WERYFIKACJI TECHNICZNEJ: CZY KAŻDY MENEDŻER MA MIEĆ UPRAWNIENIA ADMINISTRATORA KLIENTA DO TWORZENIA I USUWANIA KONT MENEDŻERÓW]`
- `[DO WERYFIKACJI TECHNICZNEJ: CZY W ŚRODOWISKU PRODUKCYJNYM SĄ WYMAGANE NIEZBĘDNE PLIKI COOKIE OPRÓCZ PAMIĘCI LOKALNEJ PRZEGLĄDARKI]`
- `[DO WERYFIKACJI TECHNICZNEJ: DOCELOWY PROCES DEZAKTYWACJI KONTA BEZ USUWANIA POWIĄZANYCH DANYCH]`
- `[DO WERYFIKACJI TECHNICZNEJ: LISTA WSPIERANYCH PRZEGLĄDAREK, WERSJI, SYSTEMÓW I URZĄDZEŃ]`
- `[DO WERYFIKACJI TECHNICZNEJ: PRODUKCYJNY PROCES OBSŁUGI INCYDENTÓW, GODZINY MONITOROWANIA I OSOBY ODPOWIEDZIALNE]`
- `[DO WYPEŁNIENIA: ADRES DO KORESPONDENCJI]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL DO INCYDENTÓW BEZPIECZEŃSTWA]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL DO REKLAMACJI]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL DO SPRAW DANYCH OSOBOWYCH]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL WSPARCIA TECHNICZNEGO]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL WSPARCIA]`
- `[DO WYPEŁNIENIA: ADRES E-MAIL]`
- `[DO WYPEŁNIENIA: ADRES PLATFORMY]`
- `[DO WYPEŁNIENIA: ADRES]`
- `[DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA INFORMACJI]`
- `[DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA]`
- `[DO WYPEŁNIENIA: DATA OSTATNIEJ AKTUALIZACJI]`
- `[DO WYPEŁNIENIA: IMIĘ I NAZWISKO USŁUGODAWCY]`
- `[DO WYPEŁNIENIA: LINK DO POLITYKI PRYWATNOŚCI]`
- `[DO WYPEŁNIENIA: LINK DO REGULAMINU]`
- `[DO WYPEŁNIENIA: NAZWA PLATFORMY – ROBOCZO „GF”]`
- `[DO WYPEŁNIENIA: NIP – JEŻELI NADANY I WYMAGANY]`
- `[DO WYPEŁNIENIA: NUMER TELEFONU – OPCJONALNIE]`
- `[DO WYPEŁNIENIA: NUMER WERSJI INFORMACJI]`
- `[DO WYPEŁNIENIA: NUMER WERSJI]`

<!-- PLACEHOLDER_INVENTORY_END -->

## Podstawa odniesienia

Projekt oparto między innymi na wymaganiach ustawy z dnia 18 lipca 2002 r. o świadczeniu usług drogą elektroniczną, w szczególności dotyczących danych Usługodawcy, informacji o szczególnych zagrożeniach, Regulaminu, jego bezpłatnego udostępnienia przed zawarciem umowy, zakresu Usług, wymagań technicznych, zakazu treści bezprawnych oraz warunków zawierania i rozwiązywania umów.

- [Ustawa o świadczeniu usług drogą elektroniczną – tekst jednolity, ELI](https://eli.gov.pl/eli/DU/2024/1513/ogl)
- [Prawo przedsiębiorców – ELI](https://eli.gov.pl/eli/DU/2018/646/ogl)
- [RODO – EUR-Lex](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)

Przed publikacją należy sprawdzić aktualny stan prawny i dopasowanie dokumentów do faktycznego modelu Usługodawcy, Klienta oraz produkcyjnego sposobu działania Platformy.

