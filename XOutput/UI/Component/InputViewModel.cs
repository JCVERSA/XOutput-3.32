using System;
using System.Windows.Media;
using System.Windows.Threading;
using XOutput.Devices;
using XOutput.Devices.Input;
using XOutput.Tools;
using XOutput.UI.Shell;
using XOutput.UI.Windows;

namespace XOutput.UI.Component
{
    public class InputViewModel : ViewModelBase<InputModel>, IDisposable
    {
        private const int BackgroundDelayMS = 500;
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly bool isAdmin;

        public InputViewModel(InputModel model, IInputDevice device, bool isAdmin) : base(model)
        {
            this.isAdmin = isAdmin;
            Model.Device = device;
            Model.Background = ThemeHelper.GetBrush("BrushSurfaceContainerLow");
            Model.Device.InputChanged += InputDevice_InputChanged;
            timer.Interval = TimeSpan.FromMilliseconds(BackgroundDelayMS);
            timer.Tick += Timer_Tick;
        }

        public void Edit()
        {
            ShellViewModel.Instance?.OpenMapping(Model.Device, isAdmin);
        }

        public void Dispose()
        {
            timer.Tick -= Timer_Tick;
            Model.Device.InputChanged -= InputDevice_InputChanged;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Model.Background = ThemeHelper.GetBrush("BrushSurfaceContainerLow");
        }

        private void InputDevice_InputChanged(object sender, DeviceInputChangedEventArgs e)
        {
            Model.Background = ThemeHelper.GetBrush("BrushInputActive");
            timer.Stop();
            timer.Start();
        }
    }
}
