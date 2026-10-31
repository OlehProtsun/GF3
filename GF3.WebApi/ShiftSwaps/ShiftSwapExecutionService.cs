using static WebApi.ShiftSwaps.ShiftSwapRules;
using BusinessLogicLayer.Contracts.Employees;
using BusinessLogicLayer.Services;
using BusinessLogicLayer.Services.Abstractions;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BusinessLogicLayer.Common;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using DataAccessLayer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.Sqlite;
using WebApi.Auth;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.Services;
using WebApi.ShiftSwaps;

namespace WebApi.ShiftSwaps;

public sealed class ShiftSwapExecutionService(
    AppDbContext db,
    IScheduleEditLockService scheduleEditLockService,
    IWorkflowLogService workflowLogService,
    IRealtimeNotifier realtimeNotifier,
    ScheduleVersionCommitService versionCommitService,
    IEmploymentContractPolicyService? contractPolicy = null)
{
    private int? reviewingManagerId;
    private readonly IEmploymentContractPolicyService policy = contractPolicy ?? new EmploymentContractPolicyService(db);

    public async Task<IEnumerable<ShiftSwapEmployeeDto>> GetEmployees(int employeeId, HttpContext context, CancellationToken cancellationToken)
    {
        var settings = await policy.GetSettingsAsync(cancellationToken);
        var employees = await db.Employees
            .AsNoTracking()
            .Where(employee => employee.Id != employeeId && !employee.IsDeleted)
            .OrderBy(employee => employee.FirstName)
            .ThenBy(employee => employee.LastName)
            .Select(employee => new ShiftSwapEmployeeDto
            {
                RequiresWholeEightHourShifts = settings.RestrictSwapsToEightHours && employee.ContractType == EmploymentContractTypes.OPrace,
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                DisplayName = employee.FirstName + " " + employee.LastName,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return employees;
    }

    public async Task<IEnumerable<ShiftSwapDto>> GetVisible(int employeeId, HttpContext context, CancellationToken cancellationToken)
    {
        var sharedContainerIds = db.Schedules
            .Where(schedule =>
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
            .Select(schedule => schedule.ContainerId);
        var requests = await BuildSwapRequestQuery()
            .Where(request =>
                ((request.Status != ShiftSwapStatus.Open && request.ScheduleSlotId == null && request.ArchivedViewsJson != null) || request.Schedule.PublicationStatus == SchedulePublicationStatus.Public) &&
                (request.FromEmployeeId == employeeId ||
                 request.TargetEmployeeId == employeeId ||
                 request.AcceptedByEmployeeId == employeeId ||
                 (request.Visibility == ShiftSwapVisibility.Public &&
                  sharedContainerIds.Contains(request.Schedule.ContainerId))))
            .Where(request => request.Status == ShiftSwapStatus.Open ||
                              (request.RequestKind == ShiftSwapRequestKind.Exchange && request.TargetEmployeeId == employeeId) ||
                              request.AcceptedByEmployeeId == employeeId ||
                              request.FromEmployeeId == employeeId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var years = requests.Select(request => request.Schedule.Year).Distinct().ToList();
        var months = requests.Select(request => request.Schedule.Month).Distinct().ToList();
        IReadOnlyList<ScheduleSlotModel> employeeSlots = requests.Count == 0
            ? []
            : await db.ScheduleSlots
                .AsNoTracking()
                .Include(slot => slot.Schedule)
                .Where(slot =>
                    slot.EmployeeId == employeeId &&
                    slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                    years.Contains(slot.Schedule.Year) &&
                    months.Contains(slot.Schedule.Month))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        var orderedRequests = requests
            .Where(request => request.Status != ShiftSwapStatus.Open ||
                request.Schedule.Slots.Any(slot => slot.Id == request.ScheduleSlotId))
            .OrderBy(request => request.Status == ShiftSwapStatus.Open ? 0 : 1)
            .ThenByDescending(request => request.CreatedAtUtc)
            .ToList();

        var exchangeContext = await LoadExchangeReadContextAsync(orderedRequests, cancellationToken);
        var dtos = orderedRequests.Select(request =>
        {
            var dto = ToDto(request, employeeId,
                scheduleEditLockService.IsLocked(request.Schedule.ContainerId, request.ScheduleId),
                GetPeriodSlots(employeeSlots, request.Schedule.Year, request.Schedule.Month));
            return PopulateExchangeDto(request, employeeId, dto, exchangeContext);
        }).ToList();
        await ApplyContractReadPoliciesAsync(orderedRequests, dtos, employeeId, cancellationToken);
        return dtos;
    }

    public Task<ShiftSwapDto> Create(int employeeId, HttpContext context, CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken)
        => CreateCoreAsync(employeeId, context, request, cancellationToken);

    private async Task<ShiftSwapDto> CreateCoreAsync(int employeeId, HttpContext context, CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken, ReviewExecution? review = null)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var sqliteTransaction = review is null ? ((SqliteConnection)db.Database.GetDbConnection()).BeginTransaction(deferred: true) : null;
        await using var createTransaction = sqliteTransaction is null ? null : await db.Database.UseTransactionAsync(sqliteTransaction, cancellationToken);
        var validated = await ValidateOfferAsync(request, employeeId, cancellationToken).ConfigureAwait(false);
        var schedule = validated.Source.Schedule;
        var slot = validated.Source;
        var offeredPeriod = validated.Period;
        var targetEmployeeId = request.TargetEmployeeId;
        var minimum = await EmploymentSwapRules.CheckOutgoingMinimumAsync(employeeId, schedule, offeredPeriod,
            validated.Return?.Schedule, validated.ReturnPeriod, policy, cancellationToken);
        if (minimum?.RequiresException == true && review?.Approval.Kind != "below_minimum_give") throw EmploymentSwapRules.GiveApprovalRequired();

        var model = new ShiftSwapRequestModel
        {
            ApprovedEmploymentExceptionApprovalId = review?.Approval.Kind == "below_minimum_give" ? review.Approval.Id : null,
            RequestKind = validated.Kind,
            AllowPartialAcceptance = request.AllowPartialAcceptance,
            ArchivedViewsJson = validated.Return is null ? null : ShiftSwapArchive.CaptureReservation(validated.Source, validated.Return),
            ReturnScheduleId = validated.Return?.ScheduleId,
            ReturnScheduleSlotId = validated.Return?.Id,
            ReturnFromTime = validated.ReturnPeriod?.FromTime,
            ReturnToTime = validated.ReturnPeriod?.ToTime,
            ScheduleId = schedule.Id,
            ScheduleSlotId = slot.Id,
            OfferedFromTime = offeredPeriod.FromTime,
            OfferedToTime = offeredPeriod.ToTime,
            FromEmployeeId = employeeId,
            TargetEmployeeId = targetEmployeeId,
            Visibility = targetEmployeeId.HasValue ? ShiftSwapVisibility.Private : ShiftSwapVisibility.Public,
            Status = ShiftSwapStatus.Open,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.ShiftSwapRequests.Add(model);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            if (createTransaction is not null) await createTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            if (validated.Return is not null && await db.ShiftSwapRequests.AsNoTracking().AnyAsync(swap =>
                swap.ReturnScheduleSlotId == validated.Return.Id && swap.Status == ShiftSwapStatus.Open, cancellationToken))
                throw new ValidationException("This return shift already has an open exchange. Refresh and choose another shift.");
            if (!await db.ShiftSwapRequests.AsNoTracking()
                .AnyAsync(swap => swap.ScheduleSlotId == slot.Id && swap.Status == ShiftSwapStatus.Open, cancellationToken)
                .ConfigureAwait(false))
                throw;
            throw ValidationException.ForField(nameof(request.ScheduleSlotId), "This shift already has an open swap offer.");
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception))
        {
            if (createTransaction is not null) await createTransaction.RollbackAsync(cancellationToken);
            if (review is not null) throw new ContractShiftConflictException("CONTRACT_CONCURRENT_CHANGE", "This swap changed during another request. Refresh and try again.");
            await using var latestState = await db.Database.BeginTransactionAsync(cancellationToken);
            if (await db.ShiftSwapRequests.AsNoTracking().AnyAsync(swap => swap.ScheduleSlotId == slot.Id && swap.Status == ShiftSwapStatus.Open, cancellationToken))
                throw ValidationException.ForField(nameof(request.ScheduleSlotId), "This shift already has an open swap offer.");
            throw new ValidationException("This swap changed during another request. Refresh and try again.");
        }

        var created = await BuildSwapRequestQuery()
            .FirstAsync(swap => swap.Id == model.Id, cancellationToken)
            .ConfigureAwait(false);

        if (createTransaction is not null) await createTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        await DispatchAsync(context, review, () => workflowLogService
            .LogAsync(
                context.User,
                $"Published a shift swap from schedule \"{created.Schedule.Name}\" at {created.Schedule.Shop?.Name ?? "the assigned shop"} for {created.Schedule.Year}-{created.Schedule.Month:00}-{slot.DayOfMonth:00}, {offeredPeriod.FromTime}-{offeredPeriod.ToTime}; offered to {(created.TargetEmployeeId.HasValue ? GetEmployeeName(created.TargetEmployee, created.TargetEmployeeId.Value) : "all eligible employees") }.",
                cancellationToken)).ConfigureAwait(false);
        await DispatchAsync(context, review, () => realtimeNotifier
            .NotifyShiftSwapsChangedAsync(created.Schedule.ContainerId, created.ScheduleId, created.ScheduleId, "employee-swap-created", created.Id)).ConfigureAwait(false);

        var employeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            created.Schedule.Year,
            created.Schedule.Month,
            cancellationToken).ConfigureAwait(false);

        return await BuildExchangeDtoAsync(created, employeeId, ToDto(created, employeeId, false, employeeMonthSlots), cancellationToken);
    }

    public Task<ShiftSwapDto> Accept(int employeeId, HttpContext context, int id, CancellationToken cancellationToken, AcceptEmployeeShiftSwapRequest? request = null)
        => AcceptCoreAsync(employeeId, context, id, cancellationToken, request);

    private async Task<ShiftSwapDto> AcceptCoreAsync(int employeeId, HttpContext context, int id, CancellationToken cancellationToken, AcceptEmployeeShiftSwapRequest? request = null, ReviewExecution? review = null)
    {
        IDbContextTransaction transaction;
        try
        {
            transaction = review?.Transaction ?? await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception))
        {
            throw new ValidationException("This swap changed during another request. Refresh and try again.");
        }
        await using var transactionLifetime = review is null ? transaction : null;
        var swap = await BuildSwapRequestQuery()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift swap with id {id} was not found.");

        if (swap.RequestKind == ShiftSwapRequestKind.Exchange) {
            if ((request?.FromTime is null) != (request?.ToTime is null)) throw ValidationException.ForField("FromTime", "Supply both start and end times.");
            if (request?.FromTime is not null && NormalizePeriod(request.FromTime, request.ToTime!, "FromTime", "ToTime") != GetSwapPeriod(swap, swap.ScheduleSlot!))
                throw new ValidationException("This offer must be accepted for the whole published period.");
            return await AcceptExchangeAsync(swap, employeeId, context, transaction, cancellationToken, review).ConfigureAwait(false);
        }

        if (swap.Schedule.PublicationStatus != SchedulePublicationStatus.Public)
        {
            throw new ValidationException("This swap belongs to a schedule that is no longer public.");
        }

        if (swap.Status != ShiftSwapStatus.Open)
            throw new ValidationException("This swap offer is no longer open.");
        if (await HasOtherPendingClaimAsync(swap.Id, review?.Approval.Id, cancellationToken)) throw OfferPendingApproval();
        EnsureSwapAllowed(swap.Schedule);

        if (swap.Visibility == ShiftSwapVisibility.Public)
        {
            var sharesContainer = await db.Schedules.AnyAsync(schedule =>
                schedule.ContainerId == swap.Schedule.ContainerId &&
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId),
                cancellationToken).ConfigureAwait(false);
            if (!sharesContainer)
            {
                throw new ValidationException("This swap belongs to another container.");
            }
        }

        EnsureScheduleIsNotLocked(swap.Schedule);
        var slot = swap.Schedule.Slots.FirstOrDefault(slot => slot.Id == swap.ScheduleSlotId)
            ?? throw new ValidationException("This shift is no longer available.");
        EnsureValidSwapDate(slot);
        var offeredPeriod = GetSwapPeriod(swap, slot);
        EnsurePeriodWithinSlot(slot, offeredPeriod);
        var employeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            swap.Schedule.Year,
            swap.Schedule.Month,
            cancellationToken).ConfigureAwait(false);
        var acceptanceUnavailableReason = GetAcceptanceUnavailableReason(
            swap,
            employeeId,
            isScheduleLocked: false,
            employeeMonthSlots,
            slot,
            offeredPeriod);
        if (acceptanceUnavailableReason is not null)
        {
            throw new ValidationException(acceptanceUnavailableReason);
        }
        var claimedPeriod = ValidateClaimedPeriod(swap, offeredPeriod, request, employeeMonthSlots, employeeId);
        await ValidateEmploymentPeriodsAsync(slot, claimedPeriod, swap.IsManagerCreated ? null : swap.FromEmployeeId, employeeId, cancellationToken);
        if (review is null && await RequiresPickupApprovalAsync(swap, employeeId, cancellationToken))
            throw EmploymentSwapRules.ClaimApprovalRequired();
        if (!swap.IsManagerCreated && swap.FromEmployeeId is { } giver &&
            (await EmploymentSwapRules.CheckOutgoingMinimumAsync(giver, swap.Schedule, claimedPeriod, null, null, policy, cancellationToken))?.RequiresException == true &&
            !await HasApprovedExceptionAsync(swap, giver, claimedPeriod, cancellationToken))
            throw EmploymentSwapRules.GiveApprovalRequired();
        var acceptingEmployee = await db.Employees
            .AsNoTracking()
            .FirstAsync(employee => employee.Id == employeeId, cancellationToken)
            .ConfigureAwait(false);
        var acceptingEmployeeName = GetEmployeeName(acceptingEmployee, employeeId);
        var manualColumnName = swap.IsManagerCreated
            ? GraphManualColumnLabelResolver.Resolve(swap.Schedule.Note, swap.ManualColumnId)
            : null;
        var beforeSnapshotJson = ShiftSwapHistorySnapshotBuilder.BuildJson(
            swap.Schedule,
            swap.Schedule.Slots,
            swap,
            employeeId,
            acceptingEmployeeName);

        var residualOffers = new List<ShiftSwapRequestModel>();
        var acceptedAtUtc = DateTimeOffset.UtcNow;
        var writingVersion = false;
        try
        {
            EnsureScheduleIsNotLocked(swap.Schedule);
            EnsurePeriodWithinSlot(slot, offeredPeriod);
            ValidateClaimedPeriod(swap, offeredPeriod, request, employeeMonthSlots, employeeId);
            writingVersion = true;
            await versionCommitService.CommitAsync(swap.ScheduleId, null, "System (before accepted swap)", cancellationToken).ConfigureAwait(false);
            writingVersion = false;
            var affectedRows = await db.ShiftSwapRequests
                .Where(request => request.Id == id && request.Status == ShiftSwapStatus.Open)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(request => request.Status, ShiftSwapStatus.Accepted)
                    .SetProperty(request => request.AcceptedByEmployeeId, employeeId)
                    .SetProperty(request => request.AcceptedAtUtc, acceptedAtUtc), cancellationToken)
                .ConfigureAwait(false);
            if (affectedRows != 1)
                throw new ValidationException("This swap offer is no longer open.");

            swap.Status = ShiftSwapStatus.Accepted;
            swap.AcceptedByEmployeeId = employeeId;
            swap.AcceptedAtUtc = acceptedAtUtc;

            if (swap.IsManagerCreated) {
                if (swap.ManualColumnId is not { } columnId) throw new ValidationException("The manual shift metadata is invalid or the cell is missing.");
                var rawCell = GraphManualColumnLabelResolver.ResolveCell(swap.Schedule.Note, columnId, slot.DayOfMonth)
                    ?? throw new ValidationException("The manual shift metadata is invalid or the cell is missing.");
                var open = await db.ShiftSwapRequests.Include(r => r.ScheduleSlot).Where(r => r.ScheduleId == swap.ScheduleId && r.IsManagerCreated &&
                    r.ManualColumnId == columnId && r.Status == ShiftSwapStatus.Open && r.ScheduleSlot!.DayOfMonth == slot.DayOfMonth).ToListAsync(cancellationToken);
                var reserved = new List<ShiftSwapPeriod> { offeredPeriod };
                if (!ManualShiftDemandRules.CanReserve(rawCell, [], offeredPeriod)) throw new ValidationException("The manual shift demand is no longer available.");
                foreach (var other in open.Where(r => r.Id != swap.Id)) {
                    var period = GetSwapPeriod(other, other.ScheduleSlot!);
                    if (!ManualShiftDemandRules.CanReserve(rawCell, reserved, period)) throw new ValidationException("The manual shift demand is no longer available.");
                    reserved.Add(period);
                }
                swap.Schedule.Note = ConsumeManualColumnPeriodFromNote(swap.Schedule.Note, columnId, slot.DayOfMonth, claimedPeriod);
                swap.Schedule.Note = UpsertManualColumnBackgroundColorInNote(swap.Schedule.Note, columnId,
                    slot.DayOfMonth, ShiftSwapHighlightRules.ToArgb(swap.Schedule.AcceptedManualShiftHighlightColor));
            }
            var remainingSlots = ApplyAcceptedSwapPeriod(slot, swap.IsManagerCreated ? null : swap.FromEmployeeId, employeeId, claimedPeriod, swap.Schedule.Slots.ToList());
            foreach (var remainderSlot in remainingSlots) db.ScheduleSlots.Add(remainderSlot);
            swap.OfferedFromTime = claimedPeriod.FromTime;
            swap.OfferedToTime = claimedPeriod.ToTime;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            var residualPeriods = new List<ShiftSwapPeriod>();
            if (string.CompareOrdinal(offeredPeriod.FromTime, claimedPeriod.FromTime) < 0) residualPeriods.Add(new(offeredPeriod.FromTime, claimedPeriod.FromTime));
            if (string.CompareOrdinal(claimedPeriod.ToTime, offeredPeriod.ToTime) < 0) residualPeriods.Add(new(claimedPeriod.ToTime, offeredPeriod.ToTime));
            foreach (var residual in residualPeriods) {
                var source = remainingSlots.Single(s => string.CompareOrdinal(s.FromTime, residual.FromTime) <= 0 && string.CompareOrdinal(s.ToTime, residual.ToTime) >= 0);
                var residualOffer = new ShiftSwapRequestModel {
                    ApprovedEmploymentExceptionApprovalId = swap.ApprovedEmploymentExceptionApprovalId,
                    ScheduleId = swap.ScheduleId, ScheduleSlotId = source.Id, FromEmployeeId = swap.FromEmployeeId,
                    IsManagerCreated = swap.IsManagerCreated, ManualColumnId = swap.ManualColumnId,
                    TargetEmployeeId = swap.TargetEmployeeId, Visibility = swap.Visibility,
                    AllowPartialAcceptance = swap.AllowPartialAcceptance, RequestKind = ShiftSwapRequestKind.Give,
                    OfferedFromTime = residual.FromTime, OfferedToTime = residual.ToTime,
                    CreatedAtUtc = acceptedAtUtc, Status = ShiftSwapStatus.Open,
                };
                db.ShiftSwapRequests.Add(residualOffer);
                residualOffers.Add(residualOffer);
            }

            EnsureScheduleEmployee(swap.Schedule, employeeId);
            await HighlightAcceptedSwapCellsAsync(swap, employeeId, cancellationToken).ConfigureAwait(false);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var afterSlots = await db.ScheduleSlots
                .AsNoTracking()
                .Where(scheduleSlot => scheduleSlot.ScheduleId == swap.ScheduleId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var afterSnapshotJson = ShiftSwapHistorySnapshotBuilder.BuildJson(
                swap.Schedule,
                afterSlots,
                swap,
                employeeId,
                acceptingEmployeeName);

            db.ShiftSwapHistories.Add(new ShiftSwapHistoryModel
            {
                SourceShiftSwapRequestId = swap.Id,
                ScheduleId = swap.ScheduleId,
                ScheduleSlotId = slot.Id,
                ScheduleName = swap.Schedule.Name,
                ContainerName = swap.Schedule.Container?.Name ?? string.Empty,
                ShopName = swap.Schedule.Shop?.Name ?? string.Empty,
                Year = swap.Schedule.Year,
                Month = swap.Schedule.Month,
                DayOfMonth = slot.DayOfMonth,
                FromTime = claimedPeriod.FromTime,
                ToTime = claimedPeriod.ToTime,
                FromEmployeeId = swap.FromEmployeeId,
                FromEmployeeName = manualColumnName ?? GetEmployeeName(swap.FromEmployee, swap.FromEmployeeId),
                TargetEmployeeId = swap.TargetEmployeeId,
                TargetEmployeeName = swap.TargetEmployeeId.HasValue
                    ? GetEmployeeName(swap.TargetEmployee, swap.TargetEmployeeId.Value)
                    : null,
                AcceptedByEmployeeId = employeeId,
                AcceptedByEmployeeName = acceptingEmployeeName,
                CreatedAtUtc = swap.CreatedAtUtc,
                AcceptedAtUtc = acceptedAtUtc,
                IsManagerCreated = swap.IsManagerCreated,
                ManualColumnId = swap.ManualColumnId,
                ManualColumnName = manualColumnName,
                BeforeSnapshotJson = beforeSnapshotJson,
                AfterSnapshotJson = afterSnapshotJson,
            });
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            writingVersion = true;
            var acceptedVersion = await versionCommitService.CommitAsync(swap.ScheduleId, null, $"Swap accepted: {acceptingEmployeeName}", cancellationToken).ConfigureAwait(false);
            if (acceptedVersion is null)
                throw new InvalidOperationException("The accepted swap did not create a schedule version.");
            writingVersion = false;
            if (review is null) await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (writingVersion && exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            if (review is null) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw new ValidationException("This schedule's version history changed during another request. Refresh and try again.");
        }
        catch (DbUpdateException exception) when (!writingVersion && exception.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            if (review is null) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw new ValidationException("This swap could not be accepted because the resulting schedule would conflict.");
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception))
        {
            if (review is null) await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw new ValidationException("This swap changed during another request. Refresh and try again.");
        }

        var accepted = await BuildSwapRequestQuery()
            .FirstAsync(request => request.Id == id, cancellationToken)
            .ConfigureAwait(false);

        await DispatchAsync(context, review, () => workflowLogService
            .LogAsync(
                context.User,
                $"Accepted {(accepted.FromEmployeeId.HasValue ? $"{GetEmployeeName(accepted.FromEmployee, accepted.FromEmployeeId.Value)}'s shift" : "an open shift")} from schedule \"{accepted.Schedule.Name}\" for {accepted.Schedule.Year}-{accepted.Schedule.Month:00}-{accepted.ScheduleSlot!.DayOfMonth:00}, {claimedPeriod.FromTime}-{claimedPeriod.ToTime}.",
                cancellationToken)).ConfigureAwait(false);
        await DispatchAsync(context, review, () => realtimeNotifier
            .NotifyScheduleChangedAsync(accepted.Schedule.ContainerId, accepted.ScheduleId, "employee-swap-accepted")).ConfigureAwait(false);
        await DispatchAsync(context, review, () => realtimeNotifier
            .NotifyShiftSwapsChangedAsync(accepted.Schedule.ContainerId, accepted.ScheduleId, accepted.ScheduleId, "employee-swap-accepted", accepted.Id)).ConfigureAwait(false);

        foreach (var residual in residualOffers)
            await DispatchAsync(context, review, () => realtimeNotifier.NotifyShiftSwapsChangedAsync(
                accepted.Schedule.ContainerId, accepted.ScheduleId, accepted.ScheduleId,
                residual.IsManagerCreated ? "manager-manual-shift-offer-created" : "employee-swap-created", residual.Id)).ConfigureAwait(false);

        var updatedEmployeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            accepted.Schedule.Year,
            accepted.Schedule.Month,
            cancellationToken).ConfigureAwait(false);

        return ToDto(accepted, employeeId, isScheduleLocked: false, updatedEmployeeMonthSlots);
    }

    public async Task<ShiftSwapDto> Cancel(int employeeId, HttpContext context, int id, CancellationToken cancellationToken)
    {
        var swap = await BuildSwapRequestQuery()
            .FirstOrDefaultAsync(request => request.Id == id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Shift swap with id {id} was not found.");

        if (swap.FromEmployeeId != employeeId)
        {
            throw new ValidationException("Only the employee who opened this swap can cancel it.");
        }

        if (swap.Status != ShiftSwapStatus.Open)
        {
            throw new ValidationException("Only open swap offers can be cancelled.");
        }

        IDbContextTransaction cancelTransaction;
        try
        {
            cancelTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception))
        {
            throw new ValidationException("This swap changed during another request. Refresh and try again.");
        }
        await using var cancelTransactionLifetime = cancelTransaction;
        var cancelledSlot = swap.ScheduleSlot;
        var cancelledAtUtc = DateTimeOffset.UtcNow;
        try
        {
            var affectedRows = await db.ShiftSwapRequests
                .Where(request => request.Id == id && request.FromEmployeeId == employeeId && request.Status == ShiftSwapStatus.Open)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(request => request.Status, ShiftSwapStatus.Cancelled)
                    .SetProperty(request => request.CancelledAtUtc, cancelledAtUtc), cancellationToken)
                .ConfigureAwait(false);
            if (affectedRows != 1)
                throw new ValidationException("Only open swap offers can be cancelled.");
            await db.ContractShiftApprovalRequests.Where(x => x.ShiftSwapRequestId == id && x.Kind == "claim" && x.Status == "pending")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "expired")
                    .SetProperty(x => x.FailureReason, "The offer was cancelled by its owner."), cancellationToken);
            swap.Status = ShiftSwapStatus.Cancelled;
            swap.CancelledAtUtc = cancelledAtUtc;
            if (swap.RequestKind == ShiftSwapRequestKind.Exchange)
                await ShiftSwapArchive.PreserveCompletedAsync(db, swap.ScheduleId, cancellationToken, swap.Id);
            await cancelTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception))
        {
            throw new ValidationException("This swap changed during another request. Refresh and try again.");
        }
        await cancelTransaction.DisposeAsync();
        await PostCommitActions.RunAsync(context, () => realtimeNotifier.NotifyManagerDataChangedAsync(
            "schedule", $"{swap.Schedule.ContainerId}:{swap.ScheduleId}", "employee-swap-cancelled", swap.Schedule.ContainerId, swap.ScheduleId));

        await PostCommitActions.RunAsync(context, () => workflowLogService
            .LogAsync(
                context.User,
                $"Cancelled the shift swap from schedule \"{swap.Schedule.Name}\" for {swap.Schedule.Year}-{swap.Schedule.Month:00}-{cancelledSlot!.DayOfMonth:00}, {swap.OfferedFromTime ?? cancelledSlot.FromTime}-{swap.OfferedToTime ?? cancelledSlot.ToTime}.",
                cancellationToken)).ConfigureAwait(false);
        await PostCommitActions.RunAsync(context, () => realtimeNotifier
            .NotifyShiftSwapsChangedAsync(swap.Schedule.ContainerId, swap.ScheduleId, swap.ScheduleId, "employee-swap-cancelled", swap.Id)).ConfigureAwait(false);

        var employeeMonthSlots = await LoadPublishedEmployeeMonthSlotsAsync(
            employeeId,
            swap.Schedule.Year,
            swap.Schedule.Month,
            cancellationToken).ConfigureAwait(false);

        return await BuildExchangeDtoAsync(swap, employeeId, ToDto(
            swap,
            employeeId,
            scheduleEditLockService.IsLocked(swap.Schedule.ContainerId, swap.ScheduleId),
            employeeMonthSlots), cancellationToken);
    }

    public async Task<ShiftSwapReturnCandidatePageDto> GetReturnCandidates(int employeeId, HttpContext context, 
        int sourceScheduleId, int sourceSlotId,
        string fromTime, string toTime, int targetEmployeeId,
        CancellationToken cancellationToken, int page = 1, int pageSize = 30)
    {
        if (page < 1 || pageSize is < 1 or > 100 || page > int.MaxValue / pageSize)
            throw new ValidationException("Invalid candidate page.");
        var sourceRequest = new CreateEmployeeShiftSwapRequest
        {
            ScheduleId = sourceScheduleId, ScheduleSlotId = sourceSlotId,
            FromTime = fromTime, ToTime = toTime, TargetEmployeeId = targetEmployeeId,
        };
        var source = await ValidateOfferAsync(sourceRequest, employeeId, cancellationToken);
        var candidates = await db.ScheduleSlots.AsNoTracking().Include(slot => slot.Schedule).ThenInclude(schedule => schedule.Shop)
            .Where(slot => slot.EmployeeId == targetEmployeeId && slot.Status == SlotStatus.ASSIGNED &&
                slot.Schedule.ContainerId == source.Source.Schedule.ContainerId &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public && slot.Schedule.AllowSwap)
            .OrderBy(slot => slot.Schedule.Year).ThenBy(slot => slot.Schedule.Month).ThenBy(slot => slot.DayOfMonth)
            .ThenBy(slot => slot.FromTime).ThenBy(slot => slot.ScheduleId).ThenBy(slot => slot.Id)
            .Skip((page - 1) * pageSize).Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasMore = candidates.Count > pageSize;
        candidates = candidates.Take(pageSize).ToList();
        var assignments = await LoadAssignmentsAsync([employeeId, targetEmployeeId],
            candidates.Select(slot => slot.Schedule).Append(source.Source.Schedule).ToList(), cancellationToken);
        var slotIds = candidates.Select(slot => slot.Id).Append(source.Source.Id).ToArray();
        var offers = await LoadOpenOffersAsync(slotIds, cancellationToken);
        var settings = await policy.GetSettingsAsync(cancellationToken);
        var types = await policy.GetContractTypesAsync([employeeId, targetEmployeeId], cancellationToken);
        var restricted = settings.RestrictSwapsToEightHours && types.Values.Contains(EmploymentContractTypes.OPrace);
        var items = candidates.Select(candidate =>
        {
            var input = ExchangeInput(employeeId, targetEmployeeId, source.Source, source.Period,
                candidate, new(candidate.FromTime, candidate.ToTime), assignments, offers);
            var windows = ShiftSwapExchangeRules.FindEligibleReturnWindows(input, candidate);
            if (restricted)
                windows = windows.Where(window => EmploymentSwapRules.IsWholeEightHourShift(candidate, window)).ToList();
            var dto = CandidateSummary(candidate, new(candidate.FromTime, candidate.ToTime));
            dto.RequiresWholeShift = restricted;
            dto.CanSelect = windows.Count > 0;
            dto.UnavailableReason = dto.CanSelect ? null : restricted ? EmploymentSwapRules.EightHourReason : ShiftSwapExchangeRules.GetExchangeConflictReason(input) ?? "No compatible return period.";
            dto.SuggestedFromTime = windows.FirstOrDefault().FromTime;
            dto.SuggestedToTime = windows.FirstOrDefault().ToTime;
            dto.EligibleWindows = windows.Select(window => new ShiftSwapEligibleWindowDto(window.FromTime, window.ToTime)).ToList();
            return dto;
        }).ToList();
        return new ShiftSwapReturnCandidatePageDto { Items = items, Page = page, PageSize = pageSize, HasMore = hasMore };
    }

    public async Task<ShiftSwapOfferPreviewDto> Preview(int employeeId, HttpContext context, 
        CreateEmployeeShiftSwapRequest request, CancellationToken cancellationToken)
    {
        var valid = await ValidateOfferAsync(request, employeeId, cancellationToken);
        var minimum = await EmploymentSwapRules.CheckOutgoingMinimumAsync(employeeId, valid.Source.Schedule, valid.Period,
            valid.Return?.Schedule, valid.ReturnPeriod, policy, cancellationToken);
        var target = request.TargetEmployeeId is { } id ? await db.Employees.AsNoTracking().SingleAsync(e => e.Id == id, cancellationToken) : null;
        return new ShiftSwapOfferPreviewDto
        {
            RequiresManagerException = minimum?.RequiresException == true,
            ManagerExceptionReason = minimum?.RequiresException == true ? EmploymentSwapRules.GiveApprovalRequired().Message : null,
            MinimumMinutes = minimum?.Minimum.MinimumMinutes, ProjectedMinutes = minimum?.ProjectedMinutes,
            AllowPartialAcceptance = request.AllowPartialAcceptance,
            RequestKind = valid.Kind == ShiftSwapRequestKind.Exchange ? "exchange" : "give",
            TargetEmployeeId = request.TargetEmployeeId,
            TargetEmployeeName = target is null ? "Everyone" : GetEmployeeName(target, target.Id),
            Source = CandidateSummary(valid.Source, valid.Period),
            Return = valid.Return is null ? null : CandidateSummary(valid.Return, valid.ReturnPeriod!.Value),
            IsValid = true,
            ProjectedMonths = ShiftSwapExchangeRules.CalculateProjectedOwnerStats(valid.Assignments, employeeId,
                valid.Source, valid.Period, valid.Return, valid.ReturnPeriod),
        };
    }

    private sealed record ValidatedOffer(ShiftSwapRequestKind Kind, ScheduleSlotModel Source, ShiftSwapPeriod Period,
        ScheduleSlotModel? Return, ShiftSwapPeriod? ReturnPeriod, IReadOnlyList<ScheduleSlotModel> Assignments);

    private async Task<ValidatedOffer> ValidateOfferAsync(CreateEmployeeShiftSwapRequest request, int employeeId,
        CancellationToken ct, int? requestId = null, ShiftSwapArchive.ExchangeReservationSnapshot? expected = null, bool forApproval = false)
    {
        var kind = request.RequestKind switch
        {
            null or "give" => ShiftSwapRequestKind.Give,
            "exchange" => ShiftSwapRequestKind.Exchange,
            _ => throw ValidationException.ForField(nameof(request.RequestKind), "Invalid request kind."),
        };
        if (kind == ShiftSwapRequestKind.Exchange && request.AllowPartialAcceptance)
            throw ValidationException.ForField(nameof(request.AllowPartialAcceptance), "Exchange offers cannot allow partial acceptance.");
        var hasReturn = request.ReturnScheduleId.HasValue || request.ReturnScheduleSlotId.HasValue ||
            request.ReturnFromTime is not null || request.ReturnToTime is not null;
        if (kind == ShiftSwapRequestKind.Give && hasReturn)
            throw new ValidationException("Give requests cannot contain return shift details.");
        if (kind == ShiftSwapRequestKind.Exchange && (request.TargetEmployeeId is not > 0 ||
            request.ReturnScheduleId is not > 0 || request.ReturnScheduleSlotId is not > 0 ||
            string.IsNullOrWhiteSpace(request.ReturnFromTime) || string.IsNullOrWhiteSpace(request.ReturnToTime)))
            throw new ValidationException("An exchange requires a recipient and a complete return shift period.");
        var schedule = await LoadEmployeePublishedScheduleAsync(request.ScheduleId, employeeId, ct);
        EnsureSwapAllowed(schedule);
        EnsureScheduleIsNotLocked(schedule, forApproval);
        var slot = schedule.Slots.FirstOrDefault(slot => slot.Id == request.ScheduleSlotId)
            ?? throw ValidationException.ForField(nameof(request.ScheduleSlotId), "Shift was not found in this schedule.");
        if (slot.EmployeeId != employeeId || slot.Status != SlotStatus.ASSIGNED)
            throw ValidationException.ForField(nameof(request.ScheduleSlotId), "You can only offer your own assigned shift.");
        EnsureValidSwapDate(slot);
        var period = ResolveRequestedPeriod(request, slot);
        if (request.TargetEmployeeId == employeeId)
            throw ValidationException.ForField(nameof(request.TargetEmployeeId), "Choose another employee or publish this swap for everyone.");
        if (request.TargetEmployeeId is <= 0 ||
            (request.TargetEmployeeId.HasValue && !await db.Employees.AnyAsync(e => e.Id == request.TargetEmployeeId && !e.IsDeleted, ct)))
            throw ValidationException.ForField(nameof(request.TargetEmployeeId), "The selected employee was not found.");
        ScheduleSlotModel? returnSlot = null;
        ShiftSwapPeriod? returnPeriod = null;
        if (kind == ShiftSwapRequestKind.Exchange)
        {
            var returnSchedule = await db.Schedules.Include(s => s.Shop).Include(s => s.Employees).Include(s => s.Slots)
                .SingleOrDefaultAsync(s => s.Id == request.ReturnScheduleId && s.ContainerId == schedule.ContainerId &&
                    s.PublicationStatus == SchedulePublicationStatus.Public && s.AllowSwap, ct)
                ?? throw new ValidationException("Return shift unavailable.");
            EnsureScheduleIsNotLocked(returnSchedule, forApproval);
            returnSlot = returnSchedule.Slots.FirstOrDefault(s => s.Id == request.ReturnScheduleSlotId && s.EmployeeId == request.TargetEmployeeId)
                ?? throw new ValidationException("Return shift unavailable.");
            returnPeriod = NormalizePeriod(request.ReturnFromTime!, request.ReturnToTime!, "ReturnFromTime", "ReturnToTime");
        }
        var slots = await LoadAssignmentsAsync([employeeId, request.TargetEmployeeId ?? employeeId],
            returnSlot is null ? [schedule] : [schedule, returnSlot.Schedule], ct);
        var offers = await LoadOpenOffersAsync(returnSlot is null ? [slot.Id] : [slot.Id, returnSlot.Id], ct);
        if (kind == ShiftSwapRequestKind.Exchange)
        {
            var reason = ShiftSwapExchangeRules.GetExchangeConflictReason(ExchangeInput(employeeId, request.TargetEmployeeId!.Value,
                slot, period, returnSlot!, returnPeriod!.Value, slots, offers, requestId, expected));
            if (reason is not null) throw new ValidationException(reason);
        }
        else if (offers.Any(offer => offer.Id != requestId))
            throw ValidationException.ForField(nameof(request.ScheduleSlotId), "This shift already has an open swap offer.");
        await ValidateEmploymentPeriodsAsync(slot, period, employeeId, request.TargetEmployeeId, ct);
        if (returnSlot is not null)
            await ValidateEmploymentPeriodsAsync(returnSlot, returnPeriod!.Value, employeeId, request.TargetEmployeeId, ct);
        return new(kind, slot, period, returnSlot, returnPeriod, slots);
    }

    private async Task ValidateEmploymentPeriodsAsync(ScheduleSlotModel slot, ShiftSwapPeriod period,
        int? ownerId, int? receiverId, CancellationToken ct)
    {
        var ids = new[] { ownerId, receiverId }.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var settings = await policy.GetSettingsAsync(ct);
        if (!settings.RestrictSwapsToEightHours) return;
        var types = await policy.GetContractTypesAsync(ids, ct);
        foreach (var id in ids)
            if (types.TryGetValue(id, out var type) && type == EmploymentContractTypes.OPrace)
                EmploymentSwapRules.ValidateWholeEightHourShift(slot, period, id);
    }

    private ShiftSwapExchangeRules.ExchangeInput ExchangeInput(int ownerId, int targetId, ScheduleSlotModel source,
        ShiftSwapPeriod sourcePeriod, ScheduleSlotModel back, ShiftSwapPeriod returnPeriod,
        IReadOnlyList<ScheduleSlotModel> assignments, IReadOnlyList<ShiftSwapRequestModel> offers, int? requestId = null,
        ShiftSwapArchive.ExchangeReservationSnapshot? expected = null)
        => new(ownerId, targetId, source, sourcePeriod, back, returnPeriod, assignments, offers,
            IsScheduleLocked(source.Schedule),
            IsScheduleLocked(back.Schedule), requestId, expected?.Source, expected?.Return);

    private async Task<List<ScheduleSlotModel>> LoadAssignmentsAsync(int[] employeeIds, IReadOnlyList<ScheduleModel> schedules, CancellationToken ct)
    {
        var periods = schedules.Select(s => s.Year * 100 + s.Month).Distinct().ToArray();
        return await db.ScheduleSlots.AsNoTracking().Include(slot => slot.Schedule)
            .Where(slot => slot.EmployeeId.HasValue && employeeIds.Contains(slot.EmployeeId.Value) && slot.Status == SlotStatus.ASSIGNED &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public && periods.Contains(slot.Schedule.Year * 100 + slot.Schedule.Month))
            .ToListAsync(ct);
    }

    private async Task<List<ShiftSwapRequestModel>> LoadOpenOffersAsync(int[] slotIds, CancellationToken ct)
        => await db.ShiftSwapRequests.AsNoTracking().Where(offer => offer.Status == ShiftSwapStatus.Open &&
            ((offer.ScheduleSlotId.HasValue && slotIds.Contains(offer.ScheduleSlotId.Value)) ||
             (offer.ReturnScheduleSlotId.HasValue && slotIds.Contains(offer.ReturnScheduleSlotId.Value)))).ToListAsync(ct);

    private static ShiftSwapReturnCandidateDto CandidateSummary(ScheduleSlotModel slot, ShiftSwapPeriod period) => new()
    {
        ScheduleId = slot.ScheduleId, ScheduleName = slot.Schedule.Name, ShopName = slot.Schedule.Shop?.Name ?? string.Empty,
        Year = slot.Schedule.Year, Month = slot.Schedule.Month, DayOfMonth = slot.DayOfMonth, SlotId = slot.Id,
        FromTime = period.FromTime, ToTime = period.ToTime, DurationMinutes = ShiftSwapExchangeRules.DurationMinutes(period), CanSelect = true,
    };

    private sealed record ExchangeReadContext(List<ScheduleModel> Schedules, List<ScheduleSlotModel> Assignments,
        List<ShiftSwapRequestModel> Offers, List<ShiftSwapHistoryModel> Histories);

    private async Task<ExchangeReadContext> LoadExchangeReadContextAsync(IReadOnlyList<ShiftSwapRequestModel> swaps, CancellationToken ct)
    {
        var exchanges = swaps.Where(s => s.RequestKind == ShiftSwapRequestKind.Exchange && !ShiftSwapArchive.IsArchived(s)).ToList();
        if (exchanges.Count == 0) return new([], [], [], []);
        var returnIds = exchanges.Where(s => s.ReturnScheduleId.HasValue).Select(s => s.ReturnScheduleId!.Value).Distinct().ToArray();
        var schedules = await db.Schedules.AsNoTracking().Include(s => s.Shop).Include(s => s.Slots).Where(s => returnIds.Contains(s.Id)).ToListAsync(ct);
        var employeeIds = exchanges.SelectMany(s => new[] { s.FromEmployeeId, s.TargetEmployeeId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var assignments = await LoadAssignmentsAsync(employeeIds, schedules.Concat(exchanges.Select(s => s.Schedule)).ToList(), ct);
        var slotIds = exchanges.SelectMany(s => new[] { s.ScheduleSlotId, s.ReturnScheduleSlotId }).Where(id => id.HasValue).Select(id => id!.Value).ToArray();
        var offers = await LoadOpenOffersAsync(slotIds, ct);
        var ids = exchanges.Select(s => s.Id).ToArray();
        var histories = await db.ShiftSwapHistories.AsNoTracking().Where(h => ids.Contains(h.SourceShiftSwapRequestId)).ToListAsync(ct);
        return new(schedules, assignments, offers, histories);
    }

    private async Task<ShiftSwapDto> BuildExchangeDtoAsync(ShiftSwapRequestModel swap, int employeeId, ShiftSwapDto dto, CancellationToken ct)
        => PopulateExchangeDto(swap, employeeId, dto, await LoadExchangeReadContextAsync([swap], ct));

    private ShiftSwapDto PopulateExchangeDto(ShiftSwapRequestModel swap, int employeeId, ShiftSwapDto dto, ExchangeReadContext context)
    {
        if (swap.RequestKind != ShiftSwapRequestKind.Exchange || ShiftSwapArchive.IsArchived(swap)) return dto;
        var back = context.Schedules.FirstOrDefault(s => s.Id == swap.ReturnScheduleId)?.Slots.FirstOrDefault(s => s.Id == swap.ReturnScheduleSlotId);
        dto.IsScheduleLocked |= back is not null && IsScheduleLocked(back.Schedule);
        var history = context.Histories.FirstOrDefault(h => h.SourceShiftSwapRequestId == swap.Id);
        ShiftSwapRules.PopulateReturnSummary(dto, swap, back, history);
        dto.CanAccept = false;
        dto.CanAcceptFull = false;
        dto.EligibleClaimWindows = [];
        var canProject = true;
        if (swap.Status == ShiftSwapStatus.Open)
        {
            var source = swap.ScheduleSlot;
            var reason = source is null || back is null ? "Return shift unavailable." :
                ShiftSwapExchangeRules.GetExchangeConflictReason(ExchangeInput(swap.FromEmployeeId ?? 0, swap.TargetEmployeeId ?? 0,
                    source, GetSwapPeriod(swap, source), back, new(swap.ReturnFromTime!, swap.ReturnToTime!),
                    context.Assignments, context.Offers, swap.Id, ShiftSwapArchive.ReadReservation(swap)));
            canProject = reason is null;
            if (reason is null && employeeId != swap.TargetEmployeeId)
                reason = employeeId == swap.FromEmployeeId ? "This is your own swap offer." : "This private swap offer is for another employee.";
            dto.CanAccept = reason is null;
            dto.CanAcceptFull = dto.CanAccept;
            dto.AcceptanceUnavailableReason = reason;
        }
        if (canProject) ShiftSwapExchangeRules.PopulateProjectedStats(dto, swap, employeeId, back, context.Assignments);
        else
        {
            dto.CurrentEmployeeHoursAfter = dto.CurrentEmployeeHoursBefore;
            dto.CurrentEmployeeWorkDaysAfter = dto.CurrentEmployeeWorkDaysBefore;
            dto.CurrentEmployeeFreeDaysAfter = dto.CurrentEmployeeFreeDaysBefore;
        }
        return dto;
    }

    private async Task<ShiftSwapDto> AcceptExchangeAsync(ShiftSwapRequestModel swap, int employeeId, HttpContext context,
        IDbContextTransaction transaction, CancellationToken ct, ReviewExecution? review = null)
    {
        if (swap.Status != ShiftSwapStatus.Open) throw new ValidationException("This swap offer is no longer open.");
        if (await HasOtherPendingClaimAsync(swap.Id, review?.Approval.Id, ct)) throw OfferPendingApproval();
        if (swap.IsManagerCreated || swap.Visibility != ShiftSwapVisibility.Private || swap.TargetEmployeeId != employeeId || swap.FromEmployeeId is not > 0)
            throw new ValidationException("This private exchange is for another employee.");
        var valid = await ValidateOfferAsync(new CreateEmployeeShiftSwapRequest
        {
            RequestKind = "exchange", ScheduleId = swap.ScheduleId, ScheduleSlotId = swap.ScheduleSlotId ?? 0,
            FromTime = swap.OfferedFromTime, ToTime = swap.OfferedToTime, TargetEmployeeId = swap.TargetEmployeeId,
            ReturnScheduleId = swap.ReturnScheduleId, ReturnScheduleSlotId = swap.ReturnScheduleSlotId,
            ReturnFromTime = swap.ReturnFromTime, ReturnToTime = swap.ReturnToTime,
        }, swap.FromEmployeeId.Value, ct, swap.Id, ShiftSwapArchive.ReadReservation(swap));
        var source = valid.Source;
        var back = valid.Return!;
        if (review is null && await RequiresPickupApprovalAsync(swap, employeeId, ct))
            throw EmploymentSwapRules.ClaimApprovalRequired();
        var returnPeriod = valid.ReturnPeriod!.Value;
        var ownerId = swap.FromEmployeeId.Value;
        if ((await EmploymentSwapRules.CheckOutgoingMinimumAsync(ownerId, source.Schedule, valid.Period,
            back.Schedule, returnPeriod, policy, ct))?.RequiresException == true && !await HasApprovedExceptionAsync(swap, ownerId, valid.Period, ct) ||
            (await EmploymentSwapRules.CheckOutgoingMinimumAsync(employeeId, back.Schedule, returnPeriod,
            source.Schedule, valid.Period, policy, ct))?.RequiresException == true && review?.Approval.Kind != "claim")
            throw EmploymentSwapRules.GiveApprovalRequired();
        var name = GetEmployeeName(swap.TargetEmployee, employeeId);
        var schedules = new[] { source.Schedule, back.Schedule }.DistinctBy(s => s.Id).OrderBy(s => s.Id).ToList();
        var before = ShiftSwapHistorySnapshotBuilder.BuildExchangeJson(schedules, schedules.SelectMany(s => s.Slots), swap, employeeId, name);
        var acceptedAt = DateTimeOffset.UtcNow;
        try
        {
            foreach (var schedule in schedules)
                await versionCommitService.CommitAsync(schedule.Id, null, "System (before accepted exchange)", ct);
            var claimed = await db.ShiftSwapRequests.Where(s => s.Id == swap.Id && s.Status == ShiftSwapStatus.Open && s.TargetEmployeeId == employeeId)
                .ExecuteUpdateAsync(update => update.SetProperty(s => s.Status, ShiftSwapStatus.Accepted)
                    .SetProperty(s => s.AcceptedByEmployeeId, employeeId).SetProperty(s => s.AcceptedAtUtc, acceptedAt), ct);
            if (claimed != 1) throw new ValidationException("This swap offer is no longer open.");

            // Release both employee/time keys before assigning either incoming interval.
            var ids = new[] { source.Id, back.Id };
            await db.ScheduleSlots.Where(s => ids.Contains(s.Id)).ExecuteUpdateAsync(update =>
                update.SetProperty(s => s.EmployeeId, (int?)null).SetProperty(s => s.Status, SlotStatus.UNFURNISHED), ct);
            foreach (var slot in new[] { source, back })
            {
                slot.EmployeeId = null;
                slot.Employee = null;
                slot.Status = SlotStatus.UNFURNISHED;
                db.Entry(slot).Property(s => s.EmployeeId).OriginalValue = null;
                db.Entry(slot).Property(s => s.Status).OriginalValue = SlotStatus.UNFURNISHED;
            }
            var sourceRemainders = ShiftSwapExchangeRules.ApplyExchangeTransferToSlot(source, ownerId, employeeId, valid.Period, source.Schedule.Slots.ToList());
            foreach (var remainder in sourceRemainders) db.ScheduleSlots.Add(remainder);
            var backUniverse = back.Schedule.Slots.Concat(sourceRemainders.Where(s => s.ScheduleId == back.ScheduleId)).Distinct().ToList();
            var returnRemainders = ShiftSwapExchangeRules.ApplyExchangeTransferToSlot(back, employeeId, ownerId, returnPeriod, backUniverse);
            foreach (var remainder in returnRemainders) db.ScheduleSlots.Add(remainder);
            EnsureScheduleEmployee(source.Schedule, employeeId);
            EnsureScheduleEmployee(back.Schedule, ownerId);
            await HighlightAcceptedSwapCellsAsync(swap, employeeId, ct);
            if (back.ScheduleId != source.ScheduleId || back.DayOfMonth != source.DayOfMonth)
                await HighlightAcceptedSwapCellsAsync(new ShiftSwapRequestModel
                { Schedule = back.Schedule, ScheduleId = back.ScheduleId, ScheduleSlot = back, FromEmployeeId = ownerId }, employeeId, ct);
            swap.Status = ShiftSwapStatus.Accepted;
            swap.AcceptedByEmployeeId = employeeId;
            swap.AcceptedAtUtc = acceptedAt;
            await db.SaveChangesAsync(ct);
            var scheduleIds = schedules.Select(s => s.Id).ToArray();
            var afterSlots = await db.ScheduleSlots.AsNoTracking().Where(s => scheduleIds.Contains(s.ScheduleId)).ToListAsync(ct);
            var after = ShiftSwapHistorySnapshotBuilder.BuildExchangeJson(schedules, afterSlots, swap, employeeId, name);
            db.ShiftSwapHistories.Add(new ShiftSwapHistoryModel
            {
                RequestKind = ShiftSwapRequestKind.Exchange, SourceShiftSwapRequestId = swap.Id,
                ScheduleId = source.ScheduleId, ScheduleSlotId = source.Id, ScheduleName = source.Schedule.Name,
                ContainerName = swap.Schedule.Container?.Name ?? string.Empty, ShopName = swap.Schedule.Shop?.Name ?? string.Empty,
                Year = source.Schedule.Year, Month = source.Schedule.Month, DayOfMonth = source.DayOfMonth,
                FromTime = valid.Period.FromTime, ToTime = valid.Period.ToTime,
                FromEmployeeId = ownerId, FromEmployeeName = GetEmployeeName(swap.FromEmployee, ownerId),
                TargetEmployeeId = employeeId, TargetEmployeeName = name, AcceptedByEmployeeId = employeeId, AcceptedByEmployeeName = name,
                CreatedAtUtc = swap.CreatedAtUtc, AcceptedAtUtc = acceptedAt,
                BeforeSnapshotJson = before[source.ScheduleId], AfterSnapshotJson = after[source.ScheduleId],
                ReturnScheduleId = back.ScheduleId, ReturnScheduleName = back.Schedule.Name, ReturnShopName = back.Schedule.Shop?.Name ?? string.Empty,
                ReturnYear = back.Schedule.Year, ReturnMonth = back.Schedule.Month, ReturnDayOfMonth = back.DayOfMonth,
                ReturnFromTime = returnPeriod.FromTime, ReturnToTime = returnPeriod.ToTime,
                ReturnBeforeSnapshotJson = before[back.ScheduleId], ReturnAfterSnapshotJson = after[back.ScheduleId],
            });
            await db.SaveChangesAsync(ct);
            foreach (var schedule in schedules)
                if (await versionCommitService.CommitAsync(schedule.Id, null, $"Exchange accepted: {name}", ct) is null)
                    throw new InvalidOperationException("The accepted exchange did not create a schedule version.");
            await ShiftSwapArchive.PreserveCompletedAsync(db, swap.ScheduleId, ct, swap.Id);
            if (review is null) await transaction.CommitAsync(ct);
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception) ||
            exception is DbUpdateException { InnerException: SqliteException { SqliteExtendedErrorCode: 2067 } })
        {
            if (review is null) await transaction.RollbackAsync(ct);
            throw new ValidationException("This exchange changed during another request. Refresh and try again.");
        }
        catch
        {
            if (review is null) await transaction.RollbackAsync(ct);
            throw;
        }
        await DispatchAsync(context, review, () => workflowLogService.LogAsync(context.User,
            $"Accepted exchange #{swap.Id}: {source.Schedule.Name} ({source.ScheduleId}), {source.Schedule.Year}-{source.Schedule.Month:00}-{source.DayOfMonth:00} {valid.Period.FromTime}-{valid.Period.ToTime} from {GetEmployeeName(swap.FromEmployee, ownerId)} to {name}; {back.Schedule.Name} ({back.ScheduleId}), {back.Schedule.Year}-{back.Schedule.Month:00}-{back.DayOfMonth:00} {returnPeriod.FromTime}-{returnPeriod.ToTime} in return.", ct));
        foreach (var schedule in schedules)
        {
            await DispatchAsync(context, review, () => realtimeNotifier.NotifyScheduleChangedAsync(schedule.ContainerId, schedule.Id, "employee-exchange-accepted"));
            await DispatchAsync(context, review, () => realtimeNotifier.NotifyShiftSwapsChangedAsync(schedule.ContainerId, schedule.Id, schedule.Id, "employee-exchange-accepted", swap.Id));
        }
        return await BuildExchangeDtoAsync(swap, employeeId, ToDto(swap, employeeId, false, valid.Assignments.Where(s => s.EmployeeId == employeeId).ToList()), ct);
    }

    private IQueryable<ShiftSwapRequestModel> BuildSwapRequestQuery()
        => db.ShiftSwapRequests
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Container)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Shop)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Employees)
                    .ThenInclude(employee => employee.Employee)
            .Include(request => request.Schedule)
                .ThenInclude(schedule => schedule.Slots)
            .Include(request => request.ScheduleSlot)
            .Include(request => request.FromEmployee)
            .Include(request => request.TargetEmployee)
            .Include(request => request.AcceptedByEmployee);

    private async Task HighlightAcceptedSwapCellsAsync(
        ShiftSwapRequestModel swap,
        int acceptingEmployeeId,
        CancellationToken cancellationToken)
    {
        var highlightColorArgb = ShiftSwapHighlightRules.ToArgb(swap.IsManagerCreated
            ? swap.Schedule.AcceptedManualShiftHighlightColor : swap.Schedule.AcceptedSwapHighlightColor);
        var affectedEmployeeIds = new HashSet<int> { acceptingEmployeeId };
        if (swap.FromEmployeeId.HasValue)
        {
            affectedEmployeeIds.Add(swap.FromEmployeeId.Value);
        }

        var existingStyles = await db.ScheduleCellStyles
            .Where(style =>
                style.ScheduleId == swap.ScheduleId &&
                style.DayOfMonth == swap.ScheduleSlot!.DayOfMonth &&
                affectedEmployeeIds.Contains(style.EmployeeId))
            .ToDictionaryAsync(style => style.EmployeeId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var employeeId in affectedEmployeeIds)
        {
            if (existingStyles.TryGetValue(employeeId, out var existingStyle))
            {
                existingStyle.BackgroundColorArgb = highlightColorArgb;
                continue;
            }

            db.ScheduleCellStyles.Add(new ScheduleCellStyleModel
            {
                ScheduleId = swap.ScheduleId,
                DayOfMonth = swap.ScheduleSlot!.DayOfMonth,
                EmployeeId = employeeId,
                BackgroundColorArgb = highlightColorArgb,
            });
        }
    }

    private async Task<ScheduleModel> LoadEmployeePublishedScheduleAsync(int scheduleId, int employeeId, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules
            .Include(schedule => schedule.Shop)
            .Include(schedule => schedule.Employees)
                .ThenInclude(employee => employee.Employee)
            .Include(schedule => schedule.Slots)
            .FirstOrDefaultAsync(schedule =>
                schedule.Id == scheduleId &&
                schedule.PublicationStatus == SchedulePublicationStatus.Public &&
                schedule.Employees.Any(employee => employee.EmployeeId == employeeId),
                cancellationToken)
            .ConfigureAwait(false);

        return schedule ?? throw ValidationException.ForField(nameof(scheduleId), "Published schedule was not found for your account.");
    }

    private async Task<IReadOnlyList<ScheduleSlotModel>> LoadPublishedEmployeeMonthSlotsAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        return await db.ScheduleSlots
            .AsNoTracking()
            .Include(slot => slot.Schedule)
            .Where(slot =>
                slot.EmployeeId == employeeId &&
                slot.Schedule.Year == year &&
                slot.Schedule.Month == month &&
                slot.Schedule.PublicationStatus == SchedulePublicationStatus.Public)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static IReadOnlyList<ScheduleSlotModel> GetPeriodSlots(
        IEnumerable<ScheduleSlotModel> slots,
        int year,
        int month)
        => slots.Where(slot => slot.Schedule.Year == year && slot.Schedule.Month == month).ToList();

    private static void EnsureScheduleEmployee(ScheduleModel schedule, int employeeId)
    {
        if (schedule.Employees.Any(employee => employee.EmployeeId == employeeId))
        {
            return;
        }

        var nextDisplayOrder = schedule.Employees.Count == 0
            ? 0
            : schedule.Employees.Max(employee => employee.DisplayOrder) + 1;
        schedule.Employees.Add(new ScheduleEmployeeModel
        {
            ScheduleId = schedule.Id,
            EmployeeId = employeeId,
            DisplayOrder = nextDisplayOrder,
        });
    }

    private static void EnsureValidSwapDate(ScheduleSlotModel slot)
    {
        if (slot.Schedule.Year is < 1 or > 9999 || slot.Schedule.Month is < 1 or > 12 ||
            slot.DayOfMonth < 1 || slot.DayOfMonth > DateTime.DaysInMonth(slot.Schedule.Year, slot.Schedule.Month))
            throw new ValidationException("This shift has an invalid calendar date.");
    }

    private async Task ApplyContractReadPoliciesAsync(IReadOnlyList<ShiftSwapRequestModel> offers,
        IReadOnlyList<ShiftSwapDto> dtos, int employeeId, CancellationToken ct)
    {
        var settings = await policy.GetSettingsAsync(ct);
        var ids = offers.SelectMany(x => new[] { x.FromEmployeeId, x.TargetEmployeeId, employeeId })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var types = await policy.GetContractTypesAsync(ids, ct);
        var offerIds = offers.Select(x => x.Id).ToArray();
        var pending = await db.ContractShiftApprovalRequests.AsNoTracking().Where(x => x.ShiftSwapRequestId.HasValue &&
            offerIds.Contains(x.ShiftSwapRequestId.Value) && x.Status == "pending" && x.Kind == "claim")
            .Select(x => new { x.ShiftSwapRequestId, x.EmployeeId }).ToListAsync(ct);
        var pendingByOffer = pending.ToLookup(x => x.ShiftSwapRequestId!.Value);
        for (var index = 0; index < offers.Count; index++)
        {
            var offer = offers[index]; var dto = dtos[index];
            if (offer.Status != ShiftSwapStatus.Open) continue;
            var restricted = settings.RestrictSwapsToEightHours && new[] { offer.FromEmployeeId, employeeId }
                .Any(id => id.HasValue && types.GetValueOrDefault(id.Value) == EmploymentContractTypes.OPrace);
            if (restricted)
            {
                dto.AllowPartialAcceptance = false;
                var slot = offer.ScheduleSlot;
                var allowed = slot is not null && EmploymentSwapRules.IsWholeEightHourShift(slot, GetSwapPeriod(offer, slot));
                if (offer.RequestKind == ShiftSwapRequestKind.Exchange && offer.ReturnScheduleSlotId is { } returnId)
                {
                    var back = await db.ScheduleSlots.AsNoTracking().SingleOrDefaultAsync(x => x.Id == returnId, ct);
                    allowed &= back is not null && EmploymentSwapRules.IsWholeEightHourShift(back, new(offer.ReturnFromTime!, offer.ReturnToTime!));
                }
                if (!allowed)
                {
                    dto.CanAccept = false; dto.CanAcceptFull = false; dto.EligibleClaimWindows = [];
                    dto.AcceptanceUnavailableReason = EmploymentSwapRules.EightHourReason;
                }
                else
                {
                    if (dto.CanAccept && !dto.CanAcceptFull)
                    { dto.CanAccept = false; dto.AcceptanceUnavailableReason = EmploymentSwapRules.EightHourReason; }
                    dto.EligibleClaimWindows = dto.EligibleClaimWindows.Where(x => x.FromTime == slot!.FromTime && x.ToTime == slot.ToTime).ToList();
                }
            }
            dto.RequiresManagerApproval = EmploymentSwapRules.RequiresPickupApproval(offer, employeeId, settings, types);
            if (types.GetValueOrDefault(employeeId) == EmploymentContractTypes.OPrace && settings.RequireBelowMinimumGiveApproval &&
                offer.RequestKind == ShiftSwapRequestKind.Exchange && offer.TargetEmployeeId == employeeId && offer.ReturnScheduleId is { } returnScheduleId)
            {
                var backSchedule = await db.Schedules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == returnScheduleId, ct);
                if (backSchedule is not null)
                    dto.RequiresManagerApproval |= (await EmploymentSwapRules.CheckOutgoingMinimumAsync(employeeId, backSchedule,
                        new(offer.ReturnFromTime!, offer.ReturnToTime!), offer.Schedule,
                        new(dto.FromTime, dto.ToTime), policy, ct))?.RequiresException == true;
            }
            dto.PickupApprovalPolicyApplies = offer.Visibility == ShiftSwapVisibility.Public
                ? settings.RequirePickupApproval
                : EmploymentSwapRules.RequiresPickupApproval(offer, offer.TargetEmployeeId, settings, types);
            dto.IsAwaitingManagerApproval = pendingByOffer.Contains(offer.Id);
            dto.ApprovalPendingForCurrentEmployee = pendingByOffer[offer.Id].Any(x => x.EmployeeId == employeeId);
            dto.CanRequestManagerApproval = dto.CanAccept && dto.RequiresManagerApproval && !dto.IsAwaitingManagerApproval;
            if (dto.IsAwaitingManagerApproval)
            {
                dto.CanAccept = false;
                dto.CanAcceptFull = false;
                dto.EligibleClaimWindows = [];
                dto.AcceptanceUnavailableReason = dto.ApprovalPendingForCurrentEmployee
                    ? "Awaiting manager approval" : "This offer is awaiting manager approval.";
            }
        }
    }

    private sealed record ReviewExecution(IDbContextTransaction Transaction, ContractShiftApprovalRequestModel Approval,
        List<Func<Task>> Events);

    internal sealed record ShiftSwapExecutionResult(int OfferId, int ContainerId, int ScheduleId,
        IReadOnlyList<int> AffectedOfferIds, int? SourceEmployeeId, int RecipientEmployeeId, string Reason);

    private static Task DispatchAsync(HttpContext context, ReviewExecution? review, Func<Task> action)
    {
        if (review is null) return PostCommitActions.RunAsync(context, action);
        review.Events.Add(action);
        return Task.CompletedTask;
    }

    private async Task<bool> HasApprovedExceptionAsync(ShiftSwapRequestModel swap, int employeeId, ShiftSwapPeriod period, CancellationToken ct)
    {
        if (swap.ApprovedEmploymentExceptionApprovalId is not { } id) return false;
        return await db.ContractShiftApprovalRequests.AsNoTracking().AnyAsync(x => x.Id == id &&
            x.Kind == "below_minimum_give" && x.Status == "approved" && x.EmployeeId == employeeId &&
            x.ResultingShiftSwapRequestId != null && x.ScheduleId == swap.ScheduleId && x.ShopId == swap.Schedule.ShopId &&
            x.Year == swap.Schedule.Year && x.Month == swap.Schedule.Month && x.OriginalDayOfMonth == swap.ScheduleSlot!.DayOfMonth &&
            string.Compare(x.FromTime, period.FromTime) <= 0 && string.Compare(x.ToTime, period.ToTime) >= 0, ct);
    }

    internal async Task<ContractShiftApprovalRequestModel> BuildGiveExceptionDraftAsync(int employeeId,
        CreateEmployeeShiftSwapRequest command, CancellationToken ct)
    {
        var valid = await ValidateOfferAsync(command, employeeId, ct, forApproval: true);
        var minimum = await EmploymentSwapRules.CheckOutgoingMinimumAsync(employeeId, valid.Source.Schedule,
            valid.Period, valid.Return?.Schedule, valid.ReturnPeriod, policy, ct);
        if (minimum?.RequiresException != true)
            throw new ContractShiftConflictException("CONTRACT_REQUEST_INAPPLICABLE", "A manager exception is no longer required. Retry through the regular swap action.");
        return SnapshotApproval("below_minimum_give", employeeId, valid.Source, valid.Period, command, valid.Return, valid.ReturnPeriod);
    }

    private async Task<bool> RequiresPickupApprovalAsync(ShiftSwapRequestModel offer, int employeeId, CancellationToken ct)
    {
        var settings = await policy.GetSettingsAsync(ct);
        var ids = new[] { offer.FromEmployeeId, employeeId }.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        var types = await policy.GetContractTypesAsync(ids, ct);
        if (!types.ContainsKey(employeeId)) throw new KeyNotFoundException("The employee profile could not be found.");
        return EmploymentSwapRules.RequiresPickupApproval(offer, employeeId, settings, types);
    }

    private Task<bool> HasOtherPendingClaimAsync(int offerId, int? permittedApprovalId, CancellationToken ct)
        => db.ContractShiftApprovalRequests.AsNoTracking().AnyAsync(x => x.Kind == "claim" && x.Status == "pending" &&
            x.ShiftSwapRequestId == offerId && (!permittedApprovalId.HasValue || x.Id != permittedApprovalId.Value), ct);

    private static ContractShiftConflictException OfferPendingApproval()
        => new("CONTRACT_OFFER_PENDING_APPROVAL", "This offer is awaiting manager approval.");

    internal async Task<ContractShiftApprovalRequestModel> BuildClaimDraftAsync(int employeeId, int offerId,
        AcceptEmployeeShiftSwapRequest? command, CancellationToken ct, int? permittedApprovalId = null)
    {
        var swap = await BuildSwapRequestQuery().SingleOrDefaultAsync(x => x.Id == offerId, ct)
            ?? throw new KeyNotFoundException("The swap offer was not found.");
        if (swap.Status != ShiftSwapStatus.Open || swap.Schedule.PublicationStatus != SchedulePublicationStatus.Public)
            throw new ContractShiftConflictException("CONTRACT_OFFER_STALE", "This swap offer is no longer open.");
        if (await HasOtherPendingClaimAsync(offerId, permittedApprovalId, ct))
        {
            var from = command?.FromTime ?? swap.OfferedFromTime ?? swap.ScheduleSlot?.FromTime;
            var to = command?.ToTime ?? swap.OfferedToTime ?? swap.ScheduleSlot?.ToTime;
            if (!permittedApprovalId.HasValue && await db.ContractShiftApprovalRequests.AsNoTracking().AnyAsync(x =>
                x.Kind == "claim" && x.Status == "pending" && x.ShiftSwapRequestId == offerId &&
                x.EmployeeId == employeeId && x.FromTime == from && x.ToTime == to, ct))
                throw new ContractShiftConflictException("CONTRACT_DUPLICATE_PENDING", "An equivalent approval request is already pending.");
            throw OfferPendingApproval();
        }
        EnsureSwapAllowed(swap.Schedule);
        EnsureScheduleIsNotLocked(swap.Schedule, forApproval: true);
        var slot = swap.ScheduleSlot ?? throw new ContractShiftConflictException("CONTRACT_OFFER_STALE", "This shift is no longer available.");
        EnsureValidSwapDate(slot);
        var offered = GetSwapPeriod(swap, slot);
        ShiftSwapPeriod selected;
        ScheduleSlotModel? returned = null;
        ShiftSwapPeriod? returnPeriodSnapshot = null;
        var requiresApproval = await RequiresPickupApprovalAsync(swap, employeeId, ct);
        if (swap.RequestKind == ShiftSwapRequestKind.Exchange)
        {
            if (swap.TargetEmployeeId != employeeId || swap.FromEmployeeId is not > 0)
                throw new ValidationException("This private exchange is for another employee.");
            var valid = await ValidateOfferAsync(CommandFor(swap), swap.FromEmployeeId.Value, ct, swap.Id, ShiftSwapArchive.ReadReservation(swap), forApproval: true);
            returned = valid.Return;
            returnPeriodSnapshot = valid.ReturnPeriod;
            selected = command?.FromTime is null && command?.ToTime is null ? offered :
                NormalizePeriod(command?.FromTime ?? "", command?.ToTime ?? "", "FromTime", "ToTime");
            if (selected != offered) throw new ValidationException("This offer must be accepted for the whole published period.");
            requiresApproval |= (await EmploymentSwapRules.CheckOutgoingMinimumAsync(employeeId, valid.Return!.Schedule,
                valid.ReturnPeriod!.Value, valid.Source.Schedule, valid.Period, policy, ct))?.RequiresException == true;
            if ((await EmploymentSwapRules.CheckOutgoingMinimumAsync(swap.FromEmployeeId.Value, valid.Source.Schedule,
                valid.Period, valid.Return!.Schedule, valid.ReturnPeriod, policy, ct))?.RequiresException == true &&
                !await HasApprovedExceptionAsync(swap, swap.FromEmployeeId.Value, valid.Period, ct))
                throw EmploymentSwapRules.GiveApprovalRequired();
        }
        else
        {
            if (swap.Visibility == ShiftSwapVisibility.Public && !await db.Schedules.AnyAsync(x =>
                x.ContainerId == swap.Schedule.ContainerId && x.PublicationStatus == SchedulePublicationStatus.Public &&
                x.Employees.Any(e => e.EmployeeId == employeeId), ct))
                throw new ValidationException("This swap belongs to another container.");
            var assignments = await LoadPublishedEmployeeMonthSlotsAsync(employeeId, swap.Schedule.Year, swap.Schedule.Month, ct);
            var reason = GetAcceptanceUnavailableReason(swap, employeeId, false, assignments, slot, offered);
            if (reason is not null) throw new ValidationException(reason);
            selected = ValidateClaimedPeriod(swap, offered, command, assignments, employeeId);
            await ValidateEmploymentPeriodsAsync(slot, selected, swap.IsManagerCreated ? null : swap.FromEmployeeId, employeeId, ct);
            if (!swap.IsManagerCreated && swap.FromEmployeeId is { } giver &&
                (await EmploymentSwapRules.CheckOutgoingMinimumAsync(giver, swap.Schedule, selected, null, null, policy, ct))?.RequiresException == true &&
                !await HasApprovedExceptionAsync(swap, giver, selected, ct)) throw EmploymentSwapRules.GiveApprovalRequired();
        }
        if (!requiresApproval)
            throw new ContractShiftConflictException("CONTRACT_REQUEST_INAPPLICABLE", "Manager approval is no longer required. Retry through the regular swap action.");
        var draft = SnapshotApproval("claim", employeeId, slot, selected, CommandFor(swap), returned, returnPeriodSnapshot);
        draft.ShiftSwapRequestId = swap.Id;
        return draft;
    }

    private static CreateEmployeeShiftSwapRequest CommandFor(ShiftSwapRequestModel swap) => new()
    {
        ScheduleId = swap.ScheduleId, ScheduleSlotId = swap.ScheduleSlotId ?? 0,
        FromTime = swap.OfferedFromTime, ToTime = swap.OfferedToTime,
        TargetEmployeeId = swap.TargetEmployeeId, RequestKind = swap.RequestKind == ShiftSwapRequestKind.Exchange ? "exchange" : "give",
        AllowPartialAcceptance = swap.AllowPartialAcceptance, ReturnScheduleId = swap.ReturnScheduleId,
        ReturnScheduleSlotId = swap.ReturnScheduleSlotId, ReturnFromTime = swap.ReturnFromTime, ReturnToTime = swap.ReturnToTime,
    };

    internal static CreateEmployeeShiftSwapRequest CommandFor(ContractShiftApprovalRequestModel approval) => new()
    {
        ScheduleId = approval.ScheduleId, ScheduleSlotId = approval.ScheduleSlotId,
        FromTime = approval.FromTime, ToTime = approval.ToTime, TargetEmployeeId = approval.TargetEmployeeId,
        RequestKind = approval.RequestKind, AllowPartialAcceptance = approval.AllowPartialAcceptance,
        ReturnScheduleId = approval.ReturnScheduleId, ReturnScheduleSlotId = approval.ReturnScheduleSlotId,
        ReturnFromTime = approval.ReturnFromTime, ReturnToTime = approval.ReturnToTime,
    };

    private static ContractShiftApprovalRequestModel SnapshotApproval(string kind, int employeeId, ScheduleSlotModel slot,
        ShiftSwapPeriod period, CreateEmployeeShiftSwapRequest command, ScheduleSlotModel? returned = null, ShiftSwapPeriod? returnPeriod = null) => new()
    {
        Kind = kind, EmployeeId = employeeId, ScheduleId = slot.ScheduleId, ScheduleSlotId = slot.Id,
        OriginalDayOfMonth = slot.DayOfMonth, OriginalFromTime = slot.FromTime, OriginalToTime = slot.ToTime,
        ReturnOriginalDayOfMonth = returned?.DayOfMonth, ReturnOriginalFromTime = returned?.FromTime, ReturnOriginalToTime = returned?.ToTime,
        ReturnYear = returned?.Schedule.Year, ReturnMonth = returned?.Schedule.Month, ReturnShopId = returned?.Schedule.ShopId,
        FromTime = period.FromTime, ToTime = period.ToTime, TargetEmployeeId = command.TargetEmployeeId,
        RequestKind = command.RequestKind ?? "give", AllowPartialAcceptance = command.AllowPartialAcceptance,
        ReturnScheduleId = command.ReturnScheduleId, ReturnScheduleSlotId = command.ReturnScheduleSlotId,
        ReturnFromTime = returnPeriod?.FromTime ?? command.ReturnFromTime, ReturnToTime = returnPeriod?.ToTime ?? command.ReturnToTime,
        ShopId = slot.Schedule.ShopId, Year = slot.Schedule.Year, Month = slot.Schedule.Month, CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    internal async Task<ShiftSwapExecutionResult> ExecuteReviewedAsync(int managerId, int containerId, int graphId,
        int approvalId, HttpContext context, CancellationToken ct)
    {
        if (!context.User.IsInRole(AuthRoles.Manager) || context.User.FindFirstValue("manager_id") != managerId.ToString(CultureInfo.InvariantCulture))
            throw new UnauthorizedAccessException("Only the reviewing manager can execute an approval.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var approval = await db.ContractShiftApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == approvalId &&
            x.ScheduleId == graphId && db.Schedules.Any(s => s.Id == graphId && s.ContainerId == containerId), ct)
            ?? throw new KeyNotFoundException("The approval request was not found in this graph.");
        if (approval.Status != "pending")
            throw new ContractShiftConflictException("CONTRACT_ALREADY_REVIEWED", "Only pending requests can be reviewed.");
        var review = new ReviewExecution(transaction, approval, []);
        reviewingManagerId = managerId;
        ShiftSwapDto result;
        try
        {
            var selected = new AcceptEmployeeShiftSwapRequest { FromTime = approval.FromTime, ToTime = approval.ToTime };
            var fresh = approval.Kind == "claim"
                ? await BuildClaimDraftAsync(approval.EmployeeId, approval.ShiftSwapRequestId ?? 0, selected, ct, approval.Id)
                : await BuildGiveExceptionDraftAsync(approval.EmployeeId, CommandFor(approval), ct);
            if (fresh.ScheduleId != approval.ScheduleId || fresh.ScheduleSlotId != approval.ScheduleSlotId ||
                fresh.ShopId != approval.ShopId || fresh.Year != approval.Year || fresh.Month != approval.Month ||
                fresh.FromTime != approval.FromTime || fresh.ToTime != approval.ToTime ||
                fresh.ReturnScheduleId != approval.ReturnScheduleId || fresh.ReturnScheduleSlotId != approval.ReturnScheduleSlotId ||
                fresh.ReturnFromTime != approval.ReturnFromTime || fresh.ReturnToTime != approval.ReturnToTime ||
                fresh.RequestKind != approval.RequestKind || fresh.TargetEmployeeId != approval.TargetEmployeeId || fresh.AllowPartialAcceptance != approval.AllowPartialAcceptance ||
                fresh.OriginalDayOfMonth != approval.OriginalDayOfMonth || fresh.OriginalFromTime != approval.OriginalFromTime || fresh.OriginalToTime != approval.OriginalToTime ||
                fresh.ReturnOriginalDayOfMonth != approval.ReturnOriginalDayOfMonth || fresh.ReturnOriginalFromTime != approval.ReturnOriginalFromTime ||
                fresh.ReturnOriginalToTime != approval.ReturnOriginalToTime || fresh.ReturnYear != approval.ReturnYear || fresh.ReturnMonth != approval.ReturnMonth || fresh.ReturnShopId != approval.ReturnShopId)
                throw new ValidationException("The requested shift or offer changed. Submit a new request.");
            result = approval.Kind == "claim"
                ? await AcceptCoreAsync(approval.EmployeeId, context, approval.ShiftSwapRequestId!.Value, ct, selected, review)
                : await CreateCoreAsync(approval.EmployeeId, context, CommandFor(approval), ct, review);
            var now = DateTimeOffset.UtcNow;
            if (await db.ContractShiftApprovalRequests.Where(x => x.Id == approvalId && x.Status == "pending")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "approved")
                    .SetProperty(x => x.ReviewedByManagerId, managerId).SetProperty(x => x.ReviewedAtUtc, now)
                    .SetProperty(x => x.ResultingShiftSwapRequestId, result.Id), ct) != 1)
                throw new ContractShiftConflictException("CONTRACT_ALREADY_REVIEWED", "This request was already reviewed.");
            await transaction.CommitAsync(ct);
        }
        catch (Exception exception) when (IsSqliteWriterConflict(exception))
        {
            await transaction.RollbackAsync(ct);
            throw new ContractShiftConflictException("CONTRACT_CONCURRENT_CHANGE", "This request changed during another operation. Refresh and try again.");
        }
        catch (Exception exception) when (exception is ValidationException or KeyNotFoundException or ContractShiftConflictException)
        {
            await transaction.RollbackAsync(ct);
            await transaction.DisposeAsync();
            db.ChangeTracker.Clear();
            if (exception is ContractShiftConflictException { Code: "edit_lock_conflict" or "CONTRACT_ALREADY_REVIEWED" or "CONTRACT_CONCURRENT_CHANGE" }) throw;
            var reason = exception.Message.Length > 250 ? exception.Message[..250] : exception.Message;
            if (await db.ContractShiftApprovalRequests.Where(x => x.Id == approvalId && x.Status == "pending")
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "expired").SetProperty(x => x.FailureReason, reason), ct) != 1)
                throw new ContractShiftConflictException("CONTRACT_ALREADY_REVIEWED", "This request was already reviewed.");
            await PostCommitActions.RunAsync(context, () => workflowLogService.LogAsync(context.User, $"Expired contract approval #{approvalId}: {reason}", ct));
            await PostCommitActions.RunAsync(context, () => realtimeNotifier.NotifyShiftSwapsChangedAsync(containerId, graphId, graphId, "contract-approval-expired"));
            await PostCommitActions.RunAsync(context, () => realtimeNotifier.NotifyManagerDataChangedAsync("schedule", $"{containerId}:{graphId}", "contract-approval-expired", containerId, graphId));
            throw new ContractShiftConflictException("CONTRACT_APPROVAL_EXPIRED", reason);
        }
        finally { reviewingManagerId = null; }
        await transaction.DisposeAsync();
        foreach (var action in review.Events) await PostCommitActions.RunAsync(context, action);
        return new(result.Id, containerId, graphId, [result.Id], result.FromEmployeeId, approval.EmployeeId, "contract-approval-approved");
    }

    private bool IsScheduleLocked(ScheduleModel schedule)
        => reviewingManagerId is { } managerId && scheduleEditLockService is IManagerEditLockService managerLocks
            ? managerLocks.IsLockedByAnotherManager(ManagerEditLockTargets.Schedule(schedule.ContainerId, schedule.Id), managerId)
            : scheduleEditLockService.IsLocked(schedule.ContainerId, schedule.Id);

    private void EnsureScheduleIsNotLocked(ScheduleModel schedule, bool forApproval = false)
    {
        if (IsScheduleLocked(schedule))
        {
            const string reason = "This schedule is being edited by a manager. Try again after the manager saves changes.";
            if (forApproval || reviewingManagerId.HasValue) throw new ContractShiftConflictException("edit_lock_conflict", reason);
            throw new ValidationException(reason);
        }
    }

    private static void EnsureSwapAllowed(ScheduleModel schedule)
    {
        if (!schedule.AllowSwap)
        {
            throw new ValidationException("Swaps are not allowed for this schedule.");
        }
    }

}
