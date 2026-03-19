namespace WebApi.Contracts.Containers.Graphs;

public sealed class ContainerGraphDto
{
    public int Id { get; set; }
    public int ContainerId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int PeoplePerShift { get; set; }
    public string Shift1Time { get; set; } = string.Empty;
    public string Shift2Time { get; set; } = string.Empty;
    public int MaxHoursPerEmpMonth { get; set; }
    public int MaxConsecutiveDays { get; set; }
    public int MaxConsecutiveFull { get; set; }
    public int MaxFullPerMonth { get; set; }
    public string? Note { get; set; }
    public int? AvailabilityGroupId { get; set; }
}
