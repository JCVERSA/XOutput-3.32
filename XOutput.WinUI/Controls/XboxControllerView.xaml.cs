using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using XOutput.Devices.XInput;
using XOutput.UI;

namespace XOutput.WinUI.Controls
{
    /// <summary>
    /// Live-reactive controller visualization (WinUI port of the WPF XBox component).
    /// Drives a shared <see cref="XBoxModel"/> from LiveTarget / LiveValue / Highlight
    /// dependency properties and renders continuous trigger opacity, positional stick
    /// movement and digital-button highlight into the shapes in XAML.
    /// </summary>
    public sealed partial class XboxControllerView : UserControl
    {
        private const double StickMaxOffset = 6.0;

        private readonly XBoxModel model = new XBoxModel();

        /// <summary>The XInput control whose live value is previewed.</summary>
        public XInputTypes LiveTarget
        {
            get => (XInputTypes)GetValue(LiveTargetProperty);
            set => SetValue(LiveTargetProperty, value);
        }
        public static readonly DependencyProperty LiveTargetProperty =
            DependencyProperty.Register(nameof(LiveTarget), typeof(XInputTypes), typeof(XboxControllerView),
                new PropertyMetadata(default(XInputTypes), OnLiveChanged));

        /// <summary>Normalized 0.0-1.0 live value of the input mapped to <see cref="LiveTarget"/>.</summary>
        public double LiveValue
        {
            get => (double)GetValue(LiveValueProperty);
            set => SetValue(LiveValueProperty, value);
        }
        public static readonly DependencyProperty LiveValueProperty =
            DependencyProperty.Register(nameof(LiveValue), typeof(double), typeof(XboxControllerView),
                new PropertyMetadata(0d, OnLiveChanged));

        /// <summary>Blink highlight for digital buttons (wizard guidance).</summary>
        public bool Highlight
        {
            get => (bool)GetValue(HighlightProperty);
            set => SetValue(HighlightProperty, value);
        }
        public static readonly DependencyProperty HighlightProperty =
            DependencyProperty.Register(nameof(Highlight), typeof(bool), typeof(XboxControllerView),
                new PropertyMetadata(false, OnHighlightChanged));

        private static void OnLiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((XboxControllerView)d).ApplyLiveValue();

        private static void OnHighlightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((XboxControllerView)d).ApplyHighlight();

        public XboxControllerView()
        {
            this.InitializeComponent();
            ApplyAll();
        }

        private void ApplyLiveValue()
        {
            // Reset all analog values first so a stale value never lingers.
            model.LeftTrigger = 0;
            model.RightTrigger = 0;
            model.LeftStickX = 0.5;
            model.LeftStickY = 0.5;
            model.RightStickX = 0.5;
            model.RightStickY = 0.5;
            double v = LiveValue;
            switch (LiveTarget)
            {
                case XInputTypes.L2: model.LeftTrigger = v; break;
                case XInputTypes.R2: model.RightTrigger = v; break;
                case XInputTypes.LX: model.LeftStickX = v; break;
                case XInputTypes.LY: model.LeftStickY = v; break;
                case XInputTypes.RX: model.RightStickX = v; break;
                case XInputTypes.RY: model.RightStickY = v; break;
            }
            ApplyAnalog();
        }

        private void ApplyAnalog()
        {
            LT.Opacity = model.LeftTrigger;
            RT.Opacity = model.RightTrigger;

            LGlowTransform.X = (model.LeftStickX - 0.5) * 2 * StickMaxOffset;
            LGlowTransform.Y = (0.5 - model.LeftStickY) * 2 * StickMaxOffset;
            LCapTransform.X = LGlowTransform.X;
            LCapTransform.Y = LGlowTransform.Y;

            RGlowTransform.X = (model.RightStickX - 0.5) * 2 * StickMaxOffset;
            RGlowTransform.Y = (0.5 - model.RightStickY) * 2 * StickMaxOffset;
            RCapTransform.X = RGlowTransform.X;
            RCapTransform.Y = RGlowTransform.Y;
        }

        private void ApplyHighlight()
        {
            double on = Highlight ? 0.9 : 0.0;
            double dim = Highlight ? 0.35 : 0.0;
            SetDigital(BtnA, XInputTypes.A, on, dim);
            SetDigital(BtnB, XInputTypes.B, on, dim);
            SetDigital(BtnX, XInputTypes.X, on, dim);
            SetDigital(BtnY, XInputTypes.Y, on, dim);
            SetDigital(DpadUp, XInputTypes.UP, on, dim);
            SetDigital(DpadDown, XInputTypes.DOWN, on, dim);
            SetDigital(DpadLeft, XInputTypes.LEFT, on, dim);
            SetDigital(DpadRight, XInputTypes.RIGHT, on, dim);
            SetDigital(BtnLB, XInputTypes.L1, on, dim);
            SetDigital(BtnRB, XInputTypes.R1, on, dim);
            SetDigital(LT, XInputTypes.L2, on, dim);
            SetDigital(RT, XInputTypes.R2, on, dim);
        }

        private void SetDigital(UIElement element, XInputTypes type, double on, double dim)
        {
            if (LiveTarget == type)
            {
                element.Opacity = Highlight ? on : dim;
            }
            else if (!(element == LT || element == RT))
            {
                element.Opacity = 0;
            }
        }

        private void ApplyAll()
        {
            ApplyLiveValue();
            ApplyHighlight();
        }
    }
}
