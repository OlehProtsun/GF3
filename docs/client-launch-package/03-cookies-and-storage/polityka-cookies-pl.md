# POLITYKA COOKIES I PAMIĘCI PRZEGLĄDARKI GF

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | polityka prywatności; wykaz storage |
| Klasyfikacja | PUBLICZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## 1. Zakres i kontakt

Za technologie GF odpowiada **[OWNER_REQUIRED: tożsamość Operatora i kontakt prywatności]**. Polityka dotyczy cookies, localStorage, pamięci przeglądarki, service worker, Web Push i zewnętrznych mediów. Art. 399 PKE dotyczy także informacji innych niż dane osobowe oraz pamięci innej niż cookies HTTP.

## 2. Własna pamięć aplikacji

Token gf3.auth.access-token służy do sesji, a gf3.auth.last-username i gf3.auth.password-mode zapamiętują login i tryb wpisywania hasła. Pierwszy wpis jest usuwany przez wylogowanie lub unieważnienie sesji; wpis nie ma własnego TTL przeglądarki. Login i tryb pozostają do zmiany lub usunięcia.

gf3.employee-notifications.read.* przechowuje identyfikatory odczytów powiązane z loginem/ID pracownika. Datowane wpisy są filtrowane po 5 dniach; starszy format może pozostać. gf3:employee-availability:time-presets:* zapisuje presety godzin, gf3:employee-schedule-column-order:* kolejność kolumn, a employees:list:pinned, shops:list:pinned, containers:list:pinned i availability-groups:list:pinned identyfikatory przypiętych rekordów. Ustawienia te nie mają automatycznego czasu usunięcia.

gf3.push.employeeId łączy urządzenie z kontem przy włączonym push; funkcja wyłączenia usuwa ten klucz. gf3.push.invitation.{employeeId} przechowuje czas odłożenia zaproszenia do powiadomień o 7 dni. Upływ tego czasu nie usuwa fizycznie klucza. Własne zapisy nie służą reklamie. Sam cel wygody nie oznacza jednak ustawowej niezbędności.

## 3. PWA i Web Push

GF ma manifest instalacji i service worker /sw.js z zakresem /. Skrypt obsługuje odbiór powiadomień i kliknięcie; nie potwierdzono własnej pamięci Cache API ani trybu pracy całej aplikacji offline. Przeglądarka stosuje własny cache zasobów zgodnie z odpowiedziami HTTP i ustawieniami.

Web Push wymaga świadomego włączenia przez pracownika, obsługiwanej przeglądarki i jej uprawnienia do powiadomień. Przeglądarka przechowuje subskrypcję, endpoint i klucze szyfrujące; serwer zapisuje ich kopię z identyfikatorem pracownika oraz metadane doręczeń. Uprawnienie przeglądarki nie jest ogólną zgodą RODO ani zgodą cookies. Włączenie dotyczy konkretnego urządzenia/profilu. Wyłączenie w GF podejmuje próbę usunięcia po stronie serwera i unsubscribe w przeglądarce; błąd sieci może opóźnić usunięcie rekordu serwerowego. Osobno można cofnąć permission w ustawieniach witryny. Szczegóły treści, bram push i retencji opisuje [polityka prywatności](/legal/polityka-prywatnosci.html).

## 4. Media, cookies i odbiorcy

Aktualności mogą ładować iframe youtube-nocookie.com oraz obrazy z hostów wskazanych w treści. Powoduje to połączenie z dostawcą, ujawniające co najmniej adres IP i dane żądania. Nazwa domeny „nocookie” nie dowodzi braku technologii urządzenia ani transferu. Faktyczne zapisy i odbiorcy wymagają testu.

Nie znaleziono w kodzie produktu własnego ustawiania HTTP cookies, sessionStorage, IndexedDB ani narzędzi reklamowych/analitycznych. Nie przesądza to o cookies hostingu, proxy, przeglądarki lub zewnętrznych mediów. **[PRODUCTION_TEST_REQUIRED: wykaz cookies i technologii rzeczywistej domeny, dostawców i terminów]**.

## 5. Niezbędność, wybór i usuwanie

Zapis lub odczyt ściśle niezbędny do transmisji albo usługi wyraźnie żądanej przez użytkownika może podlegać wyjątkowi art. 399 ust. 3 PKE. Dotyczy to odpowiednio niezbędnej sesji i mechanizmów obsługi świadomie żądanego push. Nie kwalifikuje automatycznie trwałego loginu, pinów lub układu kolumn. Pozostałe operacje wymagają uprzedniej informacji i ważnej zgody, o ile nie ma innego właściwego wyjątku. Zgoda musi być dobrowolna, konkretna, możliwa do odrzucenia i wycofania równie łatwo jak udzielenie.

**Stan projektu:** obecna aplikacja nie ma potwierdzonego kompletnego mechanizmu blokowania opcjonalnej pamięci i mediów przed wyborem. Przed wydaniem należy go wdrożyć albo usunąć te operacje z wydawanego zakresu oraz potwierdzić wyjątki dla pozostałych. Niniejszy tekst ani zamknięcie komunikatu nie stanowią zgody.

Użytkownik może usunąć dane witryny, cache i rejestracje service worker w ustawieniach przeglądarki. Może to wylogować i skasować lokalne ustawienia, lecz nie usuwa automatycznie danych serwera ani uprawnień systemowych we wszystkich przeglądarkach. Po pracy na urządzeniu wspólnym należy wylogować się, wyłączyć push i sprawdzić podgląd powiadomień na ekranie blokady. Blokowanie pamięci może ograniczyć zapamiętywanie sesji; zakres rzeczywistego wpływu zależy od przeglądarki.
