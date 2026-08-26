using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace XOutput.WinUI.Converters
{
    /// <summary>
    /// Converts a normalized 0.0-1.0 value into an opacity (0 = invisible, 1 = full).
    /// Booleans map to 1/0; values are clamped to [0,1].
    /// </summary>
    public class ValueToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double v = 0;
            if (value is bool b) v = b ? 1 : 0;
            else if (value is double d) v = d;
            else if (value is float f) v = f;
            else if (value is byte by) v = by / 255.0;
            else if (value is short s) v = s / 32767.0;
            return Math.Max(0.0, Math.Min(1.0, v));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts a normalized 0.0-1.0 stick axis into a pixel offset (±6 px travel).
    /// ConverterParameter "Y" inverts so value 1 moves the cap up (canvas Y grows down).
    /// </summary>
    public class StickOffsetConverter : IValueConverter
    {
        private const double MaxOffset = 6.0;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double v = value is double d ? d : 0.0;
            v = Math.Max(0.0, Math.Min(1.0, v));
            if (string.Equals(parameter as string, "Y", StringComparison.OrdinalIgnoreCase))
                return (0.5 - v) * 2 * MaxOffset;
            return (v - 0.5) * 2 * MaxOffset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
