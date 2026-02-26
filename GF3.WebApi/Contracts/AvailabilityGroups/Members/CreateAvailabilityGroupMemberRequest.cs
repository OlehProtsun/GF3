using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups.Members;

public sealed class CreateAvailabilityGroupMemberRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }
}
