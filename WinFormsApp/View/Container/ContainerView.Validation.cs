using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.View.Container
{
    public partial class ContainerView
    {
        private Dictionary<string, Control> CreateContainerErrorMap() => new()
        {
            [nameof(ContainerName)] = inputContainerName,
            [nameof(ContainerNote)] = inputContainerNote
        };

        private Dictionary<string, Control> CreateScheduleErrorMap() => new()
        {
            [nameof(ScheduleName)] = inputScheduleName,
            [nameof(ScheduleYear)] = inputYear,
            [nameof(ScheduleMonth)] = inputMonth,
            [nameof(SchedulePeoplePerShift)] = inputPeoplePerShift,
            [nameof(ScheduleShift1)] = inputShift1,
            [nameof(ScheduleShift2)] = inputShift2,
            [nameof(ScheduleMaxHoursPerEmp)] = inputMaxHours,
            [nameof(ScheduleMaxConsecutiveDays)] = inputMaxConsecutiveDays,
            [nameof(ScheduleMaxConsecutiveFull)] = inputMaxConsecutiveFull,
            [nameof(ScheduleMaxFullPerMonth)] = inputMaxFull
        };

        public void SetValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearValidationErrors();

            foreach (var (key, msg) in errors)
                if (_containerErrorMap.TryGetValue(key, out var control))
                    errorProviderContainer.SetError(control, msg);
        }

        public void SetScheduleValidationErrors(IReadOnlyDictionary<string, string> errors)
        {
            ClearScheduleValidationErrors();

            foreach (var (key, msg) in errors)
                if (_scheduleErrorMap.TryGetValue(key, out var control))
                    errorProviderSchedule.SetError(control, msg);
        }

        public void ClearValidationErrors() => errorProviderContainer.Clear();
        public void ClearScheduleValidationErrors() => errorProviderSchedule.Clear();

        public void ShowInfo(string text) => MessageBox.Show(text, "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        public void ShowError(string text) => MessageBox.Show(text, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        public bool Confirm(string text, string? caption = null)
        {
            return MessageBox.Show(text, caption ?? "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void WireAutoClearValidation()
        {
            // container
            inputContainerName.TextChanged += (_, __) => errorProviderContainer.SetError(inputContainerName, "");
            inputContainerNote.TextChanged += (_, __) => errorProviderContainer.SetError(inputContainerNote, "");

            // schedule (textbox)
            inputScheduleName.TextChanged += (_, __) => errorProviderSchedule.SetError(inputScheduleName, "");
            inputShift1.TextChanged += (_, __) => errorProviderSchedule.SetError(inputShift1, "");
            inputShift2.TextChanged += (_, __) => errorProviderSchedule.SetError(inputShift2, "");

            // schedule (numeric)
            inputYear.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputYear, "");
            inputMonth.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputMonth, "");
            inputPeoplePerShift.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputPeoplePerShift, "");

            inputMaxHours.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputMaxHours, "");
            inputMaxConsecutiveDays.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputMaxConsecutiveDays, "");
            inputMaxConsecutiveFull.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputMaxConsecutiveFull, "");
            inputMaxFull.ValueChanged += (_, __) => errorProviderSchedule.SetError(inputMaxFull, "");
        }

    }
}
