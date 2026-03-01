# API map (WebApi → Frontend integration)

Base path: `/api`

| Controller | Method | Route | Request | Response | Auth/Guard | Error codes | Frontend coverage |
|---|---|---|---|---|---|---|---|
| Employees | GET | `/api/employees` | - | `EmployeeDto[]` | none | 500 | ✅ `entities/employees/api/employeesApi.list` + `useEmployeesListQuery` |
| Employees | GET | `/api/employees/{id}` | path id | `EmployeeDto` | none | 404,500 | ✅ byId + hook |
| Employees | POST | `/api/employees` | `CreateEmployeeRequest` | `EmployeeDto` | none | 400,500 | ✅ create + mutation |
| Employees | PUT | `/api/employees/{id}` | `UpdateEmployeeRequest` | 204 | none | 400,404,500 | ✅ update + mutation |
| Employees | DELETE | `/api/employees/{id}` | path id | 204 | none | 400,404,500 | ✅ remove + mutation |
| Shops | GET | `/api/shops` | - | `ShopDto[]` | none | 500 | ✅ |
| Shops | GET | `/api/shops/{id}` | path id | `ShopDto` | none | 404,500 | ✅ |
| Shops | POST | `/api/shops` | `CreateShopRequest` | `ShopDto` | none | 400,500 | ✅ |
| Shops | PUT | `/api/shops/{id}` | `UpdateShopRequest` | 204 | none | 400,404,500 | ✅ |
| Shops | DELETE | `/api/shops/{id}` | path id | 204 | none | 404,500 | ✅ |
| Containers | GET | `/api/containers` | - | `ContainerDto[]` | none | 500 | ✅ |
| Containers | GET | `/api/containers/{id}` | path id | `ContainerDto` | none | 404,500 | ✅ |
| Containers | POST | `/api/containers` | `CreateContainerRequest` | `ContainerDto` | none | 400,500 | ✅ |
| Containers | PUT | `/api/containers/{id}` | `UpdateContainerRequest` | 204 | none | 400,404,500 | ✅ |
| Containers | DELETE | `/api/containers/{id}` | path id | 204 | none | 404,500 | ✅ |
| Containers.Graphs | GET | `/api/containers/{containerId}/graphs` | path containerId | `GraphDto[]` | none | 404,500 | ✅ |
| Containers.Graphs | GET | `/api/containers/{containerId}/graphs/{graphId}` | path ids | `GraphDto` | none | 404,500 | ✅ |
| Containers.Graphs | POST | `/api/containers/{containerId}/graphs` | `CreateGraphRequest` | `GraphDto` | none | 400,404,500 | ✅ |
| Containers.Graphs | PUT | `/api/containers/{containerId}/graphs/{graphId}` | `UpdateGraphRequest` | 204 | none | 400,404,500 | ✅ |
| Containers.Graphs | DELETE | `/api/containers/{containerId}/graphs/{graphId}` | path ids | 204 | none | 404,500 | ✅ |
| Containers.Graphs | POST | `/api/containers/{containerId}/graphs/{graphId}/generate` | `GenerateGraphRequest` | `GenerateGraphResponse` | none | 400,404,500 | ✅ |
| Containers.Slots | GET | `/api/containers/{containerId}/graphs/{graphId}/slots` | path ids | `GraphSlotDto[]` | none | 404,500 | ✅ |
| Containers.Slots | POST | `/api/containers/{containerId}/graphs/{graphId}/slots` | `CreateGraphSlotRequest` | `GraphSlotDto` | none | 400,404,500 | ✅ |
| Containers.Slots | PUT | `/api/containers/{containerId}/graphs/{graphId}/slots/{slotId}` | `UpdateGraphSlotRequest` | 204 | none | 400,404,500 | ✅ |
| Containers.Slots | DELETE | `/api/containers/{containerId}/graphs/{graphId}/slots/{slotId}` | path ids | 204 | none | 404,500 | ✅ |
| Containers.Employees | GET | `/api/containers/{containerId}/graphs/{graphId}/employees` | path ids | `GraphEmployeeDto[]` | none | 404,500 | ✅ |
| Containers.Employees | POST | `/api/containers/{containerId}/graphs/{graphId}/employees` | `AddGraphEmployeeRequest` | `GraphEmployeeDto` | none | 400,404,500 | ✅ |
| Containers.Employees | PUT | `/api/containers/{containerId}/graphs/{graphId}/employees/{graphEmployeeId}` | `UpdateGraphEmployeeRequest` | 204 | none | 400,404,500 | ✅ |
| Containers.Employees | DELETE | `/api/containers/{containerId}/graphs/{graphId}/employees/{graphEmployeeId}` | path ids | 204 | none | 404,500 | ✅ |
| Containers.CellStyles | GET | `/api/containers/{containerId}/graphs/{graphId}/cell-styles` | path ids | `GraphCellStyleDto[]` | none | 404,500 | ✅ |
| Containers.CellStyles | PUT | `/api/containers/{containerId}/graphs/{graphId}/cell-styles` | `UpsertGraphCellStyleRequest` | `GraphCellStyleDto` | none | 400,404,500 | ✅ |
| Containers.CellStyles | DELETE | `/api/containers/{containerId}/graphs/{graphId}/cell-styles/{styleId}` | path ids | 204 | none | 404,500 | ✅ |
| AvailabilityGroups | GET | `/api/availability-groups` | - | `AvailabilityGroupDto[]` | none | 500 | ✅ |
| AvailabilityGroups | GET | `/api/availability-groups/{id}` | path id | `AvailabilityGroupDto` | none | 404,500 | ✅ |
| AvailabilityGroups | GET | `/api/availability-groups/{id}/items` | path id | `AvailabilityGroupItemDto[]` | none | 404,500 | ✅ |
| AvailabilityGroups | POST | `/api/availability-groups` | `CreateAvailabilityGroupRequest` | `AvailabilityGroupDto` | none | 400,500 | ✅ |
| AvailabilityGroups | PUT | `/api/availability-groups/{id}` | `UpdateAvailabilityGroupRequest` | 204 | none | 400,404,500 | ✅ |
| AvailabilityGroups | DELETE | `/api/availability-groups/{id}` | path id | 204 | none | 404,500 | ✅ |
| AvailabilityGroups.Members | GET | `/api/availability-groups/{groupId}/members` | path groupId | `AvailabilityGroupMemberDto[]` | none | 404,500 | ✅ |
| AvailabilityGroups.Members | POST | `/api/availability-groups/{groupId}/members` | `CreateAvailabilityGroupMemberRequest` | `AvailabilityGroupMemberDto` | none | 400,404,500 | ✅ |
| AvailabilityGroups.Members | PUT | `/api/availability-groups/{groupId}/members/{memberId}` | `UpdateAvailabilityGroupMemberRequest` | 204 | none | 400,404,500 | ✅ |
| AvailabilityGroups.Members | DELETE | `/api/availability-groups/{groupId}/members/{memberId}` | path ids | 204 | none | 404,500 | ✅ |
| AvailabilityGroups.Slots | GET | `/api/availability-groups/{groupId}/slots` | path groupId | `AvailabilitySlotDto[]` | none | 404,500 | ✅ |
| AvailabilityGroups.Slots | POST | `/api/availability-groups/{groupId}/slots` | `CreateAvailabilitySlotRequest` | `AvailabilitySlotDto` | none | 400,404,500 | ✅ |
| AvailabilityGroups.Slots | PUT | `/api/availability-groups/{groupId}/slots/{slotId}` | `UpdateAvailabilitySlotRequest` | 204 | none | 400,404,500 | ✅ |
| AvailabilityGroups.Slots | DELETE | `/api/availability-groups/{groupId}/slots/{slotId}` | path ids | 204 | none | 404,500 | ✅ |
| Exports | GET | `/api/exports/containers/{containerId}/graphs/{graphId}/export/excel` | query includeStyles/includeEmployees | file (xlsx) | none | 404,500 | ✅ mutation |
| Exports | GET | `/api/exports/containers/{containerId}/graphs/{graphId}/export/sql` | query includeStyles/includeEmployees | file (sql) | none | 404,500 | ✅ mutation |
| Exports | GET | `/api/exports/containers/{containerId}/export/excel` | query includeStyles/includeEmployees | file (xlsx) | none | 404,500 | ✅ mutation |
| Exports | GET | `/api/exports/containers/{containerId}/export/sql` | query includeStyles/includeEmployees | file (sql) | none | 404,500 | ✅ mutation |
| AdminDb | GET | `/api/admin/db/metadata` | - | metadata object | loopback + `X-Admin-Token`, feature flag | 403,404,500 | ✅ query |
| AdminDb | GET | `/api/admin/db/hash` | - | `{hash:string}` | loopback + `X-Admin-Token`, feature flag | 403,404,500 | ✅ query |
| AdminDb | POST | `/api/admin/db/query` | `{sql}` | query result object | loopback + `X-Admin-Token`, feature flag | 400,403,404,500 | ✅ mutation |
| AdminDb | POST | `/api/admin/db/execute` | `{sql}` | `{affectedRows:number}` | loopback + `X-Admin-Token`, feature flag + write flag | 400,403,404,500 | ✅ mutation |
| AdminDb | POST | `/api/admin/db/import` | multipart `file` | import result object | loopback + `X-Admin-Token`, feature flag + write flag | 400,403,404,500 | ✅ mutation |
| Health | GET | `/api/health` | - | `{status,canConnect}` | none | 500 | ✅ query |

## Integration status

- Already integrated before this iteration: employees, shops (partial UI).
- Added in this iteration: containers (all nested endpoints), availability-groups (all nested endpoints), exports, admin-db, health.
- Final status: **all WebApi endpoints are represented in frontend API functions + hooks + DTO types.**
