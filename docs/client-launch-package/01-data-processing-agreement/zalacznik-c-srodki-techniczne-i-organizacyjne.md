# Załącznik C – środki techniczne i organizacyjne

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
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
