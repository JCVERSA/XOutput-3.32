using System;
using System.IO;
using System.Runtime.InteropServices;
using H.NotifyIcon;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using WinRT.Interop;
using XOutput.Devices.Input.DirectInput;
using XOutput.Tools;
using XOutput.WinUI.Pages;

namespace XOutput.WinUI
{
    /// <summary>
    /// The single top-level WinUI 3 window: native Mica backdrop (live system
    /// theme), custom title bar, NavigationView with the five destinations, and
    /// a tray icon implementing minimize-to-tray / restore / exit.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const string SettingsFilePath = "settings.json";

        private WindowsSystemDispatcherQueueHelper wsdqHelper;
        private MicaController micaController;
        private SystemBackdropConfiguration backdropConfiguration;
        private Tools.Settings settings;
        private bool allowClose = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Provide this window's handle to the shared DirectInput layer (UI-agnostic).
            DirectInputPlatform.HwndProvider = () => WindowNative.GetWindowHandle(this);

            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // ===== Custom title bar =====
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"));

            // ===== Mica backdrop (live system theme) =====
            TrySetSystemBackdrop();

            // ===== Close-to-tray: cancel close, hide to tray (see report §2) =====
            appWindow.Closing += AppWindow_Closing;

            // ===== Settings (close-to-tray preference) =====
            LoadSettings();

            // ===== Navigation =====
            NavView.SelectedItem = NavView.MenuItems[0];
            NavView_SelectionChanged(null, null);
        }

        // ===== Mica (system backdrop controller) =====

        private bool TrySetSystemBackdrop()
        {
            if (!MicaController.IsSupported())
            {
                return false;
            }
            wsdqHelper = new WindowsSystemDispatcherQueueHelper();
            wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

            // Policy object; updated live from window activation and theme changes.
            backdropConfiguration = new SystemBackdropConfiguration
            {
                IsInputActive = true,
                Theme = SystemBackdropTheme.Default,
            };
            Activated += Window_Activated;
            RootGrid.ActualThemeChanged += Window_ThemeChanged;

            micaController = new MicaController();
            // Window.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>()
            micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
            micaController.SetSystemBackdropConfiguration(backdropConfiguration);
            return true;
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (backdropConfiguration != null)
            {
                backdropConfiguration.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
            }
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (backdropConfiguration != null)
            {
                // Follow the app's effective theme (which itself follows the system).
                switch (RootGrid.ActualTheme)
                {
                    case ElementTheme.Dark:
                        backdropConfiguration.Theme = SystemBackdropTheme.Dark;
                        break;
                    case ElementTheme.Light:
                        backdropConfiguration.Theme = SystemBackdropTheme.Light;
                        break;
                    default:
                        backdropConfiguration.Theme = SystemBackdropTheme.Default;
                        break;
                }
            }
        }

        // ===== Close-to-tray =====

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            // WinUI 3 has no cancelable Window.Closing; the supported technique is
            // AppWindow.Closing (Windows App SDK 1.4+). When close-to-tray is enabled
            // and the user did not pick Exit, cancel the close and hide to tray.
            if (!allowClose && settings != null && settings.CloseToTray)
            {
                args.Cancel = true;
                HideToTray();
            }
        }

        private void HideToTray()
        {
            this.Hide(); // H.NotifyIcon extension: hide + efficiency mode
        }

        private void ShowFromTray()
        {
            this.Show();
            this.Activate();
        }

        // ===== Navigation =====

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (NavView.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            {
                switch (tag)
                {
                    case "Home": ContentFrame.Navigate(typeof(HomePage)); break;
                    case "ControllerTest": ContentFrame.Navigate(typeof(ControllerTestPage)); break;
                    case "Mapping": ContentFrame.Navigate(typeof(MappingPage)); break;
                    case "Diagnostics": ContentFrame.Navigate(typeof(DiagnosticsPage)); break;
                    case "Settings": ContentFrame.Navigate(typeof(SettingsPage)); break;
                    case "About": _ = new Dialogs.AboutDialog().ShowAsync(); break;
                }
            }
        }

        // ===== Tray =====

        private void TrayShow_Click(object sender, RoutedEventArgs e)
        {
            ShowFromTray();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            // Exit path: allow the close (skip the tray-cancel), then close.
            allowClose = true;
            this.Close();
        }

        // ===== Settings =====

        private void LoadSettings()
        {
            try
            {
                settings = Settings.Load(SettingsFilePath);
            }
            catch
            {
                settings = new Settings();
            }
        }
    }

    /// <summary>
    /// Ensures the Windows.System.DispatcherQueue controller exists on the current
    /// thread — required by the system backdrop controllers.
    /// </summary>
    internal class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController(
            [In] DispatcherQueueOptions options,
            [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        private object dispatcherQueueController;

        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (DispatcherQueue.GetForCurrentThread() != null)
            {
                return;
            }
            if (dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA
                CreateDispatcherQueueController(options, ref dispatcherQueueController);
            }
        }
    }
}
