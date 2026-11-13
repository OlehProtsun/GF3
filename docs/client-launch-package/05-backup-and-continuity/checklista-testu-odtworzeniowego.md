# Checklista testu odtworzeniowego

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | rejestr testów; odtwarzanie |
| Klasyfikacja | POUFNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.

- [ ] Zakres, cel, kopia i środowisko testowe zatwierdzone; produkcja chroniona.
- [ ] Kopia ma czas, identyfikator, sumę/integralność i znaną retencję.
- [ ] Dostępy czasowe nadane minimalnie; dane testowe zabezpieczone.
- [ ] Start/koniec i wszystkie błędy zapisane.
- [ ] Baza otwiera się, schemat zgodny, kluczowe rekordy i relacje spójne.
- [ ] Logowanie testowe, odczyt harmonogramu, zapis kontrolny i eksport sprawdzone bez realnego wysyłania komunikatów.
- [ ] Osiągnięte RPO/RTO porównane z celem.
- [ ] Dane testowe po zakończeniu usunięte zgodnie z decyzją; wynik, dowody i działania zaakceptowane.

## PWA, push i retencja w odtwarzaniu

Kopia SQLite zawiera także dane subskrypcji i doręczeń push oraz potwierdzenia regulaminów. Przy odtwarzaniu testowym zablokować wysyłkę e-mail i push, używać środowiska izolowanego i danych syntetycznych, a przed wznowieniem produkcji ponowić wykonane usunięcia oraz zweryfikować aktywność endpointów i kont. Sam restore starego wolumenu nie może wznowić subskrypcji po ich wycofaniu. Sprawdzić niezmienność PDF i spójność historii, odczytów i wersji grafików.

Nie zmieniać GF3_DATA_VOLUME, nie usuwać gf3-data i nie stosować docker compose down -v w ramach aktualizacji. Najpierw bezpieczna kopia i potwierdzenie celu operacji. To wymóg proponowanej procedury, a nie dowód jej wykonania. RPO/RTO, separacja kopii, szyfrowanie i rzeczywisty test pozostają INFRASTRUCTURE_REQUIRED / PRODUCTION_TEST_REQUIRED. Cel ćwiczenia proponuje się co najmniej raz na kwartał i po istotnej zmianie; częstotliwość wymaga zatwierdzenia operatora.
