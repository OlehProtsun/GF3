# Polityka bezpieczeństwa danych GF

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
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
