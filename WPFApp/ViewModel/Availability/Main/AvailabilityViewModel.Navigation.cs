using System.Threading.Tasks;

namespace WPFApp.ViewModel.Availability.Main
{
    /// <summary>
    /// Navigation — відповідає лише за:
    /// - CurrentSection (що показує ContentControl)
    /// - Mode (enum для простих switch-ів)
    /// - CancelTarget (куди повернутися з Edit)
    /// - SwitchTo* методи
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        // backing field для CurrentSection (тип object, бо DataTemplate підбирається по типу VM).
        private object _currentSection = null!;

        // backing field для Mode.
        private AvailabilitySection _mode = AvailabilitySection.List;

        // Id групи, відкритої в Profile (щоб після Save можна було перезавантажити profile).
        private int? _openedProfileGroupId;

        /// <summary>
        /// Поточний VM секції, який показує UI.
        /// </summary>
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        /// <summary>
        /// Поточний режим (List/Edit/Profile).
        /// </summary>
        public AvailabilitySection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        /// <summary>
        /// Куди повернутися при Cancel з Edit.
        /// </summary>
        public AvailabilitySection CancelTarget { get; private set; } = AvailabilitySection.List;

        /// <summary>
        /// Перемкнутися на список.
        /// </summary>
        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = AvailabilitySection.List;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Перемкнутися на форму редагування.
        /// </summary>
        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = AvailabilitySection.Edit;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Перемкнутися на профіль.
        /// </summary>
        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = AvailabilitySection.Profile;
            return Task.CompletedTask;
        }
    }
}
