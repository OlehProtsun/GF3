# Test Matrix Summary

## MainPresenter
| Event | Scenario | Expected |
| --- | --- | --- |
| ShowEmployeeView | Normal | `SetActivePage(Employee)`, create form once, `Show()` called, reused on subsequent navigation. |
| ShowEmployeeView | Disposed view | New instance created. |
| ShowEmployeeView | Canceled token | No view creation or show. |
| ShowAvailabilityView | Normal | `SetActivePage(Availability)`, show view. |
| ShowContainerView | Normal | `SetActivePage(Container)`, show view. |

## EmployeePresenter
| Event | Scenario | Expected |
| --- | --- | --- |
| Initialize | Service ok | `GetAllAsync`, binding source updated. |
| Search | Empty term | `GetAllAsync` called. |
| Search | Term provided | `GetByValueAsync` called. |
| Search | Cancellation/race | Stale result ignored; canceled operation does not overwrite list. |
| Add | N/A | Clear inputs/errors, `IsEdit=false`, set message, switch to edit. |
| Edit | Current item | Fields populated, `IsEdit=true`, cancel target set, switch to edit. |
| Save | Invalid | Validation errors set, no create/update. |
| Save | Create | `CreateAsync`, reload list, info message, switch back. |
| Save | Update | `UpdateAsync`, reload list, info message, switch back. |
| Delete | Confirm false | No delete call. |
| Delete | Confirm true | `DeleteAsync`, reload, info, list mode. |
| OpenProfile | Current item | Profile set, switch to profile. |
| Exception | Service throws | `ShowError`, no crash. |

## AvailabilityPresenter
| Event | Scenario | Expected |
| --- | --- | --- |
| Initialize | Service ok | Load groups, employees, binds. |
| Search | Empty term | `GetAllAsync` on group service. |
| Search | Term provided | `GetByValueAsync`. |
| Add | N/A | Clear inputs, reset matrix, switch to edit. |
| Edit | Current item | Load full model, populate matrix, switch to edit. |
| Save | Invalid group | Validation errors set. |
| Save | No selected employees | Error shown, no save. |
| Save | Valid | `SaveGroupAsync`, reload, info, switch list/profile. |
| Delete | Confirm true | `DeleteAsync`, reload, info. |
| OpenProfile | Current item | Load full, set profile, switch to profile. |
| Bind Upsert | Invalid | Error shown. |
| Bind Upsert | Create | `CreateAsync`, reload binds. |
| Bind Delete | Existing | `DeleteAsync`, reload binds. |
| Cancellation | Search canceled | No error shown. |

## ContainerPresenter
| Event | Scenario | Expected |
| --- | --- | --- |
| Initialize | Service ok | Load containers and availability groups. |
| Search | Term provided | `GetByValueAsync` on container service. |
| Add/Edit | N/A | Clear inputs/errors, switch to edit, set fields. |
| Save | Invalid | Validation errors, no save. |
| Save | Create | `CreateAsync`, reload, info, switch list. |
| Delete | Confirm true | `DeleteAsync`, reload, info, list mode. |
| OpenProfile | Current item | Set profile, load schedules. |
| Schedule Search | No container | Error shown. |
| Schedule Add | N/A | Load lookups, clear schedule, switch to edit. |
| Schedule Edit | Current item | Load detailed, populate, switch to edit. |
| Schedule Save | Invalid | Validation errors. |
| Schedule Save | Valid | `SaveWithDetailsAsync`, reload schedules, info, switch list. |
| Schedule Generate | Invalid | Validation errors and error message. |
| Schedule Generate | No groups | Error shown. |
| Schedule Generate | Valid | `GenerateAsync`, update employees/slots, info shown. |
| Exception | Service throws | `ShowError`.
