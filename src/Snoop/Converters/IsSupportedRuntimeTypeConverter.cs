﻿// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Snoop.Converters
{
	/// <summary>
	/// Converter to tell us if the given value is supported for MouseWheel editing.
	/// Used to use app.config to drive this, but now it's just a hardcoded list
	/// </summary>
	public class IsSupportedRuntimeTypeConverter : IValueConverter
	{
		public static readonly IsSupportedRuntimeTypeConverter Default = new IsSupportedRuntimeTypeConverter();

		private static readonly string[] _excludeTypeNames = { "LinearGradientBrush", "RadialGradientBrush", "TileBrush" };

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
		    // value (object)	the runtime value - compare against known incompatible types (use list from config)
			// return (boolean)	whether the type is supported or not

		    return value != null && _excludeTypeNames.All(excludeTypeName => value.GetType().Name != excludeTypeName);
		}

	    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
