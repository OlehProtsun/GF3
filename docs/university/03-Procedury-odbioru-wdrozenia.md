# GF — Procedury odbioru, wdrożenia i odtworzenia

Projekt techniczny: GF3. Podstawa: `DEV2 @ 06cb43083939fecab4c95bb27bfa094fec13055f`.

[README](../../README.md) · [Instrukcja PL](01-Dokumentacja-uzytkownika-PL.md) · [Dokumentacja techniczna](02-Dokumentacja-techniczna.md)

Dokument jest instrukcją i wzorem operacyjnym. Nie potwierdza umowy, odbioru, wykonania backupu, próby odtworzenia ani zgodności prawnej konkretnego wdrożenia. Parametry środowiska, odpowiedzialni, terminy i zgody: **Do uzgodnienia z właścicielem / administratorem**. Wymagania techniczne wynikają ze źródeł; decyzje klienta nie są przedstawione jako istniejące ustalenia.

- [Odbiór i formularze](#1-procedura-odbioru-produktu)
- [Wdrożenie](#2-procedura-wdrozenia-i-uruchomienia)
- [Kopie zapasowe](#3-kopie-zapasowe)
- [Odtworzenie i checklista](#4-dokumentacja-odtworzenia-systemu)

## 1. Procedura odbioru produktu

### Role i uzgodnienia

Proponowany podział do zatwierdzenia przez strony:

| Rola | Odpowiedzialność w procedurze |
| --- | --- |
| Wykonawca / operator środowiska testowego | Udostępnia wskazaną wersję, instrukcję, dane syntetyczne i role testowe; wspiera odtworzenie błędów |
| Klient / osoba odbierająca | Uzgadnia kryteria zamówienia, wykonuje/obserwuje próby, dokumentuje rozbieżności i podejmuje decyzję odbioru |
| Administrator wdrożenia | Przygotowuje konfigurację, uprawnienia, infrastrukturę, backup i izolację testów; odpowiada za bezpieczne uruchomienie |

Możliwe są dwa warianty: (A) odbiór w środowisku testowym przygotowanym przez wykonawcę; (B) odbiór bezpośrednio w docelowym środowisku wdrożeniowym/staging. W B przed zmianą danych należy uzgodnić izolację od użytkowników i kanałów wysyłki. Wariant nie zmienia kryteriów funkcjonalnych, ale zmienia odpowiedzialność za dostępność/konfigurację środowiska.

Przed próbami uzgodnij wersję/commit/obraz, URL, wybrany wariant, rolę kont testowych (pracownik, manager PC/phone, system manager), zakres włączonych funkcji, kryteria zamówienia, dane syntetyczne i sposób zapisu dowodów. Nie wpisuj haseł do protokołu. Każdy test wykonuj w znanym stanie początkowym; zanotuj wynik rzeczywisty, czas i dowód bez prywatnych danych. Niezgodność otrzymuje ID i trafia do harmonogramu; po poprawce powtórz scenariusz na wskazanej wersji. Decyzja może być przyjęta, warunkowa lub odmowna zgodnie z uzgodnionymi kryteriami, nie automatycznie po samym healthcheck.

### Macierz testow odbiorowych

Wszystkie poniższe próby są **NIE WYKONANO** w ramach sporządzenia dokumentacji. „Nie dotyczy” wpisuje osoba odbierająca z uzasadnieniem tylko dla funkcji faktycznie wyłączonej/poza uzgodnionym zakresem. Nazwy testowych osób/sklepów należy utworzyć syntetycznie; nie używaj prawdziwych grafików.

| ID | Rola | Scenariusz i kroki | Wynik oczekiwany | Wynik rzeczywisty | PASS/FAIL/NIE WYKONANO | Uwagi |
| --- | --- | --- | --- | --- | --- | --- |
| O-01 | Manager | Otwórz login, zaloguj poprawnymi danymi testowymi; wybierz PC | Sesja manager, pełny pulpit po wyborze; błędne dane odrzucane | Do wpisania po próbie | NIE WYKONANO | Zapisać wersję i środowisko |
| O-02 | Manager PC | Dodaj sklep (nazwa/adres), dodaj pracownika z unikalnym loginem/sześciocyfrowym hasłem; ponownie odczytaj profile | Rekordy zapisane; duplikat loginu/błędny email daje walidację; dane dostępu nie trafiają do dowodów | Do wpisania po próbie | NIE WYKONANO | Oddzielne dane syntetyczne |
| O-03 | Manager PC / employee | Utwórz miesięczną grupę, dodaj osobę, ustaw publiczne okno obejmujące próbę, zapisz; pracownik wpisuje dostępność dnia i zapisuje | Tylko przypisana osoba widzi publiczną grupę; menedżer odczytuje zapisane wpisy | Do wpisania po próbie | NIE WYKONANO | Zapisać miesiąc/okno |
| O-04 | Manager PC | Utwórz kontener/grafik; wybierz sklep, okres, dyspozycyjność, limity/pracowników; uruchom generowanie/podgląd, sprawdź wynik i zapisz | Propozycja zgodna z dostępnymi danymi albo jawna walidacja/braki; zapis odtwarzalny z profilu | Do wpisania po próbie | NIE WYKONANO | Nie zakładać wykonalności sprzecznych reguł |
| O-05 | Manager PC | Zapisz preset, otwórz inny kontener i listę presetów; sprawdź wersje grafiku, zmień komórkę, zapisz | Preset wybieralny między kontenerami; historia i zapis odpowiadają wskazanej wersji | Do wpisania po próbie | NIE WYKONANO | Bez założenia globalnej unikalności nazw |
| O-06 | Manager PC / employee | Najpierw sprawdź brak prywatnego grafiku na koncie pracownika; opublikuj i zapisz; odczytaj ponownie jako pracownik | Prywatny szkic niedostępny; opublikowany grafik przypisanej osoby widoczny z poprawnym okresem/godzinami | Do wpisania po próbie | NIE WYKONANO | Odczyt innym kontem niż manager |
| O-07 | Employee | Dla publicznego grafiku z Allow swap utwórz ofertę oddania, przejrzyj podgląd, wyślij; uprawniony odbiorca akceptuje; sprawdź obie strony | Oferta zapisana, akceptacja aktualizuje odpowiednie zmiany; niedozwolona/nieaktualna oferta odrzucona | Do wpisania po próbie | NIE WYKONANO | Jeśli wymiany włączone; osobno powtórzyć wariant wzajemny |
| O-08 | Employee / manager PC | Pracownik wysyła korektę początku/końca własnej zmiany; manager porównuje i zatwierdza/odrzuca; odczyt grafiku | Sam wniosek nie zmienia godzin; decyzja dotyczy aktualnej zmiany i jest widoczna | Do wpisania po próbie | NIE WYKONANO | Jeśli funkcja używana; powtórzyć dla zmienionej pierwotnej zmiany |
| O-09 | Employee / manager | Sprawdź skrzynkę/aktualności, oznacz odczyt; gdy opublikowano wymagany dokument, pobierz PDF, przeczytaj, potwierdź i sprawdź historię | Odczyt zapisany; bramka wymaga osobistej akceptacji właściwej wersji, historia ją pokazuje | Do wpisania po próbie | NIE WYKONANO | Tylko zatwierdzone dokumenty; test nie zatwierdza ich treści prawnej |
| O-10 | Employee | W profilu włącz push na wspieranym testowym urządzeniu, udziel zgody; wywołaj uzgodnione zdarzenie, sprawdź urządzenie i wyłącz subskrypcję | Stan urządzenia zapisany; dostarczenie lub rzeczywista przyczyna niedostępności udokumentowana | Do wpisania po próbie | NIE WYKONANO | Tylko przy konfiguracji VAPID/HTTPS; fizyczny test odrębny od symulatora |
| O-11 | Manager phone | Wybierz Phone, otwórz Home, kontener/grafik, dyspozycyjność, osobę, sklep, More; sprawdź brak edycji; spróbuj autoryzowanego testowego zapisu w tym trybie | Odczyty dozwolone; brak przycisków zarządzania; API zapisu zwraca 403 manager_phone_read_only; przełączenie Desktop dostępne | Do wpisania po próbie | NIE WYKONANO | Nie wykonywać eksperymentu na produkcyjnych rekordach |
| O-12 | Manager PC / operator | Pobierz Excel i SQL grafiku/kontenera; sprawdź typ/nazwę/otwarcie pliku; GET /api/health | Eksport poprawnego formatu; health 200 z canConnect=true dla działającej bazy | Do wpisania po próbie | NIE WYKONANO | Health nie zastępuje O-01–O-11 |
| O-13 | Employee / zwykły manager / system manager | Sprawdź /information i /database oraz chronione API różnymi kontami; dla admin DB uwzględnij politykę hosta/hasło | Zwykłe konto nie ma funkcji S; system manager nadal podlega dodatkowym guardom DB; wyłączone narzędzia 404 | Do wpisania po próbie | NIE WYKONANO | Admin news ma odrębny guard; nie uogólniać z nazwy /admin |
| O-14 | Employee | Zmień język; na koncie z syntetyczną pocztą przejdź odzyskiwanie kodem i nowe logowanie | Zapis języka; zmiana hasła tylko po poprawnym kodzie; jawny błąd przy niedostępnej poczcie | Do wpisania po próbie | NIE WYKONANO | SMTP i skrzynka testowa |

### Protokół zgodności produktu z zamówieniem

Poniższy formularz wypełniają strony. Przedmiot zamówienia i kryteria muszą dostarczyć strony; nie wynikają z samej nazwy projektu.

| Pole | Do wypełnienia |
| --- | --- |
| Klient / osoba upoważniona | |
| Wykonawca / osoba upoważniona | |
| Przedmiot i zakres zamówienia, odwołanie do uzgodnionych kryteriów | |
| Produkt, wersja, commit, identyfikator obrazu | |
| Wariant odbioru A/B, środowisko, URL, konfiguracja | |
| Data prób, osoby/role (bez haseł) | |
| Testy wykonane, wyniki i odnośniki do dowodów | |
| Niewykonane/nieobowiązujące testy i uzasadnienie | |
| Rozbieżności / ID usterek | |
| Warunki i zakres ewentualnego odbioru warunkowego | |
| Decyzja: przyjęty / warunkowo przyjęty / nieprzyjęty | |
| Imiona/nazwiska, data i podpis klienta | |
| Imiona/nazwiska, data i podpis wykonawcy | |

### Harmonogram usuwania usterek i braków

Terminy, priorytet i odpowiedzialność uzgadniają strony; brak ustalonego SLA w tym formularzu.

| ID | Opis | Reprodukcja / wpływ | Strona odpowiedzialna | Uzgodniony termin usunięcia | Data ponownego testu | Status | Ostateczny odbiór / podpis |
| --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | |
| | | | | | | | |

## 2. Procedura wdrozenia i uruchomienia

### Warunki techniczne

Wariant produkcyjny to web/PWA przez Docker + nginx, bez publikacji do App Store/Google Play. Wymagane: Docker Engine i Compose plugin, miejsce na obraz/dane/kopie, trwały zapisywalny wolumin wybrany przez `GF3_DATA_VOLUME`, poprawne uprawnienia `/data/SQLite.db`, konfiguracja z [deploy/.env.example](../../deploy/.env.example). Dla publicznego hosta: DNS domeny, dostępne porty HTTP/HTTPS, poprawny certyfikat i klucz dla wariantu HTTPS. Konkretne CPU/RAM/IOPS: niezdefiniowane / do zweryfikowania dla docelowego obciążenia. Ubuntu 24.04 jest przykładem [runbooka](../deploy.md), nie jedynym potwierdzonym OS.

Administrator tworzy prywatny `deploy/.env`, ustawia `GF3_IMAGE`, nazwę hosta/porty i **weryfikuje dokładną nazwę istniejącego woluminu**. Konfiguruje silny losowy `Jwt__SigningKey`, własne `GF3_ADMIN_DEVELOPER_PASSWORD` i `GF3_BOOTSTRAP_MANAGER_PASSWORD`, bez pozostawiania wartości przykładowych. Admin tools/remote/write pozostają wyłączone poza świadomie zatwierdzoną czynnością. Sekretów nie należy wypisywać w logach/protokole.

SMTP (`Smtp__*`) jest opcjonalne dla części systemu, ale potrzebne do działającego odzyskiwania hasła. Push wymaga `Push__Enabled`, kluczy VAPID, subject i właściwych warunków przeglądarki/HTTPS; konfiguracja nie jest dowodem dostarczenia. Testy z kopiowanymi danymi wykonuj z wyłączonymi kanałami wysyłki. DNS, certyfikaty, lokalizacja danych, retencja i właściciel sekretów: do uzgodnienia z administratorem.

### Pierwsza instalacja a aktualizacja

`GF3_REQUIRE_EXISTING_DATABASE=false` wolno ustawić tylko dla świadomej pierwszej instalacji pustego środowiska. Po założeniu bazy ustaw `true`; w istniejącej instalacji brak bazy ma zatrzymać start, a nie utworzyć pusty system. `GF3_BACKUP_BEFORE_MIGRATE=true` włącza kopię istniejącej bazy przy pending migrations. Nie zmieniaj `GF3_DATA_VOLUME` podczas aktualizacji; inna nazwa może wskazać pusty wolumin. Nie uruchamiaj `docker compose down -v`.

Przed aktualizacją zbuduj/pobierz wybrane wydanie, ustal okno prac, zatrzymaj zapisy i wykonaj niezależną kopię woluminu (§3). Automatyczne migracje wymagają zgodnej wersji obrazu i bazy; startup robi quick_check, kopię warunkową, migracje, quick_check. Błąd zatrzymuje zdrowy start. Nie wyłączaj guardów w celu obejścia błędu aktualizacji.

### Instalacja i sprawdzenie

Poniższe polecenia są instrukcjami dla administratora po przygotowaniu środowiska; nie wykonano ich podczas pisania dokumentacji. Uruchamiaj z głównego katalogu wdrożenia. Wybierz budowę ze źródeł albo `pull` wskazanego, dostępnego obrazu (`GF3_IMAGE`); nie musisz robić obu.

HTTP / budowa lokalna:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml build gf3-app
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs --tail=100 gf3-app nginx
curl --fail http://127.0.0.1/api/health
```

Dla obrazu z rejestru zamiast build użyj `docker compose --env-file deploy/.env -f deploy/docker-compose.yml pull`, po zapewnieniu dostępu do rejestru. Numer wydania/obrazu zanotuj przed startem; `latest` sam nie identyfikuje niezmiennego wydania.

HTTPS: przygotuj ścieżki `GF3_TLS_CERT_PATH`, `GF3_TLS_KEY_PATH`, zgodne z domeną/certyfikatem, następnie:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml -f deploy/docker-compose.https.yml up --build -d
docker compose --env-file deploy/.env -f deploy/docker-compose.yml -f deploy/docker-compose.https.yml ps
curl --fail https://<DOMENA>/api/health
```

`<DOMENA>` zastąp uzgodnioną domeną. Dla rejestrowego wydania użyj pull i up bez `--build`. We wszystkich kolejnych operacjach HTTPS (stop/up/logs/restart) używaj tych samych dwóch plików Compose i tej samej konfiguracji. Samo HTTP nie wymaga overlay HTTPS.

Kolejność: wolumin/konfiguracja gotowe → `gf3-app` uruchamia backend, kontroluje i migruje bazę → health aplikacji → nginx startuje po `depends_on: service_healthy` → przeglądarka ładuje web/PWA → uprawniona rola wykonuje uzgodniony bezpieczny odczyt. To dwa kontenery i osadzona SQLite, bez floty mikrousług. Dockerfile buduje React i kopiuje go do `wwwroot` backendu; Vite nie obsługuje produkcji. `index.html`/`version.json`: `no-store, max-age=0`; `/assets`: roczny immutable; pozostałe pliki: `no-cache, must-revalidate`. Nie ma tu osobnego zadania budowania historii ani rozgrzewania cache przed startem. Klient może wymagać odświeżenia po zmianie wersji.

### Warunki biznesowe uruchomienia

Przed pierwszym rzeczywistym grafikiem właściciel dostarcza strukturę organizacji/sklepy, konta menedżerów/pracowników, przypisania, okres i reguły dyspozycyjności, parametry zmian/obsady i limity. Menedżer konfiguruje dane, zbiera dostępność, generuje, sprawdza i publikuje. Realnych użytkowników dopuszcza się po konfiguracji kont, publikacji i uzgodnionym odbiorze. Nie ma potwierdzonego automatycznego harmonogramu zaproszeń/onboardingu.

System ma administracyjny importer SQL obsługujący własny „Export to Code” z grafików/kontenerów. Nie jest to automatyczny konektor migracji z dowolnego innego systemu. Ręczne wprowadzenie lub konwersja danych zewnętrznych, odpowiedzialność i kontrola jakości muszą być osobno uzgodnione.

| Zdarzenie/warunek | Wymagane? tak/nie/do uzgodnienia | Sposób ustalenia |
| --- | --- | --- |
| Okno serwisowe i czasowe zatrzymanie zapisów przy aktualizacji/restore | Tak dla bezpiecznej podmiany aktywnych danych; termin do uzgodnienia | Administrator z właścicielem informują użytkowników |
| Data go-live i etapowanie grup użytkowników | Do uzgodnienia | Osoba odbierająca zatwierdza zakres i kryteria |
| Gotowość zewnętrznego zestawu danych/konwersji | Do uzgodnienia, jeśli używany | Właściciel danych zatwierdza kompletność |
| Zamknięcie sklepu / start roku podatkowego | Nie jest wymaganiem aplikacji; ewentualna decyzja klienta | Nie wynika z implementacji GF |
| Włączenie SMTP/push | Do uzgodnienia według używanych funkcji | Administrator wykonuje osobny test bez wysyłki do rzeczywistych odbiorców |

## 3. Kopie zapasowe

### Odpowiedzialnosc i fakty implementacji

Aplikacja (`DatabaseAutoBackupHostedService`) tworzy pełny snapshot SQLite przez `SqliteConnection.BackupDatabase` co godzinę **podczas działania procesu API**; pierwszy okresowy tick następuje po godzinie od startu. Restart/wyłączenie procesu nie jest zewnętrznym harmonogramem backupów. Błędy kopii są logowane; sam upływ godziny nie dowodzi sukcesu. Workspace umieszcza pliki w podkatalogu `backups` wybranej bazy, w typowym Dockerze `/data/backups`. Zachowuje **10 najnowszych automatycznych kopii**; przy czyszczeniu nie usuwa pliku używanego jako aktywna baza. Kopie ręczne są odrębne i nie podlegają tej automatycznej retencji.

Administrator/dostawca środowiska odpowiada za uzgodniony harmonogram kopii woluminu/hosta, przeniesienie poza serwer, kontrolę dostępu, monitoring i próbę odtworzenia. Klient/właściciel zatwierdza retencję, uprawnienia i akceptowany zakres utraty danych/czas przywrócenia zgodnie z umową. Podział wymaga realnego uzgodnienia; repozytorium nie potwierdza istniejącej usługi off-site ani certyfikacji ISO 27001.

| Rodzaj | Kto odpowiada | Częstotliwość/wyzwalacz | Liczba/retencja | Lokalizacja | Ograniczenia | Jak zweryfikować |
| --- | --- | --- | --- | --- | --- | --- |
| Automatyczny pełny snapshot SQLite | Proces GF; administrator monitoruje | Co 1 h pracy API; pierwszy po 1 h | 10 najnowszych automatycznych, z ochroną aktywnego pliku | Workspace/backups, zwykle /data/backups | Ten sam wolumin; brak kopii przy wyłączonym procesie; błędy mogą wystąpić | Sprawdź log utworzenia, datę/rozmiar pliku i quick_check izolowanej kopii |
| Snapshot przed migracją | Startup GF; administrator zapewnia miejsce/zgodność | Istniejąca baza + pending migrations + BackupBeforeMigrate=true | Używa mechanizmu kopii automatycznych i jego retencji | Ten sam katalog backups | Nie jest niezależnym off-site; brak pending migrations oznacza brak tej dodatkowej kopii | Log pre-migration, plik, zgodność czasu z wdrożeniem i próba na izolowanej kopii |
| Ręczna kopia workspace | Uprawniony operator | Jawna operacja administratora | Poza automatyczną retencją 10; polityka do uzgodnienia | Katalog kopii ręcznych workspace | UI/API chronione; normalnie admin tools w produkcji wyłączone | Lista plików, integralność i odtworzenie testowe |
| Archiwum woluminu / snapshot poza hostem | Administrator/dostawca według uzgodnienia | Przed ryzykowną aktualizacją; stały interwał do uzgodnienia | Do uzgodnienia / niepotwierdzone | Niezależny storage/region do uzgodnienia | Nie jest automatycznie dostarczoną funkcją GF; wymaga bezpiecznej spójnej kopii i osobnych sekretów/obrazu | Lista archiwum, suma kontrolna, dostęp do kopii poza hostem i protokół restore |

Baza i wbudowane backupy **współdzielą ten sam wolumin Docker**. Nie chroni to przed utratą hosta/woluminu. Dla wiarygodnego DR potrzebna jest odrębna decyzja o kopii poza hostem, częstotliwości, retencji, szyfrowaniu, lokalizacji/regionie, dostępie i testach. RPO/RTO: **niepotwierdzone w repozytorium, do uzgodnienia po pomiarach**. Wymogi ochrony danych osobowych/RODO należy uwzględnić w dostępie, lokalizacji i retencji; dokument nie gwarantuje zgodności prawnej.

Serwerowa SQLite jest źródłem danych kont/pracy. Stan PWA/przeglądarki (sesja/preferencje/cache) nie jest niezależną mobilną bazą do odtworzenia organizacji. Backup pamięci iPhone nie jest procedurą odzyskania GF.

### Bezpieczne archiwum operatora

Instrukcja dla administratora po zatwierdzeniu okna prac: sprawdź `GF3_DATA_VOLUME` w prywatnej konfiguracji i potwierdź przez `docker volume inspect <WOLUMIN>`. Zatrzymaj nginx i aplikację, aby nie było zapisów w trakcie archiwizacji. Zachowaj bieżącą konfigurację/identyfikator obrazu osobno od jawnych dowodów; sekrety przechowuj bezpiecznie. Przykład runbooka, Bash, wartości zastępcze trzeba uzupełnić:

```bash
mkdir -p backups
docker compose --env-file deploy/.env -f deploy/docker-compose.yml stop nginx gf3-app
docker volume inspect <WOLUMIN>
docker run --rm -v <WOLUMIN>:/data:ro -v "$PWD/backups:/backup" alpine tar czf /backup/<PLIK_KOPII>.tar.gz -C /data .
```

Sprawdź kod wyjścia, istnienie pliku, listę `tar tzf` i sumę kontrolną. Skopiuj archiwum poza host zgodnie z uzgodnioną polityką i potwierdź dostęp. Po bezpiecznym zakończeniu prac uruchom Compose tymi samymi plikami (HTTPS: dodaj overlay) i sprawdź zdrowie. `--rm` usuwa tylko kontener pomocniczy, nie wolumin. Nie wykonuj tej instrukcji przeciw żywej instalacji w ramach tworzenia dokumentacji.

## 4. Dokumentacja odtworzenia systemu

### Przygotowanie wspolne

Wybierz (A) snapshot SQLite, gdy jest nienaruszona kopia aplikacyjna, albo (B) pełne archiwum woluminu, gdy potrzebne jest odtworzenie całego trwałego workspace. Odtworzenie przywraca stan z czasu kopii, nie scala automatycznie późniejszych zapisów. Rollback oznacza powrót do zgodnej pary wcześniejszej bazy i aplikacji. Samo cofnięcie obrazu po migracji może nie działać: stary kod nie musi rozumieć nowego schematu.

1. Uzyskaj zgodę właściciela na zakres utraty zmian i czas przerwy. Zanotuj środowisko, nazwę woluminu, wydanie/commit/obraz, czas kopii i operatora.
2. Preferuj próbę na syntetycznych danych w odizolowanym środowisku. Przy kopii rzeczywistych danych ogranicz dostęp i **wyłącz wychodzące SMTP i push**; nie uruchamiaj kopii jako publicznej produkcji.
3. Uzgodnij maintenance, zatrzymaj ruch/zapisy (nginx i gf3-app), potwierdź brak innych procesów korzystających z pliku. Zachowaj **osobną kopię aktualnego woluminu przed podmianą**, także gdy podejrzewasz uszkodzenie.
4. Sprawdź istnienie/rozmiar/czas wybranej kopii, jej sumę i listę archiwum, pojemność dysku, zgodność ścieżek, uprawnień, obrazu i sekretów. Przechowaj oryginał źródłowy bez zmian. Nie startuj aplikacji na kopii przeznaczonej do zachowania jako dowód.
5. Przygotuj osobny pusty wolumin testowy o zweryfikowanej nazwie i odrębną konfigurację Compose/portów. Zmiana `GF3_DATA_VOLUME` dotyczy wyłącznie izolowanego środowiska próby, nigdy milcząco bieżącej produkcji. Zweryfikuj model konfiguracji przed startem.

### A. Odtworzenie z aplikacyjnego snapshotu SQLite

1. Wybierz konkretny pełny plik z `backups` lub kopii ręcznych; pracuj na jego kopii. Ustal wydanie zgodne ze schematem w chwili wykonania snapshotu. Jeśli snapshot jest jedyną kopią na uszkodzonym woluminie, najpierw zabezpiecz go na niezależnym nośniku.
2. W izolowanym środowisku przygotuj **pusty** wolumin i skopiuj wybrany snapshot pod nazwą `/data/SQLite.db`. Nie przenoś plików `SQLite.db-wal`/`SQLite.db-shm` z innej bazy. Nie nakładaj snapshotu na bazę otwartą przez aplikację.
3. Przykład narzędzia woluminowego z runbooka (Bash, polecenia do uzupełnienia dopiero po kontroli celu):

```bash
docker volume inspect <WOLUMIN_TESTOWY>
docker run --rm -v <WOLUMIN_TESTOWY>:/data -v "<KATALOG_KOPII>:/backup:ro" alpine cp /backup/<WYBRANY_SNAPSHOT> /data/SQLite.db
```

**Podmiana pliku jest destrukcyjna, jeśli cel już istnieje. Nie wykonuj jej bez potwierdzenia pustego/właściwego celu, zatrzymanych zapisów i zachowanej kopii aktualnego woluminu.** Do pierwszej próby użyj pustego izolowanego celu. Dopasuj właściciela/uprawnienia do użytkownika `app` obrazu; nie ustawiaj szerokiego dostępu tylko dla obejścia błędu.

4. Na odtworzonej kopii sprawdź SQLite `PRAGMA quick_check;` przez dostępne narzędzie SQLite; oczekiwane pojedyncze `ok`. Narzędzie SQLite nie jest gwarantowanym programem w obrazie aplikacji; administrator zapewnia je w izolowanym środowisku. Startup GF dodatkowo wykonuje quick_check przed i po migracji. Wynik integralności nie dowodzi poprawności biznesowej.
5. Uruchom zgodny obraz backendu z frontendem, tym samym modelem Compose/nginx i testową konfiguracją. Dla odtwarzanej istniejącej bazy pozostaw RequireExistingDatabase=true i BackupBeforeMigrate=true. Sprawdź migracje; nie pozwól starszej aplikacji przypadkowo pracować z nowszym schematem. Nowa migracja na odtworzonej kopii jest osobnym kontrolowanym etapem aktualizacji.
6. Po pełnej weryfikacji administrator uzgadnia przeniesienie odtworzonego stanu do **dokładnie zweryfikowanego** celu produkcyjnego w maintenance, ponownie zachowując aktualny stan. Nie przełączaj produkcji na testowy wolumin przez przypadkową zmianę nazwy.

### B. Odtworzenie calego woluminu i aplikacji

1. Zapewnij zweryfikowane archiwum woluminu oraz zgodny obraz/źródła i prywatną konfigurację/certyfikaty. Archiwum woluminu nie zawiera automatycznie obrazu Docker ani wszystkich sekretów hosta.
2. Zatrzymaj aplikację/nginx. Sprawdź listę archiwum przed rozpakowaniem (bez ścieżek wykraczających poza docelowy katalog), integralność pliku oraz dokładną nazwę wybranego woluminu. Najpierw odtwarzaj do pustego odizolowanego woluminu. Przykład:

```bash
docker volume inspect <WOLUMIN_TESTOWY>
docker run --rm -v "<KATALOG_ARCHIWUM>:/backup:ro" alpine tar tzf /backup/<ARCHIWUM>.tar.gz
docker run --rm -v <WOLUMIN_TESTOWY>:/data -v "<KATALOG_ARCHIWUM>:/backup:ro" alpine tar xzf /backup/<ARCHIWUM>.tar.gz -C /data
```

Rozpakowanie może nadpisać pliki: **destrukcyjne na niepustym celu — wymaga potwierdzenia celu i kopii stanu sprzed operacji**. Starszy runbook zawiera `rm -rf /data/*` przed restore; to destrukcyjne czyszczenie. Nie wykonuj go, dopóki operator nie potwierdzi właściwego woluminu, zatrzymania wszystkich zapisów i niezależnej kopii. Nie usuwaj innego woluminu ani nie używaj `down -v`. Procedura nie wymaga nowego skryptu/endpointu przywracania.

3. Sprawdź `/data/SQLite.db`, uprawnienia i quick_check jak w A. Przywróć pasującą wersję obrazu, konfigurację nginx i sekrety przez istniejące kanały administratora. Przy rollbacku wybierz kopię sprzed migracji pasującą do starego obrazu; sam rollback kodu nie odwraca EF migrations.
4. Uruchom aplikację Compose, sprawdź jej health i logi; nginx rusza po zdrowiu app. Używaj HTTPS overlay tylko w wariancie HTTPS. Sprawdź web/PWA i role według poniższej listy, zanim otworzysz ruch produkcyjny.

### Weryfikacja i niepowodzenie

Sprawdź logi startupu/migracji, integralność, uprawnienia, `docker compose ... ps`, `curl --fail .../api/health`. Health potwierdza połączenie z bazą, **nie pełne odzyskanie systemu**. Porównaj wybrane syntetyczne/uzgodnione rekordy: konta i role, sklepy, miesiąc grafiku, przypisania, publikację, dyspozycyjność, historię/korekty. Odczytaj jako manager i pracownik bez modyfikowania rzeczywistych grafików. Sprawdź prawidłowe publiczne/prywatne dane oraz brak wysyłki w próbie izolowanej.

Przywrócony stan może nie odpowiadać starej sesji lub cache przeglądarki. W razie potrzeby wyloguj/zaloguj ponownie, odśwież aplikację i sprawdź identyfikator wersji; cache index/version jest no-store, zasoby hashowane immutable zgodnie z `ApplyFrontendCacheHeaders`. Czyszczenie przeglądarki nie naprawia bazy.

Zapisz faktyczny wynik, wybrany plik/czas, obraz, migracje, czas odtworzenia i obserwacje. Zachowaj źródłowy backup i stan sprzed podmiany do audytu zgodnie z polityką. Jeśli wystąpi błąd integralności, uprawnień, schematu lub danych, **nie włączaj zapisów produkcyjnych**. Zachowaj wcześniejszy bezpieczny snapshot, eskaluj do administratora/wykonawcy, rozwiąż przyczynę i powtórz próbę. Nie oznaczaj checklisty jako udanej na podstawie samego planu.

### Jednostronicowa checklista odtworzenia

Formularz do wypełnienia podczas rzeczywistej próby; pola wyników są celowo puste.

| Etap / pole kontrolne | Wartość / wynik / dowód |
| --- | --- |
| Data, operator, zgoda właściciela, cel próby/produkcji | |
| Środowisko, dokładna nazwa docelowego woluminu, URL/porty | |
| Wariant A/B, źródłowy backup, czas wykonania, suma | |
| Pasujący obraz/commit i konfiguracja dostępne | |
| Okno prac, zapisy zatrzymane, inni użytkownicy odłączeni | |
| Osobna kopia aktualnego woluminu zachowana poza celem | |
| Izolacja próby, SMTP/push wyłączone, dostęp ograniczony | |
| Miejsce, uprawnienia i sekrety zweryfikowane | |
| Cel pusty / podmiana jawnie zatwierdzona | |
| Przywrócony SQLite.db lub archiwum, pliki WAL zgodne | |
| quick_check przed startem / po migracji, logi | |
| App healthy → nginx → web/PWA, wersja klienta | |
| Health i bezpieczny odczyt kont/roli/publikacji/grafiku | |
| Wynik, problemy, eskalacja i termin ponownej próby | |
| Decyzja o wznowieniu zapisów, osoba zatwierdzająca | |
| Źródłowy backup i stan sprzed restore zabezpieczone | |

## 5. Podstawa techniczna i pokrycie materialow

[Dockerfile](../../Dockerfile), [Compose HTTP](../../deploy/docker-compose.yml), [HTTPS](../../deploy/docker-compose.https.yml), [nginx](../../deploy/nginx/), [runbook](../deploy.md), [backup okresowy](../../GF3.WebApi/Infrastructure/DatabaseAutoBackupHostedService.cs), [workspace/kopie](../../DataAccessLayer/Administration/SqliteDatabaseWorkspace.cs), [migracje startup](../../GF3.WebApi/Infrastructure/DatabaseMigrationStartup.cs), [cache](../../GF3.WebApi/Infrastructure/StartupConfiguration.cs), [health](../../GF3.WebApi/Controllers/HealthController.cs).

Prezentacja 3: s. 2 — warianty odbioru, protokół i harmonogram (§1); s. 3 — techniczne warunki/instalacja (§2); s. 4 — warunki biznesowe/start (§2); s. 5–6 — odpowiedzialność, typy, częstotliwość, retencja i miejsce backupu (§3); s. 7 — odtworzenie aplikacji i bazy, kolejność usług, kontrola wyniku (§4). Niepotwierdzone uzgodnienia handlowe, off-site, RPO/RTO oraz wyniki prób nie zostały zastąpione fikcyjnymi wartościami. Starsze szkice w docs/legal i docs/client-launch-package nie są dowodem podpisania umowy ani przyjęcia produktu.
