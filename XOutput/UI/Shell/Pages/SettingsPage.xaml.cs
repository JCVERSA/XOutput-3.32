using System.Windows.Controls;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Pages
{
    /// <summary>
    /// Settings page (formerly SettingsWindow).
    /// </summary>
    public partial class SettingsPage : UserControl, IViewBase<SettingsViewModel, SettingsModel>
    {
        private readonly SettingsViewModel viewModel;
        public SettingsViewModel ViewModel => viewModel;

        public SettingsPage(SettingsViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
