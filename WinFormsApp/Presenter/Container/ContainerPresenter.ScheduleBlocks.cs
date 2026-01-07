using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private sealed class ScheduleBlockState
        {
            public Guid Id { get; init; }
            public ScheduleModel Model { get; set; } = new();
            public List<ScheduleEmployeeModel> Employees { get; set; } = new();
            public List<ScheduleSlotModel> Slots { get; set; } = new();
            public int SelectedAvailabilityGroupId { get; set; }
            public Dictionary<string, string> ValidationErrors { get; set; } = new();
        }

        private readonly List<ScheduleBlockState> _scheduleBlocks = new();
        private Guid? _selectedScheduleBlockId;

        private ScheduleBlockState? GetSelectedBlock()
            => _selectedScheduleBlockId is Guid id
            ? _scheduleBlocks.FirstOrDefault(b => b.Id == id)
            : null;

        private ScheduleBlockState CreateDefaultBlock(int containerId)
        {
            var model = new ScheduleModel
            {
                ContainerId = containerId,
                Year = DateTime.Today.Year,
                Month = DateTime.Today.Month,
                PeoplePerShift = 1,
                MaxHoursPerEmpMonth = 1,
                MaxConsecutiveDays = 1,
                MaxConsecutiveFull = 1,
                MaxFullPerMonth = 1,
                Shift1Time = string.Empty,
                Shift2Time = string.Empty,
                Note = string.Empty
            };

            return new ScheduleBlockState
            {
                Id = Guid.NewGuid(),
                Model = model,
                Employees = new List<ScheduleEmployeeModel>(),
                Slots = new List<ScheduleSlotModel>(),
                SelectedAvailabilityGroupId = GetDefaultAvailabilityGroupId(model.Year, model.Month)
            };
        }

        private ScheduleBlockState CreateBlockFromSchedule(ScheduleModel model,
            IList<ScheduleEmployeeModel> employees,
            IList<ScheduleSlotModel> slots)
        {
            var copy = new ScheduleModel
            {
                Id = model.Id,
                ContainerId = model.ContainerId,
                ShopId = model.ShopId,
                Name = model.Name,
                Year = model.Year,
                Month = model.Month,
                PeoplePerShift = model.PeoplePerShift,
                Shift1Time = model.Shift1Time,
                Shift2Time = model.Shift2Time,
                MaxHoursPerEmpMonth = model.MaxHoursPerEmpMonth,
                MaxConsecutiveDays = model.MaxConsecutiveDays,
                MaxConsecutiveFull = model.MaxConsecutiveFull,
                MaxFullPerMonth = model.MaxFullPerMonth,
                Note = model.Note
            };

            return new ScheduleBlockState
            {
                Id = Guid.NewGuid(),
                Model = copy,
                Employees = employees?.ToList() ?? new List<ScheduleEmployeeModel>(),
                Slots = slots?.ToList() ?? new List<ScheduleSlotModel>(),
                SelectedAvailabilityGroupId = GetDefaultAvailabilityGroupId(copy.Year, copy.Month)
            };
        }

        private int GetDefaultAvailabilityGroupId(int year, int month)
        {
            return _allAvailabilityGroups
                .Where(g => g.Year == year && g.Month == month)
                .Select(g => g.Id)
                .FirstOrDefault();
        }

        private void CaptureSelectedBlockFromView()
        {
            var block = GetSelectedBlock();
            if (block == null) return;

            block.Model = BuildScheduleFromView();
            block.SelectedAvailabilityGroupId = _view.SelectedAvailabilityGroupId;
            block.Employees = _view.ScheduleEmployees?.ToList() ?? new List<ScheduleEmployeeModel>();
            block.Slots = _view.ScheduleSlots?.ToList() ?? new List<ScheduleSlotModel>();
        }

        private void ApplyBlockToView(ScheduleBlockState block)
        {
            _view.ScheduleId = block.Model.Id;
            _view.ScheduleContainerId = block.Model.ContainerId;
            _view.ScheduleShopId = block.Model.ShopId;
            _view.ScheduleName = block.Model.Name ?? string.Empty;
            _view.ScheduleYear = block.Model.Year;
            _view.ScheduleMonth = block.Model.Month;
            _view.SchedulePeoplePerShift = block.Model.PeoplePerShift;
            _view.ScheduleShift1 = block.Model.Shift1Time ?? string.Empty;
            _view.ScheduleShift2 = block.Model.Shift2Time ?? string.Empty;
            _view.ScheduleMaxHoursPerEmp = block.Model.MaxHoursPerEmpMonth;
            _view.ScheduleMaxConsecutiveDays = block.Model.MaxConsecutiveDays;
            _view.ScheduleMaxConsecutiveFull = block.Model.MaxConsecutiveFull;
            _view.ScheduleMaxFullPerMonth = block.Model.MaxFullPerMonth;
            _view.ScheduleNote = block.Model.Note ?? string.Empty;

            _view.SetSelectedAvailabilityGroupId(block.SelectedAvailabilityGroupId, fireEvent: false);
            _view.ScheduleEmployees = block.Employees.ToList();
            _view.ScheduleSlots = block.Slots.ToList();
        }

        private async Task OnScheduleBlockSelectCoreAsync(Guid blockId, CancellationToken ct)
        {
            if (!_scheduleBlocks.Any(b => b.Id == blockId))
                return;

            CaptureSelectedBlockFromView();
            _selectedScheduleBlockId = blockId;

            var block = GetSelectedBlock();
            if (block == null) return;

            _view.ClearScheduleValidationErrors();
            _view.SetSelectedScheduleBlock(blockId);
            ApplyBlockToView(block);

            if (block.ValidationErrors.Count > 0)
                _view.SetScheduleValidationErrors(block.ValidationErrors);

            await UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private async Task OnScheduleBlockCloseCoreAsync(Guid blockId, CancellationToken ct)
        {
            if (!_scheduleBlocks.Any(b => b.Id == blockId))
                return;

            if (!_view.Confirm("Are you sure you want to close this schedule?"))
                return;

            var index = _scheduleBlocks.FindIndex(b => b.Id == blockId);
            _scheduleBlocks.RemoveAll(b => b.Id == blockId);
            _view.RemoveScheduleBlock(blockId);

            if (_scheduleBlocks.Count == 0)
            {
                _selectedScheduleBlockId = null;
                _view.ClearSelectedScheduleBlock();
                _view.ClearScheduleInputs();
                return;
            }

            var nextIndex = Math.Max(0, Math.Min(index, _scheduleBlocks.Count - 1));
            var next = _scheduleBlocks[nextIndex];
            _selectedScheduleBlockId = next.Id;
            _view.SetSelectedScheduleBlock(next.Id);
            ApplyBlockToView(next);

            if (next.ValidationErrors.Count > 0)
                _view.SetScheduleValidationErrors(next.ValidationErrors);
            else
                _view.ClearScheduleValidationErrors();

            await UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private Task OnScheduleAddNewBlockCoreAsync(CancellationToken ct)
        {
            if (_view.IsEdit)
                return Task.CompletedTask;

            CaptureSelectedBlockFromView();

            var block = CreateDefaultBlock(_view.ScheduleContainerId);
            _scheduleBlocks.Add(block);
            _view.AddScheduleBlock(block.Id);
            _selectedScheduleBlockId = block.Id;
            _view.SetSelectedScheduleBlock(block.Id);
            ApplyBlockToView(block);

            return UpdateAvailabilityPreviewCoreAsync(ct);
        }

        private void ResetScheduleBlocks()
        {
            _scheduleBlocks.Clear();
            _selectedScheduleBlockId = null;
            _view.ClearSelectedScheduleBlock();
            _view.ClearScheduleBlocks();
        }
    }
}
