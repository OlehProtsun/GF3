using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WinFormsApp.View.Container;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private static Dictionary<string, string> ValidateAndNormalizeSchedule(
            ScheduleModel model,
            out string? normalizedShift1,
            out string? normalizedShift2)
        {
            var errors = new Dictionary<string, string>();
            normalizedShift1 = null;
            normalizedShift2 = null;

            if (model.ContainerId <= 0)
                errors[nameof(IContainerView.ScheduleContainerId)] = "Select a container.";
            if (string.IsNullOrWhiteSpace(model.Name))
                errors[nameof(IContainerView.ScheduleName)] = "Name is required.";
            if (model.Year < 1900)
                errors[nameof(IContainerView.ScheduleYear)] = "Year is invalid.";
            if (model.Month < 1 || model.Month > 12)
                errors[nameof(IContainerView.ScheduleMonth)] = "Month must be 1-12.";
            if (model.PeoplePerShift <= 0)
                errors[nameof(IContainerView.SchedulePeoplePerShift)] = "People per shift must be greater than zero.";
            if (model.MaxHoursPerEmpMonth <= 0)
                errors[nameof(IContainerView.ScheduleMaxHoursPerEmp)] = "Max hours per employee must be greater than zero.";

            if (string.IsNullOrWhiteSpace(model.Shift1Time))
            {
                errors[nameof(IContainerView.ScheduleShift1)] = "Shift1 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift1Time, out normalizedShift1, out var err1))
            {
                errors[nameof(IContainerView.ScheduleShift1)] = err1 ?? "Invalid shift1 format.";
            }

            if (string.IsNullOrWhiteSpace(model.Shift2Time))
            {
                errors[nameof(IContainerView.ScheduleShift2)] = "Shift2 is required.";
            }
            else if (!TryNormalizeShiftRange(model.Shift2Time, out normalizedShift2, out var err2))
            {
                errors[nameof(IContainerView.ScheduleShift2)] = err2 ?? "Invalid shift2 format.";
            }

            return errors;
        }

        private static bool TryParseTime(string s, out TimeSpan t)
        {
            return TimeSpan.TryParseExact(
                (s ?? "").Trim(),
                new[] { @"h\:mm", @"hh\:mm" },
                CultureInfo.InvariantCulture,
                out t);
        }

        private static bool TryNormalizeShiftRange(string? input, out string normalized, out string? error)
        {
            normalized = "";
            error = null;

            input = (input ?? "").Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Shift is required.";
                return false;
            }

            var parts = input.Split('-', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                error = "Format: HH:mm-HH:mm (spaces don't matter)";
                return false;
            }

            if (!TryParseTime(parts[0], out var from) || !TryParseTime(parts[1], out var to))
            {
                error = "Time must be H:mm or HH:mm (e.g. 9:00-15:00)";
                return false;
            }

            if (from >= to)
            {
                error = "From must be earlier than To";
                return false;
            }

            normalized = $"{from:hh\\:mm} - {to:hh\\:mm}";
            return true;
        }
    }
}
