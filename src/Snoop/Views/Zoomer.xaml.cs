// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Snoop.Controls;
using Snoop.Infrastructure;
using Snoop.Properties;
using Snoop.Utilities;

namespace Snoop.Views
{
    public partial class Zoomer
    {
        public static readonly RoutedCommand ResetCommand = new RoutedCommand("Reset", typeof(Zoomer), new InputGestureCollection { new MouseGesture(MouseAction.LeftDoubleClick), new KeyGesture(Key.F5) });
        public static readonly RoutedCommand ZoomInCommand = new RoutedCommand("ZoomIn", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.OemPlus), new KeyGesture(Key.Up, ModifierKeys.Control) });
        public static readonly RoutedCommand ZoomOutCommand = new RoutedCommand("ZoomOut", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.OemMinus), new KeyGesture(Key.Down, ModifierKeys.Control) });
        public static readonly RoutedCommand PanLeftCommand = new RoutedCommand("PanLeft", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.Left) });
        public static readonly RoutedCommand PanRightCommand = new RoutedCommand("PanRight", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.Right) });
        public static readonly RoutedCommand PanUpCommand = new RoutedCommand("PanUp", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.Up) });
        public static readonly RoutedCommand PanDownCommand = new RoutedCommand("PanDown", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.Down) });
        public static readonly RoutedCommand SwitchTo2DCommand = new RoutedCommand("SwitchTo2D", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.F2) });
        public static readonly RoutedCommand SwitchTo3DCommand = new RoutedCommand("SwitchTo3D", typeof(Zoomer), new InputGestureCollection { new KeyGesture(Key.F3) });

        private const double ZoomFactor = 1.1;

        private readonly TranslateTransform _translation;
        private readonly ScaleTransform _zoom;
        private readonly TransformGroup _transform;
        private Point _downPoint;
        private object _target;
        private VisualTree3DView _visualTree3DView;

        public Zoomer()
        {
            _translation = new TranslateTransform();
            _zoom = new ScaleTransform();
            _transform = new TransformGroup();

            CommandBindings.Add(new CommandBinding(ResetCommand, HandleReset, CanReset));
            CommandBindings.Add(new CommandBinding(ZoomInCommand, HandleZoomIn));
            CommandBindings.Add(new CommandBinding(ZoomOutCommand, HandleZoomOut));
            CommandBindings.Add(new CommandBinding(PanLeftCommand, HandlePanLeft));
            CommandBindings.Add(new CommandBinding(PanRightCommand, HandlePanRight));
            CommandBindings.Add(new CommandBinding(PanUpCommand, HandlePanUp));
            CommandBindings.Add(new CommandBinding(PanDownCommand, HandlePanDown));
            CommandBindings.Add(new CommandBinding(SwitchTo2DCommand, HandleSwitchTo2D));
            CommandBindings.Add(new CommandBinding(SwitchTo3DCommand, HandleSwitchTo3D, CanSwitchTo3D));

            InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;

            InitializeComponent();

            _transform.Children.Add(_zoom);
            _transform.Children.Add(_translation);

            Viewbox.RenderTransform = _transform;
        }

        public static void GoBabyGo()
        {
            Dispatcher dispatcher;
            if (Application.Current == null)
            {
                if (!SnoopModes.MultipleDispatcherMode)
                {
                    dispatcher = Dispatcher.CurrentDispatcher;
                }
                else
                {
                    // can't find a dispatcher
                    return;
                }
            }
            else
            {
                dispatcher = Application.Current.Dispatcher;
            }

            if (dispatcher.CheckAccess())
            {
                var zoomer = new Zoomer();
                zoomer.Magnify();
            }
            else
            {
                dispatcher.Invoke(GoBabyGo);
            }
        }

        public void Magnify()
        {
            var root = FindRoot();
            if (root == null)
            {
                MessageBox.Show
                (
                    "Can't find a current application or a PresentationSource root visual!",
                    "Can't Magnify",
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation
                );
            }

            Magnify(root);
        }

        public void Magnify(object root)
        {
            Target = root;

            var ownerWindow = SnoopWindowUtils.FindOwnerWindow();
            if (ownerWindow != null)
                Owner = ownerWindow;

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);

            Show();
            Activate();
        }

        public object Target
        {
            get { return _target; }
            set
            {
                _target = value;
                var element = CreateIfPossible(value);
                if (element != null)
                    Viewbox.Child = element;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                // load the window placement details from the user settings.
                var wp = Settings.Default.ZoomerWindowPlacement;
                wp.Length = Marshal.SizeOf(typeof(WindowPlacement));
                wp.Flags = 0;
                wp.WindowState = (wp.WindowState == NativeMethods.SW_SHOWMINIMIZED ? NativeMethods.SW_SHOWNORMAL : wp.WindowState);
                var hwnd = new WindowInteropHelper(this).Handle;
                NativeMethods.SetWindowPlacement(hwnd, ref wp);
            }
            catch
            {
                // ignored
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            Viewbox.Child = null;

            // persist the window placement details to the user settings.
            WindowPlacement wp;
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.GetWindowPlacement(hwnd, out wp);
            Settings.Default.ZoomerWindowPlacement = wp;
            Settings.Default.Save();

            SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
        }

        private void HandleReset(object target, ExecutedRoutedEventArgs args)
        {
            _translation.X = 0;
            _translation.Y = 0;
            _zoom.ScaleX = 1;
            _zoom.ScaleY = 1;
            _zoom.CenterX = 0;
            _zoom.CenterY = 0;

            if (_visualTree3DView != null)
            {
                _visualTree3DView.Reset();
                ZScaleSlider.Value = 0;
            }
        }

        private static void CanReset(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = true;
            args.Handled = true;
        }

        private void HandleZoomIn(object target, ExecutedRoutedEventArgs args)
        {
            var offset = Mouse.GetPosition(Viewbox);
            Zoom(ZoomFactor, offset);
        }

        private void HandleZoomOut(object target, ExecutedRoutedEventArgs args)
        {
            var offset = Mouse.GetPosition(Viewbox);
            Zoom(1 / ZoomFactor, offset);
        }

        private void HandlePanLeft(object target, ExecutedRoutedEventArgs args)
        {
            _translation.X -= 5;
        }

        private void HandlePanRight(object target, ExecutedRoutedEventArgs args)
        {
            _translation.X += 5;
        }

        private void HandlePanUp(object target, ExecutedRoutedEventArgs args)
        {
            _translation.Y -= 5;
        }

        private void HandlePanDown(object target, ExecutedRoutedEventArgs args)
        {
            _translation.Y += 5;
        }

        private void HandleSwitchTo2D(object target, ExecutedRoutedEventArgs args)
        {
            if (_visualTree3DView != null)
            {
                Target = _target;
                _visualTree3DView = null;
                ZScaleSlider.Visibility = Visibility.Collapsed;
            }
        }

        private void HandleSwitchTo3D(object target, ExecutedRoutedEventArgs args)
        {
            var visual = _target as Visual;
            if (_visualTree3DView == null && visual != null)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    _visualTree3DView = new VisualTree3DView(visual);
                    Viewbox.Child = _visualTree3DView;
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
                ZScaleSlider.Visibility = Visibility.Visible;
            }
        }

        private void CanSwitchTo3D(object target, CanExecuteRoutedEventArgs args)
        {
            args.CanExecute = (_target is Visual);
            args.Handled = true;
        }

        private void Content_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _downPoint = e.GetPosition(DocumentRoot);
            DocumentRoot.CaptureMouse();
        }

        private void Content_MouseMove(object sender, MouseEventArgs e)
        {
            if (DocumentRoot.IsMouseCaptured)
            {
                var delta = e.GetPosition(DocumentRoot) - _downPoint;
                _translation.X += delta.X;
                _translation.Y += delta.Y;

                _downPoint = e.GetPosition(DocumentRoot);
            }
        }

        private void Content_MouseUp(object sender, MouseEventArgs e)
        {
            DocumentRoot.ReleaseMouseCapture();
        }

        private void Content_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var zoom = Math.Pow(ZoomFactor, e.Delta / 120.0);
            var offset = e.GetPosition(Viewbox);
            Zoom(zoom, offset);
        }

        private void ZScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_visualTree3DView != null)
            {
                _visualTree3DView.ZScale = Math.Pow(10, e.NewValue);
            }
        }

        private static UIElement CreateIfPossible(object item)
        {
            return ZoomerUtilities.CreateIfPossible(item);
        }

        private void Zoom(double zoom, Point offset)
        {
            var v = new Vector((1 - zoom) * offset.X, (1 - zoom) * offset.Y);

            var translationVector = v * _transform.Value;
            _translation.X += translationVector.X;
            _translation.Y += translationVector.Y;

            _zoom.ScaleX = _zoom.ScaleX * zoom;
            _zoom.ScaleY = _zoom.ScaleY * zoom;
        }

        private object FindRoot()
        {
            object root = null;

            if (SnoopModes.MultipleDispatcherMode)
            {
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    var visual = presentationSource.RootVisual as UIElement;
                    if (visual != null && visual.Dispatcher.CheckAccess())
                    {
                        root = presentationSource.RootVisual;
                        break;
                    }
                }
            }
            else if (Application.Current != null)
            {
                // try to use the application's main window (if visible) as the root
                if (Application.Current.MainWindow != null && Application.Current.MainWindow.Visibility == Visibility.Visible)
                {
                    root = Application.Current.MainWindow;
                }
                else
                {
                    // else search for the first visible window in the list of the application's windows
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.Visibility == Visibility.Visible)
                        {
                            root = window;
                            break;
                        }
                    }
                }
            }
            else
            {
                // if we don't have a current application,
                // then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).

                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    // this is windows forms -> wpf interop

                    // call ElementHost.EnableModelessKeyboardInterop
                    // to allow the Zoomer window to receive keyboard messages.
                    ElementHost.EnableModelessKeyboardInterop(this);
                }
            }

            if (root == null)
            {
                // if we still don't have a root to magnify

                // let's iterate over PresentationSource.CurrentSources,
                // and use the first non-null, visible RootVisual we find as root to inspect.
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    var visual = presentationSource.RootVisual as UIElement;
                    if (visual != null && visual.Visibility == Visibility.Visible)
                    {
                        root = presentationSource.RootVisual;
                        break;
                    }
                }
            }

            // if the root is a window, let's magnify the window's content.
            // this is better, as otherwise, you will have window background along with the window's content.
            var windowRoot = root as Window;
            if (windowRoot?.Content != null)
            {
                root = windowRoot.Content;
            }

            return root;
        }
    }

    public class DoubleToWhitenessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (float)(double)value;
            var c = new Color { ScR = val, ScG = val, ScB = val, ScA = 1 };

            return new SolidColorBrush(c);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
