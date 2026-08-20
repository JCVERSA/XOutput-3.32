using System;
using System.Globalization;
using System.Windows.Data;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts a bool into 1 (true) or 0 (false) — used for indicator fill scales.
    /// Cannot be used backwards.
    /// </summary>
    public class BoolToDoubleConverter : IValueConverter
    {
        /// <summary>
        /// Converts the bool value.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value as bool? == true ? 1d : 0d;
        }

        /// <summary>
        /// Intentionally not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
