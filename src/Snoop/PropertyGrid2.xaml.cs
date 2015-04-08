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

namespace Snoop
{
    public partial class PropertyGrid2 : INotifyPropertyChanged
    {
        public static readonly RoutedCommand ShowBindingErrorsCommand = new RoutedCommand();
        public static readonly RoutedCommand ClearCommand = new RoutedCommand();
        public static readonly RoutedCommand SortCommand = new RoutedCommand();


        public PropertyGrid2()
        {
            _processIncrementalCall = new DelayedCall(ProcessIncrementalPropertyAdd, DispatcherPriority.Background);
            _filterCall = new DelayedCall(ProcessFilter, DispatcherPriority.Background);

            InitializeComponent();

            Loaded += HandleLoaded;
            Unloaded += HandleUnloaded;

            CommandBindings.Add(new CommandBinding(ShowBindingErrorsCommand, HandleShowBindingErrors, CanShowBindingErrors));
            CommandBindings.Add(new CommandBinding(ClearCommand, HandleClear, CanClear));
            CommandBindings.Add(new CommandBinding(SortCommand, HandleSort));


            _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.3) };
            _filterTimer.Tick += (s, e) =>
            {
                _filterCall.Enqueue();
                _filterTimer.Stop();
            };
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
                GridView gridView = ListView != null && ListView.View != null ? ListView.View as GridView : null;
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

        public ObservableCollection<PropertyInformation> Properties
        {
            get { return _properties; }
        }
        private readonly ObservableCollection<PropertyInformation> _properties = new ObservableCollection<PropertyInformation>();
        private readonly ObservableCollection<PropertyInformation> _allProperties = new ObservableCollection<PropertyInformation>();

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
                typeof(PropertyGrid2),
                new PropertyMetadata(HandleTargetChanged)
            );
        private static void HandleTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyGrid2 propertyGrid = (PropertyGrid2)d;
            propertyGrid.ChangeTarget(e.NewValue);
        }

        private void ChangeTarget(object newTarget)
        {
            if (_target != newTarget)
            {
                _target = newTarget;

                foreach (PropertyInformation property in _properties)
                {
                    property.Teardown();
                }
                RefreshPropertyGrid();

                OnPropertyChanged("Type");
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

        public Type Type
        {
            get
            {
                if (_target != null)
                    return _target.GetType();
                return null;
            }
        }


        protected override void OnFilterChanged()
        {
            base.OnFilterChanged();

            _filterTimer.Stop();
            _filterTimer.Start();
        }


        /// <summary>
        /// Delayed loading of the property inspector to avoid creating the entire list of property
        /// editors immediately after selection. Keeps that app running smooth.
        /// </summary>
        /// <returns></returns>
        private void ProcessIncrementalPropertyAdd()
        {
            int numberToAdd = 10;

            if (_propertiesToAdd == null)
            {
                _propertiesToAdd = PropertyInformation.GetProperties(_target).GetEnumerator();

                numberToAdd = 0;
            }
            int i = 0;
            for (; i < numberToAdd && _propertiesToAdd.MoveNext(); ++i)
            {
                // iterate over the PropertyInfo objects,
                // setting the property grid's filter on each object,
                // and adding those properties to the observable collection of propertiesToSort (this.properties)
                PropertyInformation property = _propertiesToAdd.Current;
                property.Filter = Filter;

                if (property.IsVisible)
                {
                    _properties.Add(property);
                }
                _allProperties.Add(property);

                // checking whether a property is visible ... actually runs the property filtering code
                if (property.IsVisible)
                    property.Index = _visiblePropertyCount++;
            }

            if (i == numberToAdd)
                _processIncrementalCall.Enqueue();
            else
                _propertiesToAdd = null;
        }

        private void HandleShowBindingErrors(object sender, ExecutedRoutedEventArgs eventArgs)
        {
            PropertyInformation propertyInformation = (PropertyInformation)eventArgs.Parameter;
            Window window = new Window
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
                    Window w = (Window)s;
                    SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(w);
                };
            window.Show();
        }
        private void CanShowBindingErrors(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && !string.IsNullOrEmpty(((PropertyInformation)e.Parameter).BindingError))
                e.CanExecute = true;
            e.Handled = true;
        }

        private void CanClear(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null && ((PropertyInformation)e.Parameter).IsLocallySet)
                e.CanExecute = true;
            e.Handled = true;
        }
        private void HandleClear(object sender, ExecutedRoutedEventArgs e)
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

            ListSortDirection direction = (ListSortDirection)columnHeader.Tag;
            return (ListSortDirection)(columnHeader.Tag = (ListSortDirection)(((int)direction + 1) % 2));
        }


        private void HandleSort(object sender, ExecutedRoutedEventArgs args)
        {
            GridViewColumnHeader headerClicked = (GridViewColumnHeader)args.OriginalSource;

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
                    if (!_properties.Contains(property))
                    {
                        InsertInPropertOrder(property);
                    }
                }
                else
                {
                    if (_properties.Contains(property))
                    {
                        _properties.Remove(property);
                    }
                }
            }

            SetIndexesOfProperties();
        }

        private void InsertInPropertOrder(PropertyInformation property)
        {
            if (_properties.Count == 0)
            {
                _properties.Add(property);
                return;
            }

            if (PropertiesAreInOrder(property, _properties[0]))
            {
                _properties.Insert(0, property);
                return;
            }

            for (int i = 0; i < _properties.Count - 1; i++)
            {
                if (PropertiesAreInOrder(_properties[i], property) && PropertiesAreInOrder(property, _properties[i + 1]))
                {
                    _properties.Insert(i + 1, property);
                    return;
                }
            }

            _properties.Add(property);
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
            for (int i = 0; i < _properties.Count; i++)
            {
                _properties[i].Index = i;
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
            foreach (PropertyInformation property in _properties)
                property.Teardown();

            _unloaded = true;
        }

        private void HandleNameClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                PropertyInformation property = (PropertyInformation)((FrameworkElement)sender).DataContext;

                object newTarget = null;

                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    newTarget = property.Binding;
                else if (Keyboard.Modifiers == ModifierKeys.Control)
                    newTarget = property.BindingExpression;
                else if (Keyboard.Modifiers == ModifierKeys.None)
                    newTarget = property.Value;

                if (newTarget != null)
                {
                    PropertyInspector.DelveCommand.Execute(property, this);
                }
            }
        }

        private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction)
        {
            Sort(comparator, direction, _properties);
            Sort(comparator, direction, _allProperties);
        }

        private void Sort(Comparison<PropertyInformation> comparator, ListSortDirection direction, ObservableCollection<PropertyInformation> propertiesToSort)
        {
            List<PropertyInformation> sorter = new List<PropertyInformation>(propertiesToSort);
            sorter.Sort(comparator);

            if (direction == ListSortDirection.Descending)
                sorter.Reverse();

            propertiesToSort.Clear();
            foreach (PropertyInformation property in sorter)
                propertiesToSort.Add(property);
        }

        private void RefreshPropertyGrid()
        {
            _allProperties.Clear();
            _properties.Clear();
            _visiblePropertyCount = 0;

            _propertiesToAdd = null;
            _processIncrementalCall.Enqueue();
        }


        private object _target;

        private IEnumerator<PropertyInformation> _propertiesToAdd;
        private readonly DelayedCall _processIncrementalCall;
        private readonly DelayedCall _filterCall;
        private int _visiblePropertyCount;
        private bool _unloaded;
        private ListSortDirection _direction = ListSortDirection.Ascending;

        private readonly DispatcherTimer _filterTimer;


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
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
