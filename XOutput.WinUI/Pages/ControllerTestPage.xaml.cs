using Microsoft.UI.Xaml.Controls;
using XOutput.Devices.XInput;

namespace XOutput.WinUI.Pages
{
    /// <summary>Controller Test page — placeholder with a live visualization demo loop.</summary>
    public sealed partial class ControllerTestPage : Page
    {
        private readonly Microsoft.UI.Xaml.DispatcherTimer timer = new Microsoft.UI.Xaml.DispatcherTimer();
        private double t = 0;

        public ControllerTestPage()
        {
            this.InitializeComponent();
            timer.Interval = System.TimeSpan.FromMilliseconds(50);
            timer.Tick += (s, e) => Tick();
            timer.Start();
        }

        private void Tick()
        {
            t += 0.05;
            // Cycle the target + value so the visualization demonstrably reacts.
            double v = 0.5 + 0.5 * System.Math.Sin(t);
            switch ((int)(t / 1.0) % 6)
            {
                case 0: TestView.LiveTarget = XInputTypes.LX; TestView.LiveValue = v; break;
                case 1: TestView.LiveTarget = XInputTypes.LY; TestView.LiveValue = v; break;
                case 2: TestView.LiveTarget = XInputTypes.RX; TestView.LiveValue = v; break;
                case 3: TestView.LiveTarget = XInputTypes.RY; TestView.LiveValue = v; break;
                case 4: TestView.LiveTarget = XInputTypes.L2; TestView.LiveValue = v; break;
                case 5: TestView.LiveTarget = XInputTypes.R2; TestView.LiveValue = v; break;
            }
            TestView.Highlight = true;
        }
    }
}
