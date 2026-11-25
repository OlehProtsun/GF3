namespace WebApi.Contracts.EmployeeLaunches;

public sealed record EmployeeLaunchUsageDto(int EmployeeId, string EmployeeName, string? Username,
    bool HasLoginAccount, bool IsOnline, DateTimeOffset? LastLoginAtUtc, DateTimeOffset? LastSeenAtUtc,
    DateTimeOffset? LastBrowserLaunchAtUtc, DateTimeOffset? LastPwaLaunchAtUtc, string? LastLaunchMode,
    DateTimeOffset? LastLaunchAtUtc);
