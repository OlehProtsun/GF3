/*
  Опис файлу: цей модуль містить реалізацію компонента InformationView у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System.Windows.Controls;

namespace WPFApp.View.Information
{
    /// <summary>
    /// Визначає публічний елемент `public partial class InformationView : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class InformationView : UserControl
    {
        /// <summary>
        /// Визначає публічний елемент `public InformationView()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public InformationView()
        {
            InitializeComponent();
        }
    }
}
