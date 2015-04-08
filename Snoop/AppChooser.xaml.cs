// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
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
		private readonly ObservableCollection<WindowInfo> _windows = new ObservableCollection<WindowInfo>();

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

						if (this._windows.Count > 0)
							this._windowsView.MoveCurrentTo(this._windows[0]);
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

	public class WindowInfo
	{
		public WindowInfo(IntPtr hwnd)
		{
			_hwnd = hwnd;			
		}

		public static void ClearCachedProcessInfo()
		{
			_processIdToValidityMap.Clear();
		}

		public event EventHandler<AttachFailedEventArgs> AttachFailed;

		public IEnumerable<NativeMethods.MODULEENTRY32> Modules
		{
			get
			{
			    if (_modules == null)
			    {
			        _modules = GetModules().ToArray();
			    }
				return _modules;
			}
		}
		/// <summary>
		/// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
		/// except that we include 32 bit modules when Snoop runs in 64 bit mode.
		/// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
		/// </summary>
		private IEnumerable<NativeMethods.MODULEENTRY32> GetModules()
		{
			int processId;
			NativeMethods.GetWindowThreadProcessId(_hwnd, out processId);

			var me32 = new NativeMethods.MODULEENTRY32();
			var hModuleSnap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.SnapshotFlags.Module | NativeMethods.SnapshotFlags.Module32, processId);
			if (!hModuleSnap.IsInvalid)
			{
				using (hModuleSnap)
				{
					me32.dwSize = (uint)Marshal.SizeOf(me32);
					if (NativeMethods.Module32First(hModuleSnap, ref me32))
					{
						do
						{
							yield return me32;
						} while (NativeMethods.Module32Next(hModuleSnap, ref me32));
					}
				}
			}
		}
		private IEnumerable<NativeMethods.MODULEENTRY32> _modules;

		public bool IsValidProcess
		{
			get
			{
				bool isValid = false;
				try
				{
					if (_hwnd == IntPtr.Zero)
						return false;

					Process process = OwningProcess;
					if (process == null)
						return false;

					// see if we have cached the process validity previously, if so, return it.
					if (_processIdToValidityMap.TryGetValue(process.Id, out isValid))
						return isValid;

					// else determine the process validity and cache it.
					if (process.Id == Process.GetCurrentProcess().Id)
					{
						isValid = false;

						// the above line stops the user from snooping on snoop, since we assume that ... that isn't their goal.
						// to get around this, the user can bring up two snoops and use the second snoop ... to snoop the first snoop.
						// well, that let's you snoop the app chooser. in order to snoop the main snoop ui, you have to bring up three snoops.
						// in this case, bring up two snoops, as before, and then bring up the third snoop, using it to snoop the first snoop.
						// since the second snoop inserted itself into the first snoop's process, you can now spy the main snoop ui from the
						// second snoop (bring up another main snoop ui to do so). pretty tricky, huh! and useful!
					}
					else
					{
						// a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
						// this includes the files:
						// PresentationFramework.dll, PresentationFramework.ni.dll
						// PresentationCore.dll, PresentationCore.ni.dll
						// wpfgfx_v0300.dll (WPF 3.0/3.5)
						// wpfgrx_v0400.dll (WPF 4.0)

						// note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
						// so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
						// see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

						// sometimes the module names aren't always the same case. compare case insensitive.
						// see for more info: http://snoopwpf.codeplex.com/workitem/6090

						if (Modules.Any(module => module.szModule.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
						                          module.szModule.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
						                          module.szModule.StartsWith("wpfgfx", StringComparison.OrdinalIgnoreCase)))
						{
						    isValid = true;
						}
					}

					_processIdToValidityMap[process.Id] = isValid;
				}
				catch
				{
				    // ignored
				}
			    return isValid;
			}
		}
		public Process OwningProcess
		{
			get { return NativeMethods.GetWindowThreadProcess(_hwnd); }
		}
		public IntPtr HWnd
		{
			get { return _hwnd; }
		}
		private readonly IntPtr _hwnd;
		public string Description
		{
			get
			{
				Process process = OwningProcess;
				return process.MainWindowTitle + " - " + process.ProcessName + " [" + process.Id + "]";
			}
		}
		public override string ToString()
		{
			return Description;
		}

		public void Snoop()
		{
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				Injector.Launch(HWnd, typeof(SnoopUI).Assembly, typeof(SnoopUI).FullName, "GoBabyGo");
			}
			catch (Exception e)
			{
				OnFailedToAttach(e);
			}
			Mouse.OverrideCursor = null;
		}
		public void Magnify()
		{
			Mouse.OverrideCursor = Cursors.Wait;
			try
			{
				Injector.Launch(HWnd, typeof(Zoomer).Assembly, typeof(Zoomer).FullName, "GoBabyGo");
			}
			catch (Exception e)
			{
				OnFailedToAttach(e);
			}
			Mouse.OverrideCursor = null;
		}

		private void OnFailedToAttach(Exception e)
		{
			var handler = AttachFailed;
			if (handler != null)
			{
				handler(this, new AttachFailedEventArgs(e, Description));
			}
		}
		
		private static readonly Dictionary<int, bool> _processIdToValidityMap = new Dictionary<int, bool>();
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
