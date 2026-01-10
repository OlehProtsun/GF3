namespace GF3.Presentation.Wpf.Services
{
    public interface IMessageDialogService
    {
        void ShowInfo(string message, string? caption = null);
        void ShowError(string message, string? caption = null);
        bool Confirm(string message, string? caption = null);
    }
}
