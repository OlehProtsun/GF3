# Regulamin korzystania z platformy GF dla pracowników

**DRAFT — DO WERYFIKACJI PRAWNEJ**, wersja **0.1.0-draft**. Dokument gotowy do przeglądu, bez zgody na produkcyjne opublikowanie lub przyjmowanie potwierdzeń. Utworzenie PDF, poprawny SHA-256 i zielone testy nie są zatwierdzeniem prawnym.

## Zakres i pliki

| Plik | Przeznaczenie |
|---|---|
| [regulamin-korzystania-z-gf-dla-pracownikow-pl.md](regulamin-korzystania-z-gf-dla-pracownikow-pl.md) | Jedyne źródło treści pracowniczej; 14 pełnych paragrafów |
| [regulamin-korzystania-z-gf-dla-pracownikow-pl.html](regulamin-korzystania-z-gf-dla-pracownikow-pl.html) | Autonomiczny responsywny HTML PL, wbudowany obraz GF i style, bez zewnętrznych żądań lub fontów |
| [regulamin-korzystania-z-gf-dla-pracownikow-pl.pdf](regulamin-korzystania-z-gf-dla-pracownikow-pl.pdf) | Właściwy PDF A4 do recenzji: 6 stron, tekst wyszukiwalny i zaznaczalny |
| [regulamin-korzystania-z-gf-dla-pracownikow-pl.pdf.sha256](regulamin-korzystania-z-gf-dla-pracownikow-pl.pdf.sha256) | SHA-256 dokładnych bajtów bieżącego PDF, nie zatwierdzenie |
| [generate-pdf.mjs](generate-pdf.mjs) | Markdown → HTML → Python/ReportLab → PDF i suma kontrolna; tryb --check bez zapisu |
| [render-pdf.py](render-pdf.py) | Renderowanie wygenerowanego HTML, osadzanie fontów i kontrola liczby stron/limitu backendu |
| [validate-pdf.py](validate-pdf.py) | Parzystość treści MD/HTML/PDF, nagłówki, Unicode, metadane, linki, fonty i granice tekstu |
| [regulation-gate-copy.md](regulation-gate-copy.md) | Proponowane teksty dialogu PL/EN; opis ograniczeń obecnych pól |
| [README.md](README.md) | Niniejsza instrukcja, źródła i wyniki |

Dokument jest pracowniczym uzupełnieniem [pełnego regulaminu usług](../electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md), nie jego zamiennikiem. Seria 0.1.0-draft dotyczy tego nowego dokumentu; harmonizowany pakiet główny ma odrębną wersję 0.2.0-draft. Pozostałe źródła: [SaaS](../saas-agreement/umowa-saas-b2b-pl.md), [DPA](../../client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md), [prywatność](../../client-launch-package/02-privacy/polityka-prywatnosci-pl.md), [pamięć](../../client-launch-package/03-cookies-and-storage/polityka-cookies-pl.md), [raport harmonizacji](../LEGAL_COMPLETION_REPORT_2026-10-19.md), [rejestr przeglądu](../LEGAL_REVIEW.md) i [dane Operatora](../OPERATOR_INPUTS.md).

Baseline lokalnego DEV2: `171aec08a34dd48e43f1eca8c23d104a4ee7b750`. W chwili rozpoczęcia zadania harmonizacja 0.2.0-draft była obecna jako niezacommitowane zmiany w tym checkout. Wykorzystano te aktualne treści, a nie starsze wersje z samego commitu; nie cofnięto ich ani nie przedstawiono ich jako osobnego zatwierdzonego commitu. Przegląd dokumentu i kodu: 2026-10-19.

## Spis treści i układ PDF

| Paragraf | Tytuł | Strona |
|---|---|---:|
| § 1 | Postanowienia ogólne | 1 |
| § 2 | Definicje | 1 |
| § 3 | Dostęp do platformy i konto użytkownika | 2 |
| § 4 | Grafiki pracy | 2 |
| § 5 | Dostępność pracownika | 3 |
| § 6 | Zamiany zmian | 3 |
| § 7 | Korekty i zgłaszanie błędów | 4 |
| § 8 | Powiadomienia | 4 |
| § 9 | Dane osobowe i prywatność | 5 |
| § 10 | Bezpieczeństwo i zasady korzystania | 5 |
| § 11 | Wymagania techniczne | 6 |
| § 12 | Problemy techniczne i kontakt | 6 |
| § 13 | Zmiany regulaminu | 6 |
| § 14 | Postanowienia końcowe | 6 |

## Powtarzalna generacja

Uruchamiać z katalogu głównego repozytorium:

```powershell
node docs/legal/employee-regulations/generate-pdf.mjs
node docs/legal/employee-regulations/generate-pdf.mjs --check
```

Skrypt używa modułów wbudowanych Node i lokalnego Pythona. Najpierw wybiera `GF_PDF_PYTHON`, następnie wykrywa pakiet runtime Codex, a poza nim `python` na Windows lub `python3` na innych systemach. Wymagane są ReportLab i pypdf; walidator dodatkowo wymaga pdfplumber. Nie dodano zależności aplikacji. Minimalna instalacja w osobnym środowisku Python, jeżeli brak pakietów:

```powershell
python -m pip install reportlab==4.4.9 pypdf pdfplumber
$env:GF_PDF_PYTHON = 'PEŁNA_ŚCIEŻKA_DO_PYTHON'
```

W tym środowisku dostępny Python: `C:/Users/Oleg/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`. Podana ścieżka jest lokalną konfiguracją narzędzia, nie adresem produkcji i nie trafia do PDF. Ograniczona piaskownica Windows blokowała proces potomny Node → Python przez EPERM; dopuszczony lokalny tryb wykonał generację. Alternatywnie, po utworzeniu aktualnego HTML można uruchomić ten sam renderer bezpośrednio przez Python. Nie należy traktować takiego obejścia jako zastępstwa późniejszego sprawdzenia HTML i --check.

Fonty są odczytywane z systemu, bez kopiowania plików fontów do repozytorium. Wykorzystano Arial Regular/Bold dostarczone z Windows i osadzone jako podzbiory TrueType. Na innym systemie skrypt obsługuje DejaVu Sans/DejaVu Sans Bold. `GF_PDF_FONT_DIR` może wskazywać katalog z odpowiednią legalnie używaną parą TTF. Zmiana fontów, wersji ReportLab lub źródeł może zmienić łamanie i SHA; wynik wymaga ponownego renderowania i przeglądu. Nie deklaruje się identycznych bajtów między różnymi fontami i runtime.

PDF ma deterministyczny techniczny znacznik metadanych 2026-10-19 00:00 UTC, niezależny od chwili ponownego uruchomienia. Nie jest on datą wejścia regulaminu w życie, akceptacji przez pracownika ani zatwierdzenia. Przy tych samych źródłach, logo, skryptach, fontach i runtime kolejne generacje dają te same bajty. --check porównuje HTML, PDF i plik sumy z odtworzonym wynikiem bez ich nadpisania. Skrypt nie wprowadza statusu APPROVED: drukuje status wpisany w źródle przez człowieka, a dla wersji niedraftowej odrzuca pozostałe pola DO UZUPEŁNIENIA/DO ZATWIERDZENIA. Sam status wpisany w pliku nie dowodzi faktycznego zatwierdzenia.

Logo pochodzi z istniejącego `FrontEnd/public/icons/gf-icon-192.png`; jest osadzone wewnątrz HTML i PDF. Nie dodano obrazów zewnętrznych. Potwierdzenie praw do brandingu pozostaje odpowiedzialnością Operatora, tak jak w pakiecie głównym.

### Adresy dokumentów

Nie wymyślono domeny Operatora. W lokalnym HTML odnośniki prowadzą do kanonicznych plików pakietu w repozytorium; PDF drukuje znane ścieżki `/legal/...` jako odwołania, zamiast tworzyć nieprawidłowe względne URI lub ujawniać ścieżki dysku. Link do urzędowej strony UODO jest aktywnym HTTPS. Po potwierdzeniu rzeczywistego originu można dodać:

```text
node docs/legal/employee-regulations/generate-pdf.mjs --base-url=<POTWIERDZONY_ORIGIN_HTTPS>
```

Argument wymaga rzeczywistego HTTPS originu, bez danych logowania, ścieżki, query lub fragmentu. Należy również uzupełnić adres w Markdown. Ten sam argument trzeba podać przy --check. Wówczas odnośniki do dokumentów są absolutnymi, klikalnymi URL w obu formatach. Przed wydaniem sprawdzić dostęp bez sesji i zgodność wersji pod tymi adresami. Nie publikować zastępczej domeny lub adresów wewnętrznych.

## Potwierdzone zachowanie i dowody z kodu

| Obszar | Źródło i istotny symbol | Wniosek dla dokumentu |
|---|---|---|
| Konto/sesja | FrontEnd/src/app/providers/AuthProvider.tsx:24–48 i logout:162; GF3.WebApi/Controllers/EmployeeProfileController.cs | Konto od Menedżera, sesja w pamięci przeglądarki, profil, odzyskiwanie; zamknięcie karty nie wylogowuje |
| Hasło i archiwizacja | BusinessLogicLayer/Security/NumericPasswordPolicy.cs:8; DataAccessLayer/Repositories/EmployeeRepository.cs:20, SoftDeleteAsync | Hasło numeryczne; IsDeleted zachowuje profil i historie, konto jest usuwane; nie erasure |
| Grafik i generator | GF3.WebApi/Controllers/EmployeeSchedulesController.cs:25, GetVisible; ContainersController.cs:271–326, GenerateGraphPreview/GenerateGraph; BusinessLogicLayer/Generators/ScheduleGenerator.cs:90–160, AttemptScore | Opublikowane przypisane grafiki; algorytm, edycja i publikacja Menedżera; brak gwarancji zgodności prawa pracy |
| Dostępność | EmployeeAvailabilityController.cs:29–57, GetVisible/GetById/UpdateSlots; BusinessLogicLayer/Services/AvailabilityGroupService.cs:115–125, SaveEmployeeAvailabilityAsync; :442–487, VisibleFrom/To i CanSubmit | Okno zgłoszeń, aktualizacje, blokady; bez gwarancji przydziału lub urlopu |
| Zamiany i wolne zmiany | EmployeeShiftSwapsController.cs:116, Create; :201, Accept; :416, Cancel; :690, AcceptExchangeAsync; :932, EnsureSwapAllowed; GF3.WebApi/ShiftSwaps/ShiftSwapRules.cs:147–168 | Przekazanie i wzajemna prywatna wymiana, przejęcie wolnej zmiany; open/accepted/cancelled, walidacje i natychmiastowa zmiana przy skutecznym przyjęciu; brak uniwersalnego etapu zatwierdzenia przez Menedżera lub jednolitego deadline |
| Korekta | GF3.WebApi/Controllers/ShiftCorrectionsController.cs:20–120, EmployeeShiftCorrectionsController.Create i GetMine | Wniosek początku/końca własnej zmiany; walidacja, jedna oczekująca korekta; samo zgłoszenie nie zmienia grafiku |
| Inbox/aktualności | FrontEnd/src/pages/employee-notifications/ui/EmployeeNotificationsPage.tsx:89–171; model/notifications.ts | Powiadomienia i odczyty, nie komunikator prywatny i nie dowód prawnego doręczenia |
| Język i push | employee-account/ui/EmployeeAccountPage.tsx, LanguageSelector/EmployeePushSettings; employee-push/ui/EmployeePushSettings.tsx; lib/enablePush.ts:5–18; lib/pushSubscription.ts:72 | Język konta PL/EN, opt-in/opt-out urządzenia, permission, ograniczenia offline/iOS; brak gwarancji wysyłki |
| Treść push | GF3.WebApi/Services/EmployeePushMessages.cs:5–55, Format; EmployeePushNotificationSource.cs:29–96 | Grafik/dostępność/zamiana/wolna zmiana, kontekst dat/godzin, ekran blokady |
| Dowód PDF | RegulationDocumentModel.cs, RegulationAcceptanceModel.cs; BusinessLogicLayer/Services/RegulationService.cs:73–150, UpdateAsync/PublishAsync/AcceptAsync | Niepodmienialna opublikowana treść, wersja/hash, snapshot konta, idempotentne osobiste potwierdzenie; nie podpis firmy |
| Historia/pobranie | GF3.WebApi/Controllers/RegulationsController.cs:19–58, GetMyHistory/GetEmployeeHistory/DownloadPdf/Accept | Historia konta, historia pracownika dla Menedżera i pobranie jako application/pdf z nazwą pliku |
| Gate 428 | RegulationAcceptanceGuardMiddleware.cs:12–64; RegulationAcceptanceGate.tsx:20–95 | Niezaznaczony checkbox, reset przy nowym dokumencie, ograniczenie zwykłych API przy pending; wyjątki auth, PDF/accept i własna historia |

Odczytano także encje, listę pól w publicznych politykach, dokumenty bezpieczeństwa oraz kanoniczne kontrakty. Nie dodano payroll, GPS, prywatnego czatu, gwarantowanych przydziałów lub nieograniczonej edycji grafiku. Nie utożsamiono wbudowanej walidacji konfliktów z pełną kontrolą prawa pracy.

## Zgodność z mechanizmem publikacji i ograniczenia

1. `RegulationService.MaxPdfBytes` wynosi **10 × 1024 × 1024 = 10 485 760 bajtów**. ValidatePdf wymaga końcówki `.pdf`, nazwy do 255 znaków, co najmniej 5 bajtów i prefiksu `%PDF-`. Bieżący plik ma **131 529 bajtów**, 6 stron A4 i poprawną strukturę pypdf/Poppler, bez szyfrowania, formularzy i skryptów. Tytuł dokumentu mieści się w 160 znakach, wersja w 80, propozycja wiadomości w 4000. Multipart kontrolera ma limit 10 600 000 bajtów. Rozmiar tego pliku pozostawia duży zapas także dla pól formularza.
2. Suma dokładnych bajtów jest zapisana w sidecar `.pdf.sha256`. Dla zweryfikowanej wersji wynosi `975e7e025f519c48cc1ffee1ba30d1662885b03541df0c95bfb43bcaa2c739f6`. **Nie jest to approved hash.** Przy każdej zmianie należy posługiwać się nowym sidecar, a zatwierdzenie powiązać z rzeczywiście sprawdzonym plikiem.
3. **Brak selekcji odbiorców:** DataAccessLayer/Repositories/RegulationRepository.cs:23–29, ListPendingAsync wybiera wszystkie opublikowane dokumenty, które dane konto jeszcze nie potwierdziło. Rola służy dopasowaniu potwierdzeń, nie filtrowaniu dokumentu. Model i formularz panelu nie mają pola audience/role. Opublikowany dokument pojawi się zarówno pracownikom, jak i menedżerom; panel uprzedza o obu rolach. Jest to blocker, jeśli wymaganiem jest publikacja wyłącznie dla pracowników. Nie zmieniono tej funkcji i nie należy obchodzić jej przez ręczne wpisy w bazie. Przed publikacją Operator/prawnik muszą zatwierdzić sposób prezentacji obu rolom albo odrębnie zlecić i przetestować wybór odbiorców. Sam PDF nie naprawia tego ograniczenia.
4. **Język:** jedno pole tytułu i wiadomości nie przełącza się automatycznie PL/EN. Checkbox i przycisk są ogólnymi tłumaczeniami kodu, nie polami dokumentu. Proponowana treść z regulation-gate-copy.md pozostaje materiałem do zatwierdzenia; nie zmieniono UI.
5. **Wersja jest globalnie unikalna** w rejestrze dokumentów, nie tylko w obrębie tego tytułu. Nie sprawdzano bazy produkcyjnej; 0.1.0-draft nie jest rezerwacją. Przed wydaniem ustalić unikalną finalną wersję, zmienić ją w źródle i odtworzyć pliki. Nowa publikacja nie zastępuje automatycznie poprzednich: użytkownik może mieć do potwierdzenia więcej niż jeden opublikowany rekord. Nie usuwać lub fałszować historycznych potwierdzeń.
6. Potwierdzono konstrukcyjną zgodność pliku i pobierania. Nie wykonywano uploadu ani testu rzeczywistego pobrania z produkcji. Nie publikowano dokumentu w żadnej bazie.

## Źródła prawa i zakres ich weryfikacji

Sprawdzone 2026-10-19, jako źródła wymagań do projektu, nie formalna opinia o wszystkich nowelizacjach:

- [UŚUDE, ELI, Dz.U. 2024 poz. 1513](https://eli.gov.pl/api/acts/DU/2024/1513/text.html): art. 5 oznaczenie usługodawcy, art. 6 ryzyka i oprogramowanie/dane urządzenia, art. 8 zakres, wymagania techniczne, treści bezprawne, warunki korzystania/zakończenia i reklamacje. Odczytano tekst i właściwe przepisy; identyfikacja i kontakty wymagają faktów Operatora. Ten dokument nie zastępuje pełnego regulaminu usług.
- [RODO, EUR-Lex, 2016/679](https://eur-lex.europa.eu/eli/reg/2016/679/oj): art. 5–6, 9, 12–22, 28 i 32 — minimalizacja, podstawy, informacja, prawa, role i bezpieczeństwo. W tym przeglądzie EUR-Lex udostępnił pełny tekst; nie powielono historycznej informacji o całkowitej niedostępności z poprzedniego audytu. Nie ustalano indywidualnych podstaw pracodawcy ani nie uznawano każdej operacji algorytmicznej za decyzję art. 22.
- [PKE, ELI, Dz.U. 2024 poz. 1221](https://eli.gov.pl/api/acts/DU/2024/1221/text.html): art. 399 ust. 1–3 — pamięć urządzenia, uprzednia informacja/zgoda i wyjątki; samo nazwanie zapisu localStorage nie wyłącza przepisu. Właściwą kwalifikację każdego zapisu zawiera pakiet główny. Akceptacja niniejszego PDF jej nie zastępuje.
- [UODO](https://uodo.gov.pl/): właściwa droga informacji i skarg. Link PDF do strony urzędu jest aktywny, lecz urząd nie zatwierdził dokumentu.

Nie wykonano pełnego badania wszystkich późniejszych zmian ustaw, praktyki Pracodawcy, umów, domeny, dostawców i konfiguracji produkcji. Przed finalnym zatwierdzeniem prawnik sprawdza aktualny stan prawny i zastosowanie do rzeczywistego modelu zatrudnienia. Główne ujawnione wcześniej luki bezpieczeństwa, opcjonalnego storage/mediów, dostawców, transferów, retencji i DR pozostają w [STOP-SHIP](../internal/CHECKLIST_PRZED_STARTEM.md); sam regulamin pracownika ich nie zamyka.

## Weryfikacja wykonana dla bieżących plików

| Kontrola | Wynik |
|---|---|
| Kompletność | 14 paragrafów, 67 numerowanych ustępów, 88 bloków treści w MD/HTML/PDF; bez lorem ipsum i pustych akapitów |
| Format | 6 stron A4, poprawny PDF 1.4, tytuł/temat/metadane PL, 131 529 bajtów, poniżej limitu |
| Tekst | Automatyczne porównanie każdego bloku i strony: MD → HTML → tekst PDF; zachowane polskie litery; tekst zaznaczalny/wyszukiwalny |
| Fonty/obrazy | Osadzone podzbiory Arial Regular/Bold; test mapowania polskich znaków; logo z istniejącego zasobu, bez brakujących obrazów |
| Granice/wygląd | Kontrola współrzędnych wszystkich znaków; odrenderowano i wizualnie sprawdzono każdą z 6 stron, bez ucięć, nachodzenia lub osamotnionych nagłówków |
| HTML | Chromium 375 i 1440 px: lang=pl, 14 nagłówków, 67 ustępów, brak poziomego overflow, obrazy załadowane, 0 zewnętrznych żądań |
| Linki | UODO HTTPS w PDF; lokalne odnośniki HTML do kanonicznych pochodnych; portalowe URL pozostają do potwierdzenia originu |
| Dotnet | RegulationServiceTests + RegulationAcceptanceGuardTests: **20/20 PASS** |
| Frontend | RegulationAcceptanceGate.test.tsx: **6/6 PASS** |
| Cały pakiet | Draft validator **7 stron PASS**; check-legal-package **119 MD/HTML, 438 linków PASS**; generator pakietu --check **PASS**. Rejestr pól uzupełniony także o DO ZATWIERDZENIA |
| Release | **FAIL oczekiwany**: 7 stron DRAFT i brak 7 rzeczywistych zatwierdzeń/wersji/hash; nie zmieniono walidatora release |

Polecenia odtworzenia kontroli (po ustawieniu GF_PDF_PYTHON, jeśli potrzebne):

```powershell
node docs/legal/employee-regulations/generate-pdf.mjs --check
& $env:GF_PDF_PYTHON docs/legal/employee-regulations/validate-pdf.py
node scripts/check-legal-package.mjs
node scripts/validate-legal-documents.mjs
dotnet test GF3.Tests/GF3.Tests.csproj --no-restore --filter 'FullyQualifiedName~RegulationServiceTests|FullyQualifiedName~RegulationAcceptanceGuardTests' --verbosity quiet
# z FrontEnd:
node node_modules/vitest/vitest.mjs run src/entities/regulations/ui/RegulationAcceptanceGate.test.tsx
```

Render do oględzin: `pdftoppm -r 105 -png docs/legal/employee-regulations/regulamin-korzystania-z-gf-dla-pracownikow-pl.pdf tmp/pdfs/gf-employee-regulations/page` po utworzeniu katalogu. Narzędzie Poppler i biblioteki PDF były dostępne w runtime. Nie uzyskano audytu PDF/UA lub pełnego audytu dostępności; dokument ma tekst i semantyczny HTML, ale PDF nie jest deklarowany jako certyfikowany dostępnościowo. Nie testowano fizycznego iPhone ani produkcyjnej infrastruktury.

## Otwarte pola i decyzje

W samym regulaminie pozostało **7 rodzajów pól**, wyłącznie wymagających rzeczywistego potwierdzenia:

1. DANE OPERATORA — pełna nazwa prawna, adres i właściwe identyfikatory; źródło: Operator i dokumenty rejestrowe, nie GitHub.
2. PUBLICZNY ADRES GF — rzeczywista bezpieczna domena i anonimowy dostęp do dokumentów; Operator/administrator infrastruktury.
3. DATA OBOWIĄZYWANIA — decyzja po kontroli zgodnego z prawem powiadomienia i zatwierdzeniu; Operator/prawnik.
4. KONTAKT WSPARCIA — działający kanał i obsada; Operator.
5. KONTAKT REKLAMACYJNY — odbiór i właściwa procedura; Operator/prawnik.
6. KONTAKT BEZPIECZEŃSTWA — pilna eskalacja i zastępstwo; Operator z Pracodawcą.
7. KONTAKT PRYWATNOŚCI — prawidłowy adres i IOD, jeżeli wyznaczony; Operator/specjalista RODO.

Pracodawca zapewnia dodatkowo własną informację RODO i kanały spraw organizacji pracy; nie wpisuje się do ogólnego dokumentu fikcyjnego klienta. Do rozstrzygnięcia: publikacja dla obu ról zamiast wyłącznie pracowników, wersjonowanie/język, rzeczywista kontrola generatora i zasady zgód na zamiany, stosowanie propozycji czasu wsparcia i reklamacji. Nie ma podpisów ani aprobat wygenerowanych automatycznie.

## Przegląd i późniejsze ręczne przekazanie do GF

1. **Potwierdzić Operatora i kontakty.** Uzupełnić 7 pól w Markdown na podstawie prawdziwych źródeł; sprawdzić kanały i informację Pracodawcy. Zweryfikować origin i odnośniki do wersji kanonicznych.
2. **Zamknąć fakty i ograniczenia.** Potwierdzić rzeczywiste funkcje wdrożenia, uprawnienia, aktualny pakiet główny i prawo. Rozstrzygnąć wybór odbiorców: obecny panel nie umożliwia employee-only. Przy takim wymaganiu wstrzymać publikację do osobnej zmiany i testu, bez ręcznych zmian bazy.
3. **Uzyskać rzeczywisty przegląd.** Operator i kwalifikowany polski prawnik zatwierdzają treść, relację z głównym regulaminem, status, unikalną wersję i sposób powiadomienia. Dowód obejmuje dokładne źródła, osoby, datę i zakres; przechowywać poza publicznym Git. Nie wystarcza podpis README lub zielony test.
4. **Ustalić datę obowiązywania.** Uwzględnić wymagane wcześniejsze udostępnienie, zawiadomienie o zmianie oraz prawa użytkowników. Wpisać ją i finalną wersję do Markdown dopiero po tej decyzji. Tekst uwagi o niezatwierdzonym projekcie zmienić wyłącznie zgodnie z faktycznym wynikiem przeglądu, nie przez automatyczne usunięcie słowa DRAFT.
5. **Wygenerować finalne pliki.** Po kontrolowanej edycji statusu/treści uruchomić generator z prawdziwym --base-url; ponowić --check, validate-pdf.py i render wszystkich stron. Skrypt nie potwierdza kwalifikacji osoby zatwierdzającej. Przy przepełnieniu nie pomijać błędu: skorygować łamanie/układ i ponownie obejrzeć cały plik.
6. **Obliczyć i potwierdzić dokładny SHA-256.** Użyć wygenerowanego sidecar lub `Get-FileHash -Algorithm SHA256`. Prawnik/Operator wiążą końcowe zatwierdzenie z tymi dokładnymi bajtami. Po jakiejkolwiek zmianie lub regeneracji z innymi fontami ponowić kontrolę i zatwierdzenie.
7. **Archiwizować.** Zachować finalny PDF, Markdown, HTML, skrypty, identyfikatory runtime/fontów, hash, potwierdzenie przeglądu i doręczenia w chronionym archiwum. Nie umieszczać danych klientów, podpisów lub sekretów w publicznym repozytorium.
8. **Najpierw test syntetyczny.** Na wydzielonej instancji sprawdzić cały obieg. Uprawniony system manager otwiera stronę Database (`/database`) i znajdujący się na niej panel Regulaminy, wypełnia Title, Version, Pop-up message i wybiera właściwy PDF. `Create draft` zapisuje nieopublikowany dokument. Sprawdzić pobrany z panelu PDF, pełny SHA, tytuł i wersję; rekord szkicu nie jest publikacją. Uprawnienie jest kontrolowane przez AuthPolicies.SystemManager oraz istniejący dostęp developerski AdminToolsGuardMiddleware; nie każdy menedżer ma ten dostęp. Sekretu dostępu nie wpisuje się do dokumentów ani nie przekazuje pracownikowi. Nie korzystać przy tym z narzędzi SQL/restore na stronie Database. Lokalizacja panelu: FrontEnd/src/pages/database/ui/DataBasePage.tsx:783; trasa: AppRouter.tsx:107.
9. **Dopiero po zgodzie opublikować.** W zatwierdzonej instancji zastosować tę samą procedurę uploadu i ponownie porównać pobrane bajty z archiwum. `Publish` wymaga oddzielnej świadomej czynności. Obecne potwierdzenie panelu informuje o obowiązku dla managers and employees. Nie publikować tej wersji DRAFT ani nie używać dodatkowego rekordu PL/EN jako obejścia odbiorców.
10. **Sprawdzić potwierdzenie.** Świeże syntetyczne konto pracownika: właściwy pending → pobranie PDF → checkbox pusty → potwierdzenie → zapis wersji/hash/konta/roli/migawek/czasu → własna historia; zwykłe API przed potwierdzeniem 428, po wszystkich wymaganych dokumentach ponownie dostępne. Sprawdzić błąd pobrania/zapisu, brak automatycznej zgody oraz zachowanie menedżera przy obecnej dystrybucji do obu ról. Testem nie zbierać fikcyjnej akceptacji rzeczywistych pracowników.
11. **Zachować stare wersje.** Opublikowanych PDF nie podmieniać i nie usuwać dowodów. Przy nowym tekście utworzyć nową unikalną wersję i zachować poprzedni hash oraz historię. Ocenić istniejące pending wielu wersji; usunięcie szkicu nie jest usunięciem zatwierdzonej historii.

Nie wykonano powyższych czynności produkcyjnych. PDF pozostaje wyłącznie w docs/legal/employee-regulations; nie skopiowano go do FrontEnd/public ani nie zapisano w bazie.
