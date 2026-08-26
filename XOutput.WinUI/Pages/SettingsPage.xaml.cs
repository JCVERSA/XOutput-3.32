using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XOutput.Tools;

namespace XOutput.WinUI.Pages
{
    /// <summary>Settings page — edits the shared Core settings (persisted by Settings.Save).</summary>
    public sealed partial class SettingsPage : Page
    {
        private const string SettingsFilePath = "settings.json";
        private readonly Settings settings;

        public SettingsPage()
        {
            this.InitializeComponent();
            settings = Settings.Load(SettingsFilePath);
            CloseToTraySwitch.IsOn = settings.CloseToTray;
            ShowAllSwitch.IsOn = settings.ShowAll;
            HidGuardianSwitch.IsOn = settings.HidGuardianEnabled;
            DisableRefreshSwitch.IsOn = settings.DisableAutoRefresh;
        }

        private void CloseToTray_Toggled(object sender, RoutedEventArgs e)
        {
            settings.CloseToTray = CloseToTraySwitch.IsOn;
            settings.Save(SettingsFilePath);
        }
        private void ShowAll_Toggled(object sender, RoutedEventArgs e)
        {
            settings.ShowAll = ShowAllSwitch.IsOn;
            settings.Save(SettingsFilePath);
        }
        private void HidGuardian_Toggled(object sender, RoutedEventArgs e)
        {
            settings.HidGuardianEnabled = HidGuardianSwitch.IsOn;
            settings.Save(SettingsFilePath);
        }
        private void DisableRefresh_Toggled(object sender, RoutedEventArgs e)
        {
            settings.DisableAutoRefresh = DisableRefreshSwitch.IsOn;
            settings.Save(SettingsFilePath);
        }
    }
}
