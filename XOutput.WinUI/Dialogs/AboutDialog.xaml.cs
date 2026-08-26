using Microsoft.UI.Xaml.Controls;
using XOutput.UpdateChecker;

namespace XOutput.WinUI.Dialogs
{
    /// <summary>About dialog showing the app version from the shared core.</summary>
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();
            VersionText.Text = "Version " + Version.AppVersion;
        }
    }
}
