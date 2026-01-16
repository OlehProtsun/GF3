using System.Windows.Media;

namespace WPFApp.Service
{
    public interface IColorPickerService
    {
        bool TryPickColor(Color? initialColor, out Color color);
    }
}
