using System.Windows;
using System.Windows.Controls;
using XOutput.UI;

namespace XOutput.UI.Shell.Overlays
{
    /// <summary>
    /// Interaction logic for MessageOverlayView.xaml
    /// </summary>
    public partial class MessageOverlayView : UserControl, IViewBase<MessageOverlayViewModel, MessageOverlayModel>
    {
        private readonly MessageOverlayViewModel viewModel;
        public MessageOverlayViewModel ViewModel => viewModel;

        public MessageOverlayView(MessageOverlayViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            ShellViewModel.Instance?.CloseOverlay();
        }
    }
}
