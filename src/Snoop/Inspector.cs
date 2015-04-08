// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Windows.Controls;

namespace Snoop
{
	public class Inspector : Grid
	{
		public PropertyFilter Filter
		{
			get { return _filter; }
			set
			{
				_filter = value;
				OnFilterChanged();
			}
		}
		private PropertyFilter _filter;

		protected virtual void OnFilterChanged() {}
	}
}
