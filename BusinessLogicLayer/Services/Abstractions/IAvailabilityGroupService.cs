using BusinessLogicLayer.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Services.Abstractions
{
    public interface IAvailabilityGroupService : IBaseService<AvailabilityGroupModel>
    {
        Task<List<AvailabilityGroupModel>> GetByValueAsync(string value, CancellationToken ct = default);

        Task SaveGroupAsync(
            AvailabilityGroupModel group,
            IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
            CancellationToken ct = default);

        Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
            LoadFullAsync(int groupId, CancellationToken ct = default);
    }

}
