using BusinessLogicLayer.Services.Abstractions;
using DataAccessLayer.Models;
using DataAccessLayer.Models.Enums;
using DataAccessLayer.Repositories.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Services
{
    // Можеш (необов'язково) наслідувати GenericService, якщо хочеш базовий CRUD для Group.
    // Якщо не треба — прибери ": GenericService<AvailabilityGroupModel>" і base(groupRepo).
    public class AvailabilityGroupService : GenericService<AvailabilityGroupModel>, IAvailabilityGroupService
    {
        private readonly IAvailabilityGroupRepository _groupRepo;
        private readonly IAvailabilityGroupMemberRepository _memberRepo;
        private readonly IAvailabilityGroupDayRepository _dayRepo;

        public AvailabilityGroupService(
            IAvailabilityGroupRepository groupRepo,
            IAvailabilityGroupMemberRepository memberRepo,
            IAvailabilityGroupDayRepository dayRepo)
            : base(groupRepo)
        {
            _groupRepo = groupRepo;
            _memberRepo = memberRepo;
            _dayRepo = dayRepo;
        }

        public async Task<List<AvailabilityGroupModel>> GetByValueAsync(
            string value,
            CancellationToken ct = default)
        {
            return await _groupRepo.GetByValueAsync(value, ct);
        }

        public async Task<(AvailabilityGroupModel group, List<AvailabilityGroupMemberModel> members, List<AvailabilityGroupDayModel> days)>
            LoadFullAsync(int groupId, CancellationToken ct = default)
        {
            // Якщо ти додав GetFullByIdAsync у репозиторій — це найзручніше:
            var full = await _groupRepo.GetFullByIdAsync(groupId, ct);
            if (full is null)
                throw new InvalidOperationException($"AvailabilityGroup with Id={groupId} not found.");

            var members = full.Members?.ToList() ?? new List<AvailabilityGroupMemberModel>();

            // days можуть бути вже включені (якщо ти include-ив їх у GetFullByIdAsync)
            // але якщо ні — доберемо через dayRepo:
            var days = await _dayRepo.GetByGroupIdAsync(groupId, ct);
            return (full, members, days);
        }

        public async Task SaveGroupAsync(
            AvailabilityGroupModel group,
            IList<(int employeeId, IList<AvailabilityGroupDayModel> days)> payload,
            CancellationToken ct = default)
        {

            if (group is null) throw new ArgumentNullException(nameof(group));
            if (payload is null) throw new ArgumentNullException(nameof(payload));

            // 1) Save group (важливо: гарантуємо, що group.Id оновиться у того ж інстансу)
            if (group.Id == 0)
            {
                var created = await _groupRepo.AddAsync(group, ct);
                group.Id = created.Id; // <-- ключовий рядок
            }
            else
            {
                await _groupRepo.UpdateAsync(group, ct);
            }

            var groupId = group.Id;


            // 2) Поточні members групи (1 запит)
            var existingMembers = await _memberRepo.GetByGroupIdAsync(groupId, ct);

            // Будуємо lookup employeeId -> member
            var memberByEmployee = existingMembers.ToDictionary(m => m.EmployeeId);

            // 3) Які мають лишитись
            var desiredEmployeeIds = payload.Select(x => x.employeeId).Distinct().ToHashSet();

            // 4) Видаляємо зайвих members (дні каскадно видаляться, якщо налаштовано)
            foreach (var m in existingMembers)
            {
                if (!desiredEmployeeIds.Contains(m.EmployeeId))
                    await _memberRepo.DeleteAsync(m.Id, ct);
            }

            // 5) Upsert member + replace days
            foreach (var (employeeId, days) in payload)
            {
                // 5.1) Member: беремо з lookup (без додаткового SELECT)
                if (!memberByEmployee.TryGetValue(employeeId, out var member))
                {
                    member = await _memberRepo.AddAsync(new AvailabilityGroupMemberModel
                    {
                        Id = 0,
                        AvailabilityGroupId = groupId,
                        EmployeeId = employeeId
                    }, ct);

                    memberByEmployee[employeeId] = member;
                }

                var memberId = member.Id;

                // 5.2) Replace days
                await _dayRepo.DeleteByMemberIdAsync(memberId, ct);

                foreach (var d in days)
                {
                    d.Id = 0;
                    d.AvailabilityGroupMemberId = memberId;

                    if (d.Kind != AvailabilityKind.INT)
                        d.IntervalStr = null;
                }

                await _dayRepo.AddRangeAsync(days, ct);

            }
        }
    }
}
