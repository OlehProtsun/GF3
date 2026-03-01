# Run locally

## Source of truth (dev URLs/ports)

- WebApi profile (`GF3.WebApi/Properties/launchSettings.json`):
  - `https://localhost:54294`
  - `http://localhost:54295`
- Vite dev server (`FrontEnd/vite.config.ts`):
  - `http://localhost:5173` (fixed, `strictPort: true`)
- ASP.NET SPA Proxy (`GF3.WebApi/WebApi.csproj`):
  - `SpaProxyServerUrl = http://localhost:5173`
  - launch command: `npm run dev` in `..\FrontEnd`

## Prerequisites

1. .NET SDK (matching `net10.0` in `GF3.WebApi/WebApi.csproj`).
2. Node.js + npm.
3. Install frontend dependencies once:

```bash
cd FrontEnd
npm install
```

## Start dev environment (recommended)

Run only backend; it auto-starts Vite through SpaProxy:

```bash
dotnet run --project GF3.WebApi/WebApi.csproj
```

Expected result:

1. WebApi starts on `https://localhost:54294` / `http://localhost:54295`.
2. Vite starts automatically on `http://localhost:5173`.
3. Open `https://localhost:54294` and frontend is served via WebApi->Vite proxy.

## API from frontend (zero CORS config)

Frontend HTTP client uses relative API base (`/api`) by default, so requests are same-origin when app is opened via WebApi URL.

- Employees list request: `GET /api/employees`
- Smoke page: `https://localhost:54294/employees`

## Smoke check

1. Open `https://localhost:54294/employees`.
2. Confirm page shows one of states:
   - Loading employees...
   - No employees found.
   - Employees table.
3. In browser Network, verify `GET /api/employees` returns JSON.
