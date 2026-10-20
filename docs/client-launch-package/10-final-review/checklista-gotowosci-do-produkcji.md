# Checklista gotowości do produkcji

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA; privacy; cookies; incydenty; exit |
| Klasyfikacja | POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

- [ ] role administrator/procesor i własne cele GF udokumentowane; DPA podpisana;
- [ ] mapa danych, rekord procesora, retencja i instrukcje Klienta uzupełnione;
- [ ] podprocesorzy, regiony, transfery i sprzeciw zatwierdzone;
- [ ] polityki prywatności/cookies opublikowane, kontakt i wersja poprawne;
- [ ] zewnętrzne media wyłączone lub click-to-load po ocenie prawnej;
- [ ] proces praw osób, naruszeń, eksportu/usunięcia i dowody przetestowane;
- [ ] zakaz danych szczególnych w polach wolnych zakomunikowany;
- [ ] privacy by design/DPIA screening i ryzyko rezydualne udokumentowane;
- [ ] Klient ma proces doręczenia informacji; sprawdzono wersje PDF, potwierdzenia kont i historię, odrębnie od podpisu firmy;
- [ ] HTTPS, dostęp, backup poza hostem, restore, monitoring i incydenty mają dowody;
- [ ] krytyczne ryzyko sześciocyfrowych kodów, sesji localStorage i braku potwierdzonego rate limitingu/MFA zostało zamknięte albo formalnie rozstrzygnięte bez dopuszczenia nieakceptowalnego ryzyka.

Wynik: **[DO WERYFIKACJI RODO: GOTOWE / WARUNKOWO / NIEGOTOWE]**.

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 24–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)

## Weryfikacja rozszerzonego zakresu

Platforma rejestruje osobiste potwierdzenie opublikowanej wersji PDF przez konto menedżera lub pracownika: identyfikator konta, rolę, migawkę loginu i nazwy oraz czas UTC. Dokument ma wersję, SHA-256 i czas publikacji; opublikowany dokument jest niezmienny, a przyjęte lub opublikowane dokumenty nie podlegają zwykłemu usunięciu. Dostępna jest historia własna i historia pracownika dla menedżera. Middleware zwraca HTTP 428 dla chronionych operacji API przy oczekujących dokumentach, z wyjątkami uwierzytelnienia, pobrania i potwierdzenia dokumentów oraz wskazanych tras. Gate prezentuje PDF i pole domyślnie niezaznaczone. Nie dowodzi to przeczytania każdej strony ani kwalifikowanego podpisu; nie podpisuje Umowy SaaS lub DPA w imieniu Klienta i nie jest ogólną zgodą RODO. Tylko prawnie zweryfikowany PDF może trafić do publikacji produkcyjnej; ten przegląd niczego nie publikuje.

- [ ] Sprawdzono push opt-in/opt-out dla urządzenia, ekran blokady i zmianę konta, nie utożsamiając permission ze zgodą RODO.
- [ ] Zamknięto ocenę opcjonalnego storage i mediów oraz zidentyfikowano dostawców/transfery.
- [ ] Archiwizacja pracownika nie została poświadczona jako wymazanie danych.

## Otwarte warunki przeglądu 2026-10-19

- OWNER_REQUIRED: rzeczywista tożsamość, forma działalności, kontakty, terminy/cena/VAT i zdolność wsparcia; weryfikacja dokumentem i testem kontaktu.
- CLIENT_REQUIRED: tożsamość/umocowanie, podstawy i informacja pracowników, retencja, rzeczywista kontrola generatora, instrukcje i podpisy.
- ACCOUNTANT_REQUIRED: kwartalny limit działalności nierejestrowanej 2026, VAT/NIP/PIT/ZUS, dokumenty i przejściowe KSeF do 2027; decyzja na danych całej działalności.
- LAWYER_REQUIRED: pełna aktualność prawa (w tym ograniczony dostęp EUR-Lex), kontrakty i art. 28, prawa JDG, art. 22/DPIA, AI Act i jego zmiany 2026, DSA/EAA/KSC, prawo pracy, terminy i granice odpowiedzialności.
- INFRASTRUCTURE_REQUIRED: rzeczywiści hosting/SMTP/backup/wsparcie/bramy push, role, regiony, umowy, transfery, TLS, izolacja, szyfrowanie i retencja endpointów.
- PRODUCTION_TEST_REQUIRED: hardening 6-cyfrowych haseł i tokenu localStorage, skuteczność uprawnień/izolacji, restore, exit/usunięcie historii, runtime cookies/storage/media i push offline/zmiana konta/ekran blokady.

Brak mechanizmu prior opt-in dla opcjonalnych zapisów i mediów pozostaje STOP-SHIP dla ich wydania, do wdrożenia albo uzasadnionego wyłączenia operacji. Zakończone delivery push >5 dni nie wyznaczają TTL wszystkich danych. Soft-delete nie wykonuje pełnego erasure. Kod potwierdzeń PDF istnieje; brakującym dowodem jest poprawny zatwierdzony PDF i test produkcji, nie implementacja od zera.

Dokładne pliki, sekcje, pola, powód niemożności ustalenia z kodu, odpowiedzialność, metoda i wpływ znajdują się w rejestrze placeholderów. Nie zmieniono żadnego statusu na APPROVED. Otwarte warunki blokują zatwierdzenie/publikację i produkcję, mimo przygotowania treści do przeglądu.
