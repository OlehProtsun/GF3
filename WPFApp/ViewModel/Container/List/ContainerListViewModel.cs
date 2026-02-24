using System.Collections.ObjectModel;    // ObservableCollection: колекція, яка повідомляє UI про зміни
using DataAccessLayer.Models;            // ContainerModel: модель контейнера з DAL
using WPFApp.MVVM.Commands;
using WPFApp.MVVM.Core;
using WPFApp.ViewModel.Container.Edit;             // ViewModelBase, AsyncRelayCommand (базова інфраструктура VM/команд)

namespace WPFApp.ViewModel.Container.List
{
    /// <summary>
    /// ContainerListViewModel — ViewModel екрану “список контейнерів”.
    ///
    /// Це “тонкий” VM:
    /// - він НЕ містить бізнес-логіки роботи з базою
    /// - він НЕ виконує запити напряму
    /// - він тільки зберігає стан UI (список, selected item, search text)
    ///   і делегує дії Owner'у (ContainerViewModel).
    ///
    /// Такий підхід правильний, бо:
    /// - Owner відповідає за навігацію/операції/запити (одне місце правди)
    /// - List VM залишається простим і стабільним
    /// </summary>
    public sealed class ContainerListViewModel : ViewModelBase
    {
        /// <summary>
        /// _owner — “керівник” цього екрану (ContainerViewModel).
        ///
        /// Він виконує реальні операції:
        /// - SearchAsync()        -> пошук контейнерів
        /// - StartAddAsync()      -> початок створення нового контейнера
        /// - EditSelectedAsync()  -> редагування обраного контейнера
        /// - DeleteSelectedAsync()-> видалення обраного контейнера
        /// - OpenProfileAsync()   -> відкрити профіль контейнера
        ///
        /// Тут, у ListVM, ми лише викликаємо ці методи командою.
        /// </summary>
        private readonly ContainerViewModel _owner;

        /// <summary>
        /// Items — список контейнерів для показу в UI (DataGrid/ListView).
        ///
        /// ObservableCollection потрібна, щоб WPF:
        /// - автоматично підхоплював додавання/видалення елементів
        /// - оновлював список без ручних “refresh”
        /// </summary>
        public ObservableCollection<ContainerModel> Items { get; } = new();

        /// <summary>
        /// SelectedItem — контейнер, який користувач зараз вибрав у списку.
        ///
        /// Використовується для команд:
        /// - Edit
        /// - Delete
        /// - OpenProfile
        ///
        /// Команди мають бути активні тільки коли SelectedItem != null,
        /// тому в setter ми викликаємо UpdateSelectionCommands().
        /// </summary>
        private ContainerModel? _selectedItem;
        public ContainerModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                // SetProperty:
                // - порівнює старе/нове значення
                // - якщо реально змінилось — присвоює
                // - піднімає PropertyChanged(nameof(SelectedItem))
                if (SetProperty(ref _selectedItem, value))
                    UpdateSelectionCommands(); // оновлюємо CanExecute для Edit/Delete/OpenProfile
            }
        }

        /// <summary>
        /// SearchText — текст пошуку, який вводить користувач.
        ///
        /// У цьому VM пошук запускається командою SearchCommand (по кнопці/Enter),
        /// тому в setter немає debounce і немає автоматичного виклику SearchAsync().
        /// </summary>
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        // -----------------------
        // Команди UI
        // -----------------------

        /// <summary>
        /// SearchCommand — “знайти/оновити список контейнерів”.
        /// Делегуємо в _owner.SearchAsync().
        /// </summary>
        public AsyncRelayCommand SearchCommand { get; }

        /// <summary>
        /// AddNewCommand — створити новий контейнер (відкрити форму Add).
        /// Делегуємо в _owner.StartAddAsync().
        /// </summary>
        public AsyncRelayCommand AddNewCommand { get; }

        /// <summary>
        /// EditCommand — редагувати вибраний контейнер.
        /// Можна виконати тільки коли SelectedItem != null.
        /// Делегуємо в _owner.EditSelectedAsync().
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }

        /// <summary>
        /// DeleteCommand — видалити вибраний контейнер.
        /// Можна виконати тільки коли SelectedItem != null.
        /// Делегуємо в _owner.DeleteSelectedAsync().
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }

        /// <summary>
        /// OpenProfileCommand — відкрити профіль вибраного контейнера.
        /// Можна виконати тільки коли SelectedItem != null.
        /// Делегуємо в _owner.OpenProfileAsync().
        /// </summary>
        public AsyncRelayCommand OpenProfileCommand { get; }

        /// <summary>
        /// Конструктор.
        ///
        /// Тут:
        /// 1) зберігаємо owner
        /// 2) створюємо команди
        ///
        /// Важливо:
        /// - canExecute для Edit/Delete/OpenProfile залежить від SelectedItem != null
        /// - коли SelectedItem змінюється, ми викликаємо RaiseCanExecuteChanged()
        ///   (див. UpdateSelectionCommands()).
        /// </summary>
        public ContainerListViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            SearchCommand = new AsyncRelayCommand(() => _owner.SearchAsync());
            AddNewCommand = new AsyncRelayCommand(() => _owner.StartAddAsync());

            EditCommand = new AsyncRelayCommand(
                () => _owner.EditSelectedAsync(),
                () => SelectedItem != null);

            DeleteCommand = new AsyncRelayCommand(
                () => _owner.DeleteSelectedAsync(),
                () => SelectedItem != null);

            OpenProfileCommand = new AsyncRelayCommand(
                () => _owner.OpenProfileAsync(),
                () => SelectedItem != null);
        }

        /// <summary>
        /// SetItems — встановити новий список контейнерів у Items.
        ///
        /// Типовий сценарій:
        /// - Owner виконав SearchAsync(), отримав список контейнерів з БД
        /// - Owner викликає listVm.SetItems(result)
        /// - UI оновлюється автоматично через ObservableCollection
        ///
        /// Поточна реалізація проста:
        /// - Items.Clear()
        /// - Items.Add(...) для кожного елемента
        ///
        /// Якщо колись будуть лаги на дуже великих списках,
        /// можна буде оптимізувати (оновлювати дифом або “не перезаливати” якщо не змінилось),
        /// але зараз це не обов'язково.
        /// </summary>
        public void SetItems(IEnumerable<ContainerModel> containers)
        {
            Items.Clear();
            foreach (var container in containers)
                Items.Add(container);
        }

        /// <summary>
        /// Оновити стан команд, які залежать від SelectedItem.
        ///
        /// Викликаємо в setter SelectedItem, щоб:
        /// - якщо SelectedItem став null — команди стали disabled
        /// - якщо SelectedItem став не null — команди стали enabled
        /// </summary>
        private void UpdateSelectionCommands()
        {
            EditCommand.RaiseCanExecuteChanged();
            DeleteCommand.RaiseCanExecuteChanged();
            OpenProfileCommand.RaiseCanExecuteChanged();
        }
    }
}
