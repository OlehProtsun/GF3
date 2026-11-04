using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using static WebApi.ShiftSwaps.ShiftSwapRules;

namespace WebApi.ShiftSwaps;

internal static class ManualShiftDemandRules
{
    internal static IReadOnlyList<ShiftSwapPeriod> ParseCell(string rawCell)
    {
        if (string.IsNullOrWhiteSpace(rawCell)) return [];
        var result = new List<ShiftSwapPeriod>();
        foreach (var token in rawCell.Split(',')) {
            var match = Regex.Match(token.Trim(), @"^(\d{1,2}:[0-5]\d)\s*[-–—]\s*(\d{1,2}:[0-5]\d)$");
            if (!match.Success) throw new ValidationException("The manual shift cell must contain valid time ranges.");
            result.Add(NormalizePeriod(match.Groups[1].Value.PadLeft(5, '0'), match.Groups[2].Value.PadLeft(5, '0'), "FromTime", "ToTime"));
        }
        return result.OrderBy(p => p.FromTime, StringComparer.Ordinal).ThenBy(p => p.ToTime, StringComparer.Ordinal).ToList();
    }

    internal static bool CanReserve(string rawCell, IReadOnlyList<ShiftSwapPeriod> alreadyOpenOffers, ShiftSwapPeriod requested)
    {
        var capacity = Capacity(ParseCell(rawCell));
        var reserved = Capacity(alreadyOpenOffers);
        var period = NormalizePeriod(requested.FromTime, requested.ToTime, "FromTime", "ToTime");
        for (var m = ParseTimeMinutes(period.FromTime)!.Value; m < ParseTimeMinutes(period.ToTime)!.Value; m++)
            if (capacity[m] <= reserved[m]) return false;
        return true;
    }

    internal static string? Consume(string rawCell, ShiftSwapPeriod claimed)
    {
        if (!CanReserve(rawCell, [], claimed)) throw new ValidationException("The manual shift demand is no longer available.");
        var capacity = Capacity(ParseCell(rawCell));
        for (var m = ParseTimeMinutes(claimed.FromTime)!.Value; m < ParseTimeMinutes(claimed.ToTime)!.Value; m++) capacity[m]--;
        var result = new List<ShiftSwapPeriod>();
        for (var level = 1; level <= capacity.Max(); level++) {
            var start = -1;
            for (var m = 0; m <= 1440; m++) {
                if (m < 1440 && capacity[m] >= level) { if (start < 0) start = m; }
                else if (start >= 0) { result.Add(new(Time(start), Time(m))); start = -1; }
            }
        }
        return result.Count == 0 ? null : string.Join(", ", result.OrderBy(p => p.FromTime, StringComparer.Ordinal)
            .ThenBy(p => p.ToTime, StringComparer.Ordinal).Select(p => $"{p.FromTime} - {p.ToTime}"));
    }

    private static int[] Capacity(IEnumerable<ShiftSwapPeriod> periods)
    {
        var result = new int[1440];
        foreach (var p in periods) {
            var period = NormalizePeriod(p.FromTime, p.ToTime, "FromTime", "ToTime");
            for (var m = ParseTimeMinutes(period.FromTime)!.Value; m < ParseTimeMinutes(period.ToTime)!.Value; m++) result[m]++;
        }
        return result;
    }
    private static string Time(int minute) => $"{minute / 60:00}:{minute % 60:00}";
}
