using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using XOutput.Devices;
using XOutput.Devices.Input;
using XOutput.Devices.Mapper;
using XOutput.Devices.XInput;
using XOutput.Tools;
using XOutput.UI.Component;
using XOutput.UI.Windows;

namespace XOutput.UI.Shell.Pages
{
    /// <summary>
    /// One device tab of the mapping page. Wraps the existing per-device
    /// <see cref="InputSettingsViewModel"/> (live input test, force feedback,
    /// HidGuardian) — all bindings/behavior are reused unchanged.
    /// </summary>
    public sealed class MappingDeviceTab : INotifyPropertyChanged
    {
        public IInputDevice Device { get; }
        public InputSettingsViewModel ViewModel { get; }
        public string Title => Device.DisplayName;

        /// <summary>
        /// Exposed for language bindings inside the tab content.
        /// </summary>
        public LanguageModel LanguageModel => LanguageModel.Instance;

        /// <summary>Live axis/slider views of the device.</summary>
        public ObservableCollection<IUpdatableView> Axes => ViewModel.Model.InputAxisViews;
        /// <summary>Live button views of the device.</summary>
        public ObservableCollection<IUpdatableView> Buttons => ViewModel.Model.InputButtonViews;
        /// <summary>Live dpad views of the device.</summary>
        public ObservableCollection<IUpdatableView> DPads => ViewModel.Model.InputDPadViews;

        public MappingDeviceTab(IInputDevice device, InputSettingsViewModel viewModel)
        {
            Device = device;
            ViewModel = viewModel;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// The "Virtual Gamepad" tab of the mapping page (informational).
    /// </summary>
    public sealed class VirtualGamepadTab
    {
        public string Title => LanguageModel.Instance.Translate("VirtualGamepad");
    }

    /// <summary>
    /// Presentation view model for the mapping page: device tabs, physical input
    /// listing, and a per-input configure panel that reads/writes the existing
    /// <see cref="MapperData"/> of the first virtual controller (no engine changes).
    /// </summary>
    public sealed class MappingPageViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly MainWindowViewModel mainViewModel;
        private readonly Dictionary<IInputDevice, MappingDeviceTab> tabsByDevice = new Dictionary<IInputDevice, MappingDeviceTab>();
        private InputMapper controllerMapper;
        private InputMapper lastControllerMapper;
        private MapperData currentMapperData;
        private bool disposed = false;

        /// <summary>
        /// Gets the tabs of the page (one per connected device + Virtual Gamepad).
        /// </summary>
        public ObservableCollection<object> Tabs { get; } = new ObservableCollection<object>();

        /// <summary>
        /// Gets or sets the selected tab.
        /// </summary>
        public object SelectedTab
        {
            get => selectedTab;
            set
            {
                if (selectedTab != value)
                {
                    selectedTab = value;
                    OnPropertyChanged(nameof(SelectedTab));
                    if (value is MappingDeviceTab)
                    {
                        RefreshSources();
                    }
                }
            }
        }
        private object selectedTab;

        /// <summary>
        /// Gets the sources of the selected device (configure input selector).
        /// </summary>
        public ObservableCollection<InputSource> Sources { get; } = new ObservableCollection<InputSource>();

        /// <summary>
        /// Gets or sets the physical input currently being configured.
        /// </summary>
        public InputSource SelectedSource
        {
            get => selectedSource;
            set
            {
                if (selectedSource != value)
                {
                    selectedSource = value;
                    OnPropertyChanged(nameof(SelectedSource));
                    OnPropertyChanged(nameof(RawValue));
                    RefreshCurrentMapperData();
                }
            }
        }
        private InputSource selectedSource;

        /// <summary>
        /// Gets the available XInput targets for the target dropdown.
        /// </summary>
        public IReadOnlyList<XInputTypes> Targets { get; } = XInputHelper.Instance.Values.ToArray();

        /// <summary>
        /// Gets or sets the XInput target the selected input maps to.
        /// Changing the target writes the mapping (same write path the existing
        /// mapping editor uses: <see cref="MapperData.Source"/>).
        /// </summary>
        public XInputTypes? SelectedTarget
        {
            get => selectedTarget;
            set
            {
                if (selectedTarget != value)
                {
                    selectedTarget = value;
                    OnPropertyChanged(nameof(SelectedTarget));
                    ApplyTargetSelection();
                }
            }
        }
        private XInputTypes? selectedTarget;

        /// <summary>
        /// Gets or sets the deadzone of the current mapping (0-50 %).
        /// </summary>
        public double DeadzonePercent
        {
            get => currentMapperData == null ? 0 : currentMapperData.Deadzone * 100;
            set
            {
                if (currentMapperData != null && !Helper.DoubleEquals(currentMapperData.Deadzone * 100, value))
                {
                    currentMapperData.Deadzone = value / 100;
                    OnPropertyChanged(nameof(DeadzonePercent));
                    OnPropertyChanged(nameof(MappedValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the current mapping is inverted (min/max swapped).
        /// </summary>
        public bool Invert
        {
            get => currentMapperData != null && currentMapperData.MinValue > currentMapperData.MaxValue;
            set
            {
                if (currentMapperData != null && value != Invert)
                {
                    double temp = currentMapperData.MinValue;
                    currentMapperData.MinValue = currentMapperData.MaxValue;
                    currentMapperData.MaxValue = temp;
                    OnPropertyChanged(nameof(Invert));
                    OnPropertyChanged(nameof(MappedValue));
                }
            }
        }

        /// <summary>
        /// Gets the raw live value of the selected input (0-1).
        /// </summary>
        public double RawValue => SelectedSource?.Value ?? 0;

        /// <summary>
        /// Gets the mapped live value of the selected input (0-1).
        /// </summary>
        public double MappedValue => currentMapperData == null ? 0 : currentMapperData.GetValue(RawValue);

        /// <summary>
        /// True when a virtual controller exists to store mappings.
        /// </summary>
        public bool HasController => controllerMapper != null;

        /// <summary>
        /// True when the selected source currently has a mapping for the selected target.
        /// </summary>
        public bool HasMapping => currentMapperData != null;

        /// <summary>
        /// Raised when the last physical device tab is removed because its device
        /// disconnected (the page then navigates home). Individual tabs are removed
        /// in place without navigating away.
        /// </summary>
        public event Action DeviceDisconnected;

        public MappingPageViewModel(MainWindowViewModel mainViewModel, IInputDevice initialDevice)
        {
            this.mainViewModel = mainViewModel;
            HidGuardianManager hidGuardianManager = ApplicationContext.Global.Resolve<HidGuardianManager>();
            foreach (var device in InputDevices.Instance.GetDevices())
            {
                InputSettingsViewModel deviceViewModel = new InputSettingsViewModel(new InputSettingsModel(), hidGuardianManager, device, mainViewModel.Model.IsAdmin);
                MappingDeviceTab tab = new MappingDeviceTab(device, deviceViewModel);
                tabsByDevice[device] = tab;
                Tabs.Add(tab);
                device.Disconnected += Device_Disconnected;
            }
            Tabs.Add(new VirtualGamepadTab());
            controllerMapper = Controllers.Instance.GetControllers().FirstOrDefault()?.Mapper;
            lastControllerMapper = controllerMapper;
            if (Tabs.Count > 1)
            {
                SelectedTab = initialDevice != null && tabsByDevice.ContainsKey(initialDevice)
                    ? tabsByDevice[initialDevice]
                    : Tabs[0];
            }
        }

        /// <summary>
        /// Refreshes the live views and the configure preview (called by the page timer).
        /// Also re-resolves the target mapper so edits keep working when controllers
        /// are added or removed while the page is open.
        /// </summary>
        public void Update()
        {
            if (disposed)
            {
                return;
            }
            InputMapper mapper = Controllers.Instance.GetControllers().FirstOrDefault()?.Mapper;
            if (!ReferenceEquals(mapper, lastControllerMapper))
            {
                lastControllerMapper = mapper;
                controllerMapper = mapper;
                RefreshCurrentMapperData();
                RaiseConfigureChanged();
            }
            if (SelectedTab is MappingDeviceTab deviceTab)
            {
                deviceTab.ViewModel.Update();
            }
            OnPropertyChanged(nameof(RawValue));
            OnPropertyChanged(nameof(MappedValue));
        }

        /// <summary>
        /// Saves the settings file (Save Mapping action).
        /// </summary>
        public void SaveMapping()
        {
            mainViewModel.SaveSettings();
        }

        /// <summary>
        /// Copies the current controller mapping profile as JSON to the clipboard.
        /// </summary>
        public void ExportProfile()
        {
            if (controllerMapper == null)
            {
                return;
            }
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(controllerMapper, Newtonsoft.Json.Formatting.Indented);
                System.Windows.Clipboard.SetText(json);
            }
            catch (Exception)
            {
                // Clipboard may be unavailable (e.g. non-interactive session)
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            foreach (var pair in tabsByDevice)
            {
                pair.Key.Disconnected -= Device_Disconnected;
                pair.Value.ViewModel.Dispose();
            }
            tabsByDevice.Clear();
        }

        private void Device_Disconnected(object sender, DeviceDisconnectedEventArgs e)
        {
            if (!(sender is IInputDevice device))
            {
                return;
            }
            // Disconnected fires on the input reader thread; marshal to the UI thread.
            System.Windows.Application.Current?.Dispatcher?.BeginInvoke((Action)(() =>
            {
                if (disposed || !tabsByDevice.TryGetValue(device, out var tab))
                {
                    return;
                }
                Tabs.Remove(tab);
                device.Disconnected -= Device_Disconnected;
                tab.ViewModel.Dispose();
                tabsByDevice.Remove(device);
                if (SelectedTab == tab)
                {
                    MappingDeviceTab next = Tabs.OfType<MappingDeviceTab>().FirstOrDefault();
                    if (next != null)
                    {
                        SelectedTab = next;
                    }
                    else
                    {
                        // No physical device left — leave the mapping page.
                        DeviceDisconnected?.Invoke();
                    }
                }
            }));
        }

        private void RefreshSources()
        {
            Sources.Clear();
            if (SelectedTab is MappingDeviceTab deviceTab)
            {
                foreach (var source in deviceTab.Device.Sources)
                {
                    Sources.Add(source);
                }
            }
            SelectedSource = Sources.FirstOrDefault();
            OnPropertyChanged(nameof(Sources));
        }

        /// <summary>
        /// Resolves (read-only) the mapper data for the current source + target.
        /// </summary>
        private void RefreshCurrentMapperData()
        {
            currentMapperData = null;
            if (controllerMapper == null || SelectedSource == null)
            {
                RaiseConfigureChanged();
                return;
            }
            // Convenience: auto-select the target this source is already mapped to.
            if (SelectedTarget == null)
            {
                selectedTarget = FindMappedTarget(SelectedSource);
                OnPropertyChanged(nameof(SelectedTarget));
            }
            if (SelectedTarget != null)
            {
                MapperData data = controllerMapper.GetMapping(SelectedTarget.Value)?.Mappers.FirstOrDefault();
                if (data != null && data.Source == SelectedSource)
                {
                    currentMapperData = data;
                }
            }
            RaiseConfigureChanged();
        }

        /// <summary>
        /// Called when the user changes the target dropdown: maps the selected
        /// physical input to the chosen XInput control (existing engine write).
        /// </summary>
        private void ApplyTargetSelection()
        {
            if (controllerMapper == null || SelectedSource == null || SelectedTarget == null)
            {
                RefreshCurrentMapperData();
                return;
            }
            MapperData data = controllerMapper.GetMapping(SelectedTarget.Value)?.Mappers.FirstOrDefault();
            if (data != null)
            {
                data.Source = SelectedSource;
                currentMapperData = data;
                RaiseConfigureChanged();
            }
        }

        private XInputTypes? FindMappedTarget(InputSource source)
        {
            if (controllerMapper == null)
            {
                return null;
            }
            foreach (var target in Targets)
            {
                MapperData data = controllerMapper.GetMapping(target)?.Mappers.FirstOrDefault();
                if (data != null && data.Source == source)
                {
                    return target;
                }
            }
            return null;
        }

        private void RaiseConfigureChanged()
        {
            OnPropertyChanged(nameof(HasController));
            OnPropertyChanged(nameof(HasMapping));
            OnPropertyChanged(nameof(DeadzonePercent));
            OnPropertyChanged(nameof(Invert));
            OnPropertyChanged(nameof(MappedValue));
            OnPropertyChanged(nameof(RawValue));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
