using System.Windows;
using System.Windows.Controls;
using XOutput.UI;

namespace XOutput.UI.Shell.Overlays
{
    /// <summary>
    /// Interaction logic for AboutOverlayView.xaml
    /// </summary>
    public partial class AboutOverlayView : UserControl, IViewBase<AboutOverlayViewModel, AboutOverlayModel>
    {
        private readonly AboutOverlayViewModel viewModel;
        public AboutOverlayViewModel ViewModel => viewModel;

        public AboutOverlayView()
        {
            viewModel = new AboutOverlayViewModel();
            DataContext = viewModel;
            InitializeComponent();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            ShellViewModel.Instance?.CloseOverlay();
        }
    }
}
