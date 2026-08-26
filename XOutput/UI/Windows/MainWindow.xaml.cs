using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using XOutput.Devices.Input.DirectInput;
using XOutput.Logging;
using XOutput.Tools;

namespace XOutput.UI.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IViewBase<MainWindowViewModel, MainWindowModel>
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(MainWindow));
        private readonly MainWindowViewModel viewModel;
        public MainWindowViewModel ViewModel => viewModel;
        private bool hardExit = false;
        private WindowState restoreState = WindowState.Normal;

        public MainWindow(MainWindowViewModel viewModel, ArgumentParser argumentParser)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            if (argumentParser.Minimized)
            {
                Visibility = Visibility.Hidden;
                ShowInTaskbar = false;
                logger.Info("Starting XOutput in minimized to taskbar");
            }
            else
            {
                ShowInTaskbar = true;
                logger.Info("Starting XOutput in normal window");
            }
            new WindowInteropHelper(this).EnsureHandle();
            // Provide our window handle to the shared DirectInput layer (it is
            // UI-framework-agnostic and cannot reference System.Windows itself).
            DirectInputPlatform.HwndProvider = () => new WindowInteropHelper(this).Handle;
            InitializeComponent();
            ShellView.Initialize(viewModel);
            ShellView.ExitRequested += () => ExitClick(this, null);
            viewModel.Initialize(Log);
            Dispatcher.Invoke(Initialize);
        }

        private async Task Initialize()
        {
            await logger.Info("The application has started.");
            await GetData();
        }

        public async Task GetData()
        {
            try
            {
                var result = await new UpdateChecker.UpdateChecker().CompareRelease();
                viewModel.VersionCompare(result);
            }
            catch (Exception)
            {
                // Version comparison failed
            }
        }

        public void Log(string msg)
        {
            ShellView.Log(msg);
        }

        private void StartAllClick(object sender, RoutedEventArgs e)
        {
            viewModel.StartAllControllers();
        }

        private void StopAllClick(object sender, RoutedEventArgs e)
        {
            viewModel.StopAllControllers();
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            hardExit = true;
            if (IsLoaded)
            {
                Close();
            }
            else
            {
                logger.Info("The application will exit.");
                Application.Current.Shutdown();
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (viewModel.GetSettings().CloseToTray && !hardExit)
            {
                e.Cancel = true;
                restoreState = WindowState;
                Visibility = Visibility.Hidden;
                ShowInTaskbar = false;
                logger.Info("The application is closed to tray.");
            }
        }

        private async void WindowClosed(object sender, EventArgs e)
        {
            viewModel.Dispose();
            await logger.Info("The application will exit.");
        }

        private void TaskbarIconTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                WindowState = restoreState;
            }
            else if (Visibility == Visibility.Hidden)
            {
                if (!IsLoaded)
                {
                    Show();
                }
                ShowInTaskbar = true;
                Visibility = Visibility.Visible;
            }
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        public void ForceShow()
        {
            Dispatcher.Invoke(() => {
                TaskbarIconTrayMouseDoubleClick(this, null);
            });
        }
    }
}
