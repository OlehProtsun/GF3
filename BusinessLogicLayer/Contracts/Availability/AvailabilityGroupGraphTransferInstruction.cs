namespace BusinessLogicLayer.Contracts.Availability;

public sealed record AvailabilityGroupGraphTransferInstruction(
    int EmployeeId, int SourceGroupId, IReadOnlyList<int> DayOfMonths);

public sealed record AvailabilityGroupGraphSaveResult(int Id, IReadOnlyList<int> SourceGroupIds, bool Changed);
