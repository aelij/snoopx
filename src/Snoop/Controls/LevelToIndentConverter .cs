using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Snoop.Controls
{
    [ValueConversion(typeof(int), typeof(Thickness))]
    public sealed class LevelToIndentMarginConverter : IValueConverter
    {
        public double IndentSize { get; set; }

        public LevelToIndentMarginConverter()
        {
            IndentSize = 19;
        }

        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            return new Thickness((o as int?).GetValueOrDefault() * IndentSize, 0, 0, 0);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}