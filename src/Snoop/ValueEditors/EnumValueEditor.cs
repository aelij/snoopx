// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace Snoop
{
	public class EnumValueEditor : ValueEditor
	{
		public EnumValueEditor()
		{
			_valuesView = (ListCollectionView)CollectionViewSource.GetDefaultView(_values);
			_valuesView.CurrentChanged += HandleSelectionChanged;
		}


		public IList<object> Values
		{
			get { return _values; }
		}
		private readonly List<object> _values = new List<object>();


		protected override void OnTypeChanged()
		{
			base.OnTypeChanged();

			_isValid = false;

			_values.Clear();

			Type propertyType = PropertyType;
			if (propertyType != null)
			{
				Array values = Enum.GetValues(propertyType);
				foreach(object value in values)
				{
					_values.Add(value);

					if (Value != null && Value.Equals(value))
						_valuesView.MoveCurrentTo(value);
				}
			}

			_isValid = true;
		}

		protected override void OnValueChanged(object newValue)
		{
			base.OnValueChanged(newValue);

			_valuesView.MoveCurrentTo(newValue);

			// sneaky trick here.  only if both are non-null is this a change
			// caused by the user.  If so, set the bool to track it.
			if ( PropertyInfo != null && newValue != null )
			{
				PropertyInfo.IsValueChangedByUser = true;
			}
		}


		private void HandleSelectionChanged(object sender, EventArgs e)
		{
			if (_isValid && Value != null)
			{
				if (!Value.Equals(_valuesView.CurrentItem))
					Value = _valuesView.CurrentItem;
			}
		}


		private bool _isValid;
		private readonly ListCollectionView _valuesView;
	}
}
