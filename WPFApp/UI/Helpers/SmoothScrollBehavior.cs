using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WPFApp.UI.Helpers
{
    /// <summary>
    /// Lightweight smooth wheel-scrolling behavior.
    /// - Does not alter templates/styles.
    /// - Animates only vertical offset for mouse wheel input.
    /// - Keyboard/Page/Home/End and horizontal scroll remain untouched.
    /// </summary>
    public static class SmoothScrollBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static readonly DependencyProperty WheelLinesPerNotchProperty =
            DependencyProperty.RegisterAttached(
                "WheelLinesPerNotch",
                typeof(double),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(3d));

        public static readonly DependencyProperty AnimationDurationMsProperty =
            DependencyProperty.RegisterAttached(
                "AnimationDurationMs",
                typeof(double),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(100d));

        private static readonly DependencyProperty AnimationHostProperty =
            DependencyProperty.RegisterAttached(
                "AnimationHost",
                typeof(ScrollAnimationHost),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(null));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        public static double GetWheelLinesPerNotch(DependencyObject obj) => (double)obj.GetValue(WheelLinesPerNotchProperty);
        public static void SetWheelLinesPerNotch(DependencyObject obj, double value) => obj.SetValue(WheelLinesPerNotchProperty, value);

        public static double GetAnimationDurationMs(DependencyObject obj) => (double)obj.GetValue(AnimationDurationMsProperty);
        public static void SetAnimationDurationMs(DependencyObject obj, double value) => obj.SetValue(AnimationDurationMsProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
                return;

            if ((bool)e.NewValue)
            {
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
                element.PreviewMouseWheel += OnPreviewMouseWheel;
                element.Unloaded -= OnElementUnloaded;
                element.Unloaded += OnElementUnloaded;
            }
            else
            {
                element.PreviewMouseWheel -= OnPreviewMouseWheel;
                element.Unloaded -= OnElementUnloaded;
                DetachAnimationHost(element);
            }
        }

        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
                DetachAnimationHost(element);
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not DependencyObject source)
                return;

            if (e.Handled)
                return;

            var viewer = FindDescendantScrollViewer(source);
            if (viewer == null)
                return;

            if (viewer.ComputedVerticalScrollBarVisibility != Visibility.Visible || viewer.ScrollableHeight <= 0)
                return;

            var host = GetOrCreateHost(source, viewer);
            var lines = Math.Max(1d, GetWheelLinesPerNotch(source));
            var deltaItems = (e.Delta / 120d) * lines;
            var pixelEstimate = Math.Max(14d, viewer.ViewportHeight / 12d);
            var deltaPixels = -deltaItems * pixelEstimate;

            host.AnimateBy(deltaPixels, TimeSpan.FromMilliseconds(Math.Max(40d, GetAnimationDurationMs(source))));
            e.Handled = true;
        }

        private static ScrollAnimationHost GetOrCreateHost(DependencyObject owner, ScrollViewer viewer)
        {
            if (owner.GetValue(AnimationHostProperty) is ScrollAnimationHost existing)
            {
                if (ReferenceEquals(existing.ScrollViewer, viewer))
                    return existing;

                existing.Dispose();
            }

            var created = new ScrollAnimationHost(viewer);
            owner.SetValue(AnimationHostProperty, created);
            return created;
        }

        private static void DetachAnimationHost(DependencyObject owner)
        {
            if (owner.GetValue(AnimationHostProperty) is ScrollAnimationHost host)
            {
                host.Dispose();
                owner.ClearValue(AnimationHostProperty);
            }
        }

        private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer sv)
                return sv;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                var found = FindDescendantScrollViewer(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        private sealed class ScrollAnimationHost : DependencyObject, IDisposable
        {
            public static readonly DependencyProperty VerticalOffsetProperty =
                DependencyProperty.Register(
                    nameof(VerticalOffset),
                    typeof(double),
                    typeof(ScrollAnimationHost),
                    new PropertyMetadata(0d, OnVerticalOffsetChanged));

            public ScrollViewer ScrollViewer { get; }

            public double VerticalOffset
            {
                get => (double)GetValue(VerticalOffsetProperty);
                set => SetValue(VerticalOffsetProperty, value);
            }

            public ScrollAnimationHost(ScrollViewer scrollViewer)
            {
                ScrollViewer = scrollViewer;
                VerticalOffset = scrollViewer.VerticalOffset;
            }

            public void AnimateBy(double delta, TimeSpan duration)
            {
                var max = Math.Max(0d, ScrollViewer.ScrollableHeight);
                var target = Math.Clamp(ScrollViewer.VerticalOffset + delta, 0d, max);

                BeginAnimation(
                    VerticalOffsetProperty,
                    new DoubleAnimation
                    {
                        From = VerticalOffset,
                        To = target,
                        Duration = new Duration(duration),
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                        FillBehavior = FillBehavior.Stop
                    },
                    HandoffBehavior.SnapshotAndReplace);

                VerticalOffset = target;
            }

            public void Dispose()
            {
                BeginAnimation(VerticalOffsetProperty, null);
            }

            private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is ScrollAnimationHost host)
                    host.ScrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
