using System;
using System.Globalization;
using System.Windows.Data;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts a value / maximum pair into a 0-1 ratio (used for fill scales).
    /// Cannot be used backwards.
    /// </summary>
    public class RatioScaleConverter : IMultiValueConverter
    {
        /// <summary>
        /// values[0] = value, values[1] = maximum.
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double value = System.Convert.ToDouble(values[0], CultureInfo.InvariantCulture);
            double max = System.Convert.ToDouble(values[1], CultureInfo.InvariantCulture);
            if (max <= 0)
            {
                return 0d;
            }
            return Math.Max(0, Math.Min(1, value / max));
        }

        /// <summary>
        /// Intentionally not implemented.
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
