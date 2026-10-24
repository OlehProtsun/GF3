# Prawa osób, retencja, usuwanie

WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ

Wersja: 0.2.0-draft | Obowiązuje od: [DO UZUPEŁNIENIA]
Operator: [DO UZUPEŁNIENIA]

Przyjęcie, proporcjonalna weryfikacja tożsamości, ustalenie administratora, rejestr terminów. Dane pracodawcy kieruj do niego i wspieraj instrukcje. Własny administrator odpowiada bez zbędnej zwłoki, co do zasady do miesiąca; przedłużenie o dwa miesiące w warunkach art. 12 wymaga informacji i uzasadnienia w pierwszym miesiącu. Oceń przesłanki praw, zakres i eksport bez danych osób trzecich. Live DB/pliki/kopie, wyjątki retencji, metoda usunięcia i ponowienie po restore: [DO UZUPEŁNIENIA]. Właściciele, bezpieczny kanał, minimalny dziennik i dowody: [DO UZUPEŁNIENIA].

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
