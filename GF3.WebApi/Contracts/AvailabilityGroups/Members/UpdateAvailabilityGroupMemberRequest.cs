using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts.AvailabilityGroups.Members;

public sealed class UpdateAvailabilityGroupMemberRequest
{
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }

    [Range(0, int.MaxValue)]
    public int DisplayOrder { get; set; }
}
