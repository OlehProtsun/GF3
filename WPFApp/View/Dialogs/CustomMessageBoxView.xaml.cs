/*
  Опис файлу: цей модуль містить реалізацію компонента CustomMessageBoxView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Windows;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.View.Dialogs
{
    /// <summary>
    /// Визначає публічний елемент `public partial class CustomMessageBoxView : Window` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class CustomMessageBoxView : Window
    {
        /// <summary>
        /// Визначає публічний елемент `public CustomMessageBoxView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public CustomMessageBoxView()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                if (DataContext is CustomMessageBoxViewModel vm)
                {
                    vm.RequestClose += result =>
                    {
                        DialogResult = result;
                        Close();
                    };
                }
            };
        }

        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CustomMessageBoxViewModel vm)
                return;

            var text = vm.Details ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch
            {
                Clipboard.SetDataObject(text, true);
            }
        }
    }
}
