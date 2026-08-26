using System.Windows.Media;
using XOutput;

namespace XOutput.UI.Shell
{
    /// <summary>
    /// One sidebar navigation entry.
    /// </summary>
    public sealed class ShellNavItem
    {
        /// <summary>
        /// Translation key of the label.
        /// </summary>
        public string LabelKey { get; }
        /// <summary>
        /// 20x20 stroke icon geometry.
        /// </summary>
        public Geometry Icon { get; }
        /// <summary>
        /// Page this item navigates to.
        /// </summary>
        public ShellPageType PageType { get; }

        public ShellNavItem(string labelKey, Geometry icon, ShellPageType pageType)
        {
            LabelKey = labelKey;
            Icon = icon;
            PageType = pageType;
        }

        /// <summary>
        /// Exposed for the language bindings on the nav label.
        /// </summary>
        public LanguageModel LanguageModel => LanguageModel.Instance;
    }
}
