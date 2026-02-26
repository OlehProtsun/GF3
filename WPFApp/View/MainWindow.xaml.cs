/*
  Опис файлу: цей модуль містить реалізацію компонента MainWindow у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFApp.ViewModel.Main;

namespace WPFApp.View
{
    /// <summary>
    /// Визначає публічний елемент `public partial class MainWindow : Window` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        /// <summary>
        /// Визначає публічний елемент `public MainWindow(MainViewModel vm)` та контракт його використання у шарі WPFApp.
        /// </summary>
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = vm;

            Loaded += (_, __) =>
            {
                
                if (_vm.CurrentViewModel == null)
                    _vm.ShowHomeCommand.Execute(null);
            };

            Closed += (_, __) => _vm.Dispose();
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}