using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using WPFApp.Infrastructure;

namespace WPFApp.ViewModel.Availability
{
    public sealed class AvailabilityViewModel : ViewModelBase
    {
        private object _currentSection;
        public object CurrentSection
        {
            get => _currentSection;
            private set => SetProperty(ref _currentSection, value);
        }

        public ICommand ShowListCommand { get; }
        public ICommand ShowEditCommand { get; }
        public ICommand ShowProfileCommand { get; }

        // спільний стан модуля (те, що в WinForms часто лежало "в формі")
        //public AvailabilityListViewModel ListVm { get; }
        //public AvailabilityEditViewModel EditVm { get; }
        //public AvailabilityProfileViewModel ProfileVm { get; }

        //public AvailabilityViewModel(/* залежності: services, repo, etc */)
        //{
        //    ListVm = new AvailabilityListViewModel(/*...*/);
        //    EditVm = new AvailabilityEditViewModel(/*...*/);
        //    ProfileVm = new AvailabilityProfileViewModel(/*...*/);

        //    ShowListCommand = new RelayCommand(() => CurrentSection = ListVm);
        //    ShowEditCommand = new RelayCommand(() => CurrentSection = EditVm);
        //    ShowProfileCommand = new RelayCommand(() => CurrentSection = ProfileVm);

        //    CurrentSection = ListVm; // старт як SelectedIndex = 0
        //}
    }
}
