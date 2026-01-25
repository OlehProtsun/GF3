using System;
using System.Collections.Generic;
using System.Text;

namespace WPFApp.ViewModel.Container.Edit.Helpers
{
    /// <summary>
    /// ContainerSection — перелік “екранів/секцій”, між якими перемикається ContainerViewModel.
    ///
    /// Навіщо:
    /// - UI може відображати різні ViewModel (ListVm/EditVm/ProfileVm/ScheduleEditVm/ScheduleProfileVm)
    /// - а ContainerViewModel тримає CurrentSection + Mode
    /// - Mode зберігається як enum, щоб логіка Cancel/Back була проста і читабельна.
    /// </summary>
    public enum ContainerSection
    {
        /// <summary>Список контейнерів.</summary>
        List,

        /// <summary>Форма додавання/редагування контейнера.</summary>
        Edit,

        /// <summary>Профіль контейнера (шапка + список schedule).</summary>
        Profile,

        /// <summary>Форма редагування schedule (матриця/генерація/стилі).</summary>
        ScheduleEdit,

        /// <summary>Профіль schedule (read-only/перегляд матриці).</summary>
        ScheduleProfile
    }
}
