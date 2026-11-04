# Checklista onboardingu Użytkowników

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | akceptacja dokumentów; dostęp |
| Klasyfikacja | WEWNĘTRZNY / DO UZGODNIENIA |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

- [ ] Użytkownik potrzebuje dostępu i zna rolę; dane są prawidłowe i minimalne.
- [ ] Konto utworzył upoważniony manager; login przekazano oddzielnie od hasła.
- [ ] Użytkownik otrzymał regulamin, politykę prywatności, cookies/storage i instrukcję bezpieczeństwa w aktualnej wersji.
- [ ] Sprawdzono zapis osobistego potwierdzenia PDF i historię; odrębnie udokumentowano podpis SaaS/DPA i doręczenie informacji pracodawcy.
- [ ] Użytkownik zmienił/chroni kod dostępu, nie udostępnia go i zna odzyskiwanie.
- [ ] Sprawdzono tylko potrzebną placówkę/harmonogram i zakaz wpisywania danych wrażliwych do notatek.
- [ ] Użytkownik zna wsparcie, incydenty, wylogowanie i alternatywny kanał grafiku.
- [ ] Przy odejściu konto/sesje są niezwłocznie odebrane, a potrzebne dane zachowane zgodnie z retencją.

## Weryfikacja rozszerzonego zakresu

Platforma rejestruje osobiste potwierdzenie opublikowanej wersji PDF przez konto menedżera lub pracownika: identyfikator konta, rolę, migawkę loginu i nazwy oraz czas UTC. Dokument ma wersję, SHA-256 i czas publikacji; opublikowany dokument jest niezmienny, a przyjęte lub opublikowane dokumenty nie podlegają zwykłemu usunięciu. Dostępna jest historia własna i historia pracownika dla menedżera. Middleware zwraca HTTP 428 dla chronionych operacji API przy oczekujących dokumentach, z wyjątkami uwierzytelnienia, pobrania i potwierdzenia dokumentów oraz wskazanych tras. Gate prezentuje PDF i pole domyślnie niezaznaczone. Nie dowodzi to przeczytania każdej strony ani kwalifikowanego podpisu; nie podpisuje Umowy SaaS lub DPA w imieniu Klienta i nie jest ogólną zgodą RODO. Tylko prawnie zweryfikowany PDF może trafić do publikacji produkcyjnej; ten przegląd niczego nie publikuje.

- [ ] Sprawdzono push opt-in/opt-out dla urządzenia, ekran blokady i zmianę konta, nie utożsamiając permission ze zgodą RODO.
- [ ] Zamknięto ocenę opcjonalnego storage i mediów oraz zidentyfikowano dostawców/transfery.
- [ ] Archiwizacja pracownika nie została poświadczona jako wymazanie danych.
