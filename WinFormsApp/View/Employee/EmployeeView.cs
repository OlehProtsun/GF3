using WinFormsApp.View.Shared;

namespace WinFormsApp.View.Employee
{
    public partial class EmployeeView : BusyForm, IEmployeeView
    {
        public EmployeeView()
        {
            InitializeComponent();
            ConfigureGrid();
            AssociateAndRaiseViewEvents();
        }
    }
}
