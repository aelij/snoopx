// (c) 2015 Eli Arbel
// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using Snoop.MethodsTab;

namespace Snoop.Converters
{
    public class SnoopParameterInfoConverter : IValueConverter
    {
        public static readonly SnoopParameterInfoConverter Default = new SnoopParameterInfoConverter();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramInfo = value as SnoopParameterInformation;
            if (paramInfo == null)
                return value;

            var converter = TypeDescriptor.GetConverter(paramInfo.ParameterType);

            var result = converter.ConvertFrom(paramInfo.ParameterValue);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    public class SnoopDependencyPropertiesConverter : IValueConverter
    {
        public static readonly SnoopDependencyPropertiesConverter Default = new SnoopDependencyPropertiesConverter();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramInfo = (SnoopParameterInformation)value;
            var t = paramInfo.DeclaringType;

            //var fields = t.GetFields(System.Reflection.BindingFlags.FlattenHierarchy);
            var fields = t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Static);

            var dpType = typeof(DependencyProperty);

            var dependencyProperties = fields
                .Where(field => dpType.IsAssignableFrom(field.FieldType))
                .Select(field => new DependencyPropertyNameValuePair
                {
                    DependencyPropertyName = field.Name,
                    DependencyProperty = (DependencyProperty) field.GetValue(null)
                }).ToList();

            dependencyProperties.Sort();

            return dependencyProperties;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    public class DependencyPropertyNameValuePair : IComparable
    {
        public string DependencyPropertyName { get; set; }

        public DependencyProperty DependencyProperty { get; set; }

        public override string ToString()
        {
            return DependencyPropertyName;
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            var toCompareTo = (DependencyPropertyNameValuePair)obj;

            return string.Compare(DependencyPropertyName, toCompareTo.DependencyPropertyName, StringComparison.Ordinal);
        }

        #endregion
    }

    public class SnoopEnumValuesConverter : IValueConverter
    {
        public static readonly SnoopEnumValuesConverter Default = new SnoopEnumValuesConverter();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum)
            {
                return Enum.GetValues(value.GetType());
            }

            if (value is bool)
            {
                return new object[] { true, false };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
