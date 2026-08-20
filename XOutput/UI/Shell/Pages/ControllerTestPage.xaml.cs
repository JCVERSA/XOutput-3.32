using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XOutput.Devices;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Pages
{
    /// <summary>
    /// Controller test page (formerly ControllerSettingsWindow): per-controller
    /// mapping editor with live XInput test values.
    /// </summary>
    public partial class ControllerTestPage : UserControl, IViewBase<ControllerSettingsViewModel, ControllerSettingsModel>
    {
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly ControllerSettingsViewModel viewModel;
        private readonly GameController controller;
        public ControllerSettingsViewModel ViewModel => viewModel;

        public ControllerTestPage(ControllerSettingsViewModel viewModel, GameController controller)
        {
            this.controller = controller;
            this.viewModel = viewModel;
            DataContext = viewModel;
            InitializeComponent();
            if (viewModel == null || controller == null)
            {
                Content.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (viewModel == null)
            {
                return;
            }
            viewModel.Update();
            timer.Interval = TimeSpan.FromMilliseconds(10);
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            timer.Tick -= TimerTick;
            timer.Stop();
            viewModel?.Dispose();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            viewModel.Update();
        }

        private void ConfigureAllButtonClick(object sender, RoutedEventArgs e)
        {
            viewModel.ConfigureAll();
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            viewModel.SetStartWhenConnected();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            viewModel.SetForceFeedback();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            controller.Mapper.Name = ViewModel.Model.Title;
        }
    }
}
