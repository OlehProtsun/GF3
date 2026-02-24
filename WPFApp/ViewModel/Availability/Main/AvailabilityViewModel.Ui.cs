using System;
using System.Threading.Tasks;
using System.Windows;
using WPFApp.Applications.Diagnostics;
using WPFApp.UI.Dialogs;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Ui — централізовані повідомлення користувачу.
    /// Виносимо в окремий partial, щоб:
    /// - CRUD/навігація не змішувались з діалогами
    /// - “як показувати помилку” було одноманітним
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        internal Task RunOnUiThreadAsync(Action action)
        {
            var d = Application.Current?.Dispatcher;
            if (d is null || d.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return d.InvokeAsync(action).Task;
        }

        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);
            CustomMessageBox.Show("Error", summary, CustomMessageBoxIcon.Error, okText: "OK", details: details);
        }

        private bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}
