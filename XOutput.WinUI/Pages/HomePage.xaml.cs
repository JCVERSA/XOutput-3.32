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
        private readonly DirectInputDevices directInputDevices = new DirectInputDevices();

        public HomePage()
        {
            this.InitializeComponent();

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
            var devices = directInputDevices.GetInputDevices(allDevices: false).ToList();
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
            _ = new AddControllerDialog().ShowAsync();
        }
    }

}
