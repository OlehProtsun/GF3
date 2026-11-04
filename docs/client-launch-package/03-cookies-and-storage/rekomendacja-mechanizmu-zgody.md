# Rekomendacja mechanizmu zgody

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | audyt i polityka cookies |
| Klasyfikacja | WEWNĘTRZNY / DECYZYJNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Rekomendacja

Wybrać **Wariant B** dla zewnętrznych mediów: nie ładować iframe YouTube ani zewnętrznego obrazu przed wyborem; pokazać neutralny placeholder „Załaduj treść zewnętrzną”, równorzędne opcje akceptacji/odrzucenia, kategorię „Media zewnętrzne”, możliwość wycofania i zapis wersji zgody. Nie oznaczać zgody z góry.

Wariant A – sama informacja bez bannera – jest właściwy tylko wtedy, gdy zewnętrzne media zostaną wyłączone/proxyowane bez strony trzeciej, a audyt produkcyjny potwierdzi wyłącznie technologie ściśle niezbędne. localStorage auth nie wymaga dekoracyjnej zgody, ale wymaga informacji i oceny bezpieczeństwa.

Kryteria: brak żądania do domeny zewnętrznej przed wyborem; odmowa nie blokuje podstawowej Platformy; zmiana decyzji jest dostępna; wersja i czas wyboru są zapisane; polityka zawiera aktualną listę. **[DO WERYFIKACJI PRAWNEJ: PODSTAWA I MODEL ZGODY]**

## Podstawy i materiały do weryfikacji

- [RODO](https://eur-lex.europa.eu/legal-content/PL/TXT/?uri=CELEX:32016R0679)
- [Ustawa o świadczeniu usług drogą elektroniczną – ELI](https://eli.gov.pl/eli/DU/2024/1513/ogl)
- [biznes.gov.pl – działalność nierejestrowana](https://biznes.gov.pl/pl/firma/zakladanie-firmy/chce-wiedziec-jak-zalozyc-wlasna-firme/dzialalnosc-nierejestrowa-oraz-inne-sytuacje-w-ktorych-nie-trzeba-rejestrowac-firmy)

## Wszystkie kategorie pamięci, nie tylko media

Ta rekomendacja obejmuje również trwały login, tryb hasła, piny, układ kolumn, presety, odczyty i zapamiętanie odłożenia zaproszenia. Wykaz storage zawiera indywidualną ocenę art. 399 PKE. Dla każdej operacji trzeba wykazać konieczność dla wyraźnie żądanej usługi albo blokować ją przed opt-in. Deklaracja „funkcjonalne” nie stanowi wyjątku. Dla push rozdzielić wybór usługi i permission urządzenia od podstawy RODO i dodatkowej pamięci. Wariant bez bannera dopuszczalny tylko po wykazaniu wyjątków i braku pozostałych operacji wymagających zgody.
