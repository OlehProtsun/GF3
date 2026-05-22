using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Enums;
using BusinessLogicLayer.Contracts.Models;
using WebApi.Contracts.AvailabilityBinds;
using WebApi.Contracts.AvailabilityGroups;
using WebApi.Contracts.AvailabilityGroups.Members;
using WebApi.Contracts.AvailabilityGroups.Slots;
using WebApi.Contracts.Containers.Graphs;
using WebApi.Contracts.Containers.Graphs.CellStyles;
using WebApi.Contracts.Containers.Graphs.Employees;
using WebApi.Contracts.Containers.Graphs.Slots;
using WebApi.Contracts.Containers.SchedulePresets;
using WebApi.Mappers;

namespace GF3.Tests;

public sealed class WebApiMapperCoverageTests
{
    [Fact]
    public void GraphMapper_RoundTripsGraphContractsAndPublicationStatus()
    {
        var model = new ScheduleModel
        {
            Id = 7,
            ContainerId = 3,
            ShopId = 5,
            Name = "May Graph",
            Year = 2026,
            Month = 5,
            PublicationStatus = SchedulePublicationStatus.Public,
            PeoplePerShift = 2,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            Note = "Planner note",
            AvailabilityGroupId = 11,
        };

        var dto = model.ToGraphDto();
        var createModel = new CreateGraphRequest
        {
            ShopId = 5,
            Name = "Created",
            Year = 2026,
            Month = 6,
            PublicationStatus = "PUBLIC",
            PeoplePerShift = 3,
            Shift1Time = "07:00 - 15:00",
            Shift2Time = "15:00 - 23:00",
            MaxHoursPerEmpMonth = 150,
            MaxConsecutiveDays = 4,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 8,
            Note = "Create note",
            AvailabilityGroupId = 12,
        }.ToCreateModel(containerId: 9);
        var updateModel = new UpdateGraphRequest
        {
            ShopId = 6,
            Name = "Updated",
            Year = 2027,
            Month = 7,
            PublicationStatus = null,
            PeoplePerShift = 1,
            Shift1Time = "09:00 - 13:00",
            Shift2Time = "13:00 - 17:00",
            MaxHoursPerEmpMonth = 120,
            MaxConsecutiveDays = 3,
            MaxConsecutiveFull = 1,
            MaxFullPerMonth = 6,
            Note = null,
            AvailabilityGroupId = null,
        }.ToUpdateModel(containerId: 10, graphId: 15);
        var previewModel = new GenerateGraphPreviewRequest
        {
            GraphId = 20,
            Graph = new CreateGraphRequest
            {
                ShopId = 8,
                Name = "Preview",
                Year = 2028,
                Month = 8,
                PublicationStatus = "private",
                PeoplePerShift = 2,
                Shift1Time = "06:00 - 14:00",
                Shift2Time = "14:00 - 22:00",
                MaxHoursPerEmpMonth = 140,
                MaxConsecutiveDays = 6,
                MaxConsecutiveFull = 4,
                MaxFullPerMonth = 9,
                Note = "Preview note",
                AvailabilityGroupId = 13,
            },
        }.ToPreviewModel(containerId: 11);

        Assert.Equal("public", dto.PublicationStatus);
        Assert.Equal("Planner note", dto.Note);
        Assert.Equal(11, dto.AvailabilityGroupId);

        Assert.Equal(9, createModel.ContainerId);
        Assert.Equal(SchedulePublicationStatus.Public, createModel.PublicationStatus);
        Assert.Equal("Create note", createModel.Note);

        Assert.Equal(15, updateModel.Id);
        Assert.Equal(10, updateModel.ContainerId);
        Assert.Equal(SchedulePublicationStatus.Private, updateModel.PublicationStatus);
        Assert.Null(updateModel.AvailabilityGroupId);

        Assert.Equal(20, previewModel.Id);
        Assert.Equal(11, previewModel.ContainerId);
        Assert.Equal(SchedulePublicationStatus.Private, previewModel.PublicationStatus);
        Assert.Equal(13, previewModel.AvailabilityGroupId);
    }

    [Fact]
    public void GraphNestedMappers_MapSlotsEmployeesStylesAndReplacementBatches()
    {
        var slotModel = new ScheduleSlotModel
        {
            Id = 4,
            ScheduleId = 10,
            DayOfMonth = 2,
            SlotNo = 1,
            FromTime = "08:00",
            ToTime = "12:00",
            EmployeeId = 7,
            Status = SlotStatus.ASSIGNED,
        };
        var slotDto = slotModel.ToGraphSlotDto();
        var createdSlot = new CreateGraphSlotRequest
        {
            DayOfMonth = 3,
            SlotNo = 2,
            FromTime = "12:00",
            ToTime = "16:00",
            EmployeeId = null,
            Status = SlotStatus.UNFURNISHED,
        }.ToCreateModel(graphId: 20);
        var updatedSlot = new UpdateGraphSlotRequest
        {
            DayOfMonth = 4,
            SlotNo = 3,
            FromTime = "16:00",
            ToTime = "20:00",
            EmployeeId = 9,
            Status = SlotStatus.ASSIGNED,
        }.ToUpdateModel(graphId: 21, slotId: 30);
        var replacement = new ReplaceGraphSlotsRequest
        {
            Slots =
            [
                new CreateGraphSlotRequest
                {
                    DayOfMonth = 5,
                    SlotNo = 1,
                    FromTime = "09:00",
                    ToTime = "11:00",
                    EmployeeId = 12,
                    Status = SlotStatus.ASSIGNED,
                },
            ],
        }.ToReplaceModels(graphId: 22);

        var employeeDto = new ScheduleEmployeeModel
        {
            Id = 3,
            ScheduleId = 10,
            EmployeeId = 7,
            MinHoursMonth = 80,
            DisplayOrder = 2,
        }.ToGraphEmployeeDto();
        var addedEmployee = new AddGraphEmployeeRequest
        {
            EmployeeId = 8,
            MinHoursMonth = null,
            DisplayOrder = 4,
        }.ToAddModel(graphId: 20);
        var updatedEmployee = new UpdateGraphEmployeeRequest
        {
            EmployeeId = 9,
            MinHoursMonth = 100,
            DisplayOrder = 5,
        }.ToUpdateModel(graphId: 21, graphEmployeeId: 31);
        var previewEmployee = new GenerateGraphPreviewEmployeeRequest
        {
            EmployeeId = 10,
            MinHoursMonth = 90,
            DisplayOrder = 6,
        }.ToPreviewModel(graphId: 22);

        var styleDto = new ScheduleCellStyleModel
        {
            Id = 6,
            ScheduleId = 10,
            EmployeeId = 7,
            DayOfMonth = 9,
            BackgroundColorArgb = -1,
            TextColorArgb = -16777216,
        }.ToGraphCellStyleDto();
        var upsertedStyle = new UpsertGraphCellStyleRequest
        {
            EmployeeId = 11,
            DayOfMonth = 12,
            BackgroundColorArgb = 123,
            TextColorArgb = null,
        }.ToUpsertModel(graphId: 23);

        Assert.Equal(4, slotDto.Id);
        Assert.Equal(SlotStatus.ASSIGNED, slotDto.Status);
        Assert.Equal(20, createdSlot.ScheduleId);
        Assert.Equal(SlotStatus.UNFURNISHED, createdSlot.Status);
        Assert.Equal(30, updatedSlot.Id);
        Assert.Equal(21, updatedSlot.ScheduleId);
        Assert.Single(replacement);
        Assert.Equal(22, replacement[0].ScheduleId);
        Assert.Equal(12, replacement[0].EmployeeId);

        Assert.Equal(80, employeeDto.MinHoursMonth);
        Assert.Equal(20, addedEmployee.ScheduleId);
        Assert.Null(addedEmployee.MinHoursMonth);
        Assert.Equal(31, updatedEmployee.Id);
        Assert.Equal(22, previewEmployee.ScheduleId);

        Assert.Equal(-1, styleDto.BackgroundColorArgb);
        Assert.Equal(23, upsertedStyle.ScheduleId);
        Assert.Equal(11, upsertedStyle.EmployeeId);
        Assert.Null(upsertedStyle.TextColorArgb);
    }

    [Fact]
    public void AvailabilityMappers_MapRequestsFlattenItemsAndValidatePublicationStatus()
    {
        var visibleFrom = new DateTimeOffset(2026, 5, 1, 8, 0, 0, TimeSpan.Zero);
        var visibleTo = visibleFrom.AddDays(7);
        var groupModel = new AvailabilityGroupModel
        {
            Id = 5,
            Name = "May availability",
            Year = 2026,
            Month = 5,
            PublicationStatus = AvailabilityPublicationStatus.Public,
            VisibleFromUtc = visibleFrom,
            VisibleToUtc = visibleTo,
        };
        var groupDto = groupModel.ToApiDto();
        var createModel = new CreateAvailabilityGroupRequest
        {
            Name = "Created",
            Year = 2026,
            Month = 6,
            PublicationStatus = "PUBLIC",
            VisibleFromUtc = visibleFrom,
            VisibleToUtc = visibleTo,
        }.ToCreateModel();
        var updateModel = new UpdateAvailabilityGroupRequest
        {
            Name = "Updated",
            Year = 2027,
            Month = 7,
            PublicationStatus = null,
            VisibleFromUtc = null,
            VisibleToUtc = null,
        }.ToUpdateModel(id: 10);

        var member = new AvailabilityGroupMemberModel
        {
            Id = 3,
            AvailabilityGroupId = 5,
            EmployeeId = 8,
            DisplayOrder = 2,
            EmployeeLastModifiedAtUtc = visibleFrom,
        };
        var memberDto = member.ToMemberDto();
        var createdMember = new CreateAvailabilityGroupMemberRequest
        {
            EmployeeId = 9,
            DisplayOrder = 4,
        }.ToCreateMemberModel(groupId: 6);
        var updatedMember = new UpdateAvailabilityGroupMemberRequest
        {
            EmployeeId = 10,
            DisplayOrder = 5,
        }.ToUpdateMemberModel(groupId: 7, memberId: 11);

        var slot = new AvailabilityGroupDayModel
        {
            Id = 30,
            AvailabilityGroupMemberId = member.Id,
            DayOfMonth = 12,
            Kind = AvailabilityKind.INT,
            IntervalStr = "09:00 - 12:00",
        };
        var slotDto = slot.ToSlotDto();
        var createdSlot = new CreateAvailabilitySlotRequest
        {
            AvailabilityGroupMemberId = 13,
            DayOfMonth = 14,
            Kind = AvailabilityKind.ANY,
            IntervalStr = null,
        }.ToCreateSlotModel();
        var updatedSlot = new UpdateAvailabilitySlotRequest
        {
            AvailabilityGroupMemberId = 15,
            DayOfMonth = 16,
            Kind = AvailabilityKind.NONE,
            IntervalStr = "Unavailable",
        }.ToUpdateSlotModel(slotId: 17);
        var flattenedItems = new[] { member }.ToItemDtos([
            slot,
            new AvailabilityGroupDayModel
            {
                Id = 99,
                AvailabilityGroupMemberId = 999,
                DayOfMonth = 1,
                Kind = AvailabilityKind.ANY,
            },
        ]).ToList();

        var invalidStatus = Assert.Throws<ValidationException>(() =>
            new CreateAvailabilityGroupRequest { PublicationStatus = "shared" }.ToCreateModel());

        Assert.Equal("public", groupDto.PublicationStatus);
        Assert.Equal(AvailabilityPublicationStatus.Public, createModel.PublicationStatus);
        Assert.Equal(AvailabilityPublicationStatus.Private, updateModel.PublicationStatus);
        Assert.Equal(10, updateModel.Id);
        Assert.Equal(visibleFrom, groupDto.VisibleFromUtc);
        Assert.Equal("Publication status must be private or public.", invalidStatus.Message);
        Assert.Contains(nameof(CreateAvailabilityGroupRequest.PublicationStatus), invalidStatus.Errors.Keys);

        Assert.Equal(visibleFrom, memberDto.EmployeeLastModifiedAtUtc);
        Assert.Equal(6, createdMember.AvailabilityGroupId);
        Assert.Equal(11, updatedMember.Id);
        Assert.Equal(7, updatedMember.AvailabilityGroupId);

        Assert.Equal("09:00 - 12:00", slotDto.IntervalStr);
        Assert.Equal(13, createdSlot.AvailabilityGroupMemberId);
        Assert.Equal(17, updatedSlot.Id);
        Assert.Single(flattenedItems);
        Assert.Equal(member.EmployeeId, flattenedItems[0].EmployeeId);
        Assert.Equal(slot.Id, flattenedItems[0].DayId);
    }

    [Fact]
    public void SchedulePresetAndBindMappers_MapNestedCollections()
    {
        var preset = new SchedulePresetModel
        {
            Id = 4,
            ContainerId = 2,
            Name = "Preset",
            ScheduleName = "June graph",
            ShopId = 3,
            Year = 2026,
            Month = 6,
            PeoplePerShift = 2,
            Shift1Time = "08:00 - 16:00",
            Shift2Time = "16:00 - 20:00",
            MaxHoursPerEmpMonth = 160,
            MaxConsecutiveDays = 5,
            MaxConsecutiveFull = 3,
            MaxFullPerMonth = 10,
            AvailabilityGroupId = 12,
            Employees =
            [
                new SchedulePresetEmployeeModel
                {
                    Id = 9,
                    EmployeeId = 7,
                    MinHoursMonth = 80,
                },
            ],
        };
        var presetDto = preset.ToSchedulePresetDto();
        var createdPreset = new CreateSchedulePresetRequest
        {
            Name = "Created preset",
            ScheduleName = "Created graph",
            ShopId = 5,
            Year = 2027,
            Month = 7,
            PeoplePerShift = 1,
            Shift1Time = "07:00 - 15:00",
            Shift2Time = "15:00 - 23:00",
            MaxHoursPerEmpMonth = 140,
            MaxConsecutiveDays = 4,
            MaxConsecutiveFull = 2,
            MaxFullPerMonth = 8,
            AvailabilityGroupId = null,
            Employees =
            [
                new CreateSchedulePresetEmployeeRequest
                {
                    EmployeeId = 11,
                    MinHoursMonth = 90,
                },
            ],
        }.ToCreateModel(containerId: 6);

        var bindDto = new BindModel
        {
            Id = 3,
            Key = "A",
            Value = "+",
            IsActive = true,
        }.ToApiDto();
        var createdBind = new CreateAvailabilityBindRequest
        {
            Key = "N",
            Value = "-",
            IsActive = false,
        }.ToCreateModel();
        var updatedBind = new UpdateAvailabilityBindRequest
        {
            Key = "P",
            Value = "09:00 - 12:00",
            IsActive = true,
        }.ToUpdateModel(id: 8);

        Assert.Equal(4, presetDto.Id);
        Assert.Equal(12, presetDto.AvailabilityGroupId);
        Assert.Single(presetDto.Employees);
        Assert.Equal(7, presetDto.Employees[0].EmployeeId);
        Assert.Equal(6, createdPreset.ContainerId);
        Assert.Single(createdPreset.Employees);
        Assert.Equal(11, Assert.Single(createdPreset.Employees).EmployeeId);
        Assert.Null(createdPreset.AvailabilityGroupId);

        Assert.Equal("+", bindDto.Value);
        Assert.Equal("N", createdBind.Key);
        Assert.False(createdBind.IsActive);
        Assert.Equal(8, updatedBind.Id);
        Assert.Equal("09:00 - 12:00", updatedBind.Value);
    }
}
