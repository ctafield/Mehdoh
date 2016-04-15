using System;
using System.Globalization;
using System.Windows.Data;

namespace FieldOfTweets.Common.UI.Convertors
{
    public class NumericFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;

            long actualValue;

            if (!long.TryParse(value.ToString(), out actualValue))
                return value;

            return actualValue.ToString("N0");

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

    }
}
