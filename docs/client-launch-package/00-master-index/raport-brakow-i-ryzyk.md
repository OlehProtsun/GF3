# Raport braków i ryzyk

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
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
| Wysoki | brak produkcyjnego dowodu właściwej publikacji i potwierdzenia | Kod ma publiczne strony, wersje/hash PDF, acceptedAt i gate 428 | start | prawnik/operator: właściwy PDF i test obu ról, nie nowa implementacja |
| Średni | pola swobodne | notatki/komunikaty mogą przyjąć dane szczególne | onboarding | zakaz, szkolenie, minimalizacja |
| Średni | brak pełnego self-service delete/export | wybrane eksporty; IsDeleted zachowuje profil i historię, usuwa konto | DPA/exit | zakres, formaty, kontrolowane usunięcie i test |
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

## Otwarte warunki przeglądu 2026-10-19

- OWNER_REQUIRED: rzeczywista tożsamość, forma działalności, kontakty, terminy/cena/VAT i zdolność wsparcia; weryfikacja dokumentem i testem kontaktu.
- CLIENT_REQUIRED: tożsamość/umocowanie, podstawy i informacja pracowników, retencja, rzeczywista kontrola generatora, instrukcje i podpisy.
- ACCOUNTANT_REQUIRED: kwartalny limit działalności nierejestrowanej 2026, VAT/NIP/PIT/ZUS, dokumenty i przejściowe KSeF do 2027; decyzja na danych całej działalności.
- LAWYER_REQUIRED: pełna aktualność prawa (w tym ograniczony dostęp EUR-Lex), kontrakty i art. 28, prawa JDG, art. 22/DPIA, AI Act i jego zmiany 2026, DSA/EAA/KSC, prawo pracy, terminy i granice odpowiedzialności.
- INFRASTRUCTURE_REQUIRED: rzeczywiści hosting/SMTP/backup/wsparcie/bramy push, role, regiony, umowy, transfery, TLS, izolacja, szyfrowanie i retencja endpointów.
- PRODUCTION_TEST_REQUIRED: hardening 6-cyfrowych haseł i tokenu localStorage, skuteczność uprawnień/izolacji, restore, exit/usunięcie historii, runtime cookies/storage/media i push offline/zmiana konta/ekran blokady.

Brak mechanizmu prior opt-in dla opcjonalnych zapisów i mediów pozostaje STOP-SHIP dla ich wydania, do wdrożenia albo uzasadnionego wyłączenia operacji. Zakończone delivery push >5 dni nie wyznaczają TTL wszystkich danych. Soft-delete nie wykonuje pełnego erasure. Kod potwierdzeń PDF istnieje; brakującym dowodem jest poprawny zatwierdzony PDF i test produkcji, nie implementacja od zera.

Dokładne pliki, sekcje, pola, powód niemożności ustalenia z kodu, odpowiedzialność, metoda i wpływ znajdują się w rejestrze placeholderów. Nie zmieniono żadnego statusu na APPROVED. Otwarte warunki blokują zatwierdzenie/publikację i produkcję, mimo przygotowania treści do przeglądu.
