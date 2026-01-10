using System.Windows;
using System.Windows.Controls;

namespace GF3.Presentation.Wpf.Views.Shared
{
    public partial class InfoCardControl : UserControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(InfoCardControl),
            new PropertyMetadata(string.Empty));

        public InfoCardControl()
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
