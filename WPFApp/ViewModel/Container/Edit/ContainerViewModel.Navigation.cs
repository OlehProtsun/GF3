/*
  Опис файлу: цей модуль містить реалізацію компонента ContainerViewModel.Navigation у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Linq;
using System.Threading.Tasks;

using BusinessLogicLayer.Contracts.Models;

using WPFApp.ViewModel.Container.Edit.Helpers;

namespace WPFApp.ViewModel.Container.Edit
{
    
    
    
    
    
    
    
    
    
    
    
    
    
    /// <summary>
    /// Визначає публічний елемент `public sealed partial class ContainerViewModel` та контракт його використання у шарі WPFApp.
    /// </summary>
    public sealed partial class ContainerViewModel
    {
        
        private object _currentSection = null!;

        
        private ContainerSection _mode = ContainerSection.List;

        
        
        
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public object CurrentSection` та контракт його використання у шарі WPFApp.
        /// </summary>
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerSection Mode` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerSection Mode
        {
            get => _mode;
            private set => SetProperty(ref _mode, value);
        }

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerSection CancelTarget { get; private set; } = ContainerSection.List;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerSection CancelTarget { get; private set; } = ContainerSection.List;

        
        
        
        
        
        
        
        /// <summary>
        /// Визначає публічний елемент `public ContainerSection ScheduleCancelTarget { get; private set; } = ContainerSection.Profile;` та контракт його використання у шарі WPFApp.
        /// </summary>
        public ContainerSection ScheduleCancelTarget { get; private set; } = ContainerSection.Profile;

        
        
        
        
        
        
        
        
        
        
        
        private int GetCurrentContainerId()
        {
            if (Mode == ContainerSection.Profile
                || Mode == ContainerSection.ScheduleEdit
                || Mode == ContainerSection.ScheduleProfile)
                return ProfileVm.ContainerId;

            return ListVm.SelectedItem?.Id ?? 0;
        }

        
        
        
        
        
        
        
        
        
        
        
        
        
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

        
        
        
        
        private Task SwitchToListAsync()
        {
            if (Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                CleanupScheduleEdit();

            CurrentSection = ListVm;
            Mode = ContainerSection.List;
            return Task.CompletedTask;
        }

        
        private Task SwitchToEditAsync()
        {
            CurrentSection = EditVm;
            Mode = ContainerSection.Edit;
            return Task.CompletedTask;
        }

        
        
        
        
        private Task SwitchToProfileAsync()
        {
            if (Mode == ContainerSection.ScheduleEdit || Mode == ContainerSection.ScheduleProfile)
                CleanupScheduleEdit();

            CurrentSection = ProfileVm;
            Mode = ContainerSection.Profile;
            return Task.CompletedTask;
        }

        
        private Task SwitchToScheduleEditAsync()
        {
            CurrentSection = ScheduleEditVm;
            Mode = ContainerSection.ScheduleEdit;
            return Task.CompletedTask;
        }

        
        private Task SwitchToScheduleProfileAsync()
        {
            CurrentSection = ScheduleProfileVm;
            Mode = ContainerSection.ScheduleProfile;
            return Task.CompletedTask;
        }


        
        
        
        private Task SyncProfileAndSelectionAsync(ContainerModel container)
            => RunOnUiThreadAsync(() =>
            {
                ProfileVm.SetProfile(container);
                ListVm.SelectedItem = ListVm.Items.FirstOrDefault(item => item.Id == container.Id) ?? container;
            });

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        private void CleanupScheduleEdit()
        {
            
            CancelScheduleEditWork();

            
            ScheduleEditVm.CancelBackgroundWork();

            
            ScheduleEditVm.ResetForNew();
        }
    }
}
