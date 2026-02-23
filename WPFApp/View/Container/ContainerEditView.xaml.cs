using System.Windows.Controls;


namespace WPFApp.View.Container
{
    /// <summary>
    /// Create/Edit form for a container.
    /// No business logic is placed here: validation/save/cancel live in the ViewModel.
    /// </summary>
    public partial class ContainerEditView : UserControl
    {
        /// <summary>
        /// Initializes visual tree and binds XAML elements to the current DataContext.
        /// </summary>
        public ContainerEditView()
        {
            InitializeComponent();
        }
    }
}
