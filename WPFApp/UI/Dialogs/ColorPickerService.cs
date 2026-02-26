/*
  Опис файлу: цей модуль містить реалізацію компонента ColorPickerService у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows;
using System.Windows.Media;
using WPFApp.View.Dialogs;

namespace WPFApp.UI.Dialogs
{
    /// <summary>
    /// Визначає публічний елемент `public sealed class ColorPickerService : IColorPickerService` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed class ColorPickerService : IColorPickerService
    {
        /// <summary>
        /// Визначає публічний елемент `public bool TryPickColor(Color? initialColor, out Color color)` та контракт його використання у шарі WPFApp.
        /// </summary>
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
