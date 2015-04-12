// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Snoop.Infrastructure;

namespace Snoop.Views
{
    /// <summary>
    /// Interaction logic for ZoomerControl.xaml
    /// </summary>
    public partial class ZoomerControl
    {
        public ZoomerControl()
        {
            InitializeComponent();

            _transform.Children.Add(_zoom);
            _transform.Children.Add(_translation);

            Viewbox.RenderTransform = _transform;
        }

        /// <summary>
        /// Gets or sets the Target property.
        /// </summary>
        public object Target
        {
            get { return GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        /// <summary>
        /// Target Dependency Property
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register
            (
                "Target",
                typeof(object),
                typeof(ZoomerControl),
                new FrameworkPropertyMetadata(null, OnTargetChanged)
            );

        /// <summary>
        /// Handles changes to the Target property.
        /// </summary>
        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ZoomerControl)d).OnTargetChanged(e);
        }

        /// <summary>
        /// Provides derived classes an opportunity to handle changes to the Target property.
        /// </summary>
        protected virtual void OnTargetChanged(DependencyPropertyChangedEventArgs e)
        {
            ResetZoomAndTranslation();

            Cursor = Cursors.SizeAll;

            UIElement element = CreateIfPossible(Target);
            if (element != null)
                Viewbox.Child = element;
        }

        private void Content_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Focus();
            _downPoint = e.GetPosition(DocumentRoot);
            DocumentRoot.CaptureMouse();
        }

        private void Content_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsValidTarget && DocumentRoot.IsMouseCaptured)
            {
                Vector delta = e.GetPosition(DocumentRoot) - _downPoint;
                _translation.X += delta.X;
                _translation.Y += delta.Y;

                _downPoint = e.GetPosition(DocumentRoot);
            }
        }

        private void Content_MouseUp(object sender, MouseEventArgs e)
        {
            DocumentRoot.ReleaseMouseCapture();
        }

        public void DoMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (IsValidTarget)
            {
                double zoom = Math.Pow(ZoomFactor, e.Delta / 120.0);
                Point offset = e.GetPosition(Viewbox);
                Zoom(zoom, offset);
            }
        }

        private void ResetZoomAndTranslation()
        {
            _zoom.ScaleX = 1.0;
            _zoom.ScaleY = 1.0;

            _translation.X = 0.0;
            _translation.Y = 0.0;
        }

        private static UIElement CreateIfPossible(object item)
        {
            return ZoomerUtilities.CreateIfPossible(item);
        }

        private void Zoom(double zoom, Point offset)
        {
            Vector v = new Vector((1 - zoom) * offset.X, (1 - zoom) * offset.Y);

            Vector translationVector = v * _transform.Value;
            _translation.X += translationVector.X;
            _translation.Y += translationVector.Y;

            _zoom.ScaleX = _zoom.ScaleX * zoom;
            _zoom.ScaleY = _zoom.ScaleY * zoom;
        }

        private bool IsValidTarget
        {
            get { return Target != null; }
        }

        private readonly TranslateTransform _translation = new TranslateTransform();
        private readonly ScaleTransform _zoom = new ScaleTransform();
        private readonly TransformGroup _transform = new TransformGroup();
        private Point _downPoint;

        private const double ZoomFactor = 1.1;
    }
}
