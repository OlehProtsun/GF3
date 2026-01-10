using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFApp.Infrastructure
{
    /// <summary>
    /// Базовий VM для WPF (INotifyPropertyChanged + SetProperty).
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Викликає PropertyChanged для однієї властивості.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Ставить значення в backing field і піднімає PropertyChanged тільки якщо значення реально змінилось.
        /// </summary>
        protected bool SetProperty<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Зручно для computed properties: OnPropertyChanged(nameof(A)); OnPropertyChanged(nameof(B)); ...
        /// </summary>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            if (propertyNames is null) return;

            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }
    }
}
