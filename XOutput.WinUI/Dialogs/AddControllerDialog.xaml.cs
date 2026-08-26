using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using XOutput.Devices.Input;
using XOutput.Devices.XInput;

namespace XOutput.WinUI.Dialogs
{
    /// <summary>
    /// 4-step "Add Controller" wizard as a single ContentDialog with internal
    /// step swapping (same validated pattern as the WPF overlay). Steps:
    /// 1 Select Device · 2 Test Inputs (live controller) · 3 Confirm · 4 Done.
    /// The full mapping engine integration (InputMapper + ViGEm creation) is
    /// wired in the next prompt; this builds the dialog shell + step flow.
    /// </summary>
    public sealed partial class AddControllerDialog : ContentDialog
    {
        private readonly XInputTypes[] steps = { XInputTypes.LX, XInputTypes.LY, XInputTypes.RX, XInputTypes.RY };
        private int step = 0;
        private InputSource selectedSource;
        private double liveValue = 0.5;

        public AddControllerDialog()
        {
            this.InitializeComponent();
            PrimaryButtonClick += OnPrimary;
            CloseButtonClick += (s, e) => { };
            ShowStep(0);
        }

        /// <summary>x:Bind target for the controller view.</summary>
        public XInputTypes LiveTarget => steps[step];
        /// <summary>x:Bind source for the controller view (live value).</summary>
        public double LiveValue => liveValue;
        /// <summary>x:Bind highlight (blink) for the controller view.</summary>
        public bool Highlight => step == 1;

        private void OnPrimary(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (step < steps.Length - 1)
            {
                step++;
                ShowStep(step);
                args.Cancel = true; // keep the dialog open while advancing
            }
            else
            {
                // Done — allow close.
            }
        }

        private void ShowStep(int s)
        {
            StepText.Text = "Step " + (s + 1) + " of " + steps.Length;
            StepCountText.Text = " — " + (s == 0 ? "Select Device" : s == 1 ? "Test Inputs" : s == 2 ? "Confirm Mapping" : "Done");
            bool showController = s == 1;
            ControllerView.Visibility = showController ? Visibility.Visible : Visibility.Collapsed;
            StepContent.Visibility = showController ? Visibility.Collapsed : Visibility.Visible;
            StepContent.Content = new TextBlock
            {
                Text = s switch
                {
                    0 => "Select the device to configure.",
                    1 => "Move the requested input.",
                    2 => "Confirm the mapping.",
                    _ => "Controller created.",
                },
                TextWrapping = TextWrapping.Wrap,
            };
            PrimaryButtonText = s == steps.Length - 1 ? "Finish" : "Next";
        }
    }
}
