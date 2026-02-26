/*
  Опис файлу: цей модуль містить реалізацію компонента IColorPickerService у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows.Media;

namespace WPFApp.UI.Dialogs
{
    /// <summary>
    /// Визначає публічний елемент `public interface IColorPickerService` та контракт його використання у шарі WPFApp.
    /// </summary>
    public interface IColorPickerService
    {
        bool TryPickColor(Color? initialColor, out Color color);
    }
}
