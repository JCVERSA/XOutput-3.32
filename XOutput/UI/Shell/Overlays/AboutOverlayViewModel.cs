using System.Windows;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Overlays
{
    /// <summary>
    /// View model for the about overlay.
    /// </summary>
    public sealed class AboutOverlayModel : ModelBase
    {
    }

    /// <summary>
    /// View model for the about overlay.
    /// </summary>
    public sealed class AboutOverlayViewModel : ViewModelBase<AboutOverlayModel>
    {
        /// <summary>
        /// Gets the overlay title.
        /// </summary>
        public string Title => LanguageModel.Instance.Translate("AboutMenu");

        /// <summary>
        /// Gets the about text.
        /// </summary>
        public string Text => LanguageModel.Instance.Translate("AboutContent") + Environment.NewLine
            + string.Format(LanguageModel.Instance.Translate("Version"), UpdateChecker.Version.AppVersion);

        public AboutOverlayViewModel() : base(new AboutOverlayModel())
        {
        }
    }
}
