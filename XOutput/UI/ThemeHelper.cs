using System;
using System.Windows;
using System.Windows.Media;

namespace XOutput.UI
{
    /// <summary>
    /// Resolves theme brushes from the application resource dictionary.
    /// Keeps runtime-created brushes (converters, view models) consistent with
    /// the Kinetic Console theme — no hardcoded colors outside of Colors.xaml.
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Gets a brush from the application resources by key.
        /// </summary>
        /// <param name="key">Resource key (e.g. "BrushPrimary")</param>
        /// <returns>The brush</returns>
        public static Brush GetBrush(string key)
        {
            if (Application.Current != null && Application.Current.Resources[key] is Brush brush)
            {
                return brush;
            }
            throw new InvalidOperationException($"Theme brush '{key}' was not found in application resources.");
        }
    }
}
