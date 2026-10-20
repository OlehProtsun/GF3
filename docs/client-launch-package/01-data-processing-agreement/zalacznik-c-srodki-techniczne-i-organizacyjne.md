# Załącznik C – środki techniczne i organizacyjne

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA § 7; polityka bezpieczeństwa |
| Klasyfikacja | UMOWNY / POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

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
| Ciągłość | kontenery i kopie lokalne | deploy | brak RTO/RPO i alternatywy | zatwierdzić plan | Biznesowa/techniczna |

## Podstawy i materiały do weryfikacji

- [RODO – art. 28, 29, 32–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Wytyczne EROD 07/2020 – administrator i podmiot przetwarzający](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)

## Macierz zabezpieczeń — rozdzielenie kodu i eksploatacji

Statusy: IMPLEMENTED = mechanizm w badanym kodzie, PARTIALLY IMPLEMENTED = mechanizm z istotną luką, PROPOSED = zobowiązanie/procedura do wdrożenia, UNVERIFIED = brak dowodu działania w produkcji. Żaden wiersz nie jest certyfikatem. Szczegółowe symbole i linie zawiera raport zakończenia.

| Kontrola | Status i dowód | Luka i wymagane działanie |
|---|---|---|
| Haszowanie haseł | IMPLEMENTED: BusinessLogicLayer/Security/PasswordHasher.cs, PBKDF2-SHA256 i sól | Nie chroni przed zgadywaniem krótkiego hasła; okresowy przegląd parametrów |
| Polityka hasła | PARTIALLY IMPLEMENTED: NumericPasswordPolicy.Length = 6 | Uprzywilejowane hasła numeryczne mają małą przestrzeń; wymagany hardening i test ryzyka |
| Rate limit/MFA | UNVERIFIED: brak potwierdzonej aplikacyjnej konfiguracji rate limit/lockout/MFA w skanowanych auth/Program | Potwierdzić lub wdrożyć na warstwie aplikacji/proxy, zabezpieczyć recovery; nie deklarować skutecznej ochrony produkcji |
| Sesja i XSS | PARTIALLY IMPLEMENTED: JWT, SessionVersion, AuthProvider | Token w localStorage dostępny dla JS; walidacja sesji nie usuwa ryzyka XSS. Ocena modelu tokenu, CSP i zależności |
| Odzyskiwanie | PARTIALLY IMPLEMENTED: EmployeeAccountService, kod 15 minut i SmtpEmailSender | Dostawca, TLS poczty, ochrona nadużyć i rzeczywista dostarczalność do potwierdzenia |
| Autoryzacja | IMPLEMENTED: role manager/employee, kontrolery i reguły system manager | Macierz obiektowa i zakres uprawnień wymagają testu; role nie są samodzielną izolacją tenantów |
| Archiwizacja | PARTIALLY IMPLEMENTED: EmployeeRepository.SoftDeleteAsync, kontrole IsDeleted | Profil/history pozostają; brak pełnego automatycznego erasure wszystkich zależności |
| Potwierdzenia dokumentów | IMPLEMENTED: RegulationService, dokument immutable, acceptance snapshot; middleware 428 | Nie podpis kwalifikowany, nie dowód lektury; prawna retencja i exit wymagają procedury |
| Push | PARTIALLY IMPLEMENTED: EmployeePushController, PushOptions, EmployeeWebPushSender i skaner | Autoryzacja employee_id, HTTPS allowlist, limity i walidacja kluczy; endpointy/klucze w bazie, brak potwierdzenia szyfrowania at rest i TTL nieaktywności |
| Transport | UNVERIFIED: deploy/nginx i overlay HTTPS są konfiguracją do uruchomienia | Potwierdzić TLS/HSTS/proxy i dostępy rzeczywistej instancji |
| SQLite/kopie | PARTIALLY IMPLEMENTED: DatabaseAutoBackupHostedService i DatabaseWorkspace | Godzinny cykl i 10 kopii nie zapewniają niezależnego DR; niezweryfikowane off-site, szyfrowanie, alerty, restore |
| Izolacja instancji | PROPOSED: oddzielna instancja/baza/wolumen/sekrety dla niezależnego Klienta | Brak dowodu wielodzierżawnej izolacji w jednej SQLite; test odrębnej instancji jest STOP-SHIP |
| Logi | PARTIALLY IMPLEMENTED: WorkflowLogService i WorkflowLogsController | Logi zarządzalne/usuwalne nie są niezmiennym audytem bezpieczeństwa; ustalić retencję, dostęp, detekcję i bezpieczne eksporty |
| Eksport i wyjście | PARTIALLY IMPLEMENTED: ExportsController XLSX/SQL; PDF regulaminu do pobrania | Nie jest to pełny DSAR/eksport klienta. Własne eksporty i kopie wymagają zabezpieczenia; nie potwierdzono ogólnego eksportu grafików do PDF |
| Pamięć i media | PARTIALLY IMPLEMENTED: inwentarz storage, iframe/imageUrl | Brak potwierdzonego prior opt-in dla opcjonalnych operacji; wymaga remediacji i testu |
| Organizacja | PROPOSED: upoważnienia, poufność, przegląd dostępów, incydenty i testy | OWNER_REQUIRED: osoby odpowiedzialne; INFRASTRUCTURE_REQUIRED / PRODUCTION_TEST_REQUIRED: dowody wdrożenia |

Środki art. 32 uwzględniają stan wiedzy, koszty, charakter i ryzyko dla osób, w tym poufność, integralność, dostępność, odporność, zdolność odtworzenia i regularne testowanie. Umowny obowiązek wdrożenia nie stanowi dowodu wykonania. Nie potwierdzono ISO/SOC2, szyfrowania bazy w spoczynku ani pozytywnego testu odtworzenia produkcji.
