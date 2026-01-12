using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPFApp.Controls
{
    public partial class NumericUpDown : UserControl
    {
        private static readonly Regex DigitsOnly = new(@"^\d+$");

        public NumericUpDown()
        {
            InitializeComponent();
            Loaded += (_, __) => ApplyValueToTextBox();
        }

        // ----- Dependency Properties -----

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int),
                typeof(NumericUpDown),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Coerce(value));
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(0, OnMinMaxChanged));

        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(100, OnMinMaxChanged));

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(1));

        public int Step
        {
            get => (int)GetValue(StepProperty);
            set => SetValue(StepProperty, value <= 0 ? 1 : value);
        }

        // ----- Handlers -----

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericUpDown)d;
            c.ApplyValueToTextBox();
        }

        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (NumericUpDown)d;
            c.Value = c.Coerce(c.Value);
            c.ApplyValueToTextBox();
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            Value = Coerce(Value + Step);
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            Value = Coerce(Value - Step);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !DigitsOnly.IsMatch(e.Text);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text, true))
            {
                e.CancelCommand();
                return;
            }

            var text = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";
            if (text.Length > 0 && !DigitsOnly.IsMatch(text))
                e.CancelCommand();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PART_TextBox.Text, out var v))
            {
                // якщо порожньо/не число — ставимо мінімум
                Value = Coerce(Minimum);
                return;
            }

            Value = Coerce(v);
        }

        // ----- Helpers -----

        private int Coerce(int v)
        {
            if (Maximum < Minimum)
                return Minimum;

            if (v < Minimum) return Minimum;
            if (v > Maximum) return Maximum;
            return v;
        }

        private void ApplyValueToTextBox()
        {
            if (PART_TextBox == null) return;

            var txt = Value.ToString();
            if (PART_TextBox.Text != txt)
                PART_TextBox.Text = txt;
        }
    }
}
