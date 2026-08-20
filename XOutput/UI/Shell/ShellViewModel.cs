using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using XOutput.Devices;
using XOutput.Devices.Input;
using XOutput.Devices.Input.DirectInput;
using XOutput.Devices.Mapper;
using XOutput.Devices.XInput;
using XOutput.Diagnostics;
using XOutput.Tools;
using XOutput.UI.Shell.Overlays;
using XOutput.UI.Shell.Pages;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell
{
    /// <summary>
    /// Drives the single-window shell: sidebar navigation, page hosting,
    /// overlay dialogs and the console drawer.
    /// </summary>
    public sealed class ShellViewModel : INotifyPropertyChanged, IDisposable
    {
        private static ShellViewModel instance;
        /// <summary>
        /// Gets the singleton instance of the shell.
        /// </summary>
        public static ShellViewModel Instance => instance;

        private readonly MainWindowViewModel mainViewModel;
        private readonly Dictionary<ShellPageType, ShellNavItem> navItemsByType = new Dictionary<ShellPageType, ShellNavItem>();
        private HomePage homePage;
        private object currentPage;
        private ShellNavItem selectedNavItem;
        private object activeOverlay;
        private bool isConsoleExpanded = true;
        private double consoleHeight = 200;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Exposed for language bindings in the shell chrome (menu, drawer).
        /// </summary>
        public LanguageModel LanguageModel => LanguageModel.Instance;

        /// <summary>
        /// Gets the sidebar navigation items.
        /// </summary>
        public ObservableCollection<ShellNavItem> NavItems { get; } = new ObservableCollection<ShellNavItem>();

        /// <summary>
        /// Gets the page currently shown in the content area.
        /// </summary>
        public object CurrentPage
        {
            get => currentPage;
            private set
            {
                if (currentPage != value)
                {
                    currentPage = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected sidebar item. Setting it navigates to its page.
        /// </summary>
        public ShellNavItem SelectedNavItem
        {
            get => selectedNavItem;
            set
            {
                if (value != null && value != selectedNavItem)
                {
                    NavigateTo(value.PageType);
                }
            }
        }

        /// <summary>
        /// Gets the overlay content currently shown, or null.
        /// </summary>
        public object ActiveOverlay
        {
            get => activeOverlay;
            private set
            {
                if (activeOverlay != value)
                {
                    activeOverlay = value;
                    OnPropertyChanged(nameof(ActiveOverlay));
                    OnPropertyChanged(nameof(HasActiveOverlay));
                }
            }
        }

        /// <summary>
        /// True while an overlay is open.
        /// </summary>
        public bool HasActiveOverlay => activeOverlay != null;

        /// <summary>
        /// Gets or sets whether the console drawer is expanded.
        /// </summary>
        public bool IsConsoleExpanded
        {
            get => isConsoleExpanded;
            set
            {
                if (isConsoleExpanded != value)
                {
                    isConsoleExpanded = value;
                    OnPropertyChanged(nameof(IsConsoleExpanded));
                }
            }
        }

        /// <summary>
        /// Gets or sets the remembered expanded height of the console drawer.
        /// </summary>
        public double ConsoleHeight
        {
            get => consoleHeight;
            set
            {
                double v = Math.Max(120, value);
                if (!Helper.DoubleEquals(consoleHeight, v))
                {
                    consoleHeight = v;
                    OnPropertyChanged(nameof(ConsoleHeight));
                }
            }
        }

        /// <summary>
        /// Raised with each log message that should be appended to the console drawer.
        /// </summary>
        public event Action<string> LogMessage;

        /// <summary>
        /// Raised when the user requests the application to exit (File -gt; Exit).
        /// </summary>
        public event Action ExitRequested;

        public ShellViewModel(MainWindowViewModel mainViewModel)
        {
            instance = this;
            this.mainViewModel = mainViewModel;
            CreateNavItems();
            homePage = new HomePage(mainViewModel);
            NavigateTo(ShellPageType.Home);
        }

        /// <summary>
        /// Appends a message to the console drawer.
        /// </summary>
        public void Log(string msg)
        {
            LogMessage?.Invoke(msg);
        }

        /// <summary>
        /// Navigates to the page of the given type (sidebar default context).
        /// </summary>
        public void NavigateTo(ShellPageType type)
        {
            switch (type)
            {
                case ShellPageType.Home:
                    CurrentPage = homePage;
                    SelectNavItem(ShellPageType.Home);
                    break;
                case ShellPageType.Settings:
                    OpenSettings();
                    break;
                case ShellPageType.Diagnostics:
                    OpenDiagnostics();
                    break;
                case ShellPageType.Mapping:
                    OpenMapping(GetDefaultMappingDevice(), mainViewModel.Model.IsAdmin);
                    break;
                case ShellPageType.ControllerTest:
                    OpenControllerTest(GetDefaultController(), mainViewModel.Model.IsAdmin);
                    break;
                default:
                    throw new ArgumentException(nameof(type));
            }
        }

        /// <summary>
        /// Opens the settings page.
        /// </summary>
        public void OpenSettings()
        {
            ApplicationContext context = ApplicationContext.Global.WithSingletons(mainViewModel.GetSettings());
            SettingsViewModel settingsViewModel = context.Resolve<SettingsViewModel>();
            CurrentPage = new SettingsPage(settingsViewModel);
            SelectNavItem(ShellPageType.Settings);
        }

        /// <summary>
        /// Opens the diagnostics page.
        /// </summary>
        public void OpenDiagnostics()
        {
            IList<IDiagnostics> elements = InputDevices.Instance.GetDevices()
                .Select(d => new InputDiagnostics(d)).OfType<IDiagnostics>().ToList();
            elements.Insert(0, new Devices.XInput.XInputDiagnostics());
            DiagnosticsViewModel diagnosticsViewModel = new DiagnosticsViewModel(new DiagnosticsModel(elements));
            CurrentPage = new DiagnosticsPage(diagnosticsViewModel);
            SelectNavItem(ShellPageType.Diagnostics);
        }

        /// <summary>
        /// Opens the mapping page for the given device.
        /// </summary>
        public void OpenMapping(IInputDevice device, bool isAdmin)
        {
            if (device == null)
            {
                return;
            }
            MappingPageViewModel pageViewModel = new MappingPageViewModel(mainViewModel, device);
            CurrentPage = new MappingPage(pageViewModel);
            SelectNavItem(ShellPageType.Mapping);
        }

        /// <summary>
        /// Opens the controller test page for the given controller.
        /// </summary>
        public void OpenControllerTest(GameController controller, bool isAdmin)
        {
            ControllerSettingsViewModel viewModel = controller == null ? null : new ControllerSettingsViewModel(new ControllerSettingsModel(), controller, isAdmin);
            CurrentPage = new ControllerTestPage(viewModel, controller);
            SelectNavItem(ShellPageType.ControllerTest);
        }

        /// <summary>
        /// Opens the mapping wizard as an in-shell overlay.
        /// </summary>
        /// <param name="inputDevices">Devices to listen to</param>
        /// <param name="mapper">Mapper that receives the configured values</param>
        /// <param name="valuesToRead">XInput values to read</param>
        /// <param name="timed">Whether the wizard runs on a timer</param>
        /// <param name="onClosed">Callback invoked when the wizard closes</param>
        public void OpenWizard(IEnumerable<IInputDevice> inputDevices, InputMapper mapper, XInputTypes[] valuesToRead, bool timed, Action onClosed = null)
        {
            AutoConfigureViewModel viewModel = new AutoConfigureViewModel(new AutoConfigureModel(), inputDevices, mapper, valuesToRead);
            WizardOverlayView view = new WizardOverlayView(viewModel, timed);
            view.Closed += () => onClosed?.Invoke();
            ActiveOverlay = view;
        }

        /// <summary>
        /// Opens the about overlay.
        /// </summary>
        public void OpenAbout()
        {
            ActiveOverlay = new AboutOverlayView();
        }

        /// <summary>
        /// Opens a themed message overlay (replaces the native MessageBox).
        /// </summary>
        public void ShowMessage(string title, string message)
        {
            MessageOverlayViewModel viewModel = new MessageOverlayViewModel(new MessageOverlayModel(), title, message);
            ActiveOverlay = new MessageOverlayView(viewModel);
        }

        /// <summary>
        /// Closes the currently open overlay.
        /// </summary>
        public void CloseOverlay()
        {
            ActiveOverlay = null;
        }

        /// <summary>
        /// Saves the settings (menu action).
        /// </summary>
        public void SaveSettings()
        {
            mainViewModel.SaveSettings();
        }

        /// <summary>
        /// Opens the Windows game controller settings (menu action).
        /// </summary>
        public void OpenWindowsGameControllerSettings()
        {
            mainViewModel.OpenWindowsGameControllerSettings();
        }

        /// <summary>
        /// Requests application exit (menu action).
        /// </summary>
        public void RequestExit()
        {
            ExitRequested?.Invoke();
        }

        public void Dispose()
        {
            instance = null;
        }

        private void CreateNavItems()
        {
            AddNavItem("HomeMenu", "IconHome", ShellPageType.Home);
            AddNavItem("ControllerTestMenu", "IconControllerTest", ShellPageType.ControllerTest);
            AddNavItem("Mapping", "IconMapping", ShellPageType.Mapping);
            AddNavItem("DiagnosticsMenu", "IconDiagnostics", ShellPageType.Diagnostics);
            AddNavItem("SettingsMenu", "IconSettings", ShellPageType.Settings);
        }

        private void AddNavItem(string labelKey, string iconKey, ShellPageType pageType)
        {
            var item = new ShellNavItem(labelKey, ResolveIcon(iconKey), pageType);
            NavItems.Add(item);
            navItemsByType[pageType] = item;
        }

        /// <summary>
        /// Resolves a geometry from the shared icon set (UI/Themes/Icons.xaml).
        /// Falls back to a 20x20 placeholder cross if the resource is missing
        /// (e.g. during design-time preview).
        /// </summary>
        private static System.Windows.Media.Geometry ResolveIcon(string iconKey)
        {
            if (System.Windows.Application.Current != null
                && System.Windows.Application.Current.TryFindResource(iconKey) is System.Windows.Media.Geometry geometry)
            {
                return geometry;
            }
            return System.Windows.Media.Geometry.Parse("M 3,10 L 17,10 M 10,3 L 10,17");
        }

        private void SelectNavItem(ShellPageType type)
        {
            if (navItemsByType.TryGetValue(type, out var item) && item != selectedNavItem)
            {
                selectedNavItem = item;
                OnPropertyChanged(nameof(SelectedNavItem));
            }
        }

        private IInputDevice GetDefaultMappingDevice()
        {
            return InputDevices.Instance.GetDevices().OfType<DirectDevice>().FirstOrDefault()
                ?? InputDevices.Instance.GetDevices().FirstOrDefault();
        }

        private GameController GetDefaultController()
        {
            return mainViewModel.Model.Controllers.FirstOrDefault()?.ViewModel.Model.Controller;
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
