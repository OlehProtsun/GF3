# Procedura zakończenia współpracy

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | DPA E; Umowa; eksport; usunięcie |
| Klasyfikacja | POUFNY UMOWNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## 1. Inicjacja

Podstawa i data końca Umowy **[DO WYPEŁNIENIA]**, okres wypowiedzenia zgodny z Umową, osoba decyzyjna i zakres danych **[DO WYPEŁNIENIA]**. Zamrozić nieuzgodnione zmiany i otworzyć kontrolowaną sprawę exit.

## 2. Plan

1. Potwierdzić ostatni dzień dostępu, zakres eksportu, format, odbiorcę i bezpieczny kanał.
2. Wykonać eksport i weryfikację; projekt Umowy wskazuje roboczo 14 dni — zsynchronizować.
3. Rozliczyć otwarte płatności, zgłoszenia, podprocesorów i nośniki.
4. Po potwierdzeniu odbioru odebrać konta managerów/pracowników, unieważnić sesje i dostępy techniczne.
5. Usunąć lub zanonimizować dane aktywne według instrukcji; projekt Umowy wskazuje roboczo 45 dni.
6. Kopie objąć wygasaniem według cyklu **[DO WYPEŁNIENIA]**, bez przywracania do bieżącej pracy.
7. Wydać protokół usunięcia/wyjątków i zamknąć rejestry.

## 3. Wyjątki

Zachowanie wymagane prawem, sporem lub zabezpieczeniem roszczeń wymaga celu, podstawy, zakresu, dostępu i terminu. Brak automatycznej „natychmiastowej” eliminacji z rotacyjnych backupów należy wyjaśnić Klientowi.

## Podstawy i materiały do weryfikacji

- [RODO – art. 5, 24–36](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [UODO – materiały o naruszeniach](https://uodo.gov.pl/pl/598/3563)
- [Wytyczne EROD 07/2020](https://www.edpb.europa.eu/documents/guideline/guidelines-072020-on-the-concepts-of-controller-and-processor-in-the-gdpr_en)

## Wykonanie żądania — procedura obowiązująca po przyjęciu

1. Osoba obsługująca rejestruje datę, zakres, administratora i termin odpowiedzi. Weryfikuje tożsamość proporcjonalnie, bez rutynowego żądania kopii dowodu. Nie wymaga podania hasła lub tokenu.
2. Własny administrator rozpatruje żądanie bez zbędnej zwłoki, zasadniczo w miesiąc. Przy złożoności/liczbie żądań zawiadamia w tym miesiącu o uzasadnionym przedłużeniu o dwa dalsze miesiące. Odmowa lub opłata wyłącznie według art. 12 ust. 5; odmowa zawiera przyczyny i informację o skardze oraz ochronie sądowej.
3. Procesor nie rozstrzyga samodzielnie praw względem danych Klienta. Przekazuje żądanie bez zbędnej zwłoki właściwemu administratorowi i zachowuje dowód przekazania; pomaga wykonać jego udokumentowaną decyzję tak, aby terminy mogły zostać dotrzymane.
4. Zakres wyszukania obejmuje profil/konto, grafiki, dostępność, korekty i zamiany, historyczne migawki i wersje, komunikaty/odczyty/notatki, potwierdzenia PDF, metadane auth, push subscriptions/deliveries, logi, eksporty, zgłoszenia, pocztę i kopie. Kontrola informacji o innych osobach poprzedza wydanie kopii.
5. Rozpatruje się odrębnie art. 15–22: sprostowanie bez fałszowania historii, usunięcie z wyjątkami art. 17 ust. 3, ograniczenie zamiast przedwczesnego wymazania, powiadomienie odbiorców według art. 19, przenoszenie wyłącznie przy właściwej podstawie i sprzeciw według art. 21. Nie obiecuje się ogólnego prawa do wszystkich plików SQL.
6. Wykonanie weryfikuje druga upoważniona osoba; odpowiedź przekazuje się bezpiecznym kanałem zweryfikowanemu odbiorcy. Rejestr zawiera minimalne dane, decyzję i referencję dowodu poza Git. Kontakty i upoważnienia: [OWNER_REQUIRED: administrator procedury i bezpieczny kanał].

## Usuwanie i ponowienie po odtworzeniu

Usunięcie pracownika oznacza archiwizację profilu przez IsDeleted oraz usunięcie konta pracownika. Nie oznacza pełnego wymazania profilu, kontaktów, historycznych grafików, dostępności, zamian, korekt, dowodów akceptacji, eksportów lub kopii. Wykonanie prawa do usunięcia wymaga ustalenia podstaw retencji, instrukcji Klienta i odrębnej kontroli powiązań; nie należy zastępować archiwizacji deklaracją anonimizacji.

Nie modyfikować niezmiennego PDF lub historycznego potwierdzenia w zwykłym UI w celu pozornego wykonania żądania. Administrator ustala prawny zakres dowodów i termin; jeśli prawo wymaga usunięcia, potrzebne są kontrolowana metoda techniczna i test zgodności zależności. Wydzielenie danych przechowywanych prawnie nie pozwala używać ich w bieżącym planowaniu.

Przy exit obowiązuje wybór Administratora z DPA E. Kopie oznacza się do wygaśnięcia w uzgodnionym maksymalnym terminie, blokuje normalny dostęp i odtwarzanie do pracy. Rejestr usunięć przechowuje się minimalnie i stosuje ponownie po restore. Push należy zatrzymać przed restore, zweryfikować endpointy i usunąć nieaktualne subskrypcje przed wznowieniem. Protokół opisuje każdą warstwę i wyjątek; nie poświadcza niewykonanego fizycznego wymazania.
