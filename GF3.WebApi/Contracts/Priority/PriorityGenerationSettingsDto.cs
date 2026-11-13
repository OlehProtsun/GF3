namespace WebApi.Contracts.Priority;

public sealed record PriorityGenerationSettingsDto(int ShiftHours, bool WeekendsOff, bool RespectAvailability);
