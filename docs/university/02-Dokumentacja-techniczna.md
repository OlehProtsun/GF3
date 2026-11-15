# GF — Dokumentacja techniczna

Projekt techniczny: GF3. Źródło: `DEV2 @ 06cb43083939fecab4c95bb27bfa094fec13055f`. Odbiorcy: programista, administrator wdrożenia, kierownik projektu i dział IT klienta. Dokument opisuje implementację, nie potwierdza wdrożenia u klienta.

- Architektura
- API i schematy DTO
- Baza danych
- Biblioteki wewnętrzne
- Zależności
- Środowiska
- DEV, TEST, PROD

## 1. Architektura

```text
Przeglądarka / PWA (React SPA)
       │ względne /api, JSON, Bearer JWT; SignalR /api/realtime/presence
       ▼
nginx (PROD, HTTP lub HTTPS)
       ▼
ASP.NET Core WebApi — statyczny bundle React + API
       │ uwierzytelnienie → autoryzacja → middleware → kontrolery/adaptacja DTO
       ▼
BusinessLogicLayer — usługi/fasady, walidacja, generator, eksport
       ▼
DataAccessLayer — repozytoria, AppDbContext / EF Core 10
       ▼
SQLite — plik bazy i lokalne kopie w trwałym workspace
```

Menedżer definiuje sklepy/pracowników, zbiera dyspozycyjność, konfiguruje i generuje grafik, weryfikuje oraz publikuje. API sprawdza rolę, tryb pracy, potwierdzenie wymaganych regulaminów i reguły domenowe przed utrwaleniem. Pracownik odczytuje opublikowane dane swojego konta; wysyła dyspozycyjność, ofertę wymiany lub wniosek korekty. Walidacja i zapis poprzedzają powiadomienia/inwalidację widoków. Wzajemna wymiana przenosi oba okresy po akceptacji; samo preview nie zapisuje oferty. Korekta wymaga decyzji menedżera i aktualności pierwotnej zmiany.

SPA odpowiada za nawigację/formularze/prezentację, API za granicę HTTP i dostęp, BLL za reguły/orkiestrację, DAL za relacje i trwałość. SignalR obsługuje obecność, zmiany danych, synchronizację blokad i sesji; nie zastępuje SQLite. Blokady edycji są w pamięci procesu, bez udokumentowanej dystrybucji między replikami. Windows Forms `GF3.Launcher` to osobny lokalny bootstrap/tray uruchamiający aplikację przeglądarkową, nie wariant produkcyjnego nginx.

## 2. API

Kontrolery HTTP JSON pod `/api`; pliki eksportów/PDF mają własne MIME. W Development dostępne `/swagger` i `/swagger/v1/swagger.json`. SignalR `/api/realtime/presence` to osobny hub uwierzytelniony, nie dodatkowy kontroler HTTP z poniższej tabeli. Połączenie jest zamykane po wygaśnięciu uwierzytelnienia.

Uwierzytelnienie: własny `JwtAuthenticationHandler`/`JwtTokenService`, nagłówek `Authorization: Bearer <TOKEN_SESJI>`. Nie jest to ASP.NET Identity ani OAuth/OIDC. Walidowane są podpis, ważność, dane sesji, wersja poświadczeń menedżera i wersja sesji pracownika; usunięte/unieważnione konto nie zachowuje dostępu tylko dzięki staremu tokenowi. `AuthAttemptLimiter` ogranicza próby logowania/odzyskiwania (429). Login zwraca token, datę wygaśnięcia i sesję; wylogowanie HTTP zwraca 204, klient usuwa sesję.

Globalny filtr MVC wymaga uwierzytelnienia, z wyjątkami `[AllowAnonymous]`. Oznaczenia tabel: **M** — manager, **E** — employee, **A** — dowolna uwierzytelniona sesja, **S** — polityka `GF3.SystemManager` (manager z `gf3.system_manager=true`), **anon** — anonimowy. Uprawnienia dotyczą też właściciela/identyfikatora konta i dostępności rekordu, nie tylko roli.

Niezależnie od tabel działa bramka regulaminów: 428 `regulations_acceptance_required`, `pendingUrl=/api/regulations/pending`, jeśli istnieją wymagane niepotwierdzone dokumenty. Wyjątki obejmują `/api/auth`, GET health/pending/własna historia, GET PDF i POST accept konkretnego regulaminu; menedżer systemowy ma też wyjątek dla administracji regulaminami. Brak JWT: 401; nieodpowiednia rola/claim: 403.

Menedżer **phone** ma tylko GET/HEAD/OPTIONS; zapisy dają 403 `manager_phone_read_only`. Tryb **choose** wymaga wyboru (403 `manager_workspace_mode_required`). Wyjątki: POST manager-mode/logout, GET session/pending/własna historia/PDF oraz POST accept regulaminu. GET nie nadaje uprawnień innej roli. `/api/admin/db` dodatkowo wymaga włączenia narzędzi, loopback lub dozwolonego remote oraz `X-GF3-Developer-Password`; wyłączona baza daje 404, niespełniona polityka 403. Ten sam nagłówek wymagany jest dla `/api/admin/system-news` i `/api/admin/regulations`; ograniczenia Enabled/loopback dotyczą tylko administracji DB. AdminSystemNews wymaga roli M i hasła deweloperskiego, nie deklaruje polityki S.

Błędy **B** w tabelach to wspólny mechanizm, stosowany gdy dana operacja napotka dany problem: walidacja/DataAnnotations/BLL → 400 z `errors`; `KeyNotFoundException` i `InvalidOperationException` → 404; konflikt aktualizacji/ograniczeń EF → 409; nieoczekiwany wyjątek → 500 bez stosu dla klienta. Dodatkowe kody przy akcji wynikają z jej deklaracji/gałęzi; tabela nie obiecuje, że każda operacja wywoła wszystkie błędy B. Konflikt cudzej blokady edycji daje 409 również przez wspólny helper. ProblemDetails: `type`, `title`, `status`, `detail`, `instance`, zwykle `traceId`; ValidationProblemDetails dodaje mapę `errors`. Kontrolery mogą też zwrócić puste 400/401/403/404. Anulowane żądanie może zakończyć się 499.

Parametry: `{...}` oznaczają path (ograniczenie `:int` jest regułą routingu, nie częścią wartości URL); `[FromQuery]` to query; `[FromForm]` i `IFormFile` to multipart/form-data; pozostałe złożone modele to JSON body, proste niewystępujące w path to query. `CancellationToken` to parametr serwera, nie pole wysyłane przez klienta. DTO/schema niżej podaje typy C#; JSON ma camelCase, `?` dopuszcza null. Brak `?` w typie wartościowym nie oznacza automatycznie atrybutu `[Required]`; osobno podano deklarowane walidatory. Domyślne wartości i reguły `Validate` w źródłach nadal obowiązują.

Przykład bez poświadczeń: `GET /api/containers/1/graphs/2/export/excel?includeStyles=true&includeEmployees=true` z nagłówkiem Bearer własnej sesji manager PC. Sukces to plik XLSX, nie JSON.

### 2.1 Katalog akcji HTTP

170 akcji w 29 plikach / 32 klasach kontrolerów. Ścieżki zachowują podstawienia `[controller]` ze źródła; routing ASP.NET akceptuje także używane przez SPA małe litery. Każdy wiersz odpowiada jednej akcji.

#### AccountLanguageController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/account-language` | Odczyt | brak | brak | 200 — LanguagePreference | M/E | B; 401 |
| PUT | `/api/account-language` | Aktualizacja | LanguagePreference preference | LanguagePreference preference | 200 — LanguagePreference | M/E | B; 400,401 |

#### AdminDbController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/admin/db/metadata` | Metadane SQLite i workspace | brak | brak | 200 — AdminDbMetadataDto + allowWriteSql:bool, maxSqlLength:int, maxImportBytes:int; patrz projekcja administracji | S | B; 403,404,500 |
| GET | `/api/admin/db/hash` | Hash aktywnego pliku bazy | brak | brak | 200 — { hash: string } | S | B; 403,404,500 |
| POST | `/api/admin/db/query` | Zapytanie odczytowe SQL | [`FromBody`] AdminDbSqlRequest request | [`FromBody`] AdminDbSqlRequest request | 200 — AdminDbQueryResultDto | S | B; 400,403,404,500 |
| POST | `/api/admin/db/execute` | Wykonanie dozwolonego SQL zapisu | [`FromBody`] AdminDbSqlRequest request | [`FromBody`] AdminDbSqlRequest request | 200 — { affectedRows: int } | S | B; 400,403,404,500 |
| POST | `/api/admin/db/import` | Import własnego skryptu SQL | `IFormFile` file | multipart: `IFormFile` file | 200 — AdminDbImportResultDto | S | B; 400,403,404,500 |
| POST | `/api/admin/db/manual-copy` | Ręczna kopia aktywnej bazy | brak | brak | 200 — AdminDbFileEntryDto | S | B; 403,404,500 |
| POST | `/api/admin/db/select` | Wybór aktywnej bazy workspace | [`FromBody`] AdminDbSelectDatabaseRequest request | [`FromBody`] AdminDbSelectDatabaseRequest request | 200 — AdminDbFileEntryDto | S | B; 400,403,404,500 |

#### AdminRegulationsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/admin/regulations` | Lista dokumentów regulaminowych | brak | brak | 200 — `IReadOnlyList`&lt;RegulationDocumentDto&gt; | S | B |
| GET | `/api/admin/regulations/acceptances` | Lista potwierdzeń regulaminów | brak | brak | 200 — `IReadOnlyList`&lt;RegulationAcceptanceDto&gt; | S | B |
| GET | `/api/admin/regulations/{documentId:int}/pdf` | Pobranie PDF regulaminu | int documentId | brak | 200 — plik application/pdf | S | B |
| POST | `/api/admin/regulations` | Utworzenie | [`FromForm`] SaveRegulationRequest request | [`FromForm`] SaveRegulationRequest request | 200 — RegulationDocumentDto | S | B |
| PUT | `/api/admin/regulations/{documentId:int}` | Aktualizacja | int documentId; [`FromForm`] SaveRegulationRequest request | [`FromForm`] SaveRegulationRequest request | 200 — RegulationDocumentDto | S | B |
| POST | `/api/admin/regulations/{documentId:int}/publish` | Publikacja dokumentu regulaminu | int documentId | brak | 200 — RegulationDocumentDto | S | B |
| DELETE | `/api/admin/regulations/{documentId:int}` | Usunięcie | int documentId | brak | 204 — brak body | S | B |

#### AuthController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| POST | `/api/Auth/login` | Logowanie | [`FromBody`] LoginRequest request | [`FromBody`] LoginRequest request | 200 — LoginResponseDto | anon | B; 401 |
| GET | `/api/Auth/session` | Sesja | brak | brak | 200 — SessionDto | A | B; 401 |
| POST | `/api/Auth/logout` | Wylogowanie | brak | brak | 204 — brak body | anon | B |
| POST | `/api/Auth/manager-mode` | Wybór trybu menedżera | [`FromBody`] ChangeManagerWorkspaceModeRequest request | [`FromBody`] ChangeManagerWorkspaceModeRequest request | 200 — LoginResponseDto | M | B; 400,403 |
| POST | `/api/Auth/password/send-code` | Wysłanie kodu odzyskiwania hasła | [`FromBody`] SendPasswordResetCodeRequest request | [`FromBody`] SendPasswordResetCodeRequest request | 200 — PasswordResetCodeDispatchDto | anon | B; 400 |
| POST | `/api/Auth/password/confirm` | Potwierdzenie resetu hasła | [`FromBody`] CompleteForgotPasswordResetRequest request | [`FromBody`] CompleteForgotPasswordResetRequest request | 204 — brak body | anon | B; 400 |

#### AvailabilityBindsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/availability-binds` | Lista | brak | brak | 200 — `IEnumerable`&lt;AvailabilityBindDto&gt; | M | B; 500 |
| GET | `/api/availability-binds/active` | Lista aktywnych skrótów | brak | brak | 200 — `IEnumerable`&lt;AvailabilityBindDto&gt; | M | B; 500 |
| GET | `/api/availability-binds/{id:int}` | Szczegóły | int id | brak | 200 — AvailabilityBindDto | M | B; 404,500 |
| POST | `/api/availability-binds` | Utworzenie | [`FromBody`] CreateAvailabilityBindRequest request | [`FromBody`] CreateAvailabilityBindRequest request | 201 — AvailabilityBindDto | M | B; 400,500 |
| PUT | `/api/availability-binds/{id:int}` | Aktualizacja | int id; [`FromBody`] UpdateAvailabilityBindRequest request | [`FromBody`] UpdateAvailabilityBindRequest request | 204 — brak body | M | B; 400,404,500 |
| DELETE | `/api/availability-binds/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B; 404,500 |

#### AvailabilityGroupsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/availability-groups` | Lista | brak | brak | 200 — `IEnumerable`&lt;AvailabilityGroupDto&gt; | M | B; 500 |
| GET | `/api/availability-groups/{id:int}` | Szczegóły | int id | brak | 200 — AvailabilityGroupDto | M | B; 404,500 |
| GET | `/api/availability-groups/{id:int}/items` | Zbiorcze wpisy dyspozycyjności | int id | brak | 200 — `IEnumerable`&lt;AvailabilityGroupItemDto&gt; | M | B; 404,500 |
| GET | `/api/availability-groups/{groupId:int}/members` | Uczestnicy grupy | int groupId | brak | 200 — `IEnumerable`&lt;AvailabilityGroupMemberDto&gt; | M | B; 404,500 |
| POST | `/api/availability-groups/{groupId:int}/members` | Dodanie uczestnika dyspozycyjności | int groupId; [`FromBody`] CreateAvailabilityGroupMemberRequest request | [`FromBody`] CreateAvailabilityGroupMemberRequest request | 201 — AvailabilityGroupMemberDto | M | B; 400,404,409,500 |
| PUT | `/api/availability-groups/{groupId:int}/members/{memberId:int}` | Zmiana uczestnika grupy | int groupId; int memberId; [`FromBody`] UpdateAvailabilityGroupMemberRequest request | [`FromBody`] UpdateAvailabilityGroupMemberRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/availability-groups/{groupId:int}/members/{memberId:int}` | Usunięcie uczestnika grupy | int groupId; int memberId | brak | 204 — brak body | M | B; 404,409,500 |
| GET | `/api/availability-groups/{groupId:int}/slots` | Wpisy dni grupy | int groupId | brak | 200 — `IEnumerable`&lt;AvailabilitySlotDto&gt; | M | B; 404,500 |
| POST | `/api/availability-groups/{groupId:int}/slots` | Utworzenie wpisu dnia dyspozycyjności | int groupId; [`FromBody`] CreateAvailabilitySlotRequest request | [`FromBody`] CreateAvailabilitySlotRequest request | 201 — AvailabilitySlotDto | M | B; 400,404,409,500 |
| PUT | `/api/availability-groups/{groupId:int}/slots/{slotId:int}` | Zmiana wpisu dnia grupy | int groupId; int slotId; [`FromBody`] UpdateAvailabilitySlotRequest request | [`FromBody`] UpdateAvailabilitySlotRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/availability-groups/{groupId:int}/slots/{slotId:int}` | Usunięcie wpisu dnia | int groupId; int slotId | brak | 204 — brak body | M | B; 404,409,500 |
| POST | `/api/availability-groups` | Utworzenie | [`FromBody`] CreateAvailabilityGroupRequest request | [`FromBody`] CreateAvailabilityGroupRequest request | 201 — AvailabilityGroupDto | M | B; 400,500 |
| PUT | `/api/availability-groups/{id:int}` | Aktualizacja | int id; [`FromBody`] UpdateAvailabilityGroupRequest request | [`FromBody`] UpdateAvailabilityGroupRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/availability-groups/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B; 404,409,500 |

#### AvailabilityGroupTransfersController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/availability-groups/{targetGroupId:int}/members/{targetMemberId:int}/transfer-sources` | Źródła transferu dni członka grupy | int targetGroupId; int targetMemberId | brak | 200 — `IEnumerable`&lt;AvailabilityTransferSourceDto&gt; | M | B |
| GET | `/api/availability-groups/transfer-preview` | Źródła transferu dla podglądu grupy | [`FromQuery`] string employeeIds; [`FromQuery`] int year; [`FromQuery`] int month; [`FromQuery`] int? targetGroupId | brak | 200 — `IEnumerable`&lt;AvailabilityTransferSourceDto&gt; | M | B; 400 |
| GET | `/api/availability-groups/{sourceGroupId:int}/transfer-hints` | Wskazania transferów dyspozycyjności | int sourceGroupId | brak | 200 — `IEnumerable`&lt;AvailabilityTransferHintDto&gt; | M | B |
| POST | `/api/availability-groups/{targetGroupId:int}/members/{targetMemberId:int}/transfer-days` | Transfer wybranych dni dyspozycyjności | int targetGroupId; int targetMemberId; [`FromBody`] TransferAvailabilityDaysRequest request | [`FromBody`] TransferAvailabilityDaysRequest request | 200 — AvailabilityTransferResultDto | M | B; 400,409 |

#### CommunicationsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/communications` | Lista | brak | brak | 200 — `IReadOnlyList`&lt;CommunicationMessageDto&gt; | M | B |
| POST | `/api/communications` | Utworzenie | [`FromBody`] CreateCommunicationMessageRequest request | [`FromBody`] CreateCommunicationMessageRequest request | 200 — CommunicationMessageDto | M | B; 400 |
| PUT | `/api/communications/{communicationId:int}` | Aktualizacja | int communicationId; [`FromBody`] UpdateCommunicationMessageRequest request | [`FromBody`] UpdateCommunicationMessageRequest request | 200 — CommunicationMessageDto | M | B; 400,404 |
| DELETE | `/api/communications/{communicationId:int}` | Usunięcie | int communicationId | brak | 204 — brak body | M | B; 404 |
| GET | `/api/communications/pending` | Dokumenty lub komunikaty oczekujące dla konta | brak | brak | 200 — `IReadOnlyList`&lt;CommunicationMessageDto&gt; | E | B |
| POST | `/api/communications/{communicationId:int}/dismiss` | Potwierdzenie odczytu komunikatu | int communicationId | brak | 204 — brak body | E | B; 400 |

#### ContainersController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/Containers` | Lista | brak | brak | 200 — `IEnumerable`&lt;ContainerDto&gt; | M | B; 500 |
| GET | `/api/Containers/{id:int}` | Szczegóły | int id | brak | 200 — ContainerDto | M | B; 404,500 |
| GET | `/api/Containers/{containerId:int}/graphs` | Grafiki kontenera | int containerId | brak | 200 — `IEnumerable`&lt;GraphDto&gt; | M | B; 404,500 |
| PUT | `/api/Containers/{containerId:int}/graphs/publication` | Publikacja/prywatyzacja grafików kontenera | int containerId; [`FromBody`] UpdateGraphsPublicationRequest request | [`FromBody`] UpdateGraphsPublicationRequest request | 204 — brak body | M | B; 400,404,409 |
| GET | `/api/Containers/{containerId:int}/schedule-presets` | Presety aplikacji dostępne z kontenera | int containerId | brak | 200 — `IEnumerable`&lt;SchedulePresetDto&gt; | M | B; 404,500 |
| POST | `/api/Containers/{containerId:int}/schedule-presets` | Zapis presetu | int containerId; [`FromBody`] CreateSchedulePresetRequest request | [`FromBody`] CreateSchedulePresetRequest request | 201 — SchedulePresetDto | M | B; 400,404,500 |
| GET | `/api/Containers/{containerId:int}/graphs/{graphId:int}` | Szczegóły grafiku kontenera | int containerId; int graphId | brak | 200 — GraphDto | M | B; 404,500 |
| POST | `/api/Containers/{containerId:int}/graphs` | Utworzenie grafiku | int containerId; [`FromBody`] CreateGraphRequest request | [`FromBody`] CreateGraphRequest request | 201 — GraphDto | M | B; 400,404,500 |
| PUT | `/api/Containers/{containerId:int}/graphs/{graphId:int}` | Aktualizacja grafiku | int containerId; int graphId; [`FromBody`] UpdateGraphRequest request | [`FromBody`] UpdateGraphRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/Containers/{containerId:int}/graphs/{graphId:int}` | Usunięcie grafiku | int containerId; int graphId | brak | 204 — brak body | M | B; 404,409,500 |
| POST | `/api/Containers/{containerId:int}/graphs/generate-preview` | Generowanie podglądu bez utrwalenia grafiku | int containerId; [`FromBody`] GenerateGraphPreviewRequest request | [`FromBody`] GenerateGraphPreviewRequest request | 200 — GenerateGraphResponse | M | B; 400,404,500 |
| POST | `/api/Containers/{containerId:int}/graphs/{graphId:int}/generate` | Generowanie zapisanego grafiku | int containerId; int graphId; [`FromBody`] GenerateGraphRequest request | [`FromBody`] GenerateGraphRequest request | 200 — GenerateGraphResponse | M | B; 400,404,409,500 |
| GET | `/api/Containers/{containerId:int}/graphs/{graphId:int}/slots` | Sloty grafiku | int containerId; int graphId | brak | 200 — `IEnumerable`&lt;GraphSlotDto&gt; | M | B; 404 |
| PUT | `/api/Containers/{containerId:int}/graphs/{graphId:int}/slots` | Zastąpienie slotów grafiku | int containerId; int graphId; [`FromBody`] ReplaceGraphSlotsRequest request | [`FromBody`] ReplaceGraphSlotsRequest request | 204 — brak body | M | B; 400,409 |
| POST | `/api/Containers/{containerId:int}/graphs/{graphId:int}/slots` | Utworzenie slotu grafiku | int containerId; int graphId; [`FromBody`] CreateGraphSlotRequest request | [`FromBody`] CreateGraphSlotRequest request | 201 — GraphSlotDto | M | B; 400,409 |
| PUT | `/api/Containers/{containerId:int}/graphs/{graphId:int}/slots/{slotId:int}` | Zmiana slotu grafiku | int containerId; int graphId; int slotId; [`FromBody`] UpdateGraphSlotRequest request | [`FromBody`] UpdateGraphSlotRequest request | 204 — brak body | M | B; 400,409 |
| DELETE | `/api/Containers/{containerId:int}/graphs/{graphId:int}/slots/{slotId:int}` | Usunięcie slotu | int containerId; int graphId; int slotId | brak | 204 — brak body | M | B; 409 |
| GET | `/api/Containers/{containerId:int}/graphs/{graphId:int}/employees` | Uczestnicy grafiku | int containerId; int graphId | brak | 200 — `IEnumerable`&lt;GraphEmployeeDto&gt; | M | B; 404 |
| POST | `/api/Containers/{containerId:int}/graphs/{graphId:int}/employees` | Dodanie uczestnika grafiku | int containerId; int graphId; [`FromBody`] AddGraphEmployeeRequest request | [`FromBody`] AddGraphEmployeeRequest request | 201 — GraphEmployeeDto | M | B; 409 |
| PUT | `/api/Containers/{containerId:int}/graphs/{graphId:int}/employees/{graphEmployeeId:int}` | Zmiana ustawień uczestnika grafiku | int containerId; int graphId; int graphEmployeeId; [`FromBody`] UpdateGraphEmployeeRequest request | [`FromBody`] UpdateGraphEmployeeRequest request | 204 — brak body | M | B; 409 |
| DELETE | `/api/Containers/{containerId:int}/graphs/{graphId:int}/employees/{graphEmployeeId:int}` | Usunięcie uczestnika grafiku | int containerId; int graphId; int graphEmployeeId | brak | 204 — brak body | M | B; 409 |
| GET | `/api/Containers/{containerId:int}/graphs/{graphId:int}/cell-styles` | Style komórek grafiku | int containerId; int graphId | brak | 200 — `IEnumerable`&lt;GraphCellStyleDto&gt; | M | B; 404 |
| PUT | `/api/Containers/{containerId:int}/graphs/{graphId:int}/cell-styles` | Zapis stylu komórki grafiku | int containerId; int graphId; [`FromBody`] UpsertGraphCellStyleRequest request | [`FromBody`] UpsertGraphCellStyleRequest request | 200 — GraphCellStyleDto | M | B; 409 |
| DELETE | `/api/Containers/{containerId:int}/graphs/{graphId:int}/cell-styles/{styleId:int}` | Usunięcie stylu komórki | int containerId; int graphId; int styleId | brak | 204 — brak body | M | B; 409 |
| POST | `/api/Containers` | Utworzenie | [`FromBody`] CreateContainerRequest request | [`FromBody`] CreateContainerRequest request | 201 — ContainerDto | M | B; 400,500 |
| PUT | `/api/Containers/{id:int}` | Aktualizacja | int id; [`FromBody`] UpdateContainerRequest request | [`FromBody`] UpdateContainerRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/Containers/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B; 400,404,409,500 |

#### EmployeeAvailabilityController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-availability` | Lista dostępna bieżącemu kontu | brak | brak | 200 — `IEnumerable`&lt;EmployeeAvailabilityGroupDto&gt; | E | B |
| GET | `/api/employee-availability/{groupId:int}` | Szczegóły | int groupId | brak | 200 — EmployeeAvailabilityGroupDto | E | B; 404 |
| PUT | `/api/employee-availability/{groupId:int}/slots` | Zapis własnych dni dyspozycyjności | int groupId; [`FromBody`] UpdateEmployeeAvailabilityRequest request | [`FromBody`] UpdateEmployeeAvailabilityRequest request | 200 — EmployeeAvailabilityGroupDto | E | B; 400,404,409 |

#### EmployeeProfileController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-profile/me` | Stan własnego konta | brak | brak | 200 — EmployeeProfileDto | E | B |
| PUT | `/api/employee-profile/me` | Aktualizacja własnego profilu | [`FromBody`] UpdateEmployeeProfileRequest request | [`FromBody`] UpdateEmployeeProfileRequest request | 200 — EmployeeProfileDto | E | B; 400 |
| POST | `/api/employee-profile/me/password/send-code` | Wysłanie kodu odzyskiwania hasła | brak | brak | 200 — PasswordResetCodeDispatchDto | E | B; 400 |
| POST | `/api/employee-profile/me/password/confirm` | Potwierdzenie resetu hasła | [`FromBody`] CompletePasswordResetRequest request | [`FromBody`] CompletePasswordResetRequest request | 204 — brak body | E | B; 400 |

#### EmployeePushController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-push/config` | Dostępność push i publiczny klucz VAPID | brak | brak | 200 — { enabled: bool, publicKey: string? } | E | B |
| PUT | `/api/employee-push/subscriptions` | Zapis subskrypcji push | SubscriptionRequest request | SubscriptionRequest request | 204 — brak body | E | B; 400,403,409,503 |
| DELETE | `/api/employee-push/subscriptions` | Usunięcie | DeleteRequest request | DeleteRequest request | 204 — brak body | E | B; 400,403 |

#### EmployeeSchedulesController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-schedules` | Lista dostępna bieżącemu kontu | brak | brak | 200 — `IEnumerable`&lt;EmployeeScheduleDto&gt; | E | B |

#### EmployeesController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/Employees` | Lista | brak | brak | 200 — `IEnumerable`&lt;EmployeeDto&gt; | M | B; 500 |
| GET | `/api/Employees/{id:int}` | Szczegóły | int id | brak | 200 — EmployeeDto | M | B; 404,500 |
| POST | `/api/Employees` | Utworzenie | [`FromBody`] CreateEmployeeRequest request | [`FromBody`] CreateEmployeeRequest request | 201 — EmployeeDto | M | B; 400,500 |
| PUT | `/api/Employees/{id:int}` | Aktualizacja | int id; [`FromBody`] UpdateEmployeeRequest request | [`FromBody`] UpdateEmployeeRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/Employees/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B; 400,404,409,500 |
| POST | `/api/Employees/{id:int}/kick` | Unieważnienie sesji pracownika | int id | brak | 204 — brak body | M | B; 404,500 |

#### EmployeeShiftSwapsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-shift-swaps/employees` | Pracownicy dostępni dla wymian | brak | brak | 200 — `IEnumerable`&lt;ShiftSwapEmployeeDto&gt; | E | B |
| GET | `/api/employee-shift-swaps` | Lista dostępna bieżącemu kontu | brak | brak | 200 — `IEnumerable`&lt;ShiftSwapDto&gt; | E | B |
| POST | `/api/employee-shift-swaps` | Utworzenie | [`FromBody`] CreateEmployeeShiftSwapRequest request | [`FromBody`] CreateEmployeeShiftSwapRequest request | 201 — ShiftSwapDto | E | B; 400 |
| POST | `/api/employee-shift-swaps/{id:int}/accept` | Akceptacja | int id | brak | 200 — ShiftSwapDto | E | B; 400,404 |
| POST | `/api/employee-shift-swaps/{id:int}/cancel` | Anulowanie | int id | brak | 200 — ShiftSwapDto | E | B; 400,404 |
| GET | `/api/employee-shift-swaps/return-candidates` | Kandydaci wzajemnej wymiany | [`FromQuery`] int sourceScheduleId; [`FromQuery`] int sourceSlotId; [`FromQuery`] string fromTime; [`FromQuery`] string toTime; [`FromQuery`] int targetEmployeeId; [`FromQuery`] int page = 1; [`FromQuery`] int pageSize = 30 | brak | 200 — ShiftSwapReturnCandidatePageDto | E | B; 400 |
| POST | `/api/employee-shift-swaps/preview` | Podgląd oferty bez zapisu | [`FromBody`] CreateEmployeeShiftSwapRequest request | [`FromBody`] CreateEmployeeShiftSwapRequest request | 200 — ShiftSwapOfferPreviewDto | E | B |

#### EmployeeUiStateController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-ui-state` | Stan własnego konta | brak | brak | 200 — EmployeeUiStateDto | E | B |
| PUT | `/api/employee-ui-state/swap-pins/{swapId:int}` | Przypięcie widocznej wymiany | int swapId | brak | 204 — brak body | E | B; 404 |
| DELETE | `/api/employee-ui-state/swap-pins/{swapId:int}` | Odpięcie wymiany | int swapId | brak | 204 — brak body | E | B |
| PUT | `/api/employee-ui-state/schedule-columns/{scheduleId:int}` | Zapis kolejności kolumn grafiku pracownika | int scheduleId; [`FromBody`] SaveEmployeeScheduleColumnOrderRequest request | [`FromBody`] SaveEmployeeScheduleColumnOrderRequest request | 204 — brak body | E | B; 400,404 |
| POST | `/api/employee-ui-state/notifications/read` | Zapis odczytu wskazanych powiadomień | [`FromBody`] MarkEmployeeNotificationsReadRequest request | [`FromBody`] MarkEmployeeNotificationsReadRequest request | 204 — brak body | E | B; 400 |

#### ExportsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/containers/{containerId:int}/graphs/{graphId:int}/export/excel` | Eksport XLSX grafiku | int containerId; int graphId; [`FromQuery`] bool includeStyles = true; [`FromQuery`] bool includeEmployees = true | brak | 200 — plik `XLSX` | M | B |
| GET | `/api/containers/{containerId:int}/graphs/{graphId:int}/export/sql` | Eksport SQL grafiku | int containerId; int graphId; [`FromQuery`] bool includeStyles = true; [`FromQuery`] bool includeEmployees = true | brak | 200 — plik `SQL` `UTF`-8 | M | B |
| GET | `/api/containers/{containerId:int}/export/excel` | Eksport XLSX kontenera | int containerId; [`FromQuery`] bool includeStyles = true; [`FromQuery`] bool includeEmployees = true | brak | 200 — plik `XLSX` | M | B |
| GET | `/api/containers/{containerId:int}/export/sql` | Eksport SQL kontenera | int containerId; [`FromQuery`] bool includeStyles = true; [`FromQuery`] bool includeEmployees = true | brak | 200 — plik `SQL` `UTF`-8 | M | B |

#### GraphVersionsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/containers/{containerId:int}/graphs/{graphId:int}/versions` | Drzewo wersji grafiku | int containerId; int graphId | brak | 200 — GraphVersionTreeDto | M | B; 404 |
| POST | `/api/containers/{containerId:int}/graphs/{graphId:int}/versions` | Commit grafiku (200 bez nowej wersji, 201 dla nowej) | int containerId; int graphId | brak | 200,201 — GraphVersionTreeDto | M | B; 404,409 |
| POST | `/api/containers/{containerId:int}/graphs/{graphId:int}/versions/{versionId:int}/checkout` | Przywrócenie wersji grafiku | int containerId; int graphId; int versionId | brak | 200 — GraphVersionTreeDto | M | B; 404,409 |
| DELETE | `/api/containers/{containerId:int}/graphs/{graphId:int}/versions/{versionId:int}` | Usunięcie | int containerId; int graphId; int versionId | brak | 204 — brak body | M | B; 404,409 |

#### HealthController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/Health` | Odczyt | brak | brak | 200 — { status: string (ok/unavailable), canConnect: bool } | anon | B; 503 |

#### ManagerGraphFillColorBindsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/manager-graph-fill-color-binds` | Stan własnego konta | brak | brak | 200 — `IReadOnlyList`&lt;ManagerGraphFillColorBindDto&gt; | M | B |
| PUT | `/api/manager-graph-fill-color-binds` | Zapis skrótu koloru | [`FromBody`] SaveManagerGraphFillColorBindRequest request | [`FromBody`] SaveManagerGraphFillColorBindRequest request | 200 — ManagerGraphFillColorBindDto | M | B; 400 |
| DELETE | `/api/manager-graph-fill-color-binds/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B |

#### ManagerGraphTextColorBindsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/manager-graph-text-color-binds` | Stan własnego konta | brak | brak | 200 — `IReadOnlyList`&lt;ManagerGraphTextColorBindDto&gt; | M | B |
| PUT | `/api/manager-graph-text-color-binds` | Zapis skrótu koloru | [`FromBody`] SaveManagerGraphTextColorBindRequest request | [`FromBody`] SaveManagerGraphTextColorBindRequest request | 200 — ManagerGraphTextColorBindDto | M | B; 400 |
| DELETE | `/api/manager-graph-text-color-binds/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B |

#### ManagerNotepadController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/manager-notepad` | Stan własnego konta | brak | brak | 200 — ManagerNotepadDto | M | B |
| POST | `/api/manager-notepad/notes` | Utworzenie notatki | [`FromBody`] SaveManagerNoteRequest request | [`FromBody`] SaveManagerNoteRequest request | 201 — ManagerNoteDto | M | B; 400 |
| PUT | `/api/manager-notepad/notes/{noteId:int}` | Zapis notatki | int noteId; [`FromBody`] SaveManagerNoteRequest request | [`FromBody`] SaveManagerNoteRequest request | 200 — ManagerNoteDto | M | B; 400,404 |
| DELETE | `/api/manager-notepad/notes/{noteId:int}` | Usunięcie notatki | int noteId | brak | 204 — brak body | M | B |
| PUT | `/api/manager-notepad/state` | Zapis układu notatnika | [`FromBody`] SaveManagerNotepadStateRequest request | [`FromBody`] SaveManagerNotepadStateRequest request | 204 — brak body | M | B |

#### ManagerProfileController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/manager-profile/me` | Stan własnego konta | brak | brak | 200 — ManagerProfileDto | M | B |
| PUT | `/api/manager-profile/me` | Aktualizacja własnego profilu | [`FromBody`] UpdateManagerProfileRequest request | [`FromBody`] UpdateManagerProfileRequest request | 200 — ManagerProfileUpdateResponseDto | M | B; 400 |
| GET | `/api/manager-profile/managers` | Lista kont menedżerów | brak | brak | 200 — `IReadOnlyList`&lt;ManagerProfileDto&gt; | M | B |
| POST | `/api/manager-profile/managers` | Utworzenie konta menedżera | [`FromBody`] CreateManagerRequest request | [`FromBody`] CreateManagerRequest request | 200 — ManagerProfileDto | M | B; 400 |
| DELETE | `/api/manager-profile/managers/{managerId:int}` | Usunięcie dozwolonego konta menedżera | int managerId | brak | 204 — brak body | M | B; 400 |

#### RegulationsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/regulations/pending` | Dokumenty lub komunikaty oczekujące dla konta | brak | brak | 200 — `IReadOnlyList`&lt;RegulationDocumentDto&gt; | A | B |
| GET | `/api/regulations/history/me` | Własna historia akceptacji regulaminów | brak | brak | 200 — `IReadOnlyList`&lt;RegulationAcceptanceDto&gt; | A | B |
| GET | `/api/regulations/history/employees/{employeeId:int}` | Historia akceptacji wskazanego pracownika | int employeeId | brak | 200 — `IReadOnlyList`&lt;RegulationAcceptanceDto&gt; | M | B |
| GET | `/api/regulations/{documentId:int}/pdf` | Pobranie PDF regulaminu | int documentId | brak | 200 — plik application/pdf | A | B |
| POST | `/api/regulations/{documentId:int}/accept` | Akceptacja | int documentId | brak | 200 — RegulationAcceptanceDto | A | B |

#### EmployeeShiftCorrectionsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/employee-shift-corrections` | Własne wnioski korekt | brak | brak | 200 — `IReadOnlyList`&lt;ShiftCorrectionRequestDto&gt; | E | B |
| POST | `/api/employee-shift-corrections` | Utworzenie | [`FromBody`] CreateShiftCorrectionRequest request | [`FromBody`] CreateShiftCorrectionRequest request | 201 — ShiftCorrectionRequestDto | E | B; 400 |

#### ManagerShiftCorrectionSettingsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/manager-shift-correction-settings` | Stan własnego konta | brak | brak | 200 — ShiftCorrectionSettingDto | M | B |
| PUT | `/api/manager-shift-correction-settings` | Zapis ustawienia korekt | [`FromBody`] SaveShiftCorrectionSettingRequest request | [`FromBody`] SaveShiftCorrectionSettingRequest request | 200 — ShiftCorrectionSettingDto | M | B |

#### ManagerShiftCorrectionsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-corrections` | Wnioski korekt danego grafiku | int containerId; int graphId | brak | 200 — `IReadOnlyList`&lt;ShiftCorrectionRequestDto&gt; | M | B; 404 |
| POST | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-corrections/{id:int}/approve` | Zatwierdzenie korekty | int containerId; int graphId; int id; [`FromBody`] ApproveShiftCorrectionRequest request | [`FromBody`] ApproveShiftCorrectionRequest request | 200 — ShiftCorrectionRequestDto | M | B; 400,404,409 |
| POST | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-corrections/{id:int}/reject` | Odrzucenie korekty | int containerId; int graphId; int id | brak | 200 — ShiftCorrectionRequestDto | M | B; 404,409 |

#### ShiftSwapLogsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-swaps/highlight-setting` | Kolor wyróżnienia zaakceptowanej wymiany | int containerId; int graphId | brak | 200 — ShiftSwapHighlightSettingDto | M | B; 404 |
| PUT | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-swaps/highlight-setting` | Zapis koloru wyróżnienia wymian | int containerId; int graphId; [`FromBody`] SaveShiftSwapHighlightSettingRequest request | [`FromBody`] SaveShiftSwapHighlightSettingRequest request | 200 — ShiftSwapHighlightSettingDto | M | B; 404 |
| GET | `/api/containers/{containerId:int}/shift-swaps` | Wymiany wszystkich grafików kontenera | int containerId | brak | 200 — `IEnumerable`&lt;ShiftSwapDto&gt; | M | B; 404 |
| POST | `/api/containers/{containerId:int}/shift-swaps/{id:int}/cancel` | Anulowanie oferty kontenera | int containerId; int id | brak | 200 — ShiftSwapDto | M | B; 400,404,409 |
| DELETE | `/api/containers/{containerId:int}/shift-swaps/{id:int}` | Usunięcie zapisu wymiany (bez cofnięcia grafiku) | int containerId; int id | brak | 204 — brak body | M | B; 404,409 |
| POST | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-swaps/manual` | Oferta z kolumny ręcznej | int containerId; int graphId; [`FromBody`] CreateManagerShiftSwapRequest request | [`FromBody`] CreateManagerShiftSwapRequest request | 201 — ShiftSwapDto | M | B; 400,404,409 |
| GET | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-swaps` | Log wymian grafiku | int containerId; int graphId | brak | 200 — `IEnumerable`&lt;ShiftSwapDto&gt; | M | B; 404 |
| POST | `/api/containers/{containerId:int}/graphs/{graphId:int}/shift-swaps/{id:int}/cancel` | Anulowanie oferty ręcznej | int containerId; int graphId; int id | brak | 204 — brak body | M | B; 400,404,409 |

#### ShopsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/Shops` | Lista | brak | brak | 200 — `IEnumerable`&lt;ShopDto&gt; | M | B; 500 |
| GET | `/api/Shops/{id:int}` | Szczegóły | int id | brak | 200 — ShopDto | M | B; 404,500 |
| POST | `/api/Shops` | Utworzenie | [`FromBody`] CreateShopRequest request | [`FromBody`] CreateShopRequest request | 201 — ShopDto | M | B; 400,500 |
| PUT | `/api/Shops/{id:int}` | Aktualizacja | int id; [`FromBody`] UpdateShopRequest request | [`FromBody`] UpdateShopRequest request | 204 — brak body | M | B; 400,404,409,500 |
| DELETE | `/api/Shops/{id:int}` | Usunięcie | int id | brak | 204 — brak body | M | B; 400,404,409,500 |

#### SystemNewsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/system-news` | Lista dostępna bieżącemu kontu | brak | brak | 200 — `IReadOnlyList`&lt;SystemNewsDto&gt; | M/E | B |
| POST | `/api/system-news/{messageId:int}/read` | Zapis odczytu aktualności | int messageId | brak | 204 — brak body | M/E | B; 404 |
| POST | `/api/system-news/read-all` | Zapis odczytu wszystkich widocznych aktualności | brak | brak | 204 — brak body | M/E | B |

#### AdminSystemNewsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/admin/system-news` | Lista | brak | brak | 200 — `IReadOnlyList`&lt;SystemNewsDto&gt; | M | B |
| POST | `/api/admin/system-news` | Utworzenie | SaveSystemNewsRequest request | SaveSystemNewsRequest request | 201 — SystemNewsDto | M | B; 400 |
| PUT | `/api/admin/system-news/{messageId:int}` | Aktualizacja | int messageId; SaveSystemNewsRequest request | SaveSystemNewsRequest request | 200 — SystemNewsDto | M | B; 400,404 |
| DELETE | `/api/admin/system-news/{messageId:int}` | Usunięcie | int messageId | brak | 204 — brak body | M | B |

#### WorkflowLogsController

| Metoda | Pełna ścieżka | Cel | Parametry path/query/body | Request DTO lub brak | Odpowiedź/status i response DTO | Dostęp/rola | Najważniejsze błędy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| GET | `/api/workflow-logs` | Ostatnia aktywność workflow | brak | brak | 200 — `IEnumerable`&lt;WorkflowLogDto&gt; | S | B |
| GET | `/api/workflow-logs/settings` | Ustawienia rejestrowania workflow | brak | brak | 200 — WorkflowLogSettingsDto | S | B |
| PUT | `/api/workflow-logs/settings` | Zapis ustawień logowania aktywności | UpdateWorkflowLogSettingsRequest request | UpdateWorkflowLogSettingsRequest request | 200 — WorkflowLogSettingsDto | S | B; 400 |
| DELETE | `/api/workflow-logs/{id:int}` | Usunięcie | int id | brak | 204 — brak body | S | B; 404 |
| POST | `/api/workflow-logs/bulk-delete` | Usunięcie logów według zakresu lub wszystkich | DeleteWorkflowLogsRequest request | DeleteWorkflowLogsRequest request | 200 — WorkflowLogDeleteResultDto | S | B; 400 |

Eksporty: XLSX `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`; SQL `text/plain; charset=utf-8`, nazwa `GF3_Graph_<ID>_<CZAS>.sql` / `GF3_Container_<ID>_<CZAS>.sql`. `includeStyles` i `includeEmployees` domyślnie true. Pobieranie regulaminu: `application/pdf`, nazwa pliku z dokumentu. Push PUT ma limit żądania 4096 bajtów, wymaga dozwolonego endpointu i poprawnych kluczy; wyłączona konfiguracja daje 503, konflikt 409. GET config zwraca enabled i publicKey (null, gdy wyłączony).

Projekcja administracji: Metadata zawiera pola AdminDbMetadataDto (sqliteVersion, databasePath, fileSizeBytes, lastModifiedUtc, userVersion, tables, objects, storageWorkspace) i opcje allowWriteSql, maxSqlLength, maxImportBytes. Workspace: ścieżki katalogów, limit retencji i listy plików. FileEntry: name, path, category, fileSizeBytes, lastModifiedUtc, isActive. Import przyjmuje plik multipart, zwraca podsumowanie transakcyjnego importu; sukces HTTP nie jest dowodem, że wszystkie instrukcje SQL zostały zastosowane — sprawdź pola wyniku.

### 2.2 Schematy modeli HTTP

Każdy model podano raz; jego pełna nazwa identyfikuje schemat używany w katalogu akcji. Kolumny pokazują deklarację typu i walidację/default ze źródła. Kolekcje to tablice JSON; słowniki to obiekty JSON. Atrybuty formatu, zakresu i długości oraz walidacja usług sumują się. Klasa dziedzicząca zawiera również pola bazowe. `string.Empty`/`[]` to wartości domyślne, nie przykładowe dane klienta.

#### BusinessLogicLayer.Contracts.Database.AdminDbFileEntryDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | ` default=string.Empty` |
| `Path` | string | ` default=string.Empty` |
| `Category` | string | ` default=string.Empty` |
| `FileSizeBytes` | long | brak atrybutu/default w deklaracji |
| `LastModifiedUtc` | `DateTime`? | brak atrybutu/default w deklaracji |
| `IsActive` | bool | brak atrybutu/default w deklaracji |

#### BusinessLogicLayer.Contracts.Database.AdminDbImportResultDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `StatementsExecuted` | int | brak atrybutu/default w deklaracji |
| `StatementsApplied` | int | brak atrybutu/default w deklaracji |
| `StatementsAlreadyExisted` | int | brak atrybutu/default w deklaracji |
| `ServiceStatementsSkipped` | int | brak atrybutu/default w deklaracji |
| `FailedStatementIndex` | int? | brak atrybutu/default w deklaracji |
| `FailureReason` | string? | brak atrybutu/default w deklaracji |

#### BusinessLogicLayer.Contracts.Database.AdminDbMetadataDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `SqliteVersion` | string | ` default=string.Empty` |
| `DatabasePath` | string | ` default=string.Empty` |
| `FileSizeBytes` | long | brak atrybutu/default w deklaracji |
| `LastModifiedUtc` | `DateTime`? | brak atrybutu/default w deklaracji |
| `UserVersion` | int | brak atrybutu/default w deklaracji |
| `Tables` | `IReadOnlyList`&lt;string&gt; | ` default=Array.Empty<string>()` |
| `Objects` | `IReadOnlyList`&lt;AdminDbObjectDto&gt; | ` default=Array.Empty<AdminDbObjectDto>()` |
| `StorageWorkspace` | AdminDbStorageWorkspaceDto | ` default=new()` |

#### BusinessLogicLayer.Contracts.Database.AdminDbObjectDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Type` | string | ` default=string.Empty` |
| `Name` | string | ` default=string.Empty` |
| `Sql` | string | ` default=string.Empty` |

#### BusinessLogicLayer.Contracts.Database.AdminDbQueryResultDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Columns` | `IReadOnlyList`&lt;string&gt; | ` default=Array.Empty<string>()` |
| `Rows` | `IReadOnlyList`&lt;`IReadOnlyList`&lt;object?&gt;&gt; | ` default=Array.Empty<IReadOnlyList<object?>>()` |
| `RowCount` | int | brak atrybutu/default w deklaracji |

#### BusinessLogicLayer.Contracts.Database.AdminDbStorageWorkspaceDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `WorkspaceRootPath` | string | ` default=string.Empty` |
| `AutomaticBackupDirectoryPath` | string | ` default=string.Empty` |
| `ManualCopyDirectoryPath` | string | ` default=string.Empty` |
| `AutomaticBackupRetentionLimit` | int | brak atrybutu/default w deklaracji |
| `AvailableDatabases` | `IReadOnlyList`&lt;AdminDbFileEntryDto&gt; | ` default=Array.Empty<AdminDbFileEntryDto>()` |
| `AutomaticBackups` | `IReadOnlyList`&lt;AdminDbFileEntryDto&gt; | ` default=Array.Empty<AdminDbFileEntryDto>()` |
| `ManualCopies` | `IReadOnlyList`&lt;AdminDbFileEntryDto&gt; | ` default=Array.Empty<AdminDbFileEntryDto>()` |

#### BusinessLogicLayer.Contracts.Enums.AvailabilityKind

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ANY` | enum / JSON 0 | dostępny cały dzień |
| `NONE` | enum / JSON 1 | niedostępny cały dzień |
| `INT` | enum / JSON 2 | dostępny w podanym przedziale |

#### BusinessLogicLayer.Contracts.Enums.SlotStatus

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `UNFURNISHED` | enum / JSON 0 | slot bez przypisanego pracownika |
| `ASSIGNED` | enum / JSON 1 | slot przypisany pracownikowi |

#### BusinessLogicLayer.Contracts.Managers.ManagerProfileDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `UserName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `RecoveryEmail` | string? | brak atrybutu/default w deklaracji |
| `LastLoginAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `IsOnline` | bool | brak atrybutu/default w deklaracji |
| `IsSystem` | bool | brak atrybutu/default w deklaracji |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `PasswordUpdatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

#### BusinessLogicLayer.Contracts.Regulations.RegulationAcceptanceDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `RegulationDocumentId` | int | brak atrybutu/default w deklaracji |
| `RegulationTitle` | string | ` default=string.Empty` |
| `RegulationVersion` | string | ` default=string.Empty` |
| `PdfSha256` | string | ` default=string.Empty` |
| `AccountRole` | string | ` default=string.Empty` |
| `AccountId` | int | brak atrybutu/default w deklaracji |
| `Username` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `AcceptedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

#### BusinessLogicLayer.Contracts.Regulations.RegulationDocumentDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Title` | string | ` default=string.Empty` |
| `Version` | string | ` default=string.Empty` |
| `Message` | string | ` default=string.Empty` |
| `PdfFileName` | string | ` default=string.Empty` |
| `PdfSha256` | string | ` default=string.Empty` |
| `IsPublished` | bool | brak atrybutu/default w deklaracji |
| `PublishedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `CreatedByManagerName` | string | ` default=string.Empty` |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `UpdatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `AcceptanceCount` | int | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AdminDb.AdminDbSelectDatabaseRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DatabasePath` | string | ` default=string.Empty` |

#### WebApi.Contracts.AdminDb.AdminDbSqlRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Sql` | string | ` default=string.Empty` |

#### WebApi.Contracts.Auth.ChangeManagerWorkspaceModeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Mode` | string | ` default=string.Empty` |

#### WebApi.Contracts.Auth.CompleteForgotPasswordResetRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Username` | string | `[Required] [MaxLength(100)] default=string.Empty` |
| `Code` | string | `[Required] [MaxLength(6)] default=string.Empty` |
| `NewPassword` | string | `[Required] [MaxLength(200)] default=string.Empty` |

#### WebApi.Contracts.Auth.LoginRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Username` | string | `[Required] [MaxLength(100)] default=string.Empty` |
| `Password` | string | `[Required] [MaxLength(200)] default=string.Empty` |

#### WebApi.Contracts.Auth.LoginResponseDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `AccessToken` | string | ` default=string.Empty` |
| `ExpiresAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `Session` | SessionDto | ` default=new()` |

#### WebApi.Contracts.Auth.PasswordResetCodeDispatchDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DeliveryHint` | string | ` default=string.Empty` |
| `ExpiresAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Auth.SendPasswordResetCodeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Username` | string | `[Required] [MaxLength(100)] default=string.Empty` |

#### WebApi.Contracts.Auth.SessionDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `WorkspaceMode` | string? | brak atrybutu/default w deklaracji |
| `IsSystemManager` | bool | brak atrybutu/default w deklaracji |
| `Role` | string | ` default=string.Empty` |
| `UserName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `ManagerId` | int? | brak atrybutu/default w deklaracji |
| `EmployeeId` | int? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityBinds.AvailabilityBindDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Key` | string | ` default=string.Empty` |
| `Value` | string | ` default=string.Empty` |
| `IsActive` | bool | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityBinds.CreateAvailabilityBindRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Key` | string | `[Required(AllowEmptyStrings = false)] default=string.Empty` |
| `Value` | string | `[Required(AllowEmptyStrings = false)] default=string.Empty` |
| `IsActive` | bool | ` default=true` |

#### WebApi.Contracts.AvailabilityBinds.UpdateAvailabilityBindRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Key` | string | `[Required(AllowEmptyStrings = false)] default=string.Empty` |
| `Value` | string | `[Required(AllowEmptyStrings = false)] default=string.Empty` |
| `IsActive` | bool | ` default=true` |

#### WebApi.Contracts.AvailabilityGroups.AvailabilityGroupDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `PublicationStatus` | string | ` default="private"` |
| `VisibleFromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `VisibleToUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.AvailabilityGroupItemDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `MemberId` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `DisplayOrder` | int | brak atrybutu/default w deklaracji |
| `DayId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `Kind` | AvailabilityKind | brak atrybutu/default w deklaracji |
| `IntervalStr` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.CreateAvailabilityGroupRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Year` | int | `[Range(1, 9999)]` |
| `Month` | int | `[Range(1, 12)]` |
| `PublicationStatus` | string? | ` default="private"` |
| `VisibleFromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `VisibleToUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.UpdateAvailabilityGroupRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Year` | int | `[Range(1, 9999)]` |
| `Month` | int | `[Range(1, 12)]` |
| `PublicationStatus` | string? | ` default="private"` |
| `VisibleFromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `VisibleToUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.Members.AvailabilityGroupMemberDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `DisplayOrder` | int | brak atrybutu/default w deklaracji |
| `EmployeeLastModifiedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.Members.CreateAvailabilityGroupMemberRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | `[Range(1, int.MaxValue)]` |
| `DisplayOrder` | int | `[Range(0, int.MaxValue)]` |

#### WebApi.Contracts.AvailabilityGroups.Members.UpdateAvailabilityGroupMemberRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | `[Range(1, int.MaxValue)]` |
| `DisplayOrder` | int | `[Range(0, int.MaxValue)]` |

#### WebApi.Contracts.AvailabilityGroups.Slots.AvailabilitySlotDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `AvailabilityGroupMemberId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `Kind` | AvailabilityKind | brak atrybutu/default w deklaracji |
| `IntervalStr` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.Slots.CreateAvailabilitySlotRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `AvailabilityGroupMemberId` | int | `[Range(1, int.MaxValue)]` |
| `DayOfMonth` | int | `[Range(1, 31)]` |
| `Kind` | AvailabilityKind | `[Required]` |
| `IntervalStr` | string? | `[StringLength(200)]` |

#### WebApi.Contracts.AvailabilityGroups.Slots.UpdateAvailabilitySlotRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `AvailabilityGroupMemberId` | int | `[Range(1, int.MaxValue)]` |
| `DayOfMonth` | int | `[Range(1, 31)]` |
| `Kind` | AvailabilityKind | `[Required]` |
| `IntervalStr` | string? | `[StringLength(200)]` |

#### WebApi.Contracts.AvailabilityGroups.Transfers.AvailabilityTransferHintDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `TargetGroupId` | int | brak atrybutu/default w deklaracji |
| `TargetGroupName` | string | ` default=string.Empty` |
| `Kind` | AvailabilityKind | brak atrybutu/default w deklaracji |
| `IntervalStr` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.Transfers.AvailabilityTransferResultDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `SourceGroupId` | int | brak atrybutu/default w deklaracji |
| `SourceGroupName` | string | ` default=string.Empty` |
| `TargetGroupId` | int | brak atrybutu/default w deklaracji |
| `TargetGroupName` | string | ` default=string.Empty` |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonths` | `List`&lt;int&gt; | ` default=[]` |

#### WebApi.Contracts.AvailabilityGroups.Transfers.AvailabilityTransferSourceDayDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `Kind` | AvailabilityKind | brak atrybutu/default w deklaracji |
| `IntervalStr` | string? | brak atrybutu/default w deklaracji |
| `CanTransfer` | bool | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.AvailabilityGroups.Transfers.AvailabilityTransferSourceDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `GroupId` | int | brak atrybutu/default w deklaracji |
| `GroupName` | string | ` default=string.Empty` |
| `MemberId` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `Days` | `List`&lt;AvailabilityTransferSourceDayDto&gt; | ` default=[]` |

#### WebApi.Contracts.AvailabilityGroups.Transfers.TransferAvailabilityDaysRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `SourceGroupId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonths` | `List`&lt;int&gt; | ` default=[]` |

#### WebApi.Contracts.Communications.CommunicationMessageDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Title` | string | ` default=string.Empty` |
| `Body` | string | ` default=string.Empty` |
| `VisibleFromUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `DeadlineAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `CreatedByManagerId` | int? | brak atrybutu/default w deklaracji |
| `CreatedByManagerName` | string | ` default=string.Empty` |
| `IsActive` | bool | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Communications.CreateCommunicationMessageRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Title` | string? | brak atrybutu/default w deklaracji |
| `Body` | string? | brak atrybutu/default w deklaracji |
| `VisibleFromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `DeadlineAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Communications.UpdateCommunicationMessageRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Title` | string? | brak atrybutu/default w deklaracji |
| `Body` | string? | brak atrybutu/default w deklaracji |
| `VisibleFromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `DeadlineAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.ContainerDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `Note` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.CreateContainerRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Note` | string? | `[MaxLength(1000)]` |

#### WebApi.Contracts.Containers.UpdateContainerRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Note` | string? | `[MaxLength(1000)]` |

#### WebApi.Contracts.Containers.Graphs.ContainerGraphDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ContainerId` | int | brak atrybutu/default w deklaracji |
| `ShopId` | int | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `PublicationStatus` | string | ` default="private"` |
| `PeoplePerShift` | int | brak atrybutu/default w deklaracji |
| `Shift1Time` | string | ` default=string.Empty` |
| `Shift2Time` | string | ` default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | brak atrybutu/default w deklaracji |
| `MaxConsecutiveDays` | int | brak atrybutu/default w deklaracji |
| `MaxConsecutiveFull` | int | brak atrybutu/default w deklaracji |
| `MaxFullPerMonth` | int | brak atrybutu/default w deklaracji |
| `Note` | string? | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.CreateContainerGraphRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ShopId` | int | `[Range(1, int.MaxValue)]` |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Year` | int | `[Range(1, 9999)]` |
| `Month` | int | `[Range(1, 12)]` |
| `PublicationStatus` | string? | ` default="private"` |
| `PeoplePerShift` | int | `[Range(1, int.MaxValue)]` |
| `Shift1Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `Shift2Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | `[Range(1, int.MaxValue)]` |
| `MaxConsecutiveDays` | int | `[Range(1, int.MaxValue)]` |
| `MaxConsecutiveFull` | int | `[Range(1, int.MaxValue)]` |
| `MaxFullPerMonth` | int | `[Range(1, int.MaxValue)]` |
| `Note` | string? | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | `[Range(1, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.CreateGraphRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ShopId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Year` | int | `[Required] [Range(1, 9999)]` |
| `Month` | int | `[Required] [Range(1, 12)]` |
| `PublicationStatus` | string? | ` default="private"` |
| `AllowSwap` | bool | ` default=true` |
| `PeoplePerShift` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Shift1Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `Shift2Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxConsecutiveDays` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxConsecutiveFull` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxFullPerMonth` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Note` | string? | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | `[Range(1, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.GenerateGraphPreviewEmployeeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MinHoursMonth` | int? | `[Range(0, int.MaxValue)]` |
| `DisplayOrder` | int | `[Range(0, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.GenerateGraphPreviewRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `GraphId` | int? | `[Range(1, int.MaxValue)]` |
| `Graph` | CreateGraphRequest | `[Required] default=new()` |
| `Employees` | `List`&lt;GenerateGraphPreviewEmployeeRequest&gt; | `[Required] [MinLength(1)] default=new()` |

#### WebApi.Contracts.Containers.Graphs.GenerateGraphRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Overwrite` | bool | ` default=true` |
| `DryRun` | bool | ` default=false` |
| `ReturnSlots` | bool | ` default=true` |

#### WebApi.Contracts.Containers.Graphs.GenerateGraphResponse

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ContainerId` | int | brak atrybutu/default w deklaracji |
| `GraphId` | int | brak atrybutu/default w deklaracji |
| `GeneratedSlotsCount` | int | brak atrybutu/default w deklaracji |
| `WrittenSlotsCount` | int | brak atrybutu/default w deklaracji |
| `Slots` | `IEnumerable`&lt;GraphSlotDto&gt;? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.GraphDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ContainerId` | int | brak atrybutu/default w deklaracji |
| `ShopId` | int | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `PublicationStatus` | string | ` default="private"` |
| `AllowSwap` | bool | ` default=true` |
| `PeoplePerShift` | int | brak atrybutu/default w deklaracji |
| `Shift1Time` | string | ` default=string.Empty` |
| `Shift2Time` | string | ` default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | brak atrybutu/default w deklaracji |
| `MaxConsecutiveDays` | int | brak atrybutu/default w deklaracji |
| `MaxConsecutiveFull` | int | brak atrybutu/default w deklaracji |
| `MaxFullPerMonth` | int | brak atrybutu/default w deklaracji |
| `Note` | string? | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | brak atrybutu/default w deklaracji |
| `LastUpdatedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.UpdateContainerGraphRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ShopId` | int | `[Range(1, int.MaxValue)]` |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Year` | int | `[Range(1, 9999)]` |
| `Month` | int | `[Range(1, 12)]` |
| `PublicationStatus` | string? | ` default="private"` |
| `PeoplePerShift` | int | `[Range(1, int.MaxValue)]` |
| `Shift1Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `Shift2Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | `[Range(1, int.MaxValue)]` |
| `MaxConsecutiveDays` | int | `[Range(1, int.MaxValue)]` |
| `MaxConsecutiveFull` | int | `[Range(1, int.MaxValue)]` |
| `MaxFullPerMonth` | int | `[Range(1, int.MaxValue)]` |
| `Note` | string? | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | `[Range(1, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.UpdateGraphRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ShopId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Year` | int | `[Required] [Range(1, 9999)]` |
| `Month` | int | `[Required] [Range(1, 12)]` |
| `PublicationStatus` | string? | ` default="private"` |
| `AllowSwap` | bool | ` default=true` |
| `PeoplePerShift` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Shift1Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `Shift2Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxConsecutiveDays` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxConsecutiveFull` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxFullPerMonth` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Note` | string? | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | `[Range(1, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.UpdateGraphsPublicationRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `PublicationStatus` | string | ` default="private"` |
| `AllowSwap` | bool? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.CellStyles.GraphCellStyleDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `BackgroundColorArgb` | int? | brak atrybutu/default w deklaracji |
| `TextColorArgb` | int? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.CellStyles.UpsertGraphCellStyleRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DayOfMonth` | int | `[Required] [Range(1, 31)]` |
| `EmployeeId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `BackgroundColorArgb` | int? | brak atrybutu/default w deklaracji |
| `TextColorArgb` | int? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.Employees.AddGraphEmployeeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MinHoursMonth` | int? | brak atrybutu/default w deklaracji |
| `DisplayOrder` | int | `[Range(0, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.Employees.GraphEmployeeDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `MinHoursMonth` | int? | brak atrybutu/default w deklaracji |
| `DisplayOrder` | int | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.Employees.UpdateGraphEmployeeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MinHoursMonth` | int? | brak atrybutu/default w deklaracji |
| `DisplayOrder` | int | `[Range(0, int.MaxValue)]` |

#### WebApi.Contracts.Containers.Graphs.Slots.CreateGraphSlotRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DayOfMonth` | int | `[Required] [Range(1, 31)]` |
| `SlotNo` | int | `[Required] [Range(1, 100)]` |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `EmployeeId` | int? | brak atrybutu/default w deklaracji |
| `Status` | SlotStatus | `[Required]` |

#### WebApi.Contracts.Containers.Graphs.Slots.GraphSlotDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `SlotNo` | int | brak atrybutu/default w deklaracji |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `EmployeeId` | int? | brak atrybutu/default w deklaracji |
| `Status` | SlotStatus | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Containers.Graphs.Slots.ReplaceGraphSlotsRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Slots` | `List`&lt;CreateGraphSlotRequest&gt; | `[Required] default=[]` |

#### WebApi.Contracts.Containers.Graphs.Slots.UpdateGraphSlotRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DayOfMonth` | int | `[Required] [Range(1, 31)]` |
| `SlotNo` | int | `[Required] [Range(1, 100)]` |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `EmployeeId` | int? | brak atrybutu/default w deklaracji |
| `Status` | SlotStatus | `[Required]` |

#### WebApi.Contracts.Containers.SchedulePresets.CreateSchedulePresetEmployeeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MinHoursMonth` | int | `[Required] [Range(0, int.MaxValue)]` |

#### WebApi.Contracts.Containers.SchedulePresets.CreateSchedulePresetRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `ScheduleName` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `ShopId` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Year` | int | `[Required] [Range(1, 9999)]` |
| `Month` | int | `[Required] [Range(1, 12)]` |
| `PeoplePerShift` | int | `[Required] [Range(1, int.MaxValue)]` |
| `Shift1Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `Shift2Time` | string | `[Required] [MaxLength(50)] default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxConsecutiveDays` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxConsecutiveFull` | int | `[Required] [Range(1, int.MaxValue)]` |
| `MaxFullPerMonth` | int | `[Required] [Range(1, int.MaxValue)]` |
| `AvailabilityGroupId` | int? | `[Range(1, int.MaxValue)]` |
| `Employees` | `IReadOnlyList`&lt;CreateSchedulePresetEmployeeRequest&gt; | ` default=Array.Empty<CreateSchedulePresetEmployeeRequest>()` |

#### WebApi.Contracts.Containers.SchedulePresets.SchedulePresetDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ContainerId` | int? | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `ScheduleName` | string | ` default=string.Empty` |
| `ShopId` | int | brak atrybutu/default w deklaracji |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `PeoplePerShift` | int | brak atrybutu/default w deklaracji |
| `Shift1Time` | string | ` default=string.Empty` |
| `Shift2Time` | string | ` default=string.Empty` |
| `MaxHoursPerEmpMonth` | int | brak atrybutu/default w deklaracji |
| `MaxConsecutiveDays` | int | brak atrybutu/default w deklaracji |
| `MaxConsecutiveFull` | int | brak atrybutu/default w deklaracji |
| `MaxFullPerMonth` | int | brak atrybutu/default w deklaracji |
| `AvailabilityGroupId` | int? | brak atrybutu/default w deklaracji |
| `Employees` | `IReadOnlyList`&lt;SchedulePresetEmployeeDto&gt; | ` default=Array.Empty<SchedulePresetEmployeeDto>()` |

#### WebApi.Contracts.Containers.SchedulePresets.SchedulePresetEmployeeDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `MinHoursMonth` | int | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.EmployeeAvailability.EmployeeAvailabilityGroupDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `VisibleFromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `VisibleToUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `PublishedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `NotificationId` | string? | brak atrybutu/default w deklaracji |
| `NotificationOccurredAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `CanSubmit` | bool | brak atrybutu/default w deklaracji |
| `IsEditLocked` | bool | brak atrybutu/default w deklaracji |
| `EditLockedBy` | string? | brak atrybutu/default w deklaracji |
| `EmployeeLastModifiedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `Slots` | `IReadOnlyList`&lt;AvailabilitySlotDto&gt; | ` default=Array.Empty<AvailabilitySlotDto>()` |

#### WebApi.Contracts.EmployeeAvailability.UpdateEmployeeAvailabilityRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Slots` | `List`&lt;UpdateEmployeeAvailabilitySlotRequest&gt; | `[Required] default=[]` |

#### WebApi.Contracts.EmployeeAvailability.UpdateEmployeeAvailabilitySlotRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DayOfMonth` | int | `[Range(1, 31)]` |
| `Kind` | AvailabilityKind | brak atrybutu/default w deklaracji |
| `IntervalStr` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.EmployeeProfile.CompletePasswordResetRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Code` | string | ` default=string.Empty` |
| `NewPassword` | string | ` default=string.Empty` |

#### WebApi.Contracts.EmployeeProfile.EmployeeProfileDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `Username` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `RecoveryEmail` | string? | brak atrybutu/default w deklaracji |
| `Phone` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.EmployeeProfile.PasswordResetCodeDispatchDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DeliveryHint` | string | ` default=string.Empty` |
| `ExpiresAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.EmployeeProfile.UpdateEmployeeProfileRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `RecoveryEmail` | string? | brak atrybutu/default w deklaracji |
| `Phone` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.EmployeeSchedules.EmployeeScheduleDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ContainerId` | int | brak atrybutu/default w deklaracji |
| `ContainerName` | string | ` default=string.Empty` |
| `ShopId` | int | brak atrybutu/default w deklaracji |
| `ShopName` | string | ` default=string.Empty` |
| `Name` | string | ` default=string.Empty` |
| `Note` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `PublicationStatus` | string | ` default="public"` |
| `AllowSwap` | bool | ` default=true` |
| `LastUpdatedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `PublishedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `Employees` | `IReadOnlyList`&lt;EmployeeScheduleEmployeeDto&gt; | ` default=Array.Empty<EmployeeScheduleEmployeeDto>()` |
| `Slots` | `IReadOnlyList`&lt;EmployeeScheduleSlotDto&gt; | ` default=Array.Empty<EmployeeScheduleSlotDto>()` |
| `RelatedScheduleAssignments` | `IReadOnlyList`&lt;EmployeeScheduleRelatedAssignmentDto&gt; | ` default=Array.Empty<EmployeeScheduleRelatedAssignmentDto>()` |

#### WebApi.Contracts.EmployeeSchedules.EmployeeScheduleEmployeeDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `FirstName` | string | ` default=string.Empty` |
| `LastName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `MinHoursMonth` | int? | brak atrybutu/default w deklaracji |
| `DisplayOrder` | int | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.EmployeeSchedules.EmployeeScheduleRelatedAssignmentDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `ScheduleName` | string | ` default=string.Empty` |

#### WebApi.Contracts.EmployeeSchedules.EmployeeScheduleSlotDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `SlotNo` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int? | brak atrybutu/default w deklaracji |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `Status` | string | ` default=string.Empty` |

#### WebApi.Contracts.Employees.CreateEmployeeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `FirstName` | string | `[Required] [MaxLength(100)] default=string.Empty` |
| `LastName` | string | `[Required] [MaxLength(100)] default=string.Empty` |
| `Phone` | string? | `[Phone]` |
| `Email` | string? | `[EmailAddress]` |
| `Username` | string? | `[MaxLength(100)]` |
| `Password` | string? | `[MaxLength(200)]` |

#### WebApi.Contracts.Employees.EmployeeDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `FirstName` | string | ` default=string.Empty` |
| `LastName` | string | ` default=string.Empty` |
| `Phone` | string? | brak atrybutu/default w deklaracji |
| `Email` | string? | brak atrybutu/default w deklaracji |
| `Username` | string? | brak atrybutu/default w deklaracji |
| `HasLoginAccount` | bool | brak atrybutu/default w deklaracji |
| `IsOnline` | bool | brak atrybutu/default w deklaracji |
| `LastLoginAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Employees.UpdateEmployeeRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `FirstName` | string | `[Required] [MaxLength(100)] default=string.Empty` |
| `LastName` | string | `[Required] [MaxLength(100)] default=string.Empty` |
| `Phone` | string? | `[Phone]` |
| `Email` | string? | `[EmailAddress]` |
| `Username` | string? | `[MaxLength(100)]` |
| `Password` | string? | `[MaxLength(200)]` |

#### WebApi.Contracts.ManagerProfile.CreateManagerRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `UserName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `RecoveryEmail` | string? | brak atrybutu/default w deklaracji |
| `Password` | string | ` default=string.Empty` |

#### WebApi.Contracts.ManagerProfile.ManagerProfileDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `UserName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `RecoveryEmail` | string? | brak atrybutu/default w deklaracji |
| `LastLoginAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `IsOnline` | bool | brak atrybutu/default w deklaracji |
| `IsSystem` | bool | brak atrybutu/default w deklaracji |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ManagerProfile.ManagerProfileUpdateResponseDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Profile` | ManagerProfileDto | ` default=new()` |
| `AccessToken` | string | ` default=string.Empty` |
| `ExpiresAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `Session` | SessionDto | ` default=new()` |

#### WebApi.Contracts.ManagerProfile.UpdateManagerProfileRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `UserName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |
| `RecoveryEmail` | string? | brak atrybutu/default w deklaracji |
| `NewPassword` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Regulations.SaveRegulationRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Title` | string | `[Required] [MaxLength(160)] default=string.Empty` |
| `Version` | string | `[Required] [MaxLength(80)] default=string.Empty` |
| `Message` | string | `[Required] [MaxLength(4000)] default=string.Empty` |
| `Pdf` | `IFormFile`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftCorrections.ApproveShiftCorrectionRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `HighlightColor` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftCorrections.CreateShiftCorrectionRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ScheduleId` | int | `[Range(1, int.MaxValue)]` |
| `ScheduleSlotId` | int | `[Range(1, int.MaxValue)]` |
| `RequestedFromTime` | string | `[Required] default=string.Empty` |
| `RequestedToTime` | string | `[Required] default=string.Empty` |

#### WebApi.Contracts.ShiftCorrections.SaveShiftCorrectionSettingRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `HighlightColor` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftCorrections.ShiftCorrectionRequestDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ContainerId` | int | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `ScheduleSlotId` | int | brak atrybutu/default w deklaracji |
| `ScheduleName` | string | ` default=string.Empty` |
| `ShopName` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `EmployeeName` | string | ` default=string.Empty` |
| `OriginalFromTime` | string | ` default=string.Empty` |
| `OriginalToTime` | string | ` default=string.Empty` |
| `RequestedFromTime` | string | ` default=string.Empty` |
| `RequestedToTime` | string | ` default=string.Empty` |
| `Status` | string | ` default=string.Empty` |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `ReviewedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `ReviewedByManagerName` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftCorrections.ShiftCorrectionSettingDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `HighlightColor` | string | ` default="#FDE68A"` |

#### WebApi.Contracts.ShiftSwaps.CreateEmployeeShiftSwapRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `RequestKind` | string? | brak atrybutu/default w deklaracji |
| `ReturnScheduleId` | int? | brak atrybutu/default w deklaracji |
| `ReturnScheduleSlotId` | int? | brak atrybutu/default w deklaracji |
| `ReturnFromTime` | string? | brak atrybutu/default w deklaracji |
| `ReturnToTime` | string? | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `ScheduleSlotId` | int | brak atrybutu/default w deklaracji |
| `FromTime` | string? | brak atrybutu/default w deklaracji |
| `ToTime` | string? | brak atrybutu/default w deklaracji |
| `TargetEmployeeId` | int? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.CreateManagerShiftSwapRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ManualColumnId` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `TargetEmployeeId` | int? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.SaveShiftSwapHighlightSettingRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `HighlightColor` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `RequestKind` | string | ` default="give"` |
| `ReturnScheduleId` | int? | brak atrybutu/default w deklaracji |
| `ReturnScheduleSlotId` | int? | brak atrybutu/default w deklaracji |
| `ReturnScheduleName` | string? | brak atrybutu/default w deklaracji |
| `ReturnShopName` | string? | brak atrybutu/default w deklaracji |
| `ReturnYear` | int? | brak atrybutu/default w deklaracji |
| `ReturnMonth` | int? | brak atrybutu/default w deklaracji |
| `ReturnDayOfMonth` | int? | brak atrybutu/default w deklaracji |
| `ReturnFromTime` | string? | brak atrybutu/default w deklaracji |
| `ReturnToTime` | string? | brak atrybutu/default w deklaracji |
| `ReturnShiftHours` | double? | brak atrybutu/default w deklaracji |
| `ReturnBeforeSnapshot` | ShiftSwapScheduleSnapshotDto? | brak atrybutu/default w deklaracji |
| `ReturnAfterSnapshot` | ShiftSwapScheduleSnapshotDto? | brak atrybutu/default w deklaracji |
| `CurrentEmployeeProjectedMonths` | `List`&lt;ShiftSwapPreviewMonthStatsDto&gt;? | brak atrybutu/default w deklaracji |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `ScheduleSlotId` | int | brak atrybutu/default w deklaracji |
| `ScheduleName` | string | ` default=string.Empty` |
| `ContainerName` | string | ` default=string.Empty` |
| `ShopName` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `FromEmployeeId` | int? | brak atrybutu/default w deklaracji |
| `FromEmployeeName` | string | ` default=string.Empty` |
| `TargetEmployeeId` | int? | brak atrybutu/default w deklaracji |
| `TargetEmployeeName` | string? | brak atrybutu/default w deklaracji |
| `AcceptedByEmployeeId` | int? | brak atrybutu/default w deklaracji |
| `AcceptedByEmployeeName` | string? | brak atrybutu/default w deklaracji |
| `Visibility` | string | ` default="public"` |
| `Status` | string | ` default="open"` |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `AcceptedAtUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `ShiftHours` | double | brak atrybutu/default w deklaracji |
| `CurrentEmployeeHoursBefore` | double | brak atrybutu/default w deklaracji |
| `CurrentEmployeeHoursAfter` | double | brak atrybutu/default w deklaracji |
| `CurrentEmployeeWorkDaysBefore` | int | brak atrybutu/default w deklaracji |
| `CurrentEmployeeWorkDaysAfter` | int | brak atrybutu/default w deklaracji |
| `CurrentEmployeeFreeDaysBefore` | int | brak atrybutu/default w deklaracji |
| `CurrentEmployeeFreeDaysAfter` | int | brak atrybutu/default w deklaracji |
| `FromEmployeeHoursBefore` | double | brak atrybutu/default w deklaracji |
| `FromEmployeeHoursAfter` | double | brak atrybutu/default w deklaracji |
| `IsManagerCreated` | bool | brak atrybutu/default w deklaracji |
| `ManualColumnId` | int? | brak atrybutu/default w deklaracji |
| `ManualColumnName` | string? | brak atrybutu/default w deklaracji |
| `IsCreatedByCurrentEmployee` | bool | brak atrybutu/default w deklaracji |
| `IsScheduleLocked` | bool | brak atrybutu/default w deklaracji |
| `CanAccept` | bool | brak atrybutu/default w deklaracji |
| `AcceptanceUnavailableReason` | string? | brak atrybutu/default w deklaracji |
| `CanCancel` | bool | brak atrybutu/default w deklaracji |
| `BeforeSnapshot` | ShiftSwapScheduleSnapshotDto? | brak atrybutu/default w deklaracji |
| `AfterSnapshot` | ShiftSwapScheduleSnapshotDto? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapEligibleWindowDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `FromTime` | string | brak atrybutu/default w deklaracji |
| `ToTime` | string | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapEmployeeDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `FirstName` | string | ` default=string.Empty` |
| `LastName` | string | ` default=string.Empty` |
| `DisplayName` | string | ` default=string.Empty` |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapHighlightSettingDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `HighlightColor` | string | ` default="#BBF7D0"` |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapOfferPreviewDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `RequestKind` | string | ` default="give"` |
| `TargetEmployeeId` | int? | brak atrybutu/default w deklaracji |
| `TargetEmployeeName` | string | ` default="Everyone"` |
| `Source` | ShiftSwapReturnCandidateDto | ` default=new()` |
| `Return` | ShiftSwapReturnCandidateDto? | brak atrybutu/default w deklaracji |
| `ProjectedMonths` | `List`&lt;ShiftSwapPreviewMonthStatsDto&gt; | ` default=[]` |
| `IsValid` | bool | brak atrybutu/default w deklaracji |
| `UnavailableReason` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapPreviewMonthStatsDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `HoursBefore` | double | brak atrybutu/default w deklaracji |
| `HoursAfter` | double | brak atrybutu/default w deklaracji |
| `WorkDaysBefore` | int | brak atrybutu/default w deklaracji |
| `WorkDaysAfter` | int | brak atrybutu/default w deklaracji |
| `FreeDaysBefore` | int | brak atrybutu/default w deklaracji |
| `FreeDaysAfter` | int | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapReturnCandidateDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ScheduleId` | int | brak atrybutu/default w deklaracji |
| `ScheduleName` | string | ` default=string.Empty` |
| `ShopName` | string | ` default=string.Empty` |
| `Year` | int | brak atrybutu/default w deklaracji |
| `Month` | int | brak atrybutu/default w deklaracji |
| `DayOfMonth` | int | brak atrybutu/default w deklaracji |
| `SlotId` | int | brak atrybutu/default w deklaracji |
| `FromTime` | string | ` default=string.Empty` |
| `ToTime` | string | ` default=string.Empty` |
| `DurationMinutes` | int | brak atrybutu/default w deklaracji |
| `CanSelect` | bool | brak atrybutu/default w deklaracji |
| `UnavailableReason` | string? | brak atrybutu/default w deklaracji |
| `SuggestedFromTime` | string? | brak atrybutu/default w deklaracji |
| `SuggestedToTime` | string? | brak atrybutu/default w deklaracji |
| `EligibleWindows` | `List`&lt;ShiftSwapEligibleWindowDto&gt; | ` default=[]` |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapReturnCandidatePageDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Items` | `List`&lt;ShiftSwapReturnCandidateDto&gt; | ` default=[]` |
| `Page` | int | brak atrybutu/default w deklaracji |
| `PageSize` | int | brak atrybutu/default w deklaracji |
| `HasMore` | bool | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapScheduleSnapshotDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Rows` | `List`&lt;ShiftSwapScheduleSnapshotRowDto&gt; | ` default=[]` |

#### WebApi.Contracts.ShiftSwaps.ShiftSwapScheduleSnapshotRowDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `EmployeeId` | int | brak atrybutu/default w deklaracji |
| `EmployeeName` | string | ` default=string.Empty` |
| `Kind` | string | ` default="employee"` |
| `DayValues` | `Dictionary`&lt;int, string&gt; | ` default=[]` |

#### WebApi.Contracts.Shops.CreateShopRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Address` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Description` | string? | `[MaxLength(1000)]` |

#### WebApi.Contracts.Shops.ShopDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Name` | string | ` default=string.Empty` |
| `Address` | string | ` default=string.Empty` |
| `Description` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.Shops.UpdateShopRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Name` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Address` | string | `[Required] [MaxLength(200)] default=string.Empty` |
| `Description` | string? | `[MaxLength(1000)]` |

#### WebApi.Contracts.WorkflowLogs.DeleteWorkflowLogsRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DeleteAll` | bool | brak atrybutu/default w deklaracji |
| `FromUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |
| `ToUtc` | `DateTimeOffset`? | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.WorkflowLogs.UpdateWorkflowLogSettingsRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `IsEnabled` | bool | brak atrybutu/default w deklaracji |
| `Audience` | string | `[Required] default=string.Empty` |

#### WebApi.Contracts.WorkflowLogs.WorkflowLogDeleteResultDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `DeletedCount` | int | brak atrybutu/default w deklaracji |

#### WebApi.Contracts.WorkflowLogs.WorkflowLogDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `OccurredAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `ActorRole` | string | ` default=string.Empty` |
| `ActorEmployeeId` | int? | brak atrybutu/default w deklaracji |
| `ActorName` | string | ` default=string.Empty` |
| `Action` | string | ` default=string.Empty` |

#### WebApi.Contracts.WorkflowLogs.WorkflowLogSettingsDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `IsEnabled` | bool | brak atrybutu/default w deklaracji |
| `Audience` | string | ` default=string.Empty` |

#### WebApi.Controllers.BrowserKeys

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `P256dh` | string | `[Required, MaxLength(512)]` |
| `Auth` | string | `[Required, MaxLength(256)]` |

#### WebApi.Controllers.DeleteRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Endpoint` | string | `[Required, MaxLength(2048)]` |

#### WebApi.Controllers.EmployeeUiStateDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ScheduleColumnOrders` | `IReadOnlyDictionary`&lt;int, int[]&gt; | ` default=new Dictionary<int, int[]>()` |
| `ReadNotificationIds` | `IReadOnlyList`&lt;string&gt; | ` default=[]` |
| `PinnedSwapIds` | `IReadOnlyList`&lt;int&gt; | ` default=[]` |

#### WebApi.Controllers.GraphVersionDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `ParentVersionId` | int? | brak atrybutu/default w deklaracji |
| `VersionNumber` | int | brak atrybutu/default w deklaracji |
| `BranchName` | string | ` default=null!` |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `AuthorName` | string | ` default=null!` |
| `EmployeeCount` | int | brak atrybutu/default w deklaracji |
| `SlotCount` | int | brak atrybutu/default w deklaracji |
| `CellStyleCount` | int | brak atrybutu/default w deklaracji |
| `IsCurrent` | bool | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.GraphVersionTreeDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `CurrentVersionId` | int? | brak atrybutu/default w deklaracji |
| `Versions` | `IReadOnlyList`&lt;GraphVersionDto&gt; | ` default=[]` |

#### WebApi.Controllers.LanguagePreference

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Language` | string | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.ManagerGraphFillColorBindDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Key` | string | ` default=string.Empty` |
| `FillColor` | string | ` default=string.Empty` |

#### WebApi.Controllers.ManagerGraphTextColorBindDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Key` | string | ` default=string.Empty` |
| `TextColor` | string | ` default=string.Empty` |

#### WebApi.Controllers.ManagerNoteDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Title` | string | ` default=string.Empty` |
| `Content` | string | ` default=string.Empty` |
| `Color` | string | ` default="yellow"` |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `UpdatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.ManagerNotepadDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Notes` | `IReadOnlyList`&lt;ManagerNoteDto&gt; | ` default=[]` |
| `State` | ManagerNotepadStateDto | ` default=new()` |

#### WebApi.Controllers.ManagerNotepadStateDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `IsExpanded` | bool | brak atrybutu/default w deklaracji |
| `IsPinned` | bool | brak atrybutu/default w deklaracji |
| `Height` | int | ` default=520` |

#### WebApi.Controllers.MarkEmployeeNotificationsReadRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `NotificationIds` | `IReadOnlyList`&lt;string&gt;? | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.SaveEmployeeScheduleColumnOrderRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `ColumnOrder` | `IReadOnlyList`&lt;int&gt;? | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.SaveManagerGraphFillColorBindRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Key` | string? | brak atrybutu/default w deklaracji |
| `FillColor` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.SaveManagerGraphTextColorBindRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Key` | string? | brak atrybutu/default w deklaracji |
| `TextColor` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.SaveManagerNoteRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Title` | string? | brak atrybutu/default w deklaracji |
| `Content` | string? | brak atrybutu/default w deklaracji |
| `Color` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.SaveManagerNotepadStateRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `IsExpanded` | bool | brak atrybutu/default w deklaracji |
| `IsPinned` | bool | brak atrybutu/default w deklaracji |
| `Height` | int | ` default=520` |

#### WebApi.Controllers.SaveSystemNewsRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Title` | string? | brak atrybutu/default w deklaracji |
| `Body` | string? | brak atrybutu/default w deklaracji |
| `Audience` | string? | brak atrybutu/default w deklaracji |
| `ImageUrl` | string? | brak atrybutu/default w deklaracji |
| `VideoUrl` | string? | brak atrybutu/default w deklaracji |

#### WebApi.Controllers.SubscriptionRequest

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Endpoint` | string | `[Required, MaxLength(2048)]` |
| `Keys` | BrowserKeys | `[Required]` |

#### WebApi.Controllers.SystemNewsDto

| Właściwość | Typ C# / nullable | Walidacja / default |
| --- | --- | --- |
| `Id` | int | brak atrybutu/default w deklaracji |
| `Title` | string | ` default=string.Empty` |
| `Body` | string | ` default=string.Empty` |
| `Audience` | string | ` default="all"` |
| `ImageUrl` | string? | brak atrybutu/default w deklaracji |
| `VideoUrl` | string? | brak atrybutu/default w deklaracji |
| `IsRead` | bool | brak atrybutu/default w deklaracji |
| `CreatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |
| `UpdatedAtUtc` | `DateTimeOffset` | brak atrybutu/default w deklaracji |

## 3. Baza danych

SQLite jest relacyjnym magazynem EF Core 10. Konfiguracja WebApi `ConnectionStrings:Default` ma pierwszeństwo; bez niej `StartupConfiguration.ResolveConnectionString` wybiera workspace w lokalnym katalogu danych aplikacji `GF3/SQLite.db` (z uwzględnieniem zapisanego wyboru bazy). Osobna kompozycja BLL `AddBusinessLogicStack()` obsługuje `GF3_CONNECTION_STRING`. Docker jawnie wskazuje `/data/SQLite.db` na trwałym woluminie. Nie utożsamiaj tych ścieżek ze wspólną lokalizacją na każdym hoście.

Startup sprawdza istnienie bazy przy `RequireExistingDatabase`, wykonuje quick_check istniejącej bazy, ustala pending migrations, opcjonalnie tworzy kopię przed migracją, stosuje EF migrations i sprawdza integralność ponownie. Migracje nie są zadaniem przeglądarki. Kopia lokalna nie jest off-site DR. Szczegóły bezpiecznego odtwarzania w procedurach eksploatacyjnych.

Katalog obejmuje wszystkie **39 encji** z DbSet i mapowania AppDbContext. Nazwy fizyczne, typy SQLite, PK, FK, indeksy/check pochodzą z aktualnego snapshotu migracji i konfiguracji modelu; nie są wnioskowane z liczby mnogiej DbSet. W tabelach pól `NULL tak` oznacza dopuszczenie null przez EF; C# w kolumnie modelu może używać enum lub konwersji czasu zamiast typu magazynu. `INTEGER` obejmuje także bool/enum/ticks; `TEXT` m.in. tekst i część dat. Wszystkie kolumny fizyczne wymieniono poniżej. Nawigacje nie tworzą dodatkowych kolumn.

### 3.1 Sklepy, pracownicy, konta i kontenery

#### shop — ShopModel

Model: ShopModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `address` | `Address : string / TEXT` | nie | — |
| `description` | `Description : string / TEXT` | tak | — |
| `name` | `Name : string / TEXT` | nie | — |

Indeksy: `HasIndex("Name") .IsUnique() .HasDatabaseName("ux_shop_name")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Name [Required]`; `Address [Required]`.

#### employee — EmployeeModel

Model: EmployeeModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `email` | `Email : string / TEXT` | tak | — |
| `first_name` | `FirstName : string / TEXT` | nie | — |
| `is_deleted` | `IsDeleted : bool / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(false)` |
| `last_name` | `LastName : string / TEXT` | nie | — |
| `phone` | `Phone : string / TEXT` | tak | — |

Indeksy: `HasIndex("FirstName", "LastName") .IsUnique() .HasDatabaseName("ux_employee_full_name") .HasFilter("\"is_deleted\" = 0")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `FirstName [Required]`; `LastName [Required]`.

#### employee_account — EmployeeAccountModel

Model: EmployeeAccountModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `language` | `Language : string / TEXT` | nie | `.HasMaxLength(2)` |
| `last_login_at_utc` | `LastLoginAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `last_seen_at_utc` | `LastSeenAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `password_hash` | `PasswordHash : string / TEXT` | nie | — |
| `password_reset_code_hash` | `PasswordResetCodeHash : string / TEXT` | tak | — |
| `password_reset_expires_at_utc` | `PasswordResetExpiresAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `password_reset_requested_at_utc` | `PasswordResetRequestedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `password_updated_at_utc` | `PasswordUpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `session_version` | `SessionVersion : int / INTEGER` | nie | — |
| `username` | `Username : string / TEXT` | nie | `.HasMaxLength(100)`; `.UseCollation("NOCASE")` |

Indeksy: `HasIndex("EmployeeId") .IsUnique() .HasDatabaseName("ux_employee_account_employee")`; `HasIndex("Username") .IsUnique() .HasDatabaseName("ux_employee_account_username")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithOne("Account") .HasForeignKey("DataAccessLayer.Models.EmployeeAccountModel", "EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Language [Required]`; `EmployeeId [Required]`; `Username [Required]`; `PasswordHash [Required]`.

#### manager_account — ManagerAccountModel

Model: ManagerAccountModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `display_name` | `DisplayName : string / TEXT` | nie | `.HasMaxLength(160)` |
| `is_system` | `IsSystem : bool / INTEGER` | nie | — |
| `language` | `Language : string / TEXT` | nie | `.HasMaxLength(2)` |
| `last_login_at_utc` | `LastLoginAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `password_hash` | `PasswordHash : string / TEXT` | nie | — |
| `password_reset_code_hash` | `PasswordResetCodeHash : string / TEXT` | tak | — |
| `password_reset_expires_at_utc` | `PasswordResetExpiresAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `password_reset_requested_at_utc` | `PasswordResetRequestedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `password_updated_at_utc` | `PasswordUpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `recovery_email` | `RecoveryEmail : string / TEXT` | tak | `.HasMaxLength(254)` |
| `updated_at_utc` | `UpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `username` | `Username : string / TEXT` | nie | `.HasMaxLength(100)`; `.UseCollation("NOCASE")` |

Indeksy: `HasIndex("IsSystem") .IsUnique() .HasDatabaseName("ux_manager_account_system") .HasFilter("\"is_system\" = 1")`; `HasIndex("Username") .IsUnique() .HasDatabaseName("ux_manager_account_username")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Language [Required]`; `Username [Required]`; `DisplayName [Required]`; `PasswordHash [Required]`; `RecoveryEmail [MaxLength(254)]`.

#### container — ContainerModel

Model: ContainerModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `name` | `Name : string / TEXT` | nie | — |
| `note` | `Note : string / TEXT` | tak | — |

Indeksy: `HasIndex("Name") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Name [Required]`.

### 3.2 Grafiki, sloty, uczestnicy, presety i wersje

#### schedule_cell_style — ScheduleCellStyleModel

Model: ScheduleCellStyleModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `background_color_argb` | `BackgroundColorArgb : int? / INTEGER` | tak | — |
| `day_of_month` | `DayOfMonth : int / INTEGER` | nie | — |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `text_color_argb` | `TextColorArgb : int? / INTEGER` | tak | — |

Indeksy: `HasIndex("EmployeeId")`; `HasIndex("ScheduleId", "DayOfMonth", "EmployeeId") .IsUnique() .HasDatabaseName("ux_sched_cell_style")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany("CellStyles") .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_schedule_cell_style_dom", "day_of_month BETWEEN 1 AND 31")`.

Walidacja deklaracji modelu: `ScheduleId [Required]`; `DayOfMonth [Required]`; `EmployeeId [Required]`.

#### schedule_employee — ScheduleEmployeeModel

Model: ScheduleEmployeeModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `display_order` | `DisplayOrder : int / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(0)` |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `min_hours_month` | `MinHoursMonth : int? / INTEGER` | tak | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("EmployeeId")`; `HasIndex("ScheduleId", "EmployeeId") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany("ScheduleEmployees") .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany("Employees") .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `ScheduleId [Required]`; `EmployeeId [Required]`; `DisplayOrder [Required]`.

#### schedule — ScheduleModel

Model: ScheduleModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `accepted_swap_highlight_color` | `AcceptedSwapHighlightColor : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasMaxLength(7)`; `.HasDefaultValue("#BBF7D0")` |
| `allow_swap` | `AllowSwap : bool / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(true)` |
| `availability_group_id` | `AvailabilityGroupId : int? / INTEGER` | tak | — |
| `container_id` | `ContainerId : int / INTEGER` | nie | — |
| `max_consecutive_days` | `MaxConsecutiveDays : int / INTEGER` | nie | — |
| `max_consecutive_full` | `MaxConsecutiveFull : int / INTEGER` | nie | — |
| `max_full_per_month` | `MaxFullPerMonth : int / INTEGER` | nie | — |
| `max_hours_per_emp_month` | `MaxHoursPerEmpMonth : int / INTEGER` | nie | — |
| `month` | `Month : int / INTEGER` | nie | — |
| `name` | `Name : string / TEXT` | nie | — |
| `note` | `Note : string / TEXT` | tak | — |
| `people_per_shift` | `PeoplePerShift : int / INTEGER` | nie | — |
| `publication_status` | `PublicationStatus : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Private")` |
| `published_at_utc` | `PublishedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `shift1_time` | `Shift1Time : string / TEXT` | nie | — |
| `shift2_time` | `Shift2Time : string / TEXT` | nie | — |
| `shop_id` | `ShopId : int / INTEGER` | nie | — |
| `year` | `Year : int / INTEGER` | nie | — |

Indeksy: `HasIndex("AvailabilityGroupId") .HasDatabaseName("ix_sched_avail_group")`; `HasIndex("ContainerId") .HasDatabaseName("ix_sched_container")`; `HasIndex("PublicationStatus") .HasDatabaseName("ix_sched_publication_status")`; `HasIndex("ContainerId", "ShopId") .HasDatabaseName("ix_sched_container_shop")`; `HasIndex("ShopId", "Year", "Month") .HasDatabaseName("ix_sched_shop_month")`; `HasIndex(new[] { "ContainerId" }, "ix_sched_container")`; `HasIndex(new[] { "ContainerId", "ShopId" }, "ix_sched_container_shop")`; `HasIndex(new[] { "ShopId", "Year", "Month" }, "ix_sched_shop_month")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.AvailabilityGroupModel", "AvailabilityGroup") .WithMany() .HasForeignKey("AvailabilityGroupId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ContainerModel", "Container") .WithMany("Schedules") .HasForeignKey("ContainerId") .OnDelete(DeleteBehavior.Restrict) .IsRequired()`; `HasOne("DataAccessLayer.Models.ShopModel", "Shop") .WithMany("Schedules") .HasForeignKey("ShopId") .OnDelete(DeleteBehavior.Restrict) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_schedule_max_consecutive_days", "max_consecutive_days >= 1")`; `HasCheckConstraint("ck_schedule_max_consecutive_full", "max_consecutive_full >= 1")`; `HasCheckConstraint("ck_schedule_max_full_per_month", "max_full_per_month >= 0")`; `HasCheckConstraint("ck_schedule_max_hours_per_emp_month", "max_hours_per_emp_month >= 0")`; `HasCheckConstraint("ck_schedule_month", "month BETWEEN 1 AND 12")`; `HasCheckConstraint("ck_schedule_people_per_shift", "people_per_shift >= 1")`; `HasCheckConstraint("ck_schedule_shift1_format", "shift1_time LIKE '__:__ - __:__'")`; `HasCheckConstraint("ck_schedule_shift2_format", "shift2_time LIKE '__:__ - __:__'")`.

Walidacja deklaracji modelu: `ContainerId [Required]`; `ShopId [Required]`; `Name [Required]`; `Year [Required]`; `Month [Required]`; `PublicationStatus [Required]`; `AllowSwap [Required]`; `AcceptedSwapHighlightColor [Required]`; `PeoplePerShift [Required]`; `Shift1Time [Required]`; `Shift2Time [Required]`; `MaxHoursPerEmpMonth [Required]`; `MaxConsecutiveDays [Required]`; `MaxConsecutiveFull [Required]`; `MaxFullPerMonth [Required]`.

#### schedule_preset_employee — SchedulePresetEmployeeModel

Model: SchedulePresetEmployeeModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `min_hours_month` | `MinHoursMonth : int / INTEGER` | nie | — |
| `schedule_preset_id` | `SchedulePresetId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("EmployeeId") .HasDatabaseName("ix_schedule_preset_employee_employee")`; `HasIndex("SchedulePresetId", "EmployeeId") .IsUnique() .HasDatabaseName("ux_schedule_preset_employee")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", null) .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.SchedulePresetModel", "SchedulePreset") .WithMany("Employees") .HasForeignKey("SchedulePresetId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_schedule_preset_employee_min_hours", "min_hours_month >= 0")`.

Walidacja deklaracji modelu: `SchedulePresetId [Required]`; `EmployeeId [Required]`; `MinHoursMonth [Required]`.

#### schedule_preset — SchedulePresetModel

Model: SchedulePresetModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `availability_group_id` | `AvailabilityGroupId : int? / INTEGER` | tak | — |
| `container_id` | `ContainerId : int? / INTEGER` | tak | — |
| `max_consecutive_days` | `MaxConsecutiveDays : int / INTEGER` | nie | — |
| `max_consecutive_full` | `MaxConsecutiveFull : int / INTEGER` | nie | — |
| `max_full_per_month` | `MaxFullPerMonth : int / INTEGER` | nie | — |
| `max_hours_per_emp_month` | `MaxHoursPerEmpMonth : int / INTEGER` | nie | — |
| `month` | `Month : int / INTEGER` | nie | — |
| `name` | `Name : string / TEXT` | nie | — |
| `people_per_shift` | `PeoplePerShift : int / INTEGER` | nie | — |
| `schedule_name` | `ScheduleName : string / TEXT` | nie | — |
| `shift1_time` | `Shift1Time : string / TEXT` | nie | — |
| `shift2_time` | `Shift2Time : string / TEXT` | nie | — |
| `shop_id` | `ShopId : int / INTEGER` | nie | — |
| `year` | `Year : int / INTEGER` | nie | — |

Indeksy: `HasIndex("AvailabilityGroupId") .HasDatabaseName("ix_schedule_preset_avail_group")`; `HasIndex("ContainerId") .HasDatabaseName("ix_schedule_preset_container")`; `HasIndex("ShopId") .HasDatabaseName("ix_schedule_preset_shop")`; `HasIndex("ContainerId", "Name") .IsUnique() .HasDatabaseName("ux_schedule_preset_container_name")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.AvailabilityGroupModel", null) .WithMany() .HasForeignKey("AvailabilityGroupId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ContainerModel", null) .WithMany() .HasForeignKey("ContainerId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ShopModel", null) .WithMany() .HasForeignKey("ShopId") .OnDelete(DeleteBehavior.Restrict) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_schedule_preset_max_consecutive_days", "max_consecutive_days >= 1")`; `HasCheckConstraint("ck_schedule_preset_max_consecutive_full", "max_consecutive_full >= 1")`; `HasCheckConstraint("ck_schedule_preset_max_full_per_month", "max_full_per_month >= 1")`; `HasCheckConstraint("ck_schedule_preset_max_hours_per_emp_month", "max_hours_per_emp_month >= 1")`; `HasCheckConstraint("ck_schedule_preset_month", "month BETWEEN 1 AND 12")`; `HasCheckConstraint("ck_schedule_preset_people_per_shift", "people_per_shift >= 1")`; `HasCheckConstraint("ck_schedule_preset_shift1_format", "shift1_time LIKE '__:__ - __:__'")`; `HasCheckConstraint("ck_schedule_preset_shift2_format", "shift2_time LIKE '__:__ - __:__'")`.

Walidacja deklaracji modelu: `Name [Required]`; `ScheduleName [Required]`; `ShopId [Required]`; `Year [Required]`; `Month [Required]`; `PeoplePerShift [Required]`; `Shift1Time [Required]`; `Shift2Time [Required]`; `MaxHoursPerEmpMonth [Required]`; `MaxConsecutiveDays [Required]`; `MaxConsecutiveFull [Required]`; `MaxFullPerMonth [Required]`.

#### schedule_slot — ScheduleSlotModel

Model: ScheduleSlotModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `day_of_month` | `DayOfMonth : int / INTEGER` | nie | — |
| `employee_id` | `EmployeeId : int? / INTEGER` | tak | — |
| `from_time` | `FromTime : string / TEXT` | nie | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `slot_no` | `SlotNo : int / INTEGER` | nie | — |
| `status` | `Status : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("UNFURNISHED")` |
| `to_time` | `ToTime : string / TEXT` | nie | — |

Indeksy: `HasIndex("EmployeeId")`; `HasIndex("ScheduleId", "DayOfMonth", "FromTime", "ToTime", "EmployeeId") .IsUnique() .HasDatabaseName("ux_slot_unique_emp_per_time") .HasFilter("employee_id IS NOT NULL")`; `HasIndex("ScheduleId", "DayOfMonth", "FromTime", "ToTime", "SlotNo") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany("ScheduleSlots") .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany("Slots") .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_schedule_slot_dom", "day_of_month BETWEEN 1 AND 31")`; `HasCheckConstraint("ck_schedule_slot_slot_no", "slot_no >= 1")`; `HasCheckConstraint("ck_schedule_slot_status_pair", "((status='UNFURNISHED' AND employee_id IS NULL) OR (status='ASSIGNED' AND employee_id IS NOT NULL))")`; `HasCheckConstraint("ck_schedule_slot_time_format", "from_time LIKE '__:__' AND to_time LIKE '__:__'")`; `HasCheckConstraint("ck_schedule_slot_time_order", "from_time < to_time")`.

Walidacja deklaracji modelu: `ScheduleId [Required]`; `DayOfMonth [Required]`; `SlotNo [Required]`; `Status [Required]`; `FromTime [Required]`; `ToTime [Required]`.

#### schedule_version — ScheduleVersionModel

Model: ScheduleVersionModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `branch_name` | `BranchName : string / TEXT` | nie | `.HasMaxLength(80)` |
| `cell_style_count` | `CellStyleCount : int / INTEGER` | nie | — |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `created_by_manager_id` | `CreatedByManagerId : int? / INTEGER` | tak | — |
| `created_by_manager_name` | `CreatedByManagerName : string / TEXT` | nie | `.HasMaxLength(160)` |
| `employee_count` | `EmployeeCount : int / INTEGER` | nie | — |
| `parent_version_id` | `ParentVersionId : int? / INTEGER` | tak | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `slot_count` | `SlotCount : int / INTEGER` | nie | — |
| `snapshot_json` | `SnapshotJson : string / TEXT` | nie | — |
| `version_number` | `VersionNumber : int / INTEGER` | nie | — |

Indeksy: `HasIndex("ParentVersionId")`; `HasIndex(new[] { "ScheduleId", "BranchName", "VersionNumber" }, "ix_schedule_version_branch")`; `HasIndex(new[] { "ScheduleId", "VersionNumber" }, "ux_schedule_version_number") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ScheduleVersionModel", "ParentVersion") .WithMany("ChildVersions") .HasForeignKey("ParentVersionId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany("Versions") .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_schedule_version_counts", "employee_count >= 0 AND slot_count >= 0 AND cell_style_count >= 0")`; `HasCheckConstraint("ck_schedule_version_number", "version_number >= 1")`.

Walidacja deklaracji modelu: `BranchName [MaxLength(80)]`; `CreatedByManagerName [MaxLength(160)]`.

#### schedule_version_state — ScheduleVersionStateModel

Model: ScheduleVersionStateModel. PK: `"ScheduleId"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `current_version_id` | `CurrentVersionId : int? / INTEGER` | tak | — |

Indeksy: `HasIndex("CurrentVersionId")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ScheduleVersionModel", "CurrentVersion") .WithMany() .HasForeignKey("CurrentVersionId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithOne("VersionState") .HasForeignKey("DataAccessLayer.Models.ScheduleVersionStateModel", "ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

### 3.3 Dyspozycyjnosc, skroty i transfery

#### availability_group_day — AvailabilityGroupDayModel

Model: AvailabilityGroupDayModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `availability_group_member_id` | `AvailabilityGroupMemberId : int / INTEGER` | nie | — |
| `day_of_month` | `DayOfMonth : int / INTEGER` | nie | — |
| `interval_str` | `IntervalStr : string / TEXT` | tak | — |
| `kind` | `Kind : string / TEXT` | nie | — |

Indeksy: `HasIndex("AvailabilityGroupMemberId", "DayOfMonth") .IsUnique() .HasDatabaseName("ux_avail_group_day_member_dom")`; `HasIndex(new[] { "AvailabilityGroupMemberId", "DayOfMonth" }, "ux_avail_group_day_member_dom") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.AvailabilityGroupMemberModel", "AvailabilityGroupMember") .WithMany("Days") .HasForeignKey("AvailabilityGroupMemberId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_avail_group_day_dom", "day_of_month BETWEEN 1 AND 31")`; `HasCheckConstraint("ck_avail_group_day_kind_interval", "((kind = 'INT' AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11) OR (kind = 'ANY' AND interval_str IS NULL) OR kind = 'NONE')")`.

Walidacja deklaracji modelu: `AvailabilityGroupMemberId [Required]`; `DayOfMonth [Required]`; `Kind [Required]`.

#### availability_group_day_transfer — AvailabilityGroupDayTransferModel

Model: AvailabilityGroupDayTransferModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `day_of_month` | `DayOfMonth : int / INTEGER` | nie | — |
| `source_member_id` | `SourceMemberId : int / INTEGER` | nie | — |
| `target_member_id` | `TargetMemberId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("SourceMemberId", "DayOfMonth") .IsUnique() .HasDatabaseName("ux_avail_transfer_source_day")`; `HasIndex("TargetMemberId", "DayOfMonth") .IsUnique() .HasDatabaseName("ux_avail_transfer_target_day")`; `HasIndex(new[] { "SourceMemberId", "DayOfMonth" }, "ux_avail_transfer_source_day") .IsUnique()`; `HasIndex(new[] { "TargetMemberId", "DayOfMonth" }, "ux_avail_transfer_target_day") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.AvailabilityGroupMemberModel", "SourceMember") .WithMany() .HasForeignKey("SourceMemberId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.AvailabilityGroupMemberModel", "TargetMember") .WithMany() .HasForeignKey("TargetMemberId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_avail_transfer_distinct_members", "source_member_id <> target_member_id")`; `HasCheckConstraint("ck_avail_transfer_dom", "day_of_month BETWEEN 1 AND 31")`.

Walidacja deklaracji modelu: `SourceMemberId [Required]`; `TargetMemberId [Required]`; `DayOfMonth [Required]`; `CreatedAtUtc [Required]`.

#### availability_group_member — AvailabilityGroupMemberModel

Model: AvailabilityGroupMemberModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `availability_group_id` | `AvailabilityGroupId : int / INTEGER` | nie | — |
| `display_order` | `DisplayOrder : int / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(0)` |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `employee_last_modified_at_utc` | `EmployeeLastModifiedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `joined_at_utc` | `JoinedAtUtc : DateTimeOffset? / TEXT` | tak | — |

Indeksy: `HasIndex("EmployeeId")`; `HasIndex("AvailabilityGroupId", "EmployeeId") .IsUnique() .HasDatabaseName("ux_avail_group_member_group_emp")`; `HasIndex(new[] { "AvailabilityGroupId", "EmployeeId" }, "ux_avail_group_member_group_emp") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.AvailabilityGroupModel", "AvailabilityGroup") .WithMany("Members") .HasForeignKey("AvailabilityGroupId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `AvailabilityGroupId [Required]`; `EmployeeId [Required]`; `DisplayOrder [Required]`.

#### availability_group — AvailabilityGroupModel

Model: AvailabilityGroupModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `month` | `Month : int / INTEGER` | nie | — |
| `name` | `Name : string / TEXT` | nie | — |
| `notification_available_at_utc` | `NotificationAvailableAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `notification_legacy_visible_from_utc` | `NotificationLegacyVisibleFromUtc : DateTimeOffset? / TEXT` | tak | — |
| `publication_status` | `PublicationStatus : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Private")` |
| `published_at_utc` | `PublishedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `visible_from_utc` | `VisibleFromUtc : DateTimeOffset? / TEXT` | tak | — |
| `visible_to_utc` | `VisibleToUtc : DateTimeOffset? / TEXT` | tak | — |
| `year` | `Year : int / INTEGER` | nie | — |

Indeksy: `HasIndex("PublicationStatus", "VisibleFromUtc", "VisibleToUtc") .HasDatabaseName("ix_avail_group_publication_visibility")`; `HasIndex("Year", "Month", "Name") .IsUnique() .HasDatabaseName("ux_avail_group_year_month_name")`; `HasIndex(new[] { "Year", "Month", "Name" }, "ix_avail_group_year_month_name")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: `HasCheckConstraint("ck_availability_group_month", "month BETWEEN 1 AND 12")`.

Walidacja deklaracji modelu: `Name [Required]`; `Year [Required]`; `Month [Required]`; `PublicationStatus [Required]`.

#### AvailabilityBinds — BindModel

Model: BindModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `Id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `IsActive` | `IsActive : bool / INTEGER` | nie | — |
| `Key` | `Key : string / TEXT` | nie | — |
| `ManagerAccountId` | `ManagerAccountId : int? / INTEGER` | tak | — |
| `Value` | `Value : string / TEXT` | nie | — |

Indeksy: `HasIndex("Key") .IsUnique() .HasDatabaseName("ux_availability_bind_global_key") .HasFilter("\"ManagerAccountId\" IS NULL")`; `HasIndex("ManagerAccountId", "Key") .IsUnique() .HasDatabaseName("ux_availability_bind_manager_key")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ManagerAccount") .WithMany() .HasForeignKey("ManagerAccountId") .OnDelete(DeleteBehavior.Cascade)`.

CHECK: brak własnego CHECK.

### 3.4 Wymiany i korekty

#### manager_shift_correction_setting — ManagerShiftCorrectionSettingModel

Model: ManagerShiftCorrectionSettingModel. PK: `"ManagerAccountId"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `manager_account_id` | `ManagerAccountId : int / INTEGER` | nie | — |
| `highlight_color` | `HighlightColor : string / TEXT` | nie | `.HasMaxLength(7)` |

Indeksy: brak dodatkowych.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ManagerAccount") .WithOne() .HasForeignKey("DataAccessLayer.Models.ManagerShiftCorrectionSettingModel", "ManagerAccountId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### shift_correction_request — ShiftCorrectionRequestModel

Model: ShiftCorrectionRequestModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `day_of_month` | `DayOfMonth : int / INTEGER` | nie | — |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `original_from_time` | `OriginalFromTime : string / TEXT` | nie | `.HasMaxLength(5)` |
| `original_to_time` | `OriginalToTime : string / TEXT` | nie | `.HasMaxLength(5)` |
| `requested_from_time` | `RequestedFromTime : string / TEXT` | nie | `.HasMaxLength(5)` |
| `requested_to_time` | `RequestedToTime : string / TEXT` | nie | `.HasMaxLength(5)` |
| `reviewed_at_utc` | `ReviewedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `reviewed_by_manager_id` | `ReviewedByManagerId : int? / INTEGER` | tak | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `schedule_slot_id` | `ScheduleSlotId : int / INTEGER` | nie | — |
| `status` | `Status : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Pending")` |

Indeksy: `HasIndex("ReviewedByManagerId")`; `HasIndex("ScheduleSlotId", "Status") .IsUnique() .HasDatabaseName("ux_shift_correction_pending_slot") .HasFilter("status = 'Pending'")`; `HasIndex(new[] { "EmployeeId", "Status" }, "ix_shift_correction_employee_status")`; `HasIndex(new[] { "ScheduleId", "Status" }, "ix_shift_correction_schedule_status")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ReviewedByManager") .WithMany() .HasForeignKey("ReviewedByManagerId") .OnDelete(DeleteBehavior.SetNull)`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany("ShiftCorrectionRequests") .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_shift_correction_dom", "day_of_month BETWEEN 1 AND 31")`; `HasCheckConstraint("ck_shift_correction_requested_order", "requested_from_time < requested_to_time")`; `HasCheckConstraint("ck_shift_correction_time_format", "original_from_time LIKE '__:__' AND original_to_time LIKE '__:__' AND requested_from_time LIKE '__:__' AND requested_to_time LIKE '__:__'")`.

#### shift_swap_history — ShiftSwapHistoryModel

Model: ShiftSwapHistoryModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `accepted_at_utc` | `AcceptedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `accepted_by_employee_id` | `AcceptedByEmployeeId : int / INTEGER` | nie | — |
| `accepted_by_employee_name` | `AcceptedByEmployeeName : string / TEXT` | nie | `.HasMaxLength(200)` |
| `after_snapshot_json` | `AfterSnapshotJson : string / TEXT` | nie | — |
| `before_snapshot_json` | `BeforeSnapshotJson : string / TEXT` | nie | — |
| `container_name` | `ContainerName : string / TEXT` | nie | `.HasMaxLength(200)` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `day_of_month` | `DayOfMonth : int / INTEGER` | nie | — |
| `from_employee_id` | `FromEmployeeId : int? / INTEGER` | tak | — |
| `from_employee_name` | `FromEmployeeName : string / TEXT` | nie | `.HasMaxLength(200)` |
| `from_time` | `FromTime : string / TEXT` | nie | `.HasMaxLength(5)` |
| `is_manager_created` | `IsManagerCreated : bool / INTEGER` | nie | — |
| `manual_column_id` | `ManualColumnId : int? / INTEGER` | tak | — |
| `manual_column_name` | `ManualColumnName : string / TEXT` | tak | `.HasMaxLength(200)` |
| `month` | `Month : int / INTEGER` | nie | — |
| `request_kind` | `RequestKind : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Give")` |
| `return_after_snapshot_json` | `ReturnAfterSnapshotJson : string / TEXT` | tak | — |
| `return_before_snapshot_json` | `ReturnBeforeSnapshotJson : string / TEXT` | tak | — |
| `return_day_of_month` | `ReturnDayOfMonth : int? / INTEGER` | tak | — |
| `return_from_time` | `ReturnFromTime : string / TEXT` | tak | `.HasMaxLength(5)` |
| `return_month` | `ReturnMonth : int? / INTEGER` | tak | — |
| `return_schedule_id` | `ReturnScheduleId : int? / INTEGER` | tak | — |
| `return_schedule_name` | `ReturnScheduleName : string / TEXT` | tak | `.HasMaxLength(200)` |
| `return_shop_name` | `ReturnShopName : string / TEXT` | tak | `.HasMaxLength(200)` |
| `return_to_time` | `ReturnToTime : string / TEXT` | tak | `.HasMaxLength(5)` |
| `return_year` | `ReturnYear : int? / INTEGER` | tak | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `schedule_name` | `ScheduleName : string / TEXT` | nie | `.HasMaxLength(200)` |
| `schedule_slot_id` | `ScheduleSlotId : int / INTEGER` | nie | — |
| `shop_name` | `ShopName : string / TEXT` | nie | `.HasMaxLength(200)` |
| `source_shift_swap_request_id` | `SourceShiftSwapRequestId : int / INTEGER` | nie | — |
| `target_employee_id` | `TargetEmployeeId : int? / INTEGER` | tak | — |
| `target_employee_name` | `TargetEmployeeName : string / TEXT` | tak | `.HasMaxLength(200)` |
| `to_time` | `ToTime : string / TEXT` | nie | `.HasMaxLength(5)` |
| `year` | `Year : int / INTEGER` | nie | — |

Indeksy: `HasIndex(new[] { "ScheduleId", "AcceptedAtUtc" }, "ix_shift_swap_history_schedule_accepted")`; `HasIndex(new[] { "SourceShiftSwapRequestId" }, "ux_shift_swap_history_source_request") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany() .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_shift_swap_history_dom", "day_of_month BETWEEN 1 AND 31")`; `HasCheckConstraint("ck_shift_swap_history_month", "month BETWEEN 1 AND 12")`.

Walidacja deklaracji modelu: `ReturnScheduleName [MaxLength(200), Column("return_schedule_name")]`; `ReturnShopName [MaxLength(200), Column("return_shop_name")]`; `ReturnFromTime [MaxLength(5), Column("return_from_time")]`; `ReturnToTime [MaxLength(5), Column("return_to_time")]`; `SourceShiftSwapRequestId [Required]`; `ScheduleId [Required]`; `ScheduleSlotId [Required]`; `ScheduleName [Required]`; `ContainerName [Required]`; `ShopName [Required]`; `Year [Required]`; `Month [Required]`; `DayOfMonth [Required]`; `FromTime [Required]`; `ToTime [Required]`; `FromEmployeeName [Required]`; `TargetEmployeeName [MaxLength(200)]`; `AcceptedByEmployeeId [Required]`; `AcceptedByEmployeeName [Required]`; `CreatedAtUtc [Required]`; `AcceptedAtUtc [Required]`; `IsManagerCreated [Required]`; `ManualColumnName [MaxLength(200)]`; `BeforeSnapshotJson [Required]`; `AfterSnapshotJson [Required]`.

#### shift_swap_request — ShiftSwapRequestModel

Model: ShiftSwapRequestModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `accepted_at_utc` | `AcceptedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `accepted_by_employee_id` | `AcceptedByEmployeeId : int? / INTEGER` | tak | — |
| `archived_views_json` | `ArchivedViewsJson : string / TEXT` | tak | — |
| `cancelled_at_utc` | `CancelledAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `from_employee_id` | `FromEmployeeId : int? / INTEGER` | tak | — |
| `is_manager_created` | `IsManagerCreated : bool / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(false)` |
| `manual_column_id` | `ManualColumnId : int? / INTEGER` | tak | — |
| `offered_from_time` | `OfferedFromTime : string / TEXT` | tak | — |
| `offered_to_time` | `OfferedToTime : string / TEXT` | tak | — |
| `request_kind` | `RequestKind : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Give")` |
| `return_from_time` | `ReturnFromTime : string / TEXT` | tak | `.HasMaxLength(5)` |
| `return_schedule_id` | `ReturnScheduleId : int? / INTEGER` | tak | — |
| `return_schedule_slot_id` | `ReturnScheduleSlotId : int? / INTEGER` | tak | — |
| `return_to_time` | `ReturnToTime : string / TEXT` | tak | `.HasMaxLength(5)` |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `schedule_slot_id` | `ScheduleSlotId : int? / INTEGER` | tak | — |
| `status` | `Status : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Open")` |
| `target_employee_id` | `TargetEmployeeId : int? / INTEGER` | tak | — |
| `visibility` | `Visibility : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue("Public")` |

Indeksy: `HasIndex("AcceptedByEmployeeId")`; `HasIndex("ReturnScheduleId", "Status") .HasDatabaseName("ix_shift_swap_return_schedule_status")`; `HasIndex("ReturnScheduleSlotId", "Status") .IsUnique() .HasDatabaseName("ux_shift_swap_open_return_slot") .HasFilter("status = 'Open' AND request_kind = 'Exchange' AND return_schedule_slot_id IS NOT NULL")`; `HasIndex("ScheduleSlotId", "Status") .IsUnique() .HasDatabaseName("ux_shift_swap_open_slot") .HasFilter("status = 'Open'")`; `HasIndex(new[] { "FromEmployeeId", "Status" }, "ix_shift_swap_from_status")`; `HasIndex(new[] { "ScheduleId", "Status" }, "ix_shift_swap_schedule_status")`; `HasIndex(new[] { "TargetEmployeeId", "Status" }, "ix_shift_swap_target_status")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "AcceptedByEmployee") .WithMany() .HasForeignKey("AcceptedByEmployeeId") .OnDelete(DeleteBehavior.Restrict)`; `HasOne("DataAccessLayer.Models.EmployeeModel", "FromEmployee") .WithMany() .HasForeignKey("FromEmployeeId") .OnDelete(DeleteBehavior.Restrict)`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany() .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.ScheduleSlotModel", "ScheduleSlot") .WithMany() .HasForeignKey("ScheduleSlotId") .OnDelete(DeleteBehavior.Cascade)`; `HasOne("DataAccessLayer.Models.EmployeeModel", "TargetEmployee") .WithMany() .HasForeignKey("TargetEmployeeId") .OnDelete(DeleteBehavior.Restrict)`.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `ReturnFromTime [MaxLength(5), Column("return_from_time")]`; `ReturnToTime [MaxLength(5), Column("return_to_time")]`; `ScheduleId [Required]`; `IsManagerCreated [Required]`; `Visibility [Required]`; `Status [Required]`; `CreatedAtUtc [Required]`.

### 3.5 Komunikacja, powiadomienia, push i ustawienia

#### communication_message — CommunicationMessageModel

Model: CommunicationMessageModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `body` | `Body : string / TEXT` | nie | `.HasMaxLength(4000)` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `created_by_manager_id` | `CreatedByManagerId : int? / INTEGER` | tak | — |
| `created_by_manager_name` | `CreatedByManagerName : string / TEXT` | nie | `.HasMaxLength(160)` |
| `deadline_at_utc` | `DeadlineAtUtc : DateTimeOffset / TEXT` | nie | — |
| `title` | `Title : string / TEXT` | nie | `.HasMaxLength(160)` |
| `visible_from_utc` | `VisibleFromUtc : DateTimeOffset? / TEXT` | tak | — |

Indeksy: `HasIndex("CreatedAtUtc") .HasDatabaseName("ix_communication_message_created")`; `HasIndex("CreatedByManagerId")`; `HasIndex("DeadlineAtUtc") .HasDatabaseName("ix_communication_message_deadline")`; `HasIndex("VisibleFromUtc") .HasDatabaseName("ix_communication_message_visible_from")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "CreatedByManager") .WithMany() .HasForeignKey("CreatedByManagerId") .OnDelete(DeleteBehavior.SetNull)`.

CHECK: `HasCheckConstraint("ck_communication_message_body", "length(trim(body)) > 0")`; `HasCheckConstraint("ck_communication_message_deadline", "deadline_at_utc > created_at_utc")`; `HasCheckConstraint("ck_communication_message_title", "length(trim(title)) > 0")`.

Walidacja deklaracji modelu: `Title [Required]`; `Body [Required]`; `CreatedByManagerName [Required]`.

#### employee_communication_dismissal — EmployeeCommunicationDismissalModel

Model: EmployeeCommunicationDismissalModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `communication_message_id` | `CommunicationMessageId : int / INTEGER` | nie | — |
| `dismissed_at_utc` | `DismissedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("CommunicationMessageId", "EmployeeId") .IsUnique() .HasDatabaseName("ux_employee_comm_dismissal_msg_emp")`; `HasIndex("EmployeeId", "DismissedAtUtc") .HasDatabaseName("ix_employee_comm_dismissal_emp_time")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.CommunicationMessageModel", "CommunicationMessage") .WithMany("Dismissals") .HasForeignKey("CommunicationMessageId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### employee_notification_read — EmployeeNotificationReadModel

Model: EmployeeNotificationReadModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `notification_id` | `NotificationId : string / TEXT` | nie | `.HasMaxLength(256)` |
| `read_at_utc` | `ReadAtUtc : DateTimeOffset / TEXT` | nie | — |

Indeksy: `HasIndex("EmployeeId", "NotificationId") .IsUnique() .HasDatabaseName("ux_emp_notification_read_emp_notification")`; `HasIndex("EmployeeId", "ReadAtUtc") .HasDatabaseName("ix_emp_notification_read_emp_time")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### employee_pinned_swap — EmployeePinnedSwapModel

Model: EmployeePinnedSwapModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `pinned_at_utc` | `PinnedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `shift_swap_id` | `ShiftSwapId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("ShiftSwapId")`; `HasIndex("EmployeeId", "PinnedAtUtc") .HasDatabaseName("ix_employee_pinned_swap_employee_time")`; `HasIndex("EmployeeId", "ShiftSwapId") .IsUnique() .HasDatabaseName("ux_employee_pinned_swap_employee_swap")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.ShiftSwapRequestModel", "ShiftSwap") .WithMany() .HasForeignKey("ShiftSwapId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### employee_push_delivery — EmployeePushDeliveryModel

Model: EmployeePushDeliveryModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : long / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `attempt_count` | `AttemptCount : int / INTEGER` | nie | — |
| `created_at_utc` | `CreatedAtUtc : long / INTEGER` | nie | — |
| `finalized_at_utc` | `FinalizedAtUtc : long? / INTEGER` | tak | — |
| `lease_token` | `LeaseToken : string / TEXT` | tak | `.HasMaxLength(36)` |
| `lease_until_utc` | `LeaseUntilUtc : long? / INTEGER` | tak | — |
| `next_attempt_at_utc` | `NextAttemptAtUtc : long / INTEGER` | nie | — |
| `notification_id` | `NotificationId : string / TEXT` | nie | `.HasMaxLength(256)` |
| `outcome` | `Outcome : string / TEXT` | tak | `.HasMaxLength(32)` |
| `subscription_id` | `SubscriptionId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("FinalizedAtUtc")`; `HasIndex("NextAttemptAtUtc", "LeaseUntilUtc")`; `HasIndex("SubscriptionId", "NotificationId") .IsUnique()`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeePushSubscriptionModel", "Subscription") .WithMany() .HasForeignKey("SubscriptionId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### employee_push_subscription — EmployeePushSubscriptionModel

Model: EmployeePushSubscriptionModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `auth` | `Auth : string / TEXT` | nie | `.HasMaxLength(256)` |
| `created_at_utc` | `CreatedAtUtc : long / INTEGER` | nie | — |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `endpoint` | `Endpoint : string / TEXT` | nie | `.HasMaxLength(2048)` |
| `p256dh` | `P256dh : string / TEXT` | nie | `.HasMaxLength(512)` |
| `updated_at_utc` | `UpdatedAtUtc : long / INTEGER` | nie | — |

Indeksy: `HasIndex("Endpoint") .IsUnique()`; `HasIndex("EmployeeId", "Id")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### employee_schedule_column_preference — EmployeeScheduleColumnPreferenceModel

Model: EmployeeScheduleColumnPreferenceModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `column_order_json` | `ColumnOrderJson : string / TEXT` | nie | — |
| `employee_id` | `EmployeeId : int / INTEGER` | nie | — |
| `schedule_id` | `ScheduleId : int / INTEGER` | nie | — |
| `updated_at_utc` | `UpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |

Indeksy: `HasIndex("ScheduleId")`; `HasIndex("EmployeeId", "ScheduleId") .IsUnique() .HasDatabaseName("ux_emp_schedule_column_pref_emp_schedule")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.EmployeeModel", "Employee") .WithMany() .HasForeignKey("EmployeeId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`; `HasOne("DataAccessLayer.Models.ScheduleModel", "Schedule") .WithMany() .HasForeignKey("ScheduleId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### manager_graph_fill_color_bind — ManagerGraphFillColorBindModel

Model: ManagerGraphFillColorBindModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `fill_color` | `FillColor : string / TEXT` | nie | `.HasMaxLength(7)` |
| `key` | `Key : string / TEXT` | nie | `.HasMaxLength(64)` |
| `manager_account_id` | `ManagerAccountId : int / INTEGER` | nie | — |

Indeksy: `HasIndex("ManagerAccountId", "FillColor") .IsUnique() .HasDatabaseName("ux_manager_graph_fill_bind_color")`; `HasIndex("ManagerAccountId", "Key") .IsUnique() .HasDatabaseName("ux_manager_graph_fill_bind_key")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ManagerAccount") .WithMany() .HasForeignKey("ManagerAccountId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### manager_graph_text_color_bind — ManagerGraphTextColorBindModel

Model: ManagerGraphTextColorBindModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `key` | `Key : string / TEXT` | nie | `.HasMaxLength(64)` |
| `manager_account_id` | `ManagerAccountId : int / INTEGER` | nie | — |
| `text_color` | `TextColor : string / TEXT` | nie | `.HasMaxLength(7)` |

Indeksy: `HasIndex("ManagerAccountId", "Key") .IsUnique() .HasDatabaseName("ux_manager_graph_text_bind_key")`; `HasIndex("ManagerAccountId", "TextColor") .IsUnique() .HasDatabaseName("ux_manager_graph_text_bind_color")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ManagerAccount") .WithMany() .HasForeignKey("ManagerAccountId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### manager_note — ManagerNoteModel

Model: ManagerNoteModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `color` | `Color : string / TEXT` | nie | `.HasMaxLength(16)` |
| `content` | `Content : string / TEXT` | nie | `.HasMaxLength(20000)` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `manager_account_id` | `ManagerAccountId : int / INTEGER` | nie | — |
| `title` | `Title : string / TEXT` | nie | `.HasMaxLength(160)` |
| `updated_at_utc` | `UpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |

Indeksy: `HasIndex("ManagerAccountId", "UpdatedAtUtc") .HasDatabaseName("ix_manager_note_manager_updated")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ManagerAccount") .WithMany() .HasForeignKey("ManagerAccountId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Title [MaxLength(160)]`; `Color [MaxLength(16)]`.

#### manager_notepad_state — ManagerNotepadStateModel

Model: ManagerNotepadStateModel. PK: `"ManagerAccountId"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `manager_account_id` | `ManagerAccountId : int / INTEGER` | nie | — |
| `height` | `Height : int / INTEGER` | nie | — |
| `is_expanded` | `IsExpanded : bool / INTEGER` | nie | — |
| `is_pinned` | `IsPinned : bool / INTEGER` | nie | — |
| `updated_at_utc` | `UpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |

Indeksy: brak dodatkowych.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.ManagerAccountModel", "ManagerAccount") .WithOne() .HasForeignKey("DataAccessLayer.Models.ManagerNotepadStateModel", "ManagerAccountId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

#### system_news_message — SystemNewsMessageModel

Model: SystemNewsMessageModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `audience` | `Audience : string / TEXT` | nie | `.HasMaxLength(16)` |
| `body` | `Body : string / TEXT` | nie | `.HasMaxLength(10000)` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `image_url` | `ImageUrl : string / TEXT` | tak | `.HasMaxLength(2800000)` |
| `title` | `Title : string / TEXT` | nie | `.HasMaxLength(160)` |
| `updated_at_utc` | `UpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `video_url` | `VideoUrl : string / TEXT` | tak | `.HasMaxLength(500)` |

Indeksy: `HasIndex("CreatedAtUtc") .HasDatabaseName("ix_system_news_created")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Title [MaxLength(160), Column("title")]`; `Audience [MaxLength(16), Column("audience")]`; `VideoUrl [MaxLength(500), Column("video_url")]`.

#### system_news_read — SystemNewsReadModel

Model: SystemNewsReadModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `account_id` | `AccountId : int / INTEGER` | nie | — |
| `account_role` | `AccountRole : string / TEXT` | nie | `.HasMaxLength(16)` |
| `message_id` | `MessageId : int / INTEGER` | nie | — |
| `read_at_utc` | `ReadAtUtc : DateTimeOffset / TEXT` | nie | — |

Indeksy: `HasIndex("AccountRole", "AccountId", "ReadAtUtc") .HasDatabaseName("ix_system_news_read_subject_time")`; `HasIndex("MessageId", "AccountRole", "AccountId") .IsUnique() .HasDatabaseName("ux_system_news_read_subject")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.SystemNewsMessageModel", "Message") .WithMany("Reads") .HasForeignKey("MessageId") .OnDelete(DeleteBehavior.Cascade) .IsRequired()`.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `AccountRole [MaxLength(16), Column("account_role")]`.

### 3.6 Regulaminy i logi

#### regulation_acceptance — RegulationAcceptanceModel

Model: RegulationAcceptanceModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `accepted_at_utc` | `AcceptedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `account_id` | `AccountId : int / INTEGER` | nie | — |
| `account_role` | `AccountRole : string / TEXT` | nie | `.HasMaxLength(16)` |
| `display_name_snapshot` | `DisplayNameSnapshot : string / TEXT` | nie | `.HasMaxLength(160)` |
| `regulation_document_id` | `RegulationDocumentId : int / INTEGER` | nie | — |
| `username_snapshot` | `UsernameSnapshot : string / TEXT` | nie | `.HasMaxLength(100)` |

Indeksy: `HasIndex("AccountRole", "AccountId", "AcceptedAtUtc") .HasDatabaseName("ix_regulation_acceptance_subject_time")`; `HasIndex("RegulationDocumentId", "AccountRole", "AccountId") .IsUnique() .HasDatabaseName("ux_regulation_acceptance_subject")`.

FK/relacje (encja zależna → nadrzędna): `HasOne("DataAccessLayer.Models.RegulationDocumentModel", "RegulationDocument") .WithMany("Acceptances") .HasForeignKey("RegulationDocumentId") .OnDelete(DeleteBehavior.Restrict) .IsRequired()`.

CHECK: `HasCheckConstraint("ck_regulation_acceptance_role", "account_role IN ('manager', 'employee')")`.

Walidacja deklaracji modelu: `AccountRole [Required]`; `UsernameSnapshot [Required]`; `DisplayNameSnapshot [Required]`.

#### regulation_document — RegulationDocumentModel

Model: RegulationDocumentModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `created_at_utc` | `CreatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `created_by_manager_id` | `CreatedByManagerId : int? / INTEGER` | tak | — |
| `created_by_manager_name` | `CreatedByManagerName : string / TEXT` | nie | `.HasMaxLength(160)` |
| `is_published` | `IsPublished : bool / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(false)` |
| `message` | `Message : string / TEXT` | nie | `.HasMaxLength(4000)` |
| `pdf_content` | `PdfContent : byte[] / BLOB` | nie | — |
| `pdf_file_name` | `PdfFileName : string / TEXT` | nie | `.HasMaxLength(255)` |
| `pdf_sha256` | `PdfSha256 : string / TEXT` | nie | `.HasMaxLength(64)` |
| `published_at_utc` | `PublishedAtUtc : DateTimeOffset? / TEXT` | tak | — |
| `title` | `Title : string / TEXT` | nie | `.HasMaxLength(160)` |
| `updated_at_utc` | `UpdatedAtUtc : DateTimeOffset / TEXT` | nie | — |
| `version` | `Version : string / TEXT` | nie | `.HasMaxLength(80)`; `.UseCollation("NOCASE")` |

Indeksy: `HasIndex("Version") .IsUnique() .HasDatabaseName("ux_regulation_document_version")`; `HasIndex("IsPublished", "PublishedAtUtc") .HasDatabaseName("ix_regulation_document_published")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `Title [Required]`; `Version [Required]`; `Message [Required]`; `PdfFileName [Required]`; `PdfContent [Required]`; `PdfSha256 [Required]`; `CreatedByManagerName [Required]`.

#### workflow_log_entry — WorkflowLogEntryModel

Model: WorkflowLogEntryModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `action` | `Action : string / TEXT` | nie | `.HasMaxLength(512)` |
| `actor_employee_id` | `ActorEmployeeId : int? / INTEGER` | tak | — |
| `actor_name` | `ActorName : string / TEXT` | nie | `.HasMaxLength(160)` |
| `actor_role` | `ActorRole : string / TEXT` | nie | `.HasMaxLength(32)` |
| `occurred_at_utc` | `OccurredAtUtc : DateTimeOffset / TEXT` | nie | — |

Indeksy: `HasIndex("OccurredAtUtc") .HasDatabaseName("ix_workflow_log_occurred_at")`; `HasIndex("ActorRole", "OccurredAtUtc") .HasDatabaseName("ix_workflow_log_role_time")`; `HasIndex(new[] { "OccurredAtUtc" }, "ix_workflow_log_occurred_at")`; `HasIndex(new[] { "ActorRole", "OccurredAtUtc" }, "ix_workflow_log_role_time")`.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: brak własnego CHECK.

Walidacja deklaracji modelu: `OccurredAtUtc [Required]`; `ActorRole [Required]`; `ActorName [Required]`; `Action [Required]`.

#### workflow_log_settings — WorkflowLogSettingsModel

Model: WorkflowLogSettingsModel. PK: `"Id"`.

| Kolumna | Właściwość modelu / typ magazynu | NULL | Ograniczenia pola |
| --- | --- | --- | --- |
| `id` | `Id : int / INTEGER` | nie | `.ValueGeneratedOnAdd()` |
| `audience` | `Audience : string / TEXT` | nie | `.ValueGeneratedOnAdd()`; `.HasMaxLength(16)`; `.HasDefaultValue("all")` |
| `is_enabled` | `IsEnabled : bool / INTEGER` | nie | `.ValueGeneratedOnAdd()`; `.HasDefaultValue(true)` |

Indeksy: brak dodatkowych.

FK/relacje (encja zależna → nadrzędna): brak FK na tej encji.

CHECK: `HasCheckConstraint("ck_workflow_log_settings_audience", "audience IN ('all', 'managers', 'employees')")`; `HasCheckConstraint("ck_workflow_log_settings_singleton", "id = 1")`.

Walidacja deklaracji modelu: `Audience [Required]`.

### 3.7 Reguly przekrojowe i procedury SQL

`Cascade` usuwa zależne rekordy wraz z nadrzędnym; `Restrict` blokuje usunięcie przy zależnościach; `SetNull` pozostawia rekord z pustym FK. Brak jawnego `OnDelete` w zapisie relacji nie jest obietnicą kaskady — obowiązuje model EF (dla opcjonalnej relacji domyślne ClientSetNull). `WithMany` oznacza relację wiele-do-jednego, `WithOne` jeden-do-jednego. `HasIndex(...).IsUnique()` i PK wymuszają unikalność, także przy zapisie poza UI. Wiersze podają nazwy właściwości w indeksach/FK; fizyczne nazwy kolumn są w tabelach pól.

Pracownik ma `IsDeleted`; usuwanie może archiwizować osobę potrzebną historii zamiast kasować wszystkie dane. Nie ma globalnego filtra EF usuwającego historyczne osoby z każdego odczytu. Konta/poświadczenia są oddzielone od danych pracownika. Użytkownik nie podaje skrótów haseł, wersji sesji ani technicznych timestampów w zwykłym formularzu.

Presety są wybieralne między kontenerami; `SchedulePresetModel.ContainerId` jest nullable, relacja nie wymusza już własności pojedynczego kontenera. Migracja MakeSchedulePresetsApplicationWide jest obowiązująca. Nie oznacza to globalnej unikalności nazw: sprawdzanie nazwy w ContainerService nadal odnosi się do bieżącego kontenera. Wersje grafiku przechowują snapshot i stan bieżącej gałęzi; zmiana kodu bez zgodnej bazy nie jest rollbackiem danych.

Walidacja ma trzy poziomy: adnotacje modeli/requestów, Fluent/indeksy/CHECK w EF oraz usługi domenowe (zakres dat i miesiąca, przedziały czasu, przypisania, publikacja, konflikt zmian/duplikatów, aktualność wniosku). MaxLength w modelu EF/SQLite nie zastępuje wszystkich reguł usług; SQLite nie zachowuje się jak serwer wymuszający każdy limit varchar. Zapis AppDbContext nadaje timestamp publikacji grafiku i tożsamość/czas dostępności powiadomienia o dyspozycyjności. Powiadomienia push mają unikalne dostarczenia i lease/retry, a nie osobny magazyn danych użytkownika w telefonie.

Brak własnych procedur/wyzwalaczy potwierdzonych w źródłach migracji. Operacje generatora, walidacji i repozytoriów są kodem C#/EF, nie procedurami SQL. SQLite jest bazą plikową; źródła nie ustanawiają shardingu, rozproszonej bazy ani klastra. Dostęp/backup i wydajność wymagają weryfikacji dla rzeczywistego obciążenia.

## 4. Biblioteki wewnetrzne

### BusinessLogicLayer

Biblioteka .NET 10 odwołująca się do DAL, z własnymi kontraktami w `BusinessLogicLayer/Contracts`. IContainerService orkiestruje kontenery, grafiki i ich zasoby; usługi Schedule/Slot/Employee obsługują szczegóły planowania; AvailabilityGroup/Transfer — dyspozycyjność i transfery; Employee/Shop i fasady — dane organizacyjne; Account/Auth/Profile — konta, hashowanie, odzyskiwanie i sesje; Communication/Regulation — wiadomości i osobiste potwierdzenia; Export/DataBuilder/Template — SQL/XLSX; AdminDb/SqliteAdminFacade — ograniczone narzędzia bazy. Presence obsługuje stan aktywności w procesie.

Rejestracja `AddBusinessLogicLayer()` wymaga istniejących repozytoriów i konfiguracji usług integracyjnych. `AddBusinessLogicStack(connectionString, databasePath)` rejestruje workspace SQLite, DAL, BLL i opcje szablonów. Usługi/repozytoria zwykle scoped, obecność/hasher/lokalizator szablonów singleton, generator transient. W WebApi dodatkowo rejestrowane są JWT, SMTP, realtime, blokady, background cleanup/backup/push. Nie twórz oddzielnego DbContext z inną ścieżką niż workspace.

Walidacja domeny zgłasza `BusinessLogicLayer.Common.ValidationException` z mapą pól; operacje braku zasobu i bezpiecznego usuwania używają wyjątków lub DeleteOperationResult zgodnie z kontraktem konkretnej metody. API tłumaczy je mechanizmem §2. Komunikaty UI nie zastępują walidacji usług i bazy.

Kontrakt generatora IScheduleGenerator:

```csharp
Task<IList<ScheduleSlotModel>> GenerateAsync(
    ScheduleModel schedule,
    IEnumerable<AvailabilityGroupModel> availabilities,
    IEnumerable<ScheduleEmployeeModel> employees,
    IProgress<int>? progress = null,
    CancellationToken ct = default);
```

Typy wejścia/wyjścia powyżej pochodzą z `BusinessLogicLayer.Contracts.Models`, nie bezpośrednio z modeli EF o podobnych nazwach. ScheduleGenerator normalizuje dane, buduje macierze dostępności, wybiera przydziały i emituje sloty. Uwzględnia okna ANY/NONE/INT, obsadę jednej/dwóch zmian, limity godzin, dni kolejnych/pełnych, cele minimalnych godzin i równoważenie obsady. Deduplikuje pracowników; przy duplikatach zachowuje większe minimum godzin. Null wejścia jest błędem; nieprawidłowy rok/miesiąc, brak obsady, szablonów zmian lub pracowników mogą dać pustą listę. Ma anulowanie i raportowanie postępu. Wynik wymaga kontroli konfliktów; algorytm nie daje gwarancji optymalnego/wykonalnego rozwiązania każdego zestawu reguł. Generowanie propozycji i jej utrwalenie są oddzielnymi operacjami warstwy wyższej.

### DataAccessLayer

Biblioteka .NET 10: AppDbContext, modele, migracje, repozytoria i administracja SQLite. `AddDataAccess()` rejestruje scoped repozytoria i kontekst z `ISqliteDatabaseWorkspace.ConnectionString`. Repozytoria utrwalają grafy/relacje, bez wiedzy o HTTP; kontekst aktualizuje timestampy publikacji. Ograniczenia SQL i błędy konkurencji mogą zgłosić DbUpdateException/DbUpdateConcurrencyException; należy zachować transakcje i obsługę błędów warstwy wyższej. SQL administracyjny nie jest zwykłym API domenowym. Repozytoria i interfejsy określają operacje trwałości; pełny model fizyczny w §3.

### GF3.WebApi i klient

WebApi to adapter/host: mapuje requesty na kontrakty BLL i wyniki na DTO HTTP, egzekwuje JWT/role/polityki, udostępnia pliki i SignalR, uruchamia migracje/usługi tła oraz hostuje SPA. Nie jest biblioteką zewnętrzną. Frontend ma lokalne implementacje importów `react-router-dom`, `@tanstack/react-query` i devtools przez aliasy Vite; nie są to deklarowane pakiety npm. i18n i cache zapytań są kodem projektu.

### Publiczne kontrakty uslug BLL

Poniższy katalog wskazuje wejścia/wyjścia i dziedziczenie każdego publicznego interfejsu z Services/Abstractions. Typy wejściowe i wynikowe podano w sygnaturach; zasady integracji i DI opisano powyżej. Metody bazowe obowiązują również w interfejsach dziedziczących. Sygnatury są kontraktem C#, nie dodatkowymi endpointami HTTP.

#### IAdminDbService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<AdminDbMetadataDto> GetMetadataAsync(CancellationToken ct = default)` |
| `Task<string> GetDbHashAsync(CancellationToken ct = default)` |
| `Task<AdminDbQueryResultDto> ExecuteQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)` |
| `Task<int> ExecuteNonQueryAsync(string sql, int maxSqlLength, CancellationToken ct = default)` |
| `Task<AdminDbImportResultDto> ImportSqlAsync(byte[] fileBytes, int maxImportBytes, CancellationToken ct = default)` |
| `Task<AdminDbFileEntryDto> CreateManualCopyAsync(CancellationToken ct = default)` |
| `Task<AdminDbFileEntryDto> SelectDatabaseAsync(string databasePath, CancellationToken ct = default)` |

#### IAuthService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<AuthenticatedSessionDto?> AuthenticateAsync(string username, string password, CancellationToken ct = default)` |
| `Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)` |
| `Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)` |

#### `IAvailabilityGroupService : IBaseService<AvailabilityGroupModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task SaveGroupAsync( AvailabilityGroupModel group, IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload, CancellationToken ct = default)` |
| `Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)> LoadFullAsync(int groupId, CancellationToken ct = default)` |
| `Task<List<EmployeeAvailabilityModel>> GetPublishedForEmployeeAsync(int employeeId, DateTimeOffset nowUtc, CancellationToken ct = default)` |
| `Task<EmployeeAvailabilityModel> GetPublishedForEmployeeByIdAsync(int employeeId, int groupId, DateTimeOffset nowUtc, CancellationToken ct = default)` |
| `Task<EmployeeAvailabilityModel> SaveEmployeeAvailabilityAsync( int employeeId, int groupId, IList<AvailabilityGroupDayModel> days, DateTimeOffset nowUtc, CancellationToken ct = default)` |
| `Task<List<AvailabilityGroupMemberModel>> GetMembersAsync(int groupId, CancellationToken ct = default)` |
| `Task<AvailabilityGroupMemberModel> CreateMemberAsync(int groupId, AvailabilityGroupMemberModel model, CancellationToken ct = default)` |
| `Task UpdateMemberAsync(int groupId, int memberId, AvailabilityGroupMemberModel model, CancellationToken ct = default)` |
| `Task DeleteMemberAsync(int groupId, int memberId, CancellationToken ct = default)` |
| `Task<List<AvailabilityGroupDayModel>> GetSlotsAsync(int groupId, CancellationToken ct = default)` |
| `Task<AvailabilityGroupDayModel> CreateSlotAsync(int groupId, AvailabilityGroupDayModel model, CancellationToken ct = default)` |
| `Task UpdateSlotAsync(int groupId, int slotId, AvailabilityGroupDayModel model, CancellationToken ct = default)` |
| `Task DeleteSlotAsync(int groupId, int slotId, CancellationToken ct = default)` |

#### IAvailabilityGroupTransferService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<List<AvailabilityTransferSourceModel>> GetSourcesAsync( int targetGroupId, int targetMemberId, CancellationToken ct = default)` |
| `Task<List<AvailabilityTransferSourceModel>> GetPreviewSourcesAsync( IReadOnlyCollection<int> employeeIds, int year, int month, int? targetGroupId, CancellationToken ct = default)` |
| `Task<List<AvailabilityTransferHintModel>> GetHintsAsync( int sourceGroupId, CancellationToken ct = default)` |
| `Task<AvailabilityTransferResultModel> TransferDaysAsync( int targetGroupId, int targetMemberId, int sourceGroupId, IReadOnlyCollection<int> dayOfMonths, CancellationToken ct = default)` |

#### `IBaseService<TEntity> where TEntity : class`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<TEntity?> GetAsync(int id, CancellationToken ct = default)` |
| `Task<List<TEntity>> GetAllAsync(CancellationToken ct = default)` |
| `Task<TEntity> CreateAsync(TEntity entity, CancellationToken ct = default)` |
| `Task UpdateAsync(TEntity entity, CancellationToken ct = default)` |
| `Task DeleteAsync(int id, CancellationToken ct = default)` |

#### `IBindService : IBaseService<BindModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<BindModel?> GetAsync(int id, int managerAccountId, CancellationToken ct = default)` |
| `Task<List<BindModel>> GetAllAsync(int managerAccountId, CancellationToken ct = default)` |
| `Task<BindModel> CreateAsync(BindModel entity, int managerAccountId, CancellationToken ct = default)` |
| `Task UpdateAsync(BindModel entity, int managerAccountId, CancellationToken ct = default)` |
| `Task DeleteAsync(int id, int managerAccountId, CancellationToken ct = default)` |
| `Task<List<BindModel>> GetActiveAsync(CancellationToken ct = default)` |
| `Task<List<BindModel>> GetActiveAsync(int managerAccountId, CancellationToken ct = default)` |
| `Task<BindModel?> GetByKeyAsync(string key, CancellationToken ct = default)` |
| `Task<BindModel> UpsertByKeyAsync(BindModel model, CancellationToken ct = default)` |

#### ICommunicationService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<IReadOnlyList<CommunicationMessageDto>> ListForManagerAsync(CancellationToken ct = default)` |
| `Task<CommunicationMessageDto> CreateAsync( CreateCommunicationMessageRequest request, int? managerId, string? managerDisplayName, CancellationToken ct = default)` |
| `Task<CommunicationMessageDto> UpdateAsync( int communicationId, UpdateCommunicationMessageRequest request, CancellationToken ct = default)` |
| `Task DeleteAsync(int communicationId, CancellationToken ct = default)` |
| `Task<IReadOnlyList<CommunicationMessageDto>> GetPendingForEmployeeAsync( int employeeId, CancellationToken ct = default)` |
| `Task DismissForEmployeeAsync(int employeeId, int communicationId, CancellationToken ct = default)` |

#### `IContainerService : IBaseService<ContainerModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<List<ContainerModel>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)` |
| `Task<List<ScheduleModel>?> GetGraphsAsync(int containerId, CancellationToken ct = default)` |
| `Task<ScheduleModel?> GetGraphByIdAsync(int containerId, int graphId, CancellationToken ct = default)` |
| `Task<List<ScheduleModel>> GetPublishedGraphsForEmployeeAsync(int employeeId, CancellationToken ct = default)` |
| `Task<ScheduleModel> CreateGraphAsync(int containerId, ScheduleModel model, CancellationToken ct = default)` |
| `Task UpdateGraphAsync(int containerId, int graphId, ScheduleModel model, CancellationToken ct = default)` |
| `Task<int> UpdateGraphPublicationAsync( int containerId, SchedulePublicationStatus publicationStatus, bool? allowSwap, CancellationToken ct = default)` |
| `Task DeleteGraphAsync(int containerId, int graphId, CancellationToken ct = default)` |
| `Task<List<SchedulePresetModel>?> GetSchedulePresetsAsync(int containerId, CancellationToken ct = default)` |
| `Task<SchedulePresetModel> CreateSchedulePresetAsync(int containerId, SchedulePresetModel model, CancellationToken ct = default)` |
| `Task<GenerateGraphResult> GenerateGraphAsync( int containerId, int graphId, bool overwrite, bool dryRun, IProgress<int>? progress, CancellationToken ct = default)` |
| `Task<GenerateGraphResult> GenerateGraphPreviewAsync( int containerId, ScheduleModel model, IEnumerable<ScheduleEmployeeModel> employees, IProgress<int>? progress, CancellationToken ct = default)` |
| `Task<List<ScheduleSlotModel>?> GetGraphSlotsAsync(int containerId, int graphId, CancellationToken ct = default)` |
| `Task ReplaceGraphSlotsAsync(int containerId, int graphId, IEnumerable<ScheduleSlotModel> slots, CancellationToken ct = default)` |
| `Task<ScheduleSlotModel> CreateGraphSlotAsync(int containerId, int graphId, ScheduleSlotModel model, CancellationToken ct = default)` |
| `Task UpdateGraphSlotAsync(int containerId, int graphId, int slotId, ScheduleSlotModel model, CancellationToken ct = default)` |
| `Task DeleteGraphSlotAsync(int containerId, int graphId, int slotId, CancellationToken ct = default)` |
| `Task<List<ScheduleEmployeeModel>?> GetGraphEmployeesAsync(int containerId, int graphId, CancellationToken ct = default)` |
| `Task<ScheduleEmployeeModel> AddGraphEmployeeAsync(int containerId, int graphId, ScheduleEmployeeModel model, CancellationToken ct = default)` |
| `Task UpdateGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, ScheduleEmployeeModel model, CancellationToken ct = default)` |
| `Task RemoveGraphEmployeeAsync(int containerId, int graphId, int graphEmployeeId, CancellationToken ct = default)` |
| `Task<List<ScheduleCellStyleModel>?> GetGraphCellStylesAsync(int containerId, int graphId, CancellationToken ct = default)` |
| `Task<ScheduleCellStyleModel> UpsertGraphCellStyleAsync(int containerId, int graphId, ScheduleCellStyleModel model, CancellationToken ct = default)` |
| `Task DeleteGraphCellStyleAsync(int containerId, int graphId, int styleId, CancellationToken ct = default)` |

#### IEmailSender

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task SendAsync( string toEmail, string? toName, string subject, string textBody, CancellationToken ct = default)` |

#### IEmployeeAccountService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<EmployeeAccountModel?> GetByEmployeeIdAsync(int employeeId, CancellationToken ct = default)` |
| `Task<IReadOnlyDictionary<int, EmployeeAccountModel>> GetByEmployeeIdsAsync(IEnumerable<int> employeeIds, CancellationToken ct = default)` |
| `Task<EmployeeAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)` |
| `Task<EmployeeAccountModel?> UpsertForEmployeeAsync(int employeeId, string? username, string? password, CancellationToken ct = default)` |
| `Task MarkLoginSucceededAsync(int employeeId, CancellationToken ct = default)` |
| `Task TouchLastSeenAsync(int employeeId, CancellationToken ct = default)` |
| `Task<bool> RevokeSessionsAsync(int employeeId, CancellationToken ct = default) => Task.FromResult(false)` |
| `Task<EmployeePasswordResetChallengeDto> CreatePasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)` |
| `Task CompletePasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)` |
| `Task ClearPasswordResetChallengeAsync(int employeeId, CancellationToken ct = default)` |

#### IEmployeeFacade

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<IReadOnlyList<EmployeeDto>> GetAllAsync(CancellationToken ct = default)` |
| `Task<IReadOnlyList<EmployeeDto>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task<EmployeeDto?> GetAsync(int id, CancellationToken ct = default)` |
| `Task<EmployeeDto> CreateAsync(SaveEmployeeRequest request, CancellationToken ct = default)` |
| `Task UpdateAsync(SaveEmployeeRequest request, CancellationToken ct = default)` |
| `Task DeleteAsync(int id, CancellationToken ct = default)` |
| `Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)` |

#### IEmployeePresenceService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `bool IsEmployeeOnline(int employeeId)` |
| `IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> employeeIds)` |
| `EmployeePresenceChange ConnectEmployee(int employeeId, string connectionId, DateTimeOffset connectedAtUtc)` |
| `EmployeePresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc)` |

#### IEmployeeProfileService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<EmployeeProfileDto> GetAsync(int employeeId, CancellationToken ct = default)` |
| `Task<EmployeeProfileDto> UpdateContactAsync(int employeeId, string? recoveryEmail, string? phone, CancellationToken ct = default)` |
| `Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(int employeeId, CancellationToken ct = default)` |
| `Task ConfirmPasswordResetAsync(int employeeId, string code, string newPassword, CancellationToken ct = default)` |

#### `IEmployeeService : IBaseService<EmployeeModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<List<EmployeeModel>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task<EmployeeModel> UpdateContactAsync(int employeeId, string? email, string? phone, CancellationToken ct = default)` |
| `Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)` |

#### IExcelTemplateLocator

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `string GetScheduleTemplatePath()` |
| `string GetContainerTemplatePath()` |

#### IGraphExportService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<byte[]> ExportGraphSqlAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)` |
| `Task<byte[]> ExportGraphExcelCsvAsync(int containerId, int graphId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)` |
| `Task<byte[]> ExportContainerSqlAsync(int containerId, bool includeEmployees, bool includeStyles, CancellationToken ct = default)` |

#### IGraphTemplateExportService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync( int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)` |
| `Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync( int containerId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)` |

#### IManagerAccountService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<ManagerAccountModel?> AuthenticateAsync(string username, string password, CancellationToken ct = default)` |
| `Task<ManagerAccountModel?> GetByUsernameAsync(string username, CancellationToken ct = default)` |
| `Task<ManagerProfileDto> GetProfileAsync(int? managerId, string? userName, CancellationToken ct = default)` |
| `Task<IReadOnlyList<ManagerProfileDto>> ListProfilesAsync(CancellationToken ct = default)` |
| `Task<ManagerProfileDto> CreateAsync(CreateManagerAccountRequest request, CancellationToken ct = default)` |
| `Task<ManagerProfileDto> UpdateProfileAsync(int? managerId, string? userName, UpdateManagerProfileRequest request, CancellationToken ct = default)` |
| `Task<ManagerProfileDto> DeleteAsync(int managerId, int? currentManagerId, string? currentUserName, CancellationToken ct = default)` |
| `Task MarkLoginSucceededAsync(int managerId, CancellationToken ct = default)` |
| `Task<PasswordResetDispatchResult> SendPasswordResetCodeAsync(string username, CancellationToken ct = default)` |
| `Task ConfirmPasswordResetAsync(string username, string code, string newPassword, CancellationToken ct = default)` |

#### IManagerPresenceService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `bool IsManagerOnline(int managerId)` |
| `IReadOnlyDictionary<int, bool> GetOnlineStates(IEnumerable<int> managerIds)` |
| `ManagerPresenceChange ConnectManager(int managerId, string connectionId, DateTimeOffset connectedAtUtc)` |
| `ManagerPresenceChange? DisconnectConnection(string connectionId, DateTimeOffset disconnectedAtUtc)` |

#### IRegulationService

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<IReadOnlyList<RegulationDocumentDto>> ListDocumentsAsync(CancellationToken ct = default)` |
| `Task<IReadOnlyList<RegulationDocumentDto>> ListPendingAsync(RegulationSubject subject, CancellationToken ct = default)` |
| `Task<IReadOnlyList<RegulationAcceptanceDto>> ListAcceptancesAsync(CancellationToken ct = default)` |
| `Task<IReadOnlyList<RegulationAcceptanceDto>> ListAcceptancesAsync(string role, int accountId, CancellationToken ct = default)` |
| `Task<RegulationDocumentDto> CreateAsync(SaveRegulationDocumentRequest request, int? managerId, string managerName, CancellationToken ct = default)` |
| `Task<RegulationDocumentDto> UpdateAsync(int documentId, SaveRegulationDocumentRequest request, CancellationToken ct = default)` |
| `Task<RegulationDocumentDto> PublishAsync(int documentId, CancellationToken ct = default)` |
| `Task DeleteAsync(int documentId, CancellationToken ct = default)` |
| `Task<RegulationAcceptanceDto> AcceptAsync(int documentId, RegulationSubject subject, CancellationToken ct = default)` |
| `Task<RegulationPdfDto> GetPdfAsync(int documentId, bool requirePublished, CancellationToken ct = default)` |

#### `IScheduleEmployeeService : IBaseService<ScheduleEmployeeModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |

#### IScheduleExcelContextBuilder

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `ScheduleExcelContext BuildScheduleContext( ScheduleModel graph, ShopModel? shop, IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots)` |
| `ContainerExcelContext BuildContainerContext(ContainerModel container, IReadOnlyList<GraphExcelContext> graphs)` |

#### IScheduleExportDataBuilder

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `ScheduleSqlExportData BuildSqlData( ScheduleModel schedule, IReadOnlyList<ScheduleEmployeeModel> employees, IReadOnlyList<ScheduleSlotModel> slots, IReadOnlyList<ScheduleCellStyleModel> cellStyles, AvailabilityGroupModel? availabilityGroup, IReadOnlyList<AvailabilityGroupMemberModel>? availabilityMembers, IReadOnlyList<AvailabilityGroupDayModel>? availabilityDays)` |

#### `IScheduleService : IBaseService<ScheduleModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<List<ScheduleModel>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task<List<ScheduleModel>> GetByContainerAsync(int containerId, string? value = null, CancellationToken ct = default)` |
| `Task SaveWithDetailsAsync( ScheduleModel schedule, IEnumerable<ScheduleEmployeeModel> employees, IEnumerable<ScheduleSlotModel> slots, IEnumerable<ScheduleCellStyleModel> cellStyles, CancellationToken ct = default)` |
| `Task<ScheduleModel?> GetDetailedAsync(int id, CancellationToken ct = default)` |

#### `IScheduleSlotService : IBaseService<ScheduleSlotModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |

#### IShopFacade

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<IReadOnlyList<ShopDto>> GetAllAsync(CancellationToken ct = default)` |
| `Task<IReadOnlyList<ShopDto>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task<ShopDto?> GetAsync(int id, CancellationToken ct = default)` |
| `Task<ShopDto> CreateAsync(SaveShopRequest request, CancellationToken ct = default)` |
| `Task UpdateAsync(SaveShopRequest request, CancellationToken ct = default)` |
| `Task DeleteAsync(int id, CancellationToken ct = default)` |
| `Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)` |

#### `IShopService : IBaseService<ShopModel>`

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `Task<List<ShopModel>> GetByValueAsync(string value, CancellationToken ct = default)` |
| `Task<DeleteOperationResult> TryDeleteAsync(int id, CancellationToken ct = default)` |

#### ISqliteAdminFacade

| Operacja — typ wyniku, wejścia i wartości domyślne |
| --- |
| `string DatabasePath { get; }` |
| `Task<SqlExecutionResultDto> ExecuteSqlAsync(string sql, CancellationToken ct)` |
| `Task ImportSqlScriptAsync(string sqlScript, CancellationToken ct)` |
| `Task<DatabaseInfoDto> GetDatabaseInfoAsync(CancellationToken ct)` |
| `Task<string> ComputeFileHashAsync(string filePath, CancellationToken ct)` |

## 5. Oprogramowanie zewnetrzne

Wersje/range to deklaracje manifestów przypiętego commitu, nie audyt wersji wszystkich zależności przechodnich. Licencje komercyjne/subskrypcje, ceny, limity usług, operator i hosting: Niepotwierdzone w repozytorium; do uzgodnienia przed wdrożeniem.

| Pakiet/usługa | Deklarowana wersja/range | Cel i wymaganie / ograniczenia |
| --- | --- | --- |
| `WebPush` | `1.0.13` | push opcjonalny; wymaga konfiguracji VAPID i obsługi przeglądarki |
| `Microsoft.AspNetCore.OpenApi` | `10.0.9` | Swagger/OpenAPI; UI w Development |
| `Microsoft.AspNetCore.SpaProxy` | `10.0.9` | SPA proxy w Development |
| `Microsoft.AspNetCore.SpaServices.Extensions` | `10.0.9` | SPA proxy w Development |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.9` | SQLite / EF; narzędzia Design/Tools tylko build/dev |
| `Swashbuckle.AspNetCore` | `10.2.3` | Swagger/OpenAPI; UI w Development |
| `ClosedXML` | `0.105.0` | eksport XLSX |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.0` | wiązanie opcji konfiguracji |
| `Microsoft.EntityFrameworkCore` | `10.0.9` | SQLite / EF; narzędzia Design/Tools tylko build/dev |
| `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.9` | SQLite / EF; narzędzia Design/Tools tylko build/dev |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.9` | SQLite / EF; narzędzia Design/Tools tylko build/dev |
| `SQLitePCLRaw.bundle_e_sqlite3` | `3.0.3` | SQLite / EF; narzędzia Design/Tools tylko build/dev |
| `@gsap/react` | `^2.1.2` | runtime klienta; animacje UI |
| `@microsoft/signalr` | `^10.0.0` | runtime klienta; realtime z WebApi |
| `gsap` | `^3.15.0` | runtime klienta; animacje UI |
| `react` | `^19.2.0` | runtime klienta |
| `react-dom` | `^19.2.0` | runtime klienta |
| `@eslint/js` | `^9.39.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@playwright/test` | `^1.59.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@testing-library/jest-dom` | `^6.9.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@testing-library/react` | `^16.3.2` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@testing-library/user-event` | `^14.6.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@types/node` | `^24.10.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@types/react` | `^19.2.7` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@types/react-dom` | `^19.2.3` | narzędzie dev/build/test, nie usługa produkcyjna |
| `@vitejs/plugin-react` | `^5.1.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `eslint` | `^9.39.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `eslint-plugin-react-hooks` | `^7.0.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `eslint-plugin-react-refresh` | `^0.4.24` | narzędzie dev/build/test, nie usługa produkcyjna |
| `globals` | `^16.5.0` | narzędzie dev/build/test, nie usługa produkcyjna |
| `jsdom` | `^29.1.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `typescript` | `~5.9.3` | narzędzie dev/build/test, nie usługa produkcyjna |
| `typescript-eslint` | `^8.48.0` | narzędzie dev/build/test, nie usługa produkcyjna |
| `vite` | `^7.3.1` | narzędzie dev/build/test, nie usługa produkcyjna |
| `vite-plugin-svgr` | `^4.5.0` | narzędzie dev/build/test, nie usługa produkcyjna |
| `vitest` | `^4.1.5` | narzędzie dev/build/test, nie usługa produkcyjna |
| .NET SDK / ASP.NET runtime | 10.0 (obrazy Docker), projekty net10.0 | SDK do budowy; runtime do uruchomienia |
| Node.js / npm | 20+ dla rozwoju; Docker/CI Node 22 | Budowa SPA, nie serwer produkcyjnego UI |
| Docker Engine + Compose plugin | brak przypiętej wersji Engine/Compose | Produkcyjny wariant instalacji; trwały wolumin SQLite |
| nginx | stable-alpine | Reverse proxy; HTTPS z osobnym overlay i certyfikatem |
| SMTP | konfigurowalny, bez przypiętego dostawcy | Opcjonalne wysyłanie maili; konieczne dla działającego odzyskiwania hasła |
| Usługa Web Push przeglądarki | zależna od urządzenia | Opcjonalna; opóźnienie/dostarczenie zależy od platformy |

Baza SQLite jest osadzona; nie wymaga osobnego serwera SQL. Repo nie ustanawia płatnej chmury, kolejki ani zewnętrznego cache.

## 6. Wymagania srodowiskowe

### Frontend / uzytkownik

| Obszar | Minimum | Zalecane |
| --- | --- | --- |
| OS | System z obsługiwaną przeglądarką; brak zadeklarowanej ogólnej listy wersji OS | Wspierany przez organizację i producenta, przetestowany na docelowych urządzeniach |
| Przeglądarka | Obsługa współczesnej SPA; konkretne minimalne wersje niezdefiniowane / do zweryfikowania dla docelowego obciążenia | Aktualizowana przeglądarka, sprawdzona macierz rzeczywistych urządzeń |
| Instalacja | Dostęp do URL web; PWA opcjonalna | HTTPS dla publicznego wdrożenia |
| Push | Opcjonalny; iOS/iPadOS 16.4+, Home Screen, HTTPS i zgoda | Sprawdzenie rzeczywistego dostarczenia na urządzeniu; test WebKit nie dowodzi push iPhone |
| CPU/RAM/dysk/łącze | niezdefiniowane / do zweryfikowania dla docelowego obciążenia | niezdefiniowane / do zweryfikowania dla docelowego obciążenia |
| Usługi | Łączność do serwera GF | Stabilna łączność; polityka dostępu organizacji do uzgodnienia |

### Backend / build / deploy

| Obszar | Minimum | Zalecane |
| --- | --- | --- |
| OS | Host zgodny z wybraną metodą .NET/Docker; Ubuntu 24.04 jest przykładem instrukcji | Wspierany system z aktualizacjami; wybór administratora, nie jedyna dozwolona dystrybucja |
| Budowanie | .NET 10 SDK, Node.js 20+, npm | Node 22 zgodnie z CI/Docker; zgodność konkretnej wersji Node z Vite sprawdzić przy instalacji |
| Runtime produkcyjny | ASP.NET 10 w obrazie, Docker Engine + Compose plugin, nginx | Przypięty identyfikator przetestowanego wydania/obrazu |
| Dane | Trwały zapisywalny wolumin, `/data/SQLite.db`; uprawnienia użytkownika obrazu | Osobna zweryfikowana kopia poza hostem/woluminem i kontrola miejsca |
| Sieć / chmura | Dostępne porty, DNS i certyfikaty dla wariantu publicznego HTTPS | Monitoring health/logów i plan odtwarzania; dostawca/region do uzgodnienia |
| CPU/RAM/dysk/IOPS | niezdefiniowane / do zweryfikowania dla docelowego obciążenia | niezdefiniowane / do zweryfikowania dla docelowego obciążenia |
| RPO/RTO, hosting/SLA | Niepotwierdzone w repozytorium | Do uzgodnienia z właścicielem / administratorem po pomiarach i próbie odtworzenia |
| SMTP/push | Nie są wymagane do wszystkich funkcji; konfiguracja potrzebna do ich użycia | Sekrety poza Git, test izolowany przed uruchomieniem wysyłki |

## 7. Uruchamianie i proces zmian

### DEV

W katalogu głównym:

```powershell
dotnet restore GF3.sln
dotnet run --project GF3.WebApi/WebApi.csproj
```

Przed pierwszym uruchomieniem klienta w osobnym terminalu:

```powershell
cd FrontEnd
npm ci
```

Dopuszczalny jest również `npm install`; dla odtworzenia lockfile użyj `npm ci`. SpaProxy uruchamia `npm run dev` przy starcie backendu; Vite działa na `http://localhost:5173` (strictPort). Profil WebApi: `https://localhost:54294` i `http://localhost:54295`. Można uruchomić Vite osobno przez `npm run dev`, unikając dwóch procesów na tym samym porcie. Względne `/api` jest proxy do backendu, WebSocket włączony. Dev Swagger jest pod URL API. Ekran pracowników ma trasę `/employee`.

Prywatne wartości ustaw przez środowisko/ignorowany plik: `ConnectionStrings__Default`, `Jwt__SigningKey`, `Smtp__Host`, `Smtp__UserName`, `Smtp__Password`, `Smtp__FromAddress` oraz wymagane opcje bootstrap/admin. Nie kopiuj przykładowych haseł profilu developerskiego do wdrożenia. Własne konta i baza testowa muszą być odseparowane od produkcji.

### TEST i build

Backend (CI przywraca wcześniej GF3.Tests i dodaje `--no-restore`):

```powershell
dotnet test GF3.Tests/GF3.Tests.csproj --configuration Release --filter "Category!=LocalOnly"
```

Frontend, z katalogu głównego:

```powershell
cd FrontEnd
npm ci
npm run lint
npm run test
npm run build
```

`npm run test` = `vitest run`; `npm run build` = `tsc -b` i `vite build`. xUnit obejmuje backend/integracje SQLite, Testing Library/Vitest komponenty, Playwright przeglądarki. `LocalOnly` jest wyłączone w CI. Skrypty build/test nie stanowią dowodu wykonania testów odbiorowych.

E2E: zainstaluj przeglądarki Playwright zgodne z pakietem (`npx playwright install`), uruchom aplikację testową (np. Vite z osobnego terminala), potem w FrontEnd:

```powershell
npm run test:e2e
```

Konfiguracja nie ma `webServer`: nie uruchamia aplikacji sama. Bazowy URL to `PLAYWRIGHT_BASE_URL` lub `http://localhost:5173`. Projekty: webkit-iphone-x, webkit-laptop, chromium, mobile-chrome. Wiele scenariuszy przechwytuje API i dostarcza fixtures; rzeczywisty odbiór wymaga odrębnych kont/danych/środowiska. Fixtures nie dowodzą integracji produkcyjnej, fizycznego iPhone ani odtwarzania bazy.

### PROD i proces zmian

Docker: Node 22 buduje React, SDK .NET 10 publikuje backend z `SkipFrontendBuild=true`, bundle trafia do `wwwroot`, obraz ASP.NET 10 uruchamia aplikację jako użytkownik `app` na 8080. Nginx jest publicznym proxy; HTTPS wymaga overlay/certyfikatów. Health `/api/health` sprawdza możliwość połączenia z bazą. Pełną instalację, kopie zapasowe i rollback opisano w dokumencie „GF — Procedury odbioru, wdrożenia i odtworzenia”.

CI: push do main/master, pull_request i workflow_dispatch uruchamiają backend Release, lint/test/build frontend; zależny job buduje `gf3-web:ci`. Publikacja GHCR jest osobnym ręcznym workflow_dispatch z tagiem, zapisuje wskazany tag i latest. To nie jest automatyczne wdrożenie na produkcję. PR → przegląd → merge jest zalecanym procesem zespołu; repozytorium nie jest dowodem wymuszonej polityki zatwierdzeń. Przed produkcyjną aktualizacją operator wykonuje backup i kontroluje zgodność bazy/obrazu.

## 8. Pokrycie prezentacji i status weryfikacji

Prezentacja 2: s. 2 — odbiorcy/cel (wstęp, §1); s. 3 — dokumentacja API (§2); s. 4 — baza (§3); s. 5 — własne biblioteki (§4); s. 6 — oprogramowanie zewnętrzne (§5); s. 7–8 — środowisko frontend/backend (§6); s. 9–10 — DEV/PROD/TEST, budowanie i proces zespołu (§7). Wymagane obszary dokumentacji są pokryte; przykłady narzędzi/platform dokumentacji nie są zależnościami GF.

To dokumentacja źródeł na wskazanym commicie. Zestawiono akcje kontrolerów i schematy encji; nie wykonano opisanych wyżej testów aplikacji, odbioru, migracji ani odtwarzania. Warunki infrastruktury klienta pozostają do uzgodnienia.
