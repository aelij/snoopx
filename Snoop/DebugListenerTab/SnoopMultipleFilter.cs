using System;
using System.Collections.Generic;
using System.Linq;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public class SnoopMultipleFilter : SnoopFilter
	{
		private readonly List<SnoopFilter> _singleFilters = new List<SnoopFilter>();

		public override bool FilterMatches(string debugLine)
		{
		    return _singleFilters.All(filter => filter.FilterMatches(debugLine));
		}

	    public override bool SupportsGrouping
		{
			get { return false; }
		}

		public override string GroupId
		{
			get { return _singleFilters.Count == 0 ? string.Empty : _singleFilters[0].GroupId; }
		    set { throw new NotSupportedException(); }
		}

		public bool IsValidMultipleFilter
		{
			get
			{
				return _singleFilters.Count > 0;
			}
		}

		public void AddFilter(SnoopFilter singleFilter)
		{
			if (!singleFilter.SupportsGrouping)
				throw new NotSupportedException("The filter is not grouped");
			_singleFilters.Add(singleFilter);
		}

		public void RemoveFilter(SnoopFilter singleFilter)
		{
			singleFilter.IsGrouped = false;
			_singleFilters.Remove(singleFilter);
		}

		public void AddRange(ICollection<SnoopFilter> filters, string groupId)
		{
			foreach (var filter in filters)
			{
				if (!filter.SupportsGrouping)
					throw new NotSupportedException("The filter is not grouped");

				filter.IsGrouped = true;
				filter.GroupId = groupId;
			}
			_singleFilters.AddRange(filters);
		}

		public void ClearFilters()
		{
			foreach (var filter in _singleFilters)
				filter.IsGrouped = false;
			_singleFilters.Clear();
		}

		public bool ContainsFilter(SnoopSingleFilter filter)
		{
			return _singleFilters.Contains(filter);
		}
	}
}
