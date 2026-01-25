# Schedule generation save validation

## Root cause
Opening the edit screen assumes `ScheduleModel.AvailabilityGroupId` is set. When a schedule is saved without generation, `AvailabilityGroupId` is `null`, so the edit path casts the nullable value to `int`, which throws `InvalidOperationException` (`Nullable object must have a value`) and stops the edit flow. The observed stack is:

```
InvalidOperationException: Nullable object must have a value.
   at WPFApp.ViewModel.Container.ContainerViewModel.EditSelectedScheduleAsync(...)
```

## Fix locations
- **UI validation**: `ContainerViewModel.SaveScheduleAsync` now blocks saving schedules that do not contain generated data (availability group + slots) and shows a user-facing message.
- **Edit guard**: `ContainerViewModel.EditSelectedScheduleAsync` (and multi-open) now detect missing generated data and show a message instead of throwing.
- **Service validation**: `ScheduleService.SaveWithDetailsAsync` enforces the same rule so invalid schedules cannot be persisted from other callers.

## Generated content rule
A schedule is considered generated only when it has:
- `AvailabilityGroupId` set, and
- at least one `ScheduleSlotModel`.
