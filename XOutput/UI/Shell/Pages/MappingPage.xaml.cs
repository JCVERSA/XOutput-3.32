using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Pages
{
    /// <summary>
    /// Mapping page: per-device tabs, physical input listing, configure panel
    /// and the force feedback / HidGuardian tools (formerly InputSettingsWindow).
    /// </summary>
    public partial class MappingPage : UserControl
    {
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly MappingPageViewModel viewModel;

        public MappingPageViewModel ViewModel => viewModel;

        public MappingPage(MappingPageViewModel viewModel)
        {
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
            viewModel.DeviceDisconnected += ViewModel_DeviceDisconnected;
        }

        private void ViewModel_DeviceDisconnected()
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (IsLoaded)
                {
                    ShellViewModel.Instance?.NavigateTo(ShellPageType.Home);
                }
            }));
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            viewModel.Update();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            timer.Tick -= TimerTick;
            timer.Stop();
            viewModel.DeviceDisconnected -= ViewModel_DeviceDisconnected;
            viewModel.Dispose();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            viewModel.Update();
        }

        private InputSettingsViewModel GetSelectedDeviceViewModel()
        {
            if (DataContext is MappingPageViewModel vm && vm.SelectedTab is MappingDeviceTab tab)
            {
                return tab.ViewModel;
            }
            return null;
        }

        private void ForceFeedbackButtonClick(object sender, RoutedEventArgs e)
        {
            GetSelectedDeviceViewModel()?.TestForceFeedback();
        }

        private void ForceFeedbackCheckBoxChecked(object sender, RoutedEventArgs e)
        {
            GetSelectedDeviceViewModel()?.SetForceFeedbackEnabled();
        }

        private void AddHidGuardianButtonClick(object sender, RoutedEventArgs e)
        {
            GetSelectedDeviceViewModel()?.AddHidGuardian();
        }

        private void RemoveHidGuardianButtonClick(object sender, RoutedEventArgs e)
        {
            GetSelectedDeviceViewModel()?.RemoveHidGuardian();
        }

        private void SaveMappingClick(object sender, RoutedEventArgs e)
        {
            viewModel.SaveMapping();
        }

        private void ExportProfileClick(object sender, RoutedEventArgs e)
        {
            viewModel.ExportProfile();
        }
    }
}
