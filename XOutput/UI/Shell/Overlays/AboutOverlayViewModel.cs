using System;
using System.ComponentModel;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Overlays
{
    /// <summary>
    /// Model for the about overlay.
    /// </summary>
    public sealed class AboutOverlayModel : ModelBase
    {
    }

    /// <summary>
    /// View model for the about overlay. Re-evaluates the translated strings
    /// when the application language changes while the overlay is open.
    /// </summary>
    public sealed class AboutOverlayViewModel : ViewModelBase<AboutOverlayModel>, INotifyPropertyChanged
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
            LanguageModel.Instance.PropertyChanged += LanguageModel_PropertyChanged;
        }

        private void LanguageModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LanguageModel.Data))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        /// <summary>
        /// Raised when the translated strings change.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
