using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using XOutput.Devices.Input;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts an <see cref="IInputDevice"/> into a short VID/PID string
    /// (e.g. "VID 046D · PID C24F") parsed from the hardware ID. Returns an
    /// empty string for devices without a hardware ID (keyboard, mouse).
    /// Cannot be used backwards.
    /// </summary>
    public class SourceToVidPidConverter : IValueConverter
    {
        private static readonly Regex vidRegex = new Regex("VID_([0-9A-Fa-f]{4})");
        private static readonly Regex pidRegex = new Regex("PID_([0-9A-Fa-f]{4})");

        /// <summary>
        /// Converts the device to its VID/PID label.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IInputDevice device = value as IInputDevice;
            string hardwareId = device?.HardwareID;
            if (string.IsNullOrEmpty(hardwareId))
            {
                return "";
            }
            var vid = vidRegex.Match(hardwareId);
            var pid = pidRegex.Match(hardwareId);
            if (!vid.Success && !pid.Success)
            {
                return "";
            }
            string result = "";
            if (vid.Success)
            {
                result += "VID " + vid.Groups[1].Value.ToUpperInvariant();
            }
            if (pid.Success)
            {
                if (result.Length > 0)
                {
                    result += " · ";
                }
                result += "PID " + pid.Groups[1].Value.ToUpperInvariant();
            }
            return result;
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
