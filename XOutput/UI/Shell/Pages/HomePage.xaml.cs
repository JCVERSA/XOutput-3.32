using System.Windows;
using System.Windows.Controls;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Pages
{
    /// <summary>
    /// Home page: device list, virtual controllers and the status bar.
    /// </summary>
    public partial class HomePage : UserControl, IViewBase<MainWindowViewModel, MainWindowModel>
    {
        private readonly MainWindowViewModel viewModel;
        public MainWindowViewModel ViewModel => viewModel;

        public HomePage(MainWindowViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
        }

        private void AddControllerClick(object sender, RoutedEventArgs e)
        {
            viewModel.OpenAddControllerWizard();
        }

        private void RefreshClick(object sender, RoutedEventArgs e)
        {
            viewModel.RefreshGameControllers();
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            viewModel.RefreshGameControllers();
        }
    }
}
