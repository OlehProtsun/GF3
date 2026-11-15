# README — pakiet startowy pierwszego Klienta

| Metadane | Wartość |
| --- | --- |
| Produkt | GF |
| Wersja | 0.2.0-draft |
| Status | DRAFT – WYMAGA WERYFIKACJI |
| Właściciel | [DO WYPEŁNIENIA: WŁAŚCICIEL DOKUMENTU] |
| Recenzent | [DO WYPEŁNIENIA: RECENZENT] |
| Data obowiązywania | [DO WYPEŁNIENIA: DATA OBOWIĄZYWANIA] |
| Data przeglądu | 2026-10-19 — przegląd redakcyjny i kodu; bez zatwierdzenia prawnego |
| Dokumenty powiązane | indeks; finalny raport; istniejące dokumenty |
| Klasyfikacja | WEWNĘTRZNY |

> Projekt do weryfikacji prawnej, RODO, księgowej, podatkowej, biznesowej i technicznej w odpowiednim zakresie. Nie stanowi finalnej porady ani gwarancji zgodności lub bezpieczeństwa.


## 1. Struktura

Sekcje 00–10 prowadzą od indeksu przez DPA, prywatność, storage, bezpieczeństwo, backup, launch, support, billing i exit do bramki finalnej.

## 2. Klasy dokumentów

Publiczne informują Użytkowników; umowne wiążą Strony; wewnętrzne są dowodami procesu i nie powinny być publikowane. Klasyfikację ma każdy plik MD.

## 3. Dokumenty podpisywane przez Klienta

Umowa SaaS + Załącznik 1 (katalog istniejący), DPA A–E oraz protokół uruchomienia; pozostałe tylko gdy prawnik wskaże.

## 4. Dokumenty Użytkownika

Regulamin, zagrożenia, privacy i cookies są czytane/udostępniane lub akceptowane według decyzji prawnej. Platforma zapisuje wersje PDF i osobiste potwierdzenia; dla zawarcia Umowy SaaS/DPA i doręczenia informacji pracodawcy wymagane są odrębne dowody.

## 5. Pliki wewnętrzne

Analizy, rejestry, incydenty, backup/BCP, formularze z danymi, billing, gap report i checklisty przechowuje się z ograniczonym dostępem.

## 6. Placeholdery

Zamykaj rejestr 00 w źródłowym pliku, wpisując wartość, właściciela i dowód. Po każdej zmianie ponownie wygeneruj rejestr. Nie zamieniaj niewiadomej na założenie.

## 7. HTML i PDF

Otwórz HTML lokalnie w Chrome/Edge, wybierz Drukuj → Zapisz jako PDF, A4, skala 100%, nagłówki przeglądarki wyłączone. Sprawdź każdą stronę i podpisy; HTML nie wymaga buildu ani internetu.

## 8. Wersjonowanie

Schemat [DO WYPEŁNIENIA: np. major.minor], data obowiązywania, recenzent i changelog. Podpisanej wersji nie nadpisuj.

## 9. Podprocesorzy

Techniczny potwierdza dostawcę/usługę/region/transfer/link; IOD ocenia, prawnik zatwierdza model powiadomienia; zmiana trafia do DPA D, privacy i changelogu przed użyciem.

## 10. Archiwizacja podpisów

Zachowaj niezmienny PDF, dowód podpisu/doręczenia, wersję źródłową, datę i uprawnienia w [DO WYPEŁNIENIA: REPOZYTORIUM DOKUMENTÓW], poza publicznym repo.

## 11. Pakiet dla prawnika

Umowa/Załącznik, Regulamin/zagrożenia, DPA A–E, privacy/cookies, support/exit, macierz, pytania prawne i decyzje — bez sekretów i danych pracowników.

## 12. Pakiet dla księgowego

Umowa i cena/termin, warianty, pytania, instrukcja, szablony i CSV; poproś o pisemną decyzję VAT/KSeF/PIT/ZUS/dokumentu.

## 13. Pakiet uruchomieniowy Klienta

Po recenzjach: podpisowy zestaw, publiczne polityki, formularze wdrożenia/kontaktów, checklisty użytkowników, wsparcie, protokół dostępu i uruchomienia.

## 14. Identyfikacja blokad

Filtruj kolumnę „Blokuje” w rejestrze placeholderów i finalnym raporcie. GO dopiero po zamknięciu wszystkich pozycji podpis/produkcja/fakturowanie i załączeniu dowodów.

Generator odtwarza tylko nowy katalog i nie zmienia aplikacji ani istniejących dokumentów. Status całego pakietu pozostaje DRAFT.

## Mapa źródeł kanonicznych — przegląd 2026-10-19

Status całego zestawu: **DRAFT — REVIEW-READY redakcyjnie i technicznie po walidacji; bez zatwierdzenia prawnego i bez zgody produkcyjnej**. REVIEW-READY oznacza przygotowanie treści do pracy recenzenta, nie zamknięcie zewnętrznych faktów i STOP-SHIP. GF jest produktem, GF3 identyfikatorem technicznym.

| Warstwa | Źródło merytoryczne od katalogu repozytorium | Relacja i pierwszeństwo |
|---|---|---|
| Umowa B2B | docs/legal/saas-agreement/umowa-saas-b2b-pl.md i zalacznik-1-zakres-uslug.md | Jeden negocjowany SaaS; parametry handlowe są propozycją, nie faktem sprzedaży |
| Pełny regulamin | docs/legal/electronic-services-regulations/regulamin-swiadczenia-uslug-elektronicznych-pl.md | Publiczny /legal/regulamin.html jest pełną reprezentacją, nie konkurencyjnym skrótem |
| DPA | docs/client-launch-package/01-data-processing-agreement/umowa-powierzenia-przetwarzania-danych-pl.md oraz A–E | Pierwszeństwo w sprawach ochrony danych; zgoda osoby korzystającej z konta nie zawiera DPA |
| Prywatność i urządzenie | docs/client-launch-package/02-privacy/polityka-prywatnosci-pl.md; 03-cookies-and-storage/polityka-cookies-pl.md | Publiczne HTML pod /legal/ są stabilną reprezentacją tej treści; informacja pracodawcy pozostaje odrębna |
| Publiczne strony uzupełniające | FrontEnd/public/legal/index.html, zasady-korzystania.html, podwykonawcy.html, bezpieczenstwo.html | Uzupełniające informacje; nie samodzielny zamiennik SaaS/DPA lub źródło prywatnych danych |
| Ewidencja wewnętrzna | Analiza ról, mapa, retencja, rejestry, COOKIE_AUDIT, ryzyko i procedury oraz raport 2026-10-19 | Fakty kodu, interpretacje i luki; nie publikować poufnych rejestrów w public assets |
| Wzory alternatywne | docs/legal/templates | Wskazane krótkie DPA/TOM/umowa/klauzula/SLA są historycznymi wzorami pomocniczymi; aktywne zobowiązania pochodzą z dokumentów kanonicznych |
| Operacje / oferta | docs/client-launch-package/04–10; docs/commercial-offer | Procedury, formularze i materiał handlowy; wypełnione umowy i dowody poza Git; nie zmieniają samodzielnie kontraktu |

Bezwzględnie obowiązujące prawo jest nadrzędne. DPA ma pierwszeństwo w ochronie danych; w pozostałym zakresie indywidualny aneks, SaaS, uzgodniony zakres, regulamin i pomocnicze zasady. Wybranie pustego szablonu nie tworzy równoległych zobowiązań.

## Edycja i generacja

Edytować Markdown dokumentu kanonicznego. Generatory saas-agreement i electronic-services-regulations aktualizują jego prywatny HTML oraz informacje o pliku w README. generate-package.mjs czyta istniejące źródła, aktualizuje cztery pochodne HTML, publiczny regulamin/privacy/cookies oraz rejestr pól; **nie odtwarza i nie nadpisuje Markdown starymi wzorami**. Pozostałe publiczne HTML są bezpośrednio redagowane. Zmiana publicznej treści unieważnia poprzednie zatwierdzenie dokładnych bajtów; nie wpisano nowego APPROVED ani zatwierdzonego SHA.

Odrębny PDF do potwierdzeń przygotować wyłącznie po przeglądzie: wydruk zatwierdzonej treści, kontrola każdej strony, numeru, daty i hasha, archiwum i akceptacja uprawnionych osób. Nie kopiować do PDF gate Umowy SaaS, DPA, TOMs, faktur, podpisów lub informacji prywatnej. W tym zadaniu nie wygenerowano i nie wgrano PDF, nie publikowano dokumentów i nie zmieniono infrastruktury.
