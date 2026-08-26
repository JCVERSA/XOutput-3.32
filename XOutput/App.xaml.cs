using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XOutput.Logging;
using XOutput.Tools;
using XOutput.UI.Windows;

namespace XOutput
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(App));

        private MainWindowViewModel mainWindowViewModel;
        private SingleInstanceProvider singleInstanceProvider;
        private ArgumentParser argumentParser;

        /// <summary>
        /// Append-only startup log that survives across launches (the TraceLogger
        /// deletes XOutput.log on start). If the app fails to open, this file
        /// contains the last startup steps / exception for diagnosis.
        /// </summary>
        private const string StartupLogFile = "XOutput-startup.log";

        private static void LogStartup(string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(Environment.CurrentDirectory, StartupLogFile),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
            }
            catch (Exception)
            {
                // Best effort — a failing startup log must not fail startup.
            }
        }

        public App()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string cwd = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(cwd))
            {
                cwd = Environment.CurrentDirectory;
            }
            Directory.SetCurrentDirectory(cwd);
            LogStartup("Starting XOutput " + UpdateChecker.Version.AppVersion + " from " + exePath);

            ApplicationContext globalContext = ApplicationContext.Global;
            globalContext.Resolvers.Add(Resolver.CreateSingleton(Dispatcher));
            globalContext.AddFromConfiguration(typeof(ApplicationConfiguration));
            globalContext.AddFromConfiguration(typeof(UI.UIConfiguration));

            singleInstanceProvider = new SingleInstanceProvider();
            argumentParser = globalContext.Resolve<ArgumentParser>();
#if !DEBUG
            Dispatcher.UnhandledException += UnhandledException;
#endif
        }

        /// <summary>
        /// Handles otherwise-unhandled dispatcher exceptions: logs them, shows the
        /// themed error overlay, and marks the exception as handled so the session
        /// survives a transient UI error (the user can still exit normally).
        /// </summary>
        private void UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                logger.Error(e.Exception).GetAwaiter().GetResult();
                // Best effort: the shell may not be available on a crash path.
                XOutput.UI.Shell.ShellViewModel.Instance?.ShowMessage(LanguageModel.Instance.Translate("Error"),
                    e.Exception.Message + Environment.NewLine + e.Exception.StackTrace);
            }
            catch (Exception)
            {
                // Never throw from the exception handler itself.
            }
            e.Handled = true;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                if (singleInstanceProvider.TryGetLock())
                {
                    LogStartup("Single-instance lock acquired.");
                    singleInstanceProvider.StartNamedPipe();
                    try
                    {
                        var mainWindow = ApplicationContext.Global.Resolve<MainWindow>();
                        mainWindowViewModel = mainWindow.ViewModel;
                        MainWindow = mainWindow;
                        singleInstanceProvider.ShowEvent += mainWindow.ForceShow;
                        if (!argumentParser.Minimized)
                        {
                            mainWindow.Show();
                        }
                        ApplicationContext.Global.Resolve<Devices.Input.Mouse.MouseHook>().StartHook();
                        LogStartup("Main window shown.");
                    }
                    catch (Exception ex)
                    {
                        LogStartup("STARTUP EXCEPTION: " + ex);
                        logger.Error(ex);
                        // Best effort: the shell may not exist yet on a startup failure.
                        XOutput.UI.Shell.ShellViewModel.Instance?.ShowMessage(LanguageModel.Instance.Translate("Error"), ex.ToString());
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    LogStartup("Another instance owns the single-instance lock; notifying it.");
                    if (!singleInstanceProvider.Notify())
                    {
                        // The mutex is held but the running instance did not answer —
                        // it is stuck or was killed without releasing the mutex.
                        LogStartup("Running instance did not respond; showing guidance message.");
                        MessageBox.Show(
                            "XOutput is already running but is not responding.\n\n" +
                            "Open Task Manager, end every 'XOutput' process, then start XOutput again.",
                            "XOutput");
                    }
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                LogStartup("STARTUP EXCEPTION: " + ex);
                logger.Error(ex);
                MessageBox.Show("XOutput failed to start:\n\n" + ex.Message, "XOutput");
                Application.Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            mainWindowViewModel?.Dispose();
            singleInstanceProvider.StopNamedPipe();
            singleInstanceProvider.Close();
            ApplicationContext.Global.Close();
        }
    }
}
