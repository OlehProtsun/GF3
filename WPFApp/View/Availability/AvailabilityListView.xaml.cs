using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFApp.ViewModel.Availability;

namespace WPFApp.View.Availability
{
    /// <summary>
    /// Interaction logic for AvailabilityListView.xaml
    /// </summary>
    public partial class AvailabilityListView : UserControl
    {
        public AvailabilityListView()
        {
            InitializeComponent();
            inputSearch.KeyDown += InputSearch_KeyDown;
            dataGridAvailabilityList.MouseDoubleClick += DataGridAvailabilityList_MouseDoubleClick;
        }

        private void InputSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (DataContext is AvailabilityListViewModel vm && vm.SearchCommand.CanExecute(null))
                vm.SearchCommand.Execute(null);
        }

        private void DataGridAvailabilityList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is AvailabilityListViewModel vm && vm.OpenProfileCommand.CanExecute(null))
                vm.OpenProfileCommand.Execute(null);
        }
    }
}
