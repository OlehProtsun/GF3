using System.Threading.Tasks;
using WPFApp.ViewModel.Container.Edit.Helpers;

namespace WPFApp.ViewModel.Container.Edit
{
    /// <summary>
    /// ContainerViewModel.Navigation — частина (partial) ContainerViewModel,
    /// яка відповідає ТІЛЬКИ за:
    /// - поточний активний екран (CurrentSection)
    /// - поточний режим (Mode)
    /// - правила “куди повернутись при Cancel/Back”
    /// - перемикання між екранами (SwitchTo*)
    /// - “прибирання” важкого стану при виході з ScheduleEdit/ScheduleProfile
    ///
    /// Важливо:
    /// - Логіка тут НЕ виконує CRUD або генерацію.
    /// - Вона лише керує тим, який VM зараз показаний у UI.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        // backing field для властивості CurrentSection
        private object _currentSection = null!;

        // backing field для властивості Mode
        private ContainerSection _mode = ContainerSection.List;

        /// <summary>
        /// Поточний об’єкт секції, який UI показує.
        ///
        /// Тип object використаний тому, що це може бути:
        /// - ListVm
        /// - EditVm
        /// - ProfileVm
        /// - ScheduleEditVm
        /// - ScheduleProfileVm
        ///
        /// UI зазвичай робить ContentControl.Content = CurrentSection
        /// і підбирає DataTemplate по типу.
        /// </summary>
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        /// <summary>
        /// Поточний режим (enum), щоб:
        /// - логіка Cancel/Back була простішою
        /// - можна було легко перевіряти “де ми зараз”
        /// </summary>
        public ContainerSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        /// <summary>
        /// Куди повернутися при Cancel з ContainerEdit (форма контейнера).
        ///
        /// Наприклад:
        /// - якщо відкрили Edit із Profile → CancelTarget = Profile
        /// - якщо відкрили Edit із List → CancelTarget = List
        /// </summary>
        public ContainerSection CancelTarget { get; private set; } = ContainerSection.List;

        /// <summary>
        /// Куди повернутися при Cancel з ScheduleEdit (форма schedule).
        ///
        /// Наприклад:
        /// - якщо відкрили ScheduleEdit із ScheduleProfile → ScheduleCancelTarget = ScheduleProfile
        /// - якщо відкрили ScheduleEdit із Profile → ScheduleCancelTarget = Profile
        /// </summary>
        public ContainerSection ScheduleCancelTarget { get; private set; } = ContainerSection.Profile;

        /// <summary>
        /// Отримати “поточний активний ContainerId”.
        ///
        /// Логіка:
        /// - якщо ми в Profile/ScheduleEdit/ScheduleProfile → containerId беремо з ProfileVm
        /// - інакше (List/Edit) → containerId беремо з ListVm.SelectedItem
        ///
        /// Навіщо:
        /// - багато дій (Schedule search/add/edit) вимагають containerId
        /// - цей helper робить код коротшим і однаковим у всіх методах.
        /// </summary>
        private int GetCurrentContainerId()
        {
            if (Mode == ContainerSection.Profile
                || Mode == ContainerSection.ScheduleEdit
                || Mode == ContainerSection.ScheduleProfile)
                return ProfileVm.ContainerId;

            return ListVm.SelectedItem?.Id ?? 0;
        }

        /// <summary>
        /// Cancel із форми контейнера (Edit).
        ///
        /// Поведінка:
        /// - якщо ми на екрані Edit:
        ///     - повертаємось в Profile або List (залежить від CancelTarget)
        /// - в усіх інших режимах:
        ///     - повертаємось в List
        ///
        /// Важливо:
        /// - Тут ми також чистимо помилки валідації форми EditVm,
        ///   щоб вони не “прилипали” на наступний відкритий контейнер.
        /// </summary>
        internal Task CancelAsync()
        {
            EditVm.ClearValidationErrors();

            return Mode switch
            {
                ContainerSection.Edit => CancelTarget == ContainerSection.Profile
                    ? SwitchToProfileAsync()
                    : SwitchToListAsync(),
                _ => SwitchToListAsync()
            };
        }

        /// <summary>
        /// Cancel із екранів schedule (ScheduleEdit або ScheduleProfile).
        ///
        /// Логіка:
        /// - якщо ми в ScheduleEdit:
        ///     - повертаємось у ScheduleProfile або Profile (залежить від ScheduleCancelTarget)
        /// - якщо ми в ScheduleProfile:
        ///     - повертаємось у Profile
        /// - в інших випадках:
        ///     - теж у Profile (бо schedule живуть у профілі контейнера)
        /// </summary>
        internal Task CancelScheduleAsync()
        {
            ScheduleEditVm.ClearValidationErrors();

            return Mode switch
            {
                ContainerSection.ScheduleEdit => ScheduleCancelTarget == ContainerSection.ScheduleProfile
                    ? SwitchToScheduleProfileAsync()
                    : SwitchToProfileAsync(),

                ContainerSection.ScheduleProfile => SwitchToProfileAsync(),

                _ => SwitchToProfileAsync()
            };
        }

        /// <summary>
        /// Перемкнутися на список контейнерів.
        /// Якщо ми виходимо зі schedule-екранів — очищаємо важкий schedule state.
        /// </summary>
        private Task SwitchToListAsync()
        {
            if (Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                CleanupScheduleEdit();

            CurrentSection = ListVm;
            Mode = ContainerSection.List;
            return Task.CompletedTask;
        }

        /// <summary>Перемкнутися на форму редагування контейнера.</summary>
        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = ContainerSection.Edit;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Перемкнутися на профіль контейнера.
        /// Якщо ми виходимо зі schedule-екранів — очищаємо schedule state.
        /// </summary>
        private Task SwitchToProfileAsync()
        {
            if (Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                CleanupScheduleEdit();

            CurrentSection = ProfileVm;
            Mode = ContainerSection.Profile;
            return Task.CompletedTask;
        }

        /// <summary>Перемкнутися на форму редагування schedule.</summary>
        private Task SwitchToScheduleEditAsync()
        {
            CurrentSection = ScheduleEditVm;
            Mode = ContainerSection.ScheduleEdit;
            return Task.CompletedTask;
        }

        /// <summary>Перемкнутися на профіль schedule.</summary>
        private Task SwitchToScheduleProfileAsync()
        {
            CurrentSection = ScheduleProfileVm;
            Mode = ContainerSection.ScheduleProfile;
            return Task.CompletedTask;
        }

        /// <summary>
        /// CleanupScheduleEdit — “прибрати” важкий стан schedule-редактора,
        /// коли користувач залишає ScheduleEdit/ScheduleProfile.
        ///
        /// Чому це важливо:
        /// - schedule-матриці/preview використовують CancellationTokenSource і background задачі
        /// - DataView/DataTable можуть бути великими
        /// - якщо не чистити — можливі витоки пам’яті і “фонові” tasks після виходу
        ///
        /// Що робимо:
        /// 1) зупиняємо preview pipeline, який живе у ContainerViewModel
        /// 2) зупиняємо CTS у ScheduleEditVm (матриця/preview)
        /// 3) (опційно) робимо ResetForNew, щоб звільнити DataView/колекції
        /// </summary>
        private void CleanupScheduleEdit()
        {
            // 1) preview pipeline в ContainerViewModel
            CancelScheduleEditWork();

            // 2) matrix/preview CTS всередині ScheduleEditVm
            ScheduleEditVm.CancelBackgroundWork();

            // 3) скидання важкого стану (таблиці/колекції)
            ScheduleEditVm.ResetForNew();
        }
    }
}
