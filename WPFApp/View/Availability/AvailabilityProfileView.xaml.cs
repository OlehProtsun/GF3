using System;
using System.Windows;
using System.Windows.Controls;
using WPFApp.View.Availability.Helpers;
using WPFApp.ViewModel.Availability.Profile;

namespace WPFApp.View.Availability
{
    /// <summary>
    /// AvailabilityProfileView.xaml.cs
    ///
    /// Принцип:
    /// - View підписується на vm.MatrixChanged, щоб перебудувати колонки DataGrid,
    ///   бо колонки залежать від DataTable.Columns (працівники можуть змінюватися).
    /// - Логіку побудови колонок винесено в AvailabilityMatrixGridBuilder.
    /// </summary>
    public partial class AvailabilityProfileView : UserControl
    {
        // Поточний VM, на який ми підписані (щоб коректно відписуватись).
        private AvailabilityProfileViewModel? _vm;

        public AvailabilityProfileView()
        {
            InitializeComponent();

            // DataContext може змінюватися під час життя view.
            DataContextChanged += OnDataContextChanged;

            // Unloaded — гарантована точка відписки від подій VM.
            Unloaded += OnUnloaded;

            // Loaded залишаємо як “страховку”, якщо DataContext підв’язали до Loaded.
            Loaded += OnLoaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityProfileViewModel);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachViewModel(DataContext as AvailabilityProfileViewModel);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            DetachViewModel();
        }

        private void AttachViewModel(AvailabilityProfileViewModel? viewModel)
        {
            // 1) Якщо підключають той самий інстанс — нічого не робимо.
            if (ReferenceEquals(_vm, viewModel))
                return;

            // 2) Від’єднуємо старий VM (якщо був).
            DetachViewModel();

            // 3) Запам’ятовуємо новий VM.
            _vm = viewModel;

            // 4) Якщо VM відсутній — виходимо (нема на що підписуватись).
            if (_vm is null)
                return;

            // 5) Підписуємось на MatrixChanged.
            _vm.MatrixChanged += VmOnMatrixChanged;

            // 6) Перший build колонок одразу.
            AvailabilityMatrixGridBuilder.BuildReadOnly(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
        }

        private void DetachViewModel()
        {
            // 1) Якщо VM нема — нічого робити.
            if (_vm is null)
                return;

            // 2) Відписка від події (щоб не було memory leaks та подвійних викликів).
            _vm.MatrixChanged -= VmOnMatrixChanged;

            // 3) Обнуляємо посилання.
            _vm = null;
        }

        private void VmOnMatrixChanged(object? sender, EventArgs e)
        {
            // 1) Якщо VM вже від’єднано — ігноруємо.
            if (_vm is null)
                return;

            // 2) Якщо подія прийшла не з UI thread — маршалимо в Dispatcher.
            //    Це робить код стійкішим, якщо колись MatrixChanged буде підніматися з background.
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => VmOnMatrixChanged(sender, e));
                return;
            }

            // 3) Перебудовуємо колонки.
            AvailabilityMatrixGridBuilder.BuildReadOnly(_vm.ProfileAvailabilityMonths.Table, dataGridAvailabilityMonthProfile);
        }
    }
}