using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.DataBaseContext;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts.ShiftSwaps;
using WebApi.Realtime;
using WebApi.Services;

namespace WebApi.ShiftSwaps;

public sealed class ContractShiftApprovalService(AppDbContext db, ShiftSwapExecutionService execution,
    IEmploymentContractPolicyService policy, IWorkflowLogService log, IRealtimeNotifier notifier)
{
    public async Task<ContractShiftApprovalDto> CreateClaimRequestAsync(int employeeId, CreateContractClaimApprovalRequest request,
        HttpContext context, CancellationToken ct = default)
        => await CreateAsync(employeeId, () => execution.BuildClaimDraftAsync(employeeId, request.ShiftSwapRequestId,
            new AcceptEmployeeShiftSwapRequest { FromTime = request.FromTime, ToTime = request.ToTime }, ct), context, ct);

    public async Task<ContractShiftApprovalDto> CreateGiveExceptionRequestAsync(int employeeId, CreateEmployeeShiftSwapRequest request,
        HttpContext context, CancellationToken ct = default)
        => await CreateAsync(employeeId, () => execution.BuildGiveExceptionDraftAsync(employeeId, request, ct), context, ct);

    private async Task<ContractShiftApprovalDto> CreateAsync(int employeeId, Func<Task<ContractShiftApprovalRequestModel>> build,
        HttpContext context, CancellationToken ct)
    {
        ContractShiftApprovalRequestModel draft;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            draft = await build();
            db.ContractShiftApprovalRequests.Add(draft);
            try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); }
            catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
            {
                await transaction.RollbackAsync(ct);
                await transaction.DisposeAsync();
                db.Entry(draft).State = EntityState.Detached;
                if (draft.Kind == "claim" && !await db.ContractShiftApprovalRequests.AsNoTracking().AnyAsync(x =>
                    x.Kind == "claim" && x.Status == "pending" && x.EmployeeId == employeeId &&
                    x.ShiftSwapRequestId == draft.ShiftSwapRequestId && x.FromTime == draft.FromTime && x.ToTime == draft.ToTime, ct))
                    throw new ContractShiftConflictException("CONTRACT_OFFER_PENDING_APPROVAL", "This offer is awaiting manager approval.");
                throw new ContractShiftConflictException("CONTRACT_DUPLICATE_PENDING", "An equivalent approval request is already pending.");
            }
        }
        await NotifyAsync(draft, context, "contract-approval-submitted", ct);
        return (await MapAsync([draft], ct)).Single();
    }

    public async Task<IReadOnlyList<ContractShiftApprovalDto>> GetMyRequestsAsync(int employeeId, CancellationToken ct = default)
        => await MapAsync(await db.ContractShiftApprovalRequests.AsNoTracking().Where(x => x.EmployeeId == employeeId)
            .OrderBy(x => x.Status == "pending" ? 0 : 1).ThenByDescending(x => x.Id).ToListAsync(ct), ct);

    public async Task<IReadOnlyList<ContractShiftApprovalDto>> GetGraphRequestsAsync(int containerId, int graphId, CancellationToken ct = default)
    {
        await EnsureGraphAsync(containerId, graphId, ct);
        return await MapAsync(await db.ContractShiftApprovalRequests.AsNoTracking().Where(x => x.ScheduleId == graphId)
            .OrderBy(x => x.Status == "pending" ? 0 : 1).ThenByDescending(x => x.Id).ToListAsync(ct), ct);
    }

    public async Task<ContractShiftApprovalDto> ApproveAsync(int managerId, int containerId, int graphId, int id,
        HttpContext context, CancellationToken ct = default)
    {
        await execution.ExecuteReviewedAsync(managerId, containerId, graphId, id, context, ct);
        var approval = await db.ContractShiftApprovalRequests.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        await NotifyAsync(approval, context, "contract-approval-approved", ct);
        return (await MapAsync([approval], ct)).Single();
    }

    public async Task<ContractShiftApprovalDto> RejectAsync(int managerId, int containerId, int graphId, int id,
        HttpContext context, CancellationToken ct = default)
    {
        await EnsureGraphAsync(containerId, graphId, ct);
        var approval = await db.ContractShiftApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ScheduleId == graphId, ct)
            ?? throw new KeyNotFoundException("The approval request was not found in this graph.");
        var now = DateTimeOffset.UtcNow;
        if (await db.ContractShiftApprovalRequests.Where(x => x.Id == id && x.Status == "pending").ExecuteUpdateAsync(update =>
            update.SetProperty(x => x.Status, "rejected").SetProperty(x => x.ReviewedAtUtc, now)
                .SetProperty(x => x.ReviewedByManagerId, managerId), ct) != 1) throw AlreadyReviewed();
        approval.Status = "rejected"; approval.ReviewedAtUtc = now; approval.ReviewedByManagerId = managerId;
        await NotifyAsync(approval, context, "contract-approval-rejected", ct);
        return (await MapAsync([approval], ct)).Single();
    }

    public async Task<ContractShiftApprovalDto> CancelAsync(int employeeId, int id, HttpContext context, CancellationToken ct = default)
    {
        var approval = await db.ContractShiftApprovalRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.EmployeeId == employeeId, ct)
            ?? throw new KeyNotFoundException("The approval request was not found.");
        var now = DateTimeOffset.UtcNow;
        if (await db.ContractShiftApprovalRequests.Where(x => x.Id == id && x.Status == "pending" && x.EmployeeId == employeeId)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.Status, "cancelled").SetProperty(x => x.CancelledAtUtc, now), ct) != 1)
            throw AlreadyReviewed();
        approval.Status = "cancelled"; approval.CancelledAtUtc = now;
        await NotifyAsync(approval, context, "contract-approval-cancelled", ct);
        return (await MapAsync([approval], ct)).Single();
    }

    private static ContractShiftConflictException AlreadyReviewed()
        => new("CONTRACT_ALREADY_REVIEWED", "Only pending requests can be reviewed or cancelled.");

    private async Task EnsureGraphAsync(int containerId, int graphId, CancellationToken ct)
    {
        if (!await db.Schedules.AnyAsync(x => x.Id == graphId && x.ContainerId == containerId, ct))
            throw new KeyNotFoundException("The graph was not found in this container.");
    }

    private async Task NotifyAsync(ContractShiftApprovalRequestModel approval, HttpContext context, string reason, CancellationToken ct)
    {
        var containerId = await db.Schedules.Where(x => x.Id == approval.ScheduleId).Select(x => x.ContainerId).SingleAsync(ct);
        await PostCommitActions.RunAsync(context, () => log.LogAsync(context.User, $"{reason}: contract approval #{approval.Id} ({approval.Kind}).", ct));
        await PostCommitActions.RunAsync(context, () => notifier.NotifyManagerDataChangedAsync("schedule", $"{containerId}:{approval.ScheduleId}", reason, containerId, approval.ScheduleId));
        await PostCommitActions.RunAsync(context, () => notifier.NotifyShiftSwapsChangedAsync(containerId, approval.ScheduleId, approval.ScheduleId, reason));
    }

    private async Task<IReadOnlyList<ContractShiftApprovalDto>> MapAsync(IReadOnlyList<ContractShiftApprovalRequestModel> approvals, CancellationToken ct)
    {
        var scheduleIds = approvals.Select(x => x.ScheduleId).ToArray();
        var schedules = await db.Schedules.AsNoTracking().Include(x => x.Shop).Where(x => scheduleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var offerIds = approvals.Where(x => x.ShiftSwapRequestId.HasValue).Select(x => x.ShiftSwapRequestId!.Value).Distinct().ToArray();
        var offers = await db.ShiftSwapRequests.AsNoTracking().Where(x => offerIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var histories = await db.ShiftSwapHistories.AsNoTracking().Where(x => offerIds.Contains(x.SourceShiftSwapRequestId))
            .ToDictionaryAsync(x => x.SourceShiftSwapRequestId, ct);
        var employeeIds = approvals.SelectMany(x => new int?[] { x.EmployeeId, x.TargetEmployeeId })
            .Concat(offers.Values.Select(x => x.FromEmployeeId)).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToArray();
        var employees = await db.Employees.AsNoTracking().Where(x => employeeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        string? Name(int? id) => id is { } value && employees.TryGetValue(value, out var person) ? $"{person.FirstName} {person.LastName}" : null;
        var result = new List<ContractShiftApprovalDto>();
        foreach (var approval in approvals)
        {
            schedules.TryGetValue(approval.ScheduleId, out var schedule);
            var offer = approval.ShiftSwapRequestId is { } offerId ? offers.GetValueOrDefault(offerId) : null;
            var archived = offer is not null && ShiftSwapArchive.IsArchived(offer) ? ShiftSwapArchive.ReadView(offer.ArchivedViewsJson!, 0) : null;
            var isClaim = approval.Kind == "claim";
            var history = isClaim && approval.Status == "approved" && approval.ShiftSwapRequestId is { } historyOfferId
                && histories.TryGetValue(historyOfferId, out var completed) && completed.AcceptedByEmployeeId == approval.EmployeeId ? completed : null;
            var fromId = isClaim ? history is not null ? history.FromEmployeeId : archived is not null ? archived.FromEmployeeId : offer?.FromEmployeeId : approval.EmployeeId;
            var toId = isClaim ? approval.EmployeeId : approval.TargetEmployeeId;
            var dto = new ContractShiftApprovalDto
            {
                Id = approval.Id, Kind = approval.Kind, Status = approval.Status, EmployeeId = approval.EmployeeId,
                EmployeeName = Name(approval.EmployeeId) ?? "",
                FromEmployeeId = fromId, FromEmployeeName = isClaim ? history?.FromEmployeeName ?? archived?.FromEmployeeName ?? Name(fromId) : Name(fromId),
                ToEmployeeId = toId, ToEmployeeName = history?.AcceptedByEmployeeName ?? Name(toId),
                IsManagerCreated = isClaim ? history?.IsManagerCreated ?? archived?.IsManagerCreated ?? offer?.IsManagerCreated : false,
                ReturnDayOfMonth = approval.ReturnOriginalDayOfMonth, ReturnYear = approval.ReturnYear, ReturnMonth = approval.ReturnMonth,
                ScheduleId = approval.ScheduleId, ScheduleSlotId = approval.ScheduleSlotId, ScheduleName = schedule?.Name ?? "",
                ShopId = approval.ShopId, ShopName = schedule?.Shop.Name ?? "", Year = approval.Year, Month = approval.Month,
                DayOfMonth = approval.OriginalDayOfMonth,
                FromTime = approval.FromTime, ToTime = approval.ToTime, ShiftSwapRequestId = approval.ShiftSwapRequestId,
                ResultingShiftSwapRequestId = approval.ResultingShiftSwapRequestId, TargetEmployeeId = approval.TargetEmployeeId,
                RequestKind = approval.RequestKind, ReturnScheduleId = approval.ReturnScheduleId, ReturnScheduleSlotId = approval.ReturnScheduleSlotId,
                ReturnFromTime = approval.ReturnFromTime, ReturnToTime = approval.ReturnToTime, CreatedAtUtc = approval.CreatedAtUtc,
                ReviewedAtUtc = approval.ReviewedAtUtc, ReviewedByManagerId = approval.ReviewedByManagerId, CancelledAtUtc = approval.CancelledAtUtc,
                IsStale = approval.Status == "expired", FailureReason = approval.FailureReason,
                ApprovalReason = approval.Kind == "claim" ? "The offer is reserved while this pickup request awaits manager approval." : "Monthly minimum exception required.",
            };
            if (approval.Status == "pending" && employees.TryGetValue(approval.EmployeeId, out var employee) && !employee.IsDeleted)
            {
                var outgoing = approval.Kind == "below_minimum_give" ? schedule : approval.RequestKind == "exchange" && approval.ReturnScheduleId is { } returnId
                    ? await db.Schedules.AsNoTracking().Include(x => x.Shop).SingleOrDefaultAsync(x => x.Id == returnId, ct) : null;
                if (outgoing is not null)
                {
                    var isGive = approval.Kind == "below_minimum_give";
                    var incoming = isGive && approval.ReturnScheduleId is { } incomingId
                        ? await db.Schedules.AsNoTracking().SingleOrDefaultAsync(x => x.Id == incomingId, ct) : isGive ? null : schedule;
                    var check = await EmploymentSwapRules.CheckOutgoingMinimumAsync(approval.EmployeeId, outgoing,
                        isGive ? new(approval.FromTime, approval.ToTime) : new(approval.ReturnFromTime!, approval.ReturnToTime!),
                        incoming, incoming is null ? null : isGive ? new(approval.ReturnFromTime!, approval.ReturnToTime!) : new(approval.FromTime, approval.ToTime), policy, ct);
                    if (check is not null)
                    {
                        dto.MinimumMinutes = check.Minimum.MinimumMinutes; dto.ProjectedMinutes = check.ProjectedMinutes;
                        dto.MinimumShopName = outgoing.Shop.Name; dto.MinimumYear = outgoing.Year; dto.MinimumMonth = outgoing.Month;
                    }
                }
            }
            result.Add(dto);
        }
        return result;
    }
}
