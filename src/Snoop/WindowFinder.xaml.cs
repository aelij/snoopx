// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Snoop
{
    public enum WindowFinderType { Snoop, Magnify };

    /// <summary>
    /// Interaction logic for WindowFinder.xaml
    /// </summary>
    public partial class WindowFinder
    {
        public WindowFinder()
        {
            InitializeComponent();

            // ReSharper disable once AssignNullToNotNullAttribute
            _crosshairsCursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Snoop.Resources.SnoopCrosshairsCursor.cur"));
            
            PreviewMouseLeftButtonDown += WindowFinderMouseLeftButtonDown;
            MouseMove += WindowFinderMouseMove;
            MouseLeftButtonUp += WindowFinderMouseLeftButtonUp;
        }
        
        public WindowFinderType WindowFinderType { get; set; }

        private void WindowFinderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartSnoopTargetsSearch();
            e.Handled = true;
        }

        private void WindowFinderMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsDragging) return;

            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                StopSnoopTargetsSearch();
                return;
            }

            var windowUnderCursor = NativeMethods.GetWindowUnderMouse();
            if (_windowUnderCursor == null)
            {
                _windowUnderCursor = new WindowInfo(windowUnderCursor);
            }

            if (IsVisualFeedbackWindow(windowUnderCursor))
            {
                // if the window under the cursor is the feedback window, just ignore it.
                return;
            }

            if (windowUnderCursor != _windowUnderCursor.HWnd)
            {
                // the window under the cursor has changed

                RemoveVisualFeedback();
                _windowUnderCursor = new WindowInfo(windowUnderCursor);
                if (_windowUnderCursor.IsValidProcess)
                {
                    ShowVisualFeedback();
                }
            }

            UpdateFeedbackWindowPosition();
        }

        private void WindowFinderMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopSnoopTargetsSearch();
            if (_windowUnderCursor != null && _windowUnderCursor.IsValidProcess)
            {
                if (WindowFinderType == WindowFinderType.Snoop)
                {
                    AttachSnoop();
                }
                else if (WindowFinderType == WindowFinderType.Magnify)
                {
                    AttachMagnify();
                }
            }
        }

        private void StartSnoopTargetsSearch()
        {
            CaptureMouse();
            IsDragging = true;
            Cursor = _crosshairsCursor;
            _windowUnderCursor = null;
        }

        private void StopSnoopTargetsSearch()
        {
            ReleaseMouseCapture();
            IsDragging = false;
            Cursor = Cursors.Arrow;
            RemoveVisualFeedback();
        }

        private void ShowVisualFeedback()
        {
            if (_feedbackWindow == null)
            {
                _feedbackWindow = new SnoopabilityFeedbackWindow { Owner = Application.Current.MainWindow };

                // we don't have to worry about not having an application or not having a main window,
                // for, we are still in Snoop's process and not in the injected process.
                // so, go ahead and grab the Application.Current.MainWindow.
            }

            if (!_feedbackWindow.IsVisible)
            {
                _feedbackWindow.SnoopTargetName = _windowUnderCursor.Description;

                UpdateFeedbackWindowPosition();
                _feedbackWindow.Show();

                if (_feedbackWindowHandle == IntPtr.Zero)
                {
                    var wih = new WindowInteropHelper(_feedbackWindow);
                    _feedbackWindowHandle = wih.Handle;
                }
            }
        }

        private void RemoveVisualFeedback()
        {
            if (_feedbackWindow != null && _feedbackWindow.IsVisible)
            {
                _feedbackWindow.Hide();
            }
        }

        private bool IsVisualFeedbackWindow(IntPtr hwnd)
        {
            return hwnd != IntPtr.Zero && hwnd == _feedbackWindowHandle;
        }

        private void UpdateFeedbackWindowPosition()
        {
            if (_feedbackWindow != null)
            {
                var pos = NativeMethods.GetCursorPosition();
                // ReSharper disable PossibleNullReferenceException
                var mouse = PresentationSource.FromVisual(this).CompositionTarget.TransformFromDevice.Transform(pos);
                // ReSharper restore PossibleNullReferenceException
                _feedbackWindow.Left = mouse.X - 34;
                _feedbackWindow.Top = mouse.Y + 10;
            }
        }

        private void AttachSnoop()
        {
            new AttachFailedHandler(_windowUnderCursor);
            _windowUnderCursor.Snoop();
        }

        private void AttachMagnify()
        {
            new AttachFailedHandler(_windowUnderCursor);
            _windowUnderCursor.Magnify();
        }

        private static readonly DependencyPropertyKey _isDraggingPropertyKey = DependencyProperty.RegisterReadOnly(
            "IsDragging", typeof (bool), typeof (WindowFinder), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty IsDraggingProperty = _isDraggingPropertyKey.DependencyProperty;

        public bool IsDragging
        {
            get { return (bool) GetValue(IsDraggingProperty); }
            private set { SetValue(_isDraggingPropertyKey, value); }
        }

        private WindowInfo _windowUnderCursor;
        private SnoopabilityFeedbackWindow _feedbackWindow;
        private IntPtr _feedbackWindowHandle;
        private readonly Cursor _crosshairsCursor;
    }
}
