# Raport braków i ryzyk

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | final review; security; backup; cookies |
| Klasyfikacja | POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Podsumowanie

Pakiet jest kompletnym szkieletem do profesjonalnego przeglądu, ale nie potwierdza gotowości prawnej ani produkcyjnej. Ocena repozytorium jest statyczna i nie potwierdza konfiguracji działającego środowiska.

| Priorytet | Brak/ryzyko | Dowód z audytu repo | Blokuje | Właściciel / działanie |
|---|---|---|---|---|
| Krytyczny | słabe uwierzytelnienie uprzywilejowane | dokładnie 6 cyfr; brak potwierdzonego rate limitingu/MFA | produkcję | techniczny: projekt i test kontroli |
| Wysoki | JWT w localStorage / XSS | klucz `gf3.auth.access-token` | produkcję | ocena sesji, CSP/XSS i architektury |
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

Zamknąć decyzje z final-review, wykonać przeglądy specjalistyczne, wdrożyć kontrole techniczne, przetestować produkcję i uzupełnić rejestr placeholderów. Nie przenosić ryzyka krytycznego na Klienta ogólną klauzulą.

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
