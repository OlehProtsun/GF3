/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Ui у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Threading.Tasks;
using System.Windows;              
using System.Windows.Media;        
using WPFApp.Applications.Diagnostics;
using WPFApp.UI.Dialogs;
using WPFApp.ViewModel.Dialogs;    

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal bool TryPickScheduleCellColor(Color? initialColor, out Color color)
            => _colorPickerService.TryPickColor(initialColor, out color);

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        internal Task RunOnUiThreadAsync(Action action)
        {
            if (Application.Current?.Dispatcher is null)
            {
                action();
                return Task.CompletedTask;
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }

            return Application.Current.Dispatcher.InvokeAsync(action).Task;
        }

        
        
        
        
        internal void ShowInfo(string text)
            => CustomMessageBox.Show("Info", text, CustomMessageBoxIcon.Info, okText: "OK");

        
        
        
        internal void ShowError(string text)
            => CustomMessageBox.Show("Error", text, CustomMessageBoxIcon.Error, okText: "OK");

        
        
        
        
        
        
        
        
        
        
        internal void ShowError(Exception ex)
        {
            var (summary, details) = ExceptionMessageBuilder.Build(ex);

            CustomMessageBox.Show(
                "Error",
                summary,
                CustomMessageBoxIcon.Error,
                okText: "OK",
                details: details);
        }

        
        
        
        
        
        
        
        
        
        
        internal bool Confirm(string text, string? caption = null)
            => CustomMessageBox.Show(
                caption ?? "Confirm",
                text,
                CustomMessageBoxIcon.Warning,
                okText: "Yes",
                cancelText: "No");
    }
}
