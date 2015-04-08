using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Snoop.Infrastructure;
using Snoop.Properties;

namespace Snoop.DebugListenerTab
{
	/// <summary>
	/// Interaction logic for DebugListenerControl.xaml
	/// </summary>
	public partial class DebugListenerControl : IListener
	{
		private readonly FiltersViewModel _filtersViewModel;
		private readonly SnoopDebugListener _snoopDebugListener;
        private readonly StringBuilder _allText;

		public DebugListenerControl()
		{
			_filtersViewModel = new FiltersViewModel(Settings.Default.SnoopDebugFilters);
		    _snoopDebugListener = new SnoopDebugListener();
		    _allText = new StringBuilder();
		    DataContext = _filtersViewModel;

			InitializeComponent();

			_snoopDebugListener.RegisterListener(this);
		}

		private void checkBoxStartListening_Checked(object sender, RoutedEventArgs e)
		{
			Debug.Listeners.Add(_snoopDebugListener);
			PresentationTraceSources.DataBindingSource.Listeners.Add(_snoopDebugListener);
		}

		private void checkBoxStartListening_Unchecked(object sender, RoutedEventArgs e)
		{
			Debug.Listeners.Remove(SnoopDebugListener.ListenerName);
			PresentationTraceSources.DataBindingSource.Listeners.Remove(_snoopDebugListener);
		}

		public void Write(string str)
		{
            _allText.Append(str + Environment.NewLine);
			if (!_filtersViewModel.IsSet || _filtersViewModel.FilterMatches(str))
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Render, () => DoWrite(str));
			}
		}

		private void DoWrite(string str)
		{
			DebugContent.AppendText(str + Environment.NewLine);
			DebugContent.ScrollToEnd();
		}


		private void buttonClear_Click(object sender, RoutedEventArgs e)
		{
			DebugContent.Clear();
            _allText.Clear();
		}

		private void buttonClearFilters_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Are you sure you want to clear your filters?", "Clear Filters Confirmation", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes)
			{
				_filtersViewModel.ClearFilters();
				Settings.Default.SnoopDebugFilters = null;
                DebugContent.Text = _allText.ToString();
			}
		}

		private void buttonSetFilters_Click(object sender, RoutedEventArgs e)
		{
		    var setFiltersWindow = new SetFiltersWindow(_filtersViewModel)
		    {
		        Topmost = true,
		        Owner = Window.GetWindow(this),
		        WindowStartupLocation = WindowStartupLocation.CenterOwner
		    };
		    setFiltersWindow.ShowDialog();

            string[] allLines = _allText.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            DebugContent.Clear();
            foreach (string line in allLines)
            {
                if (_filtersViewModel.FilterMatches(line))
                {
                    DebugContent.AppendText(line + Environment.NewLine);
                }
            }
		}

		private void comboBoxPresentationTraceLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ComboBoxPresentationTraceLevel == null || ComboBoxPresentationTraceLevel.Items == null || ComboBoxPresentationTraceLevel.Items.Count <= ComboBoxPresentationTraceLevel.SelectedIndex || ComboBoxPresentationTraceLevel.SelectedIndex < 0)
				return;

			var selectedComboBoxItem = ComboBoxPresentationTraceLevel.Items[ComboBoxPresentationTraceLevel.SelectedIndex] as ComboBoxItem;
			if (selectedComboBoxItem == null || selectedComboBoxItem.Tag == null)
				return;


			var sourceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), selectedComboBoxItem.Tag.ToString());
			PresentationTraceSources.DataBindingSource.Switch.Level = sourceLevel;
		}
	}
}
