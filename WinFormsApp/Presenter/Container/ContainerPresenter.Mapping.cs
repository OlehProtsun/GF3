using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp.Presenter.Container
{
    public sealed partial class ContainerPresenter
    {
        private ScheduleModel BuildScheduleFromView()
        {
            return new ScheduleModel
            {
                Id = _view.ScheduleId,
                ContainerId = _view.ScheduleContainerId,
                Name = _view.ScheduleName,
                Year = _view.ScheduleYear,
                Month = _view.ScheduleMonth,
                PeoplePerShift = _view.SchedulePeoplePerShift,
                Shift1Time = _view.ScheduleShift1,
                Shift2Time = _view.ScheduleShift2,
                MaxHoursPerEmpMonth = _view.ScheduleMaxHoursPerEmp,
                MaxConsecutiveDays = _view.ScheduleMaxConsecutiveDays,
                MaxConsecutiveFull = _view.ScheduleMaxConsecutiveFull,
                MaxFullPerMonth = _view.ScheduleMaxFullPerMonth,
            };
        }
    }
}
