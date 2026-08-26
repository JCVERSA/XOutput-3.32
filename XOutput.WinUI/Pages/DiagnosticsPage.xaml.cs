using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XOutput.Devices.Input;
using XOutput.Devices.XInput;
using XOutput.Devices.XInput.Vigem;
using XOutput.WinUI.Animation;

namespace XOutput.WinUI.Pages
{
    /// <summary>Diagnostics page — system + per-device checks via the shared core.</summary>
    public sealed partial class DiagnosticsPage : Page
    {
        public DiagnosticsPage()
        {
            this.InitializeComponent();
            // Micro-interaction: hover/press scale feedback on the compositor thread.
            MicroInteractions.AttachScaleFeedback(ExportButton);
            VigemResult.Text = VigemDevice.IsAvailable() ? "installed" : "not installed";
            DeviceDiagnostics.ItemsSource = InputDevices.Instance.GetDevices()
                .Select(d => new DeviceDiagItem(d))
                .ToList();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder — report export wired in the next prompt.
        }
    }

    /// <summary>Lightweight diagnostics row model (classic Binding in the ItemsControl).</summary>
    public sealed class DeviceDiagItem
    {
        public string Name { get; }
        public int Axes { get; }
        public int Buttons { get; }
        public int DPads { get; }

        public DeviceDiagItem(IInputDevice device)
        {
            Name = device.DisplayName;
            Axes = device.Sources.Count(s => InputSourceTypes.Axis.HasFlag(s.Type));
            Buttons = device.Sources.Count(s => s.Type == InputSourceTypes.Button);
            DPads = device.DPads.Count();
        }
    }
}
