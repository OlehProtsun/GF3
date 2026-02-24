using System.Windows.Media;

namespace WPFApp.UI.Dialogs
{
    public interface IColorPickerService
    {
        bool TryPickColor(Color? initialColor, out Color color);
    }
}
