using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Linq;
using XOutput.Logging;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell
{
    /// <summary>
    /// Interaction logic for MainShellView.xaml
    /// </summary>
    public partial class MainShellView : UserControl
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(MainShellView));

        private const int MaxLogLines = 4000;
        private const int MaxLogChars = 300000;
        private int logLineCount = 0;

        private ShellViewModel viewModel;

        public MainShellView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates the shell view model and binds it to this view.
        /// </summary>
        /// <param name="mainViewModel">The application-level view model (home page + menu actions)</param>
        public void Initialize(MainWindowViewModel mainViewModel)
        {
            viewModel = new ShellViewModel(mainViewModel);
            DataContext = viewModel;
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.LogMessage += AppendLog;
            viewModel.ExitRequested += ExitRequestedHandler;
            ApplyDrawerState();
        }

        /// <summary>
        /// Appends a message to the console drawer (routed from the main window).
        /// </summary>
        /// <param name="msg">Message</param>
        public void Log(string msg)
        {
            viewModel?.Log(msg);
        }

        /// <summary>
        /// Raised when the user requests exit through the shell menu.
        /// </summary>
        public event Action ExitRequested;

        private void ExitRequestedHandler()
        {
            ExitRequested?.Invoke();
        }

        private void AppendLog(string msg)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    logBox.AppendText(msg + Environment.NewLine);
                    logLineCount++;
                    if (logLineCount > MaxLogLines || logBox.Text.Length > MaxLogChars)
                    {
                        // Trim the oldest half of the log so long sessions do not
                        // grow the TextBox memory without bound.
                        string text = logBox.Text;
                        int cut = text.IndexOf('\n', Math.Max(0, text.Length / 2));
                        if (cut < 0)
                        {
                            cut = text.Length - 1;
                        }
                        logBox.Text = text.Substring(cut + 1);
                        logLineCount = logBox.Text.Count(c => c == '\n') + 1;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Cannot log into the log box: " + msg + Environment.NewLine);
                    logger.Error(ex);
                }
            }));
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.IsConsoleExpanded) || e.PropertyName == nameof(ShellViewModel.ConsoleHeight))
            {
                ApplyDrawerState();
            }
        }

        private void ApplyDrawerState()
        {
            if (viewModel == null)
            {
                return;
            }
            if (viewModel.IsConsoleExpanded)
            {
                DrawerRow.Height = new GridLength(Math.Max(120, viewModel.ConsoleHeight));
                DrawerSplitter.Visibility = Visibility.Visible;
                logBox.Visibility = Visibility.Visible;
                ChevronDown.Visibility = Visibility.Visible;
                ChevronUp.Visibility = Visibility.Collapsed;
            }
            else
            {
                DrawerRow.Height = GridLength.Auto;
                DrawerSplitter.Visibility = Visibility.Collapsed;
                logBox.Visibility = Visibility.Collapsed;
                ChevronDown.Visibility = Visibility.Collapsed;
                ChevronUp.Visibility = Visibility.Visible;
            }
        }

        private void DrawerSplitterDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            if (viewModel != null && DrawerRow.Height.IsAbsolute)
            {
                viewModel.ConsoleHeight = DrawerRow.Height.Value;
            }
        }

        private void ToggleConsoleClick(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.IsConsoleExpanded = !viewModel.IsConsoleExpanded;
            }
        }

        private void CopyLogClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(logBox.Text);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            viewModel?.SaveSettings();
        }

        private void GameControllersClick(object sender, RoutedEventArgs e)
        {
            viewModel?.OpenWindowsGameControllerSettings();
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            viewModel?.NavigateTo(ShellPageType.Settings);
        }

        private void DiagnosticsClick(object sender, RoutedEventArgs e)
        {
            viewModel?.NavigateTo(ShellPageType.Diagnostics);
        }

        private void AboutClick(object sender, RoutedEventArgs e)
        {
            viewModel?.OpenAbout();
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            viewModel?.RequestExit();
        }
    }
}
