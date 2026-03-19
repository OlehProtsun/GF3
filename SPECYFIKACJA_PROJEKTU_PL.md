# Specyfikacja projektu

## 1. Cel i zakres dokumentu
Niniejszy dokument powstał na podstawie analizy istniejącego repozytorium projektu oraz dołączonego artefaktu opisowego i odzwierciedla wyłącznie stan faktyczny implementacji. Opis obejmuje kod źródłowy, konfigurację, pliki infrastrukturalne, dokumentację pomocniczą oraz artefakty projektu bez dopisywania funkcji lub wymagań, których nie da się potwierdzić w repozytorium (GF3.sln, GF3.WebApi/Program.cs, FrontEnd/package.json, WPFApp/App.xaml.cs, docs/run-local.md, C:\Users\Oleg\Desktop\p1.pptx).

## 2. Podstawa analizy
Przeanalizowano następujące obszary repozytorium:

- rozwiązanie i podział na projekty .NET oraz frontend webowy (GF3.sln, GF3.WebApi/WebApi.csproj, BusinessLogicLayer/BusinessLogicLayer.csproj, DataAccessLayer/DataAccessLayer.csproj, WPFApp/WPFApp.csproj, FrontEnd/package.json)
- backend Web API, kontrakty HTTP, middleware, konfigurację i mapowanie DTO (GF3.WebApi/Controllers, GF3.WebApi/Contracts, GF3.WebApi/Middleware, GF3.WebApi/appsettings.json)
- współdzieloną logikę biznesową, generator grafików, eksporty i konfigurację DI (BusinessLogicLayer/Extensions.cs, BusinessLogicLayer/Generators/ScheduleGenerator.cs, BusinessLogicLayer/Services)
- warstwę danych, modele EF Core, migracje oraz repozytoria (DataAccessLayer/AppDbContext.cs, DataAccessLayer/Repositories, DataAccessLayer/Migrations)
- frontend webowy React/Vite wraz z routingiem, klientem API, stanem i widokami (FrontEnd/src, FrontEnd/vite.config.ts)
- klienta desktopowego WPF wraz z modułami MVVM i lokalnymi funkcjami administracyjnymi (WPFApp/App.xaml.cs, WPFApp/ViewModel, WPFApp/View)
- dokumentację i skrypty pomocnicze (API.md, docs/api-map.md, docs/api-contracts.md, docs/api-usage.md, docs/run-local.md, tools/check-architecture.sh)
- artefakt opisowy wskazujący zamierzony kontekst biznesowy produktu (C:\Users\Oleg\Desktop\p1.pptx)

Nota metodologiczna: priorytet miały elementy potwierdzone w kodzie i konfiguracji. Dokumentacja tekstowa oraz plik `p1.pptx` były traktowane pomocniczo. W miejscach niejednoznacznych wskazano brak jednoznacznego potwierdzenia w repozytorium.

## 3. Streszczenie projektu
Projekt jest systemem do zarządzania grafikami pracy pracowników z modułami: pracownicy, sklepy, grupy dostępności, kontenery oraz grafiki/harmonogramy przypisane do sklepów. W systemie zaimplementowano zarówno ręczną edycję grafików, jak i automatyczne generowanie obsady na podstawie ograniczeń i dostępności pracowników (BusinessLogicLayer/Generators/ScheduleGenerator.cs, BusinessLogicLayer/Services/ContainerService.cs, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).

Cel biznesowy można odtworzyć dość pewnie jako planowanie pracy zespołów w małych lub średnich organizacjach operujących na zmianach. Artefakt `p1.pptx` wskazuje wprost na zarządzanie grafikami pracy dla małych firm usługowych, ale szczegółowe wymagania branżowe nie zostały formalnie opisane w repozytorium i wymagają potwierdzenia biznesowego (C:\Users\Oleg\Desktop\p1.pptx, FrontEnd/src/entities/containers/model/types.ts, DataAccessLayer/Models/ScheduleModel.cs).

## 4. Specyfikacja techniczna

### 4.1. Architektura ogólna
- Repozytorium ma architekturę wielomodułową opartą o współdzieloną warstwę logiki biznesowej i danych. Główne moduły to: `GF3.WebApi`, `FrontEnd`, `WPFApp`, `BusinessLogicLayer`, `DataAccessLayer` (GF3.sln).
- Web API udostępnia REST-owe endpointy dla frontendu React oraz Swagger/OpenAPI. Frontend komunikuje się z backendem po ścieżkach `/api`, a w trybie developerskim wykorzystuje proxy Vite i ASP.NET SpaProxy (GF3.WebApi/Program.cs, FrontEnd/vite.config.ts, docs/run-local.md).
- Klient desktopowy WPF nie korzysta z Web API. Rejestruje bezpośrednio warstwę biznesową i łączy się z tą samą bazą SQLite przez `AddBusinessLogicStack`, co oznacza osobny kanał dostępu do danych obok interfejsu HTTP (WPFApp/App.xaml.cs, WPFApp/Applications/Configuration/DatabasePathProvider.cs, BusinessLogicLayer/Extensions.cs).
- Trwała persystencja oparta jest o SQLite i EF Core. Web API uruchamia migracje automatycznie przy starcie aplikacji (GF3.WebApi/Program.cs, DataAccessLayer/AppDbContext.cs, DataAccessLayer/Migrations).
- Dodatkowo istnieje lekki skrypt kontrolny architektury, który pilnuje, aby WPF nie odwoływał się bezpośrednio do `DataAccessLayer` (tools/check-architecture.sh).

### 4.2. Użyte technologie
| Obszar | Technologie potwierdzone w repozytorium | Źródła |
| --- | --- | --- |
| Frontend web | React 19, TypeScript 5.9, Vite 7, CSS Modules, `vite-plugin-svgr` | FrontEnd/package.json, FrontEnd/vite.config.ts |
| Routing i stan web | Własne lekkie implementacje aliasowane pod `react-router-dom` i `@tanstack/react-query` | FrontEnd/src/shared/lib/react-router-dom.tsx, FrontEnd/src/shared/lib/tanstack/react-query.tsx, FrontEnd/vite.config.ts |
| Klient desktopowy | WPF na `.NET 10-windows`, MVVM, `CommunityToolkit.Mvvm`, `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.DependencyInjection` | WPFApp/WPFApp.csproj, WPFApp/App.xaml.cs |
| Backend | ASP.NET Core Web API na `.NET 10`, Swagger/OpenAPI, SpaProxy | GF3.WebApi/WebApi.csproj, GF3.WebApi/Program.cs |
| Logika biznesowa | Własne serwisy i fasady, generator harmonogramów, eksporty Excel/SQL | BusinessLogicLayer/Services, BusinessLogicLayer/Generators/ScheduleGenerator.cs |
| Baza danych | EF Core 10, SQLite, migracje code-first | DataAccessLayer/DataAccessLayer.csproj, DataAccessLayer/AppDbContext.cs, DataAccessLayer/Migrations |
| Dokumenty/eksport | ClosedXML, lokalne szablony XLSX | BusinessLogicLayer/BusinessLogicLayer.csproj, WPFApp/WPFApp.csproj, GF3.WebApi/Resources/ExcelTemplate, WPFApp/Resources/Excel |
| Infrastruktura developerska | launchSettings, Vite proxy, SpaProxy, skrypt bash do kontroli architektury | GF3.WebApi/Properties/launchSettings.json, FrontEnd/vite.config.ts, tools/check-architecture.sh |
| Testy | Brak potwierdzonych działających testów automatycznych; rozwiązanie odwołuje się do brakującego projektu `ArchitectureTests` | GF3.sln, wynik `dotnet build GF3.sln`, brak pliku `ArchitectureTests/ArchitectureTests.csproj` |
| Monitoring i logowanie | Wbudowane logowanie ASP.NET Core, `ProblemDetails`, własny middleware wyjątków | GF3.WebApi/appsettings.json, GF3.WebApi/Program.cs, GF3.WebApi/Middleware/ApiExceptionMiddleware.cs |

### 4.3. Kluczowe biblioteki i usługi wykryte w projekcie
- `ClosedXML` służy do generowania plików Excel dla pojedynczych grafików i całych kontenerów oraz do obsługi szablonów eksportu (BusinessLogicLayer/Services/GraphTemplateExportService.cs, WPFApp/Applications/Export/ScheduleExportService.cs).
- `Microsoft.EntityFrameworkCore.Sqlite` zapewnia dostęp do lokalnej bazy SQLite wraz z migracjami i ograniczeniami modelu danych (DataAccessLayer/DataAccessLayer.csproj, DataAccessLayer/AppDbContext.cs).
- `Swashbuckle.AspNetCore` oraz `Microsoft.AspNetCore.OpenApi` udostępniają dokumentację Swagger dla Web API (GF3.WebApi/WebApi.csproj, GF3.WebApi/Program.cs).
- `Microsoft.AspNetCore.SpaProxy` i `Microsoft.AspNetCore.SpaServices.Extensions` łączą backend z frontendem Vite w trybie developerskim (GF3.WebApi/WebApi.csproj, docs/run-local.md).
- Własny `SqliteAdminService` i warstwa `AdminDbService` umożliwiają odczyt metadanych bazy, hashowanie plików, wykonywanie bezpiecznie ograniczonych zapytań oraz import skryptów SQL (DataAccessLayer/Administration/SqliteAdminService.cs, BusinessLogicLayer/Services/AdminDbService.cs, GF3.WebApi/Controllers/AdminDbController.cs).
- Własne aliasy do routera i query clienta oznaczają, że frontend nie korzysta z oficjalnych pakietów `react-router-dom` i `@tanstack/react-query`, mimo że importy wyglądają podobnie (FrontEnd/vite.config.ts, FrontEnd/src/shared/lib/react-router-dom.tsx, FrontEnd/src/shared/lib/tanstack/react-query.tsx).
- Brak potwierdzonych integracji biznesowych z usługami zewnętrznymi typu płatności, e-mail, SMS, ERP, CRM, storage cloud czy SSO.

### 4.4. Backend — analiza techniczna
Backend składa się z trzech głównych warstw: Web API, wspólnej warstwy biznesowej oraz warstwy danych EF Core/SQLite. `GF3.WebApi` odpowiada za HTTP, walidację wejścia, mapowanie DTO i konfigurację middleware. `BusinessLogicLayer` zawiera logikę domenową, generator grafików, eksporty i fasady administracyjne. `DataAccessLayer` przechowuje model bazy, repozytoria i migracje (GF3.WebApi/Program.cs, BusinessLogicLayer/Extensions.cs, DataAccessLayer/AppDbContext.cs).

Obszary API są wyraźnie rozdzielone na kontrolery:

- pracownicy: pełny CRUD `GET/POST/PUT/DELETE /api/employees` (GF3.WebApi/Controllers/EmployeesController.cs)
- sklepy: pełny CRUD `GET/POST/PUT/DELETE /api/shops` (GF3.WebApi/Controllers/ShopsController.cs)
- kontenery: pełny CRUD `GET/POST/PUT/DELETE /api/containers` oraz zagnieżdżone grafiki, sloty, pracownicy grafików, style komórek i presety (GF3.WebApi/Controllers/ContainersController.cs)
- grupy dostępności: CRUD grup, członków i slotów dostępności oraz widok pozycji zbiorczych `items` (GF3.WebApi/Controllers/AvailabilityGroupsController.cs)
- bindy dostępności: CRUD skrótów oraz lista aktywnych bindów (GF3.WebApi/Controllers/AvailabilityBindsController.cs)
- eksporty: eksport pojedynczego grafiku i całego kontenera do XLSX lub SQL (GF3.WebApi/Controllers/ExportsController.cs)
- zdrowie aplikacji: `GET /api/health` (GF3.WebApi/Controllers/HealthController.cs)
- administracja bazą: metadane, hash, query, execute i import SQL z dodatkowymi zabezpieczeniami (GF3.WebApi/Controllers/AdminDbController.cs, GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs)

Warstwa kontraktów HTTP jest oparta o DataAnnotations. Przykładowo: pracownik wymaga `FirstName` i `LastName`, sklep wymaga `Name` i `Address`, a grafik wymaga zestawu parametrów planowania takich jak `ShopId`, miesiąc, rok, liczba osób na zmianę, godziny dwóch zmian oraz limity obciążenia pracowników (GF3.WebApi/Contracts/Employees/CreateEmployeeRequest.cs, GF3.WebApi/Contracts/Shops/CreateShopRequest.cs, GF3.WebApi/Contracts/Containers/Graphs/CreateGraphRequest.cs).

Model danych jest rozbudowany i obejmuje m.in. `Containers`, `Shops`, `Employees`, `Schedules`, `SchedulePresets`, `ScheduleEmployees`, `ScheduleSlots`, `ScheduleCellStyles`, `AvailabilityGroups`, `AvailabilityGroupMembers`, `AvailabilityGroupDays` i `AvailabilityBinds`. W `AppDbContext` zdefiniowano liczne ograniczenia unikalności i walidacji na poziomie bazy, np. unikalność pełnego imienia i nazwiska pracownika, unikalność nazwy sklepu, spójność statusu slotu z przypisaniem pracownika czy zakresy dni miesiąca i numerów slotów (DataAccessLayer/AppDbContext.cs).

Nazewnictwo domeny jest niespójne między warstwami. W warstwie danych i biznesowej dominuje pojęcie `Schedule`, natomiast w API i frontendzie webowym ten sam byt jest w wielu miejscach prezentowany jako `Graph`. Nie blokuje to działania, ale utrudnia czytelność i dokumentowanie systemu (DataAccessLayer/Models/ScheduleModel.cs, BusinessLogicLayer/Contracts/Models/Models.cs, GF3.WebApi/Contracts/Containers/Graphs/GraphDto.cs, FrontEnd/src/entities/containers/api/dto.ts).

Logika biznesowa obejmuje:

- walidację danych pracowników i sklepów wraz z kontrolą duplikatów (BusinessLogicLayer/Services/EmployeeService.cs, BusinessLogicLayer/Services/ShopService.cs)
- pełną obsługę kontenerów i grafików, w tym CRUD, przypisania pracowników, ręczne sloty, style komórek, presety i wywołanie generatora (BusinessLogicLayer/Services/ContainerService.cs)
- pełną obsługę grup dostępności, członków, dni i serializacji pozycji zbiorczych (BusinessLogicLayer/Services/AvailabilityGroupService.cs, BusinessLogicLayer/Services/AvailabilityPayloadBuilder.cs, BusinessLogicLayer/Services/AvailabilityCodeParser.cs)
- zarządzanie bindami dostępności (BusinessLogicLayer/Services/BindService.cs)
- eksporty SQL i XLSX dla grafików i kontenerów (BusinessLogicLayer/Services/GraphExportService.cs, BusinessLogicLayer/Services/GraphTemplateExportService.cs)
- administrację bazą SQLite z ograniczeniem dopuszczalnych poleceń (BusinessLogicLayer/Services/AdminDbService.cs)

Automatyczny generator grafiku jest realnie zaimplementowany, a nie tylko zadeklarowany. `ScheduleGenerator` zawiera logikę rozkładania slotów, uwzględniania dostępności, limitów miesięcznych i kolejnych dni pracy, a `ContainerService` serializuje dostęp do generacji danego grafiku za pomocą `SemaphoreSlim` trzymanego per `graphId` (BusinessLogicLayer/Generators/ScheduleGenerator.cs, BusinessLogicLayer/Services/ContainerService.cs).

Bezpieczeństwo backendu jest ograniczone do walidacji wejścia, CORS dla środowiska developerskiego, middleware wyjątków oraz ochrony narzędzi administracyjnych. W repozytorium nie ma konfiguracji `AddAuthentication`, `AddAuthorization`, JWT, cookies, ról ani claims. Narzędzia administracyjne są zabezpieczone tylko przez żądanie z loopbacka i nagłówek `X-Admin-Token`, a w `appsettings.json` funkcja jest włączona z `AllowWriteSql=true`, co należy traktować jako stan nieprodukcyjny lub wymagający potwierdzenia (GF3.WebApi/Program.cs, GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs, GF3.WebApi/appsettings.json).

Obsługa błędów HTTP jest zaimplementowana przez własny middleware, który mapuje błędy walidacji na `400`, błędy braku danych na `404`, przerwania żądania na `499`, a nieobsłużone wyjątki na `500` w formacie `ProblemDetails`/JSON (GF3.WebApi/Middleware/ApiExceptionMiddleware.cs).

Konfiguracja środowiskowa jest minimalna. `ConnectionStrings:Default` jest puste, a aplikacja używa fallbacku do pliku `%LocalAppData%\GF3\SQLite.db`. Szablony eksportów mogą być pobierane z katalogu konfiguracyjnego lub kopiowane do `LocalAppData` przez `ExcelTemplateLocator` (GF3.WebApi/appsettings.json, GF3.WebApi/appsettings.Development.json, GF3.WebApi/Program.cs, BusinessLogicLayer/Services/Excel/ExcelTemplateLocator.cs).

Nie potwierdzono w repozytorium: cache, kolejki, webhooki, background jobs, zewnętrznego monitoringu, rate limiting, polityk backupu, disaster recovery, wielośrodowiskowych pipeline'ów CI/CD ani konteneryzacji Docker.

Elementy nieukończone lub ryzykowne po stronie backendu:

- rozwiązanie odwołuje się do brakującego projektu `ArchitectureTests`, przez co `dotnet build GF3.sln` kończy się błędem (GF3.sln, brak `ArchitectureTests/ArchitectureTests.csproj`)
- brak testów automatycznych potwierdzonych w repozytorium
- `ShopService` nie wykonuje własnej kontroli referencji przy usuwaniu sklepu mimo istnienia metody repozytorium `HasScheduleReferencesAsync`, więc część błędów może być zwracana dopiero przez bazę danych (BusinessLogicLayer/Services/ShopService.cs, DataAccessLayer/Repositories/ShopRepository.cs)
- `AppDbContext` zawiera zduplikowaną konfigurację `ScheduleCellStyleModel`, co nie wygląda na intencjonalny fragment domeny (DataAccessLayer/AppDbContext.cs)
- `ApiSamples.http` zawiera komentarz sugerujący eksport CSV, podczas gdy faktyczny endpoint zwraca XLSX (GF3.WebApi/ApiSamples.http, GF3.WebApi/Controllers/ExportsController.cs)

Kluczowe pliki i obszary, na których oparto wnioski: `GF3.WebApi/Program.cs`, `GF3.WebApi/appsettings.json`, `GF3.WebApi/Controllers/*`, `GF3.WebApi/Contracts/*`, `GF3.WebApi/Middleware/*`, `BusinessLogicLayer/Extensions.cs`, `BusinessLogicLayer/Services/*`, `BusinessLogicLayer/Generators/ScheduleGenerator.cs`, `DataAccessLayer/AppDbContext.cs`, `DataAccessLayer/Repositories/*`, `DataAccessLayer/Migrations/*`.

### 4.5. Frontend — analiza techniczna
Repozytorium zawiera dwa odrębne interfejsy użytkownika: frontend webowy React/Vite oraz klient desktopowy WPF. Oba korzystają z tej samej domeny biznesowej, ale tylko frontend webowy komunikuje się przez HTTP z `GF3.WebApi`. Klient WPF wywołuje logikę biznesową bezpośrednio w procesie aplikacji (FrontEnd/src, WPFApp/App.xaml.cs, BusinessLogicLayer/Extensions.cs).

#### Frontend webowy (React/Vite)
Struktura aplikacji jest modułowa i oparta o katalogi `app`, `pages`, `entities`, `shared`. Routing jest realizowany przez własną implementację `BrowserRouter` i ręczne dopasowywanie ścieżek zamiast pełnej konfiguracji `react-router-dom`. Obsługiwane są m.in. ścieżki dla pracowników, sklepów, grup dostępności, kontenerów i grafików, natomiast nie wszystkie istniejące strony są faktycznie podłączone do routingu (FrontEnd/src/app/router/router.tsx, FrontEnd/src/shared/lib/react-router-dom.tsx).

Stan i wywołania API realizowane są przez własny odpowiednik `QueryClient` oraz hooki `useQuery`/`useMutation`, a klucze zapytań są scentralizowane w `queryKeys`. To rozwiązanie działa jak lekki cache/invalidation layer, ale nie jest oficjalnym pakietem TanStack Query (FrontEnd/src/app/providers/QueryProvider.tsx, FrontEnd/src/shared/lib/tanstack/react-query.tsx, FrontEnd/src/shared/api/queryKeys.ts).

Warstwa komunikacji HTTP używa wspólnego `httpClient`, który obsługuje JSON, błędy API i pobieranie `Blob`. Jednocześnie jest tu wykryta konkretna rozbieżność implementacyjna: klient zawsze ustawia `Content-Type: application/json` i serializuje `body` przez `JSON.stringify`, co nie pasuje do wywołania `adminDbApi.importSql(file)` z `FormData`. Oznacza to, że webowy upload pliku do `/api/admin/db/import` nie ma kompletnego, potwierdzonego działania (FrontEnd/src/shared/api/httpClient.ts, FrontEnd/src/entities/admin-db/api/adminDbApi.ts).

Główne moduły webowe:

- pracownicy: listowanie, profil, tworzenie, edycja i usuwanie są w pełni podłączone do API (FrontEnd/src/pages/employee-list/ui/EmployeeListPage.tsx, FrontEnd/src/pages/employee-profile/ui/EmployeeProfilePage.tsx, FrontEnd/src/pages/employee-edit/ui/EmployeeEditPage.tsx, FrontEnd/src/entities/employees/api/queries.ts)
- sklepy: analogiczny CRUD z pełnym przepływem danych (FrontEnd/src/pages/shop-list/ui/ShopListPage.tsx, FrontEnd/src/pages/shop-profile/ui/ShopProfilePage.tsx, FrontEnd/src/pages/shop-edit/ui/ShopEditPage.tsx, FrontEnd/src/entities/shops/api/queries.ts)
- dostępność: listy, profil i rozbudowany edytor macierzy dostępności z pracownikami, dniami, bindami i synchronizacją członków/slotów (FrontEnd/src/pages/availability/ui/AvailabilityPage.tsx, FrontEnd/src/pages/availability-profile/ui/AvailabilityProfilePage.tsx, FrontEnd/src/pages/availability-edit/ui/AvailabilityEditPage.tsx, FrontEnd/src/entities/availability-groups/api/queries.ts, FrontEnd/src/entities/availability-groups/model/matrix.ts)
- kontenery i grafiki: listy kontenerów, profil kontenera, profil grafiku, tworzenie i rozbudowana edycja grafiku z przypisaniami pracowników, ręczną macierzą slotów, generowaniem, stylami komórek, bindami oraz presetami (FrontEnd/src/pages/container/ui/ContainerPage.tsx, FrontEnd/src/pages/container-graph-profile/ui/ContainerGraphProfilePage.tsx, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, FrontEnd/src/entities/containers/api/queries.ts, FrontEnd/src/entities/containers/model/graphWorkspace.ts)
- eksporty: pobieranie XLSX i SQL dla grafiku oraz kontenera z poziomu UI kontenerów/grafików (FrontEnd/src/entities/exports/api/exportsApi.ts, FrontEnd/src/entities/containers/ui/ContainerGraphProfileWorkspace.tsx, FrontEnd/src/entities/containers/ui/ContainerProfileWorkspace.tsx)

Formularze są walidowane ręcznie po stronie UI, bez dedykowanej biblioteki formularzy. Potwierdzono walidacje wymaganych pól, zakresów liczbowych i komunikatów błędów w formularzach pracowników, sklepów, grup dostępności i grafików (FrontEnd/src/entities/employees/ui/EmployeeDetailsForm.tsx, FrontEnd/src/entities/shops/ui/ShopDetailsForm.tsx, FrontEnd/src/entities/availability-groups/ui/AvailabilityGroupFormDialog.tsx, FrontEnd/src/entities/containers/ui/ContainerGraphDetailsForm.tsx).

W edytorze grafiku istnieje funkcja ręcznych kolumn i kolejności kolumn, ale dane te nie mają osobnego modelu po stronie backendu. Są kodowane w ukrytym bloku metadanych dopisywanym do pola `note`. To jest funkcjonalność rzeczywiście zaimplementowana po stronie webowej, lecz oparta na pośredniej serializacji tekstowej, a nie na dedykowanym kontrakcie API (FrontEnd/src/entities/containers/model/graphNote.ts, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, GF3.WebApi/Contracts/Containers/Graphs/CreateGraphRequest.cs).

Responsywność i dostępność są częściowo potwierdzone. W kodzie występują media queries dla wielu widoków oraz atrybuty `aria-*`, role `dialog`, `listbox`, `alert`, `search`, a także komunikaty błędów `aria-live`. Brak jednak jawnej deklaracji zgodności z WCAG czy listy wspieranych przeglądarek (FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.module.css, FrontEnd/src/shared/ui/ConfirmDialog/ConfirmDialog.tsx, FrontEnd/src/shared/ui/components/ErrorBanner/ErrorBanner.tsx).

Elementy niedokończone po stronie web:

- `HomePage`, `InformationPage` i `DataBasePage` są stronami-stubami z napisem `TODO` (FrontEnd/src/pages/home/ui/HomePage.tsx, FrontEnd/src/pages/information/ui/InformationPage.tsx, FrontEnd/src/pages/database/ui/DataBasePage.tsx)
- istnieje `HomeTestPage`, ale nie jest podłączona do routingu (FrontEnd/src/pages/hometest/ui/HomeTestPage.tsx, FrontEnd/src/app/router/router.tsx)
- warstwa klienta dla `health` i `admin-db` istnieje, lecz nie jest wykorzystywana przez ukończone strony webowe (FrontEnd/src/entities/health/api/queries.ts, FrontEnd/src/entities/admin-db/api/queries.ts, FrontEnd/src/pages/database/ui/DataBasePage.tsx)
- `FrontEnd/README.md` jest domyślnym README szablonu Vite i nie dokumentuje faktycznego projektu (FrontEnd/README.md)

Nie znaleziono potwierdzonych testów frontendowych.

#### Klient desktopowy (WPF)
Klient WPF jest rozbudowanym, realnie działającym modułem projektu, a nie szkieletem. Rejestruje `MainWindow`, widoki i view modele dla obszarów: Home, Employee, Availability, Shop, Container, Information i Database. Korzysta z hosta DI, usług biznesowych oraz lokalnej ścieżki bazy `%LocalAppData%\GF3\SQLite.db` (WPFApp/App.xaml.cs, WPFApp/Applications/Configuration/DatabasePathProvider.cs).

Potwierdzone moduły WPF:

- Home: dashboard bieżącego miesiąca, aktywne grafiki, statystyki i widok „who works today” (WPFApp/ViewModel/Home/HomeViewModel.cs)
- Employee i Shop: wielosekcyjny UI `List/Edit/Profile` oparty o fasady domenowe i powiadomienia o zmianie bazy (WPFApp/ViewModel/Employee/EmployeeViewModel.cs, WPFApp/ViewModel/Shop/ShopViewModel.cs)
- Availability: listy, profil i edytor dostępności z bindami (WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs)
- Container: listy, profil kontenera, edycja grafiku, profil grafiku, generowanie, eksport oraz obsługa kolorowania komórek (WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs, WPFApp/Applications/Export/ScheduleExportService.cs)
- Database: wykonywanie SQL, import skryptów `.sql`, odświeżanie metadanych bazy i prezentacja wyników zapytań (WPFApp/ViewModel/Database/DatabaseViewModel.cs, WPFApp/View/DatabaseView.xaml)

Moduł `Information` w WPF nie zawiera logiki biznesowej i wyświetla ogólny, placeholderowy tekst informacyjny. To należy traktować jako obszar nieukończony lub pomocniczy (WPFApp/ViewModel/Information/InformationViewModel.cs, WPFApp/View/Information/InformationView.xaml).

Kluczowe pliki i obszary, na których oparto wnioski: `FrontEnd/package.json`, `FrontEnd/vite.config.ts`, `FrontEnd/src/app/router/router.tsx`, `FrontEnd/src/shared/api/httpClient.ts`, `FrontEnd/src/entities/*`, `FrontEnd/src/pages/*`, `WPFApp/App.xaml.cs`, `WPFApp/ViewModel/*`, `WPFApp/View/*`, `WPFApp/Applications/Export/ScheduleExportService.cs`.

### 4.6. Integracje z istniejącymi systemami i usługami zewnętrznymi
Brak potwierdzonych integracji biznesowych z systemami zewnętrznymi w rodzaju ERP, CRM, bramek płatniczych, e-mail/SMS, usług chmurowych, brokerów kolejek czy zewnętrznych magazynów plików. Potwierdzono jedynie integracje techniczne z lokalnym plikiem SQLite, lokalnymi szablonami XLSX, Swagger/OpenAPI i narzędziami deweloperskimi Vite/SpaProxy (DataAccessLayer/AppDbContext.cs, BusinessLogicLayer/Services/Excel/ExcelTemplateLocator.cs, GF3.WebApi/Program.cs, FrontEnd/vite.config.ts).

### 4.7. Obsługiwane urządzenia, platformy i środowiska użytkownika końcowego
- aplikacja webowa: potwierdzono działanie jako SPA w przeglądarce przez `BrowserRouter`, Vite i statyczne pliki serwowane z `wwwroot`; brak jawnej listy wspieranych przeglądarek (FrontEnd/src/main.tsx, FrontEnd/src/app/router/router.tsx, GF3.WebApi/Program.cs)
- aplikacja desktopowa: potwierdzono wsparcie dla Windows przez `net10.0-windows` i `UseWPF=true` (WPFApp/WPFApp.csproj)
- urządzenia mobilne: brak jednoznacznego potwierdzenia pełnego wsparcia; w CSS istnieją breakpointy i adaptacje dla mniejszych szerokości, ale repozytorium nie zawiera deklaracji wsparcia mobile-first ani testów na urządzeniach (FrontEnd/src/app/layouts/overlay-sidebar-layout/OverlaySidebarLayout.module.css, FrontEnd/src/pages/container/ui/ContainerPage.module.css)

### 4.8. Wymagane zasoby i komponenty środowiska
- .NET 10 dla backendu, warstw współdzielonych i klienta WPF (GF3.WebApi/WebApi.csproj, BusinessLogicLayer/BusinessLogicLayer.csproj, DataAccessLayer/DataAccessLayer.csproj, WPFApp/WPFApp.csproj)
- Node.js i npm dla developerskiego uruchamiania i budowania frontendu webowego (FrontEnd/package.json, docs/run-local.md)
- SQLite jako baza danych, domyślnie w `%LocalAppData%\GF3\SQLite.db`, jeśli nie podano connection stringa (GF3.WebApi/Program.cs, WPFApp/Applications/Configuration/DatabasePathProvider.cs)
- lokalne szablony eksportu Excel w zasobach projektu lub katalogu `LocalAppData` (GF3.WebApi/Resources/ExcelTemplate, WPFApp/Resources/Excel, BusinessLogicLayer/Services/Excel/ExcelTemplateLocator.cs)
- w środowisku developerskim: Web API na `https://localhost:54294` / `http://localhost:54295`, Vite na `http://localhost:5173` (GF3.WebApi/Properties/launchSettings.json, FrontEnd/vite.config.ts, docs/run-local.md)

### 4.9. Skala systemu
Repozytorium nie zawiera wiarygodnych danych o liczbie użytkowników, oczekiwanym obciążeniu, wolumenach danych produkcyjnych ani docelowym czasie odpowiedzi. Można potwierdzić jedynie pojedyncze parametry techniczne:

- maksymalna długość SQL dla narzędzi administracyjnych: `20000` znaków (GF3.WebApi/appsettings.json, GF3.WebApi/Options/AdminToolsOptions.cs)
- maksymalny rozmiar importu SQL przez admin tools: `2000000` bajtów (GF3.WebApi/appsettings.json, GF3.WebApi/Options/AdminToolsOptions.cs)
- numer slotu grafiku ograniczony do zakresu `1..100`, dzień miesiąca do `1..31` (GF3.WebApi/Contracts/Containers/Graphs/Slots/CreateGraphSlotRequest.cs, DataAccessLayer/AppDbContext.cs)
- desktopowy klient WPF ogranicza liczbę jednocześnie otwartych grafików do `20` na poziomie stałej aplikacyjnej (WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs)

Brak danych o cache, kolejkach, shardingu, limitach współbieżności użytkowników lub horyzontalnym skalowaniu.

## 5. Specyfikacja funkcjonalna

### 5.1. Podejście analityczne
Analiza funkcjonalna została zsyntetyzowana z implementacji modeli, serwisów, kontrolerów, widoków, DTO i przepływów danych. Funkcjonalność została uznana za istniejącą tylko tam, gdzie udało się potwierdzić ją kodem wykonywalnym, kontraktem API lub widokiem rzeczywiście połączonym z logiką aplikacji. Elementy oparte wyłącznie na nazwie pliku, placeholderze lub niedokończonym ekranie zostały oznaczone jako częściowe albo niejednoznaczne (GF3.WebApi/Controllers, BusinessLogicLayer/Services, FrontEnd/src/pages, WPFApp/ViewModel).

### 5.2. Pełna lista funkcjonalności

#### Zarządzanie personelem
- **CRUD pracowników**  
  System umożliwia listowanie, tworzenie, edycję, podgląd i usuwanie pracowników. Funkcja jest dostępna w Web API, w kliencie webowym oraz w kliencie WPF. Usuwanie jest blokowane, gdy pracownik jest użyty w dostępności lub grafikach (GF3.WebApi/Controllers/EmployeesController.cs, BusinessLogicLayer/Services/EmployeeService.cs, FrontEnd/src/pages/employee-list/ui/EmployeeListPage.tsx, FrontEnd/src/pages/employee-edit/ui/EmployeeEditPage.tsx, WPFApp/ViewModel/Employee/EmployeeViewModel.cs).  
  Status: **zaimplementowane**

#### Zarządzanie sklepami
- **CRUD sklepów/lokalizacji**  
  System umożliwia listowanie, tworzenie, edycję, podgląd i usuwanie sklepów. Funkcja jest obecna w API, webie i WPF. Brak dodatkowego, przyjaznego komunikatu o referencjach przy usuwaniu sklepu po stronie serwisu może skutkować błędem z poziomu bazy (GF3.WebApi/Controllers/ShopsController.cs, BusinessLogicLayer/Services/ShopService.cs, FrontEnd/src/pages/shop/ui/ShopPage.tsx, WPFApp/ViewModel/Shop/ShopViewModel.cs).  
  Status: **zaimplementowane**

#### Dostępność pracowników
- **CRUD grup dostępności**  
  System pozwala tworzyć, przeglądać, edytować i usuwać grupy dostępności opisane nazwą, rokiem i miesiącem. Funkcja jest obecna w API, webie i WPF (GF3.WebApi/Controllers/AvailabilityGroupsController.cs, BusinessLogicLayer/Services/AvailabilityGroupService.cs, FrontEnd/src/pages/availability/ui/AvailabilityPage.tsx, WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs).  
  Status: **zaimplementowane**

- **Zarządzanie członkami grupy dostępności**  
  Do grupy można przypisywać pracowników wraz z kolejnością wyświetlania. Funkcja jest obsługiwana przez zagnieżdżone endpointy, webowy edytor i klienta WPF (GF3.WebApi/Controllers/AvailabilityGroupsController.cs, GF3.WebApi/Contracts/AvailabilityGroups/Members/CreateAvailabilityGroupMemberRequest.cs, FrontEnd/src/entities/availability-groups/api/queries.ts, WPFApp/ViewModel/Availability/Main/AvailabilityViewModel.cs).  
  Status: **zaimplementowane**

- **Edycja macierzy dostępności dzień/pracownik**  
  Dla każdego pracownika w grupie można określić dostępność na poziomie dnia miesiąca jako `ANY`, `NONE` lub interwał godzinowy. Webowy edytor i warstwa biznesowa implementują parsowanie oraz synchronizację tych danych (BusinessLogicLayer/Services/AvailabilityCodeParser.cs, GF3.WebApi/Contracts/AvailabilityGroups/Slots/CreateAvailabilitySlotRequest.cs, FrontEnd/src/entities/availability-groups/model/matrix.ts, FrontEnd/src/entities/availability-groups/ui/AvailabilityScheduleMatrix.tsx).  
  Status: **zaimplementowane**

- **Bindy skrótów dostępności**  
  System przechowuje słownik skrótów klawiaturowych lub oznaczeń mapowanych na wartości dostępności. Funkcja jest dostępna przez osobny moduł API i wykorzystywana w webowym edytorze dostępności oraz grafiku, a także przez klienta WPF (GF3.WebApi/Controllers/AvailabilityBindsController.cs, BusinessLogicLayer/Services/BindService.cs, FrontEnd/src/entities/availability-binds/api/queries.ts, WPFApp/ViewModel/Availability/Edit/AvailabilityEditViewModel.Binds.cs).  
  Status: **zaimplementowane**

#### Kontenery i grafiki
- **CRUD kontenerów**  
  Kontener jest bytem grupującym grafiki oraz agregującym eksporty/statystyki. Można go tworzyć, edytować, przeglądać i usuwać. Funkcja występuje w API, webie i WPF (GF3.WebApi/Controllers/ContainersController.cs, BusinessLogicLayer/Services/ContainerService.cs, FrontEnd/src/pages/container/ui/ContainerPage.tsx, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).  
  Status: **zaimplementowane**

- **CRUD grafików w kontenerze**  
  W obrębie kontenera można zakładać grafiki przypisane do sklepu, miesiąca, roku i zestawu parametrów planowania. Funkcjonalność jest pełna po stronie API, webu i WPF (GF3.WebApi/Controllers/ContainersController.cs, GF3.WebApi/Contracts/Containers/Graphs/CreateGraphRequest.cs, FrontEnd/src/entities/containers/api/queries.ts, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).  
  Status: **zaimplementowane**

- **Przypisywanie pracowników do grafiku**  
  Dla grafiku można utrzymywać listę pracowników z `minHoursMonth` i `displayOrder`. Funkcję obsługują zagnieżdżone endpointy i oba klienty UI (GF3.WebApi/Controllers/ContainersController.cs, GF3.WebApi/Contracts/Containers/Graphs/Employees/AddGraphEmployeeRequest.cs, FrontEnd/src/entities/containers/api/queries.ts, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).  
  Status: **zaimplementowane**

- **Ręczna edycja slotów grafiku**  
  System umożliwia ręczne tworzenie, aktualizację i usuwanie slotów zmian z godziną od-do, statusem oraz opcjonalnym przypisaniem pracownika. Funkcja występuje w API, webowym edytorze i logice WPF (GF3.WebApi/Controllers/ContainersController.cs, GF3.WebApi/Contracts/Containers/Graphs/Slots/CreateGraphSlotRequest.cs, FrontEnd/src/entities/containers/model/graphWorkspace.ts, WPFApp/ViewModel/Container/ScheduleEdit).  
  Status: **zaimplementowane**

- **Kolorowanie komórek grafiku**  
  Dla kombinacji dzień/pracownik można zapisać styl komórki z kolorem tła i tekstu. Funkcja ma dedykowane endpointy oraz UI po stronie web i WPF (GF3.WebApi/Controllers/ContainersController.cs, GF3.WebApi/Contracts/Containers/Graphs/CellStyles/UpsertGraphCellStyleRequest.cs, FrontEnd/src/entities/containers/ui/ContainerGraphColorDialog.tsx, WPFApp/ViewModel/Container/ScheduleEdit/ContainerScheduleEditViewModel.CellStyling.cs).  
  Status: **zaimplementowane**

- **Automatyczne generowanie grafiku**  
  Dla istniejącego grafiku można uruchomić generator uwzględniający dostępność i limity pracy. Obsługiwane są tryby `Overwrite`, `DryRun` i `ReturnSlots`. Funkcja jest dostępna przez API, webowy edytor i klienta WPF (GF3.WebApi/Controllers/ContainersController.cs, GF3.WebApi/Contracts/Containers/Graphs/GenerateGraphRequest.cs, BusinessLogicLayer/Generators/ScheduleGenerator.cs, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).  
  Status: **zaimplementowane**

- **Presety grafików**  
  System pozwala zapisywać preset parametrów grafiku wraz z listą pracowników i później go wybierać w interfejsie. Potwierdzono listowanie i tworzenie presetów; brak jednoznacznie potwierdzonych endpointów do aktualizacji i usuwania presetów (GF3.WebApi/Controllers/ContainersController.cs, GF3.WebApi/Contracts/Containers/SchedulePresets/CreateSchedulePresetRequest.cs, FrontEnd/src/entities/containers/ui/ContainerGraphPresetDialog.tsx, FrontEnd/src/entities/containers/ui/ContainerGraphPresetSelect.tsx).  
  Status: **zaimplementowane częściowo**

- **Ręczne kolumny i kolejność kolumn w webowym edytorze grafiku**  
  Webowy edytor wspiera dodatkowe kolumny oraz własną kolejność kolumn, ale dane te są zapisywane pośrednio w polu `note` jako ukryty blok metadanych HTML. Brak osobnego modelu backendowego dla tej funkcji (FrontEnd/src/entities/containers/model/graphNote.ts, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, GF3.WebApi/Contracts/Containers/Graphs/CreateGraphRequest.cs).  
  Status: **zaimplementowane częściowo**

#### Eksporty i artefakty danych
- **Eksport pojedynczego grafiku do XLSX**  
  Grafiki można eksportować do pliku Excel budowanego na podstawie szablonu i danych o pracownikach, slotach oraz stylach komórek (GF3.WebApi/Controllers/ExportsController.cs, BusinessLogicLayer/Services/GraphTemplateExportService.cs, FrontEnd/src/entities/exports/api/exportsApi.ts, WPFApp/Applications/Export/ScheduleExportService.cs).  
  Status: **zaimplementowane**

- **Eksport kontenera do XLSX**  
  Cały kontener wraz z zestawieniem sklepów i grafików można eksportować do pliku Excel (GF3.WebApi/Controllers/ExportsController.cs, BusinessLogicLayer/Services/GraphTemplateExportService.cs, WPFApp/Applications/Export/ScheduleExportService.cs).  
  Status: **zaimplementowane**

- **Eksport grafiku i kontenera do SQL**  
  Repozytorium implementuje eksport danych domenowych jako skryptów SQL. Funkcja jest obsługiwana przez Web API i przez klienta WPF (GF3.WebApi/Controllers/ExportsController.cs, BusinessLogicLayer/Services/GraphExportService.cs, WPFApp/Applications/Export/ScheduleExportService.cs).  
  Status: **zaimplementowane**

- **Eksport CSV-like dla grafiku**  
  W warstwie biznesowej istnieje metoda eksportu CSV-owego, ale nie znaleziono jej wystawienia przez kontroler ani użycia w webowym UI. Wymaga potwierdzenia, czy to kod pomocniczy, czy nieużywana funkcja (BusinessLogicLayer/Services/GraphExportService.cs, GF3.WebApi/Controllers/ExportsController.cs).  
  Status: **niejednoznaczne**

#### Administracja i diagnostyka
- **Health check backendu**  
  Endpoint `/api/health` zwraca status aplikacji i informację o możliwości połączenia z bazą. W repozytorium istnieje także klient frontendowy dla tego endpointu, ale brak gotowej strony webowej, która z niego korzysta (GF3.WebApi/Controllers/HealthController.cs, FrontEnd/src/entities/health/api/healthApi.ts, FrontEnd/src/entities/health/api/queries.ts).  
  Status: **zaimplementowane częściowo**

- **Administracja bazą przez API**  
  Web API udostępnia administracyjne operacje na SQLite: metadane, hash, zapytania tylko do odczytu, ograniczone komendy modyfikujące oraz import pliku SQL. Funkcja ma własne zabezpieczenia, ale nie ma gotowego webowego ekranu użytkowego (GF3.WebApi/Controllers/AdminDbController.cs, GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs, BusinessLogicLayer/Services/AdminDbService.cs, FrontEnd/src/pages/database/ui/DataBasePage.tsx).  
  Status: **zaimplementowane częściowo**

- **Administracja bazą w kliencie desktopowym**  
  Klient WPF zawiera pełny ekran wykonania SQL, importu skryptu, przeglądu metadanych bazy i prezentacji wyników zapytań. To najbardziej kompletna implementacja funkcji administracyjnych w repozytorium (WPFApp/ViewModel/Database/DatabaseViewModel.cs, WPFApp/View/DatabaseView.xaml).  
  Status: **zaimplementowane**

#### Dashboard i informacje
- **Dashboard bieżących danych operacyjnych**  
  W kliencie WPF dostępny jest dashboard z aktualnym czasem, aktywnymi grafikami, statystykami miesiąca i widokiem osób pracujących danego dnia. Analogiczna strona webowa jest tylko stubem (WPFApp/ViewModel/Home/HomeViewModel.cs, FrontEnd/src/pages/home/ui/HomePage.tsx).  
  Status: **zaimplementowane częściowo**

- **Ekran informacji o systemie**  
  W repozytorium istnieją widoki `Information` zarówno w webie, jak i w WPF, ale ich treść jest placeholderowa lub pozbawiona logiki domenowej (FrontEnd/src/pages/information/ui/InformationPage.tsx, WPFApp/ViewModel/Information/InformationViewModel.cs, WPFApp/View/Information/InformationView.xaml).  
  Status: **zaimplementowane częściowo**

### 5.3. Mapowanie funkcjonalności frontend/backend
| Funkcjonalność | Frontend web | Klient WPF | Backend / warstwa usług | Status kompletności |
| --- | --- | --- | --- | --- |
| Pracownicy | listy, profil, edycja, usuwanie | list/edit/profile | API + BLL + DAL | kompletne end-to-end |
| Sklepy | listy, profil, edycja, usuwanie | list/edit/profile | API + BLL + DAL | kompletne end-to-end |
| Grupy dostępności | lista, profil, edytor macierzy | lista, profil, edytor | API + BLL + DAL | kompletne end-to-end |
| Bindy dostępności | wykorzystywane w edytorach | wykorzystywane w edytorze | API + BLL + DAL | kompletne end-to-end |
| Kontenery | lista, profil, edycja | lista, profil, edycja | API + BLL + DAL | kompletne end-to-end |
| Grafiki | profil, edycja, generowanie, eksport | profil, edycja, generowanie, eksport | API + BLL + DAL | kompletne end-to-end |
| Presety grafików | listowanie, tworzenie, wybór | brak jednoznacznego potwierdzenia pełnego odpowiednika | API + BLL + DAL | częściowe |
| Health | klient API istnieje, brak gotowego ekranu | brak potwierdzonego ekranu | API | częściowe |
| Admin DB | klient API istnieje, ekran web `TODO` | pełny ekran bazy | API + BLL + DAL | częściowe w webie, pełne w WPF |
| Dashboard home | ekran `TODO` | gotowy dashboard | BLL + DAL bez API | częściowe na poziomie całego projektu |
| Information | ekran `TODO` | ekran placeholderowy | brak logiki domenowej | częściowe |

## 6. Przypadki użycia

### 6.1. Aktorzy systemu
- **Planista / manager grafiku**: użytkownik tworzący grafiki, przypisujący pracowników i uruchamiający generator. Nazwa „manager” pojawia się w dołączonym artefakcie `p1.pptx`, a zachowania wynikają z modułów UI i serwisów harmonogramowania (C:\Users\Oleg\Desktop\p1.pptx, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).
- **Operator danych kadrowych**: użytkownik utrzymujący kartoteki pracowników, sklepów i grup dostępności (GF3.WebApi/Controllers/EmployeesController.cs, GF3.WebApi/Controllers/ShopsController.cs, GF3.WebApi/Controllers/AvailabilityGroupsController.cs).
- **Administrator techniczny bazy**: użytkownik wykonujący zapytania administracyjne, importy SQL i diagnostykę lokalnej bazy (GF3.WebApi/Controllers/AdminDbController.cs, WPFApp/ViewModel/Database/DatabaseViewModel.cs).
- **Proces generatora grafiku**: techniczny aktor wykonujący automatyczne rozkładanie slotów zmian na podstawie danych wejściowych (BusinessLogicLayer/Generators/ScheduleGenerator.cs).

### 6.2. Przypadki użycia

#### Przypadek użycia: Utworzenie i zapisanie pracownika
- Aktor: operator danych kadrowych
- Warunki wejściowe: uruchomiona aplikacja webowa lub WPF; dostępna baza SQLite; unikalna kombinacja imienia i nazwiska (FrontEnd/src/pages/employee-edit/ui/EmployeeEditPage.tsx, BusinessLogicLayer/Services/EmployeeService.cs)
- Scenariusz główny:
  1. Użytkownik otwiera formularz pracownika.
  2. Wprowadza imię, nazwisko oraz opcjonalnie telefon i e-mail.
  3. Formularz wysyła żądanie do API lub fasady WPF.
  4. Serwis waliduje wymagane pola i duplikaty.
  5. Rekord zostaje zapisany i wraca do listy/profilu.
- Scenariusze alternatywne:
  - walidacja e-maila lub brak wymaganych pól kończy zapis błędem walidacji
  - duplikat pełnego imienia i nazwiska kończy zapis błędem domenowym
- Kryteria akceptacji: pracownik jest widoczny na liście i możliwy do odczytu po identyfikatorze
- Odniesienia: `GF3.WebApi/Controllers/EmployeesController.cs`, `GF3.WebApi/Contracts/Employees/CreateEmployeeRequest.cs`, `BusinessLogicLayer/Services/EmployeeService.cs`, `FrontEnd/src/entities/employees/api/queries.ts`, `WPFApp/ViewModel/Employee/EmployeeViewModel.cs`

#### Przypadek użycia: Zdefiniowanie grupy dostępności
- Aktor: planista / manager grafiku
- Warunki wejściowe: istnieją pracownicy; użytkownik ma otwarty moduł dostępności (BusinessLogicLayer/Services/AvailabilityGroupService.cs, FrontEnd/src/pages/availability-edit/ui/AvailabilityEditPage.tsx)
- Scenariusz główny:
  1. Użytkownik tworzy grupę dostępności dla wskazanego miesiąca i roku.
  2. Dodaje pracowników do grupy.
  3. Uzupełnia macierz dni jako `ANY`, `NONE` lub interwały godzinowe.
  4. Zapis powoduje synchronizację grupy, członków i slotów.
- Scenariusze alternatywne:
  - niepoprawny format interwału lub konflikt danych powoduje komunikat walidacyjny
  - brak grupy lub członka skutkuje błędem `404`
- Kryteria akceptacji: grupa i jej pozycje są widoczne w profilu oraz możliwe do powiązania z grafikiem
- Odniesienia: `GF3.WebApi/Controllers/AvailabilityGroupsController.cs`, `BusinessLogicLayer/Services/AvailabilityGroupService.cs`, `FrontEnd/src/entities/availability-groups/model/matrix.ts`, `FrontEnd/src/pages/availability-edit/ui/AvailabilityEditPage.tsx`

#### Przypadek użycia: Utworzenie grafiku w kontenerze
- Aktor: planista / manager grafiku
- Warunki wejściowe: istnieje kontener, sklep oraz opcjonalnie grupa dostępności (GF3.WebApi/Contracts/Containers/Graphs/CreateGraphRequest.cs, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx)
- Scenariusz główny:
  1. Użytkownik otwiera kontener i inicjuje nowy grafik.
  2. Uzupełnia parametry planowania: sklep, nazwa, rok, miesiąc, zmiany i limity.
  3. Dodaje pracowników i zapisuje grafik.
  4. System tworzy rekord grafiku oraz powiązania pracowników/slotów.
- Scenariusze alternatywne:
  - błędne dane zakresowe powodują błąd walidacji DTO
  - brak kontenera lub sklepu powoduje błąd domenowy
- Kryteria akceptacji: grafik jest widoczny na liście grafików kontenera i otwiera się w profilu/edytorze
- Odniesienia: `GF3.WebApi/Controllers/ContainersController.cs`, `BusinessLogicLayer/Services/ContainerService.cs`, `FrontEnd/src/entities/containers/api/queries.ts`, `WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs`

#### Przypadek użycia: Automatyczne wygenerowanie grafiku
- Aktor: planista / manager grafiku
- Warunki wejściowe: istnieją pracownicy przypisani do grafiku; opcjonalnie przypisana grupa dostępności; grafik ma ustawione limity i godziny zmian (BusinessLogicLayer/Generators/ScheduleGenerator.cs, GF3.WebApi/Contracts/Containers/Graphs/GenerateGraphRequest.cs)
- Scenariusz główny:
  1. Użytkownik wybiera istniejący grafik.
  2. Uruchamia generowanie z parametrami `overwrite` / `dryRun`.
  3. Serwis ładuje pracowników, dostępność i istniejące sloty.
  4. Generator oblicza przydziały i zapisuje lub zwraca wynik.
- Scenariusze alternatywne:
  - `dryRun=true` zwraca wynik bez zapisu do bazy
  - konflikt walidacyjny lub brak danych wejściowych kończy operację błędem
- Kryteria akceptacji: sloty grafiku są zapisane albo zwrócone w odpowiedzi i widoczne w UI
- Odniesienia: `GF3.WebApi/Controllers/ContainersController.cs`, `BusinessLogicLayer/Services/ContainerService.cs`, `BusinessLogicLayer/Generators/ScheduleGenerator.cs`, `FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx`

#### Przypadek użycia: Eksport grafiku do pliku
- Aktor: planista / manager grafiku
- Warunki wejściowe: istnieje zapisany grafik lub kontener (GF3.WebApi/Controllers/ExportsController.cs, WPFApp/Applications/Export/ScheduleExportService.cs)
- Scenariusz główny:
  1. Użytkownik wybiera eksport XLSX albo SQL.
  2. System pobiera dane grafiku/kontenera.
  3. Generator eksportu buduje plik na podstawie modeli domenowych i szablonu.
  4. Użytkownik pobiera plik lub zapisuje go lokalnie.
- Scenariusze alternatywne:
  - brak szablonu Excel kończy operację błędem pliku
  - nieistniejący grafik lub kontener kończy operację `404`
- Kryteria akceptacji: plik jest zwracany jako odpowiedź HTTP albo generowany przez klienta WPF
- Odniesienia: `GF3.WebApi/Controllers/ExportsController.cs`, `BusinessLogicLayer/Services/GraphTemplateExportService.cs`, `BusinessLogicLayer/Services/GraphExportService.cs`, `WPFApp/Applications/Export/ScheduleExportService.cs`

#### Przypadek użycia: Administracja lokalną bazą danych
- Aktor: administrator techniczny bazy
- Warunki wejściowe: dla API wymagane jest localhost i poprawny `X-Admin-Token`; w WPF dostępny jest ekran `Database` (GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs, WPFApp/ViewModel/Database/DatabaseViewModel.cs)
- Scenariusz główny:
  1. Administrator otwiera moduł bazy.
  2. Przegląda metadane, wykonuje zapytanie albo ładuje skrypt SQL.
  3. System waliduje ograniczenia komendy i wykonuje operację.
  4. Po powodzeniu odświeżane są informacje o bazie.
- Scenariusze alternatywne:
  - komendy spoza dozwolonego zakresu są odrzucane
  - brak tokenu lub wyłączona flaga admin tools kończy dostęp błędem `403/404`
- Kryteria akceptacji: wynik zapytania albo komunikat wykonania jest widoczny użytkownikowi
- Odniesienia: `GF3.WebApi/Controllers/AdminDbController.cs`, `BusinessLogicLayer/Services/AdminDbService.cs`, `DataAccessLayer/Administration/SqliteAdminService.cs`, `WPFApp/View/DatabaseView.xaml`

## 7. Specyfikacja niefunkcjonalna

### Wydajność
- Nie ustalono mierzalnych wymagań wydajnościowych ani limitów czasów odpowiedzi na podstawie repozytorium.
- Pośrednie optymalizacje techniczne są obecne: `staleTime=30s` w webowym query providerze, wirtualizacja niektórych siatek danych w WPF oraz semafor ograniczający równoległe generowanie jednego grafiku (FrontEnd/src/app/providers/QueryProvider.tsx, WPFApp/View/Container/ContainerScheduleEditView.xaml.cs, BusinessLogicLayer/Services/ContainerService.cs).
- Brak benchmarków, testów obciążeniowych i telemetrycznych danych wydajności.

### Dostępność
- Backend udostępnia endpoint health check oraz uruchamia migracje automatycznie przy starcie (GF3.WebApi/Controllers/HealthController.cs, GF3.WebApi/Program.cs).
- Brak potwierdzonej konfiguracji HA, replikacji, wieloinstancyjności, load balancingu lub automatycznego restartu usług.

### Skalowalność
- Zastosowano pojedynczą bazę SQLite i lokalne pliki szablonów, co wskazuje na architekturę lekką i raczej pojedynczo-instancyjną (DataAccessLayer/AppDbContext.cs, BusinessLogicLayer/Services/Excel/ExcelTemplateLocator.cs).
- Nie potwierdzono cache rozproszonego, brokerów kolejek, partycjonowania danych ani oddzielnego storage dla plików.

### Bezpieczeństwo
- Potwierdzono walidację wejścia przez DataAnnotations i dodatkową walidację domenową, middleware wyjątków oraz deweloperski CORS ograniczony do `localhost:5173` w środowisku Development (GF3.WebApi/Program.cs, GF3.WebApi/Contracts/*, GF3.WebApi/Middleware/ApiExceptionMiddleware.cs).
- Nie potwierdzono mechanizmów uwierzytelniania użytkownika końcowego, autoryzacji, ról, JWT, cookies, SSO ani rate limiting.
- Admin tools są ograniczone do loopbacka i nagłówka `X-Admin-Token`, ale repozytorium przechowuje przykładowy token i ma włączone `AllowWriteSql`, co wymaga zmiany przed użyciem produkcyjnym (GF3.WebApi/appsettings.json, GF3.WebApi/Middleware/AdminToolsGuardMiddleware.cs).

### Zgodność z politykami danych
- Nie ustalono na podstawie repozytorium polityk retencji danych, anonimizacji, RODO, backupu, archiwizacji ani szyfrowania danych w spoczynku.
- Potwierdzono jedynie fizyczne przechowywanie danych w lokalnym pliku SQLite w katalogu użytkownika (WPFApp/Applications/Configuration/DatabasePathProvider.cs, GF3.WebApi/Program.cs).

### UI/UX i dostępność
- Web UI zawiera liczne media queries, role ARIA, `aria-invalid`, `aria-live`, dialogi modalne i komponenty błędów, co potwierdza podstawowe działania pro-dostępnościowe (FrontEnd/src/shared/ui, FrontEnd/src/entities/availability-groups/ui, FrontEnd/src/entities/containers/ui).
- Nie znaleziono formalnych testów dostępności, deklaracji WCAG, matrixa przeglądarek ani kryteriów UX.

### Monitoring i utrzymanie
- Potwierdzono standardowe logowanie ASP.NET Core i dokumentację Swagger (GF3.WebApi/appsettings.json, GF3.WebApi/Program.cs).
- Brak potwierdzonej integracji z Application Insights, OpenTelemetry, Sentry, Serilog/Seq lub innym zewnętrznym monitoringiem.
- Repozytorium zawiera skrypt architektoniczny `tools/check-architecture.sh`, ale brak pełnego pipeline CI/CD (tools/check-architecture.sh, brak plików w `.github/workflows`).

### Wdrożenie i eksploatacja
- Potwierdzono instrukcję uruchomienia lokalnego dla backendu i frontendu webowego oraz automatyczne migracje bazy podczas startu Web API (docs/run-local.md, GF3.WebApi/Program.cs).
- Nie wykryto plików Docker, `docker-compose`, Helm, Terraform, ARM/Bicep ani gotowych pipeline'ów deploymentowych.
- Weryfikacja kompilacyjna repozytorium ujawniła błąd solution spowodowany brakującym projektem `ArchitectureTests`; weryfikacja buildu frontendu webowego nie została jednoznacznie ukończona z powodu ograniczenia środowiska uruchomieniowego Node w analizowanym środowisku, a nie z powodu potwierdzonego błędu kodu (GF3.sln, brak `ArchitectureTests/ArchitectureTests.csproj`).

## 8. Ryzyka, luki i obszary wymagające doprecyzowania
- Brak projektu `ArchitectureTests/ArchitectureTests.csproj` przy jednoczesnym wpisie do `GF3.sln` powoduje niespójność repozytorium i błąd buildu rozwiązania (GF3.sln).
- Brak potwierdzonych testów automatycznych backendu, frontendu i WPF zwiększa ryzyko regresji.
- Webowe strony `Home`, `Information` i `DataBase` są stubami `TODO`, mimo że część powiązanego backendu już istnieje (FrontEnd/src/pages/home/ui/HomePage.tsx, FrontEnd/src/pages/information/ui/InformationPage.tsx, FrontEnd/src/pages/database/ui/DataBasePage.tsx).
- Webowa obsługa importu SQL przez `admin-db` jest niespójna z ogólnym klientem HTTP i wymaga poprawki `FormData` (FrontEnd/src/shared/api/httpClient.ts, FrontEnd/src/entities/admin-db/api/adminDbApi.ts).
- W projekcie nie ma uwierzytelniania użytkownika końcowego, a admin tools są skonfigurowane w sposób wyraźnie niedomyślony do produkcji (`Enabled=true`, `AllowWriteSql=true`, token w repozytorium) (GF3.WebApi/appsettings.json).
- Dokumentacja jest częściowo niespójna z kodem: `ApiSamples.http` ma host i komentarze niezgodne z aktualnymi endpointami eksportu, a `FrontEnd/README.md` pozostaje README szablonu Vite (GF3.WebApi/ApiSamples.http, FrontEnd/README.md, docs/run-local.md).
- Nazewnictwo `graph` vs `schedule` zwiększa ryzyko błędnych interpretacji funkcji i modeli między warstwami (BusinessLogicLayer/Contracts/Models/Models.cs, GF3.WebApi/Contracts/Containers/Graphs/GraphDto.cs, FrontEnd/src/entities/containers/model/types.ts).
- Moduł `Information` w WPF i webie jest placeholderowy, a `HomeTestPage` wygląda na kod pomocniczy lub martwy (WPFApp/ViewModel/Information/InformationViewModel.cs, WPFApp/View/Information/InformationView.xaml, FrontEnd/src/pages/hometest/ui/HomeTestPage.tsx).
- Funkcja ręcznych kolumn w webowym edytorze jest zapisywana w polu `note`, co utrudnia walidację, integracje i przyszłe utrzymanie (FrontEnd/src/entities/containers/model/graphNote.ts, GF3.WebApi/Contracts/Containers/Graphs/CreateGraphRequest.cs).
- Brak potwierdzonego procesu backupu, odtwarzania i wdrożenia produkcyjnego wymaga doprecyzowania przed użyciem systemu operacyjnie.

## 9. Wycena projektu

### 9.1. Założenia do wyceny
Poniższa wycena jest ostrożną estymacją nakładu pracy wynikającą wyłącznie z analizy faktycznie istniejącego repozytorium. Nie jest to wycena handlowa oparta o pełny warsztat biznesowy, backlog ani historię projektu. Zakres nie obejmuje funkcji, których nie da się potwierdzić w kodzie (GF3.sln, GF3.WebApi, FrontEnd, WPFApp).

Przyjęto:

- `1 MD = 8 roboczogodzin`
- osobno oszacowano wartość już istniejącego zakresu oraz koszt domknięcia repozytorium do bardziej stabilnego wydania
- szerokość widełek wynika z braku testów, niejednoznaczności części funkcji pomocniczych i braku potwierdzonego celu produkcyjnego dla obu klientów UI

### 9.2. Szacowany nakład pracy
| Zakres | Szacowany nakład | Uzasadnienie |
| --- | --- | --- |
| Zakres już zaimplementowany: warstwa danych, migracje, serwisy biznesowe, generator, eksporty | 40-65 MD | rozbudowany model danych, generator harmonogramów, eksporty XLSX/SQL, administracja SQLite, migracje (DataAccessLayer/AppDbContext.cs, BusinessLogicLayer/Generators/ScheduleGenerator.cs, BusinessLogicLayer/Services/*) |
| Zakres już zaimplementowany: Web API | 20-35 MD | pełny zestaw kontrolerów CRUD, endpointy zagnieżdżone, middleware wyjątków, admin tools, health, Swagger (GF3.WebApi/Controllers/*, GF3.WebApi/Program.cs) |
| Zakres już zaimplementowany: frontend webowy React | 25-45 MD | rozbudowany moduł kontenerów/grafików, CRUD pracowników/sklepów, dostępność, eksporty, własny router i query layer (FrontEnd/src/*) |
| Zakres już zaimplementowany: klient desktopowy WPF | 35-60 MD | osobny dashboard, moduły CRUD, baza danych, edycja grafików, eksporty, MVVM, lokalna administracja bazy (WPFApp/ViewModel/*, WPFApp/View/*) |
| **Łącznie wartość już zaimplementowanego zakresu** | **120-205 MD** | estymata odtworzenia podobnego zakresu funkcjonalnego od zera |
| Zakres brakujący / do domknięcia | 10-25 MD | ukończenie webowych stron `Home`, `Information`, `DataBase`, naprawa uploadu `admin-db`, porządkowanie dokumentacji i martwych elementów (FrontEnd/src/pages/home/ui/HomePage.tsx, FrontEnd/src/pages/database/ui/DataBasePage.tsx, FrontEnd/src/shared/api/httpClient.ts) |
| Testy / hardening / uporządkowanie solution | 12-28 MD | odtworzenie lub usunięcie brakującego `ArchitectureTests`, testy krytycznych ścieżek, przegląd konfiguracji bezpieczeństwa i buildów (GF3.sln, brak `ArchitectureTests/ArchitectureTests.csproj`, GF3.WebApi/appsettings.json) |
| Wdrożenie / przygotowanie produkcyjne | 10-20 MD | decyzje o modelu wdrożenia, zabezpieczeniach, backupie, obsłudze środowisk i monitoringu; brak gotowych artefaktów deploymentowych w repozytorium |
| Dokumentacja / stabilizacja | 5-10 MD | ujednolicenie dokumentacji technicznej i doprecyzowanie rozbieżności kod-dokumentacja |
| **Łącznie do domknięcia obecnego repozytorium** | **37-83 MD** | estymata ostrożna dla doprowadzenia istniejącego kodu do bardziej uporządkowanego stanu |

### 9.3. Szacowany harmonogram
- **Odtworzenie obecnego zakresu od zera**: około **6-10 miesięcy** pracy jednej osoby technicznej o szerokim profilu full-stack/.NET/WPF lub odpowiednio krócej przy zespole 2-3 osób. Widełki są szerokie ze względu na dwa różne klienty UI i rozbudowaną logikę generatora.
- **Domknięcie aktualnego repozytorium**: około **6-12 tygodni** dla 1-2 osób, jeśli celem jest uporządkowanie solution, testy krytycznych ścieżek, domknięcie stubów webowych i przygotowanie bezpieczniejszej konfiguracji.
- Brak jednoznacznego potwierdzenia biznesowego, czy oba klienty UI mają być rozwijane równolegle. To istotnie wpływa na harmonogram.

### 9.4. Szacowana kwota lub sposób kalkulacji
Repozytorium nie zawiera stawek, dlatego rekomendowany jest model kalkulacji:

- **kwota zakresu już zaimplementowanego** = `120-205 MD × stawka za 1 MD`
- **kwota domknięcia obecnego repozytorium** = `37-83 MD × stawka za 1 MD`
- równoważnie: `liczba godzin × stawka godzinowa`, gdzie `godziny = MD × 8`

Przykładowa wycena kwotowa wymaga osobnej decyzji o stawce i nie powinna być arbitralnie przyjmowana bez kontekstu kontraktowego.

### 9.5. Warunki widełek
Na szerokość widełek wpływają przede wszystkim:

- decyzja, czy utrzymywane mają być oba klienty UI: webowy i WPF
- oczekiwany poziom testów automatycznych i ewentualne odtworzenie projektu `ArchitectureTests`
- decyzja o docelowym modelu bezpieczeństwa, bo obecnie brak uwierzytelniania użytkownika końcowego
- zakres dokończenia modułów placeholderowych `Home`, `Information`, `DataBase`
- to, czy funkcje pomocnicze zapisane pośrednio w `note` mają dostać docelowy model danych
- potrzeba wdrożenia produkcyjnego, backupu, monitoringu i CI/CD, których repozytorium obecnie nie dostarcza

## 10. Podsumowanie
Na podstawie repozytorium można z dużą pewnością potwierdzić, że projekt implementuje system zarządzania grafikami pracy z automatycznym generowaniem harmonogramów, obsługą dostępności pracowników, eksportami XLSX/SQL oraz dwoma klientami UI: webowym i desktopowym. Solidnie zaimplementowane są przede wszystkim modele danych, logika generatora, CRUD głównych encji, edycja grafików i eksporty (BusinessLogicLayer/Generators/ScheduleGenerator.cs, DataAccessLayer/AppDbContext.cs, GF3.WebApi/Controllers, FrontEnd/src/pages/container-graph-edit/ui/ContainerGraphEditPage.tsx, WPFApp/ViewModel/Container/Edit/ContainerViewModel.cs).

Częściowo zaimplementowane lub niedomknięte są: webowe strony `Home`, `Information`, `DataBase`, webowa warstwa administracji bazą, presety grafików w pełnym cyklu życia, moduł informacji oraz niektóre artefakty dokumentacyjne. Dodatkowym ryzykiem jest brak testów i brakujący projekt `ArchitectureTests`, który psuje build solution (FrontEnd/src/pages/home/ui/HomePage.tsx, FrontEnd/src/pages/database/ui/DataBasePage.tsx, WPFApp/ViewModel/Information/InformationViewModel.cs, GF3.sln).

Na podstawie samego repozytorium nie udało się jednoznacznie ustalić: docelowego modelu wdrożenia produkcyjnego, polityk bezpieczeństwa użytkowników końcowych, polityk danych/RODO, wymagań wydajnościowych, strategii backupu oraz tego, czy oba klienty UI mają być rozwijane równolegle jako równoprawne kanały dostępu do systemu.
