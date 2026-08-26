using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XOutput.Devices;
using XOutput.Devices.Input;
using XOutput.Devices.Input.DirectInput;
using XOutput.Devices.XInput.Vigem;
using XOutput.Tools;
using XOutput.WinUI.Animation;
using XOutput.WinUI.Dialogs;

namespace XOutput.WinUI.Pages
{
    /// <summary>
    /// Home page: two-card layout (Input Devices + Virtual Controllers) plus a
    /// status bar, matching the validated WPF redesign. Items animate in on the
    /// compositor thread (see Animation/CompositionAnimations.cs); the status
    /// dots are compositor-rendered with animated color transitions (StatusDot).
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private const string SettingsFilePath = "settings.json";
        private readonly DirectInputDevices directInputDevices = new DirectInputDevices();
        private readonly Settings settings;

        public HomePage()
        {
            this.InitializeComponent();
            settings = Settings.Load(SettingsFilePath);

            // Micro-interaction: hover/press scale feedback on the compositor thread.
            MicroInteractions.AttachScaleFeedback(AddControllerButton);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();

            // Card-level entrance (fade + subtle slide) on the compositor thread.
            CompositionAnimations.AttachEntrance(DevicesCard, 10);
            CompositionAnimations.AttachEntrance(ControllersCard, 10);
            CompositionAnimations.AttachEntrance(StatusBar, 8);
        }

        // ===== List item entrances (compositor ImplicitAnimations) =====

        private void DeviceItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                CompositionAnimations.AttachEntrance(element, 10);
            }
        }

        private void ControllerItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                CompositionAnimations.AttachEntrance(element, 10);
            }
        }

        // ===== Data =====

        private void Refresh()
        {
            // Enumerate DirectInput hardware, honoring the ShowAll setting (mirrors
            // WPF MainWindowViewModel.RefreshGameControllers).
            List<Vortice.DirectInput.DeviceInstance> instances =
                directInputDevices.GetInputDevices(settings.ShowAll).ToList();

            // Drop wrappers that are disconnected or no longer present on the bus.
            foreach (var device in InputDevices.Instance.GetDevices().OfType<DirectDevice>().ToArray())
            {
                if (!instances.Any(x => x.InstanceGuid == device.Id) || !device.Connected)
                {
                    InputDevices.Instance.Remove(device);
                    device.Dispose();
                }
            }

            // Wrap newly detected devices. CreateDirectDevice adds the wrapper to
            // InputDevices.Instance and returns null for devices that should be skipped.
            foreach (var instance in instances)
            {
                if (!InputDevices.Instance.GetDevices().OfType<DirectDevice>().Any(d => d.Id == instance.InstanceGuid))
                {
                    directInputDevices.CreateDirectDevice(instance);
                }
            }

            // Bind from the shared registry so the list stays in sync with it.
            var devices = InputDevices.Instance.GetDevices().ToList();
            DevicesList.ItemsSource = devices;

            var controllers = Controllers.Instance.GetControllers().ToList();
            ControllersList.ItemsSource = controllers;
            ControllersList.Visibility = controllers.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            int inputCount = InputDevices.Instance.GetDevices().Count();
            int controllerCount = Controllers.Instance.GetControllers().Count();
            DeviceCountText.Text = "Active Devices: " + inputCount;
            ControllerCountText.Text = "Active Controllers: " + controllerCount;

            bool vigem = VigemDevice.IsAvailable();
            ControllersStatus.Text = vigem
                ? "ViGEm available. Virtual controllers will appear here when created."
                : "ViGEm not installed.";
        }

        private void AddController_Click(object sender, RoutedEventArgs e)
        {
            _ = new AddControllerDialog { XamlRoot = this.XamlRoot }.ShowAsync();
        }
    }

}
