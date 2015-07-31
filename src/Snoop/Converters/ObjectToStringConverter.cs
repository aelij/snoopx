// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Snoop.Converters
{
	public class ObjectToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return "{null}";

			var fe = value as FrameworkElement;
			if (!string.IsNullOrEmpty(fe?.Name))
				return fe.Name + " (" + value.GetType().Name + ")";

			var command = value as RoutedCommand;
			if (!string.IsNullOrEmpty(command?.Name))
				return command.Name + " (" + command.GetType().Name + ")";

			return "(" + value.GetType().Name + ")";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
