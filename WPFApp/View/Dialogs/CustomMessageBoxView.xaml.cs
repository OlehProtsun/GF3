using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFApp.ViewModel.Dialogs;

namespace WPFApp.View.Dialogs
{
    /// <summary>
    /// Interaction logic for CustomMessageBoxView.xaml
    /// </summary>
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
    }
}
