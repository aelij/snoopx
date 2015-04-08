using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public class FiltersViewModel : INotifyPropertyChanged
	{
		private readonly List<SnoopMultipleFilter> _multipleFilters = new List<SnoopMultipleFilter>();
		private bool _isDirty;

		public void ResetDirtyFlag()
		{
			_isDirty = false;
			foreach (var filter in _filters)
			{
				filter.ResetDirtyFlag();
			}
		}

		public bool IsDirty
		{
			get
			{
			    return _isDirty || _filters.Any(filter => filter.IsDirty);
			}
		}

		public FiltersViewModel()
		{
			_filters.Add(new SnoopSingleFilter());
			FilterStatus = _isSet ? "Filter is ON" : "Filter is OFF";
		}

		public FiltersViewModel(IList<SnoopSingleFilter> singleFilters)
		{
			InitializeFilters(singleFilters);
		}

		public void InitializeFilters(IList<SnoopSingleFilter> singleFilters)
		{
			_filters.Clear();

			if (singleFilters == null)
			{
				_filters.Add(new SnoopSingleFilter());
				IsSet = false;
				return;
			}

			foreach (var filter in singleFilters)
				_filters.Add(filter);

			var groupings = (from x in singleFilters where x.IsGrouped select x).GroupBy(x => x.GroupId);
			foreach (var grouping in groupings)
			{
				var multipleFilter = new SnoopMultipleFilter();
				var groupedFilters = grouping.ToArray();
				if (groupedFilters.Length == 0)
					continue;

				multipleFilter.AddRange(groupedFilters, groupedFilters[0].GroupId);
				_multipleFilters.Add(multipleFilter);
			}

			SetIsSet();
		}

		internal void SetIsSet()
		{
		    IsSet = _filters != null &&
		            (_filters.Count != 1 || !(_filters[0] is SnoopSingleFilter) ||
		             !string.IsNullOrEmpty(((SnoopSingleFilter) _filters[0]).Text));
		}

	    public void ClearFilters()
		{
			_multipleFilters.Clear();
			_filters.Clear();
			_filters.Add(new SnoopSingleFilter());
			IsSet = false;
		}

		public bool FilterMatches(string str)
		{
		    return Filters.Where(filter => !filter.IsGrouped).Any(filter => filter.FilterMatches(str)) ||
		           _multipleFilters.Any(multipleFilter => multipleFilter.FilterMatches(str));
		}

	    public void GroupFilters(ICollection<SnoopFilter> filtersToGroup)
		{
			SnoopMultipleFilter multipleFilter = new SnoopMultipleFilter();
			multipleFilter.AddRange(filtersToGroup, (_multipleFilters.Count + 1).ToString());

			_multipleFilters.Add(multipleFilter);
		}

		public void AddFilter(SnoopFilter filter)
		{
			_isDirty = true;
			_filters.Add(filter);
		}

		public void RemoveFilter(SnoopFilter filter)
		{
			_isDirty = true;
			var singleFilter = filter as SnoopSingleFilter;
			if (singleFilter != null)
			{
				//foreach (var multipeFilter in this.multipleFilters)
				int index = 0;
				while (index < _multipleFilters.Count)
				{
					var multipeFilter = _multipleFilters[index];
					if (multipeFilter.ContainsFilter(singleFilter))
						multipeFilter.RemoveFilter(singleFilter);

					if (!multipeFilter.IsValidMultipleFilter)
						_multipleFilters.RemoveAt(index);
					else
						index++;
				}
			}
			_filters.Remove(filter);
		}

		public void ClearFilterGroups()
		{
			foreach (var filterGroup in _multipleFilters)
			{
				filterGroup.ClearFilters();
			}
			_multipleFilters.Clear();
		}

		private bool _isSet;
		private string _filterStatus;
		public bool IsSet
		{
			get
			{
				return _isSet;
			}
			set
			{
				_isSet = value;
				RaisePropertyChanged("IsSet");
				FilterStatus = _isSet ? "Filter is ON" : "Filter is OFF";
			}
		}

		public string FilterStatus
		{
			get
			{
				return _filterStatus;
			}
			set
			{
				_filterStatus = value;
				RaisePropertyChanged("FilterStatus");
			}
		}

		private readonly ObservableCollection<SnoopFilter> _filters = new ObservableCollection<SnoopFilter>();
		public IEnumerable<SnoopFilter> Filters
		{
			get
			{
				return _filters;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void RaisePropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
