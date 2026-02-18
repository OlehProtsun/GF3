using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPFApp.View.Dialogs
{
    public enum UIStatusKind
    {
        Success = 0,
        Working = 1
    }

    public partial class UIStatus : UserControl
    {
        private static readonly SolidColorBrush Neutral = BrushFrom("#14161A");
        private static readonly SolidColorBrush BorderNeutral = BrushFrom("#E6E8EC");

        private static readonly SolidColorBrush SuccessGreen = BrushFrom("#1E7A3B");
        private static readonly SolidColorBrush BorderGreen = BrushFrom("#33C36B");

        public UIStatus()
        {
            InitializeComponent();
            ApplyStatusVisuals();
        }

        public UIStatusKind Status
        {
            get => (UIStatusKind)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(UIStatusKind),
                typeof(UIStatus),
                new PropertyMetadata(UIStatusKind.Success, OnAnyChanged));

        // NEW: custom message
        public string? Message
        {
            get => (string?)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(UIStatus),
                new PropertyMetadata(null, OnAnyChanged));

        private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((UIStatus)d).ApplyStatusVisuals();

        private void ApplyStatusVisuals()
        {
            if (RootBorder == null || StatusText == null || SuccessIcon == null || WorkingIcon == null)
                return;

            if (SuccessPath == null || SpinnerRing == null || SpinnerArc == null)
                return;

            var defaultText = Status == UIStatusKind.Working ? "Working..." : "Successfully";
            var text = string.IsNullOrWhiteSpace(Message) ? defaultText : Message!;

            if (Status == UIStatusKind.Working)
            {
                RootBorder.BorderBrush = BorderNeutral;

                StatusText.Text = text;
                StatusText.Foreground = Neutral;

                SuccessIcon.Visibility = Visibility.Collapsed;
                WorkingIcon.Visibility = Visibility.Visible;

                SpinnerRing.Stroke = Neutral;
                SpinnerArc.Stroke = Neutral;
            }
            else
            {
                RootBorder.BorderBrush = BorderGreen;

                StatusText.Text = text;
                StatusText.Foreground = SuccessGreen;

                SuccessIcon.Visibility = Visibility.Visible;
                WorkingIcon.Visibility = Visibility.Collapsed;

                SuccessPath.Stroke = SuccessGreen;
            }
        }

        private static SolidColorBrush BrushFrom(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }
    }
}
