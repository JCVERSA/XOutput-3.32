using System.Windows;
using System.Windows.Controls;
using XOutput.Devices.XInput;

namespace XOutput.UI.Component
{
    /// <summary>
    /// Interaction logic for XBox.xaml
    /// </summary>
    public partial class XBox : Viewbox, IViewBase<XBoxViewModel, XBoxModel>
    {
        public static readonly DependencyProperty XInputTypeProperty = DependencyProperty.Register("XInputType", typeof(XInputTypes), typeof(XBox), new FrameworkPropertyMetadata(OnXInputTypeChanged, null));
        public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight", typeof(bool), typeof(XBox), new FrameworkPropertyMetadata(OnHightlightChanged, null));

        /// <summary>
        /// Identifies the <see cref="LiveTarget"/> dependency property: the XInput
        /// control whose live value is being previewed (L2/R2/LX/LY/RX/RY).
        /// </summary>
        public static readonly DependencyProperty LiveTargetProperty = DependencyProperty.Register("LiveTarget", typeof(XInputTypes), typeof(XBox), new FrameworkPropertyMetadata(OnLiveValueChanged, null));

        /// <summary>
        /// Identifies the <see cref="LiveValue"/> dependency property: the normalized
        /// 0.0-1.0 live value of the input mapped to <see cref="LiveTarget"/>.
        /// </summary>
        public static readonly DependencyProperty LiveValueProperty = DependencyProperty.Register("LiveValue", typeof(double), typeof(XBox), new FrameworkPropertyMetadata(0d, OnLiveValueChanged, null));

        private static void OnXInputTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var xbox = (XBox)d;
            xbox.ViewModel.Model.XInputType = (XInputTypes)e.NewValue;
        }

        private static void OnHightlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var xbox = (XBox)d;
            xbox.ViewModel.Model.Highlight = (bool)e.NewValue;
        }

        private static void OnLiveValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((XBox)d).ApplyLiveValue();
        }

        public XInputTypes XInputType
        {
            get { return (XInputTypes)GetValue(XInputTypeProperty); }
            set { SetValue(XInputTypeProperty, value); ViewModel.Model.XInputType = value; }
        }
        public bool Highlight
        {
            get { return (bool)GetValue(HighlightProperty); }
            set { SetValue(HighlightProperty, value); ViewModel.Model.Highlight = value; }
        }

        /// <summary>
        /// Gets or sets the XInput control whose live value is previewed.
        /// </summary>
        public XInputTypes LiveTarget
        {
            get { return (XInputTypes)GetValue(LiveTargetProperty); }
            set { SetValue(LiveTargetProperty, value); }
        }

        /// <summary>
        /// Gets or sets the normalized 0.0-1.0 live value of the input mapped to
        /// <see cref="LiveTarget"/>. Drives the continuous trigger fill and the
        /// positional stick movement of the visualization.
        /// </summary>
        public double LiveValue
        {
            get { return (double)GetValue(LiveValueProperty); }
            set { SetValue(LiveValueProperty, value); }
        }

        protected readonly XBoxViewModel viewModel;
        public XBoxViewModel ViewModel => viewModel;

        public XBox()
        {
            viewModel = new XBoxViewModel(new XBoxModel());
            DataContext = viewModel;
            InitializeComponent();
        }

        /// <summary>
        /// Routes the current <see cref="LiveValue"/> into the model property for the
        /// current <see cref="LiveTarget"/>. All analog values are reset first so a
        /// stale value from a previous target never lingers (e.g. when the wizard
        /// advances to the next step). Digital targets route nowhere.
        /// </summary>
        private void ApplyLiveValue()
        {
            XBoxModel model = ViewModel.Model;
            model.LeftTrigger = 0;
            model.RightTrigger = 0;
            model.LeftStickX = 0.5;
            model.LeftStickY = 0.5;
            model.RightStickX = 0.5;
            model.RightStickY = 0.5;
            double v = LiveValue;
            switch (LiveTarget)
            {
                case XInputTypes.L2:
                    model.LeftTrigger = v;
                    break;
                case XInputTypes.R2:
                    model.RightTrigger = v;
                    break;
                case XInputTypes.LX:
                    model.LeftStickX = v;
                    break;
                case XInputTypes.LY:
                    model.LeftStickY = v;
                    break;
                case XInputTypes.RX:
                    model.RightStickX = v;
                    break;
                case XInputTypes.RY:
                    model.RightStickY = v;
                    break;
                default:
                    break;
            }
        }
    }
}
