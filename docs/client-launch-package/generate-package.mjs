import { mkdir, readFile, readdir, writeFile } from "node:fs/promises";
import { dirname, join, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { createHash } from "node:crypto";

const packageRoot = dirname(fileURLToPath(import.meta.url));
const docsRoot = join(packageRoot, "..");
const repoRoot = join(docsRoot, "..");
const records = [];
const created = new Map();

const GDPR = "https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679";
const UODO = "https://uodo.gov.pl/pl/598/3563";
const EDPB = "https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en";
const USUDE = "https://eli.gov.pl/eli/DU/2024/1513/ogl";
const BIZNES = "https://biznes.gov.pl/pl/firma/zakladanie-firmy/chce-wiedziec-jak-zalozyc-wlasna-firme/dzialalnosc-nierejestrowa-oraz-inne-sytuacje-w-ktorych-nie-trzeba-rejestrowac-firmy";
const KSEF = "https://ksef.podatki.gov.pl/informacje-ogolne-ksef-20/";
const DN = "https://podatki.gov.pl/poradniki-i-informatory/dzialalnosc-nierejestrowana";

function metadata(title, related = "[DO WYPEŁNIENIA: DOKUMENTY POWIĄZANE]", classification = "WEWNĘTRZNY / DO UZGODNIENIA") {
  return `# ${title}\n\n| Metadane | Wartość |\n| --- | --- |\n| Produkt | GF |\n| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |\n| Status | DRAFT – WYMAGA WERYFIKACJI |\n| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |\n| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |\n| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |\n| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |\n| Dokumenty powiązane | ${related} |\n| Klasyfikacja | ${classification} |\n\n> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.\n\n`;
}

function bases(items) {
  return `\n## Podstawy i materiały do weryfikacji\n\n${items.map(([label, url]) => `- [${label}](${url})`).join("\n")}\n`;
}

function add(path, title, purpose, audience, owner, body, options = {}) {
  const content = metadata(title, options.related, options.classification) + body.trim() + "\n";
  created.set(path, content);
  records.push({ path, title, purpose, audience, owner, beforeDemo: options.demo ?? "Nie", beforeSignature: options.signature ?? "Nie", beforeProduction: options.production ?? "Nie", status: "DRAFT" });
}

function addRaw(path, title, purpose, audience, owner, content, options = {}) {
  if (arguments.length === 2) {
    content = title;
    title = path.split("/").at(-1);
    purpose = "Szablon rejestru";
    audience = "Właściciel procesu";
    owner = "Właściciel SaaS";
    options = {};
  }
  created.set(path, content);
  records.push({ path, title, purpose, audience, owner, beforeDemo: options.demo ?? "Nie", beforeSignature: options.signature ?? "Nie", beforeProduction: options.production ?? "Nie", status: "DRAFT" });
}

const dpaBases = bases([["RODO – art. 28, 29, 32–36", GDPR], ["Wytyczne EROD 07/2020 – administrator i podmiot przetwarzający", EDPB], ["UODO – materiały o naruszeniach", UODO]]);
const gdprBases = bases([["RODO – art. 5, 24–36", GDPR], ["UODO – materiały o naruszeniach", UODO], ["Wytyczne EROD 07/2020", EDPB]]);
const legalBases = bases([["RODO", GDPR], ["Ustawa o świadczeniu usług drogą elektroniczną – ELI", USUDE], ["biznes.gov.pl – działalność nierejestrowana", BIZNES]]);
const taxBases = bases([["biznes.gov.pl – działalność nierejestrowana", BIZNES], ["podatki.gov.pl – działalność nierejestrowana", DN], ["KSeF 2.0 – informacje Ministerstwa Finansów", KSEF]]);

add("01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md", "UMOWA POWIERZENIA PRZETWARZANIA DANYCH OSOBOWYCH", "Umowa z art. 28 RODO", "Administrator i Podmiot przetwarzający", "Usługodawca / prawnik / IOD", `
## Strony

zawarta pomiędzy **[DO WYPEŁNIENIA: PEŁNA NAZWA, ADRES, KRS I NIP KLIENTA]**, reprezentowanym przez **[DO WYPEŁNIENIA: REPREZENTANT KLIENTA]**, dalej „Administratorem”, a **[DO WYPEŁNIENIA: IMIĘ, NAZWISKO I PUBLICZNE DANE USŁUGODAWCY]**, dalej „Podmiotem przetwarzającym”.

## § 1. Definicje

1. Administrator – Klient określający cele i sposoby przetwarzania danych pracowników i danych organizacyjnych.
2. Podmiot przetwarzający – Usługodawca przetwarzający Dane osobowe na udokumentowane Polecenie Administratora.
3. Podmiot podprzetwarzający – podmiot przetwarzający Dane osobowe w imieniu Podmiotu przetwarzającego.
4. Dane osobowe, Przetwarzanie, Naruszenie ochrony danych osobowych i RODO mają znaczenie nadane w RODO.
5. Dane Klienta – dane i treści wprowadzone do Platformy lub przekazane w związku z Usługą SaaS.
6. Platforma – aplikacja SaaS GF pod adresem **[DO WYPEŁNIENIA: ADRES PLATFORMY]**.
7. Polecenie Administratora – udokumentowana instrukcja wynikająca z Umowy SaaS, niniejszej Umowy, konfiguracji Platformy albo autoryzowanego zgłoszenia.

## § 2. Przedmiot i czas

1. Administrator powierza przetwarzanie w celu świadczenia Usługi SaaS opisanej w Umowie o świadczenie usług SaaS z dnia **[DO WYPEŁNIENIA: DATA UMOWY SAAS]**.
2. Powierzenie trwa przez okres Umowy SaaS oraz ograniczony okres eksportu, usuwania z systemu aktywnego i cyklu kopii. Dalsze przechowywanie jest dopuszczalne tylko, gdy wymaga tego prawo.
3. Dane nie są wykorzystywane poza Poleceniami, chyba że obowiązek wynika z prawa; w takim przypadku Podmiot przetwarzający informuje Administratora przed przetwarzaniem, o ile prawo tego nie zabrania.

## § 3. Charakter i cel

1. Operacje obejmują zapis, organizowanie, odczyt, zmianę, publikowanie uprawnionym Użytkownikom, transmisję, eksport, tworzenie kopii, ograniczanie i usuwanie.
2. Potwierdzone cele obejmują profile i Konta, uwierzytelnianie, grafiki, dostępność, zamiany zmian, komunikaty i powiadomienia, logi operacyjne, wsparcie, bezpieczeństwo, kopie i eksport.
3. Ogólne korekty inne niż dostępność i zamiany, pliki oraz czat nie są objęte, jeżeli nie zostaną wdrożone i udokumentowane.

## § 4. Osoby i dane

1. Kategorie osób i danych określa Załącznik B. Obejmują pracowników, menedżerów, Administratora Klienta i byłych Użytkowników, których zapis musi pozostać w historii.
2. System przechowuje m.in. imię, nazwisko, telefon, e-mail, nazwę Użytkownika, rolę, identyfikatory, lokalizację organizacyjną, grafiki, godziny, dostępność, zamiany, komunikaty, statusy odczytu oraz metadane logowania i działań.
3. Nie potwierdzono strukturalnych pól zdrowia, niepełnosprawności, biometrii, związków zawodowych, religii, poglądów, orientacji seksualnej ani skazań. Administrator nie powinien ich wprowadzać. Pola notatek i komunikatów stwarzają ryzyko wpisania danych nadmiarowych.

## § 5. Polecenia

1. Podmiot przetwarzający działa wyłącznie na Polecenia i nie wykorzystuje danych pracowników dla własnych niezależnych celów.
2. O Poleceniu, które w ocenie Podmiotu przetwarzającego narusza prawo, informuje Administratora i wstrzymuje jego wykonanie w dopuszczalnym zakresie.

## § 6. Poufność

1. Dostęp otrzymują wyłącznie osoby upoważnione w zakresie niezbędnym do zadania, objęte poufnością także po ustaniu dostępu.
2. Uprawnienia są odbierane niezwłocznie po ustaniu potrzeby.

## § 7. Bezpieczeństwo

1. Podmiot przetwarzający utrzymuje środki odpowiednie do ryzyka zgodnie z art. 32 RODO i Załącznikiem C.
2. Załącznik C rozróżnia stan potwierdzony, luki i działania zalecane; nie potwierdza certyfikacji ani pełnego bezpieczeństwa.

## § 8. Podprzetwarzanie

1. Wersja robocza stosuje ogólne pisemne upoważnienie z uprzednim powiadomieniem **[DO WERYFIKACJI PRAWNEJ: TERMIN POWIADOMIENIA O NOWYM PODPRZETWARZAJĄCYM]** i prawem uzasadnionego sprzeciwu.
2. Lista stanowi Załącznik D. Podmiot przetwarzający zawiera z Podmiotem podprzetwarzającym obowiązki nie mniej chroniące niż niniejsza Umowa.
3. Awaryjna zmiana dla bezpieczeństwa lub ciągłości jest dopuszczalna z powiadomieniem bez zbędnej zwłoki. **[DO WERYFIKACJI RODO: MODEL AUTORYZACJI PODPRZETWARZAJĄCYCH]**

## § 9. Transfery międzynarodowe

1. Region hostingu i dostawca SMTP nie są potwierdzone. Dane nie mogą być przekazane poza EOG bez udokumentowania podstawy.
2. W razie transferu stosuje się odpowiednio decyzję o adekwatności, standardowe klauzule umowne, ocenę transferu i środki uzupełniające. **[DO WERYFIKACJI RODO: REGIONY I MECHANIZMY TRANSFEROWE]**

## § 10. Pomoc Administratorowi

1. Podmiot przetwarzający rozsądnie pomaga przy prawach osób, bezpieczeństwie, ocenie Naruszenia, DPIA, konsultacji i wykazaniu zgodności.
2. Rozległe prace mogą być dodatkowo płatne po odrębnym uzgodnieniu, z wyjątkiem pomocy wynikającej z naruszenia przypisanego Podmiotowi przetwarzającemu.

## § 11. Żądania osób

1. Żądanie jest rejestrowane i przekazywane Administratorowi bez zbędnej zwłoki. Podmiot przetwarzający nie odpowiada merytorycznie bez upoważnienia.
2. Udostępnia dostępne informacje i funkcje potrzebne do wykonania decyzji Administratora.

## § 12. Naruszenia

1. Nie każdy Incydent bezpieczeństwa jest Naruszeniem ochrony danych. Po stwierdzeniu lub wiarygodnym podejrzeniu Naruszenia Podmiot przetwarzający zawiadamia Administratora bez zbędnej zwłoki.
2. Pierwsza informacja może być niepełna i jest aktualizowana. Obejmuje znany charakter, systemy, kategorie i przybliżoną skalę danych i osób, możliwe skutki, zabezpieczenie oraz środki zaradcze.
3. Kanał: **[DO WYPEŁNIENIA: ADRES INCYDENTOWY]**.

## § 13. Audyt

1. Najpierw stosuje się dokumentację, kwestionariusz i dowody, następnie audyt zdalny, a audyt na miejscu tylko przy uzasadnionej potrzebie i rozsądnym wyprzedzeniu.
2. Audyt chroni poufność i dane innych klientów; testy destrukcyjne wymagają odrębnego uzgodnienia.
3. Koszty ponosi Administrator, chyba że potwierdzono istotne naruszenie obowiązków Podmiotu przetwarzającego. **[DO WERYFIKACJI PRAWNEJ: ZASADY I KOSZTY AUDYTU]**

## § 14. Zwrot lub usunięcie

1. Administrator żąda eksportu w okresie **[DO WERYFIKACJI TECHNICZNEJ: OKRES EKSPORTU – PROPOZYCJA 14 DNI]** w technicznie dostępnym formacie; aktualnie potwierdzono wybrane eksporty XLSX i SQL, nie pełny eksport wszystkich danych.
2. Usunięcie z systemu aktywnego następuje w okresie **[DO WERYFIKACJI TECHNICZNEJ: OKRES USUNIĘCIA Z SYSTEMU AKTYWNEGO – PROPOZYCJA 45 DNI]**.
3. Kopie są usuwane przez rotację: **[DO WERYFIKACJI TECHNICZNEJ: RETENCJA KOPII – KOD UTRZYMUJE 10 NAJNOWSZYCH KOPII]**. Nie gwarantuje się natychmiastowego fizycznego usunięcia z każdej kopii.
4. Procedurę i potwierdzenie określa Załącznik E; wyjątki prawne muszą być udokumentowane.

## § 15. Odpowiedzialność i pierwszeństwo

1. Umowa nie ustanawia odrębnego limitu odpowiedzialności; stosuje się zgodny z prawem limit Umowy SaaS, bez wyłączenia szkody umyślnej.
2. W sprawach ochrony danych pierwszeństwo ma niniejsza Umowa, a w pozostałych – Umowa SaaS.

## § 16. Końcowe

1. Umowa trwa jak powierzenie. Zmiany wymagają formy **[DO WERYFIKACJI PRAWNEJ: PISEMNEJ LUB DOKUMENTOWEJ]**.
2. Stosuje się prawo polskie i sąd zgodny z Umową SaaS. Załączniki A–E są integralne.
3. Podpisy: Administrator **[DO WYPEŁNIENIA: PODPIS]**; Podmiot przetwarzający **[DO WYPEŁNIENIA: PODPIS]**.
${dpaBases}`, { related: "Umowa SaaS; Załączniki A–E", signature: "Tak", production: "Tak", classification: "UMOWNY / POUFNY" });

add("01-data-processing-agreement/zalacznik-a-opis-przetwarzania.md", "Załącznik A – opis przetwarzania", "Opis art. 28 RODO", "Strony DPA", "IOD / właściciel SaaS", `
| Element | Opis |
| --- | --- |
| Przedmiot | Obsługa Platformy GF dla Klienta |
| Czas | Okres Umowy SaaS plus eksport, usuwanie i rotacja kopii |
| Charakter | Zapis, organizowanie, odczyt, zmiana, udostępnienie uprawnionym, eksport, kopia, usunięcie |
| Cel | Grafiki, dostępność, zamiany, komunikacja, Konta, bezpieczeństwo i wsparcie |
| Moduły | Pracownicy, lokalizacje, dostępność, grafiki, zamiany, komunikaty, powiadomienia, logi, eksport |
| Częstotliwość | Ciągła podczas korzystania; kopia w kodzie co godzinę podczas działania procesu |
| Region hostingu | [DO WERYFIKACJI TECHNICZNEJ: REGION PRODUKCYJNY] |
| Kopie | SQLite; 10 najnowszych automatycznych kopii w tym samym obszarze danych – stan kodu |
| Dostęp wsparcia | [DO WYPEŁNIENIA: ROLE I PROCEDURA DOSTĘPU PRODUKCYJNEGO] |
| Eksport | Wybrane XLSX i SQL; pełny eksport do ustalenia |
| Usunięcie | Zgodnie z Załącznikiem E i instrukcją Administratora |`, { related: "DPA; Załącznik E", signature: "Tak", production: "Tak", classification: "UMOWNY / POUFNY" });

add("01-data-processing-agreement/zalacznik-b-kategorie-danych-i-osob.md", "Załącznik B – kategorie danych i osób", "Inwentarz DPA", "Strony DPA", "IOD", `
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

Nie potwierdzono pól przeznaczonych dla zdrowia, niepełnosprawności, biometrii, religii, poglądów, związków zawodowych, orientacji, życia seksualnego ani skazań. Pola notatek, nazw i komunikatów mogą przyjąć dowolny tekst; należy zakazać danych nadmiarowych i wdrożyć instrukcje. **[DO WERYFIKACJI RODO: CZY KLIENT PLANUJE DANE SZCZEGÓLNYCH KATEGORII LUB SKAZAŃ]**`, { related: "DPA § 4", signature: "Tak", production: "Tak", classification: "UMOWNY / POUFNY" });

add("01-data-processing-agreement/zalacznik-c-srodki-techniczne-i-organizacyjne.md", "Załącznik C – środki techniczne i organizacyjne", "Macierz TOM", "Strony DPA / audytor", "Właściciel techniczny / IOD", `
| Kontrola | Stan bieżący | Dowód w repozytorium | Luka | Działanie | Weryfikacja |
| --- | --- | --- | --- | --- | --- |
| Kontrola dostępu | Role menedżer/pracownik i autoryzacja API | kontrolery i router | Administrator nie jest osobną rolą | zatwierdzić macierz uprawnień | Techniczna |
| Uwierzytelnianie | JWT; Hasło dokładnie 6 cyfr | moduły auth | MFA i ograniczanie prób niepotwierdzone | przegląd bezpieczeństwa, MFA/rate limit | Techniczna/RODO |
| Hasła | PBKDF2-SHA256, losowa sól | PasswordHasher | parametry i polityka do okresowego przeglądu | test i plan aktualizacji | Techniczna |
| Sesja | Token JWT w localStorage | AuthProvider | ekspozycja przy XSS | ocenić bezpieczniejszy model | Techniczna |
| Transport | wariant nginx HTTPS istnieje | compose HTTPS | produkcyjne TLS niepotwierdzone | wymusić HTTPS i test konfiguracji | Techniczna |
| Szyfrowanie spoczynkowe | Niepotwierdzone | brak warstwy w kodzie | plik SQLite i kopie | szyfrowanie wolumenu/kopii | Techniczna |
| Sekrety | zmienne środowiskowe i przykład .env | compose | proces rotacji nieudokumentowany | magazyn i rotacja sekretów | Techniczna |
| Sieć | nginx, dev CORS, admin domyślnie ograniczony | konfiguracja API | hardening produkcyjny nieaudytowany | przegląd nagłówków i ekspozycji | Techniczna |
| Kopie | co godzinę podczas działania; 10 najnowszych | serwis kopii | ten sam wolumen, brak off-site i testu | zewnętrzna zaszyfrowana kopia i test | Techniczna |
| Odtwarzanie | biblioteka SQLite i narzędzia admin | workspace/admin | procedura i wynik testu brak | wykonać izolowany test | Techniczna |
| Logowanie | wybrane logi aktora, roli, czasu, działania | workflow logs | brak centralnego SIEM i polityki retencji | ustalić retencję i alerty | Techniczna/RODO |
| Monitoring | logi konsolowe, health endpoint | WebApi/deploy | monitoring i dyżur niepotwierdzone | healthcheck i alerty | Techniczna |
| Aktualizacje | CI i kontenery | workflow/Dockerfile | rytm patchowania brak | kalendarz i skan zależności | Techniczna |
| Incydenty | brak formalnego workflow w aplikacji | brak potwierdzenia | procedura operacyjna nowa | wdrożyć rejestr i kanał | Biznesowa |
| Separacja | jedna baza/organizacja | model i wdrożenie | brak multi-tenant izolacji | osobne wdrożenie/dane na Klienta | Techniczna |
| Usuwanie | CRUD i kasowanie logów; zależności blokują | kontrolery | brak pełnej orkiestracji usunięcia | runbook i protokół | Techniczna/RODO |
| Minimalizacja | pola profilu są ograniczone | modele | wolny tekst może zawierać nadmiar | instrukcja i limity | RODO |
| Poufność osób | Niepotwierdzona organizacyjnie | poza repozytorium | brak dokumentów | upoważnienia i zobowiązania | Biznesowa |
| Dostawcy | SMTP i hosting do ustalenia | konfiguracja ogólna | brak DPA/listy/regionu | kwalifikacja i umowy | RODO |
| Ciągłość | kontenery i kopie lokalne | deploy | brak RTO/RPO i alternatywy | zatwierdzić plan | Biznesowa/techniczna |`, { related: "DPA § 7; polityka bezpieczeństwa", signature: "Tak", production: "Tak", classification: "UMOWNY / POUFNY" });

add("01-data-processing-agreement/zalacznik-d-podmioty-podprzetwarzajace.md", "Załącznik D – podmioty podprzetwarzające", "Lista dostawców danych", "Administrator", "Usługodawca / IOD", `
| Dostawca | Usługa / rola | Zakres | Kraj/region | Poza EOG | Mechanizm | Link DPA/prywatności | Dodano | Uwagi |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| [DO WYPEŁNIENIA: DOSTAWCA HOSTINGU] | hosting produkcyjny | baza i aplikacja | [DO WYPEŁNIENIA: REGION HOSTINGU] | [DO WERYFIKACJI RODO: TRANSFER] | [DO WYPEŁNIENIA: MECHANIZM] | [DO WYPEŁNIENIA: LINK] | [DO WYPEŁNIENIA: DATA] | Dostawcy nie potwierdzono w repozytorium |
| [DO WYPEŁNIENIA: DOSTAWCA SMTP] | wysyłka kodów odzyskiwania | e-mail, nazwa, kod w wiadomości | [DO WYPEŁNIENIA: REGION SMTP] | [DO WERYFIKACJI RODO: TRANSFER] | [DO WYPEŁNIENIA: MECHANIZM] | [DO WYPEŁNIENIA: LINK] | [DO WYPEŁNIENIA: DATA] | Kod używany do resetu Hasła |
| [DO WYPEŁNIENIA: DOSTAWCA ZEWNĘTRZNYCH KOPII] | docelowa kopia off-site | zaszyfrowana baza | [DO WYPEŁNIENIA: REGION] | [DO WERYFIKACJI RODO: TRANSFER] | [DO WYPEŁNIENIA: MECHANIZM] | [DO WYPEŁNIENIA: LINK] | [DO WYPEŁNIENIA: DATA] | Brak potwierdzonej kopii off-site |

Nginx, Docker i SQLite są komponentami technicznymi, a nie automatycznie Podmiotami podprzetwarzającymi. YouTube/Google może być odrębnym odbiorcą przy ładowaniu zewnętrznego osadzenia; jego rola i aktywacja wymagają oceny. **[DO WERYFIKACJI RODO: KWALIFIKACJA ZEWNĘTRZNYCH MEDIÓW]**`, { related: "DPA § 8–9", signature: "Tak", production: "Tak", classification: "UMOWNY / POUFNY" });

add("01-data-processing-agreement/zalacznik-e-procedura-usuniecia-lub-zwrotu-danych.md", "Załącznik E – zwrot lub usunięcie danych", "Procedura zakończenia DPA", "Strony DPA", "Właściciel techniczny / IOD", `
1. Administrator składa udokumentowane Polecenie eksportu, zwrotu, usunięcia albo ograniczenia.
2. Strony potwierdzają zakres: profile, Konta, grafiki, dostępność, zamiany, komunikaty, logi, eksporty i kopie.
3. Eksport wykorzystuje wyłącznie dostępny format; pełny eksport wymaga **[DO WERYFIKACJI TECHNICZNEJ: ZAKRES I FORMAT]**.
4. Odbiorca i kanał dostawy są weryfikowani; plik jest usuwany po potwierdzeniu odbioru.
5. Dostępy i sesje są cofane, a system aktywny usuwany lub anonimizowany po zamknięciu rozliczeń.
6. Kopie wygasają przez rotację; opóźnione usunięcie i wyjątki prawne trafiają do protokołu.
7. Podmioty podprzetwarzające wykonują zgodne instrukcje.
8. Potwierdzenie opisuje faktycznie wykonane operacje i nie certyfikuje nieweryfikowalnego fizycznego nadpisania nośników.`, { related: "DPA § 14; pakiet 09", signature: "Tak", production: "Tak", classification: "UMOWNY / POUFNY" });

add("01-data-processing-agreement/checklista-dpa.md", "Checklista DPA", "Kontrola przed podpisaniem", "Usługodawca / Klient", "Prawnik / IOD", `
- [ ] Uzupełniono Strony, Umowę SaaS i podpisy.
- [ ] Administrator potwierdził kategorie danych, osoby i brak planowanych danych szczególnych.
- [ ] Zatwierdzono Polecenia, pomoc, audyt, naruszenia i hierarchię dokumentów.
- [ ] Uzupełniono hosting, SMTP, regiony, transfery, DPA dostawców i model autoryzacji.
- [ ] Załącznik C odpowiada produkcji, a luki mają właściciela i termin.
- [ ] Uzgodniono eksport, system aktywny, kopie i wyjątki retencji.
- [ ] Kanał Incydentów jest testowany.
- [ ] DPA podpisano przed przekazaniem danych pracowników.`, { related: "DPA i A–E", signature: "Tak", production: "Tak" });

add("01-data-processing-agreement/README.md", "README – DPA", "Instrukcja pakietu powierzenia", "Właściciel pakietu", "IOD", `
Podpisywane są DPA i uzgodnione Załączniki A–E. Załącznik C nie może opisywać rekomendacji jako stanu istniejącego, a D wymaga uzupełnienia przed produkcją. HTML jest generowany z wersji Markdown. Zmiana dostawcy wymaga aktualizacji D, oceny transferu i powiadomienia zgodnie z wybranym modelem.`, { related: "DPA; Umowa SaaS", signature: "Tak", production: "Tak" });

const privacyBases = bases([["RODO – art. 5, 6, 12–14, 24, 28, 30 i 32", GDPR], ["Wytyczne EROD 07/2020", EDPB], ["UODO", "https://uodo.gov.pl"]]);

add("02-privacy/analiza-rol-rodo.md", "Analiza ról RODO", "Rozdział ról administrator/procesor", "Usługodawca / Klient / IOD", "IOD", `
| Cel | Dane | Osoby | Rola Usługodawcy | Proponowana podstawa | Retencja | Odbiorcy | Weryfikacja |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Grafiki, profile, dostępność, zamiany, komunikaty | dane pracowników | Użytkownicy | Podmiot przetwarzający | Polecenie i DPA | wg Klienta/DPA | hosting, SMTP wg zakresu | [DO WERYFIKACJI RODO: ROLE] |
| Umowa i kontakt B2B | dane reprezentantów | kontakty Klienta | odrębny Administrator | art. 6 ust. 1 lit. b/f – propozycja | [DO WERYFIKACJI RODO: RETENCJA] | księgowość, doradcy | podstawa i balans |
| Rozliczenia i dokumentacja | dane kontaktowe i podatkowe | Strony | odrębny Administrator | art. 6 ust. 1 lit. c – propozycja | okres ustawowy | księgowość, organy | obowiązki krajowe |
| Roszczenia | umowa i komunikacja | Strony | odrębny Administrator | art. 6 ust. 1 lit. f – propozycja | przedawnienie plus bufor | prawnicy, sądy | balans |
| Bezpieczeństwo własnej infrastruktury | Konto, logi, Incydenty | Użytkownicy | do analizy: Administrator lub procesor | art. 6 ust. 1 lit. f / DPA | [DO WERYFIKACJI RODO: RETENCJA LOGÓW] | hosting | rozdzielić cele |
| Wsparcie | kontakt i treść zgłoszenia | zgłaszający | zależnie od treści | Umowa/DPA lub lit. f | [DO WERYFIKACJI RODO: RETENCJA WSPARCIA] | osoby wsparcia | procedura kwalifikacji |

Nie należy przyjmować jednej roli dla wszystkich operacji. Dane pracownicze pozostają co do zasady pod kontrolą Klienta; własne rozliczenia, kontrakt i roszczenia są odrębnymi celami Usługodawcy.${privacyBases}`, { related: "DPA; polityka prywatności", signature: "Tak", production: "Tak", classification: "WEWNĘTRZNY / RODO" });

add("02-privacy/polityka-prywatnosci-pl.md", "POLITYKA PRYWATNOŚCI PLATFORMY GF", "Publiczna informacja prywatności", "Odwiedzający, kontakty i Użytkownicy", "Usługodawca / IOD", `
## 1. Administrator i kontakt

Dla własnych celów opisanych poniżej administratorem jest **[DO WYPEŁNIENIA: DANE USŁUGODAWCY]**. Kontakt: **[DO WYPEŁNIENIA: E-MAIL RODO]**. Dla grafików, danych zatrudnieniowych i organizacyjnych administratorem jest zwykle Klient wskazany Użytkownikowi przez pracodawcę lub organizację.

## 2. Cele, dane i podstawy

Usługodawca jako odrębny administrator może obsługiwać kontakt B2B, zawarcie i wykonanie Umowy, rozliczenia, bezpieczeństwo, wsparcie i roszczenia. Podstawy – art. 6 ust. 1 lit. b, c lub f RODO – są propozycją **[DO WERYFIKACJI RODO: PODSTAWY DLA KAŻDEGO CELU]**. Kategorie obejmują dane kontaktowe, reprezentację, korespondencję, rozliczenia i techniczne metadane niezbędne do bezpieczeństwa.

## 3. Dane pracowników

Profile, grafiki, dostępność, godziny, zamiany, komunikaty i powiadomienia są co do zasady przetwarzane przez Usługodawcę jako Podmiot przetwarzający na Polecenie Klienta. Żądania dotyczące tych danych należy kierować przede wszystkim do Klienta.

## 4. Źródła, wymóg i konsekwencje

Dane pochodzą od osoby, Klienta lub z działania Konta. Dane umowne i kontaktowe są potrzebne do współpracy; ich brak może uniemożliwić kontakt lub dostęp. Pola dobrowolne są oznaczane w interfejsie lub formularzu.

## 5. Odbiorcy i transfery

Odbiorcami mogą być osoby upoważnione, księgowość, prawnicy, hosting i SMTP. Lista i regiony: **[DO WYPEŁNIENIA: LINK DO LISTY DOSTAWCÓW]**. Transfer poza EOG nie jest potwierdzony i wymaga podstawy oraz oceny. Zewnętrzne osadzenia YouTube lub obrazy mogą łączyć przeglądarkę z dostawcą – zob. Polityka cookies/storage.

## 6. Retencja

Okres zależy od celu, Umowy, instrukcji Klienta, roszczeń i obowiązków księgowych. Szczegóły: **[DO WYPEŁNIENIA: LINK DO TABELI RETENCJI]**. Kopie mają obecnie retencję liczbową 10 najnowszych, nie zatwierdzony okres czasowy.

## 7. Prawa

W granicach RODO przysługują dostęp, sprostowanie, usunięcie, ograniczenie, przenoszenie, sprzeciw oraz skarga do Prezesa UODO. Realizacja zależy od roli i podstawy. Kontakt do Klienta: **[DO WYPEŁNIENIA: DANE ADMINISTRATORA KLIENTA DLA UŻYTKOWNIKÓW]**.

## 8. Automatyzacja

Generator grafiku wspomaga organizację, ale decyzję i publikację wykonuje menedżer. Nie potwierdzono decyzji wywołujących skutki prawne podejmowanych wyłącznie automatycznie ani profilowania marketingowego. **[DO WERYFIKACJI RODO: OCENA ART. 22 I PROFILOWANIA]**

## 9. Technologie przeglądarki

Platforma używa localStorage, w tym do tokenu dostępu i ustawień. Może ładować zewnętrzne media. Szczegóły i preferencje zawiera Polityka cookies i storage: **[DO WYPEŁNIENIA: LINK]**.
${privacyBases}`, { related: "DPA; polityka cookies; Regulamin", demo: "Tak", production: "Tak", classification: "PUBLICZNY" });

add("02-privacy/klauzula-informacyjna-kontakty-b2b-pl.md", "Klauzula informacyjna – kontakty B2B", "Informacja art. 13/14", "Reprezentanci i kontakty Klienta", "Usługodawca / IOD", `
Administratorem danych jest **[DO WYPEŁNIENIA: DANE USŁUGODAWCY]**, kontakt **[DO WYPEŁNIENIA: E-MAIL RODO]**. Dane kontaktowe i reprezentacyjne są przetwarzane dla negocjacji, wykonania Umowy, rozliczeń, obowiązków prawnych i roszczeń na proponowanych podstawach art. 6 ust. 1 lit. b, c i f RODO **[DO WERYFIKACJI RODO: PODSTAWY]**. Dane pochodzą od osoby, jej organizacji lub publicznego rejestru. Odbiorcami mogą być hosting, poczta, księgowość, prawnicy i organy. Retencja odpowiada Umowie, obowiązkom i przedawnieniu **[DO WERYFIKACJI RODO: OKRESY]**. Przysługują prawa właściwe dla podstawy oraz skarga do Prezesa UODO.${privacyBases}`, { related: "polityka prywatności", signature: "Tak", production: "Tak", classification: "PUBLICZNY / DO PRZEKAZANIA" });

add("02-privacy/klauzula-informacyjna-uzytkownicy-platformy-pl.md", "Klauzula informacyjna – Użytkownicy Platformy", "Informacja dla pracowników", "Menedżerowie i pracownicy", "Klient / IOD", `
Administratorem danych grafikowych i zatrudnieniowo-organizacyjnych jest zwykle **[DO WYPEŁNIENIA: DANE KLIENTA JAKO ADMINISTRATORA]**, kontakt **[DO WYPEŁNIENIA: KONTAKT RODO KLIENTA]**. GF i Usługodawca działają w tym zakresie jako Platforma i Podmiot przetwarzający. Cele, podstawy, retencję, odbiorców oraz konsekwencje podania danych określa Klient **[DO WERYFIKACJI RODO: KLAUZULA KLIENTA]**. Usługodawca może być odrębnym administratorem ograniczonych danych potrzebnych do bezpieczeństwa własnej infrastruktury i kontaktu, zgodnie z Polityką prywatności. Żądania dotyczące grafiku, profilu i pracy należy kierować najpierw do Klienta. Skarga przysługuje do Prezesa UODO.${privacyBases}`, { related: "DPA; polityka prywatności", production: "Tak", classification: "PUBLICZNY / DLA UŻYTKOWNIKÓW" });

add("02-privacy/mapa-danych-osobowych.md", "Mapa danych osobowych", "Inwentarz danych w kodzie", "IOD / techniczny właściciel", "IOD", `
| Moduł/encja | Pola | Osoba | Cel | Rola | Dostęp | Odbiorca | Retencja | Eksport | Usunięcie | Ryzyko |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Employee | imię, nazwisko, telefon, e-mail, ID | pracownik | profil i grafik | procesor | menedżer; własny profil częściowo | hosting/SMTP | [DO WERYFIKACJI RODO] | XLSX pośrednio | CRUD z ograniczeniami | kontakt i historia |
| EmployeeAccount | username, hash, czasy logowania/seen/reset, sessionVersion | pracownik | auth | procesor / bezpieczeństwo do analizy | system, pracownik | hosting/SMTP | [DO WERYFIKACJI RODO] | brak pełnego | przez zależność profilu | auth wysokie |
| ManagerAccount | username, nazwa, recovery e-mail, hash, czasy | menedżer | auth/admin | procesor / do analizy | menedżerowie | hosting/SMTP | [DO WERYFIKACJI RODO] | brak | manager CRUD | szerokie uprawnienia |
| Schedule/Slot | nazwa, miesiąc, godziny, pracownik, notatka | pracownik | grafik | procesor | role wg publikacji | hosting; XLSX/SQL odbiorca | wg Klienta | XLSX/SQL | CRUD | wolny tekst |
| Availability | dzień, rodzaj, przedział, modyfikacja | pracownik | dostępność | procesor | pracownik/menedżer | hosting | wg Klienta | pośrednio | CRUD grupy | może ujawnić wrażliwe przyczyny w nazwach |
| ShiftSwap | strony, czas, status, snapshot, nazwy historyczne | pracownik | zamiany i dowód | procesor | role | hosting | [DO WERYFIKACJI RODO] | brak pełnego | część historii trwała | duplikacja danych |
| Communication | tytuł, treść, autor, odczyt | Użytkownicy | ogłoszenia | procesor | role | hosting | do ustalenia | nie | manager CRUD | wolny tekst |
| WorkflowLog | aktor, rola, ID, działanie, czas | Użytkownik | audyt operacyjny | do analizy | menedżer | hosting | ustawialna, bez finalnej polityki | nie | manager delete | nadmiar treści |
| UI state/localStorage | token, username, role pośrednio, ID powiadomień, preferencje | Użytkownik | sesja i UX | do analizy | przeglądarka | urządzenie; YouTube przy mediach | różna | nie | logout/clear | XSS i urządzenie wspólne |
| ManagerNote | tytuł, treść, właściciel | menedżer/inne osoby w treści | prywatne notatki | procesor | właściciel | hosting | [DO WERYFIKACJI RODO] | nie | CRUD | dane nadmiarowe |
| Support/billing | kontakt, treść, dokument | kontakty B2B | wsparcie/rozliczenie | administrator | upoważnione osoby | e-mail/księgowość | ustawowa/roszczenia | na żądanie | procedura | poza bazą aplikacji |`, { related: "analiza ról; DPA B", production: "Tak", classification: "WEWNĘTRZNY / RODO" });

add("02-privacy/okresy-retencji-danych.md", "Proponowane okresy retencji", "Harmonogram retencji", "IOD / Klient", "IOD", `
| Kategoria | Proponowana aktywna retencja | Kopie | Wyzwalacz | Powód | Usunięcie | Weryfikacja |
| --- | ---: | ---: | --- | --- | --- | --- |
| Aktywne Konta | czas upoważnienia | rotacja kopii | odebranie dostępu | dostęp | dezaktywacja/usunięcie | RODO/techniczna |
| Konta nieaktywne | [DO WERYFIKACJI RODO: OKRES] | rotacja | dezaktywacja | roszczenia/audyt | anonimizacja/usunięcie | prawna |
| Grafiki, dostępność, zamiany | [DO WERYFIKACJI RODO: WYMÓG KLIENTA] | rotacja | koniec okresu/Umowy | organizacja i prawo pracy po stronie Klienta | CRUD/procedura | Klient/prawnik |
| Komunikaty i powiadomienia | termin widoczności plus [DO WERYFIKACJI RODO] | rotacja | deadline | komunikacja | CRUD/cleanup | techniczna |
| Logi operacyjne/bezpieczeństwa | [DO WERYFIKACJI RODO: OKRES LOGÓW] | rotacja | zapis | bezpieczeństwo/roszczenia | ustawienia i delete | RODO/techniczna |
| Reset Hasła | kod 15 minut; metadane [DO WERYFIKACJI RODO] | rotacja | użycie/wygaśnięcie | bezpieczeństwo | czyszczenie pól | techniczna |
| Wsparcie | [DO WERYFIKACJI RODO: OKRES] | wg poczty | zamknięcie | obsługa/roszczenia | skrzynka/rejestr | prawna |
| Umowy i rozliczenia | [DO WERYFIKACJI PRAWNEJ I PODATKOWEJ: OKRES] | archiwum | zakończenie/rok | obowiązki i roszczenia | kontrolowane | księgowa |
| Akceptacje dokumentów | czas używania plus [DO WERYFIKACJI PRAWNEJ] | docelowo | akceptacja | dowód | rejestr | brak implementacji |
| Kopie | 10 najnowszych, okres czasowy zmienny | ten sam zbiór | kolejna kopia | odporność | rotacja | test produkcji |
| Incydenty | [DO WERYFIKACJI RODO: OKRES] | wg archiwum | zamknięcie | wykazanie i lessons learned | kontrolowane | IOD |
| Eksporty | do potwierdzenia odbioru + krótki bufor | nie ustalono | dostawa | wyjście danych | bezpieczne usunięcie | techniczna |
| Nieudane logowania | brak potwierdzonego osobnego rejestru | nie dotyczy | próba | bezpieczeństwo | wdrożyć jeśli potrzebne | techniczna |
${privacyBases}`, { related: "DPA § 14; pakiet 09", production: "Tak", classification: "WEWNĘTRZNY / RODO" });

add("02-privacy/rejestr-kategorii-czynnosci-przetwarzania.md", "Rejestr kategorii czynności przetwarzania – procesor", "Projekt art. 30 ust. 2 RODO", "Podmiot przetwarzający / IOD", "IOD", `
## Dane procesora
**[DO WYPEŁNIENIA: DANE PODMIOTU PRZETWARZAJĄCEGO I KONTAKT RODO]**

| Kategoria Administratorów | Kategorie przetwarzania | Osoby/dane | Transfer | Podprzetwarzający | Środki ogólne |
| --- | --- | --- | --- | --- | --- |
| Polskie firmy korzystające z GF | profile, auth, grafiki, dostępność, zamiany, komunikaty, logi, kopie, eksport/usunięcie | zob. DPA B | [DO WERYFIKACJI RODO] | zob. DPA D | role, hashowanie, HTTPS wariant, kopie, logi; luki w DPA C |

Konieczność, kompletność i poziom szczegółowości rejestru wymagają oceny IOD. Właściciel: **[DO WYPEŁNIENIA: WŁAŚCICIEL]**; przegląd: **[DO WYPEŁNIENIA: DATA]**.${privacyBases}`, { related: "DPA A–D", production: "Tak", classification: "WEWNĘTRZNY / RODO" });

add("02-privacy/README.md", "README – prywatność", "Instrukcja dokumentów prywatności", "Właściciel pakietu", "IOD", `
Polityka prywatności i obie klauzule są dokumentami do przekazania lub publikacji. Analiza ról, mapa, retencja i rejestr są wewnętrzne. Klient musi uzupełnić własną klauzulę pracowniczą; polityka Usługodawcy nie zastępuje informacji Klienta. HTML jest generowany z polityki Markdown.`, { related: "DPA; Regulamin; cookies", production: "Tak" });

const cookieBases = bases([["UŚUDE – obowiązki informacyjne", USUDE], ["RODO", GDPR], ["Polityka prywatności YouTube/Google – do weryfikacji", "https://policies.google.com/privacy?hl=pl"]]);

add("03-cookies-and-storage/audyt-cookies-i-storage.md", "Audyt cookies i pamięci przeglądarki", "Audyt kodu storage i stron trzecich", "Techniczny / IOD", "Właściciel techniczny", `
## Wynik

Nie znaleziono kodu ustawiającego własne HTTP cookies, sessionStorage, IndexedDB, service worker, analitykę, piksele, telemetrykę, chat ani menedżer zgód. Uwierzytelnianie używa JWT w localStorage. Interfejs zapisuje również nazwę Użytkownika, tryb logowania, odczytane powiadomienia, kolejność kolumn i przypięte rekordy.

Moduł aktualności może automatycznie osadzać iframe z youtube-nocookie.com i ładować dowolny zewnętrzny imageUrl. To powoduje połączenie przeglądarki z osobą trzecią; faktyczne cookies/storage i transfer zależą od dostawcy i wymagają testu sieciowego.

## Braki

- Brak publicznej polityki i mechanizmu zgody/aktywacji mediów.
- Brak automatycznego inwentarza storage i retencji tokenu.
- JWT w localStorage zwiększa skutki podatności XSS.
- Cookies proxy/load balancera i produkcji nie zostały zweryfikowane.

**[DO WERYFIKACJI TECHNICZNEJ: AUDYT PRODUKCYJNY DEVTOOLS I NAGŁÓWKÓW SET-COOKIE]**`, { related: "wykaz storage; polityka cookies", demo: "Tak", production: "Tak", classification: "WEWNĘTRZNY / TECHNICZNY" });

add("03-cookies-and-storage/wykaz-cookies-i-storage.md", "Wykaz cookies i storage", "Inwentarz technologii przeglądarki", "Użytkownicy / IOD", "Techniczny właściciel", `
| Nazwa/klucz | Typ | Dostawca | Cel | Niezbędne | Czas | Strona | Dane | Rekomendacja | Dowód |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| gf3.auth.access-token | localStorage | GF | sesja JWT | Tak dla obecnego modelu | do logout/wygaśnięcia/wyczyszczenia | first | token | informacja; ocenić bezpieczniejszy model | AuthProvider |
| gf3.auth.last-username | localStorage | GF | wygoda logowania | Nieściśle | do wyczyszczenia | first | username | opcja i informacja | LoginPage |
| gf3.auth.password-mode | localStorage | GF | PC/Phone | Nieściśle | do wyczyszczenia | first | preferencja | informacja | LoginPage |
| gf3.employee-notifications.read.* | localStorage | GF | odczyty powiadomień | Funkcjonalne | część 7 dni, część trwała; maks. 300 | first | ID, czas, employee ID/username w kluczu | minimalizacja i informacja | helper notifications |
| gf3:employee-schedule-column-order:* | localStorage | GF | kolejność kolumn | Funkcjonalne | do wyczyszczenia | first | employee ID/username, schedule ID | informacja | schedule page |
| *:list:pinned | localStorage | GF | przypięte rekordy | Nie | do wyczyszczenia | first | ID rekordów | informacja | list cards |
| employee UI state | baza serwera | GF | synchronizacja odczytów/pinów | Funkcjonalne | [DO WERYFIKACJI RODO] | server | employee ID, IDs, czas | retencja | EmployeeUiState |
| youtube-nocookie.com | iframe / możliwe cookies/storage | Google/YouTube | wideo aktualności | Nie | wg dostawcy | third | IP, nagłówki, interakcje – do testu | uprzednia zgoda lub click-to-load | system news |
| zewnętrzny imageUrl | żądanie HTTP / cache | wskazany host | obraz aktualności | Nie | wg przeglądarki/hosta | third | IP, nagłówki | proxy/allowlist/zgoda | system news |
| cookies produkcyjne | HTTP cookie | [DO WERYFIKACJI TECHNICZNEJ] | [DO WERYFIKACJI TECHNICZNEJ] | nieustalone | nieustalone | nieustalone | nieustalone | audyt DevTools | środowisko niebadane |`, { related: "audyt; polityka cookies", demo: "Tak", production: "Tak", classification: "PUBLICZNY PO ZATWIERDZENIU" });

add("03-cookies-and-storage/polityka-cookies-pl.md", "POLITYKA COOKIES I PAMIĘCI PRZEGLĄDARKI GF", "Publiczna polityka technologii", "Użytkownicy", "Usługodawca / IOD", `
## 1. Zakres

Polityka opisuje cookies, localStorage i zewnętrzne zasoby Platformy. Cookies są danymi zapisywanymi przez serwer/przeglądarkę; localStorage jest odrębną pamięcią i nie jest cookie.

## 2. Technologie GF

Obecny kod nie ustawia własnych HTTP cookies. W localStorage zapisuje token sesji, nazwę Użytkownika i tryb logowania, identyfikatory odczytanych powiadomień, układ kolumn i przypięte rekordy. Pełna tabela: **[DO WYPEŁNIENIA: LINK DO WYKAZU]**.

## 3. Zewnętrzne treści

Aktualności mogą zawierać obraz z zewnętrznego hosta lub wideo YouTube w domenie youtube-nocookie.com. Załadowanie może ujawnić dostawcy IP i nagłówki oraz uruchomić jego technologie. Do czasu wdrożenia decyzji zgody treści powinny być wyłączone albo ładowane dopiero po świadomym wyborze.

## 4. Analityka i marketing

Nie potwierdzono analityki, reklamy ani pikseli. Nie wolno ich uruchamiać bez aktualizacji polityki i odpowiedniej podstawy/zgody.

## 5. Zarządzanie

Użytkownik może usuwać dane strony w ustawieniach przeglądarki. Usunięcie tokenu wyloguje, a blokada localStorage może uniemożliwić obecne uwierzytelnienie lub zapamiętywanie ustawień. Opcjonalne media powinny mieć równoważne „Akceptuję” i „Odrzucam” oraz możliwość zmiany decyzji.

## 6. Kontakt i wersja

Kontakt: **[DO WYPEŁNIENIA: E-MAIL RODO]**. Polityka prywatności: **[DO WYPEŁNIENIA: LINK]**. Produkcyjny wykaz wymaga **[DO WERYFIKACJI TECHNICZNEJ: TESTU PRZEGLĄDARKOWEGO]**.
${cookieBases}`, { related: "polityka prywatności; wykaz storage", demo: "Tak", production: "Tak", classification: "PUBLICZNY" });

add("03-cookies-and-storage/rekomendacja-mechanizmu-zgody.md", "Rekomendacja mechanizmu zgody", "Decyzja cookies/storage", "Właściciel SaaS / IOD", "IOD / techniczny", `
## Rekomendacja

Wybrać **Wariant B** dla zewnętrznych mediów: nie ładować iframe YouTube ani zewnętrznego obrazu przed wyborem; pokazać neutralny placeholder „Załaduj treść zewnętrzną”, równorzędne opcje akceptacji/odrzucenia, kategorię „Media zewnętrzne”, możliwość wycofania i zapis wersji zgody. Nie oznaczać zgody z góry.

Wariant A – sama informacja bez bannera – jest właściwy tylko wtedy, gdy zewnętrzne media zostaną wyłączone/proxyowane bez strony trzeciej, a audyt produkcyjny potwierdzi wyłącznie technologie ściśle niezbędne. localStorage auth nie wymaga dekoracyjnej zgody, ale wymaga informacji i oceny bezpieczeństwa.

Kryteria: brak żądania do domeny zewnętrznej przed wyborem; odmowa nie blokuje podstawowej Platformy; zmiana decyzji jest dostępna; wersja i czas wyboru są zapisane; polityka zawiera aktualną listę. **[DO WERYFIKACJI PRAWNEJ: PODSTAWA I MODEL ZGODY]**`, { related: "audyt i polityka cookies", production: "Tak", classification: "WEWNĘTRZNY / DECYZYJNY" });

add("03-cookies-and-storage/README.md", "README – cookies i storage", "Instrukcja audytu", "Właściciel pakietu", "IOD / techniczny", `
Audyt kodu wskazuje localStorage i opcjonalne treści zewnętrzne, nie własne cookies ani analitykę. Przed publikacją wykonać test produkcyjny DevTools. Rekomendowany jest click-to-load/zgoda dla mediów albo ich wyłączenie. HTML polityki powstaje z Markdown.`, { related: "audyt; wykaz; polityka", demo: "Tak", production: "Tak" });

// 04 — bezpieczeństwo i incydenty
add("04-security-and-incidents/polityka-bezpieczenstwa-danych.md", "Polityka bezpieczeństwa danych GF", "Polityka wewnętrzna", "Właściciel SaaS", "IOD / techniczny", `
## 1. Cel i zakres

Polityka obejmuje dane Klienta, konta managerów i pracowników, harmonogramy, dostępności, zamiany, komunikaty, notatki i logi operacyjne. Jest projektem kontroli organizacyjnych; nie stanowi deklaracji wdrożenia środka bez dowodu.

## 2. Zasady

Minimalizacja danych i uprawnień, rozliczalność, poufność, integralność, dostępność oraz privacy by design. Nie wpisywać do pól swobodnych danych szczególnych kategorii ani danych o wyrokach. Dostęp nadaje się imiennie, odbiera bezzwłocznie po zmianie roli i przegląda **[DO WYPEŁNIENIA: CZĘSTOTLIWOŚĆ]**.

## 3. Kontrole bazowe

| Obszar | Stan potwierdzony w kodzie | Wymagane przed produkcją |
|---|---|---|
| Hasła | sześć cyfr, hash PBKDF2-SHA256; sesje JWT | **[DO WERYFIKACJI TECHNICZNEJ: WZMOCNIENIE POLITYKI, RATE LIMITING I MFA DLA MANAGERA]** |
| Transport | konfiguracja nginx i opcjonalny overlay HTTPS | **[DO WERYFIKACJI TECHNICZNEJ: TLS W PRODUKCJI]** |
| Sesja | token w localStorage; wersjonowanie sesji | **[DO WERYFIKACJI TECHNICZNEJ: OCENA XSS, TTL, ROTACJA I BEZPIECZNE PRZECHOWYWANIE]** |
| Uprawnienia | role manager/pracownik i kontrole endpointów | test macierzy uprawnień i okresowy przegląd |
| Backup | automatyczna kopia SQLite co ok. godzinę podczas pracy aplikacji, 10 kopii | kopia poza wolumenem, szyfrowanie, alerty i test odtworzenia |
| Logi | operacyjne zdarzenia aktora; brak potwierdzonego IP/UA | retencja, dostęp i monitoring bez nadmiernego logowania |
| Sekrety | konfiguracja środowiskowa; skan repo bez potwierdzonego sekretu | sejf sekretów, rotacja i skan CI |

## 4. Eksploatacja

Aktualizacje zależności i obrazów po ocenie ryzyka; zmiany przez przegląd i kontrolowane wdrożenie; logi i backup dostępne tylko upoważnionym. Urządzenia z dostępem produkcyjnym wymagają aktualizacji, blokady ekranu, szyfrowania, ochrony przed malware i możliwości odebrania dostępu. Dostępu produkcyjnego nie współdzielić; działania wysokiego ryzyka zatwierdza druga osoba **[DO WYPEŁNIENIA: TRYB]**.

Podwykonawca otrzymuje czasowy, minimalny dostęp dopiero po poufności i weryfikacji. Eksport jest autoryzowany, szyfrowany i usuwany po odbiorze; usunięcie podlega procedurze exit i nie obiecuje natychmiastowego czyszczenia każdej kopii. Incydenty obsługiwać według procedury. Dostawców i transfery prowadzić w rejestrze. Co najmniej raz w roku oraz po istotnej zmianie wykonać przegląd ryzyka.

## 5. Odpowiedzialność i dowody

Właściciel zatwierdza ryzyko, techniczny wdraża kontrole, IOD/prawnik kwalifikuje RODO, Klient administruje swoimi Użytkownikami. Dowody: wyniki testów, rejestry dostępu/incydentów/backupu, wersje procedur i protokoły przeglądu.
${gdprBases}`, { related: "DPA C; incydenty; backup", production: "Tak", classification: "POUFNY WEWNĘTRZNY" });

add("04-security-and-incidents/procedura-reagowania-na-incydenty.md", "Procedura reagowania na incydenty", "Procedura operacyjna", "Właściciel SaaS", "IOD / techniczny", `
## 1. Zgłoszenie i triage

Każdy sygnał otrzymuje identyfikator, czas wykrycia, zgłaszającego i właściciela. Nie przesyłać haseł, tokenów ani pełnych danych osobowych. Zachować dowody w sposób kontrolowany; nie wykonywać destrukcyjnych działań bez kopii i decyzji.

| Klasa | Przykład | Reakcja docelowa |
|---|---|---|
| SEV1 krytyczny | aktywne ujawnienie danych, przejęcie uprzywilejowanego konta, całkowita utrata danych | natychmiastowa eskalacja **[DO WYPEŁNIENIA: CZAS]** |
| SEV2 wysoki | istotna niedostępność, podejrzenie naruszenia ograniczonego zakresu | **[DO WYPEŁNIENIA: CZAS]** |
| SEV3 średni | błąd z obejściem, ograniczony wpływ | w godzinach wsparcia |
| SEV4 niski | pytanie, kosmetyka | kolejka planowa |

## 2. Cykl

1. Zarejestruj i potwierdź odbiór.
2. Ogranicz skutki (np. unieważnij sesję, izoluj komponent) z zapisem decyzji.
3. Ustal zakres, oś czasu, dane i osoby dotknięte; odróżnij incydent bezpieczeństwa od naruszenia danych.
4. Powiadom IOD/prawnika i Klienta zgodnie z rolą procesora.
5. Usuń przyczynę, bezpiecznie przywróć i monitoruj.
6. Udokumentuj komunikację, dowody i decyzję o zamknięciu.
7. W ciągu **[DO WYPEŁNIENIA: TERMIN POST-MORTEM]** przeprowadź analizę przyczyn i plan działań.

## 3. Szablon przeglądu po incydencie

Oś czasu; wykrycie i co zadziałało/nie zadziałało; przyczyna bez obwiniania osób; wpływ; decyzje i komunikacja; zabezpieczenia doraźne; działania trwałe z właścicielem/terminem; dowód testu; ryzyko rezydualne i zatwierdzający: **[DO WYPEŁNIENIA]**.

## 4. Komunikacja

Kanał awaryjny: **[DO WYPEŁNIENIA: KANAŁ]**. Rzecznik: **[DO WYPEŁNIENIA: OSOBA]**. Komunikat do Klienta zawiera fakty, zakres, skutki, podjęte środki, zalecenia i kolejny termin aktualizacji; nie zawiera spekulacji. Publiczna komunikacja wymaga decyzji właściciela i konsultacji prawnej.
${gdprBases}`, { related: "procedura naruszenia; formularz; rejestr", production: "Tak", classification: "POUFNY WEWNĘTRZNY" });

add("04-security-and-incidents/procedura-naruszenia-ochrony-danych.md", "Procedura naruszenia ochrony danych osobowych", "Procedura RODO", "Właściciel SaaS / IOD", "IOD / prawnik", `
## 1. Rozróżnienie ról

Jako procesor GF zawiadamia administratora (Klienta) bez zbędnej zwłoki po stwierdzeniu naruszenia i współpracuje. To administrator co do zasady ocenia ryzyko oraz obowiązek zgłoszenia organowi w 72 godziny i zawiadomienia osób. Dla danych własnych GF działa jako administrator. Terminu 72 godzin nie należy przedstawiać jako terminu procesora do UODO bez analizy roli.

## 2. Przepływ

\`\`\`mermaid
flowchart TD
  A["Sygnał lub zdarzenie"] --> B["Zabezpieczenie dowodów i ograniczenie skutków"]
  B --> C{"Czy naruszono poufność, integralność lub dostępność danych osobowych?"}
  C -- "Nie / niepewne" --> D["Rejestr incydentu i dalsza analiza"]
  C -- "Tak" --> E{"Rola GF dla danych"}
  E -- "Procesor" --> F["Bez zbędnej zwłoki zawiadom Klienta i przekaż informacje etapami"]
  E -- "Administrator" --> G["Oceń ryzyko praw i wolności"]
  F --> H["Współpraca z Klientem, zachowanie osi czasu"]
  G --> I{"Ryzyko wymagające zgłoszenia?"}
  I -- "Tak" --> J["Zgłoszenie UODO do 72 h od stwierdzenia; przy wysokim ryzyku ocena zawiadomienia osób"]
  I -- "Nie" --> K["Udokumentuj przesłanki braku zgłoszenia"]
  H --> L["Usunięcie przyczyny i post-mortem"]
  J --> L
  K --> L
\`\`\`

## 3. Minimalny materiał

Charakter naruszenia, kategorie i przybliżona liczba osób/rekordów, czas wykrycia i stwierdzenia, systemy, prawdopodobne konsekwencje, podjęte/planowane środki, punkt kontaktowy oraz braki uzupełniane etapami. Nie opóźniać pierwszego zawiadomienia Klienta z powodu niepełnych danych.

## 4. Szablon zawiadomienia Klienta

**Temat:** PILNE — możliwe naruszenie danych / **[DO WYPEŁNIENIA: ID]**. Stwierdzono: **[DO WYPEŁNIENIA]**. Zakres: **[DO WYPEŁNIENIA]**. Skutki: **[DO WYPEŁNIENIA]**. Środki: **[DO WYPEŁNIENIA]**. Następna aktualizacja: **[DO WYPEŁNIENIA]**. Kontakt: **[DO WYPEŁNIENIA]**.

## 5. Dokumentacja i zamknięcie

Odnotować moment „stwierdzenia”, decyzje, podstawę oceny ryzyka, powiadomienia i uzasadnienie. Zamknięcie wymaga właściciela, działań korygujących i daty weryfikacji skuteczności.
${gdprBases}`, { related: "DPA; checklista 72h; rejestr", production: "Tak", classification: "POUFNY / DANE O INCYDENCIE" });

add("04-security-and-incidents/formularz-zgloszenia-incydentu.md", "Formularz zgłoszenia incydentu", "Formularz operacyjny", "Zgłaszający / koordynator", "IOD / techniczny", `
## Dane zgłoszenia

- ID: **[DO WYPEŁNIENIA]**; data/czas/strefa: **[DO WYPEŁNIENIA]**; kanał: **[DO WYPEŁNIENIA]**.
- Zgłaszający i bezpieczny kontakt: **[DO WYPEŁNIENIA]**.
- System/funkcja i środowisko: **[DO WYPEŁNIENIA]**.
- Obserwacja (bez sekretów i zbędnych danych): **[DO WYPEŁNIENIA]**.
- Pierwszy zauważony czas i czy zdarzenie trwa: **[DO WYPEŁNIENIA]**.
- Możliwy wpływ na poufność / integralność / dostępność: **[DO WYPEŁNIENIA]**.
- Potencjalne dane/osoby/Klienci: **[DO WYPEŁNIENIA]**.
- Podjęte działania i lokalizacja dowodów: **[DO WYPEŁNIENIA]**.
- Wstępny SEV, właściciel, eskalacja IOD: **[DO WYPEŁNIENIA]**.
- Powiadomienia, decyzja o naruszeniu, zamknięcie i działania następcze: **[DO WYPEŁNIENIA]**.`, { related: "rejestr incydentów; procedury", production: "Tak", classification: "POUFNY" });

addRaw("04-security-and-incidents/rejestr-incydentow-template.csv", "id;data_wykrycia;data_stwierdzenia;zglaszajacy;system;opis_bez_danych_wrazliwych;sev;czy_dane_osobowe;rola_gf;klient_powiadomiony_data;uodo_decyzja;osoby_decyzja;wlasciciel;status;dzialania;data_zamkniecia;link_do_dowodow\n");

add("04-security-and-incidents/checklista-72-godziny.md", "Checklista 72 godziny", "Checklista naruszenia", "Koordynator incydentu / IOD", "IOD / prawnik", `
## Natychmiast

- [ ] Odnotuj wykrycie i moment stwierdzenia, strefę czasową, rolę GF i właściciela.
- [ ] Ogranicz skutki, zabezpiecz dowody, nie niszcz logów.
- [ ] Ustal, czy obejmuje dane osobowe i którego Klienta.
- [ ] Jeśli GF jest procesorem, zawiadom Klienta bez zbędnej zwłoki kanałem **[DO WYPEŁNIENIA]**.

## Do decyzji administratora

- [ ] Kategorie/liczby osób i rekordów; skutki; środki; kontakt.
- [ ] Udokumentowana ocena ryzyka i decyzja UODO (administrator).
- [ ] Jeżeli zgłoszenie wymagane: formularz i wysyłka nie później niż 72 h od stwierdzenia; opóźnienie uzasadnione.
- [ ] Ocena wysokiego ryzyka i zawiadomienia osób.
- [ ] Informacje brakujące przekazywane etapami.

## Po opanowaniu

- [ ] Przyczyna, działania korygujące, walidacja, komunikat końcowy i retencja dowodów.
${gdprBases}`, { related: "procedura naruszenia", production: "Tak", classification: "POUFNY" });

add("04-security-and-incidents/analiza-ryzyka-template.md", "Analiza ryzyka incydentu — szablon", "Szablon oceny", "IOD / właściciel ryzyka", "Prawnik / techniczny", `
## Kontekst

ID, rola GF, aktywa, proces, osoby i kategorie danych: **[DO WYPEŁNIENIA]**.

## Ocena

| Scenariusz szkody | Źródło/zagrożenie | Istniejące zabezpieczenia | Prawdopodobieństwo 1–5 | Skutek 1–5 | Wynik | Ryzyko dla praw i wolności |
|---|---|---|---:|---:|---:|---|
| **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |

Uwzględnić możliwość identyfikacji, skalę, wrażliwość kontekstu pracy, dzieci/osoby podatne, odwracalność, poufność, integralność i dostępność. Progi i apetyt na ryzyko: **[DO WYPEŁNIENIA]**. Decyzja, środki, właściciel, termin, ryzyko rezydualne i akceptujący: **[DO WYPEŁNIENIA]**.
${gdprBases}`, { related: "polityka bezpieczeństwa; incydenty", production: "Tak", classification: "POUFNY" });

add("04-security-and-incidents/README.md", "README — bezpieczeństwo i incydenty", "Instrukcja użycia", "Właściciel pakietu", "IOD / techniczny", `
Przed produkcją uzupełnić kontakty i czasy, przeprowadzić ćwiczenie tabletop, zamknąć krytyczne luki uwierzytelnienia/TLS/backupu oraz uzgodnić kanał naruszeń z Klientem. Rejestr nie powinien zawierać sekretów ani nadmiarowych danych.`, { related: "wszystkie pliki 04", production: "Tak" });

// 05 — backup i ciągłość
add("05-backup-and-continuity/procedura-kopii-zapasowych.md", "Procedura kopii zapasowych", "Procedura operacyjna", "Techniczny właściciel usługi", "Właściciel SaaS / IOD", `
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
${gdprBases}`, { related: "odtwarzanie; BCP; DPA C", production: "Tak", classification: "POUFNY WEWNĘTRZNY" });

add("05-backup-and-continuity/procedura-odtwarzania-danych.md", "Procedura odtwarzania danych", "Runbook wysokiego poziomu", "Techniczny właściciel usługi", "Właściciel SaaS / IOD", `
## Warunki wejścia

Zgłoszenie/zmiana zatwierdzona przez **[DO WYPEŁNIENIA: ROLA]**, określony zakres i punkt w czasie, ocena incydentu, komunikacja z Klientem oraz bezpieczna kopia obecnego stanu. Nie wykonywać przywracania na produkcji bez jawnego zatwierdzenia.

## Przebieg

1. Zatrzymaj zapisy lub odizoluj środowisko zgodnie z zatwierdzonym planem.
2. Wybierz kopię z katalogu na podstawie czasu, integralności i retencji; udokumentuj identyfikator.
3. Odtwórz najpierw w odseparowanym środowisku **[DO WYPEŁNIENIA]**.
4. Zweryfikuj otwarcie bazy, integralność, wersję schematu, kluczowe liczności i reprezentatywne operacje bez ujawniania danych.
5. Zatwierdź przełączenie, wykonaj kontrolowane przywrócenie i test smoke.
6. Monitoruj, przekaż wynik Klientowi, odnotuj RPO/RTO rzeczywiste i zamknij zmianę.

## Cofnięcie i awaria

Jeśli walidacja nie przejdzie, nie promuj kopii; zachowaj stan, wróć do ostatniego bezpiecznego punktu i eskaluj. Konkretne polecenia i ścieżki serwera pozostają w chronionym runbooku technicznym, nie w pakiecie klienta.`, { related: "procedura backup; test; BCP", production: "Tak", classification: "POUFNY WEWNĘTRZNY" });

add("05-backup-and-continuity/checklista-testu-odtworzeniowego.md", "Checklista testu odtworzeniowego", "Checklista kontrolna", "Techniczny wykonawca", "Właściciel SaaS", `
- [ ] Zakres, cel, kopia i środowisko testowe zatwierdzone; produkcja chroniona.
- [ ] Kopia ma czas, identyfikator, sumę/integralność i znaną retencję.
- [ ] Dostępy czasowe nadane minimalnie; dane testowe zabezpieczone.
- [ ] Start/koniec i wszystkie błędy zapisane.
- [ ] Baza otwiera się, schemat zgodny, kluczowe rekordy i relacje spójne.
- [ ] Logowanie testowe, odczyt harmonogramu, zapis kontrolny i eksport sprawdzone bez realnego wysyłania komunikatów.
- [ ] Osiągnięte RPO/RTO porównane z celem.
- [ ] Dane testowe po zakończeniu usunięte zgodnie z decyzją; wynik, dowody i działania zaakceptowane.`, { related: "rejestr testów; odtwarzanie", production: "Tak", classification: "POUFNY" });

addRaw("05-backup-and-continuity/rejestr-testow-backup-template.csv", "id_testu;data;wykonawca;identyfikator_kopii;czas_kopii;srodowisko;zakres;wynik_integralnosci;czas_odtworzenia_min;rpo_osiagniete;rto_osiagniete;wynik_testow;odchylenia;dzialania;wlasciciel;termin;zatwierdzil\n");

add("05-backup-and-continuity/plan-ciaglosci-dzialania-light.md", "Plan ciągłości działania — light", "Plan operacyjny", "Właściciel SaaS", "Techniczny / IOD", `
## Usługi krytyczne

Logowanie, odczyt/opublikowanie harmonogramu, dostępność, zamiany i komunikacja. Zależności: hosting **[DO WERYFIKACJI TECHNICZNEJ]**, DNS/TLS **[DO WERYFIKACJI TECHNICZNEJ]**, baza/backup, dostawca e-mail **[DO WERYFIKACJI TECHNICZNEJ]**.

## Cele i scenariusze

Docelowe RTO **[DO WYPEŁNIENIA]**, RPO **[DO WYPEŁNIENIA]**, godziny obsady **[DO WYPEŁNIENIA]**. Scenariusze: awaria aplikacji/hosta, uszkodzenie bazy, utrata DNS/TLS, niedostępność poczty, przejęcie konta, brak osoby kluczowej. Dla każdego: właściciel, detekcja, obejście, komunikacja, kryterium odtworzenia i powrotu.

## Tryb awaryjny

Klient utrzymuje zatwierdzony alternatywny kanał publikacji grafików **[DO WYPEŁNIENIA]**. GF komunikuje status przez **[DO WYPEŁNIENIA]** z częstotliwością zależną od SEV. Nie wysyłać danych harmonogramu niezatwierdzonym kanałem.

## Ćwiczenia

Tabletop i test backupu przed startem oraz co **[DO WYPEŁNIENIA]**; po zdarzeniu aktualizacja planu, kontaktów i ryzyka.`, { related: "incydenty; backup; wsparcie", production: "Tak", classification: "POUFNY WEWNĘTRZNY" });

add("05-backup-and-continuity/README.md", "README — backup i ciągłość", "Instrukcja użycia", "Właściciel pakietu", "Techniczny / IOD", `
Stan kodu zapewnia lokalne kopie rotacyjne, lecz przed produkcją trzeba potwierdzić kopię odseparowaną, szyfrowanie, alerty, RPO/RTO i pozytywny test odtworzenia. Dokumenty nie zawierają poleceń ani prywatnych ścieżek serwera.`, { related: "wszystkie pliki 05", production: "Tak" });

// 06 — uruchomienie klienta
add("06-client-launch/protokol-uruchomienia-platformy.md", "Protokół uruchomienia Platformy GF", "Protokół dwustronny", "Usługodawca / Klient", "Prawnik / techniczny", `
## 1. Strony i podstawa

Usługodawca: **[DO WYPEŁNIENIA: DANE]**. Klient: **[DO WYPEŁNIENIA: DANE I KRS/NIP]**. Umowa SaaS nr **[DO WYPEŁNIENIA]**, DPA nr **[DO WYPEŁNIENIA]**, planowana data uruchomienia **[DO WYPEŁNIENIA]**.

## 2. Zakres

Uruchomienie obejmuje środowisko **[DO WYPEŁNIENIA]**, funkcje potwierdzone: konta managerów/pracowników, organizację pracowników, dostępności, harmonogramy i publikację, zamiany/korekty, powiadomienia/komunikację oraz eksporty dostępne w uzgodnionym zakresie. Nie obejmuje funkcji niestwierdzonych ani kodu źródłowego.

## 3. Kryteria gotowości

- [ ] podpisana Umowa i DPA z załącznikami; zaakceptowane regulamin, polityki i ryzyka;
- [ ] dane wdrożeniowe i konta przekazane bezpiecznym kanałem;
- [ ] TLS, uprawnienia, kopia odseparowana i test odtworzenia potwierdzone;
- [ ] model zewnętrznych mediów/cookies rozstrzygnięty;
- [ ] testy: logowanie ról, utworzenie/odczyt/publikacja grafiku, dostępność, komunikat, eksport, wylogowanie/unieważnienie sesji;
- [ ] kontakty wsparcia/incydentów i tryb awaryjny uzgodnione;
- [ ] otwarte odchylenia mają właściciela, termin i pisemną akceptację ryzyka.

## 4. Wynik

Wynik: **[DO WYPEŁNIENIA: PRZYJĘTY / WARUNKOWO / ODRZUCONY]**. Odchylenia: **[DO WYPEŁNIENIA]**. Start świadczenia i okresu rozliczeniowego: **[DO WYPEŁNIENIA]**. Cena referencyjna 500,00 zł miesięcznie; kwalifikacja VAT/netto/brutto i dokument sprzedaży zgodnie z Umową po konsultacji.

## 5. Podpisy

Klient — imię, funkcja, data, podpis: **[DO WYPEŁNIENIA]**. Usługodawca — imię, data, podpis: **[DO WYPEŁNIENIA]**. Podpis nie potwierdza zgodności poza zakresem wskazanych testów.`, { related: "Umowa SaaS; DPA; checklista; akceptacja", demo: "Tak", production: "Tak", classification: "POUFNY UMOWNY" });

add("06-client-launch/checklista-onboardingu-klienta.md", "Checklista onboardingu Klienta", "Checklista wdrożenia", "Koordynator wdrożenia", "Klient / techniczny", `
## Przed konfiguracją

- [ ] tożsamość i umocowanie Stron, kontakty, Umowa, DPA i załączniki uzgodnione;
- [ ] role RODO, retencja, podprocesorzy i kanał naruszeń ustalone;
- [ ] zakres placówek, managerów, pracowników i migracji ograniczony do minimum;
- [ ] Klient potwierdził podstawę przekazania danych i zakaz danych szczególnych w polach wolnych.

## Konfiguracja i odbiór

- [ ] produkcyjny hosting/region, TLS, SMTP, kopie, RPO/RTO i kontakty zweryfikowane;
- [ ] konta managerów imienne; pierwsze dane dostępowe przekazane oddzielnym bezpiecznym kanałem;
- [ ] uprawnienia, sesje, odzyskiwanie, harmonogram, publikacja, komunikacja i eksport przetestowane;
- [ ] polityki publiczne opublikowane pod zatwierdzonymi linkami;
- [ ] szkolenie i tryb awaryjny zakończone;
- [ ] protokół uruchomienia i odchylenia podpisane; dokument rozliczeniowy zaplanowany.`, { related: "protokół startu; formularze", production: "Tak" });

add("06-client-launch/checklista-onboardingu-uzytkownikow.md", "Checklista onboardingu Użytkowników", "Checklista dla Klienta", "Manager Klienta", "Koordynator wdrożenia", `
- [ ] Użytkownik potrzebuje dostępu i zna rolę; dane są prawidłowe i minimalne.
- [ ] Konto utworzył upoważniony manager; login przekazano oddzielnie od hasła.
- [ ] Użytkownik otrzymał regulamin, politykę prywatności, cookies/storage i instrukcję bezpieczeństwa w aktualnej wersji.
- [ ] Akceptacja/udostępnienie udokumentowane poza Platformą, ponieważ kod nie potwierdza rejestrowania akceptacji prawnej.
- [ ] Użytkownik zmienił/chroni kod dostępu, nie udostępnia go i zna odzyskiwanie.
- [ ] Sprawdzono tylko potrzebną placówkę/harmonogram i zakaz wpisywania danych wrażliwych do notatek.
- [ ] Użytkownik zna wsparcie, incydenty, wylogowanie i alternatywny kanał grafiku.
- [ ] Przy odejściu konto/sesje są niezwłocznie odebrane, a potrzebne dane zachowane zgodnie z retencją.`, { related: "akceptacja dokumentów; dostęp", production: "Tak" });

add("06-client-launch/formularz-danych-wdrozeniowych.md", "Formularz danych wdrożeniowych", "Formularz Klienta", "Klient", "Koordynator wdrożenia", `
## Organizacja

Pełna firma, forma, KRS/NIP, adres, reprezentant: **[DO WYPEŁNIENIA]**. Zakres placówek/sklepów i nazewnictwo: **[DO WYPEŁNIENIA]**. Planowana data i strefa: **[DO WYPEŁNIENIA]**.

## Zakres danych

Liczba managerów/pracowników, sposób minimalnego importu, pola wymagane i właściciel jakości: **[DO WYPEŁNIENIA]**. Nie wpisywać tu haseł, tokenów, danych szczególnych, pełnej bazy ani realnych danych pracowników — przekazać zatwierdzonym bezpiecznym kanałem.

## Konfiguracja biznesowa

Zasady grafików, publikacji, dostępności, zamian, komunikatów, eksportów i retencji: **[DO WYPEŁNIENIA]**. Treści zewnętrzne: **[DO WYPEŁNIENIA: WYŁĄCZONE / CLICK-TO-LOAD]**. Alternatywny kanał ciągłości: **[DO WYPEŁNIENIA]**.

## Zatwierdzenie

Osoba przekazująca, podstawa/umocowanie, data i akceptacja zakresu: **[DO WYPEŁNIENIA]**.`, { related: "DPA A/B; protokół", production: "Tak", classification: "POUFNY" });

add("06-client-launch/formularz-osob-kontaktowych.md", "Formularz osób kontaktowych", "Formularz stron", "Klient / Usługodawca", "Koordynator", `
| Funkcja | Organizacja | Imię i nazwisko | Służbowy e-mail/telefon | Godziny/strefa | Zastępca | Upoważnienie |
|---|---|---|---|---|---|---|
| Umowa i rozliczenia | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Wdrożenie / manager | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Incydenty/RODO 24/7 | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Wsparcie techniczne | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |

Kontakty wykorzystywać tylko do wskazanych celów; aktualizację zgłaszać w **[DO WYPEŁNIENIA: TERMIN]**.`, { related: "wsparcie; incydenty; klauzula B2B", production: "Tak", classification: "POUFNY" });

add("06-client-launch/protokol-przekazania-dostepu-administratora.md", "Protokół przekazania dostępu managera", "Protokół bezpieczeństwa", "Usługodawca / Klient", "Techniczny", `
Kod potwierdza rolę „manager”, nie odrębną techniczną rolę Administratora. Konto: identyfikator bez hasła **[DO WYPEŁNIENIA]**, zakres **[DO WYPEŁNIENIA]**, odbiorca i umocowanie **[DO WYPEŁNIENIA]**. Login przekazano kanałem **[DO WYPEŁNIENIA]**, tajny kod oddzielnym kanałem **[DO WYPEŁNIENIA]**; żadnego sekretu nie wpisywać do protokołu.

- [ ] odbiorca zweryfikowany; konto imienne, minimalne uprawnienia;
- [ ] odbiorca potwierdził dostęp i odpowiedzialność za konta pracowników;
- [ ] odzyskiwanie i unieważnienie sesji sprawdzone;
- [ ] wymuszona zmiana/startowa rotacja **[DO WERYFIKACJI TECHNICZNEJ]**;
- [ ] data/czas przekazania, przekazujący, odbierający i podpisy: **[DO WYPEŁNIENIA]**.`, { related: "polityka bezpieczeństwa; protokół startu", production: "Tak", classification: "POUFNY" });

add("06-client-launch/potwierdzenie-akceptacji-dokumentow.md", "Potwierdzenie udostępnienia i akceptacji dokumentów", "Dowód organizacyjny", "Klient / Usługodawca", "Prawnik / IOD", `
Platforma nie ma potwierdzonego mechanizmu rejestrowania akceptacji wersji dokumentów. Niniejszy formularz jest dowodem poza systemem, nie zastępuje analizy, które dokumenty wymagają akceptacji, a które tylko udostępnienia.

| Dokument | Wersja | Data udostępnienia | Sposób/link | Akceptacja wymagana? | Osoba/umocowanie | Data/podpis |
|---|---|---|---|---|---|---|
| Umowa SaaS i Załącznik 1 | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | Tak | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| DPA A–E | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | Tak | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Regulamin / zagrożenia | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WERYFIKACJI PRAWNEJ]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| Prywatność / cookies-storage | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | udostępnienie | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |

Klient zobowiązuje się przekazać Użytkownikom właściwe wersje i zachować dowód zgodnie z uzgodnionym procesem.`, { related: "regulamin; polityki; umowy", production: "Tak", classification: "POUFNY UMOWNY" });

add("06-client-launch/README.md", "README — uruchomienie Klienta", "Instrukcja kolejności", "Koordynator wdrożenia", "Klient / prawnik / techniczny", `
Kolejność: uzupełnienie danych → podpis Umowy/DPA → konfiguracja bez sekretów w dokumentach → testy i szkolenie → potwierdzenie dokumentów → protokół startu. Status „warunkowy” wymaga jawnego ryzyka, terminu i właściciela. HTML protokołu powstaje z Markdown.`, { related: "wszystkie pliki 06", production: "Tak" });

// 07 — wsparcie
add("07-support/zasady-wsparcia-technicznego.md", "Zasady wsparcia technicznego", "Załącznik operacyjny", "Usługodawca", "Klient / prawnik", `
## 1. Zakres

Wsparcie obejmuje diagnozę działania funkcji objętych Umową, obsługę błędów, pytania eksploatacyjne i uzgodnione zmiany. Nie obejmuje urządzeń/sieci Klienta, usług stron trzecich poza kontrolą GF, szkoleń ponad zakres ani nowych funkcji bez wyceny. Abonament referencyjny: 500,00 zł miesięcznie; zakres płatnych zmian wymaga osobnej akceptacji.

## 2. Kanał i godziny

Kanał podstawowy **[DO WYPEŁNIENIA]**, awaryjny SEV1 **[DO WYPEŁNIENIA]**, godziny i dni **[DO WYPEŁNIENIA]**, strefa Europe/Warsaw. Istniejący projekt Umowy przewiduje pierwszą odpowiedź do 2 Dni Roboczych; krótsze czasy poniżej są celami dopiero po uzgodnieniu.

| Klasa | Wpływ | Pierwsza odpowiedź | Aktualizacje / obejście |
|---|---|---|---|
| P1/SEV1 | krytyczna niedostępność lub aktywny incydent | **[DO WYPEŁNIENIA: SLA]** | **[DO WYPEŁNIENIA]** |
| P2/SEV2 | istotna funkcja bez rozsądnego obejścia | **[DO WYPEŁNIENIA: SLA]** | **[DO WYPEŁNIENIA]** |
| P3/SEV3 | ograniczenie z obejściem | do 2 Dni Roboczych, o ile Umowa nie stanowi inaczej | uzgodnione |
| P4/SEV4 | pytanie/kosmetyka/wniosek | do 2 Dni Roboczych | backlog/wycena |

Pierwsza odpowiedź nie jest czasem naprawy. Bieg może zostać wstrzymany, gdy niezbędna odpowiedź Klienta jest niedostępna, z zapisem czasu i przyczyny.

## 3. Bezpieczeństwo i eskalacja

Nie przesyłać haseł, tokenów ani całych baz. Incydenty danych przechodzą do procedury naruszeń. Eskalacja biznesowa **[DO WYPEŁNIENIA]**, techniczna **[DO WYPEŁNIENIA]**, RODO 24/7 **[DO WYPEŁNIENIA]**. Zamknięcie po potwierdzeniu albo po **[DO WYPEŁNIENIA]** bez odpowiedzi, z możliwością ponownego otwarcia.`, { related: "Umowa SaaS; incydenty; klasyfikacja", production: "Tak", classification: "POUFNY UMOWNY" });

add("07-support/formularz-zgloszenia-bledu.md", "Formularz zgłoszenia błędu", "Formularz Klienta", "Zgłaszający", "Wsparcie", `
- Data/czas/strefa, kontakt, Klient: **[DO WYPEŁNIENIA]**.
- Środowisko, rola (bez danych osoby), funkcja: **[DO WYPEŁNIENIA]**.
- Oczekiwany i rzeczywisty rezultat: **[DO WYPEŁNIENIA]**.
- Minimalne kroki odtworzenia i częstotliwość: **[DO WYPEŁNIENIA]**.
- Wpływ/liczba osób, obejście, proponowany priorytet: **[DO WYPEŁNIENIA]**.
- Wersja/przeglądarka/urządzenie bez identyfikatorów wrażliwych: **[DO WYPEŁNIENIA]**.
- Zrzut/log po anonimizacji i bez sekretów: **[DO WYPEŁNIENIA: BEZPIECZNY LINK]**.
- Czy możliwe naruszenie danych/bezpieczeństwa: **[DO WYPEŁNIENIA]** — jeśli tak, użyć kanału incydentowego.`, { related: "zasady wsparcia; incydenty", production: "Tak", classification: "POUFNY" });

add("07-support/formularz-zgloszenia-zmiany.md", "Formularz zgłoszenia zmiany", "Formularz biznesowy", "Klient", "Właściciel produktu", `
Cel biznesowy, problem, proponowany rezultat i kryteria odbioru: **[DO WYPEŁNIENIA]**. Użytkownicy/role i pilność: **[DO WYPEŁNIENIA]**. Wpływ na dane osobowe, role, retencję, podprocesorów, bezpieczeństwo, dostępność i dokumenty: **[DO WYPEŁNIENIA]**. Oczekiwany termin: **[DO WYPEŁNIENIA]**.

## Ocena GF

Zakres, poza zakresem, zależności, ryzyko, estymacja, cena i VAT/dokument sprzedaży **[DO WERYFIKACJI BIZNESOWEJ: WYCENA]** / **[DO WERYFIKACJI KSIĘGOWEJ]**. Wymagane testy, aktualizacja DPA/polityk i rollback: **[DO WYPEŁNIENIA]**. Decyzja Klienta i Usługodawcy: **[DO WYPEŁNIENIA]**. Brak akceptacji oznacza brak zobowiązania do realizacji.`, { related: "wsparcie; protokół odbioru", production: "Tak", classification: "POUFNY" });

add("07-support/klasyfikacja-zgloszen.md", "Klasyfikacja zgłoszeń", "Macierz operacyjna", "Wsparcie", "Klient", `
| Typ | Kryterium | Priorytet | Ścieżka |
|---|---|---|---|
| Incydent bezpieczeństwa/RODO | poufność, integralność, dostępność lub podejrzenie danych | SEV1–SEV4 według wpływu | procedura incydentowa; nie zwykły backlog |
| Awaria | funkcja wcześniej działająca nie spełnia kryterium | P1–P3 | formularz błędu |
| Problem użytkowy | konfiguracja/instrukcja | P3/P4 | wsparcie |
| Zmiana | nowe lub zmienione zachowanie | P4 / plan | analiza i akceptacja zakresu/ceny |
| Dane/RODO | żądanie osoby, retencja, eksport/usunięcie | prawny termin | IOD + Klient administrator |

Priorytet wynika z wpływu i pilności, nie z nazwy nadanej przez zgłaszającego. Spór klasyfikacyjny rozstrzygają kontakty operacyjne, a incydent potencjalny pozostaje eskalowany do czasu kwalifikacji.`, { related: "zasady wsparcia; incydenty", production: "Tak" });

add("07-support/instrukcja-zglaszania-problemow-dla-klienta.md", "Instrukcja zgłaszania problemów dla Klienta", "Instrukcja użytkowa", "Usługodawca", "Klient", `
1. Sprawdź, czy problem dotyczy jednej osoby, roli czy wszystkich i zapisz dokładny czas.
2. Nie wysyłaj hasła, tokenu, pliku bazy, danych szczególnych ani zrzutu z niepotrzebnymi danymi.
3. Opisz oczekiwane/rzeczywiste zachowanie, bezpieczne kroki, wpływ i obejście.
4. Wyślij kanałem **[DO WYPEŁNIENIA]**. Podejrzenie przejęcia, wycieku lub utraty danych zgłoś natychmiast kanałem **[DO WYPEŁNIENIA: INCYDENTY]**.
5. Zachowaj numer i odpowiadaj w jednym wątku. Nie podejmuj destrukcyjnych prób naprawy ani wielokrotnego resetowania danych bez instrukcji.

Statusy: Nowe → Triage → Oczekuje na Klienta/GF → W realizacji → Do weryfikacji → Zamknięte.`, { related: "formularz błędu; zasady wsparcia", production: "Tak", classification: "DLA KLIENTA" });

add("07-support/README.md", "README — wsparcie", "Instrukcja użycia", "Właściciel pakietu", "Klient / prawnik", `
Przed podpisaniem zsynchronizować SLA z Umową. Obecne 2 Dni Robocze są punktem odniesienia, nie gwarancją naprawy. Uzupełnić kanały, godziny, eskalacje i zasady płatnych zmian.`, { related: "wszystkie pliki 07", production: "Tak" });

// 08 — rozliczenia
add("08-billing-and-records/instrukcja-wystawiania-dokumentu-sprzedazy.md", "Instrukcja wystawiania dokumentu sprzedaży", "Projekt księgowy", "Usługodawca", "Księgowy / doradca podatkowy", `
## Zasada

Przed pierwszym dokumentem ustalić status działalności nierejestrowanej, VAT, wymagany typ dokumentu, obowiązki KSeF, moment sprzedaży/przychodu, numerację i dane nabywcy. Instrukcja nie rozstrzyga netto/brutto, zwolnienia VAT ani KSeF.

## Cykl miesięczny

1. Zweryfikuj limit i warunki działalności nierejestrowanej oraz zmiany statusu.
2. Potwierdź wykonanie okresu, cenę 500,00 zł miesięcznie, datę sprzedaży i termin 7 dni zgodnie z projektem Umowy.
3. Pobierz zatwierdzone dane Stron; nie kopiuj danych pracowników.
4. Wybierz wariant dokumentu wyłącznie po decyzji **[DO WERYFIKACJI KSIĘGOWEJ: FAKTURA / RACHUNEK / INNY]**.
5. Uzupełnij VAT opisowo jako **[DO WERYFIKACJI PODATKOWEJ]**; nie wpisuj automatycznie 23% ani „netto/brutto”.
6. Sprawdź KSeF **[DO WERYFIKACJI KSIĘGOWEJ: OBOWIĄZEK I TERMIN]**, wystaw, doręcz zatwierdzonym kanałem i odnotuj płatność.
7. Zapisz w ewidencji sprzedaży i przechowuj zgodnie z decyzją księgową.

## Kontrola

Cztery oczy dla pierwszych trzech dokumentów. Korekty i anulowanie wyłącznie zgodnie z aktualnymi zasadami księgowymi. Nie modyfikować wystawionego dokumentu bez śladu.
${taxBases}`, { related: "warianty; numeracja; ewidencje; Umowa", production: "Tak", classification: "POUFNY KSIĘGOWY" });

addRaw("08-billing-and-records/ewidencja-sprzedazy-template.csv", "lp;data_sprzedazy;data_wystawienia_dokumentu;numer_dokumentu;nabywca;nip_nabywcy;opis_uslugi;okres_uslugi;kwota_nalezna;kwota_otrzymana;data_platnosci;termin_platnosci;status;sposob_platnosci;narastajaco_w_kwartale;uwagi\n");
addRaw("08-billing-and-records/ewidencja-kosztow-template.csv", "lp;data;numer_dokumentu;dostawca;nip_dostawcy;kategoria;opis;kwota;data_platnosci;zwiazek_z_dzialalnoscia;plik_dowodowy;uwagi\n");
addRaw("08-billing-and-records/rejestr-platnosci-template.csv", "numer_faktury;klient;kwota;data_wystawienia;termin_platnosci;data_platnosci;status_platnosci;dni_opoznienia;data_przypomnienia;uwagi\n");

add("08-billing-and-records/zasady-numeracji-dokumentow.md", "Zasady numeracji dokumentów", "Projekt księgowy", "Usługodawca", "Księgowy", `
Schemat roboczy: **[DO WERYFIKACJI KSIĘGOWEJ: NP. GF/ROK/MIESIĄC/NUMER]**. Numeracja jest jednoznaczna, chronologiczna w przyjętej serii i nieużywana ponownie. Osobne serie dla dokumentów/korekt tylko po decyzji księgowej. Rejestr nadaje numer przed finalizacją; anulowanie zachowuje numer, status, przyczynę i dowód. Zmiana schematu od daty **[DO WYPEŁNIENIA]** z mapą poprzedniej serii. Numer KSeF, jeśli dotyczy, jest odrębnym identyfikatorem.`, { related: "instrukcja sprzedaży; KSeF", production: "Tak", classification: "POUFNY KSIĘGOWY" });

add("08-billing-and-records/checklista-rozliczen-miesiecznych.md", "Checklista rozliczeń miesięcznych", "Checklista księgowa", "Usługodawca", "Księgowy", `
- [ ] zweryfikowano bieżący status działalności, limit przychodu i VAT;
- [ ] potwierdzono obowiązek/tryb KSeF i prawidłowy typ dokumentu;
- [ ] okres usługi, data sprzedaży, cena 500,00 zł i 7-dniowy termin zgodne z Umową;
- [ ] dane nabywcy i numeracja zweryfikowane; brak danych pracowników;
- [ ] oznaczenie VAT i kwotę do zapłaty zatwierdził **[DO WYPEŁNIENIA]**;
- [ ] dokument wystawiono/doręczono, zapisano w ewidencji, dostęp ograniczono;
- [ ] wpływ uzgodniono z rachunkiem, opóźnienia i korekty odnotowano;
- [ ] oceniono obowiązek rejestracji działalności/ZUS i konsultację na następny miesiąc.`, { related: "instrukcja; ewidencje; warianty", production: "Tak", classification: "POUFNY KSIĘGOWY" });

add("08-billing-and-records/warianty-rozliczenia-do-konsultacji.md", "Warianty rozliczenia do konsultacji", "Macierz decyzyjna", "Usługodawca", "Księgowy / prawnik", `
## Decyzje blokujące

| Pytanie | Warianty do oceny | Dowód/decyzja |
|---|---|---|
| Działalność nierejestrowana | warunki spełnione / obowiązek rejestracji / inna forma | **[DO WERYFIKACJI PRAWNEJ I KSIĘGOWEJ]** |
| VAT | zwolnienie podmiotowe/przedmiotowe / czynny VAT / inne | **[DO WERYFIKACJI PODATKOWEJ]** |
| Cena 500,00 zł | kwota należna w uzgodnionym wariancie; nie nazywać automatycznie netto/brutto | **[DO WERYFIKACJI KSIĘGOWEJ]** |
| Dokument | faktura / rachunek / inny prawidłowy dowód | **[DO WERYFIKACJI KSIĘGOWEJ]** |
| KSeF | obowiązek, termin, tryb, uprawnienia i awaria | **[DO WERYFIKACJI KSIĘGOWEJ]** |
| PIT/ZUS/płatnik | obowiązki usługodawcy i ewentualne obowiązki Klienta | **[DO WERYFIKACJI PODATKOWEJ I PRAWNEJ]** |

Nie zawierać w Umowie gwarancji braku ZUS/VAT ani kwalifikacji płatnika. Ponowić ocenę przed pierwszą sprzedażą, po zmianie prawa/statusu i po zbliżeniu do limitu. Źródła i data weryfikacji: **[DO WYPEŁNIENIA]**.
${taxBases}`, { related: "instrukcja sprzedaży; Umowa", production: "Tak", classification: "POUFNY DECYZYJNY" });

add("08-billing-and-records/README.md", "README — rozliczenia i ewidencje", "Instrukcja użycia", "Usługodawca", "Księgowy", `
Szablony faktury i rachunku są alternatywne i mają widoczną blokadę VAT/KSeF. Nie wysyłać żadnego przed pisemną decyzją księgową. CSV używają separatora średnikowego i nie powinny zawierać danych Użytkowników. Cena 500,00 zł jest wspólna z Umową, bez automatycznej etykiety netto/brutto.`, { related: "wszystkie pliki 08", production: "Tak", classification: "POUFNY KSIĘGOWY" });

// 09 — zakończenie współpracy
add("09-data-exit/procedura-zakonczenia-wspolpracy.md", "Procedura zakończenia współpracy", "Procedura offboardingu", "Usługodawca / Klient", "Prawnik / IOD / techniczny", `
## 1. Inicjacja

Podstawa i data końca Umowy **[DO WYPEŁNIENIA]**, okres wypowiedzenia zgodny z Umową, osoba decyzyjna i zakres danych **[DO WYPEŁNIENIA]**. Zamrozić nieuzgodnione zmiany i otworzyć kontrolowaną sprawę exit.

## 2. Plan

1. Potwierdzić ostatni dzień dostępu, zakres eksportu, format, odbiorcę i bezpieczny kanał.
2. Wykonać eksport i weryfikację; projekt Umowy wskazuje roboczo 14 dni — zsynchronizować.
3. Rozliczyć otwarte płatności, zgłoszenia, podprocesorów i nośniki.
4. Po potwierdzeniu odbioru odebrać konta managerów/pracowników, unieważnić sesje i dostępy techniczne.
5. Usunąć lub zanonimizować dane aktywne według instrukcji; projekt Umowy wskazuje roboczo 45 dni.
6. Kopie objąć wygasaniem według cyklu **[DO WYPEŁNIENIA]**, bez przywracania do bieżącej pracy.
7. Wydać protokół usunięcia/wyjątków i zamknąć rejestry.

## 3. Wyjątki

Zachowanie wymagane prawem, sporem lub zabezpieczeniem roszczeń wymaga celu, podstawy, zakresu, dostępu i terminu. Brak automatycznej „natychmiastowej” eliminacji z rotacyjnych backupów należy wyjaśnić Klientowi.
${gdprBases}`, { related: "DPA E; Umowa; eksport; usunięcie", production: "Tak", classification: "POUFNY UMOWNY" });

add("09-data-exit/protokol-eksportu-danych.md", "Protokół eksportu danych", "Protokół przekazania", "Usługodawca / Klient", "IOD / techniczny", `
Sprawa/Umowa, żądający i umocowanie: **[DO WYPEŁNIENIA]**. Zakres i wyłączenia: **[DO WYPEŁNIENIA]**. Format (np. CSV/PDF dostępny w funkcji lub uzgodniony format) **[DO WYPEŁNIENIA]**; kod źródłowy i tajemnice Usługodawcy nie są przedmiotem eksportu. Stan na datę **[DO WYPEŁNIENIA]**.

Kontrola: minimalizacja, liczność i spójność **[DO WYPEŁNIENIA]**; szyfrowanie/pakiet **[DO WYPEŁNIENIA]**; kanał pliku i klucza odrębne **[DO WYPEŁNIENIA]**; suma kontrolna **[DO WYPEŁNIENIA]**; data przekazania/pobrania **[DO WYPEŁNIENIA]**; odbiorca zweryfikowany **[DO WYPEŁNIENIA]**. Kopia robocza usunięta dnia **[DO WYPEŁNIENIA]**. Uwagi i podpisy Stron: **[DO WYPEŁNIENIA]**.`, { related: "procedura exit; DPA E", production: "Tak", classification: "POUFNY" });

add("09-data-exit/protokol-usuniecia-danych.md", "Protokół usunięcia danych", "Protokół wykonania", "Usługodawca", "Klient / IOD", `
Klient/Umowa/instrukcja: **[DO WYPEŁNIENIA]**. Zakres danych, systemy i podprocesorzy: **[DO WYPEŁNIENIA]**. Eksport/zwrot zakończony: **[DO WYPEŁNIENIA]**.

| Warstwa | Czynność | Data | Wykonawca | Kontrola | Wyjątek/termin |
|---|---|---|---|---|---|
| aktywna baza i konta/sesje | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| eksporty/robocze pliki/logi | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** | **[DO WYPEŁNIENIA]** |
| kopie rotacyjne | wygasają w cyklu **[DO WYPEŁNIENIA]** | — | **[DO WYPEŁNIENIA]** | brak zwykłego odtworzenia | **[DO WYPEŁNIENIA]** |

Nie poświadczać bez dowodu. Dane zachowane prawnie: podstawa, izolacja, dostęp i termin **[DO WYPEŁNIENIA]**. Oświadczenie i podpis wykonawcy/akceptacja Klienta: **[DO WYPEŁNIENIA]**.
${gdprBases}`, { related: "DPA E; exit; retencja", production: "Tak", classification: "POUFNY" });

add("09-data-exit/checklista-dezaktywacji-klienta.md", "Checklista dezaktywacji Klienta", "Checklista kontrolna", "Koordynator exit", "Klient / IOD", `
- [ ] podstawa, terminy, kontakty i instrukcja Klienta potwierdzone;
- [ ] eksport: zakres/format/odbiorca/kanał/test/odbiór udokumentowane;
- [ ] otwarte zgłoszenia, płatności, retencje i spory rozstrzygnięte;
- [ ] publikacja/zmiany zatrzymane w uzgodnionym momencie;
- [ ] konta managerów/pracowników, sesje i dostępy usługowe odebrane;
- [ ] baza aktywna, eksporty, logi i podprocesorzy obsłużeni według instrukcji;
- [ ] kopie rotacyjne mają datę wygaśnięcia i blokadę zwykłego przywrócenia;
- [ ] wyjątki prawne opisane; protokoły podpisane; Klient otrzymał komunikat końcowy.`, { related: "procedura exit; protokoły", production: "Tak", classification: "POUFNY" });

add("09-data-exit/README.md", "README — data exit", "Instrukcja użycia", "Właściciel pakietu", "Prawnik / IOD", `
Przed podpisaniem zgrać terminy 14/45 dni z Umową i DPA. Eksport dotyczy danych Klienta, nie kodu źródłowego. Usunięcie z aktywnej bazy i wygaśnięcie backupu dokumentuje się oddzielnie; nie wydawać bezwarunkowego certyfikatu bez dowodów.`, { related: "wszystkie pliki 09", production: "Tak" });

// 10 — finalny przegląd
add("10-final-review/checklista-gotowosci-do-podpisania.md", "Checklista gotowości do podpisania", "Checklista przeglądu", "Prawnik", "Właściciel SaaS", `
- [ ] zdolność i warunki działalności nierejestrowanej, reprezentacja i dane Stron;
- [ ] Umowa, Załącznik 1, Regulamin, informacja o zagrożeniach i DPA A–E spójne;
- [ ] cena 500,00 zł, okres, płatność 7 dni, wypowiedzenie, odpowiedzialność i wsparcie uzgodnione;
- [ ] role RODO, instrukcje, audyt, podprocesorzy, transfery, retencja i exit prawidłowe;
- [ ] podstawy przetwarzania własnego i klauzule informacyjne zatwierdzone;
- [ ] model UŚUDE, udostępnienie/akceptacja dokumentów i publiczne linki rozstrzygnięte;
- [ ] VAT, KSeF, PIT/ZUS pozostają decyzją właściwego specjalisty;
- [ ] wszystkie blokujące placeholdery zamknięte; wersje/datowanie/podpisy finalne.

Wynik: **[DO WERYFIKACJI PRAWNEJ: GOTOWE / WARUNKOWO / NIEGOTOWE]**; zastrzeżenia **[DO WYPEŁNIENIA]**.
${legalBases}`, { related: "cały pakiet; istniejące dokumenty", signature: "Tak", production: "Tak", classification: "POUFNY" });

add("10-final-review/checklista-gotowosci-do-produkcji.md", "Checklista gotowości do produkcji", "Checklista IOD i bezpieczeństwa", "IOD / techniczny", "Klient / właściciel SaaS", `
- [ ] role administrator/procesor i własne cele GF udokumentowane; DPA podpisana;
- [ ] mapa danych, rekord procesora, retencja i instrukcje Klienta uzupełnione;
- [ ] podprocesorzy, regiony, transfery i sprzeciw zatwierdzone;
- [ ] polityki prywatności/cookies opublikowane, kontakt i wersja poprawne;
- [ ] zewnętrzne media wyłączone lub click-to-load po ocenie prawnej;
- [ ] proces praw osób, naruszeń, eksportu/usunięcia i dowody przetestowane;
- [ ] zakaz danych szczególnych w polach wolnych zakomunikowany;
- [ ] privacy by design/DPIA screening i ryzyko rezydualne udokumentowane;
- [ ] Klient ma proces informowania Użytkowników, bo aplikacja nie loguje akceptacji;
- [ ] HTTPS, dostęp, backup poza hostem, restore, monitoring i incydenty mają dowody;
- [ ] krytyczne ryzyko sześciocyfrowych kodów, sesji localStorage i braku potwierdzonego rate limitingu/MFA zostało zamknięte albo formalnie rozstrzygnięte bez dopuszczenia nieakceptowalnego ryzyka.

Wynik: **[DO WERYFIKACJI RODO: GOTOWE / WARUNKOWO / NIEGOTOWE]**.
${gdprBases}`, { related: "DPA; privacy; cookies; incydenty; exit", production: "Tak", classification: "POUFNY" });

add("10-final-review/pytania-do-prawnika.md", "Pytania do prawnika", "Lista konsultacyjna", "Właściciel SaaS", "Prawnik", `
1. Czy osoba i Klient mogą podpisać dokumenty w opisanej formie i kto je reprezentuje?
2. Czy działalność nierejestrowana jest dostępna w dacie startu i jakie zdarzenie wymusza rejestrację?
3. Czy Umowa, Regulamin i tryb udostępnienia/akceptacji spełniają UŚUDE i model B2B?
4. Czy limity odpowiedzialności, wypowiedzenie, poufność, prawo polskie i sąd są spójne i wykonalne?
5. Czy DPA odpowiada art. 28 RODO, role są poprawne, a model ogólnej zgody na podprocesorów i audytu właściwy?
6. Czy czasy eksportu/usunięcia, backup i wyjątki retencyjne są zgodne między dokumentami?
7. Jak ująć YouTube/zewnętrzne obrazy, transfery, odpowiedzialność Klienta i zakaz danych szczególnych?
8. Czy potrzebne są dodatkowe postanowienia o prawach autorskich, tajemnicy przedsiębiorstwa lub konsumentach mimo zakładanego B2B?
9. Czy Klient może mieć obowiązki płatnika PIT/ZUS wobec osoby świadczącej usługę?

Odpowiedzi, rekomendowane brzmienie i data stanu prawnego: **[DO WERYFIKACJI PRAWNEJ]**.
${legalBases}`, { related: "Umowa; Regulamin; DPA; exit", signature: "Tak", production: "Tak", classification: "POUFNY" });

add("10-final-review/pytania-do-ksiegowego.md", "Pytania do księgowego i doradcy podatkowego", "Lista konsultacyjna", "Usługodawca", "Księgowy / doradca podatkowy", `
1. Czy warunki i aktualny limit działalności nierejestrowanej są spełnione oraz kiedy powstanie obowiązek rejestracji?
2. Jaki jest status VAT i jak prawidłowo opisać cenę 500,00 zł bez błędnego założenia netto/brutto?
3. Czy wystawiać fakturę, rachunek czy inny dowód; jakie elementy, data sprzedaży i numeracja są wymagane?
4. Czy i od kiedy dotyczy KSeF, jak uzyskać uprawnienia oraz obsłużyć tryb awaryjny?
5. Jak prowadzić PIT, ewidencję przychodów/kosztów, korekty i przechowywanie?
6. Czy istnieją obowiązki ZUS albo obowiązki Klienta jako płatnika PIT/ZUS?
7. Jak rozliczyć płatność z góry, opóźnienia, zmianę statusu i przekroczenie limitu?

Odpowiedzi i data weryfikacji: **[DO WERYFIKACJI KSIĘGOWEJ I PODATKOWEJ]**.
${taxBases}`, { related: "billing; Umowa; oferta", signature: "Tak", production: "Tak", classification: "POUFNY KSIĘGOWY" });

add("10-final-review/pytania-do-klienta.md", "Pytania do Klienta przed startem", "Lista uzgodnień", "Koordynator wdrożenia", "Klient", `
1. Kto reprezentuje Klienta, administruje danymi i jest kontaktem 24/7 dla incydentów?
2. Ilu managerów/pracowników i placówek obejmuje start; jakie dane są rzeczywiście niezbędne?
3. Jakie są instrukcje retencji, eksportu, usunięcia i dopuszczalnych podprocesorów?
4. Czy Klient potwierdza zakaz danych szczególnych i swobodnych opisów zdrowia/nieobecności?
5. Jak Użytkownicy otrzymają polityki i jak Klient zachowa dowód, skoro aplikacja nie rejestruje akceptacji?
6. Czy zewnętrzne media mają być wyłączone czy click-to-load po zgodzie?
7. Jakie są godziny, SLA, kanał awaryjny i alternatywny sposób publikacji grafiku?
8. Jakie testy i odchylenia warunkują podpisanie protokołu startu?
9. Jakie dane mają znaleźć się na dokumencie sprzedaży i kto potwierdza odbiór?

Odpowiedzi, właściciele i data zatwierdzenia: **[DO WYPEŁNIENIA]**.`, { related: "launch; DPA; support", production: "Tak", classification: "POUFNY" });

add("10-final-review/lista-decyzji-wlasciciela-saas.md", "Lista decyzji właściciela SaaS", "Rejestr zarządczy", "Właściciel SaaS", "Doradcy domenowi", `
| ID | Decyzja | Opcje | Właściciel | Konsultant | Termin | Blokuje | Status/dowód |
|---|---|---|---|---|---|---|---|
| D-01 | forma/status świadczenia | nierejestrowana / rejestracja / inne | Usługodawca | prawnik/księgowy | **[DO WYPEŁNIENIA]** | podpis/rozliczenie | Otwarte |
| D-02 | VAT i znaczenie 500,00 zł | **[DO WERYFIKACJI PODATKOWEJ]** | Usługodawca | księgowy | **[DO WYPEŁNIENIA]** | faktura | Otwarte |
| D-03 | KSeF i dokument | **[DO WERYFIKACJI KSIĘGOWEJ]** | Usługodawca | księgowy | **[DO WYPEŁNIENIA]** | faktura | Otwarte |
| D-04 | hosting/region/SMTP/podprocesorzy | potwierdzić dostawców | techniczny | IOD | **[DO WYPEŁNIENIA]** | DPA/produkcja | Otwarte |
| D-05 | media zewnętrzne | wyłączyć / proxy / click-to-load | właściciel | IOD/techniczny | **[DO WYPEŁNIENIA]** | produkcja | Otwarte |
| D-06 | auth managera i sesja | kontrole kompensujące / przebudowa | techniczny | bezpieczeństwo | **[DO WYPEŁNIENIA]** | produkcja | Otwarte |
| D-07 | backup poza hostem/RPO/RTO | dostawca i parametry | właściciel | techniczny | **[DO WYPEŁNIENIA]** | produkcja | Otwarte |
| D-08 | retencja/exit | terminy Umowy i backup | Klient/GF | IOD/prawnik | **[DO WYPEŁNIENIA]** | DPA | Otwarte |
| D-09 | SLA i kontakty | czasy/godziny/kanały | Strony | operacje | **[DO WYPEŁNIENIA]** | start | Otwarte |
`, { related: "raport ryzyk; placeholdery", signature: "Tak", production: "Tak", classification: "POUFNY" });

add("10-final-review/finalny-raport-gotowosci.md", "Finalny raport gotowości", "Raport bramkowy", "Właściciel pakietu", "Prawnik / IOD / księgowy / techniczny", `
## Ocena

**Status: GOTOWE DO PROFESJONALNEGO PRZEGLĄDU — NIEGOTOWE DO PODPISANIA ANI PRODUKCJI.** Pakiet pokrywa wymagane dokumenty, ale otwarte decyzje są materialne.

## Blokery podpisania

Tożsamość/umocowanie Stron, dostępność działalności nierejestrowanej, finalna Umowa i odpowiedzialność, DPA z realnymi podprocesorami/regionami/transferami, terminy retencji/exit, SLA oraz opinie prawna, RODO i księgowo-podatkowa.

## Blokery produkcji

Potwierdzenie HTTPS i hostingu; wzmocnienie lub formalne zamknięcie ryzyka sześciocyfrowego hasła, braku potwierdzonego rate limitingu/MFA i JWT w localStorage; decyzja zewnętrznych mediów; dostawca SMTP; backup poza wolumenem, szyfrowanie/alerty/test restore; monitoring, kontakty i testy uprawnień/export/delete.

## Kolejność zamknięcia

1. Decyzje właściciela i odpowiedzi Klienta.
2. Przegląd prawnika, IOD oraz księgowego/doradcy podatkowego.
3. Remediacje techniczne i dowody testów.
4. Zamknięcie rejestru placeholderów i finalne wersje.
5. Negocjacja i podpis Umowy + DPA A–E.
6. Onboarding, odbiór, protokół startu i pierwszy prawidłowy dokument sprzedaży.

Data ponownej oceny i podpisy recenzentów: **[DO WYPEŁNIENIA]**.`, { related: "wszystkie pliki 10; raport braków; placeholdery", signature: "Tak", production: "Tak", classification: "POUFNY" });

for (const path of [
  "01-data-processing-agreement/zalacznik-a-opis-przetwarzania.md",
  "01-data-processing-agreement/zalacznik-b-kategorie-danych-i-osob.md",
  "01-data-processing-agreement/zalacznik-c-srodki-techniczne-i-organizacyjne.md",
  "01-data-processing-agreement/zalacznik-d-podmioty-podprzetwarzajace.md",
  "01-data-processing-agreement/zalacznik-e-procedura-usuniecia-lub-zwrotu-danych.md"
]) created.set(path, created.get(path).trimEnd() + "\n" + dpaBases);
created.set("03-cookies-and-storage/rekomendacja-mechanizmu-zgody.md", created.get("03-cookies-and-storage/rekomendacja-mechanizmu-zgody.md").trimEnd() + "\n" + legalBases);

// HTML — wspólny, samowystarczalny renderer bez skryptów i zależności zewnętrznych
function esc(value) {
  return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;");
}

function inline(value) {
  let out = esc(value);
  out = out.replace(/\[([^\]]+)\]\((https?:\/\/[^)]+)\)/g, '<a href="$2">$1</a>');
  out = out.replace(/\*\*([^*]+)\*\*/g, "<strong>$1</strong>");
  out = out.replace(/`([^`]+)`/g, "<code>$1</code>");
  return out;
}

function markdownBody(md) {
  const lines = md.replace(/\r/g, "").split("\n");
  const out = [];
  let i = 0;
  while (i < lines.length) {
    const line = lines[i];
    if (!line.trim()) { i++; continue; }
    if (line.startsWith("```")) {
      const lang = line.slice(3).trim(); const block = []; i++;
      while (i < lines.length && !lines[i].startsWith("```")) block.push(lines[i++]);
      i++;
      out.push(`<pre data-language="${esc(lang)}"><code>${esc(block.join("\n"))}</code></pre>`); continue;
    }
    const heading = /^(#{1,4})\s+(.+)$/.exec(line);
    if (heading) { const level = Math.min(heading[1].length, 4); out.push(`<h${level}>${inline(heading[2])}</h${level}>`); i++; continue; }
    if (line.startsWith("> ")) { out.push(`<blockquote>${inline(line.slice(2))}</blockquote>`); i++; continue; }
    if (line.includes("|") && i + 1 < lines.length && /^\s*\|?\s*:?-+/.test(lines[i + 1])) {
      const rows = []; const split = x => x.trim().replace(/^\||\|$/g, "").split("|").map(v => v.trim());
      const headers = split(line); i += 2;
      while (i < lines.length && lines[i].includes("|") && lines[i].trim()) rows.push(split(lines[i++]));
      out.push(`<div class="table-wrap"><table><thead><tr>${headers.map(h => `<th>${inline(h)}</th>`).join("")}</tr></thead><tbody>${rows.map(r => `<tr>${headers.map((_, n) => `<td>${inline(r[n] ?? "")}</td>`).join("")}</tr>`).join("")}</tbody></table></div>`); continue;
    }
    if (/^\s*(?:[-*]|\d+\.)\s+/.test(line)) {
      const ordered = /^\s*\d+\./.test(line); const items = [];
      while (i < lines.length && /^\s*(?:[-*]|\d+\.)\s+/.test(lines[i])) items.push(lines[i++].replace(/^\s*(?:[-*]|\d+\.)\s+/, ""));
      const tag = ordered ? "ol" : "ul"; out.push(`<${tag}>${items.map(x => `<li>${inline(x)}</li>`).join("")}</${tag}>`); continue;
    }
    const p = [line]; i++;
    while (i < lines.length && lines[i].trim() && !/^(?:#{1,4}\s|>|```|\s*(?:[-*]|\d+\.)\s+)/.test(lines[i]) && !(lines[i].includes("|") && i + 1 < lines.length && /^\s*\|?\s*:?-+/.test(lines[i + 1]))) p.push(lines[i++]);
    out.push(`<p>${inline(p.join(" "))}</p>`);
  }
  return out.join("\n");
}

function htmlDocument(title, main, documentType = "Dokument") {
  return `<!doctype html>
<html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>${esc(title)} — DRAFT</title><style>
:root{--ink:#17202a;--muted:#5f6b76;--line:#d7dde3;--accent:#245b78;--paper:#fff;--warn:#8b2f2f}*{box-sizing:border-box}body{margin:0;background:#eef1f4;color:var(--ink);font:15px/1.55 system-ui,-apple-system,"Segoe UI",Arial,sans-serif}.sheet{width:min(210mm,100%);min-height:297mm;margin:18px auto;padding:16mm 17mm;background:var(--paper);box-shadow:0 6px 26px #1a2a3617}.docbar{display:flex;justify-content:space-between;gap:12px;border-bottom:2px solid var(--accent);padding-bottom:8px;color:var(--muted);font-size:12px}.draft{color:var(--warn);font-weight:800;letter-spacing:.08em}h1{font-size:25px;line-height:1.2;margin:20px 0 14px;color:#123c54}h2{font-size:18px;margin:25px 0 8px;color:#184e68;border-bottom:1px solid var(--line);padding-bottom:4px}h3{font-size:15px;margin:18px 0 6px}p{margin:7px 0}a{color:#145f84;overflow-wrap:anywhere}blockquote{margin:12px 0;padding:10px 12px;border-left:4px solid #d69b2d;background:#fff8e8}.table-wrap{overflow-x:auto;margin:10px 0 16px}table{border-collapse:collapse;width:100%;font-size:12.5px}th,td{border:1px solid var(--line);padding:6px 7px;text-align:left;vertical-align:top}th{background:#edf4f7}ul,ol{padding-left:22px}li{margin:3px 0}code,pre{font-family:ui-monospace,Consolas,monospace}pre{white-space:pre-wrap;border:1px solid var(--line);background:#f7f9fa;padding:10px}.footer{margin-top:28px;border-top:1px solid var(--line);padding-top:7px;color:var(--muted);font-size:11px}.amount{font-size:23px;font-weight:800;color:#123c54}.notice{border:2px solid #b56b20;background:#fff8e8;padding:12px;margin:12px 0}.signature-grid{display:grid;grid-template-columns:1fr 1fr;gap:25px;margin-top:32px}.signature{border-top:1px solid #444;padding-top:6px;margin-top:36px}@media(max-width:720px){.sheet{margin:0;padding:22px 16px;min-height:0}.docbar,.signature-grid{grid-template-columns:1fr;display:grid}.table-wrap{margin-right:-8px}h1{font-size:22px}}@media print{@page{size:A4;margin:18mm}body{background:#fff}.sheet{width:auto;min-height:0;margin:0;padding:0;box-shadow:none}.table-wrap{overflow:visible}a{color:inherit;text-decoration:none}h2,table,blockquote,pre,.notice{break-inside:avoid}h1,h2,h3,h4{break-after:avoid}p,li{orphans:3;widows:3}.docbar{position:static}}
</style></head><body><main class="sheet"><header class="docbar"><span>GF · ${esc(documentType)}</span><span>Wersja: [DO WYPEŁNIENIA] · <span class="draft">DRAFT</span></span></header>${main}<footer class="footer">Projekt do profesjonalnej weryfikacji. Data obowiązywania: [DO WYPEŁNIENIA] · Właściciel: [DO WYPEŁNIENIA] · Recenzent: [DO WYPEŁNIENIA]</footer></main></body></html>`;
}

for (const [mdPath, htmlPath, type] of [
  ["01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md", "01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.html", "Umowa powierzenia"],
  ["02-privacy/polityka-prywatnosci-pl.md", "02-privacy/polityka-prywatnosci-pl.html", "Polityka prywatności"],
  ["03-cookies-and-storage/polityka-cookies-pl.md", "03-cookies-and-storage/polityka-cookies-pl.html", "Polityka cookies i storage"],
  ["06-client-launch/protokol-uruchomienia-platformy.md", "06-client-launch/protokol-uruchomienia-platformy.html", "Protokół uruchomienia"]
]) {
  const source = created.get(mdPath);
  const title = records.find(x => x.path === mdPath)?.title ?? type;
  addRaw(htmlPath, title + " — HTML", "Wersja do druku A4", "Strony / Użytkownicy", "Właściciel dokumentu", htmlDocument(title, markdownBody(source), type), { demo: "Tak", signature: type.includes("Umowa") || type.includes("Protokół") ? "Tak" : "Nie", production: "Tak" });
}

const invoiceBody = `<h1>FAKTURA — WZÓR ROBOCZY</h1><div class="notice"><strong>NIE WYSTAWIAĆ.</strong> [DO WERYFIKACJI KSIĘGOWEJ I PODATKOWEJ: status VAT, prawidłowy typ dokumentu, znaczenie kwoty 500,00 zł oraz obowiązek i tryb KSeF]</div>
<table><tr><th>Numer</th><td>[DO WYPEŁNIENIA]</td><th>Miejsce i data wystawienia</th><td>[DO WYPEŁNIENIA]</td></tr><tr><th>Data sprzedaży</th><td>[DO WYPEŁNIENIA]</td><th>Termin płatności</th><td>[DO WYPEŁNIENIA: 7 dni zgodnie z Umową]</td></tr></table>
<div class="signature-grid"><section><h2>Sprzedawca</h2><p>[DO WYPEŁNIENIA: imię i nazwisko osoby fizycznej, adres, NIP jeśli nadany i wymagany]</p><p>Kontakt: [DO WYPEŁNIENIA] · Rachunek bankowy: [DO WYPEŁNIENIA]</p><p>Status/formuła: [DO WERYFIKACJI KSIĘGOWEJ]</p></section><section><h2>Nabywca</h2><p>[DO WYPEŁNIENIA: pełna firma, adres, NIP]</p></section></div>
<h2>Pozycje</h2><table><thead><tr><th>Lp.</th><th>Nazwa usługi</th><th>Ilość</th><th>J.m.</th><th>Cena</th><th>Kwota</th></tr></thead><tbody><tr><td>1</td><td>Dostęp do Platformy GF — abonament za [DO WYPEŁNIENIA: okres]</td><td>1</td><td>miesiąc</td><td>500,00 zł</td><td>500,00 zł</td></tr></tbody></table>
<p>VAT/zwolnienie: <strong>[DO WERYFIKACJI PODATKOWEJ: właściwe oznaczenie; nie wpisywać automatycznie konkretnej stawki ani „netto/brutto”]</strong></p><p class="amount">Do zapłaty: 500,00 zł</p><p>Słownie: [DO WYPEŁNIENIA] · Forma płatności: [DO WYPEŁNIENIA] · KSeF/identyfikator: [DO WERYFIKACJI KSIĘGOWEJ]</p><p>Uwagi: [DO WYPEŁNIENIA]</p>`;
addRaw("08-billing-and-records/wzor-faktury-pl.html", "Wzór faktury PL", "Alternatywny szablon sprzedaży", "Usługodawca / Klient", "Księgowy", htmlDocument("Wzór faktury PL", invoiceBody, "Dokument sprzedaży"), { signature: "Tak", production: "Tak", classification: "POUFNY KSIĘGOWY" });

const receiptBody = `<h1>RACHUNEK — WZÓR ROBOCZY</h1><div class="notice"><strong>NIE WYSTAWIAĆ.</strong> [DO WERYFIKACJI KSIĘGOWEJ I PODATKOWEJ: czy rachunek jest prawidłowym dokumentem, status VAT, KSeF i prezentacja kwoty]</div>
<table><tr><th>Numer</th><td>[DO WYPEŁNIENIA]</td><th>Data i miejsce</th><td>[DO WYPEŁNIENIA]</td></tr><tr><th>Okres usługi</th><td>[DO WYPEŁNIENIA]</td><th>Termin płatności</th><td>[DO WYPEŁNIENIA: 7 dni zgodnie z Umową]</td></tr></table><div class="signature-grid"><section><h2>Wystawca</h2><p>[DO WYPEŁNIENIA: prawidłowe dane]</p></section><section><h2>Odbiorca</h2><p>[DO WYPEŁNIENIA: firma, adres, NIP]</p></section></div>
<h2>Usługa</h2><table><thead><tr><th>Opis</th><th>Ilość</th><th>Kwota</th></tr></thead><tbody><tr><td>Dostęp do Platformy GF — abonament miesięczny</td><td>1</td><td>500,00 zł</td></tr></tbody></table><p class="amount">Należność: 500,00 zł</p><p>Kwalifikacja VAT/KSeF: [DO WERYFIKACJI KSIĘGOWEJ I PODATKOWEJ] · Słownie/forma płatności: [DO WYPEŁNIENIA]</p><div class="signature-grid"><div class="signature">Wystawca</div><div class="signature">Odbiorca (jeśli wymagane)</div></div>`;
addRaw("08-billing-and-records/wzor-rachunku-pl.html", "Wzór rachunku PL", "Alternatywny szablon sprzedaży", "Usługodawca / Klient", "Księgowy", htmlDocument("Wzór rachunku PL", receiptBody, "Dokument sprzedaży"), { signature: "Tak", production: "Tak", classification: "POUFNY KSIĘGOWY" });

// 00 — indeks i raporty nadrzędne
const masterPaths = [
  "00-master-index/indeks-dokumentow.md", "00-master-index/macierz-spojnosci-dokumentow.md", "00-master-index/rejestr-placeholderow.md",
  "00-master-index/raport-brakow-i-ryzyk.md", "00-master-index/changelog.md", "00-master-index/README.md"
];
const existingRefs = [
  { path: "../commercial-offer/", title: "Oferta handlowa — wersja autorytatywna", purpose: "Prezentacja handlowa", audience: "Klient", owner: "Właściciel SaaS", beforeDemo: "Tak", beforeSignature: "Tak", beforeProduction: "Nie", status: "Istniejący DRAFT — niezmieniony" },
  { path: "../legal/saas-agreement/", title: "Umowa SaaS i Załącznik nr 1 — wersja autorytatywna", purpose: "Kontrakt główny", audience: "Strony", owner: "Usługodawca / prawnik", beforeDemo: "Nie", beforeSignature: "Tak", beforeProduction: "Tak", status: "Istniejący DRAFT — niezmieniony" },
  { path: "../legal/electronic-services-regulations/", title: "Regulamin i informacja o zagrożeniach — wersja autorytatywna", purpose: "UŚUDE i zasady Użytkownika", audience: "Użytkownicy", owner: "Usługodawca / prawnik", beforeDemo: "Tak", beforeSignature: "Tak", beforeProduction: "Tak", status: "Istniejący DRAFT — niezmieniony" }
];
const indexRows = [...existingRefs, ...masterPaths.map(path => ({ path, title: path.split("/").at(-1), purpose: "Nawigacja i kontrola", audience: "Właściciel pakietu / recenzenci", owner: "Właściciel SaaS", beforeDemo: "Nie", beforeSignature: "Tak", beforeProduction: "Tak", status: "DRAFT" })), ...records]
  .sort((a, b) => a.path.localeCompare(b.path, "pl"))
  .map((x, i) => `| ${i + 1} | \`${x.path}\` | ${x.title} | ${x.purpose} | ${x.audience} | ${x.owner} | ${x.beforeDemo} | ${x.beforeSignature} | ${x.beforeProduction} | ${x.status} |`).join("\n");

add("00-master-index/indeks-dokumentow.md", "Indeks dokumentów pakietu pierwszego Klienta", "Indeks nadrzędny", "Właściciel SaaS / recenzenci", "Właściciel pakietu", `
## Zakres

Pakiet zawiera 81 wymaganych artefaktów w 11 sekcjach. Plik techniczny \`generate-package.mjs\` służy wyłącznie deterministycznemu odtworzeniu pakietu i nie jest dokumentem klienta. Wszystkie artefakty mają status DRAFT.

| Lp. | Ścieżka | Tytuł | Cel | Odbiorca | Właściciel | Przed demo | Przed podpisaniem | Przed produkcją | Status |
|---:|---|---|---|---|---|---|---|---|---|
${indexRows}

## Istniejące wersje autorytatywne — niepowielone

- \`docs/commercial-offer/\` — oferta handlowa;
- \`docs/legal/saas-agreement/\` — Umowa SaaS i Załącznik nr 1;
- \`docs/legal/electronic-services-regulations/\` — Regulamin i informacja o zagrożeniach.

Przed użyciem sprawdzić rejestr placeholderów, raport ryzyk i finalny raport gotowości.`, { related: "cały pakiet; istniejące dokumenty", signature: "Tak", production: "Tak" });

add("00-master-index/macierz-spojnosci-dokumentow.md", "Macierz spójności dokumentów", "Kontrola terminologii i parametrów", "Recenzenci", "Właściciel pakietu", `
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

Nie zmieniono dokumentów istniejących. Wszelkie korekty do nich należy wykonać osobno, z wersją i wpisem w changelogu.`, { related: "oferta; Umowa; Regulamin; DPA; billing", signature: "Tak", production: "Tak" });

add("00-master-index/raport-brakow-i-ryzyk.md", "Raport braków i ryzyk", "Raport audytowy", "Właściciel SaaS / recenzenci", "Właściciel pakietu", `
## Podsumowanie

Pakiet jest kompletnym szkieletem do profesjonalnego przeglądu, ale nie potwierdza gotowości prawnej ani produkcyjnej. Ocena repozytorium jest statyczna i nie potwierdza konfiguracji działającego środowiska.

| Priorytet | Brak/ryzyko | Dowód z audytu repo | Blokuje | Właściciel / działanie |
|---|---|---|---|---|
| Krytyczny | słabe uwierzytelnienie uprzywilejowane | dokładnie 6 cyfr; brak potwierdzonego rate limitingu/MFA | produkcję | techniczny: projekt i test kontroli |
| Wysoki | JWT w localStorage / XSS | klucz \`gf3.auth.access-token\` | produkcję | ocena sesji, CSP/XSS i architektury |
| Wysoki | backup w tej samej przestrzeni | 10 kopii SQLite, około godzinowo podczas pracy | produkcję | kopia odseparowana, szyfrowanie, alert, restore |
| Wysoki | dostawcy/regiony nieustalone | hosting i SMTP niepotwierdzone | DPA/produkcję | inwentaryzacja i umowy podprocesorów |
| Wysoki | zewnętrzne media | dowolny image URL i youtube-nocookie iframe | produkcję/polityki | wyłączyć/proxy/click-to-load |
| Wysoki | TLS niepotwierdzony produkcyjnie | repo ma osobny overlay HTTPS | produkcję | test publicznego HTTPS i proxy |
| Wysoki | brak logowania akceptacji prawa | brak wersji/dat akceptacji i publicznych tras polityk | start | proces poza systemem lub wdrożenie |
| Średni | pola swobodne | notatki/komunikaty mogą przyjąć dane szczególne | onboarding | zakaz, szkolenie, minimalizacja |
| Średni | brak pełnego self-service delete/export | częściowe eksporty i manager deletion zależne od relacji | DPA/exit | zakres/formats/runbook i test |
| Średni | monitoring i metadane | logi aktora; brak potwierdzonego IP/UA/alertingu | produkcję | minimalny monitoring i retencja |
| Blokujący biznesowo | status działalności/VAT/KSeF/PIT/ZUS | brak możliwości rozstrzygnięcia z kodu | podpis/fakturę | prawnik + księgowy + doradca podatkowy |

## Pozytywne ustalenia ograniczone

Potwierdzono role manager/pracownik i autoryzację endpointów, hash PBKDF2-SHA256, wersjonowanie sesji, logi operacyjne, kopie SQLite i narzędzia restore/export. Ukierunkowany skan śledzonych plików nie potwierdził literalnego sekretu ani prywatnego klucza; nie jest to gwarancja pełnego secret-scanu. Nie odczytywano lokalnych sekretów.

## Remediacja przed startem

Zamknąć decyzje z final-review, wykonać przeglądy specjalistyczne, wdrożyć kontrole techniczne, przetestować produkcję i uzupełnić rejestr placeholderów. Nie przenosić ryzyka krytycznego na Klienta ogólną klauzulą.`, { related: "final review; security; backup; cookies", signature: "Tak", production: "Tak", classification: "POUFNY" });

add("00-master-index/changelog.md", "Changelog pakietu", "Historia zmian dokumentacyjnych", "Recenzenci", "Właściciel pakietu", `
## [DRAFT 0.1] — 2026-07-28

### Dodano

- 81 wymaganych plików pakietu w \`docs/client-launch-package/\`;
- DPA §1–16 i A–E, prywatność/RODO, storage, incydenty, backup/BCP, launch, support, billing, exit i final review;
- sześć samowystarczalnych wersji HTML do A4;
- indeks, macierz, automatyczny rejestr placeholderów oraz raport ryzyk.

### Istniejące materiały

Przeczytano i wykorzystano terminologię z \`docs/commercial-offer/\`, \`docs/legal/saas-agreement/\` i \`docs/legal/electronic-services-regulations/\`. **Nie zmodyfikowano ich.** Nie zmieniono kodu aplikacji, schematu bazy, konfiguracji produkcyjnej ani danych.

### Do kolejnej wersji

Wpisać wyniki profesjonalnych konsultacji i technicznych testów jako osobne, identyfikowalne zmiany; nie zmieniać statusu na finalny bez podpisów recenzentów.`, { related: "indeks; macierz; git diff", production: "Tak" });

add("00-master-index/README.md", "README — pakiet startowy pierwszego Klienta", "Instrukcja pakietu", "Właściciel SaaS", "Recenzenci", `
## Użycie

1. Zacznij od raportu braków, rejestru decyzji i placeholderów.
2. Wyślij właściwe checklisty prawnikowi, IOD i księgowemu; nie wysyłaj poufnych runbooków Klientowi.
3. Po remediacji zaktualizuj dowody, wersje i changelog.
4. Negocjuj i podpisuj wyłącznie wybrane dokumenty; następnie onboarding, protokół uruchomienia i dokument sprzedaży.

## Pakiet podpisowy rekomendowany

Umowa SaaS + Załącznik nr 1 (wersje autorytatywne w istniejącym katalogu), DPA z A–E, protokół uruchomienia oraz uzgodnione potwierdzenia. Publicznie: Regulamin, zagrożenia, polityka prywatności i cookies/storage, ewentualnie publiczna lista podprocesorów. Wewnętrznie: analizy, rejestry, incydenty, backup, billing i checklisty.

HTML są samowystarczalne, responsywne, bez JavaScript, analityki i zewnętrznych fontów. Generator można uruchomić ponownie po świadomej zmianie źródła; nadpisuje tylko pliki w tym nowym pakiecie.`, { related: "indeks; finalny raport; istniejące dokumenty", signature: "Tak", production: "Tak" });

created.set("00-master-index/macierz-spojnosci-dokumentow.md", created.get("00-master-index/macierz-spojnosci-dokumentow.md") + `
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
`);

created.set("00-master-index/raport-brakow-i-ryzyk.md", created.get("00-master-index/raport-brakow-i-ryzyk.md") + `
## Wymagane zmiany aplikacji i infrastruktury

| Tytuł | Powód | Dokument prawny | Obszar kodu/infrastruktury | Kryterium akceptacji | Priorytet | Skutek bezpieczeństwa | Przed startem |
|---|---|---|---|---|---|---|---|
| Ochrona logowania managera | sześciocyfrowa przestrzeń i brak potwierdzonych ograniczeń | polityka bezpieczeństwa, Regulamin, DPA C | auth API/UI/proxy | test rate limit/lockout, decyzja MFA, bezpieczne recovery i dowody | Krytyczny | przejęcie konta i danych | Tak |
| Bezpieczna sesja | token localStorage zwiększa skutek XSS | privacy, cookies, DPA C | frontend auth, CSP, API token | udokumentowany threat model, test XSS/CSP, TTL/rotacja/revocation | Wysoki | kradzież sesji | Tak |
| Media zewnętrzne | żądania przed decyzją Użytkownika | cookies/privacy | moduły aktualności/powiadomień | zero requestów third-party przed click-to-load albo media wyłączone | Wysoki | ujawnienie IP/metadanych | Tak |
| Backup odseparowany | obecne kopie współdzielą awarię wolumenu | DPA C/E, BCP | hosting, zadanie backupu | szyfrowana kopia poza hostem, alarm, retencja, pozytywny restore RPO/RTO | Wysoki | utrata/dostępność danych | Tak |
| Akceptacja wersji dokumentów | brak dowodu w aplikacji | Regulamin/privacy/cookies | onboarding lub moduł legal | wersja, czas, użytkownik, typ akcji i eksport dowodu albo zatwierdzony proces zewnętrzny | Wysoki | brak rozliczalności | Tak lub proces kompensujący |
| Kontrolowany export/delete | częściowe funkcje i zależności encji | DPA E, exit | eksporty, konta, retencja | test kompletności, autoryzacji, formatów, aktywnego usunięcia i backup expiry | Wysoki | nadmiarowa retencja/niepełny zwrot | Tak |
| Monitoring i incident readiness | brak potwierdzonych alertów | DPA, polityka incydentów | health/logging/alerting | alert testowy, dyżur, retencja logów bez sekretów, ćwiczenie tabletop | Średni | późna detekcja | Tak |
`);

const ownerDecisions = `
| Decyzja | Opcje | Rekomendowany domyślny wariant | Ryzyko | Termin |
|---|---|---|---|---|
| Prawna nazwa produktu | GF / inna zatwierdzona | GF zgodnie z dokumentami | niespójność | [DO WYPEŁNIENIA] |
| Publiczne dane Usługodawcy | zakres ustawowy/umowny | komplet po konsultacji, bez danych prywatnych ponad wymóg | nieważne zawiadomienia | [DO WYPEŁNIENIA] |
| Model 500,00 zł | płatne z góry/z dołu; kwalifikacja podatkowa | z góry zgodnie z projektem, kwalifikacja księgowa otwarta | rozliczenia | [DO WYPEŁNIENIA] |
| Pilot | brak / okres płatny / odrębne testy | brak domniemania pilota | konflikt z Umową | [DO WYPEŁNIENIA] |
| Wypowiedzenie | 1 miesiąc / inny | 1 miesiąc na koniec miesiąca zgodnie z projektem | churn/spór | [DO WYPEŁNIENIA] |
| Odpowiedzialność | limit 6 miesięcy / inny | decyzja prawnika, bez konfliktu DPA | odpowiedzialność | [DO WYPEŁNIENIA] |
| Godziny wsparcia | robocze / rozszerzone | Dni Robocze, bez 24/7 poza incydentami | oczekiwania | [DO WYPEŁNIENIA] |
| Kontakt incydentowy | e-mail/telefon/system | dedykowany kanał z zastępstwem | opóźnienie | [DO WYPEŁNIENIA] |
| Backup częstotliwość/retencja | obecne / warstwowe | off-site warstwowy po teście RPO/RTO | utrata danych | [DO WYPEŁNIENIA] |
| Limit Użytkowników | brak / liczba | wpisać w Załączniku 1 | zakres/cena | [DO WYPEŁNIENIA] |
| Custom development | poza abonamentem / limit godzin | zawsze odrębne zamówienie | scope creep | [DO WYPEŁNIENIA] |
| Eksport/usunięcie | 14/45 dni / inne | zsynchronizować po teście i opinii IOD | niewykonalna obietnica | [DO WYPEŁNIENIA] |
| Podprocesorzy | zgoda ogólna/szczególna | ogólna z powiadomieniem, do przeglądu | art. 28/transfer | [DO WYPEŁNIENIA] |
| Analityka/media | brak / consent / click-to-load | brak analityki; media wyłączone lub click-to-load | prywatność | [DO WYPEŁNIENIA] |
| Publiczne trasy prawne | wdrożyć / hostować osobno | publiczne stabilne URL | brak informacji | [DO WYPEŁNIENIA] |
| Akceptacja dokumentów | w aplikacji / poza systemem | proces dowodowy przed startem; wdrożenie planowe | rozliczalność | [DO WYPEŁNIENIA] |
| Przejście na JDG | próg/termin/plan | monitorowanie miesięczne i plan przed progiem | niezgodny status | [DO WYPEŁNIENIA] |
`;
created.set("10-final-review/lista-decyzji-wlasciciela-saas.md", metadata("Lista decyzji właściciela SaaS", "raport ryzyk; pytania do doradców", "POUFNY") + ownerDecisions.trim() + "\n");

created.set("10-final-review/pytania-do-prawnika.md", created.get("10-final-review/pytania-do-prawnika.md") + `
## Dodatkowe pytania wymagane

- Jak wpływa utrata statusu studenta na ZUS/rozliczenia i treść Umowy?
- Czy podpis dokumentowy/elektroniczny jest wystarczający dla Umowy, DPA i instrukcji?
- Jak opisać relację z Użytkownikami-pracownikami Klienta i podstawy prywatności własnej GF?
`);
created.set("10-final-review/pytania-do-ksiegowego.md", created.get("10-final-review/pytania-do-ksiegowego.md") + `
## Dane do jednoznacznej odpowiedzi

NIP i rachunek bankowy Usługodawcy, opis usługi, finalna kwota należna, rozpoznanie przychodu, koszty podatkowe, rozliczenie roczne PIT, utrata statusu studenta, korekty, ciągłość numeracji i plan przejścia do JDG: **[DO WERYFIKACJI KSIĘGOWEJ I PODATKOWEJ]**.
`);
created.set("10-final-review/pytania-do-klienta.md", created.get("10-final-review/pytania-do-klienta.md") + `
## Parametry do zebrania

Pełne dane prawne i reprezentant; kontakt billing/RODO/techniczny; planowana liczba Użytkowników, lokalizacje i role; funkcje/wyłączenia/custom work; import i dane szczególne; retencja/eksport; wsparcie; pilot; data uruchomienia i pierwszego fakturowania: **[DO WYPEŁNIENIA]**.
`);

const allGeneratedPaths = [...new Set([...masterPaths, ...records.map(x => x.path)])].sort((a, b) => a.localeCompare(b, "pl"));
const filesCreatedList = allGeneratedPaths.map(path => `- ${path}`).join("\n");
created.set("10-final-review/finalny-raport-gotowosci.md", metadata("Finalny raport gotowości", "indeks; rejestr placeholderów; raport braków", "POUFNY") + `
## 1. Executive summary

**Status pakietu: Ready for professional review / gotowe do profesjonalnego przeglądu.** Nie jest gotowe do negocjacji w wersji finalnej, podpisania ani produkcji, ponieważ pozostają blokujące placeholdery i kontrole bez dowodów. Demo bez danych realnych jest możliwe dopiero po zamknięciu blockerów prezentacyjnych.

## 2. Files created

Utworzono 81 wymaganych artefaktów (generator techniczny nie jest dokumentem):

${filesCreatedList}

## 3. Existing files reviewed

- docs/commercial-offer/ — oferta; przeczytana, niezmieniona.
- docs/legal/saas-agreement/ — Umowa SaaS i Załącznik 1; przeczytane, niezmienione.
- docs/legal/electronic-services-regulations/ — Regulamin i zagrożenia; przeczytane, niezmienione.

## 4. Confirmed system functionality

Role manager/pracownik; manager tworzy konta; odzyskiwanie przez e-mail; profile pracowników; placówki/organizacja; dostępności; tworzenie, publikacja i odczyt grafików; zamiany; powiadomienia/komunikaty i notatki managera; logi operacyjne; wybrane eksporty; administracyjne operacje backup/restore. Brak potwierdzonego self-registration, samodzielnego usuwania konta i logu akceptacji dokumentów.

## 5. Confirmed infrastructure

ASP.NET Core, frontend web, nginx, Docker Compose, SQLite i nazwany wolumen. Kod zawiera ogólny SMTP, lecz dostawca/region nie są potwierdzone. Hosting, region, monitoring i off-site backup pozostają do weryfikacji. YouTube w domenie youtube-nocookie.com i dowolne zewnętrzne obrazy mogą być osadzane; rola i transfer wymagają oceny.

## 6. Critical blockers

- Prezentacja: publiczne dane/linki, bezpieczne dane demo, wyłączenie lub kontrola mediów zewnętrznych.
- Podpis: strony/umocowanie, działalność nierejestrowana, DPA/podprocesorzy/regiony, retencja, odpowiedzialność, SLA i wszystkie decyzje prawne.
- Produkcja: HTTPS; auth managera (6 cyfr, rate limiting/MFA); sesja localStorage/XSS; SMTP; off-site backup/szyfrowanie/alert/test restore; monitoring; export/delete; incydenty.
- Pierwsza faktura: VAT, znaczenie 500,00 zł, typ dokumentu, NIP, KSeF, PIT/ZUS/płatnik, numeracja i rachunek.

## 7. Legal review

Identyfikacja strony w działalności nierejestrowanej; kwalifikacja SaaS/UŚUDE; podpisy; hierarchia; odpowiedzialność i sąd; IP/kod; DPA art. 28, audyt, podprocesorzy i transfery; retencja/exit; relacja z pracownikami; akceptacja Regulaminu; ewentualne ZUS/płatnik.

## 8. Accounting review

NIP, VAT, dokument sprzedaży i pola, ostateczne znaczenie 500,00 zł, przychód/koszty/PIT, KSeF, status studenta, płatnik, JDG, bank, numeracja i korekty.

## 9. RODO review

Role dla każdego celu, rejestr procesora, instrukcje, podstawy własne GF, podprocesorzy/transfery, retencja, prawa osób, DPIA screening, naruszenia, media zewnętrzne, pola swobodne i proces informowania/akceptacji.

## 10. Technical work

Remediacje i kryteria akceptacji są w raporcie braków: auth, bezpieczna sesja/CSP, media click-to-load, TLS, podprocesorzy/regiony, SMTP, backup odseparowany i restore, alerty, eksport/usunięcie oraz dowód akceptacji dokumentów.

## 11. Remaining placeholders

Pełny automatyczny wykaz: ../00-master-index/rejestr-placeholderow.md. Wszystkie pozycje mają status Otwarte do czasu wpisania wartości i dowodu recenzji.

## 12. Recommended signing package

1. Autorytatywna Umowa SaaS i Załącznik nr 1 z docs/legal/saas-agreement/.
2. DPA: umowa-powierzenia-przetwarzania-danych-pl.md wraz z Załącznikami A–E.
3. Protokół uruchomienia Platformy.
4. Protokół przekazania dostępu managera i potwierdzenie dokumentów — jeśli zatwierdzi prawnik.

## 13. Recommended public documents

Regulamin UŚUDE, informacja o zagrożeniach, polityka prywatności, polityka cookies/storage oraz — po decyzji — publiczny wykaz podprocesorów. Publiczne URL muszą być stabilne i dostępne bez logowania.

## 14. Recommended internal documents

Analizy ról/ryzyka, mapy/rejestry, procedury bezpieczeństwa/incydentów/backupu/BCP, formularze robocze, billing, decyzje, gap report i checklisty. Protokoły z danymi Stron są poufne, nie publiczne.

## 15. Next actions

| Kolejność | Właściciel | Działanie | Termin | Blokuje |
|---:|---|---|---|---|
| 1 | Właściciel SaaS + Klient | zamknąć decyzje i dane wejściowe | [DO WYPEŁNIENIA] | podpis/produkcję |
| 2 | Prawnik | Umowa, Regulamin, DPA i działalność nierejestrowana | [DO WYPEŁNIENIA] | podpis |
| 3 | IOD | role, podprocesorzy, transfery, retencja i polityki | [DO WYPEŁNIENIA] | podpis/produkcję |
| 4 | Księgowy/doradca podatkowy | VAT, KSeF, PIT/ZUS, dokument i 500,00 zł | [DO WYPEŁNIENIA] | fakturę |
| 5 | Techniczny | remediacje i dowody testów | [DO WYPEŁNIENIA] | produkcję |
| 6 | Właściciel pakietu | uzupełnić placeholdery, wersje, linki i changelog | [DO WYPEŁNIENIA] | podpis/produkcję |
| 7 | Strony | negocjować i podpisać Umowę + DPA A–E | [DO WYPEŁNIENIA] | onboarding |
| 8 | Koordynator | onboarding, odbiór, protokół startu | [DO WYPEŁNIENIA] | produkcję |
| 9 | Księgowy/Usługodawca | wystawić pierwszy zatwierdzony dokument | [DO WYPEŁNIENIA] | rozliczenie |
`);

created.set("00-master-index/README.md", metadata("README — pakiet startowy pierwszego Klienta", "indeks; finalny raport; istniejące dokumenty", "WEWNĘTRZNY") + `
## 1. Struktura

Sekcje 00–10 prowadzą od indeksu przez DPA, prywatność, storage, bezpieczeństwo, backup, launch, support, billing i exit do bramki finalnej.

## 2. Klasy dokumentów

Publiczne informują Użytkowników; umowne wiążą Strony; wewnętrzne są dowodami procesu i nie powinny być publikowane. Klasyfikację ma każdy plik MD.

## 3. Dokumenty podpisywane przez Klienta

Umowa SaaS + Załącznik 1 (katalog istniejący), DPA A–E oraz protokół uruchomienia; pozostałe tylko gdy prawnik wskaże.

## 4. Dokumenty Użytkownika

Regulamin, zagrożenia, privacy i cookies są czytane/udostępniane lub akceptowane według decyzji prawnej. Platforma nie zapisuje obecnie wersji akceptacji, więc wymagany jest dowód kompensujący.

## 5. Pliki wewnętrzne

Analizy, rejestry, incydenty, backup/BCP, formularze z danymi, billing, gap report i checklisty przechowuje się z ograniczonym dostępem.

## 6. Placeholdery

Zamykaj rejestr 00 w źródłowym pliku, wpisując wartość, właściciela i dowód. Po każdej zmianie ponownie wygeneruj rejestr. Nie zamieniaj niewiadomej na założenie.

## 7. HTML i PDF

Otwórz HTML lokalnie w Chrome/Edge, wybierz Drukuj → Zapisz jako PDF, A4, skala 100%, nagłówki przeglądarki wyłączone. Sprawdź każdą stronę i podpisy; HTML nie wymaga buildu ani internetu.

## 8. Wersjonowanie

Schemat [DO WYPEŁNIENIA: np. major.minor], data obowiązywania, recenzent i changelog. Podpisanej wersji nie nadpisuj.

## 9. Podprocesorzy

Techniczny potwierdza dostawcę/usługę/region/transfer/link; IOD ocenia, prawnik zatwierdza model powiadomienia; zmiana trafia do DPA D, privacy i changelogu przed użyciem.

## 10. Archiwizacja podpisów

Zachowaj niezmienny PDF, dowód podpisu/doręczenia, wersję źródłową, datę i uprawnienia w [DO WYPEŁNIENIA: REPOZYTORIUM DOKUMENTÓW], poza publicznym repo.

## 11. Pakiet dla prawnika

Umowa/Załącznik, Regulamin/zagrożenia, DPA A–E, privacy/cookies, support/exit, macierz, pytania prawne i decyzje — bez sekretów i danych pracowników.

## 12. Pakiet dla księgowego

Umowa i cena/termin, warianty, pytania, instrukcja, szablony i CSV; poproś o pisemną decyzję VAT/KSeF/PIT/ZUS/dokumentu.

## 13. Pakiet uruchomieniowy Klienta

Po recenzjach: podpisowy zestaw, publiczne polityki, formularze wdrożenia/kontaktów, checklisty użytkowników, wsparcie, protokół dostępu i uruchomienia.

## 14. Identyfikacja blokad

Filtruj kolumnę „Blokuje” w rejestrze placeholderów i finalnym raporcie. GO dopiero po zamknięciu wszystkich pozycji podpis/produkcja/fakturowanie i załączeniu dowodów.

Generator odtwarza tylko nowy katalog i nie zmienia aplikacji ani istniejących dokumentów. Status całego pakietu pozostaje DRAFT.
`);

async function walkDocs(dir) {
  const output = [];
  for (const entry of await readdir(dir, { withFileTypes: true })) {
    const full = join(dir, entry.name);
    if (entry.isDirectory()) output.push(...await walkDocs(full));
    else if (/\.(?:md|html)$/i.test(entry.name)) output.push(full);
  }
  return output;
}

for (const [path, content] of created) {
  const destination = join(packageRoot, path);
  await mkdir(dirname(destination), { recursive: true });
  await writeFile(destination, content, "utf8");
}

const placeholderRows = [];
const placeholderPattern = /\[(?:DO WYPEŁNIENIA|DO WERYFIKACJI(?: [A-ZĄĆĘŁŃÓŚŹŻ]+)*)(?::[^\]\r\n]+)?\]/g;
for (const file of await walkDocs(docsRoot)) {
  if (file.endsWith("rejestr-placeholderow.md")) continue;
  const source = await readFile(file, "utf8");
  for (const match of source.matchAll(placeholderPattern)) {
    const before = source.slice(0, match.index);
    const heading = [...before.matchAll(/^#{1,4}\s+(.+)$/gm)].at(-1)?.[1] ?? "Metadane / treść";
    const value = match[0];
    const owner = value.includes("PRAWNEJ") ? "Prawnik" : value.includes("KSIĘGOWEJ") || value.includes("PODATKOWEJ") ? "Księgowy / doradca podatkowy" : value.includes("RODO") ? "IOD" : value.includes("TECHNICZNEJ") ? "Techniczny" : value.includes("BIZNESOWEJ") ? "Właściciel SaaS" : "Właściciel dokumentu";
    const lower = (value + " " + heading).toLowerCase();
    const blocks = [];
    if (/prezent|demo|publicz|link|polityk|regulamin/.test(lower)) blocks.push("Prezentacja");
    if (/stron|podpis|umow|reprezent|odpowiedzial|dpa|prawnej/.test(lower)) blocks.push("Podpis");
    if (/vat|ksef|księgow|podatk|faktur|rachun/.test(lower)) blocks.push("Fakturowanie");
    if (/hosting|region|smtp|tls|backup|produkc|incydent|rpo|rto|auth|sesj|technicz/.test(lower)) blocks.push("Produkcja");
    const requiredValue = value.includes(":") ? value.slice(value.indexOf(":") + 1, -1).trim() : "Konkretna zatwierdzona wartość";
    const verification = value.startsWith("[DO WYPEŁNIENIA") ? "Uzupełnienie i dowód właściciela" : value.replace(/^\[DO WERYFIKACJI\s*/, "").split(":")[0].replace(/\]$/, "").trim();
    placeholderRows.push({ file: relative(docsRoot, file).replaceAll("\\", "/"), heading, value, requiredValue, owner, verification, blocks: [...new Set(blocks)].join(", ") || "Nie — redakcyjne" });
  }
}

const registryHeader = `# Rejestr placeholderów\n\n| Metadane | Wartość |\n|---|---|\n| Produkt | GF |\n| Wersja | 0.1 DRAFT |\n| Status | DRAFT – WYMAGA WERYFIKACJI |\n| Właściciel | Właściciel pakietu |\n| Recenzent | prawnik / IOD / księgowy / techniczny |\n| Data obowiązywania | nie dotyczy – projekt |\n| Data przeglądu | przed podpisaniem i produkcją |\n| Dokumenty powiązane | cały katalog docs |\n| Klasyfikacja | POUFNY WEWNĘTRZNY |\n\n> Rejestr wygenerowano ze wszystkich plików MD/HTML w katalogu docs. Sam rejestr jest wyłączony ze skanowania, aby uniknąć rekurencji. Powtórzenia są celowe; wszystkie pozycje pozostają otwarte do czasu wpisania wartości i dowodu.\n\n| ID | Plik | Sekcja | Placeholder | Wymagana wartość | Właściciel | Rodzaj weryfikacji | Blokuje |\n|---:|---|---|---|---|---|---|---|\n`;
const registryBody = placeholderRows.sort((a, b) => a.file.localeCompare(b.file, "pl") || a.value.localeCompare(b.value, "pl")).map((x, i) => `| P-${String(i + 1).padStart(4, "0")} | \`${x.file}\` | ${x.heading.replaceAll("|", "\\|")} | ${x.value.replaceAll("|", "\\|")} | ${x.requiredValue.replaceAll("|", "\\|")} | ${x.owner} | ${x.verification.replaceAll("|", "\\|")} | ${x.blocks} |`).join("\n");
const registryPath = join(packageRoot, "00-master-index/rejestr-placeholderow.md");
await writeFile(registryPath, registryHeader + registryBody + "\n", "utf8");

const manifest = [...created.keys(), "00-master-index/rejestr-placeholderow.md"].sort();
const digest = createHash("sha256").update(manifest.map(x => x + "\n").join("")).digest("hex");
console.log(JSON.stringify({ packageRoot, requiredArtifacts: manifest.length, generator: "generate-package.mjs", placeholders: placeholderRows.length, manifestSha256: digest }, null, 2));
