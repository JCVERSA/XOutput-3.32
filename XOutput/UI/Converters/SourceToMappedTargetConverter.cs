using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using XOutput.Devices;
using XOutput.Devices.Input;
using XOutput.Devices.XInput;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Converts an <see cref="InputSource"/> into a translated list of the XInput
    /// targets it is currently mapped to (reverse lookup across all controllers).
    /// Presentation only — the mapping data is read from the existing mappers.
    /// Cannot be used backwards.
    /// </summary>
    public class SourceToMappedTargetConverter : IValueConverter
    {
        /// <summary>
        /// Converts the source to a mapped target label.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            InputSource source = value as InputSource;
            if (source == null)
            {
                return LanguageModel.Instance.Translate("NoMappedTargets");
            }
            List<string> targets = new List<string>();
            foreach (var controller in Controllers.Instance.GetControllers())
            {
                foreach (var mapping in controller.Mapper.Mappings)
                {
                    if (mapping.Value.Mappers.Any(m => m.Source == source))
                    {
                        targets.Add(LanguageModel.Instance.Translate("XInputTypes." + mapping.Key));
                    }
                }
            }
            if (targets.Count == 0)
            {
                return LanguageModel.Instance.Translate("NoMappedTargets");
            }
            return string.Join(", ", targets.Distinct());
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
