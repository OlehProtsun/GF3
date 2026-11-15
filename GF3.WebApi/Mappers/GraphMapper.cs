using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.Containers.Graphs;

namespace WebApi.Mappers;

/// <summary>
/// Maps graph-related API requests and DTOs to the business-layer schedule model.
/// </summary>
public static class GraphMapper
{
    public static GraphDto ToGraphDto(this ScheduleModel model, DateTimeOffset? lastUpdatedAtUtc = null) => new()
    {
        Id = model.Id,
        ContainerId = model.ContainerId,
        ShopId = model.ShopId,
        Name = model.Name,
        Year = model.Year,
        Month = model.Month,
        PublicationStatus = ToApiPublicationStatus(model.PublicationStatus),
        AllowSwap = model.AllowSwap,
        PeoplePerShift = model.PeoplePerShift,
        Shift1Time = model.Shift1Time,
        Shift2Time = model.Shift2Time,
        MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
        MaxConsecutiveDays = model.MaxConsecutiveDays,
        MaxConsecutiveFull = model.MaxConsecutiveFull,
        MaxFullPerMonth = model.MaxFullPerMonth,
        Note = model.Note,
        AvailabilityGroupId = model.AvailabilityGroupId,
        LastUpdatedAtUtc = lastUpdatedAtUtc,
    };

    public static ScheduleModel ToCreateModel(this CreateGraphRequest request, int containerId)
        => MapGraphModel(
            containerId,
            request.ShopId,
            request.Name,
            request.Year,
            request.Month,
            request.PublicationStatus,
            request.AllowSwap,
            request.PeoplePerShift,
            request.Shift1Time,
            request.Shift2Time,
            request.MaxHoursPerEmpMonth,
            request.MaxConsecutiveDays,
            request.MaxConsecutiveFull,
            request.MaxFullPerMonth,
            request.Note,
            request.AvailabilityGroupId);

    public static ScheduleModel ToUpdateModel(this UpdateGraphRequest request, int containerId, int graphId)
        => MapGraphModel(
            containerId,
            request.ShopId,
            request.Name,
            request.Year,
            request.Month,
            request.PublicationStatus,
            request.AllowSwap,
            request.PeoplePerShift,
            request.Shift1Time,
            request.Shift2Time,
            request.MaxHoursPerEmpMonth,
            request.MaxConsecutiveDays,
            request.MaxConsecutiveFull,
            request.MaxFullPerMonth,
            request.Note,
            request.AvailabilityGroupId,
            graphId);

    public static ScheduleModel ToPreviewModel(this GenerateGraphPreviewRequest request, int containerId)
        => MapGraphModel(
            containerId,
            request.Graph.ShopId,
            request.Graph.Name,
            request.Graph.Year,
            request.Graph.Month,
            request.Graph.PublicationStatus,
            request.Graph.AllowSwap,
            request.Graph.PeoplePerShift,
            request.Graph.Shift1Time,
            request.Graph.Shift2Time,
            request.Graph.MaxHoursPerEmpMonth,
            request.Graph.MaxConsecutiveDays,
            request.Graph.MaxConsecutiveFull,
            request.Graph.MaxFullPerMonth,
            request.Graph.Note,
            request.Graph.AvailabilityGroupId,
            request.GraphId ?? 0);

    private static ScheduleModel MapGraphModel(
        int containerId,
        int shopId,
        string name,
        int year,
        int month,
        string? publicationStatus,
        bool allowSwap,
        int peoplePerShift,
        string shift1Time,
        string shift2Time,
        int maxHoursPerEmpMonth,
        int maxConsecutiveDays,
        int maxConsecutiveFull,
        int maxFullPerMonth,
        string? note,
        int? availabilityGroupId,
        int id = 0)
        => new()
        {
            Id = id,
            ContainerId = containerId,
            ShopId = shopId,
            Name = name,
            Year = year,
            Month = month,
            PublicationStatus = ToDomainPublicationStatus(publicationStatus),
            AllowSwap = allowSwap,
            PeoplePerShift = peoplePerShift,
            Shift1Time = shift1Time,
            Shift2Time = shift2Time,
            MaxHoursPerEmpMonth = maxHoursPerEmpMonth,
            MaxConsecutiveDays = maxConsecutiveDays,
            MaxConsecutiveFull = maxConsecutiveFull,
            MaxFullPerMonth = maxFullPerMonth,
            Note = note,
            AvailabilityGroupId = availabilityGroupId,
        };

    private static string ToApiPublicationStatus(SchedulePublicationStatus status)
        => status == SchedulePublicationStatus.Public ? "public" : "private";

    private static SchedulePublicationStatus ToDomainPublicationStatus(string? status)
        => string.Equals(status, "public", StringComparison.OrdinalIgnoreCase)
            ? SchedulePublicationStatus.Public
            : SchedulePublicationStatus.Private;
}
