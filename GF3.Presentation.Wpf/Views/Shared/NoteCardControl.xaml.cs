using System.Windows;
using System.Windows.Controls;

namespace GF3.Presentation.Wpf.Views.Shared
{
    public partial class NoteCardControl : UserControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(NoteCardControl),
            new PropertyMetadata(string.Empty));

        public NoteCardControl()
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
