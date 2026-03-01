# Frontend ↔ WebApi integration guide

## Chosen typing strategy

Swagger/OpenAPI is enabled at `/swagger/v1/swagger.json`, but package installation from npm registry is blocked in this environment.
So the frontend uses **manual DTO typing** with strict colocated files:

- `entities/<entity>/api/dto.ts` → wire contracts (API DTO)
- `entities/<entity>/model/types.ts` → domain model/input types
- `entities/<entity>/api/<entity>Api.ts` → thin HTTP layer
- `entities/<entity>/api/queries.ts` → query/mutation hooks + invalidation

When registry access is available, you can switch to generated types by adding `openapi-typescript` and generating `src/shared/api/generated.ts` from `/swagger/v1/swagger.json`.

## API conventions

- Shared HTTP client: `src/shared/api/httpClient.ts`
- Base URL source:
  - default: `/api` (works with Vite proxy)
  - optional override: `VITE_API_BASE_URL`
- Query key conventions: `src/shared/api/queryKeys.ts`
  - e.g. `['employees','list']`, `['shops','byId',id]`

## How to add a new endpoint

1. Add DTOs to `entities/<entity>/api/dto.ts`.
2. Add HTTP function to `entities/<entity>/api/<entity>Api.ts`.
3. Add hook to `entities/<entity>/api/queries.ts`:
   - query for read
   - mutation for write
   - invalidate related keys on success
4. Use hook from UI in `features/*` or `pages/*`.

## Current integrated entities

- Employees:
  - GET `/api/employees`
  - POST `/api/employees`
  - DELETE `/api/employees/{id}`
- Shops:
  - GET `/api/shops`
  - POST `/api/shops`
  - DELETE `/api/shops/{id}`
