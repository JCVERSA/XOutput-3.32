using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Overlays
{
    /// <summary>
    /// In-shell mapping wizard overlay (formerly AutoConfigureWindow).
    /// </summary>
    public partial class WizardOverlayView : UserControl, IViewBase<AutoConfigureViewModel, AutoConfigureModel>
    {
        private readonly AutoConfigureViewModel viewModel;
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private readonly bool timed;
        private bool initialized = false;

        public AutoConfigureViewModel ViewModel => viewModel;

        /// <summary>
        /// Raised when the wizard closes (including automatic close after the last step).
        /// </summary>
        public event Action Closed;

        public WizardOverlayView(AutoConfigureViewModel viewModel, bool timed)
        {
            this.viewModel = viewModel;
            this.timed = timed;
            DataContext = viewModel;
            InitializeComponent();
        }

        private async void WindowLoaded(object sender, RoutedEventArgs e)
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            await Task.Delay(100);
            if (!IsLoaded)
            {
                return;
            }
            viewModel.Initialize();
            viewModel.IsMouseOverButtons = () =>
            {
                return DisableButton.IsMouseOver || SaveButton.IsMouseOver;
            };
            if (timed)
            {
                timer.Interval = TimeSpan.FromMilliseconds(25);
                timer.Tick += TimerTick;
                timer.Start();
            }
        }

        private void WindowUnloaded(object sender, RoutedEventArgs e)
        {
            timer.Tick -= TimerTick;
            timer.Stop();
            viewModel.Close();
            Closed?.Invoke();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            if (viewModel.IncreaseTime())
            {
                bool hasNextInput = viewModel.SaveValues();
                if (!hasNextInput)
                {
                    CloseOverlay();
                }
            }
        }

        private void DisableClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.SaveDisableValues())
            {
                CloseOverlay();
            }
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            if (!viewModel.SaveValues())
            {
                CloseOverlay();
            }
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            CloseOverlay();
        }

        private void CloseOverlay()
        {
            ShellViewModel.Instance?.CloseOverlay();
        }
    }
}
