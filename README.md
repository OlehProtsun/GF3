# GF3

GF3 is a scheduling and workforce planning app with a .NET backend, SQLite data layer, launcher, tests, and React/Vite frontend.

## Project Layout

- `BusinessLogicLayer/` - scheduling, availability, export, auth, and employee workflow services.
- `DataAccessLayer/` - EF Core models, migrations, repositories, and SQLite administration.
- `GF3.WebApi/` - ASP.NET API, auth, controllers, SignalR realtime services, and frontend hosting.
- `GF3.Launcher/` - Windows launcher for the local app experience.
- `GF3.Tests/` - xUnit coverage for business logic, data access, controllers, middleware, and launcher behavior.
- `FrontEnd/` - React + TypeScript + Vite client.
- `docs/` - API and local run notes.
- `scripts/` - local publishing helpers.
- `tools/` - developer utilities such as the algorithm stress harness.

## Requirements

- .NET 10 SDK
- Node.js 20+
- npm

## Local Development

```powershell
dotnet restore GF3.sln
dotnet build GF3.sln
cd FrontEnd
npm install
npm run dev
```

Run the API from the repository root:

```powershell
dotnet run --project GF3.WebApi/WebApi.csproj
```

In development, the API proxies frontend requests to `http://localhost:5173`. A Debug build of `GF3.WebApi` restores `FrontEnd/node_modules` automatically when the folder is missing, then SpaProxy runs `npm run dev`.

## Configuration

Tracked `appsettings*.json` files should stay safe for GitHub. Put private values in environment variables or local ignored config files. Useful environment variable names:

- `ConnectionStrings__Default`
- `Jwt__SigningKey`
- `Smtp__Host`
- `Smtp__UserName`
- `Smtp__Password`
- `Smtp__FromAddress`

## Verification

```powershell
dotnet test GF3.sln
cd FrontEnd
npm run lint
npm run build
```
