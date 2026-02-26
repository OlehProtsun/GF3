namespace WebApi.Contracts.AvailabilityGroups.Members;

public sealed class AvailabilityGroupMemberDto
{
    public int Id { get; set; }
    public int AvailabilityGroupId { get; set; }
    public int EmployeeId { get; set; }
}
