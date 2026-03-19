# API contracts maintenance (manual DTO mode)

Swagger UI exists, but automatic TS generation is currently not enabled in this repo runtime.
Therefore contracts are maintained manually with strict conventions.

## Conventions

1. Every endpoint must have DTO definitions in `src/entities/<entity>/api/dto.ts` (or `model/types.ts` for non-domain utility entities).
2. Every endpoint must have a thin client function in `src/entities/<entity>/api/<entity>Api.ts`.
3. Every endpoint must have a React Query hook in `src/entities/<entity>/api/queries.ts`.
4. Every entity publishes only through `src/entities/<entity>/index.ts`.
5. Query keys are defined centrally in `src/shared/api/queryKeys.ts`.
6. All HTTP calls use `src/shared/api/httpClient.ts` for normalized `ApiError` parsing.

## Update workflow

1. Add or update backend endpoint/contract.
2. Update `docs/api-map.md`.
3. Add/update DTO type(s).
4. Add/update API client function.
5. Add/update query/mutation hook + invalidation.
6. Run `npm run build`.
