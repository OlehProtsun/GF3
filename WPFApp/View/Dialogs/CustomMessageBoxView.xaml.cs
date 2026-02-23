using System;
using System.Windows;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.View.Dialogs
{
    public partial class CustomMessageBoxView : Window
    {
        public CustomMessageBoxView()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                if (DataContext is CustomMessageBoxViewModel vm)
                {
                    vm.RequestClose += result =>
                    {
                        DialogResult = result;
                        Close();
                    };
                }
            };
        }

        private void CopyDetails_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not CustomMessageBoxViewModel vm)
                return;

            var text = vm.Details ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch
            {
                Clipboard.SetDataObject(text, true);
            }
        }
    }
}
