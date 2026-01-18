using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFApp.Infrastructure;

namespace WPFApp.View.Dialogs
{
    public partial class ColorPickerDialog : Window
    {
        private bool _allowClose;

        public ObservableCollection<ColorSwatch> Swatches { get; } = new();

        public Color SelectedColor { get; private set; }

        public ColorPickerDialog(Color? initialColor = null)
        {
            InitializeComponent();
            DataContext = this;

            InitializeSwatches();

            if (initialColor.HasValue)
            {
                CustomHexTextBox.Text = ColorHelpers.ToHex(initialColor.Value);
            }
        }

        private void InitializeSwatches()
        {
            var colors = new[]
            {
                "#FFFFFF", "#F2F2F2", "#D9D9D9", "#BFBFBF", "#808080", "#000000",
                "#FFF2CC", "#FFE599", "#FFD966", "#F6B26B", "#E69138", "#C65911",
                "#FCE5CD", "#F9CB9C", "#F6B26B", "#E69138", "#B45F06", "#783F04",
                "#F4CCCC", "#EA9999", "#E06666", "#CC0000", "#990000", "#660000",
                "#D9EAD3", "#B6D7A8", "#93C47D", "#6AA84F", "#38761D", "#274E13",
                "#D0E0E3", "#A2C4C9", "#76A5AF", "#45818E", "#134F5C", "#0C343D",
                "#CFE2F3", "#9FC5E8", "#6FA8DC", "#3D85C6", "#0B5394", "#073763",
                "#D9D2E9", "#B4A7D6", "#8E7CC3", "#674EA7", "#351C75", "#20124D"
            };

            foreach (var hex in colors)
            {
                if (!ColorHelpers.TryParseHexColor(hex, out var color))
                    continue;

                Swatches.Add(new ColorSwatch(color));
            }
        }

        private void SwatchButton_Click(object sender, RoutedEventArgs e)
        {
            _allowClose = true;

            if (sender is not Button button || button.Tag is not Color color)
                return;

            SelectedColor = color;
            DialogResult = true;
        }

        private void CustomButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ColorHelpers.TryParseHexColor(CustomHexTextBox.Text, out var color))
            {
                MessageBox.Show("Enter a valid hex color (#RRGGBB or #AARRGGBB).", "Invalid Color",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _allowClose = true;
            SelectedColor = color;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _allowClose = true;
            DialogResult = false;
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Забороняємо Alt+F4 / системне закриття
            if (!_allowClose)
                e.Cancel = true;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Escape = Cancel
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                _allowClose = true;
                DialogResult = false;
                e.Handled = true;
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }



    }

    public sealed class ColorSwatch
    {
        public ColorSwatch(Color color)
        {
            Color = color;
            Brush = new SolidColorBrush(color);
            Brush.Freeze();
            Hex = ColorHelpers.ToHex(color);
        }

        public Color Color { get; }
        public SolidColorBrush Brush { get; }
        public string Hex { get; }
    }
}
