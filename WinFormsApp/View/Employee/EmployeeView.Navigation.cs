using DataAccessLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using WinFormsApp.ViewModel;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView
    {
        public void SwitchToEditMode()
        {
            tabControl.SelectedTab = tabEditAdnCreate;
            Mode = EmployeeViewModel.Edit;
        }

        public void SwitchToListMode()
        {
            tabControl.SelectedTab = tabList;
            Mode = EmployeeViewModel.List;
        }

        public void SwitchToProfileMode()
        {
            tabControl.SelectedTab = tabProfile;
            Mode = EmployeeViewModel.Profile;
        }

        public void SetProfile(EmployeeModel m)
        {
            labelName.Text = $"{m.FirstName} {m.LastName}";
            labelEmail.Text = string.IsNullOrWhiteSpace(m.Email) ? "—" : m.Email;
            labelPhone.Text = string.IsNullOrWhiteSpace(m.Phone) ? "—" : m.Phone;
            labelId.Text = m.Id.ToString();
        }

        public void SetEmployeeListBindingSource(BindingSource employeeList) =>
            dataGrid.DataSource = employeeList;
    }
}
