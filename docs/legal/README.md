# Pakiet prawny GF3

WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ

Wersja: 0.1.0-draft | Obowiązuje od: [DO UZUPEŁNIENIA]
Operator: [DO UZUPEŁNIENIA]

Kanoniczne publiczne pliki: `FrontEnd/public/legal/*.html` i `legal.css`. Linki: logowanie, oba układy i bramka PDF. `docs/design-system.json` pozostaje zamrożonym opisem; nie zmieniamy semantyki. Prywatne wzory i TOM nie należą do dist/wwwroot.

Onboarding: fakty Operatora → przegląd i STOP-SHIP → podpisane Zamówienie/DPA poza repo → izolowana instancja → zatwierdzony PDF/wersja/hash w rejestrze → upload/publish przez system managera → pending manager/pracownik → nie zaznaczony checkbox i lektura PDF → własne potwierdzenie → utrwalony dowód → dostęp API. Nowa publikacja wymaga potwierdzenia, historia pozostaje. Nie publikować roboczych PDF na produkcji.

`node scripts/validate-legal-documents.mjs`; `--release` tylko po rzeczywistym przeglądzie. Brak gwarancji zgodności/certyfikacji lub zatwierdzenia komercyjnego startu.

Źródła sprawdzone 2026-10-08, ponownie zweryfikować przed podpisem:
- https://eli.gov.pl/api/acts/DU/2024/1513/text.html
- https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng
- https://eli.gov.pl/api/acts/DU/2024/1221/text.html
- https://ksef.podatki.gov.pl/etapy-wdrozenia-ksef/

## Weryfikacja wykonania — 2026-10-08

- Baseline HEAD `5980123995199dc4b6d40d2e3b17ff49731a6a39`, DEV2. Dostarczony Plan.md był jedyną początkową zmianą i nie został zmodyfikowany przez wykonawcę. TechStack.md nie znaleziono.
- dotnet restore/build GF3.sln: sukces, build bez ostrzeżeń/błędów. dotnet test GF3.sln: 491/491; po dodaniu kolejnego scenariusza i rzeczywistych ścieżek biznesowych ponownie wykonano 18/18 RegulationAcceptanceGuardTests.
- npm ci: sukces; raport zależności: 17 podatności (2 low, 4 moderate, 11 high) w niezmienionym lockfile, bez aktualizacji poza zakresem. npm run lint: 0 błędów, 18 ostrzeżeń w nietkniętych plikach; dodatkowy lint nowych testów: 0. npm run build: sukces (istniejące ostrzeżenia Rollup/chunk size).
- Vitest: 22/22 w czterech testowanych komponentach: login, dwa układy, gate PDF.
- API test host używa prawdziwego JWT, SQLite, polityk i admin middleware. Manager/employee: dostęp przed publikacją, 428 przy pending, dostęp po akceptacji, kolejna wersja blokuje. Rzeczywiste odczyty grafików, zamian i eksport SQL działają przed/po; duplikaty, nieopublikowane PDF, brakujące dokumenty i niedostępność DB sprawdzone. Admin upload/publish wymaga dotychczasowych uprawnień i hasła developerskiego.
- Playwright Chromium oraz WebKit iPhone X: 10/10. publiczne strony/cross-links/druk, login links, krótki ekran 320×568, employee tabs i krawędzie keypad. Desktop obsługuje Tab/Enter; mobile sprawdza fokus i aktywację programowo, nie fizyczną klawiaturę iPhone. Emulacja nie jest testem fizycznego urządzenia.
- Siedem publicznych ścieżek lokalnego Vite i skompilowanego Production ASP.NET: 200/text/html bez wejścia i bez SPA shell; oddzielna syntetyczna SQLite. Brak prywatnych wzorów/TOM w sprawdzonych dist/wwwroot.
- Draft validator: sukces; --release odrzuca placeholdery i brak zatwierdzeń, zgodnie z wymaganiem. To celowa blokada publikacji, nie zatwierdzenie prawne.
- git diff --check dla zmian wykonawcy: sukces; istniejące trailing whitespace w dostarczonym Plan.md pozostawiono.

- curl na odrębnej syntetycznej instancji Production: GET /api/containers → 428 po publikacji PDF, → 200 natychmiast po POST accept; bez drukowania tokenów.

## Dostosowania i ograniczenia

Wzór pracowniczy pozostaje prywatny zgodnie z sekcją B planu; publiczna polityka prowadzi do informacji o jego uzyskaniu w hubie, zamiast publikować prywatny załącznik. Karta phone-login zyskała lokalne przewijanie dla krótkich ekranów bez zmiany logiki PIN. Snapshot design-system pozostawiono, a linki dokumentuje ten README. Dotychczasowe PDF pozostają wymaganymi opublikowanymi wersjami według istniejącego repository; brak nowej logiki zastępowania wersji.

Nie wykonano audytu zewnętrznej domeny/proxy, podpisów, prawnej akceptacji, fizycznych urządzeń lub komercyjnego wdrożenia. Dane Operatora, dostawcy/retencja, podpisy i STOP-SHIP muszą być ukończone poza kodem. Brak gwarancji zgodności/certyfikacji lub zgody na start komercyjny.

## Pliki zmienione

- `FrontEnd/e2e/legal-documents.spec.ts`
- `FrontEnd/public/legal/bezpieczenstwo.html`
- `FrontEnd/public/legal/index.html`
- `FrontEnd/public/legal/legal.css`
- `FrontEnd/public/legal/pliki-cookies.html`
- `FrontEnd/public/legal/podwykonawcy.html`
- `FrontEnd/public/legal/polityka-prywatnosci.html`
- `FrontEnd/public/legal/regulamin.html`
- `FrontEnd/public/legal/zasady-korzystania.html`
- `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.module.css`
- `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.test.tsx`
- `FrontEnd/src/app/layouts/employee-workspace-layout/EmployeeWorkspaceLayout.tsx`
- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.module.css`
- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.test.tsx`
- `FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.tsx`
- `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.test.tsx`
- `FrontEnd/src/entities/regulations/ui/RegulationAcceptanceGate.tsx`
- `FrontEnd/src/pages/login/ui/LoginPage.module.css`
- `FrontEnd/src/pages/login/ui/LoginPage.test.tsx`
- `FrontEnd/src/pages/login/ui/LoginPage.tsx`
- `FrontEnd/src/shared/i18n/pl.json`
- `GF3.Tests/RegulationAcceptanceGuardTests.cs`
- `GF3.WebApi/Controllers/RegulationsController.cs`
- `GF3.WebApi/Middleware/RegulationAcceptanceGuardMiddleware.cs`
- `GF3.WebApi/Program.cs`
- `GF3.WebApi/Services/RegulationSubjectResolver.cs`
- `docs/legal/COOKIE_AUDIT.md`
- `docs/legal/LEGAL_REVIEW.md`
- `docs/legal/OPERATOR_INPUTS.md`
- `docs/legal/README.md`
- `docs/legal/electronic-services-regulations/README.md`
- `docs/legal/electronic-services-regulations/checklista-wdrozenia-regulaminu.md`
- `docs/legal/electronic-services-regulations/generate-html.mjs`
- `docs/legal/electronic-services-regulations/informacja-o-zagrozeniach-pl.md`
- `docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.html`
- `docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md`
- `docs/legal/internal/CHECKLIST_PRZED_STARTEM.md`
- `docs/legal/internal/Ocena_Ryzyka_i_DPIA_Screening.md`
- `docs/legal/internal/Procedura_DSAR_Retencji_Usuwania.md`
- `docs/legal/internal/Procedura_Incydentow_i_Naruszen.md`
- `docs/legal/internal/Rejestr_Czynnosci_Przetwarzania.md`
- `docs/legal/internal/Rejestr_Podwykonawcow_i_Transferow.md`
- `docs/legal/saas-agreement/README.md`
- `docs/legal/saas-agreement/checklista-przed-podpisaniem.md`
- `docs/legal/saas-agreement/generate-html.mjs`
- `docs/legal/saas-agreement/umowa-saas-b2b-pl.html`
- `docs/legal/saas-agreement/umowa-saas-b2b-pl.md`
- `docs/legal/saas-agreement/zalacznik-1-zakres-uslug.md`
- `docs/legal/templates/Klauzula_Informacyjna_Dla_Pracownikow_Wzor.md`
- `docs/legal/templates/Umowa_Powierzenia_Danych_DPA.md`
- `docs/legal/templates/Zalacznik_SLA_i_Wsparcie.md`
- `docs/legal/templates/Zalacznik_TOMs_i_Retencja.md`
- `docs/legal/templates/Zamowienie_Umowa_B2B.md`
- `scripts/validate-legal-documents.mjs`
