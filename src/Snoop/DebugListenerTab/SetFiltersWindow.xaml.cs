using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Snoop.Infrastructure;
using Snoop.Properties;

namespace Snoop.DebugListenerTab
{
	/// <summary>
	/// Interaction logic for SetFiltersWindow.xaml
	/// </summary>
	public partial class SetFiltersWindow
	{
		public SetFiltersWindow(FiltersViewModel viewModel)
		{
			DataContext = viewModel;
			viewModel.ResetDirtyFlag();

			InitializeComponent();

			_initialFilters = MakeDeepCopyOfFilters(ViewModel.Filters);

			Loaded += SetFiltersWindow_Loaded;
			Closed += SetFiltersWindow_Closed;
		}
	
		internal FiltersViewModel ViewModel
		{
			get
			{
				return DataContext as FiltersViewModel;
			}
		}

		private void SetFiltersWindow_Loaded(object sender, RoutedEventArgs e)
		{
			SnoopPartsRegistry.AddSnoopVisualTreeRoot(this);
		}
		private void SetFiltersWindow_Closed(object sender, EventArgs e)
		{
			if (_setFilterClicked || !ViewModel.IsDirty)
				return;

			var saveChanges = MessageBox.Show("Save changes?", "Changes", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
			if (saveChanges)
			{
				ViewModel.SetIsSet();
				SaveFiltersToSettings();
				return;
			}

			ViewModel.InitializeFilters(_initialFilters);

			SnoopPartsRegistry.RemoveSnoopVisualTreeRoot(this);
		}

		private void buttonAddFilter_Click(object sender, RoutedEventArgs e)
		{
			//ViewModel.Filters.Add(new SnoopSingleFilter());
			ViewModel.AddFilter(new SnoopSingleFilter());
			//this.listBoxFilters.ScrollIntoView(this.listBoxFilters.ItemContainerGenerator.ContainerFromIndex(this.listBoxFilters.Items.Count - 1));

		}
		private void buttonRemoveFilter_Click(object sender, RoutedEventArgs e)
		{
			FrameworkElement frameworkElement = sender as FrameworkElement;
			if (frameworkElement == null)
				return;

			SnoopFilter filter = frameworkElement.DataContext as SnoopFilter;
			if (filter == null)
				return;

			ViewModel.RemoveFilter(filter);
		}
		private void buttonSetFilter_Click(object sender, RoutedEventArgs e)
		{
			SaveFiltersToSettings();

			//this.ViewModel.IsSet = true;
			ViewModel.SetIsSet();
			_setFilterClicked = true;
			Close();
		}

		private void textBlockFilter_Loaded(object sender, RoutedEventArgs e)
		{
			var textBox = sender as TextBox;
			if (textBox != null)
			{
				textBox.Focus();
				ListBoxFilters.ScrollIntoView(textBox);
			}
		}

		private void menuItemGroupFilters_Click(object sender, RoutedEventArgs e)
		{
			var filtersToGroup = ListBoxFilters.SelectedItems.OfType<SnoopFilter>()
                .Where(filter => filter.SupportsGrouping).ToList();
		    ViewModel.GroupFilters(filtersToGroup);
		}
		private void menuItemClearFilterGroups_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.ClearFilterGroups();
		}
		private void menuItemSetInverse_Click(object sender, RoutedEventArgs e)
		{
			foreach (SnoopFilter filter in ListBoxFilters.SelectedItems)
			{
				if (filter == null)
					continue;

				filter.IsInverse = !filter.IsInverse;
			}
		}

		private void SaveFiltersToSettings()
		{
		    Settings.Default.SnoopDebugFilters = ViewModel.Filters.OfType<SnoopSingleFilter>().ToArray();
		}

		private List<SnoopSingleFilter> MakeDeepCopyOfFilters(IEnumerable<SnoopFilter> filters)
		{
		    return filters.OfType<SnoopSingleFilter>()
                .Select(singleFilter => singleFilter.Clone()).ToList();
		}

		private readonly List<SnoopSingleFilter> _initialFilters;
		private bool _setFilterClicked;
	}
}
