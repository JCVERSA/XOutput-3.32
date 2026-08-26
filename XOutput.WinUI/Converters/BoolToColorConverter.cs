using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace XOutput.WinUI.Converters
{
    /// <summary>Bool → brush for the device status dot.</summary>
    public sealed class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool ok = value as bool? == true;
            return new SolidColorBrush(ok ? Color.FromArgb(255, 90, 200, 90) : Color.FromArgb(255, 160, 160, 160));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
