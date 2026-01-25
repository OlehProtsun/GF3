using DataAccessLayer.Models;                 // ContainerModel — модель контейнера з DAL (БД/сервіс)
using WPFApp.Infrastructure;                  // ViewModelBase, AsyncRelayCommand (база MVVM + команди)
using WPFApp.ViewModel.Container.Edit;
using WPFApp.ViewModel.Container.List;
using WPFApp.ViewModel.Container.ScheduleList;        // ContainerScheduleListViewModel — список розкладів у профілі

namespace WPFApp.ViewModel.Container.Profile
{
    /// <summary>
    /// ContainerProfileViewModel — ViewModel екрану “профіль контейнера”.
    ///
    /// Роль цього VM:
    /// 1) Показати базові дані контейнера (Id, Name, Note).
    /// 2) Показати під-екран зі списком розкладів цього контейнера (ScheduleListVm).
    /// 3) Дати команди навігації/дій (Back/Cancel/Edit/Delete), які делегуються owner'у.
    ///
    /// Це “тонкий” VM (thin ViewModel):
    /// - він НЕ ходить в БД
    /// - НЕ виконує важких розрахунків
    /// - НЕ містить складної бізнес-логіки
    /// Він тільки тримає стан відображення і делегує команди.
    /// </summary>
    public sealed class ContainerProfileViewModel : ViewModelBase
    {
        /// <summary>
        /// Owner (ContainerViewModel) — керівник навігації/операцій.
        ///
        /// Саме owner знає:
        /// - як повернутись назад/закрити екран (CancelAsync)
        /// - як відкрити редагування (EditSelectedAsync)
        /// - як видалити (DeleteSelectedAsync)
        ///
        /// Профільний VM лише викликає ці методи через команди.
        /// </summary>
        private readonly ContainerViewModel _owner;

        // ----------------------------
        // 1) Властивості профілю (UI)
        // ----------------------------

        /// <summary>
        /// ContainerId — ідентифікатор контейнера.
        /// Використовується тільки для відображення (read-only у UI зазвичай),
        /// але технічно тут set доступний (через SetProperty).
        /// </summary>
        private int _containerId;
        public int ContainerId
        {
            get => _containerId;
            private set => SetProperty(ref _containerId, value);
        }

        /// <summary>
        /// Name — назва контейнера.
        /// </summary>
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Note — примітка контейнера (може бути null).
        /// </summary>
        private string? _note;
        public string? Note
        {
            get => _note;
            private set => SetProperty(ref _note, value);
        }

        // --------------------------------------------
        // 2) Вкладений VM зі списком розкладів контейнера
        // --------------------------------------------

        /// <summary>
        /// ScheduleListVm — під-екран списку розкладів, який показується всередині профілю.
        ///
        /// Навіщо вкладений VM:
        /// - профіль контейнера показує “шапку” (Id/Name/Note)
        /// - і нижче список розкладів (окремий компонент)
        /// - розумно тримати це як композицію: ProfileVM включає ScheduleListVM.
        ///
        /// Важливо:
        /// - цей ListVM працює з тим самим owner’ом, щоб команди/навігація були єдині.
        /// </summary>
        public ContainerScheduleListViewModel ScheduleListVm { get; }

        // ----------------------------
        // 3) Команди UI
        // ----------------------------

        /// <summary>
        /// BackCommand — повернутись назад з профілю.
        /// </summary>
        public AsyncRelayCommand BackCommand { get; }

        /// <summary>
        /// CancelProfileCommand — історично друга кнопка “закрити/відмінити”.
        ///
        /// Щоб не дублювати логіку, ми робимо її alias на BackCommand:
        /// - обидві команди виконують одне і те саме (CancelAsync).
        /// </summary>
        public AsyncRelayCommand CancelProfileCommand { get; }

        /// <summary>
        /// EditCommand — відкрити редагування вибраного контейнера (owner вирішує що є “selected”).
        /// </summary>
        public AsyncRelayCommand EditCommand { get; }

        /// <summary>
        /// DeleteCommand — видалити вибраний контейнер (owner керує підтвердженням/видаленням).
        /// </summary>
        public AsyncRelayCommand DeleteCommand { get; }

        /// <summary>
        /// Конструктор.
        ///
        /// Тут:
        /// 1) запам’ятовуємо owner
        /// 2) створюємо вкладений ScheduleListVm
        /// 3) ініціалізуємо команди
        /// </summary>
        public ContainerProfileViewModel(ContainerViewModel owner)
        {
            _owner = owner;

            // Вкладений VM списку розкладів.
            // Він використовує owner для пошуку/відкриття/мультивідкриття розкладів.
            ScheduleListVm = new ContainerScheduleListViewModel(owner);

            // Обидві команди (Back і CancelProfile) роблять одне й те саме — закрити профіль.
            BackCommand = new AsyncRelayCommand(() => _owner.CancelAsync());
            CancelProfileCommand = BackCommand; // ✅ без дублювання

            // Редагування і видалення делегуємо owner'у.
            EditCommand = new AsyncRelayCommand(() => _owner.EditSelectedAsync());
            DeleteCommand = new AsyncRelayCommand(() => _owner.DeleteSelectedAsync());
        }

        // ----------------------------
        // 4) Заповнення профілю даними
        // ----------------------------

        /// <summary>
        /// SetProfile — заповнює ViewModel даними з ContainerModel.
        ///
        /// Коли викликається:
        /// - після того як owner завантажив контейнер з БД/сервісу
        /// - перед показом профілю у UI
        ///
        /// Принцип:
        /// - ProfileVM не завантажує дані сам
        /// - він лише “приймає” готову модель і розкладає її по властивостях
        /// </summary>
        public void SetProfile(ContainerModel model)
        {
            // Якщо в твоєму коді model гарантовано не null — перевірка не потрібна.
            // Але як захист від випадкового виклику з null — можна додати:
            // if (model == null) throw new ArgumentNullException(nameof(model));

            ContainerId = model.Id;
            Name = model.Name;
            Note = model.Note;
        }
    }
}
