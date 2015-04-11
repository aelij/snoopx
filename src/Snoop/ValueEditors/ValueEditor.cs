// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Snoop
{
	public class ValueEditor : ContentControl
	{
		public bool IsSelected
		{
			get { return (bool)GetValue(IsSelectedProperty); }
			set { SetValue(IsSelectedProperty, value); }
		}
		public static DependencyProperty IsSelectedProperty =
			DependencyProperty.Register
			(
				"IsSelected",
				typeof(bool),
				typeof(ValueEditor)
			);

		public object Value
		{
			get { return GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}
		public static DependencyProperty ValueProperty =
			DependencyProperty.Register
			(
				"Value",
				typeof(object),
				typeof(ValueEditor),
				new PropertyMetadata(HandleValueChanged)
			);
		private static void HandleValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((ValueEditor)sender).OnValueChanged(e.NewValue);
		}
		protected virtual void OnValueChanged(object newValue)
		{
		}

		public object DescriptiveValue
		{
			get { return (bool)GetValue(DescriptiveValueProperty); }
			set { SetValue(DescriptiveValueProperty, value); }
		}
		public static DependencyProperty DescriptiveValueProperty =
			DependencyProperty.Register
			(
				"DescriptiveValue",
				typeof(object),
				typeof(ValueEditor)
			);

		public Type PropertyType
		{
			get { return (Type)GetValue(PropertyTypeProperty); }
			set { SetValue(PropertyTypeProperty, value); }
		}
		public static DependencyProperty PropertyTypeProperty =
			DependencyProperty.Register
			(
				"PropertyType",
				typeof(object),
				typeof(ValueEditor),
				new PropertyMetadata(HandleTypeChanged)
			);
		private static void HandleTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			((ValueEditor)sender).OnTypeChanged();
		}
		protected virtual void OnTypeChanged()
		{
		}

		public bool IsEditable
		{
			get { return (bool)GetValue(IsEditableProperty); }
			set { SetValue(IsEditableProperty, value); }
		}
		public static DependencyProperty IsEditableProperty =
			DependencyProperty.Register
			(
				"IsEditable",
				typeof(bool),
				typeof(ValueEditor)
			);

		public PropertyInformation PropertyInfo
		{
			[DebuggerStepThrough]
			get { return (PropertyInformation)GetValue(PropertyInfoProperty); }
			set { SetValue(PropertyInfoProperty, value); }
		}
		public static readonly DependencyProperty PropertyInfoProperty =
			DependencyProperty.Register("PropertyInfo", typeof(PropertyInformation),
				typeof(ValueEditor), new FrameworkPropertyMetadata());
	}
}
