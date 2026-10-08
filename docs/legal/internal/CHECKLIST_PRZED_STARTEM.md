# STOP-SHIP — przed uruchomieniem

WERSJA ROBOCZA — DO WERYFIKACJI PRAWNEJ

Wersja: 0.1.0-draft | Obowiązuje od: [DO UZUPEŁNIENIA]
Operator: [DO UZUPEŁNIENIA]

Zielone testy nie zatwierdzają poniższych warunków.

- [ ] Tożsamość: CEIDG/KRS, nazwa/adres/NIP/kontakt, PKD, podatki. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Prawo: Zamówienie/DPA, umocowanie i umowy dostawców; ochrona B2C oraz niezawodowych umów JDG. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] RODO: Role, art. 13/28/30, ryzyko/DPIA, retencja, DSAR, dowody EOG/legalnych transferów. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Bezpieczeństwo: Hasło bez fallback, nieprzewidywalne PIN, rate limiting/blokady, monitoring, JWT i backup, TLS, least privilege, restore, aktualizacje, izolacja, incident runbook i logi dostępu/eksportu. Sześciocyfrowe hasła i token localStorage wymagają osobnego przeglądu/hardeningu przed danymi pracowników. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Przeglądarka: Runtime audyt, art. 399 PKE; opcjonalne trackery wyłączone do prior opt-in; marketing art. 398 odrębnie. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Handel/księgowość: Podpisane ceny/faktury/zakończenie, VAT/OSS/VAT-UE, KSeF 2026 i przejściowe progi; brak fikcyjnego billing. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Operacje: Domena/TLS, kontakt, retencja/restore/usunięcie, okno eksportu, realne obietnice, odpowiedzialność i test kontaktu incydentowego. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Zakres regulacji: DSA dla hostingu/treści, EAA/polskie e-commerce B2B/B2C, NIS2/KSC, dokumentacja zatrudnienia i monitoring wysokiego ryzyka; ocena stosowania/niestosowania z uzasadnieniem. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Publikacja: Uzupełnienia, prawnik/wersja/hash, zatwierdzony PDF/podpisana umowa, release validator, mobile/desktop/klawiatura/anonymous; wcześniejsze wersje archiwizowane poza repo. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

- [ ] Granica klienta: Osobna baza/wolumen/sekrety/domena/backup i test izolacji. Nie mieszać niepowiązanych pracodawców w jednej SQLite. Właściciel / dowód URL lub ścieżka / termin / status / podpis prawnika lub księgowego: [DO UZUPEŁNIENIA].

## Proceduralne bramki klienta — dowody poza Git/public

Poniższe warunki uzupełniają wszystkie powyższe STOP-SHIP, nie zastępują ich. Spotkanie może być wyłącznie demonstracją na danych syntetycznych; nie uprawnia do przyjęcia rzeczywistych danych pracowników ani uruchomienia produkcji. Każdy wiersz wymaga wskazanej osoby, chronionego miejsca dowodu, rzeczywistej daty testu/przeglądu i statusu. Statusy: PENDING / PASS / FAIL / BLOCKED / NOT VERIFIED. Nie wpisywać PASS bez dowodu ani sekretów/danych pracowników w repo.

| Bramka / niezależnie sprawdzalny warunek | Odpowiedzialność do przypisania | Właściciel | Chronione miejsce dowodu | Data testu / przeglądu | Status |
|---|---|---|---|---|---|
| Spotkanie: syntetyczne demo, DRAFT oznaczone; kartka wymagań bez zbierania list personelu przed DPA i bezpieczną transmisją | Operator | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Operator verified: prawdziwa tożsamość i dopuszczalny status działalności stałego SaaS potwierdzone przez prawnika/księgowego | Operator + prawo/finanse | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Client representative verified: dane rejestrowe klienta i umocowanie podpisującego sprawdzone ze źródłem | Klient + prawny właściciel | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Single executed SaaS contract: jeden zatwierdzony/podpisany SaaS i zakres; ewentualna okładka jawnie zgodna z nim; kopie/podpisy/doręczenie zachowane | Umocowane strony + prawnik | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Executed DPA/TOMs: finalny zakres danych/instrukcji, rzeczywiste środki i retencja zatwierdzone/podpisane przed produkcyjnymi danymi | Klient-administrator + operator-procesor | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Support and finance ready: cena/waluta/podatki/ZUS/VAT/KSeF/rachunek i pierwszy okres sprawdzone; kanał wsparcia/eskalacja działa; SLA tylko uzgodnione | Finanse + operator + klient | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Subprocessors accounted for: hosting/SMTP/backup/wsparcie, regiony, umowy, zgody i transfery potwierdzone | Privacy/techniczny właściciel | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Employee notice approved and delivered: pracodawca zatwierdził własne cele/podstawy i informację, udokumentował doręczenie; nie blankietowa zgoda checkbox | Pracodawca + HR/privacy | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Separate production instance verified: domena/DB/volume/sekrety/backup wyłącznie klienta; test izolacji i restore na oddzielnym środowisku | Techniczny/security właściciel | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Published PDF approved: zgodny z finalnymi publicznymi warunkami, tytuł/wersja/data/hash/review, właściwe role; bez SaaS/DPA/TOMs/faktur/zgody GDPR; stare PDF/history zachowane | Prawnik + operator + system manager | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Test user acceptance/428/history: świeże syntetyczne manager/employee → należny pending → pobranie → pusty checkbox → API 428 → accept wszystkich → API działa; nowa wersja ponownie wymagana, historia własna i employee dla managera po gate w granicach uprawnień | Tester + system manager | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Dry-run and handover: grafiki/swap działają po accept, manager przeszkolony, dostęp przekazany bezpiecznie; faktyczny protokół podpisany przez umocowane strony, otwarte kwestie sklasyfikowane | Operator + upoważniony klient | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Exit/export/delete process validated: rzeczywisty format i terminy, odebranie dostępu, zwrot/usunięcie według DPA, wyjątki i cykl kopii; dowód testu | Operator + klient-administrator | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |
| Incident contact tested: kontakt obu stron odebrał próbne zgłoszenie, eskalacja i powiadomienie bez zbędnej zwłoki zgodne z DPA | Security/privacy obu stron | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] | [DO UZUPEŁNIENIA] |

Decyzja go/no-go / osoba uprawniona / data / chronione dowody / niezakończone blokery: [DO UZUPEŁNIENIA]. Nierozwiązany STOP-SHIP oznacza NO-GO dla production; podpis protokołu lub zielone testy nie znoszą blokady.

Materiały: [playbook UA](../OPERATOR_ONBOARDING_PLAYBOOK_UA.md), [karta klienta](../templates/Karta_Wdrozenia_Klienta_Wzor.md), [protokół przekazania](../templates/Protokol_Uruchomienia_i_Przekazania_Wzor.md), [rejestr przeglądu](../LEGAL_REVIEW.md).
