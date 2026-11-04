# GF — raport harmonizacji dokumentacji prawnej

Wersja: 0.2.0-draft. Data przeglądu: 2026-10-19. **WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ**.

## 1. Zakres, punkt odniesienia i status

Repozytorium GF3, produkt GF, gałąź DEV2. Zbadany HEAD i wymagany baseline: **171aec08a34dd48e43f1eca8c23d104a4ee7b750**; na początku pracy drzewo było czyste. Badanie objęło cały istniejący pakiet docs/legal, docs/client-launch-package, docs/commercial-offer, siedem publicznych stron oraz generatory, walidator i testy legal-documents. Inwentarz plików zmienionych znajduje się w sekcji 9.

Przygotowano merytoryczny projekt do przeglądu: pełne klauzule pozostają tekstem proponowanych zobowiązań, a nie deklaracją ich wykonania. **REVIEW-READY w znaczeniu redakcyjnym i technicznym nie oznacza LEGALLY APPROVED ani PRODUCTION-READY.** Otwarte fakty, kwalifikacje i dowody wymieniono poniżej oraz w rejestrze pól. Nie potwierdzono kompletnej aktualności wszystkich zmian prawa na datę przeglądu z powodu ograniczenia odczytu części źródeł; nie nadano statusu zatwierdzenia prawnego.

Nie zmieniono kodu działania aplikacji, modeli, migracji, logowania, przepływów użytkownika ani infrastruktury. Nie wdrażano, nie przesyłano PDF do gate i nie publikowano dokumentów jako zatwierdzonych. Nie dopisano operatora, klientów, podpisów, umów dostawców, regionów, certyfikatów ani wyników testów produkcyjnych.

## 2. Klasyfikacja twierdzeń

| Etykieta | Znaczenie i zastosowanie w przeglądzie |
|---|---|
| VERIFIED IN CODE | Mechanizm odczytany w dokładnym badanym commicie; tabela dowodów poniżej. Nie dowodzi konfiguracji ani praktyki |
| VERIFIED IN DEPLOYMENT | Wymaga identyfikacji wdrożenia i aktualnego protokołu. **Nie nadano tej etykiety żadnemu twierdzeniu o produkcji**. Lokalne testy stron nie są testem produkcji |
| PROPOSED CONTRACTUAL TERM | Klauzule SaaS/DPA/regulaminu i procedur do przyjęcia przez uprawnione strony; m.in. cena, 14 dni uprzedzenia podprocesora i terminy exit |
| UNVERIFIED EXTERNAL FACT | Tożsamości, upoważnienia, podatki, rzeczywiści dostawcy/regiony, kontakty, praktyka klienta i skuteczność zabezpieczeń |
| REQUIRES LEGAL REVIEW | Podstawy przetwarzania, role według celu, zakres PKE, art. 22/DPIA, warunki umów i stosowanie regulacji sektorowych, z ograniczeniami źródeł |

Każde twierdzenie o wdrożeniu bez dowodu pozostaje UNVERIFIED EXTERNAL FACT lub PRODUCTION_TEST_REQUIRED. W tabelach TOM zastosowano IMPLEMENTED, PARTIALLY IMPLEMENTED, PROPOSED i UNVERIFIED; IMPLEMENTED odnosi się do kodu, nie do certyfikacji środowiska.

## 3. Dokumenty kanoniczne i relacje

| Źródło merytoryczne | Reprezentacja / dokument wspierający | Pierwszeństwo |
|---|---|---|
| [SaaS B2B](saas-agreement/umowa-saas-b2b-pl.md) i [zakres](saas-agreement/zalacznik-1-zakres-uslug.md) | HTML obok Markdown; oferta handlowa i zamówienie jako pomocnicze | Jeden negocjowany kontrakt; odrębna umowa firmy, nie checkbox pracownika |
| [Pełny regulamin](electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md) | HTML do wydruku i pełny FrontEnd/public/legal/regulamin.html | Zasady usługi; nie konkurencyjny skrót lub zmiana ceny/DPA |
| [DPA](../client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md) | Integralne załączniki A–E i prywatny HTML | Pierwszeństwo w ochronie danych, bez zniesienia art. 28 lub praw osób przez limit SaaS |
| [Prywatność](../client-launch-package/02-privacy/polityka-prywatnosci-pl.md) i [pamięć](../client-launch-package/03-cookies-and-storage/polityka-cookies-pl.md) | Prywatne HTML i publiczne /legal/polityka-prywatnosci.html, /legal/pliki-cookies.html | Ta sama treść merytoryczna; oddzielna klauzula pracodawcy |
| Publiczne index, zasady-korzystania, podwykonawcy, bezpieczenstwo | Bezpośrednio redagowane HTML | Informacje uzupełniające, bez poufnych rejestrów lub DPA w public assets |
| Role, mapa, retencja, rejestry, TOMs, ryzyko, procedury | Pakiety wewnętrzne i checklisty 04–10 | Dowody i organizacja; nie dowód wykonania zaplanowanych kontroli |
| Krótkie templates: SaaS/zamówienie, DPA, TOMs, klauzula i SLA | Oznaczone historyczne wzory pomocnicze z linkiem do źródła | Nie stosować równolegle jako odrębnych wzorców sprzecznych obowiązków |

Bezwzględnie obowiązujące prawo jest nadrzędne; dla danych DPA, dla pozostałych spraw indywidualny aneks, SaaS, uzgodniony zakres, regulamin i pomocnicze zasady. Oferta i protokół nie zastępują umocowania, podpisów ani recenzji.

## 4. Wymaganie → dokument → dowód oprogramowania

Poniższe odnośniki i symbole odnoszą się do baseline, nie do konfiguracji serwera. Numery linii są punktami wejścia do mechanizmu.

| Wymaganie i poprawiony fakt | Dokument / kwalifikacja | Dokładny plik i punkt wejścia |
|---|---|---|
| Art. 5/6/12–14: cele, źródła, role, prawa, dane obowiązkowe | Privacy, obie klauzule, analiza ról, mapa; REQUIRES LEGAL REVIEW dla podstaw | DataAccessLayer/Models/EmployeeModel.cs:32–38 (IsDeleted, Phone, Email); EmployeeAccountModel.cs:17,34,49 (Language, hash/reset). Stan modelu VERIFIED IN CODE |
| Konto i odtwarzanie sesji, JWT w localStorage | Storage, privacy, DPA C, ryzyko | FrontEnd/src/app/providers/AuthProvider.tsx:24–48, AUTH_TOKEN_STORAGE_KEY i persistToken; logout:162; API sesji w GF3.WebApi/Controllers/AuthController.cs i GF3.WebApi/Auth/JwtAuthenticationHandler.cs |
| Krótkie numeryczne hasło i hash | TOMs/security; PARTIALLY IMPLEMENTED | BusinessLogicLayer/Security/NumericPasswordPolicy.cs:8, Length=6; PasswordHasher.cs:12–21, 100000 iteracji PBKDF2-SHA256 i sól. Brak pozytywnego dowodu rate limit/MFA w badanych auth/Program nie dowodzi ich braku na proxy |
| Reset kodu i zewnętrzna poczta | DPA B/D, mapa, retencja | BusinessLogicLayer/Services/EmployeeAccountService.cs:18, PasswordResetCodeLifetime=15 minut; GF3.WebApi/Infrastructure/SmtpEmailSender.cs:21–55, SendAsync i konfigurowalne EnableSsl. Dostawca/region/dostarczalność UNVERIFIED |
| Generator i realna kontrola człowieka | SaaS/zakres/regulamin/privacy; ocena art. 22 i AI Act | BusinessLogicLayer/Generators/ScheduleGenerator.cs:90–160, AttemptScore.IsBetterThan i parametry; GF3.WebApi/Controllers/ContainersController.cs:271–326, GenerateGraphPreview/GenerateGraph, DryRun/Overwrite; publikacja:90–124,201,227. Kod umożliwia kontrolę, nie dowodzi rzeczywistej praktyki Klienta |
| Dostępność i publikacja | DPA A/B i role | GF3.WebApi/Controllers/EmployeeAvailabilityController.cs, AvailabilityGroupsController.cs; GF3.WebApi/Services/EmployeePushNotificationSource.cs:41–66, status public, członkostwo, VisibleFrom/To i deadline |
| Korekty, zamiany i historie | DPA A/B/E, mapa, retencja, zakres | DataAccessLayer/Models/ShiftCorrectionRequestModel.cs:11; ShiftSwapRequestModel.cs:12; GF3.WebApi/Controllers/ShiftCorrectionsController.cs, EmployeeShiftSwapsController.cs, GraphVersionsController.cs:295; migracje AddShiftCorrectionRequests, PreserveCompletedSwapHistory, AddReciprocalShiftSwaps. Nie są nieistniejącą ogólną funkcją korekt |
| Soft-delete nie wykonuje erasure | Privacy, DPA E, DSAR, exit, retencja | DataAccessLayer/Repositories/EmployeeRepository.cs:20–41, SoftDeleteAsync ustawia IsDeleted i usuwa konto, zachowuje profil; BusinessLogicLayer/Services/EmployeeService.cs:53–55. Relacje/historia nie są czyszczone przez tę funkcję |
| Art. 28 zakres powierzenia, podprocesorzy i zakończenie | DPA §§ 2–16 i A–E, pełne klauzule; PROPOSED CONTRACTUAL TERM | Modele danych, mechanizmy eksportu/kopii/usunięcia w tej tabeli określają wykonalność. Kod nie dowodzi podpisania DPA, upoważnienia dostawców lub realizacji audytu |
| Istniejący service worker i urządzenie | Cookies/storage, privacy, push w DPA/TOMs | FrontEnd/src/entities/employee-push/lib/pushSubscription.ts:27, registerWorker, /sw.js scope /; FrontEnd/public/sw.js:9, push oraz notificationclick. Brak fetch handlera/Cache API w tym pliku; nie deklarować całego trybu offline |
| Jawne włączenie push i opt-out | Privacy, cookies, DPA A/B/E; VERIFIED IN CODE | FrontEnd/src/entities/employee-push/lib/enablePush.ts:5–18, requestPermission w działaniu użytkownika; pushSubscription.ts:49, ensureSubscription i :72, disableForDevice. Próba DELETE może nie dojść offline; unsubscribe i permission to odrębne operacje |
| Endpoint, klucze i powiązanie odbiorcy | DPA B/C, security i role | GF3.WebApi/Controllers/EmployeePushController.cs:15–72, employee_id, upsert i DELETE endpointu właściciela; GF3.WebApi/Options/PushOptions.cs:13–29, allowlist HTTPS, walidacja adresu/kluczy; nie dowód dostawcy lub umowy |
| Retencja subskrypcji i doręczeń | Retencja, DPA E, privacy i backup | DataAccessLayer/Models/EmployeePushSubscriptionModel.cs:3, EmployeePushDeliveryModel.cs:3; Models/DataBaseContext/AppDbContext.cs:119–151, cascade subskrypcji/delivery. Soft-delete nie usuwa fizycznie Employee, więc nie należy zakładać automatycznej kaskady całej bazy |
| Retry, czas, wynik wysyłki | Privacy i DPA A, retencja | GF3.WebApi/Infrastructure/EmployeeWebPushHostedService.cs:18, ScanAsync:30, cutoff:38, statusy i retry w ScanAsync; skan około 30 sekund, do 8 prób, finalizacje starsze niż 5 dni. Brak TTL endpointów i nieukończonych zadań pozostaje luką |
| Treść, PL/EN i ekran blokady | Publiczne polityki, regulamin, DPIA | GF3.WebApi/Services/EmployeePushMessages.cs:5–55, Format; EmployeePushNotificationSource.cs:29, język konta i wybór kandydatów. Nazwy/okres/terminy/godziny mogą być widoczne bez otwarcia aplikacji |
| Szyfrowanie transmisji nie usuwa metadanych | Privacy, podwykonawcy, DPA D i screening transferów | GF3.WebApi/Services/EmployeeWebPushSender.cs:12–36, WebPushClient.SendNotificationAsync; limit payload 3072 i TTL oferty 3600/pozostałe do 432000 sekund. Usługa dostawcy może obsługiwać metadane; at rest/region/SCC niepotwierdzone |
| Pamięć zaproszenia i konta push | Pełny inwentarz z oceną PKE per wiersz | pushSubscription.ts:3, gf3.push.employeeId; EmployeePushInvitation.tsx:11–18, cooldown 7 dni i gf3.push.invitation.{id}; upływ czasu nie usuwa fizycznie klucza |
| Pozostałe klucze UI | Wykaz storage z czasem, usunięciem, dostawcą i koniecznością | LoginPage.tsx:14–30; shared/lib/employeeNotificationReadState.ts:3,88–154; employee-availability/ui/EmployeeAvailabilityPage.tsx:44,389,494; employee-schedule/ui/EmployeeSchedulePage.tsx:571–596; shared/lib/records/usePinnedRecords.ts:23–31 |
| 5 dni odczytów, a nie stare 7/maksimum 300 jako reguła całości | Retencja i polityka cookies | GF3.WebApi/Services/EmployeeNotificationRetention.cs:8–15; FrontEnd/src/shared/lib/employeeNotificationReadState.ts:3. Stary format przeglądarki może pozostać |
| Mediów nie blokuje sam baner | Cookies, rekomendacja, ryzyko i STOP-SHIP | FrontEnd/src/features/manager-system-news/ui/ManagerSystemNews.tsx:32–51; pages/employee-notifications/ui/EmployeeNotificationsPage.tsx:139,248, iframe youtube-nocookie i zewnętrzny imageUrl. Brak potwierdzonego prior opt-in; nie dodano kosmetycznego bannera |
| PDF, wersja, hash i dowód potwierdzenia | Pełny regulamin, privacy, DPA, checklisty | DataAccessLayer/Models/RegulationDocumentModel.cs:8–65; RegulationAcceptanceModel.cs:8–45; BusinessLogicLayer/Services/RegulationService.cs:73–150, zakaz zmiany/usunięcia opublikowanego PDF, snapshot; :171–184, SHA-256 |
| Historia i API 428, granica oświadczenia | Regulamin/README/launch; VERIFIED IN CODE | GF3.WebApi/Controllers/RegulationsController.cs:19–58, history/me i history/employees; Middleware/RegulationAcceptanceGuardMiddleware.cs:12–64, warunki/wyjątki; FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx. Nie podpis kwalifikowany, nie dowód przeczytania ani zawarcie umowy firmy |
| Logi nie są niezmiennym audytem wszystkich dostępów | Rejestry, TOMs, incident, retencja | DataAccessLayer/Models/WorkflowLogEntryModel.cs:10; GF3.WebApi/Services/WorkflowLogService.cs i Controllers/WorkflowLogsController.cs. Nie przypisano niezweryfikowanych IP/UA/alertów |
| Eksporty i PDF — zakres | DPA E, DSAR, exit i zakres | GF3.WebApi/Controllers/ExportsController.cs:30–69, wybrane XLSX/SQL; RegulationsController.cs:36–40, pobranie wgranego PDF. Brak podstawy do obietnicy pełnego eksportu klienta lub ogólnego generatora PDF grafików |
| Kopie lokalne i brak dowodu DR | DPA C/E, security, backup/BCP | GF3.WebApi/Infrastructure/DatabaseAutoBackupHostedService.cs:10,25–44, godzinny timer podczas pracy; DataAccessLayer/Administration/SqliteDatabaseWorkspace.cs:55–57,115–118, folder backups i 10 kopii. Brak dowodu off-site, szyfrowania i restore produkcji |
| Izolacja/TLS/cache | Publiczne bezpieczeństwo, TOMs, STOP-SHIP | deploy/docker-compose.yml:17–31 i końcowa definicja wolumenu; deploy/docker-compose.https.yml i deploy/nginx; GF3.WebApi/Infrastructure/StartupConfiguration.cs:14–20, no-store index/version, immutable assets i rewalidacja pozostałych. Wariant konfiguracji nie dowodzi aktywnego TLS lub separacji |
| UŚUDE 5/6/8 i handlowe warunki | Pełny regulamin §§ 1–26, SaaS i zakres, zagrożenia | Zachowano login/profile/gate i publiczne ścieżki; regulamin obejmuje wymagania, treści, zawarcie/rozwiązanie, reklamacje i ryzyka. Tożsamość, kontakt, daty i umocowanie są zewnętrzne |
| Art. 30, 32–36 i 44–49 | Wypełnione rejestry celów, macierze, procedury, DPA C–E, screening DPIA i źródła | Kod daje mechanizmy i luki opisane powyżej; nie potwierdza formalnej ewidencji klientów, ćwiczeń, TIA, SCC lub zatwierdzonego DPIA |

Nie przeglądano prywatnych plików .env, nie kopiowano endpointów użytkowników, tokenów, danych klientów lub sekretów do repozytorium.

## 5. Korekty i harmonizacja

Usunięto aktywne twierdzenia o nieistnieniu service worker, wersji i historii akceptacji, planowanej dopiero implementacji gate oraz nieistnieniu wdrożonych korekt. Opisano archiwizację zamiast twardego usunięcia pracownika, rzeczywisty zakres eksportów i ograniczenia retencji. Oddzielono TTL rekordu, logiczną widoczność, ważność JWT i czas życia przesyłki push. Nie utożsamiono przyjęcia wysyłki przez bramę z doręczeniem lub odczytem.

Uzupełniono pełne klauzule RODO, źródła, odbiorców, prawa, kryteria retencji, wolny tekst i art. 9, role dla własnego kontaktu/bezpieczeństwa i powierzenia. Zgoda przeglądarki, podstawa RODO, zgoda z art. 399 i osobiste potwierdzenie regulaminu są rozdzielone.

DPA obejmuje obowiązki art. 28 ust. 3, aktywne uprzednie zawiadomienie o podprocesorze, uzasadniony sprzeciw, odpowiedzialność z ust. 4, audyt niewykluczany organizacją lub kosztami, pomoc w art. 32–36, sygnalizowanie niezgodnej instrukcji i wybór zwrotu/usunięcia. Pilna zmiana nie legalizuje automatycznie dostępu bez uprzedniej autoryzacji. Spór o płatność nie pozwala zatrzymać całej bazy powierzonych pracowników.

Cena 500 PLN i terminy projektu są wyraźnie proponowane. Forma działalności nie wynika z historii repozytorium. Ujęto kwartalny limit 2026, odrębny limit przejściowy KSeF i potrzebę kwalifikacji księgowej. Screening AI Act, DSA, EAA/PAD, KSC i art. 22 nie przypisuje automatycznej zgodności ani wyłączenia. Ochrony niezawodowej umowy JDG nie wyłącza samo B2B.

Generator klienta dawniej odtwarzał kilkadziesiąt Markdown ze starych literałów. Teraz czyta istniejące źródła, generuje wyłącznie cztery prywatne i trzy publiczne HTML oraz rejestr; --check wykrywa nieaktualne pochodne bez zapisu. Nie nadpisuje merytorycznych dokumentów lub faktur. Oryginalny walidator release i jego wymagania zatwierdzenia pozostają bez osłabienia.

## 6. Otwarte fakty i blokery

| Klasa | Informacja / blokada | Odpowiedzialny i sposób zamknięcia |
|---|---|---|
| OWNER_REQUIRED | Prawdziwa tożsamość, forma prawna, adres, identyfikatory, kontakty i IOD jeżeli powołany; zdolność wsparcia, ceny/terminy, data obowiązywania | Właściciel: dokument rejestracyjny lub kwalifikacja statusu, decyzje i działający kontakt; nie profil GitHub |
| CLIENT_REQUIRED | Tożsamość/umocowanie, instrukcje, podstawy zatrudnienia, retencja, doręczenie informacji, praktyka oceny generatora i podpisy | Klient: dokumenty, karta wdrożenia i kontrolowane dowody poza Git |
| ACCOUNTANT_REQUIRED | Dopuszczalność działalności nierejestrowanej, całe przychody, VAT/NIP/PIT/ZUS, fakturowanie i KSeF | Księgowy: ocena całej działalności i przepisów 2026/2027; wzór faktury nie jest dowodem |
| LAWYER_REQUIRED | Aktualność ogłoszonego prawa, klauzule i odpowiedzialność, prawa JDG, art. 6/9/22/28/DPIA, AI Act i terminy zmian, DSA/EAA/KSC/prawo pracy | Prawnik/specjalista: rzeczywista opinia na ustalonych faktach i wersjach dokumentów. Dostęp EUR-Lex nie pozwolił potwierdzić pełnej aktualności |
| INFRASTRUCTURE_REQUIRED | Faktyczni dostawcy hostingu/SMTP/kopii/wsparcia/bram push, role/regiony, umowy, transfery, TLS/at rest/izolacja i maksymalna retencja endpointów/kopii | Operator infrastruktury: umowy, panele i konfiguracja z chronionym dowodem, bez ujawnienia sekretów |
| PRODUCTION_TEST_REQUIRED | Ochrona sześciocyfrowych kont, XSS/token, kontrola ról/izolacji, restore, bezpieczny eksport, erasure historii/PDF, cookies i media, push offline/zmiana konta | Właściciel techniczny: testy syntetyczne rzeczywistej instancji i protokół. Lokalne testy dokumentów nie zamykają tych warunków |

Szczególnie istotne STOP-SHIP: opcjonalna pamięć/media bez potwierdzonego wyjątku lub blokującej zgody; krótkie hasła uprzywilejowane i brak dowodu ochrony przed zgadywaniem; token localStorage/XSS; nieznani dostawcy i transfery; lokalne kopie bez testu DR; brak pełnego kontrolowanego procesu usunięcia historycznych danych. Dokumentowanie luki nie jest jej usunięciem.

[Rejestr pól](../client-launch-package/00-master-index/rejestr-placeholderow.md) wskazuje każde odnalezione wystąpienie wraz z plikiem, linią, sekcją, brakującym faktem, powodem, klasą, odpowiedzialnym, metodą i wpływem. Puste formularze przyszłych spraw i historyczne wzory nie tworzą dodatkowych zobowiązań. Wszystkie pola pozostają otwarte do rzeczywistego potwierdzenia; nie wypełniono podpisów lub wyników fikcyjnymi danymi.

## 7. Prawo i granice weryfikacji

Pełne odnośniki, konkretne przepisy, daty i screening: [LEGAL_SOURCES_2026-10-19.md](LEGAL_SOURCES_2026-10-19.md). Odczytano ELI UŚUDE/PKE, oficjalne materiały EROD, UODO, MF/KSeF, działalności nierejestrowanej i KSC oraz materiały Komisji dotyczące AI Act. EUR-Lex zwracał blokadę wymagającą JavaScript także dla próby PDF skonsolidowanego AI Act; nie udawano odczytu pełnych aktualnych aktów. Informacja Komisji o AI Omnibus wymaga zestawienia z ogłoszonym aktem i datą stosowania. Pełny przegląd nowelizacji RODO/AI Act/DSA/KC/prawa pracy pozostaje do prawnika.

## 8. Walidacja i ograniczenia

Wyniki końcowe:

| Kontrola | Wynik |
|---|---|
| node scripts/validate-legal-documents.mjs | PASS — 7 stron |
| Generatory electronic-services-regulations i saas-agreement | PASS — HTML i inwentarze/hash źródeł, bez zatwierdzenia prawnego |
| node docs/client-launch-package/generate-package.mjs | PASS — 4 prywatne i 3 publiczne pochodne HTML oraz rejestr otwartych pól; sprawdzone SHA-256 97 źródeł Markdown nie zmieniły się podczas generacji |
| Ten sam generator --check | PASS — zgodność bajtowa pochodnych i rejestru, bez zapisu |
| node scripts/check-legal-package.mjs | PASS — 115 plików MD/HTML i 395 lokalnych linków/fragmentów; brak sprawdzanych przestarzałych twierdzeń i prywatnych odnośników w głównych publicznych politykach |
| Playwright e2e/legal-documents.spec.ts | PASS — 16/16, Chromium, Mobile Chrome, WebKit laptop/iPhone; po ostatniej zmianie renderowania dodatkowo 4/4 testy całego zestawu stron |
| ESLint dla zmienionego legal-documents.spec.ts | PASS |
| node --check generatora i nowego kontrolera pakietu | PASS |
| git diff --check; kontrola zakresu | PASS — bez zmian kodu aplikacji, modeli, migracji, wdrożenia lub walidatora release; bez pozostawionych usunięć wcześniejszych wyników testów |
| node scripts/validate-legal-documents.mjs --release | FAIL oczekiwany — 7 stron DRAFT i 7 brakujących autentycznych zatwierdzeń/wersji/hash; walidator niezmieniony |

Testy uruchamiane w ograniczonej piaskownicy Windows napotkały spawn EPERM; dopuszczony lokalny tryb pozwolił uruchomić Vite i Playwright. Nie jest to wada dokumentów ani dowód testu produkcji. Wynik testów podaje stan faktycznie wykonany, nie zakładany.

Pierwszy przebieg legal-documents ujawnił limit wymiaru zrzutu WebKit dla pełnego długiego regulaminu, fokus klawiatury WebKit po emulacji druku oraz historyczne oczekiwanie stopki prawnej na /swap. Test zaktualizowano do istniejącej strony /profile, rozmiaru CSS/fragmentów dla długich zrzutów oraz jawnego fokusu w WebKit; nie zmieniono UI, tras ani polityki widoczności stopki. Ponowny pełny przebieg oraz końcowa kontrola renderowania przeszły. Obejrzano zrzuty fragmentów regulaminu i polityki prywatności w szerokości 375 px; tekst, odstępy i oznaczenie DRAFT są czytelne. Numeracja zagnieżdżonych punktów jest zachowana przez generator.

Nie testowano fizycznych urządzeń, zewnętrznej domeny, faktycznego dostawcy push/SMTP, podpisów, księgowości, powierzenia ani produkcyjnego restore/usunięcia. Te ograniczenia pozostają niezależne od wyniku testów HTML.

## 9. Zmienione pliki

Pełny wykaz z końcowego git diff, wraz z nowymi raportami i kontrolerem pakietu, znajduje się poniżej. Tymczasowe skrypty i lokalne wyniki testów nie stanowią zmian dostarczanego pakietu. Odzyskano wcześniejsze śledzone artefakty testowe automatycznie czyszczone przez Playwright; nie pozostawiono ich usunięć.


Liczba zmienionych/dodanych plików: 116.

- [FrontEnd/e2e/legal-documents.spec.ts](../../FrontEnd/e2e/legal-documents.spec.ts)
- [FrontEnd/public/legal/bezpieczenstwo.html](../../FrontEnd/public/legal/bezpieczenstwo.html)
- [FrontEnd/public/legal/index.html](../../FrontEnd/public/legal/index.html)
- [FrontEnd/public/legal/legal.css](../../FrontEnd/public/legal/legal.css)
- [FrontEnd/public/legal/pliki-cookies.html](../../FrontEnd/public/legal/pliki-cookies.html)
- [FrontEnd/public/legal/podwykonawcy.html](../../FrontEnd/public/legal/podwykonawcy.html)
- [FrontEnd/public/legal/polityka-prywatnosci.html](../../FrontEnd/public/legal/polityka-prywatnosci.html)
- [FrontEnd/public/legal/regulamin.html](../../FrontEnd/public/legal/regulamin.html)
- [FrontEnd/public/legal/zasady-korzystania.html](../../FrontEnd/public/legal/zasady-korzystania.html)
- [docs/client-launch-package/00-master-index/README.md](../client-launch-package/00-master-index/README.md)
- [docs/client-launch-package/00-master-index/changelog.md](../client-launch-package/00-master-index/changelog.md)
- [docs/client-launch-package/00-master-index/indeks-dokumentow.md](../client-launch-package/00-master-index/indeks-dokumentow.md)
- [docs/client-launch-package/00-master-index/macierz-spojnosci-dokumentow.md](../client-launch-package/00-master-index/macierz-spojnosci-dokumentow.md)
- [docs/client-launch-package/00-master-index/raport-brakow-i-ryzyk.md](../client-launch-package/00-master-index/raport-brakow-i-ryzyk.md)
- [docs/client-launch-package/00-master-index/rejestr-placeholderow.md](../client-launch-package/00-master-index/rejestr-placeholderow.md)
- [docs/client-launch-package/01-data-processing-agreement/README.md](../client-launch-package/01-data-processing-agreement/README.md)
- [docs/client-launch-package/01-data-processing-agreement/checklista-dpa.md](../client-launch-package/01-data-processing-agreement/checklista-dpa.md)
- [docs/client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.html](../client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.html)
- [docs/client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md](../client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md)
- [docs/client-launch-package/01-data-processing-agreement/zalacznik-a-opis-przetwarzania.md](../client-launch-package/01-data-processing-agreement/zalacznik-a-opis-przetwarzania.md)
- [docs/client-launch-package/01-data-processing-agreement/zalacznik-b-kategorie-danych-i-osob.md](../client-launch-package/01-data-processing-agreement/zalacznik-b-kategorie-danych-i-osob.md)
- [docs/client-launch-package/01-data-processing-agreement/zalacznik-c-srodki-techniczne-i-organizacyjne.md](../client-launch-package/01-data-processing-agreement/zalacznik-c-srodki-techniczne-i-organizacyjne.md)
- [docs/client-launch-package/01-data-processing-agreement/zalacznik-d-podmioty-podprzetwarzajace.md](../client-launch-package/01-data-processing-agreement/zalacznik-d-podmioty-podprzetwarzajace.md)
- [docs/client-launch-package/01-data-processing-agreement/zalacznik-e-procedura-usuniecia-lub-zwrotu-danych.md](../client-launch-package/01-data-processing-agreement/zalacznik-e-procedura-usuniecia-lub-zwrotu-danych.md)
- [docs/client-launch-package/02-privacy/README.md](../client-launch-package/02-privacy/README.md)
- [docs/client-launch-package/02-privacy/analiza-rol-rodo.md](../client-launch-package/02-privacy/analiza-rol-rodo.md)
- [docs/client-launch-package/02-privacy/klauzula-informacyjna-kontakty-b2b-pl.md](../client-launch-package/02-privacy/klauzula-informacyjna-kontakty-b2b-pl.md)
- [docs/client-launch-package/02-privacy/klauzula-informacyjna-uzytkownicy-platformy-pl.md](../client-launch-package/02-privacy/klauzula-informacyjna-uzytkownicy-platformy-pl.md)
- [docs/client-launch-package/02-privacy/mapa-danych-osobowych.md](../client-launch-package/02-privacy/mapa-danych-osobowych.md)
- [docs/client-launch-package/02-privacy/okresy-retencji-danych.md](../client-launch-package/02-privacy/okresy-retencji-danych.md)
- [docs/client-launch-package/02-privacy/polityka-prywatnosci-pl.html](../client-launch-package/02-privacy/polityka-prywatnosci-pl.html)
- [docs/client-launch-package/02-privacy/polityka-prywatnosci-pl.md](../client-launch-package/02-privacy/polityka-prywatnosci-pl.md)
- [docs/client-launch-package/02-privacy/rejestr-kategorii-czynnosci-przetwarzania.md](../client-launch-package/02-privacy/rejestr-kategorii-czynnosci-przetwarzania.md)
- [docs/client-launch-package/03-cookies-and-storage/README.md](../client-launch-package/03-cookies-and-storage/README.md)
- [docs/client-launch-package/03-cookies-and-storage/audyt-cookies-i-storage.md](../client-launch-package/03-cookies-and-storage/audyt-cookies-i-storage.md)
- [docs/client-launch-package/03-cookies-and-storage/polityka-cookies-pl.html](../client-launch-package/03-cookies-and-storage/polityka-cookies-pl.html)
- [docs/client-launch-package/03-cookies-and-storage/polityka-cookies-pl.md](../client-launch-package/03-cookies-and-storage/polityka-cookies-pl.md)
- [docs/client-launch-package/03-cookies-and-storage/rekomendacja-mechanizmu-zgody.md](../client-launch-package/03-cookies-and-storage/rekomendacja-mechanizmu-zgody.md)
- [docs/client-launch-package/03-cookies-and-storage/wykaz-cookies-i-storage.md](../client-launch-package/03-cookies-and-storage/wykaz-cookies-i-storage.md)
- [docs/client-launch-package/04-security-and-incidents/README.md](../client-launch-package/04-security-and-incidents/README.md)
- [docs/client-launch-package/04-security-and-incidents/analiza-ryzyka-template.md](../client-launch-package/04-security-and-incidents/analiza-ryzyka-template.md)
- [docs/client-launch-package/04-security-and-incidents/checklista-72-godziny.md](../client-launch-package/04-security-and-incidents/checklista-72-godziny.md)
- [docs/client-launch-package/04-security-and-incidents/formularz-zgloszenia-incydentu.md](../client-launch-package/04-security-and-incidents/formularz-zgloszenia-incydentu.md)
- [docs/client-launch-package/04-security-and-incidents/polityka-bezpieczenstwa-danych.md](../client-launch-package/04-security-and-incidents/polityka-bezpieczenstwa-danych.md)
- [docs/client-launch-package/04-security-and-incidents/procedura-naruszenia-ochrony-danych.md](../client-launch-package/04-security-and-incidents/procedura-naruszenia-ochrony-danych.md)
- [docs/client-launch-package/04-security-and-incidents/procedura-reagowania-na-incydenty.md](../client-launch-package/04-security-and-incidents/procedura-reagowania-na-incydenty.md)
- [docs/client-launch-package/05-backup-and-continuity/README.md](../client-launch-package/05-backup-and-continuity/README.md)
- [docs/client-launch-package/05-backup-and-continuity/checklista-testu-odtworzeniowego.md](../client-launch-package/05-backup-and-continuity/checklista-testu-odtworzeniowego.md)
- [docs/client-launch-package/05-backup-and-continuity/plan-ciaglosci-dzialania-light.md](../client-launch-package/05-backup-and-continuity/plan-ciaglosci-dzialania-light.md)
- [docs/client-launch-package/05-backup-and-continuity/procedura-kopii-zapasowych.md](../client-launch-package/05-backup-and-continuity/procedura-kopii-zapasowych.md)
- [docs/client-launch-package/05-backup-and-continuity/procedura-odtwarzania-danych.md](../client-launch-package/05-backup-and-continuity/procedura-odtwarzania-danych.md)
- [docs/client-launch-package/06-client-launch/README.md](../client-launch-package/06-client-launch/README.md)
- [docs/client-launch-package/06-client-launch/checklista-onboardingu-klienta.md](../client-launch-package/06-client-launch/checklista-onboardingu-klienta.md)
- [docs/client-launch-package/06-client-launch/checklista-onboardingu-uzytkownikow.md](../client-launch-package/06-client-launch/checklista-onboardingu-uzytkownikow.md)
- [docs/client-launch-package/06-client-launch/formularz-danych-wdrozeniowych.md](../client-launch-package/06-client-launch/formularz-danych-wdrozeniowych.md)
- [docs/client-launch-package/06-client-launch/formularz-osob-kontaktowych.md](../client-launch-package/06-client-launch/formularz-osob-kontaktowych.md)
- [docs/client-launch-package/06-client-launch/potwierdzenie-akceptacji-dokumentow.md](../client-launch-package/06-client-launch/potwierdzenie-akceptacji-dokumentow.md)
- [docs/client-launch-package/06-client-launch/protokol-przekazania-dostepu-administratora.md](../client-launch-package/06-client-launch/protokol-przekazania-dostepu-administratora.md)
- [docs/client-launch-package/06-client-launch/protokol-uruchomienia-platformy.html](../client-launch-package/06-client-launch/protokol-uruchomienia-platformy.html)
- [docs/client-launch-package/06-client-launch/protokol-uruchomienia-platformy.md](../client-launch-package/06-client-launch/protokol-uruchomienia-platformy.md)
- [docs/client-launch-package/07-support/README.md](../client-launch-package/07-support/README.md)
- [docs/client-launch-package/07-support/formularz-zgloszenia-bledu.md](../client-launch-package/07-support/formularz-zgloszenia-bledu.md)
- [docs/client-launch-package/07-support/formularz-zgloszenia-zmiany.md](../client-launch-package/07-support/formularz-zgloszenia-zmiany.md)
- [docs/client-launch-package/07-support/instrukcja-zglaszania-problemow-dla-klienta.md](../client-launch-package/07-support/instrukcja-zglaszania-problemow-dla-klienta.md)
- [docs/client-launch-package/07-support/klasyfikacja-zgloszen.md](../client-launch-package/07-support/klasyfikacja-zgloszen.md)
- [docs/client-launch-package/07-support/zasady-wsparcia-technicznego.md](../client-launch-package/07-support/zasady-wsparcia-technicznego.md)
- [docs/client-launch-package/08-billing-and-records/README.md](../client-launch-package/08-billing-and-records/README.md)
- [docs/client-launch-package/08-billing-and-records/checklista-rozliczen-miesiecznych.md](../client-launch-package/08-billing-and-records/checklista-rozliczen-miesiecznych.md)
- [docs/client-launch-package/08-billing-and-records/instrukcja-wystawiania-dokumentu-sprzedazy.md](../client-launch-package/08-billing-and-records/instrukcja-wystawiania-dokumentu-sprzedazy.md)
- [docs/client-launch-package/08-billing-and-records/warianty-rozliczenia-do-konsultacji.md](../client-launch-package/08-billing-and-records/warianty-rozliczenia-do-konsultacji.md)
- [docs/client-launch-package/08-billing-and-records/zasady-numeracji-dokumentow.md](../client-launch-package/08-billing-and-records/zasady-numeracji-dokumentow.md)
- [docs/client-launch-package/09-data-exit/README.md](../client-launch-package/09-data-exit/README.md)
- [docs/client-launch-package/09-data-exit/checklista-dezaktywacji-klienta.md](../client-launch-package/09-data-exit/checklista-dezaktywacji-klienta.md)
- [docs/client-launch-package/09-data-exit/procedura-zakonczenia-wspolpracy.md](../client-launch-package/09-data-exit/procedura-zakonczenia-wspolpracy.md)
- [docs/client-launch-package/09-data-exit/protokol-eksportu-danych.md](../client-launch-package/09-data-exit/protokol-eksportu-danych.md)
- [docs/client-launch-package/09-data-exit/protokol-usuniecia-danych.md](../client-launch-package/09-data-exit/protokol-usuniecia-danych.md)
- [docs/client-launch-package/10-final-review/checklista-gotowosci-do-podpisania.md](../client-launch-package/10-final-review/checklista-gotowosci-do-podpisania.md)
- [docs/client-launch-package/10-final-review/checklista-gotowosci-do-produkcji.md](../client-launch-package/10-final-review/checklista-gotowosci-do-produkcji.md)
- [docs/client-launch-package/10-final-review/finalny-raport-gotowosci.md](../client-launch-package/10-final-review/finalny-raport-gotowosci.md)
- [docs/client-launch-package/10-final-review/lista-decyzji-wlasciciela-saas.md](../client-launch-package/10-final-review/lista-decyzji-wlasciciela-saas.md)
- [docs/client-launch-package/10-final-review/pytania-do-klienta.md](../client-launch-package/10-final-review/pytania-do-klienta.md)
- [docs/client-launch-package/10-final-review/pytania-do-ksiegowego.md](../client-launch-package/10-final-review/pytania-do-ksiegowego.md)
- [docs/client-launch-package/10-final-review/pytania-do-prawnika.md](../client-launch-package/10-final-review/pytania-do-prawnika.md)
- [docs/client-launch-package/generate-package.mjs](../client-launch-package/generate-package.mjs)
- [docs/commercial-offer/README.md](../commercial-offer/README.md)
- [docs/commercial-offer/oferta-handlowa-pl.html](../commercial-offer/oferta-handlowa-pl.html)
- [docs/legal/COOKIE_AUDIT.md](COOKIE_AUDIT.md)
- [docs/legal/LEGAL_COMPLETION_REPORT_2026-10-19.md](LEGAL_COMPLETION_REPORT_2026-10-19.md)
- [docs/legal/LEGAL_REVIEW.md](LEGAL_REVIEW.md)
- [docs/legal/LEGAL_SOURCES_2026-10-19.md](LEGAL_SOURCES_2026-10-19.md)
- [docs/legal/OPERATOR_INPUTS.md](OPERATOR_INPUTS.md)
- [docs/legal/OPERATOR_ONBOARDING_PLAYBOOK_UA.md](OPERATOR_ONBOARDING_PLAYBOOK_UA.md)
- [docs/legal/README.md](README.md)
- [docs/legal/electronic-services-regulations/README.md](electronic-services-regulations/README.md)
- [docs/legal/electronic-services-regulations/checklista-wdrozenia-regulaminu.md](electronic-services-regulations/checklista-wdrozenia-regulaminu.md)
- [docs/legal/electronic-services-regulations/informacja-o-zagrozeniach-pl.md](electronic-services-regulations/informacja-o-zagrozeniach-pl.md)
- [docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.html](electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.html)
- [docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md](electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md)
- [docs/legal/internal/CHECKLIST_PRZED_STARTEM.md](internal/CHECKLIST_PRZED_STARTEM.md)
- [docs/legal/internal/Ocena_Ryzyka_i_DPIA_Screening.md](internal/Ocena_Ryzyka_i_DPIA_Screening.md)
- [docs/legal/internal/Procedura_DSAR_Retencji_Usuwania.md](internal/Procedura_DSAR_Retencji_Usuwania.md)
- [docs/legal/internal/Procedura_Incydentow_i_Naruszen.md](internal/Procedura_Incydentow_i_Naruszen.md)
- [docs/legal/internal/Rejestr_Czynnosci_Przetwarzania.md](internal/Rejestr_Czynnosci_Przetwarzania.md)
- [docs/legal/internal/Rejestr_Podwykonawcow_i_Transferow.md](internal/Rejestr_Podwykonawcow_i_Transferow.md)
- [docs/legal/saas-agreement/README.md](saas-agreement/README.md)
- [docs/legal/saas-agreement/umowa-saas-b2b-pl.html](saas-agreement/umowa-saas-b2b-pl.html)
- [docs/legal/saas-agreement/umowa-saas-b2b-pl.md](saas-agreement/umowa-saas-b2b-pl.md)
- [docs/legal/saas-agreement/zalacznik-1-zakres-uslug.md](saas-agreement/zalacznik-1-zakres-uslug.md)
- [docs/legal/templates/Karta_Wdrozenia_Klienta_Wzor.md](templates/Karta_Wdrozenia_Klienta_Wzor.md)
- [docs/legal/templates/Klauzula_Informacyjna_Dla_Pracownikow_Wzor.md](templates/Klauzula_Informacyjna_Dla_Pracownikow_Wzor.md)
- [docs/legal/templates/Protokol_Uruchomienia_i_Przekazania_Wzor.md](templates/Protokol_Uruchomienia_i_Przekazania_Wzor.md)
- [docs/legal/templates/Umowa_Powierzenia_Danych_DPA.md](templates/Umowa_Powierzenia_Danych_DPA.md)
- [docs/legal/templates/Zalacznik_SLA_i_Wsparcie.md](templates/Zalacznik_SLA_i_Wsparcie.md)
- [docs/legal/templates/Zalacznik_TOMs_i_Retencja.md](templates/Zalacznik_TOMs_i_Retencja.md)
- [docs/legal/templates/Zamowienie_Umowa_B2B.md](templates/Zamowienie_Umowa_B2B.md)
- [scripts/check-legal-package.mjs](../../scripts/check-legal-package.mjs)
