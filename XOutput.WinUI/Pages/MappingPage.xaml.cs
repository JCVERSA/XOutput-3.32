using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using XOutput.Devices;
using XOutput.Devices.Input;
using XOutput.Devices.XInput;

namespace XOutput.WinUI.Pages
{
    /// <summary>
    /// Mapping page — per-device tabs (physical inputs) + configure card with a
    /// live controller preview driven by the selected source. Mapping write-back
    /// to InputMapper is wired in the next prompt (same engine as WPF).
    /// </summary>
    public sealed partial class MappingPage : Page
    {
        public IReadOnlyList<IInputDevice> Devices { get; }
        public XInputTypes LiveTarget { get; private set; } = XInputTypes.LX;
        public double LiveValue { get; private set; } = 0.5;
        public bool PreviewHighlight { get; private set; } = true;

        public MappingPage()
        {
            this.InitializeComponent();
            Devices = InputDevices.Instance.GetDevices().ToList();
            if (Devices.Count > 0)
            {
                DeviceTabs.SelectedItem = Devices[0];
                ShowSources(Devices[0]);
            }
        }

        private void DeviceTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DeviceTabs.SelectedItem is IInputDevice device)
            {
                ShowSources(device);
            }
        }

        private void ShowSources(IInputDevice device)
        {
            SourcesList.ItemsSource = device.Sources.ToList();
        }

        // Called from the page timer (not wired yet) — the preview reacts to a demo
        // cycle until real input polling lands in the next prompt.
        internal void UpdatePreview()
        {
            // placeholder
        }
    }
}
