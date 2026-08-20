using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using XOutput.Devices.XInput;

namespace XOutput.UI.Converters
{
    /// <summary>
    /// Calculates the color of the elements.
    /// Cannot be used backwards.
    /// </summary>
    public class ColorConverter : IMultiValueConverter
    {
        protected static readonly Brush HighlightBrush = ThemeHelper.GetBrush("BrushXboxHighlight");
        protected static readonly Brush HighlightBackBrush = ThemeHelper.GetBrush("BrushXboxHighlightBack");
        protected static readonly Brush HighlightLabelBrush = ThemeHelper.GetBrush("BrushXboxHighlightLabel");
        protected static readonly Brush DPadBackBrush = ThemeHelper.GetBrush("BrushXboxDPadBack");
        protected Dictionary<XInputTypes, Brush> foregroundColors = new Dictionary<XInputTypes, Brush>();
        protected Dictionary<XInputTypes, Brush> backgroundColors = new Dictionary<XInputTypes, Brush>();
        protected Dictionary<XInputTypes, Brush> labelColors = new Dictionary<XInputTypes, Brush>();

        public ColorConverter()
        {
            foregroundColors.Add(XInputTypes.A, ThemeHelper.GetBrush("BrushXboxA"));
            backgroundColors.Add(XInputTypes.A, ThemeHelper.GetBrush("BrushXboxABack"));
            labelColors.Add(XInputTypes.A, ThemeHelper.GetBrush("BrushXboxALabel"));
            foregroundColors.Add(XInputTypes.B, ThemeHelper.GetBrush("BrushXboxB"));
            backgroundColors.Add(XInputTypes.B, ThemeHelper.GetBrush("BrushXboxBBack"));
            labelColors.Add(XInputTypes.B, ThemeHelper.GetBrush("BrushXboxBLabel"));
            foregroundColors.Add(XInputTypes.X, ThemeHelper.GetBrush("BrushXboxX"));
            backgroundColors.Add(XInputTypes.X, ThemeHelper.GetBrush("BrushXboxXBack"));
            labelColors.Add(XInputTypes.X, ThemeHelper.GetBrush("BrushXboxXLabel"));
            foregroundColors.Add(XInputTypes.Y, ThemeHelper.GetBrush("BrushXboxY"));
            backgroundColors.Add(XInputTypes.Y, ThemeHelper.GetBrush("BrushXboxYBack"));
            labelColors.Add(XInputTypes.Y, ThemeHelper.GetBrush("BrushXboxYLabel"));
            foregroundColors.Add(XInputTypes.L1, ThemeHelper.GetBrush("BrushXboxBumper"));
            backgroundColors.Add(XInputTypes.L1, ThemeHelper.GetBrush("BrushXboxBumperBack"));
            labelColors.Add(XInputTypes.L1, ThemeHelper.GetBrush("BrushXboxBumperLabel"));
            foregroundColors.Add(XInputTypes.R1, ThemeHelper.GetBrush("BrushXboxBumper"));
            backgroundColors.Add(XInputTypes.R1, ThemeHelper.GetBrush("BrushXboxBumperBack"));
            labelColors.Add(XInputTypes.R1, ThemeHelper.GetBrush("BrushXboxBumperLabel"));
            foregroundColors.Add(XInputTypes.L2, ThemeHelper.GetBrush("BrushXboxTrigger"));
            backgroundColors.Add(XInputTypes.L2, ThemeHelper.GetBrush("BrushXboxTriggerBack"));
            labelColors.Add(XInputTypes.L2, ThemeHelper.GetBrush("BrushXboxTriggerLabel"));
            foregroundColors.Add(XInputTypes.R2, ThemeHelper.GetBrush("BrushXboxTrigger"));
            backgroundColors.Add(XInputTypes.R2, ThemeHelper.GetBrush("BrushXboxTriggerBack"));
            labelColors.Add(XInputTypes.R2, ThemeHelper.GetBrush("BrushXboxTriggerLabel"));
            foregroundColors.Add(XInputTypes.L3, ThemeHelper.GetBrush("BrushXboxStick"));
            backgroundColors.Add(XInputTypes.L3, ThemeHelper.GetBrush("BrushXboxStickBack"));
            foregroundColors.Add(XInputTypes.R3, ThemeHelper.GetBrush("BrushXboxStick"));
            backgroundColors.Add(XInputTypes.R3, ThemeHelper.GetBrush("BrushXboxStickBack"));
            foregroundColors.Add(XInputTypes.Start, ThemeHelper.GetBrush("BrushXboxStart"));
            backgroundColors.Add(XInputTypes.Start, ThemeHelper.GetBrush("BrushXboxStartBack"));
            foregroundColors.Add(XInputTypes.Back, ThemeHelper.GetBrush("BrushXboxStart"));
            backgroundColors.Add(XInputTypes.Back, ThemeHelper.GetBrush("BrushXboxStartBack"));
            foregroundColors.Add(XInputTypes.Home, ThemeHelper.GetBrush("BrushXboxHome"));
            backgroundColors.Add(XInputTypes.Home, ThemeHelper.GetBrush("BrushXboxHomeBack"));
            foregroundColors.Add(XInputTypes.UP, ThemeHelper.GetBrush("BrushXboxStick"));
            backgroundColors.Add(XInputTypes.UP, ThemeHelper.GetBrush("BrushXboxStickBack"));
            foregroundColors.Add(XInputTypes.DOWN, ThemeHelper.GetBrush("BrushXboxStick"));
            backgroundColors.Add(XInputTypes.DOWN, ThemeHelper.GetBrush("BrushXboxStickBack"));
            foregroundColors.Add(XInputTypes.LEFT, ThemeHelper.GetBrush("BrushXboxStick"));
            backgroundColors.Add(XInputTypes.LEFT, ThemeHelper.GetBrush("BrushXboxStickBack"));
            foregroundColors.Add(XInputTypes.RIGHT, ThemeHelper.GetBrush("BrushXboxStick"));
            backgroundColors.Add(XInputTypes.RIGHT, ThemeHelper.GetBrush("BrushXboxStickBack"));
        }

        /// <summary>
        /// Calculates the color of the elements.
        /// </summary>
        /// <param name="values">XInput type and highlight value</param>
        /// <param name="targetType">Ignored</param>
        /// <param name="parameter">XInput type to compare and back/label values</param>
        /// <param name="culture">Ignored</param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            XInputTypes? activeType = values[0] as XInputTypes?;
            bool? highlight = values[1] as bool?;
            var parameters = (parameter as string).Split('|');
            bool back = parameters.Length > 1 && parameters[1] == "back";
            bool label = parameters.Length > 1 && parameters[1] == "label";
            if (parameters[0] == "DPAD")
            {
                if (back)
                {
                    if (highlight == true && XInputHelper.Instance.IsDPad(activeType.Value))
                    {
                        return HighlightBackBrush;
                    }
                    return DPadBackBrush;
                }
            }
            else
            {
                var currentType = (XInputTypes)Enum.Parse(typeof(XInputTypes), parameters[0]);
                if (back)
                {
                    if (highlight == true && currentType == activeType)
                    {
                        return HighlightBackBrush;
                    }
                    else if (backgroundColors.ContainsKey(currentType))
                    {
                        return backgroundColors[currentType];
                    }
                }
                else if (label)
                {
                    if (highlight == true && currentType == activeType)
                    {
                        return HighlightLabelBrush;
                    }
                    else if (labelColors.ContainsKey(currentType))
                    {
                        return labelColors[currentType];
                    }
                }
                else
                {
                    if (highlight == true && currentType == activeType)
                    {
                        return HighlightBrush;
                    }
                    else if (foregroundColors.ContainsKey(currentType))
                    {
                        return foregroundColors[currentType];
                    }
                }
            }
            return ThemeHelper.GetBrush("BrushBackground");
        }

        /// <summary>
        /// Intentionally not implemented.
        /// </summary>
        /// <param name="value">Ignored</param>
        /// <param name="targetTypes">Ignored</param>
        /// <param name="parameter">Ignored</param>
        /// <param name="culture">Ignored</param>
        /// <returns></returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
