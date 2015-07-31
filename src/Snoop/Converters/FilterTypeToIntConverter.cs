using System;
using System.Globalization;
using System.Windows.Data;
using Snoop.DebugListenerTab;

namespace Snoop.Converters
{
    public class FilterTypeToIntConverter : IValueConverter
    {
        public static readonly FilterTypeToIntConverter Default = new FilterTypeToIntConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is FilterType))
                return value;

            var filterType = (FilterType)value;
            return (int)filterType;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int))
                return value;

            var intValue = (int)value;
            return (FilterType)intValue;
        }
    }
}
