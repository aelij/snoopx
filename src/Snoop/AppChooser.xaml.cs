// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Snoop.Properties;

namespace Snoop
{
	public partial class AppChooser
	{
		static AppChooser()
		{
			RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
		}

		public AppChooser()
		{
		    _windows = new ObservableCollection<WindowInfo>();
		    _windowsView = CollectionViewSource.GetDefaultView(_windows);

			InitializeComponent();

			CommandBindings.Add(new CommandBinding(RefreshCommand, HandleRefreshCommand));
			CommandBindings.Add(new CommandBinding(InspectCommand, HandleInspectCommand, HandleCanInspectOrMagnifyCommand));
			CommandBindings.Add(new CommandBinding(MagnifyCommand, HandleMagnifyCommand, HandleCanInspectOrMagnifyCommand));
			CommandBindings.Add(new CommandBinding(MinimizeCommand, HandleMinimizeCommand));
			CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, HandleCloseCommand));
		}

		public static readonly RoutedCommand InspectCommand = new RoutedCommand();
		public static readonly RoutedCommand RefreshCommand = new RoutedCommand();
		public static readonly RoutedCommand MagnifyCommand = new RoutedCommand();
		public static readonly RoutedCommand MinimizeCommand = new RoutedCommand();

		public ICollectionView Windows
		{
			get { return _windowsView; }
		}
		private readonly ICollectionView _windowsView;
		private readonly ObservableCollection<WindowInfo> _windows;

		public void Refresh()
		{
			_windows.Clear();

			Dispatcher.BeginInvoke
			(
				DispatcherPriority.Loaded,
				(DispatcherOperationCallback)delegate
				{
					try
					{
						Mouse.OverrideCursor = Cursors.Wait;

						foreach (IntPtr windowHandle in NativeMethods.ToplevelWindows)
						{
							WindowInfo window = new WindowInfo(windowHandle);
							if (window.IsValidProcess && !this.HasProcess(window.OwningProcess))
							{
								new AttachFailedHandler(window, this);
								this._windows.Add(window);
							}
						}
					}
					finally
					{
						Mouse.OverrideCursor = null;
					}
					return null;
				},
				null
			);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			try
			{
				// load the window placement details from the user settings.
				WindowPlacement wp = Settings.Default.AppChooserWindowPlacement;
				wp.Length = Marshal.SizeOf(typeof(WindowPlacement));
				wp.Flags = 0;
				wp.WindowState = (wp.WindowState == NativeMethods.SW_SHOWMINIMIZED ? NativeMethods.SW_SHOWNORMAL : wp.WindowState);
				IntPtr hwnd = new WindowInteropHelper(this).Handle;
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

			// persist the window placement details to the user settings.
			WindowPlacement wp;
			IntPtr hwnd = new WindowInteropHelper(this).Handle;
			NativeMethods.GetWindowPlacement(hwnd, out wp);
			Settings.Default.AppChooserWindowPlacement = wp;
			Settings.Default.Save();
		}

		private bool HasProcess(Process process)
		{
		    return _windows.Any(window => window.OwningProcess.Id == process.Id);
		}

	    private void HandleCanInspectOrMagnifyCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (_windowsView.CurrentItem != null)
				e.CanExecute = true;
			e.Handled = true;
		}

		private void HandleInspectCommand(object sender, ExecutedRoutedEventArgs e)
		{
			WindowInfo window = (WindowInfo)_windowsView.CurrentItem;
			if (window != null)
				window.Snoop();
		}

		private void HandleMagnifyCommand(object sender, ExecutedRoutedEventArgs e)
		{
			WindowInfo window = (WindowInfo)_windowsView.CurrentItem;
			if (window != null)
				window.Magnify();
		}

		private void HandleRefreshCommand(object sender, ExecutedRoutedEventArgs e)
		{
			// clear out cached process info to make the force refresh do the process check over again.
			WindowInfo.ClearCachedProcessInfo();
			Refresh();
		}

		private void HandleMinimizeCommand(object sender, ExecutedRoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void HandleCloseCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		private void HandleMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

	    private void MenuItem_OnSubmenuOpened(object sender, RoutedEventArgs e)
	    {
	        Refresh();
	    }
	}

    public class AttachFailedEventArgs : EventArgs
	{
		public Exception AttachException { get; private set; }
		public string WindowName { get; private set; }

		public AttachFailedEventArgs(Exception attachException, string windowName)
		{
			AttachException = attachException;
			WindowName = windowName;
		}		
	}

	public class AttachFailedHandler
	{
		public AttachFailedHandler(WindowInfo window, AppChooser appChooser = null)
		{
			window.AttachFailed += OnSnoopAttachFailed;
			_appChooser = appChooser;
		}

		private void OnSnoopAttachFailed(object sender, AttachFailedEventArgs e)
		{
			MessageBox.Show
			(
				string.Format
				(
					"Failed to attach to {0}. Exception occured:{1}{2}",
					e.WindowName,
					Environment.NewLine,
					e.AttachException
				),
				"Can't Snoop the process!"
			);
			if (_appChooser != null)
			{
				// TODO This should be implmemented through the event broker, not like this.
				_appChooser.Refresh();
			}
		}

		private readonly AppChooser _appChooser;
	}
}
