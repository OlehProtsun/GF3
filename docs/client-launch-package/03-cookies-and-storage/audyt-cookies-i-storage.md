# Audyt cookies i pamięci przeglądarki

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | wykaz storage; polityka cookies |
| Klasyfikacja | WEWNĘTRZNY / TECHNICZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Wynik przeglądu kodu 2026-10-19

Service worker /sw.js istnieje i obsługuje push/notificationclick, bez fetch handlera lub Cache API. Istnieją publiczne polityki /legal/*.html, wersjonowanie PDF i historia akceptacji. Nie znaleziono własnych zapisów HTTP cookies, sessionStorage, IndexedDB ani reklamowej analityki w kodzie produktu. Nie jest to dowód nieobecności cookies w produkcji.

Kompletny inwentarz kodu i kwalifikację każdej kategorii zawiera [wykaz](wykaz-cookies-i-storage.md), a dowody z symbolami i ograniczeniami [raport](../../legal/LEGAL_COMPLETION_REPORT_2026-10-19.md). Poprzedni audyt COOKIE_AUDIT z 2026-10-08 dotyczył lokalnych stron/logowania i nie jest aktualnym testem produkcji.

## Blokery

Automatyczne osadzenia mediów, trwałe preferencje bez udokumentowanej konieczności/prior opt-in, JWT w localStorage oraz nieznane cookies proxy wymagają odrębnego rozwiązania i testu. Brak pełnego mechanizmu zgody jest STOP-SHIP dla zakresu wymagającego zgody; nie naprawia go sama zmiana tekstu polityki.

## Protokół produkcyjny do wykonania

Na świeżym profilu obu ról zarejestrować sieć, Set-Cookie, cookies wszystkich domen, local/sessionStorage, IndexedDB, cache i service worker przed logowaniem, po odmowie, po zgodzie oraz po jej wycofaniu. Osobno sprawdzić media, włączenie push, wyłączenie offline/online, wylogowanie i zmianę konta na urządzeniu. Zapisać wyłącznie nazwy, cele, domeny i terminy, bez tokenów, kluczy, endpointów i realnych danych. Właściciel, domena, wersja wdrożenia i wynik: [PRODUCTION_TEST_REQUIRED: protokół audytu domeny].
