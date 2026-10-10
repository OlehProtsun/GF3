# Procedura odtwarzania danych

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | procedura backup; test; BCP |
| Klasyfikacja | POUFNY WEWNĘTRZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Warunki wejścia

Zgłoszenie/zmiana zatwierdzona przez **[DO WYPEŁNIENIA: ROLA]**, określony zakres i punkt w czasie, ocena incydentu, komunikacja z Klientem oraz bezpieczna kopia obecnego stanu. Nie wykonywać przywracania na produkcji bez jawnego zatwierdzenia.

## Przebieg

1. Zatrzymaj zapisy lub odizoluj środowisko zgodnie z zatwierdzonym planem.
2. Wybierz kopię z katalogu na podstawie czasu, integralności i retencji; udokumentuj identyfikator.
3. Odtwórz najpierw w odseparowanym środowisku **[DO WYPEŁNIENIA]**.
4. Zweryfikuj otwarcie bazy, integralność, wersję schematu, kluczowe liczności i reprezentatywne operacje bez ujawniania danych.
5. Zatwierdź przełączenie, wykonaj kontrolowane przywrócenie i test smoke.
6. Monitoruj, przekaż wynik Klientowi, odnotuj RPO/RTO rzeczywiste i zamknij zmianę.

## Cofnięcie i awaria

Jeśli walidacja nie przejdzie, nie promuj kopii; zachowaj stan, wróć do ostatniego bezpiecznego punktu i eskaluj. Konkretne polecenia i ścieżki serwera pozostają w chronionym runbooku technicznym, nie w pakiecie klienta.
