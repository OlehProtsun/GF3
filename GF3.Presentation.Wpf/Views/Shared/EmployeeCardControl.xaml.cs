using System.Windows;
using System.Windows.Controls;

namespace GF3.Presentation.Wpf.Views.Shared
{
    public partial class EmployeeCardControl : UserControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(EmployeeCardControl),
            new PropertyMetadata(string.Empty));

        public EmployeeCardControl()
        {
            InitializeComponent();
        }

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
    }
}
