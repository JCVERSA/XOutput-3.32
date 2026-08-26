using System;
using System.Globalization;
using System.Windows.Data;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts a normalized 0.0-1.0 live input value into an opacity for the
    /// controller visualization (0 = outline only, 1 = fully filled). Booleans
    /// map to 1/0 so the same converter can serve digital sources. Values are
    /// clamped to [0,1]; unsupported types yield 0.
    /// Cannot be used backwards.
    /// </summary>
    public class ValueToOpacityConverter : IValueConverter
    {
        /// <summary>
        /// Converts the value to an opacity.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v;
            if (value is bool b)
            {
                v = b ? 1.0 : 0.0;
            }
            else if (value is double d)
            {
                v = d;
            }
            else if (value is float f)
            {
                v = f;
            }
            else if (value is byte by)
            {
                v = by / 255.0;          // raw trigger range 0-255
            }
            else if (value is short s)
            {
                v = s / 32767.0;         // raw axis range -32767..32767
            }
            else if (value is int i)
            {
                v = i / 32767.0;
            }
            else
            {
                v = 0;
            }
            return Math.Max(0.0, Math.Min(1.0, v));
        }

        /// <summary>
        /// Intentionally not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a normalized 0.0-1.0 stick axis value into a pixel offset for the
    /// stick cap, centered at 0 for the 0.5 neutral position with a maximum travel
    /// of ±6 px. ConverterParameter "Y" inverts the offset so value 1 moves the cap
    /// up, matching the axis convention used by <c>Axis2DView</c> (canvas Y grows
    /// downward). Any other parameter is treated as the horizontal axis (value 1 =
    /// right). Cannot be used backwards.
    /// </summary>
    public class StickOffsetConverter : IValueConverter
    {
        private const double MaxOffset = 6.0;

        /// <summary>
        /// Converts the value to a pixel offset.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double v = value is double d ? d : 0.0;
            v = Math.Max(0.0, Math.Min(1.0, v));
            if (string.Equals(parameter as string, "Y", StringComparison.OrdinalIgnoreCase))
            {
                return (0.5 - v) * 2 * MaxOffset;
            }
            return (v - 0.5) * 2 * MaxOffset;
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
