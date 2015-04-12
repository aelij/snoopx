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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Snoop.Annotations;
using Snoop.Infrastructure;
using Snoop.Properties;
using Snoop.Utilities;
using Snoop.VisualTree;

namespace Snoop.Views
{
    // ReSharper disable once InconsistentNaming
    public partial class SnoopUI : INotifyPropertyChanged
    {
        #region Public Static Routed Commands

        public static readonly RoutedCommand IntrospectCommand = new RoutedCommand("Introspect", typeof(SnoopUI));
        public static readonly RoutedCommand RefreshCommand = new RoutedCommand("Refresh", typeof(SnoopUI));
        public static readonly RoutedCommand InspectCommand = new RoutedCommand("Inspect", typeof(SnoopUI));
        public static readonly RoutedCommand SelectFocusCommand = new RoutedCommand("SelectFocus", typeof(SnoopUI));
        public static readonly RoutedCommand SelectFocusScopeCommand = new RoutedCommand("SelectFocusScope", typeof(SnoopUI));
        public static readonly RoutedCommand ClearSearchFilterCommand = new RoutedCommand("ClearSearchFilter", typeof(SnoopUI));
        public static readonly RoutedCommand CopyPropertyChangesCommand = new RoutedCommand("CopyPropertyChanges", typeof(SnoopUI));
       
        #endregion

        #region Static Constructor

        static SnoopUI()
        {
            IntrospectCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
            RefreshCommand.InputGestures.Add(new KeyGesture(Key.F5));
            ClearSearchFilterCommand.InputGestures.Add(new KeyGesture(Key.Escape));
            CopyPropertyChangesCommand.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Shift));
        }

        #endregion

        #region Public Constructor
        public SnoopUI()
        {
            _filterCall = new DelayedCall(ProcessFilter, DispatcherPriority.Background);

            InheritanceBehavior = InheritanceBehavior.SkipToThemeNext;
            InitializeComponent();

            // wrap the following PresentationTraceSources.Refresh() call in a try/catch
            // sometimes a NullReferenceException occurs
            // due to empty <filter> elements in the app.config file of the app you are snooping
            // see the following for more info:
            // http://snoopwpf.codeplex.com/discussions/236503
            // http://snoopwpf.codeplex.com/workitem/6647
            try
            {
                PresentationTraceSources.Refresh();
                PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            }
            catch (NullReferenceException)
            {
                // swallow this exception since you can Snoop just fine anyways.
            }

            CommandBindings.Add(new CommandBinding(IntrospectCommand, HandleIntrospection));
            CommandBindings.Add(new CommandBinding(RefreshCommand, HandleRefresh));
            CommandBindings.Add(new CommandBinding(InspectCommand, HandleInspect));

            CommandBindings.Add(new CommandBinding(SelectFocusCommand, HandleSelectFocus));
            CommandBindings.Add(new CommandBinding(SelectFocusScopeCommand, HandleSelectFocusScope));

            //NOTE: this is up here in the outer UI layer so ESC will clear any typed filter regardless of where the focus is
            // (i.e. focus on a selected item in the tree, not in the property list where the search box is hosted)
            CommandBindings.Add(new CommandBinding(ClearSearchFilterCommand, ClearSearchFilterHandler));

            CommandBindings.Add(new CommandBinding(CopyPropertyChangesCommand, CopyPropertyChangesHandler));

            InputManager.Current.PreProcessInput += HandlePreProcessInput;
            Tree.SelectedItemChanged += HandleTreeSelectedItemChanged;

            // we can't catch the mouse wheel at the ZoomerControl level,
            // so we catch it here, and relay it to the ZoomerControl.
            MouseWheel += SnoopUI_MouseWheel;

            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.3) };
            _filterTimer.Tick += (s, e) =>
            {
                EnqueueAfterSettingFilter();
                _filterTimer.Stop();
            };
        }

        #endregion

        #region Public Static Methods

        public static bool GoBabyGo()
        {
            try
            {
                SnoopApplication();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("There was an error snooping! Message = {0}\n\nStack Trace:\n{1}", ex.Message, ex.StackTrace), "Error Snooping", MessageBoxButton.OK);
                return false;
            }
        }

        public static void SnoopApplication()
        {
            var dispatcher = Application.Current == null ? Dispatcher.CurrentDispatcher : Application.Current.Dispatcher;

            if (dispatcher.CheckAccess())
            {
                SnoopUI snoop = new SnoopUI();
                var title = TryGetMainWindowTitle();
                if (!string.IsNullOrEmpty(title))
                {
                    snoop.Title = string.Format("{0} - SnoopX", title);
                }

                snoop.Inspect();

                CheckForOtherDispatchers(dispatcher);
            }
            else
            {
                dispatcher.Invoke(SnoopApplication);
            }
        }

        private static void CheckForOtherDispatchers(Dispatcher mainDispatcher)
        {
            // check and see if any of the root visuals have a different mainDispatcher
            // if so, ask the user if they wish to enter multiple mainDispatcher mode.
            // if they do, launch a snoop ui for every additional mainDispatcher.
            // see http://snoopwpf.codeplex.com/workitem/6334 for more info.

            var rootVisuals = new List<Visual>();
            var dispatchers = new List<Dispatcher> { mainDispatcher };
            foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
            {
                Visual presentationSourceRootVisual = presentationSource.RootVisual;

                if (!(presentationSourceRootVisual is Window))
                    continue;

                Dispatcher presentationSourceRootVisualDispatcher = presentationSourceRootVisual.Dispatcher;

                if (dispatchers.IndexOf(presentationSourceRootVisualDispatcher) == -1)
                {
                    rootVisuals.Add(presentationSourceRootVisual);
                    dispatchers.Add(presentationSourceRootVisualDispatcher);
                }
            }

            if (rootVisuals.Count > 0)
            {
                var result =
                    MessageBox.Show
                    (
                        "Snoop has noticed windows running in multiple dispatchers!\n\n" +
                        "Would you like to enter multiple dispatcher mode, and have a separate Snoop window for each dispatcher?\n\n" +
                        "Without having a separate Snoop window for each dispatcher, you will not be able to Snoop the windows in the dispatcher threads outside of the main dispatcher. " +
                        "Also, note, that if you bring up additional windows in additional dispatchers (after Snooping), you will need to Snoop again in order to launch Snoop windows for those additional dispatchers.",
                        "Enter Multiple Dispatcher Mode",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                if (result == MessageBoxResult.Yes)
                {
                    SnoopModes.MultipleDispatcherMode = true;
                    Thread thread = new Thread(DispatchOut);
                    thread.Start(rootVisuals);
                }
            }
        }

        private static void DispatchOut(object o)
        {
            List<Visual> visuals = (List<Visual>)o;
            foreach (var visual in visuals)
            {
                visual.Dispatcher.Invoke(() =>
                {
                    var snoopOtherDispatcher = new SnoopUI();
                    snoopOtherDispatcher.Inspect(visual, visual as Window);
                });
            }
        }

        #endregion

        #region Public Properties

        #region VisualTreeItems
        
        /// <summary>
        /// This is the collection of VisualTreeItem(s) that the visual tree TreeView binds to.
        /// </summary>
        public ObservableCollection<VisualTreeItem> VisualTreeItems
        {
            get { return _visualTreeItems; }
        }

        #endregion

        #region Root

        /// <summary>
        /// Root element of the visual tree
        /// </summary>
        public VisualTreeItem Root
        {
            get { return _rootVisualTreeItem; }
            private set
            {
                _rootVisualTreeItem = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// rootVisualTreeItem is the VisualTreeItem for the root you are inspecting.
        /// </summary>
        private VisualTreeItem _rootVisualTreeItem;
        /// <summary>
        /// root is the object you are inspecting.
        /// </summary>
        private object _root;
        #endregion

        #region CurrentSelection
        /// <summary>
        /// Currently selected item in the tree view.
        /// </summary>
        public VisualTreeItem CurrentSelection
        {
            get { return _currentSelection; }
            set
            {
                if (_currentSelection != value)
                {
                    if (_currentSelection != null)
                    {
                        SaveEditedProperties(_currentSelection);
                        _currentSelection.IsSelected = false;
                    }

                    _currentSelection = value;

                    if (_currentSelection != null)
                    {
                        _currentSelection.IsSelected = true;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged("CurrentFocusScope");

                    if (_visualTreeItems.Count > 1 || _visualTreeItems.Count == 1 && _visualTreeItems[0] != _rootVisualTreeItem)
                    {
                        // Check whether the selected item is filtered out by the filter,
                        // in which case reset the filter.
                        VisualTreeItem tmp = _currentSelection;
                        while (tmp != null && !_visualTreeItems.Contains(tmp))
                        {
                            tmp = tmp.Parent;
                        }
                        if (tmp == null)
                        {
                            // The selected item is not a descendant of any root.
                            RefreshCommand.Execute(null, this);
                        }
                    }
                }
            }
        }

        private VisualTreeItem _currentSelection;

        #endregion

        #region Filter
        /// <summary>
        /// This Filter property is bound to the editable combo box that the user can type in to filter the visual tree TreeView.
        /// Every time the user types a key, the setter gets called, enqueueing a delayed call to the ProcessFilter method.
        /// </summary>
        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;

                if (!_fromTextBox)
                {
                    EnqueueAfterSettingFilter();
                }
                else
                {
                    _filterTimer.Stop();
                    _filterTimer.Start();
                }
            }
        }

        private void SetFilter(string value)
        {
            _fromTextBox = false;
            Filter = value;
            _fromTextBox = true;
        }

        private void EnqueueAfterSettingFilter()
        {
            _filterCall.Enqueue();

            OnPropertyChanged("Filter");
        }

        private string _filter = string.Empty;
        #endregion

        #region EventFilter
        public string EventFilter
        {
            get { return _eventFilter; }
            set
            {
                _eventFilter = value;
                EventsListener.Filter = value;
            }
        }
        #endregion

        #region CurrentFocus
        public IInputElement CurrentFocus
        {
            get
            {
                var newFocus = Keyboard.FocusedElement;
                if (newFocus != _currentFocus)
                {
                    // Store reference to previously focused element only if focused element was changed.
                    _previousFocus = _currentFocus;
                }
                _currentFocus = newFocus;

                return _returnPreviousFocus ? _previousFocus : _currentFocus;
            }
        }
        #endregion

        #region CurrentFocusScope
        public object CurrentFocusScope
        {
            get
            {
                if (CurrentSelection == null)
                    return null;

                var selectedItem = CurrentSelection.Target as DependencyObject;
                if (selectedItem != null)
                {
                    return FocusManager.GetFocusScope(selectedItem);
                }
                return null;
            }
        }
        #endregion

        #endregion

        #region Public Methods
        public void Inspect()
        {
            object localRoot = FindRoot();
            if (localRoot == null)
            {
                if (!SnoopModes.MultipleDispatcherMode)
                {
                    //SnoopModes.MultipleDispatcherMode is always false for all scenarios except for cases where we are running multiple dispatchers.
                    //If SnoopModes.MultipleDispatcherMode was set to true, then there definitely was a root visual found in another dispatcher, so
                    //the message below would be wrong.
                    MessageBox.Show
                    (
                        "Can't find a current application or a PresentationSource root visual!",
                        "Can't Snoop",
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation
                    );
                }

                return;
            }
            Load(localRoot);

            Window ownerWindow = SnoopWindowUtils.FindOwnerWindow();
            if (ownerWindow != null)
            {
                if (ownerWindow.Dispatcher != Dispatcher)
                {
                    return;
                }
                Owner = ownerWindow;

                // watch for window closing so we can spit out the changed properties
                ownerWindow.Closing += SnoopedWindowClosingHandler;
            }

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
            Dispatcher.UnhandledException += UnhandledExceptionHandler;

            Show();
            Activate();
        }
        public void Inspect(object localRoot, Window ownerWindow)
        {
            Dispatcher.UnhandledException += UnhandledExceptionHandler;

            Load(localRoot);

            if (ownerWindow != null)
            {
                Owner = ownerWindow;

                // watch for window closing so we can spit out the changed properties
                ownerWindow.Closing += SnoopedWindowClosingHandler;
            }

            SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);

            Show();
            Activate();
        }

        private static void UnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (SnoopModes.IgnoreExceptions)
            {
                return;
            }

            if (SnoopModes.SwallowExceptions)
            {
                e.Handled = true;
                return;
            }

            // should we check if the exception came from Snoop? perhaps seeing if any Snoop call is in the stack trace?
            var dialog = new ErrorDialog { Exception = e.Exception };
            var result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
                e.Handled = true;
        }

        public void ApplyReduceDepthFilter(VisualTreeItem newRoot)
        {
            if (_reducedDepthRoot != newRoot)
            {
                if (_reducedDepthRoot == null)
                {
                    Dispatcher.BeginInvoke
                    (
                        DispatcherPriority.Background,
                        (Action)
                        delegate
                        {
                            this._visualTreeItems.Clear();
                            this._visualTreeItems.Add(_reducedDepthRoot);
                            _reducedDepthRoot = null;
                        }
                    );
                }
                _reducedDepthRoot = newRoot;
            }
        }


        /// <summary>
        /// Loop through the properties in the current PropertyGrid and save away any properties
        /// that have been changed by the user.  
        /// </summary>
        /// <param name="owningObject">currently selected object that owns the properties in the grid (before changing selection to the new object)</param>
        private void SaveEditedProperties(VisualTreeItem owningObject)
        {
            foreach (PropertyInformation property in PropertyGrid.PropertyGrid.Properties)
            {
                if (property.IsValueChangedByUser)
                {
                    EditedPropertiesHelper.AddEditedProperty(Dispatcher, owningObject, property);
                }
            }
        }

        #endregion

        #region Protected Event Overrides
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                // load the window placement details from the user settings.
                WindowPlacement wp = Settings.Default.SnoopUIWindowPlacement;
                wp.Length = Marshal.SizeOf(typeof(WindowPlacement));
                wp.Flags = 0;
                wp.WindowState = (wp.WindowState == NativeMethods.SW_SHOWMINIMIZED ? NativeMethods.SW_SHOWNORMAL : wp.WindowState);
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                NativeMethods.SetWindowPlacement(hwnd, ref wp);

                // load whether all properties are shown by default
                PropertyGrid.ShowDefaults = Settings.Default.ShowDefaults;

                // load whether the previewer is shown by default
                PreviewArea.IsActive = Settings.Default.ShowPreviewer;
            }
            catch
            {
                // ignored
            }
        }
        /// <summary>
        /// Cleanup when closing the window.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            // unsubscribe to owner window closing event
            // replaces previous attempts to hookup to MainWindow.Closing on the wrong dispatcher thread
            // This one should be running on the right dispatcher thread since this SnoopUI instance
            // is wired up to the dispatcher thread/window that it owns
            if (Owner != null)
            {
                Owner.Closing -= SnoopedWindowClosingHandler;
            }

            CurrentSelection = null;

            InputManager.Current.PreProcessInput -= HandlePreProcessInput;
            EventsListener.Stop();

            EditedPropertiesHelper.DumpObjectsWithEditedProperties();

            // persist the window placement details to the user settings.
            WindowPlacement wp;
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.GetWindowPlacement(hwnd, out wp);
            Settings.Default.SnoopUIWindowPlacement = wp;

            // persist whether all properties are shown by default
            Settings.Default.ShowDefaults = PropertyGrid.ShowDefaults;

            // persist whether the previewer is shown by default
            Settings.Default.ShowPreviewer = PreviewArea.IsActive;

            // actually do the persisting
            Settings.Default.Save();

            SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
        }

        /// <summary>
        /// Event handler for a snooped window closing. This is our chance to spit out 
        /// all the properties that changed during the snoop session for that window.
        /// Note: there may be multiple snooped windows (when in multiple dispatcher mode)
        /// and each window is hooked up to it's own instance of SnoopUI and this event.
        /// </summary>
        private void SnoopedWindowClosingHandler(object sender, CancelEventArgs e)
        {
            // changing the selection captures any changes in the selected item at the time of window closing 
            CurrentSelection = null;
            EditedPropertiesHelper.DumpObjectsWithEditedProperties();
        }

        #endregion

        #region Private Routed Event Handlers
        /// <summary>
        /// Just for fun, the ability to run Snoop on itself :)
        /// </summary>
        private void HandleIntrospection(object sender, ExecutedRoutedEventArgs e)
        {
            Load(this);
        }
        private void HandleRefresh(object sender, ExecutedRoutedEventArgs e)
        {
            Cursor saveCursor = Mouse.OverrideCursor;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                object currentTarget = CurrentSelection != null ? CurrentSelection.Target : null;

                _visualTreeItems.Clear();

                Root = VisualTreeItem.Construct(_root, null);

                if (currentTarget != null)
                {
                    VisualTreeItem visualItem = FindItem(currentTarget);
                    if (visualItem != null)
                        CurrentSelection = visualItem;
                }

                SetFilter(_filter);
            }
            finally
            {
                Mouse.OverrideCursor = saveCursor;
            }
        }

        private void HandleInspect(object sender, ExecutedRoutedEventArgs e)
        {
            Visual visual = e.Parameter as Visual;
            if (visual != null)
            {
                VisualTreeItem node = FindItem(visual);
                if (node != null)
                    CurrentSelection = node;
            }
            else if (e.Parameter != null)
            {
                PropertyGrid.SetTarget(e.Parameter);
            }
        }
        private void HandleSelectFocus(object sender, ExecutedRoutedEventArgs e)
        {
            // We know we've stolen focus here. Let's use previously focused element.
            _returnPreviousFocus = true;
            SelectItem(CurrentFocus as DependencyObject);
            _returnPreviousFocus = false;
            OnPropertyChanged("CurrentFocus");
        }

        private void HandleSelectFocusScope(object sender, ExecutedRoutedEventArgs e)
        {
            SelectItem(e.Parameter as DependencyObject);
        }

        private void ClearSearchFilterHandler(object sender, ExecutedRoutedEventArgs e)
        {
            PropertyGrid.StringFilter = string.Empty;
        }

        private void CopyPropertyChangesHandler(object sender, ExecutedRoutedEventArgs e)
        {
            if (_currentSelection != null)
                SaveEditedProperties(_currentSelection);

            EditedPropertiesHelper.DumpObjectsWithEditedProperties();
        }

        private void SelectItem(DependencyObject item)
        {
            if (item != null)
            {
                VisualTreeItem node = FindItem(item);
                if (node != null)
                    CurrentSelection = node;
            }
        }
        #endregion

        #region Private Event Handlers

        private readonly Stopwatch _stickyInputTimer = new Stopwatch();

        private void HandlePreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            OnPropertyChanged("CurrentFocus");

            if (!_stickyInputTimer.IsRunning)
            {
                ModifierKeys currentModifiers = InputManager.Current.PrimaryKeyboardDevice.Modifiers;
                if (!((currentModifiers & ModifierKeys.Control) != 0 && (currentModifiers & ModifierKeys.Shift) != 0))
                    return;
                if ((currentModifiers & ModifierKeys.Alt) != 0)
                {
                    _stickyInputTimer.Reset();
                    _stickyInputTimer.Start();
                }
            }
            else if (_stickyInputTimer.ElapsedMilliseconds > 5000)
            {
                _stickyInputTimer.Stop();
            }

            Visual directlyOver = Mouse.PrimaryDevice.DirectlyOver as Visual;
            if ((directlyOver == null) || directlyOver.IsDescendantOf(this))
                return;

            VisualTreeItem node = FindItem(directlyOver);
            if (node != null)
                CurrentSelection = node;
        }
        private void SnoopUI_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            PreviewArea.Zoomer.DoMouseWheel(sender, e);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Find the VisualTreeItem for the specified visual.
        /// If the item is not found and is not part of the Snoop UI,
        /// the tree will be adjusted to include the window the item is in.
        /// </summary>
        private VisualTreeItem FindItem(object target)
        {
            VisualTreeItem node = _rootVisualTreeItem.FindNode(target);
            Visual rootVisual = _rootVisualTreeItem.MainVisual;
            if (node == null)
            {
                Visual visual = target as Visual;
                if (visual != null && rootVisual != null)
                {
                    // If target is a part of the SnoopUI, let's get out of here.
                    if (visual.IsDescendantOf(this))
                    {
                        return null;
                    }

                    // If not in the root tree, make the root be the tree the visual is in.
                    if (!visual.IsDescendantOf(rootVisual))
                    {
                        var presentationSource = PresentationSource.FromVisual(visual);
                        if (presentationSource == null)
                        {
                            return null; // Something went wrong. At least we will not crash with null ref here.
                        }

                        Root = new VisualItem(presentationSource.RootVisual, null);
                    }
                }

                _rootVisualTreeItem.Reload();

                node = _rootVisualTreeItem.FindNode(target);

                SetFilter(_filter);
            }
            return node;
        }

        private static string TryGetMainWindowTitle()
        {
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                return Application.Current.MainWindow.Title;
            }
            return string.Empty;
        }

        private void HandleTreeSelectedItemChanged(object sender, EventArgs e)
        {
            VisualTreeItem item = Tree.SelectedItem as VisualTreeItem;
            if (item != null)
                CurrentSelection = item;
        }

        private void ProcessFilter()
        {
            if (SnoopModes.MultipleDispatcherMode && !Dispatcher.CheckAccess())
            {
                Action action = ProcessFilter;
                Dispatcher.BeginInvoke(action);
                return;
            }

            _visualTreeItems.Clear();

            // cplotts todo: we've got to come up with a better way to do this.
            if (_filter == "Clear any filter applied to the tree view")
            {
                SetFilter(string.Empty);
            }
            else if (_filter == "Show only visuals with binding errors")
            {
                FilterBindings(_rootVisualTreeItem);
            }
            else if (_filter.Length == 0)
            {
                _visualTreeItems.Add(_rootVisualTreeItem);
            }
            else
            {
                FilterTree(_rootVisualTreeItem, _filter.ToLower());
            }
        }

        private void FilterTree(VisualTreeItem node, string localFilter)
        {
            foreach (VisualTreeItem child in node.Children)
            {
                if (child.Filter(localFilter))
                    _visualTreeItems.Add(child);
                else
                    FilterTree(child, localFilter);
            }
        }

        private void FilterBindings(VisualTreeItem node)
        {
            foreach (VisualTreeItem child in node.Children)
            {
                if (child.HasBindingError)
                    _visualTreeItems.Add(child);
                else
                    FilterBindings(child);
            }
        }

        private object FindRoot()
        {
            object rootVisual = null;

            if (SnoopModes.MultipleDispatcherMode)
            {
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    var visual = presentationSource.RootVisual as UIElement;
                    if (visual != null && visual.Dispatcher.CheckAccess())
                    {
                        rootVisual = presentationSource.RootVisual;
                        break;
                    }
                }
            }
            else if (Application.Current != null)
            {
                rootVisual = Application.Current;
            }
            else
            {
                // if we don't have a current application,
                // then we must be in an interop scenario (win32 -> wpf or windows forms -> wpf).


                // in this case, let's iterate over PresentationSource.CurrentSources,
                // and use the first non-null, visible RootVisual we find as root to inspect.
                foreach (PresentationSource presentationSource in PresentationSource.CurrentSources)
                {
                    var visual = presentationSource.RootVisual as UIElement;
                    if (visual != null && visual.Visibility == Visibility.Visible)
                    {
                        rootVisual = presentationSource.RootVisual;
                        break;
                    }
                }

                if (System.Windows.Forms.Application.OpenForms.Count > 0)
                {
                    // this is windows forms -> wpf interop

                    // call ElementHost.EnableModelessKeyboardInterop to allow the Snoop UI window
                    // to receive keyboard messages. if you don't call this method,
                    // you will be unable to edit properties in the property grid for windows forms interop.
                    ElementHost.EnableModelessKeyboardInterop(this);
                }
            }

            return rootVisual;
        }

        private void Load(object localRoot)
        {
            _root = localRoot;

            _visualTreeItems.Clear();

            Root = VisualTreeItem.Construct(localRoot, null);
            CurrentSelection = _rootVisualTreeItem;

            SetFilter(_filter);

            OnPropertyChanged("Root");
        }

        #endregion

        #region Private Fields
        private bool _fromTextBox = true;
        private readonly DispatcherTimer _filterTimer;

        private readonly ObservableCollection<VisualTreeItem> _visualTreeItems = new ObservableCollection<VisualTreeItem>();

        private string _eventFilter = string.Empty;

        private readonly DelayedCall _filterCall;

        private VisualTreeItem _reducedDepthRoot;

        private IInputElement _currentFocus;
        private IInputElement _previousFocus;

        /// <summary>
        /// Indicates whether CurrentFocus should retur previously focused element.
        /// This fixes problem where Snoop steals the focus from snooped app.
        /// </summary>
        private bool _returnPreviousFocus;

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
