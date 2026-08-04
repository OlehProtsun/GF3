# Checklista wdrożenia Regulaminu świadczenia usług drogą elektroniczną

> Checklista opisuje prace wymagane przed publikacją. Nie wprowadza zmian w aplikacji ani w środowisku produkcyjnym.

## 1. Publikacja

- [ ] Dodać publiczną trasę `/regulamin`, dostępną bez logowania.
- [ ] Udostępnić pełną wersję HTML oraz plik możliwy do pobrania; opcjonalnie przygotować równoważny PDF.
- [ ] Dodać widoczny link „Regulamin” na ekranie logowania.
- [ ] Dodać link w stopce aplikacji albo innym stałym miejscu dostępnym po zalogowaniu.
- [ ] Dodać obok link do „Informacji o szczególnych zagrożeniach”.
- [ ] Zapewnić możliwość pobrania, zapisania, odtworzenia i wydrukowania Regulaminu przed rozpoczęciem korzystania.
- [ ] Zweryfikować czytelność na telefonie, tablecie i komputerze.
- [ ] Zweryfikować polskie znaki, odnośniki wewnętrzne, tabelę treści i wydruk A4.
- [ ] Ustalić publiczny, stabilny adres, który nie wymaga ważnej sesji.
- [ ] Nie zastępować opublikowanego pliku inną treścią bez zmiany numeru wersji.

## 2. Rejestrowanie akceptacji

- [ ] Ustalić z prawnikiem, czy i dla których Użytkowników wymagane jest wyraźne oświadczenie o akceptacji.
- [ ] Dodać, tam gdzie będzie to właściwe, nieoznaczone z góry pole akceptacji przy pierwszym logowaniu lub aktywacji Konta.
- [ ] Wyświetlić bezpośredni link do pełnej treści przed oznaczeniem pola.
- [ ] Zapisać numer wersji Regulaminu.
- [ ] Zapisać datę i czas akceptacji w UTC.
- [ ] Zapisać identyfikator Użytkownika i techniczną rolę.
- [ ] Zapisać identyfikator organizacji Klienta, gdy model danych go zapewnia.
- [ ] Zachować dowód treści odpowiadającej zaakceptowanej wersji.
- [ ] Zachowywać historyczne wersje i nie podmieniać ich po akceptacji.
- [ ] Ustalić zasady ponownej akceptacji po zmianie istotnej oraz samego powiadomienia po zmianie nieistotnej.
- [ ] Zapewnić obsługę Kont utworzonych przed wdrożeniem mechanizmu.
- [ ] Zapewnić możliwość raportowania akceptacji na potrzeby Klienta i audytu.

**Stan aktualny:** w repozytorium nie potwierdzono pola akceptacji, numeru zaakceptowanej wersji ani daty akceptacji Regulaminu.

## 3. Spójność prawna i dokumentowa

- [ ] Porównać Regulamin z Umową o świadczenie usług SaaS i Załącznikiem nr 1.
- [ ] Zachować pierwszeństwo Umowy w sprawach ceny, okresu, zakresu płatnych Usług i odpowiedzialności.
- [ ] Nie wprowadzać w Regulaminie innego limitu odpowiedzialności niż w Umowie.
- [ ] Potwierdzić brak obowiązku abonamentowego po stronie pojedynczego Użytkownika.
- [ ] Porównać role i operacje z Polityką prywatności oraz Umową powierzenia przetwarzania danych osobowych.
- [ ] Porównać wymagania pamięci lokalnej i plików cookie z Polityką cookies.
- [ ] Porównać godziny oraz czasy wsparcia z Umową i ewentualnym SLA.
- [ ] Sprawdzić zgodność nazw funkcji z aktualnym interfejsem Platformy.
- [ ] Zatwierdzić sposób oznaczenia osoby wykonującej działalność nierejestrowaną.
- [ ] Zatwierdzić moment powstania relacji elektronicznej z Użytkownikiem upoważnionym przez Klienta.
- [ ] Zatwierdzić procedurę reklamacji i proponowany termin 14 dni.
- [ ] Zatwierdzić reguły blokowania bezprawnych treści i proporcjonalnego zawieszenia.
- [ ] Zatwierdzić zasady zmian Regulaminu oraz ponownej akceptacji.

### Wynik bieżącego porównania z projektem Umowy SaaS

- Model B2B, nazwy Stron, brak przeniesienia kodu i praw, Dane Klienta, brak gwarancji 100%, wsparcie w Dni Robocze oraz pierwsza odpowiedź do 2 Dni Roboczych są zgodne.
- Regulamin odsyła do Umowy w sprawach ceny, odpowiedzialności, zawieszenia za opóźnienie płatnicze i istotnych zmian zakresu; nie powiela limitu sześciomiesięcznego.
- Okresy eksportu i usunięcia danych nie zostały ponownie określone, aby nie tworzyć sprzeczności z roboczymi wartościami 14 i 45 dni w Umowie.
- Wymagane pozostają osobne: Umowa powierzenia, Polityka prywatności, Polityka cookies oraz produkcyjna procedura Incydentów.

## 4. Weryfikacja techniczna

- [ ] Potwierdzić, że samodzielna rejestracja nadal nie istnieje.
- [ ] Potwierdzić proces tworzenia Kont pracownika i menedżera oraz osobę uprawnioną.
- [ ] Ustalić, czy każdy menedżer ma mieć możliwość tworzenia i usuwania innych Kont menedżerskich.
- [ ] Zaprojektować kontrolowaną dezaktywację Konta niezależną od usunięcia pracownika i zależnych danych.
- [ ] Potwierdzić unieważnianie aktywnych sesji po odebraniu dostępu.
- [ ] Potwierdzić role oraz autoryzację wszystkich tras API.
- [ ] Przeprowadzić testy wspieranych wersji Chrome, Edge, Firefox i Safari.
- [ ] Potwierdzić wymaganie JavaScript i pamięci lokalnej oraz sprawdzić wszystkie niezbędne pliki cookie.
- [ ] Ocenić sześciocyfrowe Hasło oraz potrzebę ograniczania prób, dodatkowego składnika lub innych zabezpieczeń.
- [ ] Potwierdzić kanał i warunki odzyskiwania Hasła.
- [ ] Potwierdzić kanały powiadomień; nie obiecywać e-mail, SMS ani push bez wdrożenia.
- [ ] Potwierdzić adres wsparcia, reklamacji, Incydentów bezpieczeństwa i danych osobowych.
- [ ] Zdefiniować proces klasyfikacji, eskalacji, dokumentowania i zamykania Incydentów.
- [ ] Potwierdzić eksporty XLSX i SQL oraz ograniczyć ich dostęp do uprawnionych menedżerów.
- [ ] Potwierdzić zasady usuwania pracownika, Konta, logów i Danych Klienta.
- [ ] Potwierdzić produkcyjne kopie zapasowe i upewnić się, że Regulamin nie obiecuje odwrócenia każdej czynności.
- [ ] Potwierdzić separację danych Klienta i środowisk.
- [ ] Sprawdzić, czy publiczne pliki Regulaminu nie ujawniają nagłówków lub metadanych wewnętrznych.

## 5. Inwentarz placeholderów

| Placeholder | Plik i sekcja | Wymagana informacja | Odpowiedzialny | Rodzaj weryfikacji |
| --- | --- | --- | --- | --- |
| `[DO WYPEŁNIENIA: NAZWA PLATFORMY – ROBOCZO „GF”]` | Regulamin – tytuł; Informacja – metryka | Zatwierdzona publiczna nazwa produktu | Usługodawca / Klient | Biznesowa |
| `[DO WYPEŁNIENIA: NUMER WERSJI]` | Regulamin – metryka i § 24 | Pierwszy numer wersji, np. 1.0 | Usługodawca / prawnik | Biznesowa i prawna |
| `[DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA]` | Regulamin – metryka i § 24 | Data rozpoczęcia stosowania | Usługodawca / prawnik | Prawna |
| `[DO WYPEŁNIENIA: DATA OSTATNIEJ AKTUALIZACJI]` | Regulamin – metryka | Data zatwierdzenia treści | Właściciel dokumentu | Biznesowa |
| `[DO WYPEŁNIENIA: IMIĘ I NAZWISKO USŁUGODAWCY]` | Regulamin – § 2 | Publiczne oznaczenie Usługodawcy | Usługodawca / prawnik | Prawna |
| `[DO WYPEŁNIENIA: ADRES]` | Regulamin – § 2 | Adres wymagany do identyfikacji | Usługodawca / prawnik | Prawna |
| `[DO WYPEŁNIENIA: ADRES DO KORESPONDENCJI]` | Regulamin – § 2 | Publiczny adres korespondencyjny | Usługodawca | Biznesowa i prawna |
| `[DO WYPEŁNIENIA: ADRES E-MAIL]` | Regulamin – § 2 | Ogólny publiczny e-mail | Usługodawca | Biznesowa |
| `[DO WYPEŁNIENIA: NUMER TELEFONU – OPCJONALNIE]` | Regulamin – § 2 | Decyzja o publikacji telefonu | Usługodawca / prawnik | Biznesowa i prawna |
| `[DO WYPEŁNIENIA: NIP – JEŻELI NADANY I WYMAGANY]` | Regulamin – § 2 | Status NIP i obowiązek publikacji | Księgowy / prawnik | Prawna i księgowa |
| `[DO WYPEŁNIENIA: ADRES E-MAIL DO REKLAMACJI]` | Regulamin – § 2 i § 16 | Produkcyjny kanał reklamacji | Usługodawca | Operacyjna |
| `[DO WYPEŁNIENIA: ADRES E-MAIL WSPARCIA TECHNICZNEGO]` | Regulamin – § 2 | Publiczne oznaczenie kanału wsparcia | Usługodawca | Operacyjna |
| `[DO WERYFIKACJI PRAWNEJ: SPOSÓB OZNACZENIA USŁUGODAWCY ORAZ DOPUSZCZALNOŚĆ WSKAZANIA DZIAŁALNOŚCI NIEREJESTROWANEJ]` | Regulamin – § 2 | Finalny opis statusu | Prawnik | Prawna |
| `[DO WYPEŁNIENIA: ADRES PLATFORMY]` | Regulamin – § 3; Informacja – § 13 | Zatwierdzony publiczny URL | Administrator techniczny | Techniczna |
| `[DO WERYFIKACJI TECHNICZNEJ: CZY KAŻDY MENEDŻER MA MIEĆ UPRAWNIENIA ADMINISTRATORA KLIENTA DO TWORZENIA I USUWANIA KONT MENEDŻERÓW]` | Regulamin – § 4 | Docelowa macierz uprawnień | Klient / administrator techniczny | Techniczna i biznesowa |
| `[DO WERYFIKACJI PRAWNEJ: MOMENT ZAWARCIA UMOWY O ŚWIADCZENIE USŁUG ELEKTRONICZNYCH Z UŻYTKOWNIKIEM I SPOSÓB DOKUMENTOWANIA AKCEPTACJI]` | Regulamin – § 5 | Model akceptacji i dowód | Prawnik | Prawna |
| `[DO WERYFIKACJI TECHNICZNEJ: LISTA WSPIERANYCH PRZEGLĄDAREK, WERSJI, SYSTEMÓW I URZĄDZEŃ]` | Regulamin – § 6 | Wynik testów kompatybilności | Zespół techniczny | Techniczna |
| `[DO WERYFIKACJI TECHNICZNEJ: CZY W ŚRODOWISKU PRODUKCYJNYM SĄ WYMAGANE NIEZBĘDNE PLIKI COOKIE OPRÓCZ PAMIĘCI LOKALNEJ PRZEGLĄDARKI]` | Regulamin – § 6 | Inwentarz mechanizmów przeglądarki | Zespół techniczny / IOD | Techniczna i prawna |
| `[DO WERYFIKACJI TECHNICZNEJ: ADEKWATNOŚĆ SZEŚCIOCYFROWEGO HASŁA DO RYZYKA I POTRZEBA DODATKOWYCH ZABEZPIECZEŃ]` | Regulamin – § 7 | Ocena uwierzytelniania | Specjalista bezpieczeństwa | Techniczna |
| `[DO WERYFIKACJI PRAWNEJ: CZY WYMAGANIA UWIERZYTELNIANIA SĄ ADEKWATNE DO KATEGORII PRZETWARZANYCH DANYCH]` | Regulamin – § 7 | Ocena prawna środków ochrony | Prawnik / IOD | Prawna |
| `[DO WYPEŁNIENIA: ADRES E-MAIL WSPARCIA]` | Regulamin – § 15 | Kanał przyjmowania zgłoszeń | Usługodawca | Operacyjna |
| `[DO WYPEŁNIENIA: ADRES E-MAIL DO INCYDENTÓW BEZPIECZEŃSTWA]` | Regulamin – § 15; Informacja – § 14 | Pilny kanał bezpieczeństwa | Osoba ds. bezpieczeństwa | Techniczna i operacyjna |
| `[DO WYPEŁNIENIA: ADRES E-MAIL DO SPRAW DANYCH OSOBOWYCH]` | Regulamin – § 15 i § 22; Informacja – § 14 | Kanał ochrony danych | IOD / osoba kontaktowa | Prawna i operacyjna |
| `[DO WERYFIKACJI PRAWNEJ I OPERACYJNEJ: TERMIN ROZPATRZENIA KOMPLETNEJ REKLAMACJI – PROPOZYCJA 14 DNI]` | Regulamin – § 16 | Realny i zgodny z prawem termin | Prawnik / obsługa | Prawna i operacyjna |
| `[DO WERYFIKACJI PRAWNEJ: CHARAKTER RELACJI USŁUGODAWCY Z UŻYTKOWNIKIEM UPOWAŻNIONYM PRZEZ KLIENTA]` | Regulamin – § 17 | Kwalifikacja relacji B2B/Użytkownik | Prawnik | Prawna |
| `[DO WERYFIKACJI PRAWNEJ: RELACJA ADMINISTRATOR–PODMIOT PRZETWARZAJĄCY PRZY DEZAKTYWACJI KONTA I ŻĄDANIACH UŻYTKOWNIKA]` | Regulamin – § 18 | Obsługa żądań i retencji | Prawnik / IOD | Prawna |
| `[DO WERYFIKACJI TECHNICZNEJ: DOCELOWY PROCES DEZAKTYWACJI KONTA BEZ USUWANIA POWIĄZANYCH DANYCH]` | Regulamin – § 18 | Projekt stanu dezaktywacji | Właściciel produktu / zespół techniczny | Techniczna i biznesowa |
| `[DO WYPEŁNIENIA: LINK DO POLITYKI PRYWATNOŚCI]` | Regulamin – § 22 | Publiczny link do zatwierdzonego dokumentu | IOD / administrator techniczny | Prawna i techniczna |
| `[DO WERYFIKACJI PRAWNEJ: PODZIAŁ RÓL, OBOWIĄZKI INFORMACYJNE I OBSŁUGA PRAW OSÓB, KTÓRYCH DANE DOTYCZĄ]` | Regulamin – § 22 | Finalne role RODO | Prawnik / IOD | Prawna |
| `[DO WERYFIKACJI PRAWNEJ: TERMIN POWIADOMIENIA I PRZESŁANKI PONOWNEJ AKCEPTACJI REGULAMINU]` | Regulamin – § 23 | Procedura zmian | Prawnik | Prawna |
| `[DO WYPEŁNIENIA: LINK DO REGULAMINU]` | Regulamin – § 24 | Publiczny, stabilny URL | Administrator techniczny | Techniczna |
| `[DO WYPEŁNIENIA: NUMER WERSJI INFORMACJI]` | Informacja – metryka | Numer wersji informacji o zagrożeniach | Właściciel dokumentu | Biznesowa |
| `[DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA INFORMACJI]` | Informacja – metryka | Data publikacji informacji | Właściciel dokumentu | Biznesowa |
| `[DO WERYFIKACJI TECHNICZNEJ: PRODUKCYJNY PROCES OBSŁUGI INCYDENTÓW, GODZINY MONITOROWANIA I OSOBY ODPOWIEDZIALNE]` | Informacja – § 14 | Procedura Incydentów i dyżury | Osoba ds. bezpieczeństwa | Techniczna i operacyjna |

## 6. Zatwierdzenie przed publikacją

- [ ] Przegląd prawnika specjalizującego się w usługach elektronicznych i B2B SaaS.
- [ ] Przegląd IOD lub osoby odpowiedzialnej za ochronę danych.
- [ ] Przegląd techniczny właściciela aplikacji i osoby odpowiedzialnej za bezpieczeństwo.
- [ ] Akceptacja Usługodawcy oraz uprawnionego przedstawiciela Klienta.
- [ ] Nadanie wersji i dat oraz zamrożenie plików tej wersji.
- [ ] Test publicznego udostępnienia przed aktywacją akceptacji.

