# GF3 FrontEnd

React + TypeScript + Vite client for GF3.

## Commands

```powershell
npm install
npm run dev
npm run build
npm run lint
```

The ASP.NET API proxies development SPA traffic to `http://localhost:5173`.
When debugging `GF3.WebApi`, the backend project restores `node_modules` automatically if it is missing, then SpaProxy starts this Vite dev server.
