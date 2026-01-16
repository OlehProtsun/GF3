using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GF3.Presentation.Wpf.ViewModels
{
    public abstract class ValidatableViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return _errors.Values;
            }

            return _errors.TryGetValue(propertyName, out var errors) ? errors : Array.Empty<string>();
        }

        protected void SetErrors(string propertyName, params string[] errors)
        {
            if (errors.Length == 0)
            {
                if (_errors.Remove(propertyName))
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                }
                return;
            }

            _errors[propertyName] = new List<string>(errors);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected void ClearErrors([CallerMemberName] string? propertyName = null)
        {
            if (propertyName is null)
            {
                return;
            }

            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }
    }
}
