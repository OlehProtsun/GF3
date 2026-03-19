/*
  Опис файлу: цей модуль містить реалізацію компонента NumericUpDown у шарі WPFApp.
  Призначення: інкапсулювати поведінку UI або прикладної логіки без зміни доменної моделі.
  Примітка: коментарі описують спостережуваний потік даних, очікувані обмеження та точки взаємодії.
*/
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;


namespace WPFApp.Controls
{
    /// <summary>
    /// Визначає публічний елемент `public partial class NumericUpDown : UserControl` та контракт його використання у шарі WPFApp.
    /// </summary>
    public partial class NumericUpDown : UserControl
    {
        

        /// <summary>
        /// Визначає публічний елемент `public static readonly DependencyProperty ValueProperty =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(int),
                typeof(NumericUpDown),
                new FrameworkPropertyMetadata(
                    0,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged,
                    CoerceValue));

        /// <summary>
        /// Визначає публічний елемент `public int Value` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        

        /// <summary>
        /// Визначає публічний елемент `public static readonly DependencyProperty MinimumProperty =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(
                nameof(Minimum),
                typeof(int),
                typeof(NumericUpDown),
                new PropertyMetadata(int.MinValue, OnMinMaxChanged));

        /// <summary>
        /// Визначає публічний елемент `public int Minimum` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        

        /// <summary>
        /// Визначає публічний елемент `public static readonly DependencyProperty MaximumProperty =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(
                nameof(Maximum),
                typeof(int),
                typeof(NumericUpDown),
                new PropertyMetadata(int.MaxValue, OnMinMaxChanged));

        /// <summary>
        /// Визначає публічний елемент `public int Maximum` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        

        /// <summary>
        /// Визначає публічний елемент `public static readonly DependencyProperty StepProperty =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(
                nameof(Step),
                typeof(int),
                typeof(NumericUpDown),
                new PropertyMetadata(1, OnStepChanged, CoerceStep));

        /// <summary>
        /// Визначає публічний елемент `public int Step` та контракт його використання у шарі WPFApp.
        /// </summary>
        public int Step
        {
            get => (int)GetValue(StepProperty);
            set => SetValue(StepProperty, value);
        }

        

        /// <summary>
        /// Визначає публічний елемент `public static readonly DependencyProperty IsReadOnlyProperty =` та контракт його використання у шарі WPFApp.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(
                nameof(IsReadOnly),
                typeof(bool),
                typeof(NumericUpDown),
                new PropertyMetadata(false));

        /// <summary>
        /// Визначає публічний елемент `public bool IsReadOnly` та контракт його використання у шарі WPFApp.
        /// </summary>
        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Визначає публічний елемент `public NumericUpDown()` та контракт його використання у шарі WPFApp.
        /// </summary>
        public NumericUpDown()
        {
            InitializeComponent();
            Loaded += (_, __) => UpdateButtonState();
        }

        

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly) return;
            CommitText(); 
            SetCurrentValue(ValueProperty, CoerceToRange(Value + Step));
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly) return;
            CommitText();
            SetCurrentValue(ValueProperty, CoerceToRange(Value - Step));
        }

        

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (IsReadOnly) { e.Handled = true; return; }

            if (sender is not TextBox tb)
            {
                e.Handled = true;
                return;
            }

            string proposed = GetProposedText(tb, e.Text);
            e.Handled = !IsTextAllowed(proposed);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (IsReadOnly) { e.CancelCommand(); return; }

            if (sender is not TextBox tb)
            {
                e.CancelCommand();
                return;
            }

            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            string paste = (string)e.DataObject.GetData(typeof(string))!;
            string proposed = GetProposedText(tb, paste);
            if (!IsTextAllowed(proposed))
                e.CancelCommand();
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsReadOnly) return;

            
            if (e.Key == Key.Up)
            {
                Up_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                Down_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsReadOnly) return;

            
            if (e.Key == Key.Enter)
            {
                CommitText();
                e.Handled = true;
            }
        }

        private void SyncTextWithValue()
        {
            if (PART_TextBox == null) return;

            
            var be = PART_TextBox.GetBindingExpression(TextBox.TextProperty);
            if (be != null)
            {
                be.UpdateTarget();
            }
            else
            {
                
                PART_TextBox.SetCurrentValue(
                    TextBox.TextProperty,
                    Value.ToString(CultureInfo.InvariantCulture));
            }

            PART_TextBox.CaretIndex = PART_TextBox.Text?.Length ?? 0;
        }


        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly) return;
            CommitText();
        }

        private void CommitText()
        {
            if (PART_TextBox == null) return;

            string text = (PART_TextBox.Text ?? string.Empty).Trim();

            
            if (string.IsNullOrEmpty(text) || text == "-" || text == "+")
            {
                SyncTextWithValue();
                return;
            }

            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                SyncTextWithValue();
                return;
            }

            int coerced = CoerceToRange(parsed);
            SetCurrentValue(ValueProperty, coerced);

            
            SyncTextWithValue();
        }

        private string GetProposedText(TextBox tb, string newText)
        {
            string text = tb.Text ?? string.Empty;

            int start = tb.SelectionStart;
            int length = tb.SelectionLength;

            if (length > 0)
                text = text.Remove(start, length);

            if (start < 0) start = 0;
            if (start > text.Length) start = text.Length;

            return text.Insert(start, newText);
        }

        private bool IsTextAllowed(string text)
        {
            text = (text ?? string.Empty).Trim();

            if (text.Length == 0)
                return true;

            
            if (text == "-")
                return EffectiveMin < 0;

            
            if (text == "+")
                return true;

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
        }

        

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown c)
            {
                c.UpdateButtonState();
                c.SyncTextWithValue();
            }
        }



        private static void OnMinMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown c)
            {
                c.CoerceValue(ValueProperty);
                c.UpdateButtonState();
            }
        }

        private static void OnStepChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown c)
                c.UpdateButtonState();
        }

        private static object CoerceStep(DependencyObject d, object baseValue)
        {
            int step = (int)baseValue;
            return step <= 0 ? 1 : step;
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            var c = (NumericUpDown)d;
            int value = (int)baseValue;
            return c.CoerceToRange(value);
        }

        

        private int EffectiveMin => Math.Min(Minimum, Maximum);
        private int EffectiveMax => Math.Max(Minimum, Maximum);

        private int CoerceToRange(int value)
        {
            if (value < EffectiveMin) return EffectiveMin;
            if (value > EffectiveMax) return EffectiveMax;
            return value;
        }

        private void UpdateButtonState()
        {
            if (PART_UpButton != null)
                PART_UpButton.IsEnabled = !IsReadOnly && Value < EffectiveMax;

            if (PART_DownButton != null)
                PART_DownButton.IsEnabled = !IsReadOnly && Value > EffectiveMin;
        }


    }
}