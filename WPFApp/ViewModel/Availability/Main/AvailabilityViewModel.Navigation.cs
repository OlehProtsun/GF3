/*
  Опис файлу: цей модуль містить реалізацію компонента AvailabilityViewModel.Navigation у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Threading.Tasks;

namespace WPFApp.ViewModel.Availability.Main
{
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class AvailabilityViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class AvailabilityViewModel
    {
        
        private object _currentSection = null!;

        
        private AvailabilitySection _mode = AvailabilitySection.List;

        
        private int? _openedProfileGroupId;

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public object CurrentSection` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AvailabilitySection Mode` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilitySection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        
        
        
        /// <summary>
        /// Визначає публічний елемент `public AvailabilitySection CancelTarget { get; private set; } = AvailabilitySection.List;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public AvailabilitySection CancelTarget { get; private set; } = AvailabilitySection.List;

        
        
        
        private Task SwitchToListAsync()
        {
            CurrentSection = ListVm;
            Mode = AvailabilitySection.List;
            return Task.CompletedTask;
        }

        
        
        
        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = AvailabilitySection.Edit;
            return Task.CompletedTask;
        }

        
        
        
        private Task SwitchToProfileAsync()
        {
            CurrentSection = ProfileVm;
            Mode = AvailabilitySection.Profile;
            return Task.CompletedTask;
        }
    }
}
