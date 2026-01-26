using System.Runtime.CompilerServices;

namespace WPFApp.Infrastructure
{
    /// <summary>
    /// ObservableObject — сумісний alias базового класу для VM.
    ///
    /// ВАЖЛИВО:
    /// Раніше у вас були два майже однакові базові класи:
    /// - ObservableObject (SetProperty + Raise)
    /// - ViewModelBase (SetProperty + OnPropertyChanged + OnPropertiesChanged)
    ///
    /// Це створювало дублювання і ризики “роз’їзду” поведінки.
    ///
    /// Тепер:
    /// - ViewModelBase є канонічною реалізацією.
    /// - ObservableObject просто наслідує ViewModelBase і залишає метод Raise(...) для існуючого коду.
    /// </summary>
    public abstract class ObservableObject : ViewModelBase
    {
        /// <summary>
        /// Raise — історичний alias для OnPropertyChanged.
        /// Залишаємо його, щоб не ламати існуючі VM, які викликають Raise(...).
        /// </summary>
        protected void Raise([CallerMemberName] string? propName = null)
            => OnPropertyChanged(propName);
    }
}
