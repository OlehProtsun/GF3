using System.Windows;
using System.Windows.Media;
using WPFApp.View.Dialogs;

namespace WPFApp.Service
{
    public sealed class ColorPickerService : IColorPickerService
    {
        public bool TryPickColor(Color? initialColor, out Color color)
        {
            var dialog = new ColorPickerDialog(initialColor)
            {
                Owner = Application.Current?.MainWindow
            };

            var result = dialog.ShowDialog() == true;
            color = dialog.SelectedColor;
            return result;
        }
    }
}
