# Frontend API usage

## How to add a new endpoint

1. Add DTO in `src/entities/<entity>/api/dto.ts`.
2. Add thin client function in `src/entities/<entity>/api/<entity>Api.ts`.
3. Add React Query hook in `src/entities/<entity>/api/queries.ts`.
4. Add/update query key in `src/shared/api/queryKeys.ts`.
5. Export from `src/entities/<entity>/index.ts`.
6. Update `docs/api-map.md` and run `npm run build`.

## Error handling

- All calls use `request<T>()` from `src/shared/api/httpClient.ts`.
- Non-2xx responses throw `ApiError` with `status`, `traceId`, and `validationErrors`.
