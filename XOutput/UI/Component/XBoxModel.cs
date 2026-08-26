using XOutput.Devices.XInput;

namespace XOutput.UI
{
    public class XBoxModel : ModelBase
    {
        private XInputTypes xInputType;
        public XInputTypes XInputType
        {
            get => xInputType;
            set
            {
                if (xInputType != value)
                {
                    xInputType = value;
                    OnPropertyChanged(nameof(XInputType));
                }
            }
        }

        private bool highlight;
        public bool Highlight
        {
            get => highlight;
            set
            {
                if (highlight != value)
                {
                    highlight = value;
                    OnPropertyChanged(nameof(Highlight));
                }
            }
        }

        // ---- Live analog values (0.0-1.0), used by the reactive visualization ----
        // Triggers: 0 = released (invisible/outline only), 1 = fully pulled.
        private double leftTrigger;
        public double LeftTrigger
        {
            get => leftTrigger;
            set
            {
                double v = Clamp01(value);
                if (leftTrigger != v)
                {
                    leftTrigger = v;
                    OnPropertyChanged(nameof(LeftTrigger));
                }
            }
        }

        private double rightTrigger;
        public double RightTrigger
        {
            get => rightTrigger;
            set
            {
                double v = Clamp01(value);
                if (rightTrigger != v)
                {
                    rightTrigger = v;
                    OnPropertyChanged(nameof(RightTrigger));
                }
            }
        }

        // Sticks: 0.5 = neutral/center, 0..1 across the axis range.
        private double leftStickX;
        public double LeftStickX
        {
            get => leftStickX;
            set
            {
                double v = Clamp01(value);
                if (leftStickX != v)
                {
                    leftStickX = v;
                    OnPropertyChanged(nameof(LeftStickX));
                }
            }
        }

        private double leftStickY;
        public double LeftStickY
        {
            get => leftStickY;
            set
            {
                double v = Clamp01(value);
                if (leftStickY != v)
                {
                    leftStickY = v;
                    OnPropertyChanged(nameof(LeftStickY));
                }
            }
        }

        private double rightStickX;
        public double RightStickX
        {
            get => rightStickX;
            set
            {
                double v = Clamp01(value);
                if (rightStickX != v)
                {
                    rightStickX = v;
                    OnPropertyChanged(nameof(RightStickX));
                }
            }
        }

        private double rightStickY;
        public double RightStickY
        {
            get => rightStickY;
            set
            {
                double v = Clamp01(value);
                if (rightStickY != v)
                {
                    rightStickY = v;
                    OnPropertyChanged(nameof(RightStickY));
                }
            }
        }

        private static double Clamp01(double value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
