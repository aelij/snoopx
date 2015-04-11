using System.Windows;
using System.Windows.Controls;

namespace Snoop.Controls
{
    public static class ScrollViewerSyncBehavior
    {
        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.RegisterAttached(
            "HorizontalOffset", typeof(double), typeof(ScrollViewerSyncBehavior), new FrameworkPropertyMetadata(OnHorizontalOffsetChanged));

        private static void OnHorizontalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollViewer)d).ScrollToHorizontalOffset((double)e.NewValue);
        }

        public static void SetHorizontalOffset(ScrollViewer element, double value)
        {
            element.SetValue(HorizontalOffsetProperty, value);
        }

        public static double GetHorizontalOffset(ScrollViewer element)
        {
            return (double) element.GetValue(HorizontalOffsetProperty);
        }

        public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached(
            "VerticalOffset", typeof(double), typeof(ScrollViewerSyncBehavior), new FrameworkPropertyMetadata(OnVerticalOffsetChanged));

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ScrollViewer)d).ScrollToVerticalOffset((double)e.NewValue);
        }

        public static void SetVerticalOffset(ScrollViewer element, double value)
        {
            element.SetValue(VerticalOffsetProperty, value);
        }

        public static double GetVerticalOffset(ScrollViewer element)
        {
            return (double)element.GetValue(VerticalOffsetProperty);
        }
    }
}