using System;
using System.Text.RegularExpressions;

namespace Snoop.DebugListenerTab
{
	[Serializable]
	public class SnoopSingleFilter : SnoopFilter, ICloneable
	{
		private string _text;

		public SnoopSingleFilter()
		{
			Text = string.Empty;
		}

		public FilterType FilterType { get; set; }

		public string Text
		{
			get { return _text; }
			set
			{
				_text = value;
				RaisePropertyChanged();
			}
		}

		public override bool FilterMatches(string debugLine)
		{
			debugLine = debugLine.ToLower();
			var text = Text.ToLower();
			bool filterMatches = false;
			switch (FilterType)
			{
				case FilterType.Contains:
					filterMatches = debugLine.Contains(text);
					break;
				case FilterType.StartsWith:
					filterMatches = debugLine.StartsWith(text);
					break;
				case FilterType.EndsWith:
					filterMatches = debugLine.EndsWith(text);
					break;
				case FilterType.RegularExpression:
					filterMatches = TryMatch(debugLine, text);
					break;
			}

			if (IsInverse)
			{
				filterMatches = !filterMatches;
			}

			return filterMatches;
		}

		private static bool TryMatch(string input, string pattern)
		{
			try
			{
			    return Regex.IsMatch(input, pattern);
			}
			catch (Exception)
			{
				return false;
			}
		}

	    object ICloneable.Clone()
	    {
	        return Clone();
	    }

	    public SnoopSingleFilter Clone()
	    {
	        var newFilter = new SnoopSingleFilter
	        {
	            GroupId = GroupId,
	            IsGrouped = IsGrouped,
	            Text = _text,
	            FilterType = FilterType,
	            IsInverse = IsInverse
	        };
	        return newFilter;
	    }
	}
}
