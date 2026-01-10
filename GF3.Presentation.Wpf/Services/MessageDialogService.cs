using System.Windows;

namespace GF3.Presentation.Wpf.Services
{
    public class MessageDialogService : IMessageDialogService
    {
        public void ShowInfo(string message, string? caption = null)
        {
            MessageBox.Show(message, caption ?? "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowError(string message, string? caption = null)
        {
            MessageBox.Show(message, caption ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool Confirm(string message, string? caption = null)
        {
            var result = MessageBox.Show(message, caption ?? "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
