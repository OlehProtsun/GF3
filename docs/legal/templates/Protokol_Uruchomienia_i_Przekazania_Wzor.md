# Protokół uruchomienia i przekazania GF3 — WZÓR PRYWATNY

**DRAFT — WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ I OPERACYJNEJ**
Wersja: 0.1.0-draft | Data sporządzenia: [DO UZUPEŁNIENIA]

Wypełniać kopię w zewnętrznym chronionym archiwum, nigdy w Git lub publicznym buildzie. Nie wpisywać danych pracowników, haseł, PIN, tokenów ani kluczy. Protokół dokumentuje rzeczywiste przekazanie; **nie zastępuje Umowy SaaS, DPA, umów o pracę ani informacji GDPR pracodawcy**. Nie certyfikuje zgodności prawnej ani bezpieczeństwa. Wyniki testów muszą mieć dowody; niewykonany test oznaczyć NOT VERIFIED.

## 1. Strony i podstawa

- Klient: pełna nazwa / forma / adres / KRS-CEIDG / NIP: [DO UZUPEŁNIENIA].
- Operator: prawdziwa nazwa lub imię i nazwisko / zweryfikowany status / adres / identyfikatory: [DO UZUPEŁNIENIA].
- Upoważnieni reprezentanci stron / podstawy umocowania / dowody weryfikacji: [DO UZUPEŁNIENIA].
- Jeden podpisany SaaS: numer / wersja / data / zakres usług / referencja zamówienia, jeśli stosowane: [DO UZUPEŁNIENIA].
- Podpisane DPA/TOMs i opcjonalne uzgodnione SLA: wersje / daty / chronione referencje: [DO UZUPEŁNIENIA].
- Lokalizacja finalnych podpisów i dowodów doręczenia poza repo / osoby uprawnione: [DO UZUPEŁNIENIA].

## 2. Instancja i faktycznie dostarczony zakres

- Identyfikator instancji bez sekretów / URL / klient przypisany wyłącznie do tej instancji: [DO UZUPEŁNIENIA].
- Oddzielna baza/volume, konfiguracja, sekrety i kopie — referencja dowodu izolacji bez ujawniania sekretów: [DO UZUPEŁNIENIA].
- Planowana aktywacja / rzeczywista data i godzina aktywacji / strefa Europe/Warsaw lub jawny offset: [DO UZUPEŁNIENIA].
- Lokalizacje / liczba kont i role / limity / wersja wdrożenia: [DO UZUPEŁNIENIA].

| Funkcja z podpisanego zakresu | Faktycznie przekazany zakres / ograniczenia | Dowód / status / data |
|---|---|---|
| Zarządzanie kontami i lokalizacjami | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Grafiki: tworzenie / publikacja / widok pracownika | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Dostępność | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Zamiany zmian / zatwierdzanie / historia | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Komunikaty / powiadomienia / rzeczywisty eksport | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Inne wyłącznie uzgodnione funkcje | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |

Nie potwierdzać payroll, automatycznej prawnej poprawności grafików, eksportu PDF ani SLA, jeśli nie zostały rzeczywiście uzgodnione i zapewnione.

## 3. Bezpieczne przekazanie i szkolenie

| Czynność | Odbiorca / bezpieczna metoda, bez danych uwierzytelniających | Przekazujący / data / potwierdzenie odbioru |
|---|---|---|
| Dostęp upoważnionego managera / weryfikacja logowania | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Tworzenie kont, ograniczanie uprawnień i odbieranie dostępu | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Szkolenie: grafiki, dostępność, swap, wiadomości i historia | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Materiały i kontakty wsparcia / eskalacji | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |

Klient zachowuje odpowiedzialność za prawo pracy i legalność danych. Zamiana w aplikacji nie zastępuje jego kontroli czasu pracy. Pracodawca zatwierdza i doręcza własną informację GDPR: wersja / dowód / data / właściciel: [DO UZUPEŁNIENIA].

## 4. Dokumenty i testy odbiorowe

Publiczne regulamin/zasady i osobny PDF muszą być uzgodnione przez człowieka. Rejestr zatwierdzeń: dokument / wersja / SHA-256 / recenzent / data / role / dowód: [DO UZUPEŁNIENIA]. Opublikowany PDF: ID / nazwa / wersja / data / zakres manager-employee: [DO UZUPEŁNIENIA]. Nie publikować DRAFT ani SaaS/DPA/TOMs/faktur w gate. Checkbox użytkownika nie jest podpisem firmy ani zgodą GDPR.

Statusy testów: PASS / FAIL / NOT VERIFIED / BLOCKED. Nie zaznaczać PASS bez rzeczywistej weryfikacji; używać syntetycznych kont.

| Test | Wynik / status | Tester / data | Chronione miejsce dowodu |
|---|---|---|---|
| Izolacja jednego klienta / TLS / least privilege / auth-PIN-token | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Backup i restore w odrębnym środowisku — zakres i wynik | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Manager: logowanie / konta / publikacja grafiku / swap | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Employee: grafiki / dostępność / swap / historia | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Role widzą tylko należne pending PDF / pobranie / checkbox początkowo pusty | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| API 428 przed accept / zapis osobistego potwierdzenia / dostęp po wszystkich pending | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Nowa wersja ponownie wymaga accept / własna historia / employee history dla managera w granicach uprawnień | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Stare PDF i historia dostępne w uzgodnionym archiwum / powrót grafików i swap po accept | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Kontakt incydentowy / wsparcie / eskalacja | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Eksport / odebranie dostępu / zwrot-usunięcie / kopie i retencja zgodnie z DPA | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |

## 5. Kontakty i otwarte kwestie

- Wsparcie: kanał / godziny / warunki z umowy / eskalacja / osoby stron: [DO UZUPEŁNIENIA].
- Prywatność/DSAR, incydenty i finanse: kanały / odpowiedzialni / daty testów: [DO UZUPEŁNIENIA].
- Wyjście z usługi: dostępny format eksportu / terminy / właściciel / usunięcie i wyjątki / cykl kopii: [DO UZUPEŁNIENIA].

| Nierozwiązana kwestia / nieprzekazana funkcja | Wpływ / STOP-SHIP | Właściciel / termin | Dowód rozwiązania / status |
|---|---|---|---|
| [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |

## 6. Odbiór i decyzja

- Decyzja GO / NO-GO / odbiór z opisanymi zastrzeżeniami, faktyczne uzasadnienie i dowody: [DO UZUPEŁNIENIA].
- Potwierdzenie odbioru instancji, dostępu i szkolenia przez umocowanego klienta / data: [DO UZUPEŁNIENIA].
- Reprezentant klienta / umocowanie / podpis / data: [DO UZUPEŁNIENIA].
- Reprezentant operatora / umocowanie / podpis / data: [DO UZUPEŁNIENIA].
- Chronione miejsce podpisanego protokołu i dostarczonych kopii: [DO UZUPEŁNIENIA].

Odbiór z zastrzeżeniami nie znosi STOP-SHIP: nierozwiązany blocker prawny, ochrony danych lub bezpieczeństwa oznacza NO-GO dla production. Sam podpis protokołu nie zatwierdza nieprzetestowanych kontroli.

Powiązania: [karta wdrożenia](Karta_Wdrozenia_Klienta_Wzor.md), [playbook UA](../OPERATOR_ONBOARDING_PLAYBOOK_UA.md), [STOP-SHIP](../internal/CHECKLIST_PRZED_STARTEM.md), [rejestr review](../LEGAL_REVIEW.md).
