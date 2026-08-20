using System.Linq;
using System.Windows.Media;
using XOutput.Devices;
using XOutput.Devices.Input;

namespace XOutput.UI.Component
{
    public class ControllerModel : ModelBase
    {
        private GameController controller;
        public GameController Controller
        {
            get => controller;
            set
            {
                if (controller != value)
                {
                    controller = value;
                    OnPropertyChanged(nameof(Controller));
                    OnPropertyChanged(nameof(ActiveSourceCount));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private string buttonText;
        public string ButtonText
        {
            get => buttonText;
            set
            {
                if (buttonText != value)
                {
                    buttonText = value;
                    OnPropertyChanged(nameof(ButtonText));
                }
            }
        }
        private bool started;
        public bool Started
        {
            get => started;
            set
            {
                if (started != value)
                {
                    started = value;
                    OnPropertyChanged(nameof(Started));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        private bool canStart;
        public bool CanStart
        {
            get => canStart;
            set
            {
                if (canStart != value)
                {
                    canStart = value;
                    OnPropertyChanged(nameof(CanStart));
                }
            }
        }

        private Brush background;
        public Brush Background
        {
            get => background;
            set
            {
                if (background != value)
                {
                    background = value;
                    OnPropertyChanged(nameof(Background));
                }
            }
        }
        public string DisplayName { get { return Controller.ToString(); } }

        /// <summary>
        /// Gets the number of non-disabled mappings of the controller (presentation badge).
        /// </summary>
        public int ActiveSourceCount
        {
            get
            {
                if (Controller?.Mapper?.Mappings == null)
                {
                    return 0;
                }
                return Controller.Mapper.Mappings.Values.Count(mc => mc.Mappers.Any(m => m.Source != null && !(m.Source is DisabledInputSource)));
            }
        }

        /// <summary>
        /// Gets the localized status pill text (e.g. "Active - 12 Source(s)").
        /// </summary>
        public string StatusText
        {
            get
            {
                string key = Started ? "ActiveSources" : "Stopped";
                return string.Format(LanguageModel.Instance.Translate(key), ActiveSourceCount);
            }
        }

        public void RefreshName()
        {
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(ActiveSourceCount));
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
