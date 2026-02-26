namespace WebApi.Contracts.AvailabilityGroups;

public sealed class AvailabilityGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
}
