using System.Collections.ObjectModel;
using XOutput.UI.Component;

namespace XOutput.UI.Windows
{
    public class MainWindowModel : ModelBase
    {
        private Tools.Settings settings;
        public Tools.Settings Settings
        {
            get => settings;
            set
            {
                if (settings != value)
                {
                    settings = value;
                    OnPropertyChanged(nameof(AllDevices));
                }
            }
        }

        private readonly ObservableCollection<InputView> inputs = new ObservableCollection<InputView>();
        public ObservableCollection<InputView> Inputs { get { return inputs; } }

        public bool AllDevices
        {
            get => settings?.ShowAll ?? false;
            set
            {
                if (settings != null && settings.ShowAll != value)
                {
                    settings.ShowAll = value;
                    OnPropertyChanged(nameof(AllDevices));
                }
            }
        }

        private bool isAdmin;
        public bool IsAdmin
        {
            get => isAdmin;
            set
            {
                if (isAdmin != value)
                {
                    isAdmin = value;
                    OnPropertyChanged(nameof(IsAdmin));
                }
            }
        }

        private readonly ObservableCollection<ControllerView> controllers = new ObservableCollection<ControllerView>();
        public ObservableCollection<ControllerView> Controllers { get { return controllers; } }

        /// <summary>
        /// Gets the number of detected input devices (presentation badge).
        /// </summary>
        public int InputCount => inputs.Count;

        /// <summary>
        /// Gets the number of virtual controllers (presentation badge).
        /// </summary>
        public int ControllerCount => controllers.Count;

        private string backendName = "";
        /// <summary>
        /// Gets or sets the emulation backend name (ViGEm / SCP Toolkit).
        /// </summary>
        public string BackendName
        {
            get => backendName;
            set
            {
                if (backendName != value)
                {
                    backendName = value;
                    OnPropertyChanged(nameof(BackendName));
                }
            }
        }

        public MainWindowModel()
        {
            inputs.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(InputCount));
            controllers.CollectionChanged += (sender, e) => OnPropertyChanged(nameof(ControllerCount));
        }
    }
}
