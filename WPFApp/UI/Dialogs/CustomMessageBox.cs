/*
  Опис файлу: цей модуль містить реалізацію компонента CustomMessageBox у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using WPFApp.View.Dialogs;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.UI.Dialogs
{
    /// <summary>
    /// Визначає публічний елемент `public static class CustomMessageBox` та контракт його використання у шарі WPFApp.
    /// </summary>
    public static class CustomMessageBox
    {
        /// <summary>
        /// Визначає публічний елемент `public static bool Show(string title, string message,` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static bool Show(string title, string message,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.Info,
            string okText = "OK", string cancelText = "", string? details = null)
        {
            var owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                        ?? Application.Current.MainWindow;

            var vm = new CustomMessageBoxViewModel(title, message, icon, okText, cancelText, details ?? string.Empty);

            var wnd = new CustomMessageBoxView
            {
                Owner = owner,
                DataContext = vm
            };

            return wnd.ShowDialog() == true;
        }
    }
}
