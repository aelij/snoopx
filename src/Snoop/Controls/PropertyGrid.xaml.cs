// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Snoop.Annotations;
using Snoop.Infrastructure;
using Snoop.Utilities;

namespace Snoop.Controls
{
    public partial class PropertyGrid : INotifyPropertyChanged
    {
        public static readonly RoutedCommand ShowBindingErrorsCommand = new RoutedCommand();
        public static readonly RoutedCommand ClearCommand = new RoutedCommand();
        public static readonly RoutedCommand SortCommand = new RoutedCommand();
        public static readonly RoutedCommand SnipXamlCommand = new RoutedCommand("SnipXaml", typeof(PropertyGrid));
        public static readonly RoutedCommand PopTargetCommand = new RoutedCommand("PopTarget", typeof(PropertyGrid));
        public static readonly RoutedCommand DelveCommand = new RoutedCommand();
        public static readonly RoutedCommand DelveBindingCommand = new RoutedCommand();
        public static readonly RoutedCommand DelveBindingExpressionCommand = new RoutedCommand();

        private readonly DelayedCall _filterCall;
        private readonly ObservableCollection<PropertyInformation> _allProperties;

        private bool _unloaded;
        private ListSortDirection _direction;
        private PropertyFilter _filter;

        public PropertyGrid()
        {
            _direction = ListSortDirection.Ascending;
            _allProperties = new ObservableCollection<PropertyInformation>();
            Properties = new ObservableCollection<PropertyInformation>();
            _filterCall = new DelayedCall(ProcessFilter, DispatcherPriority.Background);

            InitializeComponent();

            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;

            CommandBindings.Add(new CommandBinding(ShowBindingErrorsCommand, HandleShowBindingErrors, CanShowBindingErrors));
            CommandBindings.Add(new CommandBinding(ClearCommand, HandleClear, CanClear));
            CommandBindings.Add(new CommandBinding(SortCommand, HandleSort));
        }

        public bool NameValueOnly
        {
            get
            {
                return _nameValueOnly;
            }
            set
            {
                _nameValueOnly = value;
                var gridView = ListView?.View as GridView;
                if (_nameValueOnly && gridView != null && gridView.Columns.Count != 2)
                {
                    gridView.Columns.RemoveAt(0);
                    while (gridView.Columns.Count > 2)
                    {
                        gridView.Columns.RemoveAt(2);
                    }
                }
            }
        }
        private bool _nameValueOnly;

        public ObservableCollection<PropertyInformation> Properties { get; }

        public object Target
        {
            get { return GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register
            (
                "Target",
                typeof(object),
                typeof(PropertyGrid),
                new PropertyMetadata(HandleTargetChanged)
            );
        private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var propertyGrid = (PropertyGrid)d;
            propertyGrid.ChangeTarget(e.NewValue);
        }

        private void ChangeTarget(object newTarget)
        {
            if (_target != newTarget)
            {
                _target = newTarget;

                foreach (var property in Properties)
                {
                    property.Teardown();
                }
                RefreshPropertyGrid();

                OnPropertyChanged(nameof(Type));
            }
        }

        public PropertyInformation Selection
        {
            get { return _selection; }
            set
            {
                _selection = value;
                OnPropertyChanged();
            }
        }
        private PropertyInformation _selection;

        public Type Type => _target?.GetType();

        protected virtual void OnFilterChanged()
        {
            _filterCall.Enqueue();
        }

        /// <summary>
        /// Delayed loading of the property inspector to avoid creating the entire list of property
        /// editors immediately after selection. Keeps that app running smooth.
        /// </summary>
        /// <returns></returns>
        private async void ProcessIncrementalPropertyAddAsync()
        {
            const int batchAmount = 10;

            var propertiesToAdd = PropertyInformation.GetProperties(_target);

            await Dispatcher.Yield(DispatcherPriority.Background);

            var visiblePropertyCount = 0;

            foreach (var property in propertiesToAdd)
            {
                // iterate over the PropertyInfo objects,
                // setting the property grid's filter on each object,
                // and adding those properties to the observable collection of propertiesToSort (this.properties)
                property.Filter = Filter;

                if (property.IsVisible)
                {
                    Properties.Add(property);
                }
                _allProperties.Add(property);

                // checking whether a property is visible ... actually runs the property filtering code
                if (property.IsVisible)
                {
                    property.Index = visiblePropertyCount++;
                }

                if ((visiblePropertyCount % batchAmount) == 0)
                {
                    await Dispatcher.Yield(DispatcherPriority.Background);
                }
            }
        }

        public PropertyFilter Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                OnPropertyChanged();
                OnFilterChanged();
            }
        }

        private static void HandleShowBindingErrors(object sender, ExecutedRoutedEventArgs eventArgs)
        {
            var propertyInformation = (PropertyInformation)eventArgs.Parameter;
            var window = new Window
            {
                Content = new TextBox
                {
                    IsReadOnly = true,
                    Text = propertyInformation.BindingError,
                    TextWrapping = TextWrapping.Wrap
                },
                Width = 400,
                Height = 300,
                Title = "Binding Errors for " + propertyInformation.DisplayName
            };
            SnoopPartsRegistry.AddSnoopVisualTreeRoot(window);
            window.Closing +=
                (s, e) =>
                {
                    var w = (Window)s;
                    SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(w);
                };
            window.Show();
        }

        private static void CanShowBindingErrors(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && !string.IsNullOrEmpty(((PropertyInformation)e.Parameter).BindingError))
                e.CanExecute = true;
            e.Handled = true;
        }

        private static void CanClear(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).IsLocallySet)
                e.CanExecute = true;
            e.Handled = true;
        }

        private static void HandleClear(object sender, ExecutedRoutedEventArgs e)
        {
            ((PropertyInformation)e.Parameter).Clear();
        }

        private static ListSortDirection GetNewSortDirection(GridViewColumnHeader columnHeader)
        {
            if (!(columnHeader.Tag is ListSortDirection))
            {
                columnHeader.Tag = ListSortDirection.Descending;
                return ListSortDirection.Descending;
            }

            var direction = (ListSortDirection)columnHeader.Tag;
            return (ListSortDirection)(columnHeader.Tag = (ListSortDirection)(((int)direction + 1) % 2));
        }

        private void HandleSort(object sender, ExecutedRoutedEventArgs args)
        {
            var headerClicked = (GridViewColumnHeader)args.OriginalSource;

            _direction = GetNewSortDirection(headerClicked);

            switch (((TextBlock)headerClicked.Column.Header).Text)
            {
                case "Name":
                    Sort(CompareNames, _direction);
                    break;
                case "Value":
                    Sort(CompareValues, _direction);
                    break;
                case "ValueSource":
                    Sort(CompareValueSources, _direction);
                    break;
            }
        }

        private void ProcessFilter()
        {
            foreach (var property in _allProperties)
            {
                if (property.IsVisible)
                {
                    if (!Properties.Contains(property))
                    {
                        InsertInPropertyOrder(property);
                    }
                }
                else
                {
                    if (Properties.Contains(property))
                    {
                        Properties.Remove(property);
                    }
                }
            }

            SetIndexesOfProperties();
        }

        private void InsertInPropertyOrder(PropertyInformation property)
        {
            if (Properties.Count == 0)
            {
                Properties.Add(property);
                return;
            }

            if (PropertiesAreInOrder(property, Properties[0]))
            {
                Properties.Insert(0, property);
                return;
            }

            for (var i = 0; i < Properties.Count - 1; i++)
            {
                if (PropertiesAreInOrder(Properties[i], property) && PropertiesAreInOrder(property, Properties[i + 1]))
                {
                    Properties.Insert(i + 1, property);
                    return;
                }
            }

            Properties.Add(property);
        }

        private bool PropertiesAreInOrder(PropertyInformation first, PropertyInformation last)
        {
            if (_direction == ListSortDirection.Ascending)
            {
                return first.CompareTo(last) <= 0;
            }
            return last.CompareTo(first) <= 0;
        }

        private void SetIndexesOfProperties()
        {
            for (var i = 0; i < Properties.Count; i++)
            {
                Properties[i].Index = i;
            }
        }

        private void HandleLoaded(object sender, EventArgs e)
        {
            if (_unloaded)
            {
                RefreshPropertyGrid();
                _unloaded = false;
            }
        }
        private void HandleUnloaded(object sender, EventArgs e)
        {
            foreach (var property in Properties)
            {
                property.Teardown();
            }

            _unloaded = true;
        }

        private void HandleNameClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var property = (PropertyInformation)((FrameworkElement)sender).DataContext;

                object newTarget = null;

                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    newTarget = property.Binding;
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                    newTarget = property.BindingExpression;
                else if (Keyboard.Modifiers == ModifierKeys.None)
                    newTarget = property.Value;

                if (newTarget != null)
                {
                    DelveCommand.Execute(property, this);
                }
            }
        }

        private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction)
        {
            Sort(comparator, direction, Properties);
            Sort(comparator, direction, _allProperties);
        }

        private static void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction, ObservableCollection<PropertyInformation> propertiesToSort)
        {
            var sorter = new List<PropertyInformation>(propertiesToSort);
            sorter.Sort(comparator);

            if (direction == ListSortDirection.Descending)
                sorter.Reverse();

            propertiesToSort.Clear();
            foreach (var property in sorter)
                propertiesToSort.Add(property);
        }

        private void RefreshPropertyGrid()
        {
            _allProperties.Clear();
            Properties.Clear();

            ProcessIncrementalPropertyAddAsync();
        }

        private object _target;

        private static int CompareNames(PropertyInformation one, PropertyInformation two)
        {
            // use the PropertyInformation CompareTo method, instead of the string.Compare method
            // so that collections get sorted correctly.
            return one.CompareTo(two);
        }

        private static int CompareValues(PropertyInformation one, PropertyInformation two)
        {
            return string.CompareOrdinal(one.StringValue, two.StringValue);
        }

        private static int CompareValueSources(PropertyInformation one, PropertyInformation two)
        {
            return string.CompareOrdinal(one.ValueSource.BaseValueSource.ToString(), two.ValueSource.BaseValueSource.ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
