# Run locally

## Backend (WebApi)

1. Run project `GF3.WebApi` using development profile.
2. Default URLs are:
   - `https://localhost:54294`
   - `http://localhost:54295`

## Frontend (Vite)

1. `cd FrontEnd`
2. `npm install` (if dependencies are available in your environment)
3. `npm run dev`

Frontend proxy sends `/api/*` to `https://localhost:54294` (configured in `vite.config.ts`).

## Smoke page

- Open `http://localhost:5173/employees`
- You should see a simple employees table with loading/error/empty states.
