using System;
using System.Globalization;
using System.Windows.Data;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts a 0-1 value into a pixel position inside a 40x40 2D indicator
    /// (centered at 20,20). Cannot be used backwards.
    /// </summary>
    public class RatioToPositionConverter : IValueConverter
    {
        private const double BoxSize = 40;
        private const double Travel = 16;

        /// <summary>
        /// Converts the 0-1 value into a pixel offset.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
            double center = BoxSize / 2;
            return center + (v - 0.5) * 2 * Travel;
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
