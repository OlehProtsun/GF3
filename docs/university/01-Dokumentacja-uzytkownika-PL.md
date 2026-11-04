# GF — Dokumentacja użytkownika (PL)

Projekt techniczny: GF.

## 1. Jak korzystać z instrukcji

Instrukcja jest samodzielnym dokumentem. Dostępne są oddzielne wersje polska i angielska; korzystaj ze spisu treści poniżej. Instrukcja obejmuje pracownika, menedżera PC i menedżera w trybie telefonu. Organizacja przydziela konto; samodzielnej rejestracji nie ma. Nazwy wytłuszczone odpowiadają kontrolkom interfejsu; nazwy sklepów, grafików i treści wiadomości wprowadza organizacja.

- Dostęp
- Menedżer PC
- Grafiki
- Telefon
- Pracownik
- Procedury
- Problemy

## 2. Dostep

W `/login` wpisz **Nazwa użytkownika** i **Hasło** (dokładnie sześć cyfr), następnie **Zaloguj się**. Przełącznik PC / **Telefon** wybiera sposób wpisania hasła, nie uprawnienia. W trybie klawiatury telefonu wybierz **Wpisz hasło**; szósta cyfra rozpoczyna sprawdzenie. **Usuń** usuwa ostatnią cyfrę, **Zamknij** zamyka klawiaturę. Poczekaj na wynik.

Menedżer po zalogowaniu wybiera **Wybierz tryb pracy**: PC / **Pełny dostęp** albo **Telefon** / **Tylko podgląd**. Pracownik przechodzi do powiadomień. Język English / Polski wybierz przez **Język aplikacji** (pracownik: profil, menedżer PC: panel nawigacji). Zakończ sesję przez **Wyloguj się**.

| Ekran | Przycisk lub pole | Co robi | Warunek lub wynik |
| --- | --- | --- | --- |
| Logowanie | **Nazwa użytkownika**, **Hasło**, **Zaloguj się** | Otwiera konto przypisanej roli | Niepusty login i sześciocyfrowe hasło |
| Odzyskiwanie `/password-recovery` | **Nie pamiętasz hasła?**, **Wyślij kod**, **Wyślij kod ponownie** | Wysyła kod na zapisany adres odzyskiwania | Wpisz login; brak adresu lub działającej poczty wymaga operatora |
| Odzyskiwanie | **Kod**, **Nowe hasło**, **Zmień hasło** | Potwierdza kod i zmienia hasło | Kod ma sześć znaków, nowe hasło sześć cyfr; po sukcesie wróć do logowania |
| Regulamin | Pobranie PDF, potwierdzenie przeczytania, **Zaakceptuj i kontynuuj** | Zapisuje osobiste potwierdzenie konkretnej wersji | Przeczytaj pobrany PDF i zaznacz pole; kolejne dokumenty mogą pojawić się osobno |

Bramka regulaminu pojawia się tylko przy wymaganym dokumencie. Potwierdzasz go osobiście; to nie zgoda RODO ani umowa w imieniu pracodawcy. Przy błędzie pobrania/zapisu ponów po usunięciu przyczyny.

## 3. Menedzer PC

Z nawigacji otwórz wybrany obszar. Listy mają wyszukiwanie, czyszczenie filtra i otwieranie szczegółów; przypinanie porządkuje obsługujące je karty. Nowy rekord otwórz przez **Dodaj nowy**. W edytorze popraw zaznaczone pola i zapisz. Anulowanie/powrót nie potwierdza zapisu. Przy blokadzie innego menedżera poczekaj na jej zwolnienie.

| Ekran | Przycisk lub pole | Co robi | Warunek lub wynik |
| --- | --- | --- | --- |
| **Strona główna** `/` | **Kto dzisiaj pracuje?**, **Ten miesiąc**, **Rozwiń wszystko**, **Zwiń wszystko**, otwarcie grafiku/kontenera | Pokazuje dzisiejszą obsadę, sumy i podglądy | Puste zestawienie możliwe przy braku przypisań |
| **Sklepy** `/shop`; profil `/shop/:shopId`; formularze `/shop/new`, `/shop/:shopId/edit` | **Nazwa**, **Adres**, **Opis**, **Edytuj sklep**, **Usuń sklep** | Tworzy/zmienia sklep; usuwa po potwierdzeniu | Popraw błędy nazwy/adresu; zależności mogą zablokować usunięcie |
| **Pracownicy** `/employee`; profil `/employee/:employeeId`; formularze `/employee/new`, `/employee/:employeeId/edit` | **Imię**, **Nazwisko**, **E-mail**, **Telefon**, **Nazwa użytkownika**, **Hasło** | Zarządza danymi i dostępem pracownika | Popraw email, duplikat loginu lub hasło inne niż sześć cyfr; przy edycji nie zmieniaj hasła bez potrzeby |
| Profil pracownika | **Edytuj pracownika**, **Wyloguj pracownika**, **Usuń pracownika** | Edytuje, kończy wszystkie sesje, usuwa/archiwizuje zgodnie z zależnościami | Potwierdź operację; zakończenie sesji nie blokuje ponownego logowania |
| **Dyspozycyjność** `/availability`; profil `/availability/:availabilityId`; formularze `/availability/new`, `/availability/:availabilityId/edit` | **Nazwa dyspozycyjności**, **Miesiąc dyspozycyjności**, **Rok dyspozycyjności**, wybór pracownika, **Dodaj**, usunięcie pracownika | Zakłada miesięczną grupę i przypisuje osoby | Nazwa i przynajmniej jeden pracownik wymagani; bez duplikatów |
| Edytor dyspozycyjności | **Publikacja**, **Prywatne**, **Udostępnione**, **Widoczne od**, **Widoczne do**, **Zapisz** | Publikuje dostęp w oknie czasu | Koniec późniejszy od początku; po zapisaniu dostęp dla przypisanych pracowników |
| Dyspozycyjność | Macierz dni, **Informacje o skrócie**, **Klawisz**, **Wartość**, **Aktywny**, **Dodaj**, **Usuń**, **Wybierz z innego** | Wpisuje kody/godziny, definiuje skróty, wybiera dostępne źródło innej dyspozycyjności | Klucz i wartość wymagane; poprawny niepowtórzony klucz; sprawdź źródło przed zatwierdzeniem |
| **Lista kontenerów** `/container` | **Dodaj nowy**, **Nazwa**, **Notatka**, otwarcie/edycja/usunięcie karty | Tworzy kontener, pokazuje profil z grafikami i sumami w tej samej stronie | Kontener musi istnieć przed zapisem grafiku; usuwanie po potwierdzeniu |
| **Komunikaty** `/communications` | **Tytuł**, **Wiadomość**, **Widoczne od**, **Widoczne do**, **Utwórz**, **Edytuj**, **Zapisz**, **Anuluj edycję**, **Usuń** | Zarządza wiadomościami pracowników | Tytuł/treść wymagane; koniec w przyszłości i po początku; usunięta wiadomość znika pracownikom |
| **Profil menedżera** `/manager-profile` | **Nazwa wyświetlana**, **Nazwa użytkownika**, **E-mail odzyskiwania**, **Nowe hasło**, **Zapisz** | Zapisuje własne dane | Hasło sześciocyfrowe; dane konta systemowego zarządza środowisko serwera |
| Profil menedżera | **Aktywni menedżerowie**, **Utwórz menedżera**, **Hasło tymczasowe**, **Usuń**, **Moja historia regulaminów** | Pokazuje aktywność, tworzy/usuwa dozwolone konto, pokazuje potwierdzenia | Nowe konto wymaga nazwy wyświetlanej, loginu i hasła; kont chronionych nie usuniesz |
| Panel PC | **Otwórz notatnik**, **Nowe**, **Tytuł notatki**, **Treść notatki**, **Zapisz**, **Usuń** | Tworzy/edytuje własne notatki, usuwa po potwierdzeniu | Sprawdź stan zapisania; zbyt długa treść daje błąd |
| Notatnik | **Szukaj notatek**, **Przypnij notatnik**, **Odepnij notatnik**, **Zmień rozmiar notatnika**, **Zwiń notatnik** | Wyszukuje, przypina i zmienia rozmiar/zwija panel | Pozycja panelu może być zapisywana; błąd zapisu wymaga ponowienia |
| Notatnik | **Pogrubienie**, **Kursywa**, **Podkreślenie**, **Przekreślenie**, **Rozmiar tekstu**, **Kolor tekstu**, **Lista punktowana**, **Lista numerowana**, **Cofnij**, **Ponów**, **Wyczyść formatowanie** | Formatuje zaznaczoną treść | Zapisz notatkę po zmianach |
| Panel PC | **Otwórz aktualności systemowe**, **Odczytaj**, **Odczytaj wszystkie**, zamknięcie | Otwiera aktualności systemowe i zapisuje odczyt | Pokazuje wiadomości dla odbiorców konta |
| Aktualności — dostęp deweloperski | **Ustawienia aktualności**, **Hasło programisty**, **Odblokuj** | Otwiera zarządzanie po dodatkowym sprawdzeniu | Tylko operator z hasłem hosta; zwykły dostęp menedżera go nie zastępuje |
| Zarządzanie aktualnościami | **Nowa aktualność**, **Tytuł**, **Tekst**, **Widoczne do**, **Wszyscy**, **Tylko menedżerowie**, **Tylko pracownicy**, **Adres URL obrazu**, **Prześlij obraz**, **Usuń**, **Link do YouTube**, **Wyczyść**, **Zapisz aktualność**, **Usuń** | Tworzy/zmienia treść i grupę odbiorców, dodaje obraz/wideo; usuwa po potwierdzeniu | Wymagane tytuł/treść; limity i format mediów sprawdzane; nie wpisuj hasła do treści |

### Funkcje systemowe

`/information` i `/database` są tylko dla menedżera systemowego. Zwykły menedżer i pracownik nie uzyskują tych uprawnień przez znajomość adresu.

| Ekran | Przycisk lub pole | Co robi | Warunek lub wynik |
| --- | --- | --- | --- |
| **Informacje** | **Ustawienia rejestrowania działań**, **Wszyscy**, **Menedżerowie**, **Pracownicy** | Steruje nowym zapisem aktywności | Wyłączenie zachowuje historię |
| Informacje | Dzień, wyszukiwanie nazwy/czasu, **Od**, **Do dnia**, **Usuń dzień**, **Usuń zakres**, **Usuń wszystko** | Przegląda/usuwa logi | Daty lokalne, koniec włącznie; usuwanie po potwierdzeniu |
| **Baza danych** | **Hasło programisty**, **Odblokuj bazę danych** | Odblokowuje narzędzia przy dozwolonej polityce hosta | Dodatkowe ograniczenia administracyjne; narzędzia mogą być wyłączone w produkcji |
| Baza | **Użyj wybranej bazy**, **Utwórz kopię ręcznie**, **Przeglądarka bazy danych**, **Użyj w wykonawcy** | Wybiera bazę, tworzy kopię, przegląda strukturę i rekordy | Zmiana bazy wpływa na całą aplikację; tylko uprawniony operator |
| Baza | **Polecenie SQL**, **Wykonaj**, **Wyczyść wyniki**, **Wybierz plik**, **Skrypt importu**, **Uruchom import** | Zapytanie/import, czyszczenie wyniku | Zapis zależy od polityki hosta/limitów; import obsługuje własny eksport do kodu, nie dowolne systemy zewnętrzne |

Potwierdzenie usunięcia pracownika jest obecnie po polsku także przy języku English: „Czy na pewno chcesz usunąć tego pracownika? Ta operacja jest nieodwracalna.”, przycisk „Usuń”.

Nie używaj danych produkcyjnych do nauki operacji administracyjnych.

## 4. Grafiki

W kontenerze otwórz `/container/:containerId/graphs/:graphId`, nowy grafik `/container/:containerId/graphs/new` lub edytor `/container/:containerId/graphs/:graphId/edit`. Profil pokazuje macierz, konflikty i sumy. **Edytuj wszystko** otwiera wskazane grafiki sesji po potwierdzeniu.

| Ekran | Przycisk lub pole | Co robi | Warunek lub wynik |
| --- | --- | --- | --- |
| **Szczegóły grafiku** | **Nazwa**, **Sklep**, **Miesiąc**, **Rok**, **Grupa dyspozycyjności** | Określa grafik i źródło dyspozycyjności | Istniejący sklep i zgodny okres |
| Szczegóły | **Osoby na zmianie**, **Maks. godzin / pracownik**, **Zmiana 1**, **Zmiana 2**, **Maks. kolejnych dni**, **Maks. kolejnych pełnych zmian**, **Maks. pełnych zmian / miesiąc** | Ustala obsadę, czasy i limity generatora | Sprzeczne limity mogą uniemożliwić obsadę |
| Preset | Wybór, **Dodaj szablon**, **Nazwa szablonu**, **Nazwa grafiku**, **Zapisz szablon** | Wczytuje/zapisuje ustawienia i nadpisania pracowników | Wybieralne między kontenerami; nie zakładaj globalnie unikalnych nazw; zapis presetu nie publikuje grafiku |
| **Pracownicy** | Wybór, **Dodaj**, usunięcie, indywidualne limity | Ustala uczestników | Przynajmniej jeden pracownik, bez duplikatów |
| Macierz | **Generuj**, wpisy komórek, **Cofnij**, **Wypełnienie**, **Tekst**, **Wyczyść zaznaczone**, **Wyczyść wszystko** | Generuje propozycję, edytuje treść/kolor, czyści wskazany zakres | Sprawdź wynik, konflikty i sumy; błąd/pusty wynik wymaga korekty danych |
| Ustawienia | **Informacje o skrócie**, **Kolumny ręczne**, **Notatka** | Definiuje skróty, dodatkowe kolumny i opis | Popraw błędy kluczy i pól przed zapisem |
| **Podgląd dyspozycyjności** | Wybór źródła, układ z boku/pod spodem | Pokazuje dyspozycyjność przy edycji | Podgląd nie zastępuje zapisu |
| **Wersje** | Lista, podgląd, checkout | Pokazuje historię i przywraca wybraną wersję do pracy | Sprawdź wersję i potwierdzenia; podgląd nie jest publikacją |
| **Publikacja** | **Prywatne**, **Udostępnione**, **Zezwól na zamianę**, **Wł.**, **Wył.**, kolor zaakceptowanej wymiany, **Zapisz** | Zapisuje widoczność i zgodę na wymiany | Zmiana formularza wymaga zapisu; pracownik widzi tylko udostępniony stan publiczny |
| **Korekty zmian** | Decyzja/porównanie wniosku | Zatwierdza lub odrzuca korektę | Sprawdź aktualność; wniosek sam nie zmienia godzin |
| Wymiany | **Dziennik zamian**, **Zobacz przed i po**, publikacja zmiany ręcznej do wymian | Pokazuje przyjęte zmiany, oferuje wolną zmianę | Zapisany grafik i włączone wymiany; duplikat otwartej oferty blokowany |
| Profil grafiku/kontenera | **Eksportuj** — ikony Excel i kodu, usunięcie | Pobiera arkusz lub SQL; usuwa rekord po potwierdzeniu | Błąd eksportu nie jest pobraniem; SQL dla uprawnionego operatora |
| Kontener — grafiki | **Otwórz wiele**, **Otwórz**, **Dodaj nowy**, **Opublikuj wszystko**, **Ukryj**, **Może zamieniać**, **Tak**, **Nie** | Wybiera kilka grafików sesji; dodaje grafik; publikuje/ukrywa wszystkie po potwierdzeniu | Publikacja wymaga wyboru zgody na wymiany; prywatyzacja odbiera pracownikom widoczność |
| Kontener — wymiany | Wyszukiwanie, **Zobacz**, **Anuluj**, **Usuń** | Porównuje ofertę, anuluje otwartą, usuwa zapis wymiany | Usunięcie historii nie cofa zaakceptowanej zmiany grafiku |
| Kolumny ręczne | **Dodaj kolumnę**, nagłówek/komórki macierzy, usunięcie kolumny, **Wybierz zmianę**, **Wszyscy**, **Wybrany pracownik**, **Opublikuj zmianę**, **Usuń**, **Anuluj** | Dodaje kolumnę, wpisuje godziny, wybiera zmianę i odbiorcę; usuwa oczekującą propozycję/anuluje opublikowaną | Wpisz zakres, np. 09:00-15:00, zapisz grafik i włącz wymiany; sprawdź oczekujące zapisy przed publikacją |
| Historia grafiku | **Historia grafiku**, wybór commitu, **Przełącz**, **Przywróć wersję**, **Usuń** | Przełącza zapisany stan lub usuwa snapshot po potwierdzeniu | Checkout może odrzucić niezapisany szkic; następny zapis tworzy gałąź. Usunięcie wersji zachowuje potomne commity, łącząc je z rodzicem |
| Korekty grafiku | **Bieżący**, **Wnioskowane**, **Zatwierdź**, **Odrzuć**, kolor zatwierdzenia | Porównuje godziny i rozstrzyga wniosek | Przed zatwierdzeniem zapisz bieżące edycje grafiku; stara zmiana może już nie odpowiadać wnioskowi |

Generator/podgląd roboczy nie gwarantuje wykonalności wszystkich ograniczeń. Zapis utrwala stan, a publikacja zapisanego stanu udostępnia go pracownikom. Sprawdź obsadę, godziny i konflikty przed publikacją. Telefon nie udostępnia tych operacji zapisu.

## 5. Telefon

Tryb **Telefon** jest tylko do odczytu. Dostępne są `/` (dzień i podsumowania), `/container` (lista), `/container/:containerId` (grafiki/statystyki), `/container/:containerId/graphs/:graphId` (macierz), `/availability` i `/availability/:availabilityId`, `/employee` i `/employee/:employeeId`, `/shop` i `/shop/:shopId` (listy/szczegóły), `/more` (konto i dodatkowa nawigacja).

| Ekran | Przycisk lub pole | Co robi | Warunek lub wynik |
| --- | --- | --- | --- |
| Listy/szczegóły | **Szukaj**, **Wyczyść wyszukiwanie**, **Wstecz**, **Spróbuj ponownie**, karta | Wyszukuje, wraca, ponawia odczyt | Niepoprawny ID/brak rekordu daje komunikat; brak tworzenia, edycji, generowania i usuwania |
| Kontener | **Statystyki**, **Sklepy**, wybór grafiku | Pokazuje sumy i szczegóły | Istniejące dane |
| **Więcej** | **Przełącz na komputer**, **Wyloguj się** | Przełącza do PC lub kończy sesję | Poczekaj na zakończenie; błąd przełączenia można ponowić |

Aby edytować, wybierz **Przełącz na komputer** w **Więcej**. W PC dostępne jest **Przełącz na telefon**. Osobista akceptacja regulaminu, przełączenie trybu i wylogowanie są dozwolonymi wyjątkami od ograniczenia zapisu, nie edycją danych organizacji.

## 6. Pracownik

Menu: `/notifications` (również `/`), `/schedule`, `/availability`, `/swap`, `/profile`. Dane i akcje ograniczone do konta i udostępnionych rekordów.

| Ekran | Przycisk lub pole | Co robi | Warunek lub wynik |
| --- | --- | --- | --- |
| Powiadomienia | **Wiadomości**, **Aktualności**, **Odczytaj**, **Odczytaj wszystkie** | Zmienia zakładkę, zapisuje przeczytanie | Otwórz wskazaną treść, jeśli jest odnośnik; pusta lista jest poprawnym stanem |
| Grafik | **Wybierz grafik**, wybór dnia, zmiana widoku, **Dostosuj grafik**, **Zastosuj**, **Anuluj** | Wybiera grafik i układ kolumn | Tylko opublikowane dane; miesiąc/rok podsumowania określa sumy |
| Grafik | **Skoryguj**, **Wyślij wniosek**, **Anuluj**, PDF | Wniosek zmiany granicy czasu; eksport/druk PDF | Wybierz dzień pracy, zmianę, początek/koniec i czas; potrzebna decyzja menedżera; zapis PDF przez okno drukowania przeglądarki |
| Dyspozycyjność | Grupa, dzień, godziny od/do, presety, **Wyczyść**, **Zapisz** | Ustawia dzień i zapisuje formularz grupy | Publiczna przypisana grupa i właściwe okno czasu; preset zapisz osobno, następnie sprawdź potwierdzenie zapisu dyspozycyjności |
| Wymiany | Wyszukiwanie, filtry, **Przypnij**, **Odepnij**, kopiowanie linku, **Historia zamian**, szczegóły | Porządkuje oferty i historię | Link nie nadaje uprawnień ani nie rezerwuje zmiany |
| Tworzenie oferty | **Utwórz ofertę**, **Grafik**, **Odbiorca**, **Podsumowanie**, **Wszyscy**, **Wybrany pracownik**, **Przekaż zmianę**, **Wymień zmiany** | Wybiera własną zmianę/okres, odbiorcę i typ | Publiczny grafik z dozwolonymi wymianami; wzajemna wymiana wymaga konkretnej osoby i jej zmiany |
| Kreator | **Zapisz wybór**, **Kontynuuj**, **Wyślij wniosek**, **Wstecz**, **Zamknij** | Zatwierdza wybór, wysyła lub opuszcza kreator | Sprawdź obie strony/okresy; wysłanie nie oznacza akceptacji |
| Oferta | **Przejmij zmianę**, **Anuluj ofertę** | Przyjmuje uprawnioną ofertę lub anuluje dozwoloną | Potwierdź akceptację; konflikt/nieaktualna oferta może być odrzucona; wzajemna akceptacja przenosi obie zmiany |
| Profil | **Edytuj**, **E-mail odzyskiwania**, **Telefon**, **Zapisz profil**, **Anuluj** | Zmienia własny kontakt | Popraw email; anulowanie przywraca formularz |
| Profil | **Wyślij kod**, **Kod weryfikacyjny**, **Nowe hasło**, **Zmień hasło** | Zmienia hasło po kodzie mailowym | Adres odzyskiwania wymagany; kontakt zapisywany przed wysłaniem kodu; nowe hasło sześciocyfrowe |
| Profil | **Język aplikacji**, **Powiadomienia push**, historia regulaminu, **Wyloguj się** | Język, push urządzenia, potwierdzenia, wylogowanie | Push opcjonalny, nie zastępuje skrzynki |

Płomień oznacza zmianę menedżera z kolumny ręcznej. Sama widoczność karty nie oznacza uprawnienia do akceptacji.

Push wymaga obsługi przeglądarki, konfiguracji serwera, HTTPS i zgody systemu. iPhone/iPad: iOS 16.4+ i GF dodany do ekranu początkowego. W profilu włącz/wyłącz przełącznik na urządzeniu. Przy odmowie zmień ustawienia przeglądarki/systemu; brak konfiguracji zgłoś operatorowi. System może opóźnić dostarczenie.

## 7. Procedury

### Pierwsza konfiguracja

1. Zaloguj się, wybierz PC, przeczytaj wymagany dokument, jeśli jest wyświetlony.
2. W sklepach wybierz **Dodaj nowy**, wpisz nazwę/adres/opis, zapisz i sprawdź profil.
3. W pracownikach wybierz **Dodaj nowy**, uzupełnij dane/dostęp, zapisz. Przekaż dane dostępowe zgodnie z zasadami organizacji.
4. Dodaj kontener z nazwą/notatką, zapisz; kontynuuj dyspozycyjnością.

### Zebranie dyspozycyjnosci

1. Dodaj grupę z nazwą, miesiącem/rokiem i uczestnikami.
2. Ustaw skróty dni/godziny, jeśli potrzebne; wybierz **Udostępnione** oraz poprawne **Widoczne od**–**Widoczne do**, zapisz.
3. Pracownik w `/availability` wybiera grupę, dzień, godziny/preset; zapisuje dzień, następnie formularz grupy i sprawdza potwierdzenie.
4. Menedżer otwiera profil grupy i weryfikuje zebrane wpisy przed generowaniem.

### Generowanie i publikacja

1. W istniejącym kontenerze dodaj grafik.
2. Wybierz preset lub wpisz sklep, okres, czasy/limity. Wybierz zgodną grupę dyspozycyjności.
3. Dodaj pracowników i ich ograniczenia; opcjonalnie zapisz ustawienia jako preset.
4. Uruchom **Generuj**, poczekaj, sprawdź macierz, konflikty i sumy. Przy błędzie popraw dane; nie publikuj niezweryfikowanego wyniku.
5. Popraw komórki, zapisz i potwierdź zapis sesji. Sprawdź profil/wersje.
6. W publikacji wybierz **Udostępnione**, ustaw **Zezwól na zamianę** według potrzeb, zapisz.

### Sprawdzenie grafiku pracownika

1. W `/schedule` wybierz udostępniony grafik.
2. Sprawdź okres, sklep i godziny dnia; możesz zmienić widok/kolumny.
3. Sprawdź sumy miesiąca/roku. Brak grafiku zgłoś menedżerowi; brak publikacji nie potwierdza dnia wolnego.

### Oddanie lub wymiana zmiany

1. W `/swap` wybierz **Utwórz ofertę**, publiczny grafik i własną zmianę/zakres czasu.
2. Wybierz **Wszyscy** albo **Wybrany pracownik**. Dla konkretnej osoby wybierz **Przekaż zmianę** lub **Wymień zmiany**; w drugim przypadku wybierz jej zmianę/okres i zapisz wybór.
3. Przejdź do **Podsumowanie**, sprawdź osobę, datę, grafik i godziny, wyślij; wróć, jeśli potrzebne poprawki.
4. Odbiorca otwiera uprawnioną ofertę, wybiera **Przejmij zmianę** i potwierdza. Po sukcesie obie strony sprawdzają aktualny grafik.
5. Właściciel używa **Anuluj ofertę**, jeśli nadal dozwolone. Historię sprawdź w **Historia zamian**.

### Korekta

1. W `/schedule` wybierz grafik i **Skoryguj**.
2. Wskaż dzień pracy, zmianę, początek lub koniec; ustaw proponowany czas (±30 min).
3. Wybierz **Wyślij wniosek**, poczekaj na wynik; **Anuluj** zamyka bez wysłania.
4. Menedżer rozpatruje wniosek w edytorze grafiku. Sprawdź decyzję i opublikowany grafik; wniosek nie jest automatyczną akceptacją.

### Zmiana hasla

1. Pracownik w profilu wybiera **Edytuj**, sprawdza **E-mail odzyskiwania**, wybiera **Wyślij kod** (kontakt zostanie wcześniej zapisany).
2. Wpisuje **Kod weryfikacyjny** i sześciocyfrowe **Nowe hasło**, wybiera **Zmień hasło**, czeka na sukces.
3. Bez sesji: **Nie pamiętasz hasła?** → login → wysłanie kodu → kod/nowe hasło → potwierdzenie → ponowne logowanie. Brak adresu wymaga operatora.
4. Menedżer niesystemowy zmienia własne hasło przez **Nowe hasło** i **Zapisz** w profilu; hasłem konta systemowego zarządza administrator środowiska.

## 8. Problemy

| Problem | Działanie |
| --- | --- |
| Błąd logowania | Sprawdź login/sześć cyfr. Przy ograniczeniu prób odczekaj zgodnie z komunikatem; użyj odzyskiwania. |
| Brak grafiku | Sprawdź okres/wybór; poproś menedżera o sprawdzenie przypisania/publikacji. Szkic prywatny jest niedostępny. |
| Brak grupy dyspozycyjności | Menedżer sprawdza przypisanie, publikację i okno czasu; pracownik nie tworzy własnej grupy. |
| Zapis z telefonu menedżera | W **Więcej** wybierz **Przełącz na komputer**. |
| Push niedostępny | Sprawdź zgodę, HTTPS, obsługę i instalację iOS; brak konfiguracji zgłoś operatorowi. Czytaj skrzynkę aplikacji. |
| Duplikaty/błędne wartości | Popraw zaznaczone pola, klucze, login i daty; nie dodawaj ponownie tej samej osoby. |
| Sieć/nieudany zapis/blokada | Zachowaj treść, usuń przyczynę błędu, ponów. Najpierw sprawdź odczytem, czy zapis już nastąpił. Przy cudzej blokadzie zaczekaj. |
| Oferta nieaktualna/konflikt | Odśwież listę i sprawdź grafik; link nie rezerwuje zmiany. |

Nierozwiązane problemy zgłoś operatorowi/kontaktowi wskazanemu przez organizację. Podaj ekran, czas i błąd bez hasła/kodu/danych innych osób. Instrukcja nie ustala SLA.

## 9. Pokrycie prezentacji

Prezentacja 1: s. 3 — samodzielny dokument i spis treści (§1); s. 4 — role/języki (§1–6); s. 5 — ekrany/pola/akcje (§2–6); s. 6 — HOW-TO (§7); s. 7 — FAQ/nawigacja (§1, §8). Wideo/chatbot/osobna platforma pomocy są alternatywami materiału, nie funkcjami GF.
