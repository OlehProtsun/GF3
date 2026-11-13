using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BusinessLogicLayer.Contracts.Enums;

namespace WebApi.Contracts.AvailabilityGroups;

public sealed class SaveAvailabilityGroupGraphRequest
{
    [Range(1, int.MaxValue)]
    public int? Id { get; init; }

    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 9999)]
    public int Year { get; init; }

    [Range(1, 12)]
    public int Month { get; init; }

    public string? PublicationStatus { get; init; }
    public DateTimeOffset? VisibleFromUtc { get; init; }
    public DateTimeOffset? VisibleToUtc { get; init; }

    [Required, MinLength(1)]
    public List<SaveAvailabilityGroupGraphMemberRequest> Members { get; init; } = [];

    [Required]
    public List<SaveAvailabilityGroupGraphTransferRequest> Transfers { get; init; } = [];
}

public sealed class SaveAvailabilityGroupGraphMemberRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; init; }

    [Required, MinLength(1), MaxLength(31)]
    public List<SaveAvailabilityGroupGraphDayRequest> Days { get; init; } = [];
}

public sealed class SaveAvailabilityGroupGraphDayRequest
{
    [Range(1, 31)]
    public int DayOfMonth { get; init; }

    [JsonRequired, EnumDataType(typeof(AvailabilityKind))]
    public AvailabilityKind Kind { get; init; }

    [StringLength(200)]
    public string? IntervalStr { get; init; }
}

public sealed class SaveAvailabilityGroupGraphTransferRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; init; }

    [Range(1, int.MaxValue)]
    public int SourceGroupId { get; init; }

    [Required, MinLength(1)]
    public List<int> DayOfMonths { get; init; } = [];
}

public sealed record SaveAvailabilityGroupGraphResponse(int Id, List<int> SourceGroupIds, bool Changed);
