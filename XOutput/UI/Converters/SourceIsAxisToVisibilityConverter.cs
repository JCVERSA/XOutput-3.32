using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using XOutput.Devices;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts an <see cref="InputSource"/> into a visibility value: visible when
    /// the source is an axis (used for the 2D preview indicator). Cannot be used backwards.
    /// </summary>
    public class SourceIsAxisToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts the source to visibility.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            InputSource source = value as InputSource;
            return source != null && source.IsAxis ? Visibility.Visible : Visibility.Collapsed;
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
