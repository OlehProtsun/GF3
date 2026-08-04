# Audyt cookies i pamięci przeglądarki

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | [DO WYPEŁNIENIA: NUMER WERSJI] |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | [DO WYPEŁNIENIA: DATA PRZEGLĄDU] |
| Dokumenty powiązane | wykaz storage; polityka cookies |
| Klasyfikacja | WEWNĘTRZNY / TECHNICZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

## Wynik

Nie znaleziono kodu ustawiającego własne HTTP cookies, sessionStorage, IndexedDB, service worker, analitykę, piksele, telemetrykę, chat ani menedżer zgód. Uwierzytelnianie używa JWT w localStorage. Interfejs zapisuje również nazwę Użytkownika, tryb logowania, odczytane powiadomienia, kolejność kolumn i przypięte rekordy.

Moduł aktualności może automatycznie osadzać iframe z youtube-nocookie.com i ładować dowolny zewnętrzny imageUrl. To powoduje połączenie przeglądarki z osobą trzecią; faktyczne cookies/storage i transfer zależą od dostawcy i wymagają testu sieciowego.

## Braki

- Brak publicznej polityki i mechanizmu zgody/aktywacji mediów.
- Brak automatycznego inwentarza storage i retencji tokenu.
- JWT w localStorage zwiększa skutki podatności XSS.
- Cookies proxy/load balancera i produkcji nie zostały zweryfikowane.

**[DO WERYFIKACJI TECHNICZNEJ: AUDYT PRODUKCYJNY DEVTOOLS I NAGŁÓWKÓW SET-COOKIE]**
