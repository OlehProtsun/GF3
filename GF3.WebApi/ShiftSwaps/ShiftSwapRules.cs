using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using WebApi.Contracts.ShiftSwaps;
using Microsoft.Data.Sqlite;

namespace WebApi.ShiftSwaps;

internal static class ShiftSwapRules
{
    internal static void PopulateReturnSummary(ShiftSwapDto dto, ShiftSwapRequestModel swap,
        ScheduleSlotModel? returnSlot, ShiftSwapHistoryModel? history = null)
    {
        var reservation = ShiftSwapArchive.ReadReservation(swap);
        dto.RequestKind = swap.RequestKind == ShiftSwapRequestKind.Exchange ? "exchange" : "give";
        dto.ReturnScheduleId = swap.ReturnScheduleId;
        dto.ReturnScheduleSlotId = swap.ReturnScheduleSlotId;
        dto.ReturnScheduleName = history?.ReturnScheduleName ?? reservation?.Return.ScheduleName ?? returnSlot?.Schedule.Name;
        dto.ReturnShopName = history?.ReturnShopName ?? reservation?.Return.ShopName ?? returnSlot?.Schedule.Shop?.Name;
        dto.ReturnYear = history?.ReturnYear ?? reservation?.Return.Year ?? returnSlot?.Schedule.Year;
        dto.ReturnMonth = history?.ReturnMonth ?? reservation?.Return.Month ?? returnSlot?.Schedule.Month;
        dto.ReturnDayOfMonth = history?.ReturnDayOfMonth ?? reservation?.Return.DayOfMonth ?? returnSlot?.DayOfMonth;
        dto.ReturnFromTime = history?.ReturnFromTime ?? swap.ReturnFromTime;
        dto.ReturnToTime = history?.ReturnToTime ?? swap.ReturnToTime;
        dto.ReturnShiftHours = dto.ReturnFromTime is null || dto.ReturnToTime is null ? null : GetTimeRangeDurationHours(dto.ReturnFromTime, dto.ReturnToTime);
        if (reservation is not null)
        {
            dto.ScheduleName = reservation.Source.ScheduleName;
            dto.ShopName = reservation.Source.ShopName;
            dto.Year = reservation.Source.Year;
            dto.Month = reservation.Source.Month;
            dto.DayOfMonth = reservation.Source.DayOfMonth;
        }
        if (history is not null)
        {
            dto.ScheduleName = history.ScheduleName;
            dto.ShopName = history.ShopName;
            dto.Year = history.Year;
            dto.Month = history.Month;
            dto.DayOfMonth = history.DayOfMonth;
            dto.FromTime = history.FromTime;
            dto.ToTime = history.ToTime;
            dto.BeforeSnapshot = ShiftSwapHistorySnapshotBuilder.Deserialize(history.BeforeSnapshotJson);
            dto.AfterSnapshot = ShiftSwapHistorySnapshotBuilder.Deserialize(history.AfterSnapshotJson);
            dto.ReturnBeforeSnapshot = ShiftSwapHistorySnapshotBuilder.Deserialize(history.ReturnBeforeSnapshotJson);
            dto.ReturnAfterSnapshot = ShiftSwapHistorySnapshotBuilder.Deserialize(history.ReturnAfterSnapshotJson);
        }
    }
    internal static bool IsSqliteWriterConflict(Exception exception)
        => exception is SqliteException { SqliteErrorCode: 5 or 6 } ||
            exception.InnerException is SqliteException { SqliteErrorCode: 5 or 6 };
    private static readonly Regex GraphNoteMetaRegex = new(
        @"(?:\r?\n\r?\n)?(?:<!--GF3_GRAPH_META:([\s\S]*?)-->|\[\[GF3_GRAPH_META:([\s\S]*?)\]\])$",
        RegexOptions.Compiled);

    internal static ShiftSwapDto ToDto(
        ShiftSwapRequestModel model,
        int currentEmployeeId,
        bool isScheduleLocked,
        IReadOnlyList<ScheduleSlotModel> currentEmployeeMonthSlots)
    {
        if (ShiftSwapArchive.IsArchived(model))
            return ShiftSwapArchive.ReadView(model.ArchivedViewsJson!, currentEmployeeId);
        var slot = model.Schedule.Slots.FirstOrDefault(slot => slot.Id == model.ScheduleSlotId) ?? model.ScheduleSlot
            ?? throw new InvalidOperationException("The swap has no shift or archived snapshot.");
        ShiftSwapPeriod offeredPeriod;
        try
        {
            offeredPeriod = GetSwapPeriod(model, slot);
        }
        catch (ValidationException)
        {
            offeredPeriod = new(model.OfferedFromTime ?? slot.FromTime, model.OfferedToTime ?? slot.ToTime);
        }
        var shiftHours = GetTimeRangeDurationHours(offeredPeriod.FromTime, offeredPeriod.ToTime);
        var fromHoursBefore = model.FromEmployeeId.HasValue ? GetEmployeeHours(model.Schedule.Slots, model.FromEmployeeId.Value) : 0;
        var isOpen = model.Status == ShiftSwapStatus.Open;
        var isOwner = model.FromEmployeeId == currentEmployeeId;
        var acceptanceUnavailableReason = GetAcceptanceUnavailableReason(
            model,
            currentEmployeeId,
            isScheduleLocked,
            currentEmployeeMonthSlots,
            slot,
            offeredPeriod);
        var canAccept = isOpen && acceptanceUnavailableReason is null;
        var eligibleWindows = canAccept ? FindEligibleClaimWindows(model, offeredPeriod, currentEmployeeMonthSlots, currentEmployeeId) : [];
        var canAcceptFull = eligibleWindows.Contains(offeredPeriod);
        var projectedClaimHours = model.AllowPartialAcceptance && canAccept && !canAcceptFull
            ? GetTimeRangeDurationHours(eligibleWindows[0].FromTime, eligibleWindows[0].ToTime) : shiftHours;
        var currentEmployeeStats = GetCurrentEmployeeSwapStats(
            model,
            currentEmployeeId,
            currentEmployeeMonthSlots,
            slot,
            projectedClaimHours,
            canAccept);
        var fromHoursAfter = fromHoursBefore;

        if (isOpen)
        {
            if (isOwner)
            {
                fromHoursAfter = Math.Max(0, fromHoursBefore - shiftHours);
            }
            else if (canAccept)
            {
                fromHoursAfter = model.IsManagerCreated ? fromHoursBefore : Math.Max(0, fromHoursBefore - projectedClaimHours);
            }
        }

        var manualColumnName = model.IsManagerCreated
            ? GraphManualColumnLabelResolver.Resolve(model.Schedule.Note, model.ManualColumnId)
            : null;

        return new ShiftSwapDto
        {
            AllowPartialAcceptance = model.RequestKind == ShiftSwapRequestKind.Give && model.AllowPartialAcceptance,
            CanAcceptFull = canAcceptFull,
            EligibleClaimWindows = eligibleWindows.Select(p => new ShiftSwapEligibleWindowDto(p.FromTime, p.ToTime)).ToList(),
            RequestKind = model.RequestKind == ShiftSwapRequestKind.Exchange ? "exchange" : "give",
            ReturnScheduleId = model.ReturnScheduleId,
            ReturnScheduleSlotId = model.ReturnScheduleSlotId,
            ReturnFromTime = model.ReturnFromTime,
            ReturnToTime = model.ReturnToTime,
            Id = model.Id,
            ScheduleId = model.ScheduleId,
            ScheduleSlotId = model.ScheduleSlotId ?? 0,
            ScheduleName = model.Schedule.Name,
            ContainerName = model.Schedule.Container?.Name ?? string.Empty,
            ShopName = model.Schedule.Shop?.Name ?? string.Empty,
            Year = model.Schedule.Year,
            Month = model.Schedule.Month,
            DayOfMonth = slot.DayOfMonth,
            FromTime = offeredPeriod.FromTime,
            ToTime = offeredPeriod.ToTime,
            FromEmployeeId = model.FromEmployeeId,
            FromEmployeeName = manualColumnName ?? GetEmployeeName(model.FromEmployee, model.FromEmployeeId),
            TargetEmployeeId = model.TargetEmployeeId,
            TargetEmployeeName = model.TargetEmployeeId.HasValue ? GetEmployeeName(model.TargetEmployee, model.TargetEmployeeId.Value) : null,
            AcceptedByEmployeeId = model.AcceptedByEmployeeId,
            AcceptedByEmployeeName = model.AcceptedByEmployeeId.HasValue
                ? GetEmployeeName(model.AcceptedByEmployee, model.AcceptedByEmployeeId.Value)
                : null,
            Visibility = model.Visibility == ShiftSwapVisibility.Private ? "private" : "public",
            Status = model.Status switch
            {
                ShiftSwapStatus.Accepted => "accepted",
                ShiftSwapStatus.Cancelled => "cancelled",
                _ => "open",
            },
            CreatedAtUtc = model.CreatedAtUtc,
            AcceptedAtUtc = model.AcceptedAtUtc,
            ShiftHours = Math.Round(shiftHours, 2),
            CurrentEmployeeHoursBefore = Math.Round(currentEmployeeStats.HoursBefore, 2),
            CurrentEmployeeHoursAfter = Math.Round(currentEmployeeStats.HoursAfter, 2),
            CurrentEmployeeWorkDaysBefore = currentEmployeeStats.WorkDaysBefore,
            CurrentEmployeeWorkDaysAfter = currentEmployeeStats.WorkDaysAfter,
            CurrentEmployeeFreeDaysBefore = currentEmployeeStats.FreeDaysBefore,
            CurrentEmployeeFreeDaysAfter = currentEmployeeStats.FreeDaysAfter,
            FromEmployeeHoursBefore = Math.Round(fromHoursBefore, 2),
            FromEmployeeHoursAfter = Math.Round(fromHoursAfter, 2),
            IsManagerCreated = model.IsManagerCreated,
            ManualColumnId = model.ManualColumnId,
            ManualColumnName = manualColumnName,
            IsCreatedByCurrentEmployee = isOwner,
            IsScheduleLocked = isScheduleLocked,
            CanAccept = canAccept,
            AcceptanceUnavailableReason = isOpen ? acceptanceUnavailableReason : null,
            CanCancel = isOwner && isOpen,
        };
    }

    internal static string? GetAcceptanceUnavailableReason(
        ShiftSwapRequestModel swap,
        int employeeId,
        bool isScheduleLocked,
        IReadOnlyList<ScheduleSlotModel> employeeMonthSlots,
        ScheduleSlotModel slot,
        ShiftSwapPeriod offeredPeriod)
    {
        if (swap.Status != ShiftSwapStatus.Open)
        {
            return "This swap offer is no longer open.";
        }

        if (swap.Schedule.PublicationStatus != SchedulePublicationStatus.Public)
            return "This swap belongs to a schedule that is no longer public.";

        if (swap.ScheduleSlotId != slot.Id || slot.ScheduleId != swap.ScheduleId)
            return "This shift is no longer available.";
        if (swap.IsManagerCreated ? slot.EmployeeId.HasValue :
            !swap.FromEmployeeId.HasValue || slot.EmployeeId != swap.FromEmployeeId)
            return swap.IsManagerCreated ? "This open shift is no longer available." :
                "This shift is no longer assigned to the employee who opened the swap.";

        try
        {
            NormalizePeriod(offeredPeriod.FromTime, offeredPeriod.ToTime, "FromTime", "ToTime");
            EnsurePeriodWithinSlot(slot, offeredPeriod);
            if (swap.IsManagerCreated && swap.ManualColumnId.HasValue)
                ConsumeManualColumnPeriodFromNote(swap.Schedule.Note, swap.ManualColumnId.Value, slot.DayOfMonth, offeredPeriod);
        }
        catch (ValidationException exception)
        {
            return exception.Message;
        }

        if (!swap.Schedule.AllowSwap)
        {
            return "Swaps are not allowed for this schedule.";
        }

        if (isScheduleLocked)
        {
            return "Schedule is locked while a manager is editing it.";
        }

        if (swap.FromEmployeeId == employeeId)
        {
            return "This is your own swap offer.";
        }

        if (swap.TargetEmployeeId.HasValue && swap.TargetEmployeeId.Value != employeeId)
        {
            return "This private swap offer is for another employee.";
        }

        var windows = FindEligibleClaimWindows(swap, offeredPeriod, employeeMonthSlots, employeeId);
        if (swap.RequestKind == ShiftSwapRequestKind.Exchange || !swap.AllowPartialAcceptance ? !windows.Contains(offeredPeriod) : windows.Count == 0)
            return "You already work during this time.";

        return null;
    }

    internal static CurrentEmployeeSwapStats GetCurrentEmployeeSwapStats(
        ShiftSwapRequestModel swap,
        int employeeId,
        IReadOnlyList<ScheduleSlotModel> employeeMonthSlots,
        ScheduleSlotModel offeredSlot,
        double shiftHours,
        bool canAccept)
    {
        var daysInMonth = DateTime.DaysInMonth(swap.Schedule.Year, swap.Schedule.Month);
        var actualHours = employeeMonthSlots.Sum(GetSlotDurationHours);
        var actualWorkDays = employeeMonthSlots.Select(slot => slot.DayOfMonth).ToHashSet();
        var beforeHours = actualHours;
        var afterHours = actualHours;
        var beforeWorkDays = new HashSet<int>(actualWorkDays);
        var afterWorkDays = new HashSet<int>(actualWorkDays);

        if (swap.Status == ShiftSwapStatus.Open && swap.FromEmployeeId == employeeId)
        {
            afterHours = Math.Max(0, actualHours - shiftHours);
            RemoveTransferredWorkDay(afterWorkDays, employeeMonthSlots, offeredSlot, shiftHours);
        }
        else if (swap.Status == ShiftSwapStatus.Open && canAccept)
        {
            afterHours = actualHours + shiftHours;
            afterWorkDays.Add(offeredSlot.DayOfMonth);
        }
        else if (swap.Status == ShiftSwapStatus.Accepted && swap.AcceptedByEmployeeId == employeeId)
        {
            beforeHours = Math.Max(0, actualHours - shiftHours);
            RemoveTransferredWorkDay(beforeWorkDays, employeeMonthSlots, offeredSlot, shiftHours);
        }
        else if (swap.Status == ShiftSwapStatus.Accepted && swap.FromEmployeeId == employeeId)
        {
            beforeHours = actualHours + shiftHours;
            beforeWorkDays.Add(offeredSlot.DayOfMonth);
        }

        return new CurrentEmployeeSwapStats(
            beforeHours,
            afterHours,
            beforeWorkDays.Count,
            afterWorkDays.Count,
            Math.Max(0, daysInMonth - beforeWorkDays.Count),
            Math.Max(0, daysInMonth - afterWorkDays.Count));
    }

    internal static void RemoveTransferredWorkDay(
        ISet<int> workDays,
        IReadOnlyList<ScheduleSlotModel> employeeMonthSlots,
        ScheduleSlotModel offeredSlot,
        double transferredHours)
    {
        var otherSameDayHours = employeeMonthSlots
            .Where(slot => slot.DayOfMonth == offeredSlot.DayOfMonth && slot.Id != offeredSlot.Id)
            .Sum(GetSlotDurationHours);
        var offeredSlotHours = employeeMonthSlots
            .Where(slot => slot.Id == offeredSlot.Id)
            .Sum(GetSlotDurationHours);
        var remainingOfferedSlotHours = Math.Max(0, offeredSlotHours - transferredHours);

        if (otherSameDayHours + remainingOfferedSlotHours <= 0.001)
        {
            workDays.Remove(offeredSlot.DayOfMonth);
        }
    }

    internal readonly record struct CurrentEmployeeSwapStats(
        double HoursBefore,
        double HoursAfter,
        int WorkDaysBefore,
        int WorkDaysAfter,
        int FreeDaysBefore,
        int FreeDaysAfter);

    internal static string GetEmployeeName(EmployeeModel? employee, int? employeeId)
    {
        var fullName = $"{employee?.FirstName} {employee?.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return employeeId.HasValue ? $"Employee #{employeeId.Value}" : "Manual column";
    }

    internal static double GetEmployeeHours(IEnumerable<ScheduleSlotModel> slots, int employeeId)
        => slots
            .Where(slot => slot.EmployeeId == employeeId)
            .Sum(GetSlotDurationHours);

    internal static IReadOnlyList<ShiftSwapPeriod> FindEligibleClaimWindows(ShiftSwapRequestModel swap, ShiftSwapPeriod offered,
        IReadOnlyList<ScheduleSlotModel> employeeMonthSlots, int acceptingEmployeeId)
    {
        var source = swap.Schedule.Slots.FirstOrDefault(s => s.Id == swap.ScheduleSlotId) ?? swap.ScheduleSlot;
        if (source is null) return [];
        var normalized = NormalizePeriod(offered.FromTime, offered.ToTime, "FromTime", "ToTime");
        var windows = new List<ShiftSwapPeriod> { normalized };
        foreach (var occupied in employeeMonthSlots.Where(s => s.EmployeeId == acceptingEmployeeId && s.Id != source.Id &&
            s.DayOfMonth == source.DayOfMonth && s.Schedule.Year == swap.Schedule.Year && s.Schedule.Month == swap.Schedule.Month &&
            s.Schedule.PublicationStatus == SchedulePublicationStatus.Public)) {
            var busy = NormalizePeriod(occupied.FromTime, occupied.ToTime, "FromTime", "ToTime");
            windows = windows.SelectMany(free => {
                if (string.CompareOrdinal(busy.ToTime, free.FromTime) <= 0 || string.CompareOrdinal(busy.FromTime, free.ToTime) >= 0) return new[] { free };
                var remaining = new List<ShiftSwapPeriod>();
                if (string.CompareOrdinal(busy.FromTime, free.FromTime) > 0) remaining.Add(new(free.FromTime, busy.FromTime));
                if (string.CompareOrdinal(busy.ToTime, free.ToTime) < 0) remaining.Add(new(busy.ToTime, free.ToTime));
                return remaining.ToArray();
            }).ToList();
        }
        return windows.OrderBy(p => p.FromTime, StringComparer.Ordinal).ToList();
    }

    internal static ShiftSwapPeriod ValidateClaimedPeriod(ShiftSwapRequestModel swap, ShiftSwapPeriod offered,
        AcceptEmployeeShiftSwapRequest? request, IReadOnlyList<ScheduleSlotModel> employeeMonthSlots, int employeeId)
    {
        if ((request?.FromTime is null) != (request?.ToTime is null))
            throw ValidationException.ForField("FromTime", "Supply both start and end times.");
        var claimed = request?.FromTime is null ? offered : NormalizePeriod(request.FromTime, request.ToTime!, "FromTime", "ToTime");
        if (string.CompareOrdinal(claimed.FromTime, offered.FromTime) < 0 || string.CompareOrdinal(claimed.ToTime, offered.ToTime) > 0)
            throw new ValidationException("The claimed period must stay inside the offered shift.");
        if ((swap.RequestKind == ShiftSwapRequestKind.Exchange || !swap.AllowPartialAcceptance) && claimed != offered)
            throw new ValidationException("This offer must be accepted for the whole published period.");
        if (!FindEligibleClaimWindows(swap, offered, employeeMonthSlots, employeeId).Any(free =>
            string.CompareOrdinal(claimed.FromTime, free.FromTime) >= 0 && string.CompareOrdinal(claimed.ToTime, free.ToTime) <= 0))
            throw new ValidationException("You already work during this time.");
        return claimed;
    }

    internal readonly record struct ShiftSwapPeriod(string FromTime, string ToTime);

    internal static ShiftSwapPeriod ResolveRequestedPeriod(CreateEmployeeShiftSwapRequest request, ScheduleSlotModel slot)
    {
        if (string.IsNullOrWhiteSpace(request.FromTime) != string.IsNullOrWhiteSpace(request.ToTime))
            throw ValidationException.ForField(string.IsNullOrWhiteSpace(request.FromTime) ? nameof(request.FromTime) : nameof(request.ToTime),
                "Supply both start and end times.");
        var fromTime = string.IsNullOrWhiteSpace(request.FromTime) ? slot.FromTime : request.FromTime;
        var toTime = string.IsNullOrWhiteSpace(request.ToTime) ? slot.ToTime : request.ToTime;
        var period = NormalizePeriod(fromTime!, toTime!, nameof(request.FromTime), nameof(request.ToTime));
        EnsurePeriodWithinSlot(slot, period);

        return period;
    }

    internal static ShiftSwapPeriod GetSwapPeriod(ShiftSwapRequestModel model, ScheduleSlotModel slot)
    {
        var fromTime = string.IsNullOrWhiteSpace(model.OfferedFromTime) ? slot.FromTime : model.OfferedFromTime;
        var toTime = string.IsNullOrWhiteSpace(model.OfferedToTime) ? slot.ToTime : model.OfferedToTime;

        return NormalizePeriod(fromTime!, toTime!, nameof(model.OfferedFromTime), nameof(model.OfferedToTime));
    }

    internal static ShiftSwapPeriod NormalizePeriod(string fromTime, string toTime, string fromFieldName, string toFieldName)
    {
        var normalizedFrom = NormalizeTimeText(fromTime, fromFieldName);
        var normalizedTo = NormalizeTimeText(toTime, toFieldName);
        var normalizedFromMinutes = ParseTimeMinutes(normalizedFrom)!.Value;
        var normalizedToMinutes = ParseTimeMinutes(normalizedTo)!.Value;

        if (normalizedToMinutes <= normalizedFromMinutes)
        {
            throw ValidationException.ForField(toFieldName, "The end time must be after the start time.");
        }

        return new ShiftSwapPeriod(normalizedFrom, normalizedTo);
    }

    internal static string NormalizeTimeText(string value, string fieldName)
    {
        var minutes = ParseTimeMinutes(value);
        if (minutes is null)
        {
            throw ValidationException.ForField(fieldName, "Use HH:mm time format.");
        }

        return $"{minutes.Value / 60:00}:{minutes.Value % 60:00}";
    }

    internal static void EnsurePeriodWithinSlot(ScheduleSlotModel slot, ShiftSwapPeriod period)
    {
        var slotFrom = ParseTimeMinutes(slot.FromTime);
        var slotTo = ParseTimeMinutes(slot.ToTime);
        var periodFrom = ParseTimeMinutes(period.FromTime);
        var periodTo = ParseTimeMinutes(period.ToTime);
        if (slotFrom is null || slotTo is null || periodFrom is null || periodTo is null || slotTo <= slotFrom)
        {
            throw new ValidationException("The selected shift has an invalid time range.");
        }

        if (periodFrom < slotFrom || periodTo > slotTo)
        {
            throw new ValidationException("The offered period must stay inside the selected shift.");
        }
    }

    internal static IReadOnlyList<ScheduleSlotModel> ApplyAcceptedSwapPeriod(
        ScheduleSlotModel slot,
        int? originalEmployeeId,
        int acceptingEmployeeId,
        ShiftSwapPeriod period,
        IReadOnlyCollection<ScheduleSlotModel> scheduleSlots)
    {
        var remainingSlots = new List<ScheduleSlotModel>();
        var originalFrom = slot.FromTime;
        var originalTo = slot.ToTime;
        var originalFromMinutes = ParseTimeMinutes(originalFrom)!.Value;
        var originalToMinutes = ParseTimeMinutes(originalTo)!.Value;
        var periodFromMinutes = ParseTimeMinutes(period.FromTime)!.Value;
        var periodToMinutes = ParseTimeMinutes(period.ToTime)!.Value;

        int AllocateSlotNo(string fromTime, string toTime)
        {
            var occupied = scheduleSlots
                .Where(existing => existing.Id != slot.Id)
                .Concat(remainingSlots)
                .Where(existing => existing.ScheduleId == slot.ScheduleId &&
                    existing.DayOfMonth == slot.DayOfMonth &&
                    existing.FromTime == fromTime && existing.ToTime == toTime)
                .Select(existing => existing.SlotNo)
                .ToHashSet();
            var slotNo = 1;
            while (occupied.Contains(slotNo))
                slotNo++;
            return slotNo;
        }

        if (periodFromMinutes > originalFromMinutes)
        {
            remainingSlots.Add(CreateRemainingSlot(slot, originalEmployeeId, originalFrom, period.FromTime,
                AllocateSlotNo(originalFrom, period.FromTime)));
        }

        if (periodToMinutes < originalToMinutes)
        {
            remainingSlots.Add(CreateRemainingSlot(slot, originalEmployeeId, period.ToTime, originalTo,
                AllocateSlotNo(period.ToTime, originalTo)));
        }

        if (remainingSlots.Count > 0)
            slot.SlotNo = AllocateSlotNo(period.FromTime, period.ToTime);

        slot.FromTime = period.FromTime;
        slot.ToTime = period.ToTime;
        slot.EmployeeId = acceptingEmployeeId;
        slot.Status = SlotStatus.ASSIGNED;

        return remainingSlots;
    }

    internal static ScheduleSlotModel CreateRemainingSlot(ScheduleSlotModel sourceSlot, int? employeeId, string fromTime, string toTime, int slotNo)
        => new()
        {
            ScheduleId = sourceSlot.ScheduleId,
            DayOfMonth = sourceSlot.DayOfMonth,
            SlotNo = slotNo,
            EmployeeId = employeeId,
            Status = employeeId.HasValue ? SlotStatus.ASSIGNED : SlotStatus.UNFURNISHED,
            FromTime = fromTime,
            ToTime = toTime,
        };

    internal static double GetSlotDurationHours(ScheduleSlotModel slot)
        => GetTimeRangeDurationHours(slot.FromTime, slot.ToTime);

    internal static double GetTimeRangeDurationHours(string fromTime, string toTime)
    {
        var fromMinutes = ParseTimeMinutes(fromTime);
        var toMinutes = ParseTimeMinutes(toTime);
        if (fromMinutes is null || toMinutes is null)
        {
            return 0;
        }

        var duration = toMinutes.Value - fromMinutes.Value;
        if (duration <= 0)
        {
            return 0;
        }

        return duration / 60d;
    }

    internal static int? ParseTimeMinutes(string value)
    {
        if (value.Length != 5 || value[2] != ':' ||
            value.Where((_, index) => index != 2).Any(character => character is < '0' or > '9'))
            return null;
        var parts = value.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length < 2 ||
            !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var hour) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var minute))
        {
            return null;
        }

        if (hour is < 0 or > 23 || minute is < 0 or > 59)
        {
            return null;
        }

        return hour * 60 + minute;
    }

    internal static bool HasOverlappingShift(
        IEnumerable<ScheduleSlotModel> slots,
        int dayOfMonth,
        string fromTime,
        string toTime,
        int employeeId,
        int excludedSlotId)
        => slots.Any(slot =>
            slot.Id != excludedSlotId &&
            slot.EmployeeId == employeeId &&
            slot.DayOfMonth == dayOfMonth &&
            TimesOverlap(slot.FromTime, slot.ToTime, fromTime, toTime));

    internal static bool TimesOverlap(string leftFrom, string leftTo, string rightFrom, string rightTo)
    {
        var leftStart = ParseTimeMinutes(leftFrom);
        var leftEnd = ParseTimeMinutes(leftTo);
        var rightStart = ParseTimeMinutes(rightFrom);
        var rightEnd = ParseTimeMinutes(rightTo);
        if (leftStart is null || leftEnd is null || rightStart is null || rightEnd is null)
        {
            return false;
        }

        return leftStart.Value < rightEnd.Value && rightStart.Value < leftEnd.Value;
    }

    internal static string? ConsumeManualColumnPeriodFromNote(string? rawNote, int manualColumnId, int dayOfMonth, ShiftSwapPeriod claimed)
    {
        const string invalid = "The manual shift metadata is invalid or the cell is missing.";
        if (string.IsNullOrWhiteSpace(rawNote)) throw new ValidationException(invalid);
        var match = GraphNoteMetaRegex.Match(rawNote);
        if (!match.Success) throw new ValidationException(invalid);
        var meta = ParseGraphNoteMeta(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value)
            ?? throw new ValidationException(invalid);
        var day = dayOfMonth.ToString(CultureInfo.InvariantCulture);
        JsonObject? cells = null;
        if (meta["m"] is JsonArray compact)
            cells = compact.OfType<JsonArray>().FirstOrDefault(c => GetJsonInt(c.ElementAtOrDefault(0)) == manualColumnId)?.ElementAtOrDefault(2) as JsonObject;
        if (cells is null && meta["manualColumns"] is JsonArray legacy)
            cells = legacy.OfType<JsonObject>().FirstOrDefault(c => GetJsonInt(c["id"]) == manualColumnId)?["cells"] as JsonObject;
        if (cells?[day] is not JsonValue value || !value.TryGetValue<string>(out var raw)) throw new ValidationException(invalid);
        var remaining = ManualShiftDemandRules.Consume(raw, claimed);
        if (remaining is null) cells.Remove(day); else cells[day] = remaining;
        if (cells.Count == 0 && meta["m"] is JsonArray columns) {
            var column = columns.OfType<JsonArray>().FirstOrDefault(c => c.Count > 2 && ReferenceEquals(c[2], cells));
            column?.RemoveAt(2);
        }
        var prefix = rawNote[..match.Index].TrimEnd();
        var suffix = $"[[GF3_GRAPH_META:b64:{EncodeGraphNoteMetaValue(meta.ToJsonString())}]]";
        return string.IsNullOrEmpty(prefix) ? suffix : $"{prefix}\n\n{suffix}";
    }

    internal static string UpsertManualColumnBackgroundColorInNote(string? rawNote, int manualColumnId, int dayOfMonth, int backgroundColorArgb)
    {
        const string invalid = "The manual shift metadata is invalid or the cell is missing.";
        if (manualColumnId <= 0 || dayOfMonth is < 1 or > 31 || string.IsNullOrWhiteSpace(rawNote))
            throw new ValidationException(invalid);
        var match = GraphNoteMetaRegex.Match(rawNote);
        if (!match.Success) throw new ValidationException(invalid);
        var meta = ParseGraphNoteMeta(match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value)
            ?? throw new ValidationException(invalid);
        var columnExists = (meta["m"] as JsonArray)?.OfType<JsonArray>()
            .Any(c => GetJsonInt(c.ElementAtOrDefault(0)) == manualColumnId) == true
            || (meta["manualColumns"] as JsonArray)?.OfType<JsonObject>()
                .Any(c => GetJsonInt(c["id"]) == manualColumnId) == true;
        if (!columnExists) throw new ValidationException(invalid);

        // Match graphNote.ts: valid legacy styles take precedence over compact styles.
        var legacy = meta["cellStyles"] as JsonArray;
        var useLegacy = legacy?.OfType<JsonObject>().Any(s =>
            GetJsonInt(s["employeeId"]) is <= 0 && GetJsonInt(s["dayOfMonth"]) is >= 1 and <= 31
            && (s["backgroundColorArgb"] is JsonValue bg && bg.TryGetValue<int>(out _)
                || s["textColorArgb"] is JsonValue fg && fg.TryGetValue<int>(out _))) == true;
        var key = useLegacy ? "cellStyles" : "s";
        var styles = meta[key] as JsonArray;
        if (styles is null) { styles = new JsonArray(); meta[key] = styles; }
        bool Matches(JsonNode? s) => useLegacy
            ? s is JsonObject obj && GetJsonInt(obj["employeeId"]) == -manualColumnId && GetJsonInt(obj["dayOfMonth"]) == dayOfMonth
            : s is JsonArray arr && GetJsonInt(arr.ElementAtOrDefault(0)) == -manualColumnId && GetJsonInt(arr.ElementAtOrDefault(1)) == dayOfMonth;
        var matching = styles.Where(Matches).ToList();
        var textColor = matching.Select(s => useLegacy ? s!["textColorArgb"] : ((JsonArray)s!).ElementAtOrDefault(3))
            .FirstOrDefault(value => value is JsonValue number && number.TryGetValue<int>(out _))?.DeepClone();
        foreach (var entry in matching) styles.Remove(entry);
        if (useLegacy)
            styles.Add(new JsonObject { ["employeeId"] = -manualColumnId, ["dayOfMonth"] = dayOfMonth,
                ["backgroundColorArgb"] = backgroundColorArgb, ["textColorArgb"] = textColor });
        else
        {
            var entry = new JsonArray(-manualColumnId, dayOfMonth, backgroundColorArgb);
            if (textColor is not null) entry.Add(textColor);
            styles.Add(entry);
        }
        var visibleNote = rawNote[..match.Index].TrimEnd();
        var suffix = $"[[GF3_GRAPH_META:b64:{EncodeGraphNoteMetaValue(meta.ToJsonString())}]]";
        return string.IsNullOrEmpty(visibleNote) ? suffix : $"{visibleNote}\n\n{suffix}";
    }

    internal static string? RemoveManualColumnCellFromNote(string? rawNote, int manualColumnId, int dayOfMonth)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            throw new ValidationException("The manual shift metadata is invalid or the cell is missing.");
        }

        var match = GraphNoteMetaRegex.Match(rawNote);
        if (!match.Success || match.Index < 0)
        {
            throw new ValidationException("The manual shift metadata is invalid or the cell is missing.");
        }

        var rawMeta = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[1].Value;
        var meta = ParseGraphNoteMeta(rawMeta);
        if (meta is null)
        {
            throw new ValidationException("The manual shift metadata is invalid or the cell is missing.");
        }

        var changed =
            RemoveCompactManualColumnCell(meta["m"] as JsonArray, manualColumnId, dayOfMonth) ||
            RemoveLegacyManualColumnCell(meta["manualColumns"] as JsonArray, manualColumnId, dayOfMonth);

        if (!changed)
        {
            throw new ValidationException("The manual shift metadata is invalid or the cell is missing.");
        }

        var visibleNote = rawNote[..match.Index].TrimEnd();
        var encodedMeta = $"b64:{EncodeGraphNoteMetaValue(meta.ToJsonString())}";
        var suffix = $"[[GF3_GRAPH_META:{encodedMeta}]]";

        return string.IsNullOrEmpty(visibleNote) ? suffix : $"{visibleNote}\n\n{suffix}";
    }

    internal static JsonObject? ParseGraphNoteMeta(string rawMeta)
    {
        var payload = rawMeta.Trim();
        if (payload.Length == 0)
        {
            return null;
        }

        if (payload.StartsWith("b64:", StringComparison.Ordinal))
        {
            try
            {
                return TryParseJsonObject(DecodeGraphNoteMetaValue(payload[4..]));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        return TryParseJsonObject(payload) ?? TryParseJsonObject(Uri.UnescapeDataString(payload));
    }

    internal static JsonObject? TryParseJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(value) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    internal static string DecodeGraphNoteMetaValue(string value)
    {
        var normalized = value
            .Replace('-', '+')
            .Replace('_', '/')
            .PadRight((int)Math.Ceiling(value.Length / 4d) * 4, '=');

        return Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
    }

    internal static string EncodeGraphNoteMetaValue(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    internal static bool RemoveCompactManualColumnCell(JsonArray? manualColumns, int manualColumnId, int dayOfMonth)
    {
        if (manualColumns is null)
        {
            return false;
        }

        var dayKey = dayOfMonth.ToString(CultureInfo.InvariantCulture);
        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonArray column || GetJsonInt(column.ElementAtOrDefault(0)) != manualColumnId)
            {
                continue;
            }

            if (column.Count < 3 || column[2] is not JsonObject cells ||
                cells[dayKey] is not JsonValue cell || !cell.TryGetValue<string>(out var text) ||
                string.IsNullOrWhiteSpace(text) || !cells.Remove(dayKey))
            {
                return false;
            }

            if (cells.Count == 0)
            {
                column.RemoveAt(2);
            }

            return true;
        }

        return false;
    }

    internal static bool RemoveLegacyManualColumnCell(JsonArray? manualColumns, int manualColumnId, int dayOfMonth)
    {
        if (manualColumns is null)
        {
            return false;
        }

        var dayKey = dayOfMonth.ToString(CultureInfo.InvariantCulture);
        foreach (var rawColumn in manualColumns)
        {
            if (rawColumn is not JsonObject column || GetJsonInt(column["id"]) != manualColumnId)
            {
                continue;
            }

            return column["cells"] is JsonObject cells && cells[dayKey] is JsonValue cell &&
                cell.TryGetValue<string>(out var text) && !string.IsNullOrWhiteSpace(text) && cells.Remove(dayKey);
        }

        return false;
    }

    internal static int? GetJsonInt(JsonNode? node)
    {
        if (node is null)
        {
            return null;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch (FormatException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
