using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFApp.ViewModel.Dialogs
{
    public enum CustomMessageBoxIcon { Info, Warning, Error }

    public sealed class CustomMessageBoxViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<bool?>? RequestClose;

        public string Title { get; }
        public string Message { get; }
        public string Details { get; }
        public string OkText { get; }
        public string CancelText { get; }
        public bool IsCancelVisible => !string.IsNullOrWhiteSpace(CancelText);
        public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

        public Geometry IconGeometry { get; }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public CustomMessageBoxViewModel(string title, string message, CustomMessageBoxIcon icon,
            string okText = "OK", string cancelText = "", string details = "")
        {
            Title = title;
            Message = message;
            Details = details;
            OkText = okText;
            CancelText = cancelText;

            IconGeometry = icon switch
            {
                CustomMessageBoxIcon.Warning => (Geometry)Application.Current.FindResource("IconWarn"),
                CustomMessageBoxIcon.Error => (Geometry)Application.Current.FindResource("IconError"),
                _ => (Geometry)Application.Current.FindResource("IconInfo"),
            };

            OkCommand = new RelayCommand(() => RequestClose?.Invoke(true));
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
