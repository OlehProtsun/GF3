using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;

namespace BusinessLogicLayer.Availability;

/// <summary>
/// Transforms compact UI/import payloads into the richer nested structure expected by availability services.
/// The builder is intentionally strict and fails on the first invalid code so the caller can return a precise
/// validation message instead of persisting a partially normalized payload.
/// </summary>
public static class AvailabilityPayloadBuilder
{
    /// <summary>
    /// Builds a service-ready payload grouped by employee identifier.
    /// </summary>
    public static bool TryBuild(
        IEnumerable<(int employeeId, IList<(int day, string code)> codes)> raw,
        out List<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
        out string? error)
    {
        payload = [];
        error = null;

        foreach (var (employeeId, codes) in raw)
        {
            if (!TryBuildDays(employeeId, codes, out var days, out error))
            {
                return false;
            }

            payload.Add((employeeId, days));
        }

        return true;
    }

    private static bool TryBuildDays(
        int employeeId,
        IList<(int day, string code)> codes,
        out List<AvailabilityGroupDayModel> days,
        out string? error)
    {
        days = new List<AvailabilityGroupDayModel>(codes.Count);
        error = null;

        foreach (var (day, code) in codes)
        {
            if (!AvailabilityCodeParser.TryParse(code, out var kind, out var interval))
            {
                error = $"Invalid code '{code}' for day {day} (employee #{employeeId}).";
                return false;
            }

            days.Add(CreateDayModel(day, kind, interval));
        }

        return true;
    }

    private static AvailabilityGroupDayModel CreateDayModel(int dayOfMonth, AvailabilityKind kind, string? interval) =>
        new()
        {
            Id = 0,
            AvailabilityGroupMemberId = 0,
            DayOfMonth = dayOfMonth,
            Kind = kind,
            IntervalStr = interval,
        };
}
