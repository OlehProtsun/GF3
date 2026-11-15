# Polityka bezpieczeństwa danych GF

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA C; incydenty; backup |
| Klasyfikacja | POUFNY WEWNĘTRZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

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

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 24–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)

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

## Web Push i PWA — zakres przetwarzania

Przetwarzanie obejmuje service worker /sw.js i subskrypcję danego urządzenia, endpoint, p256dh/auth, ID pracownika/subskrypcji, daty utworzenia i aktualizacji oraz rekordy doręczeń: ID powiadomienia, liczbę prób, termin kolejnej próby, dzierżawę zadania, wynik i datę finalizacji. Kody i endpointy traktuje się jako poufne identyfikatory i nie publikuje w dowodach audytu. Powiadomienia obejmują grafiki, dostępność, zamiany, wolne zmiany i przyjęcie zamiany; treść może ujawniać nazwę, okres, termin lub godziny pracy na ekranie blokady. Język PL/EN wynika z konta.

Włączenie jest działaniem pracownika i wymaga permission przeglądarki; jest ono odrębne od podstawy RODO i od oceny art. 399 PKE. Wyłączenie dla urządzenia podejmuje usunięcie subskrypcji serwera i unsubscribe w przeglądarce; błąd sieci może pozostawić rekord serwera. Cofnięcie samego permission nie gwarantuje usunięcia. Odpowiedź bramy 404/410 powoduje usunięcie subskrypcji; usunięcie rekordu subskrypcji usuwa zależne doręczenia. Brak potwierdzonego TTL nieaktywnej subskrypcji wymaga decyzji retencyjnej.

Skaner działa co około 30 sekund przy włączonej usłudze; pomija zdarzenia sprzed opt-in i odczytane. Przejściowe błędy są ponawiane do 8 prób; ostateczny wynik „sent” oznacza przyjęcie wysyłki przez usługę, nie odczyt na urządzeniu. Zakończone doręczenia są usuwane po 5 dniach od finalizacji podczas skanów; zadań nieukończonych i subskrypcji nie obejmuje ten warunek. TTL wysyłki jest ograniczony do godziny dla ofert zamian/wolnych zmian i do 5 dni dla pozostałych zdarzeń, zależnie od wieku zdarzenia.

Web Push szyfruje treść i używa HTTPS, ale dostawcy bram przeglądarki/systemu mogą otrzymywać metadane transmisji. Konfiguracja dopuszcza hosty FCM, Mozilla, Apple i Windows; nie jest to dowód zawartej umowy, konkretnego operatora prawnego, regionu lub transferu. Należy ustalić rolę każdego rzeczywistego dostawcy i podstawę rozdziału V przed produkcją. Brak gwarancji dostarczenia; nie używać jako jedynego kanału skutecznego zawiadomienia pracownika.
