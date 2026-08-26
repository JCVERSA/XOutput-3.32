using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI;

namespace XOutput.WinUI.Controls
{
    /// <summary>
    /// Small connected/disconnected status dot rendered entirely by the
    /// compositor (ShapeVisual + CompositionColorBrush). The color change is
    /// animated with a <c>ColorKeyFrameAnimation</c> on the brush — a real
    /// compositor-thread color transition — instead of an instant brush swap.
    /// </summary>
    public sealed partial class StatusDot : UserControl
    {
        /// <summary>Color used for the connected state (matches the old WPF-era converter values).</summary>
        public static readonly Color ConnectedColor = Color.FromArgb(255, 90, 200, 90);
        /// <summary>Color used for the disconnected state.</summary>
        public static readonly Color DisconnectedColor = Color.FromArgb(255, 160, 160, 160);

        /// <summary>
        /// Gets or sets whether the dot shows the "connected" color. Changing the
        /// value animates the color over <see cref="ColorAnimationDuration"/>.
        /// </summary>
        public bool IsConnected
        {
            get => (bool)GetValue(IsConnectedProperty);
            set => SetValue(IsConnectedProperty, value);
        }
        public static readonly DependencyProperty IsConnectedProperty =
            DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(StatusDot),
                new PropertyMetadata(false, OnIsConnectedChanged));

        private static readonly TimeSpan ColorAnimationDuration = TimeSpan.FromMilliseconds(250);

        private ShapeVisual dotVisual;
        private CompositionColorBrush dotBrush;
        private CompositionRoundedRectangleGeometry dotGeometry;

        public StatusDot()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            EnsureVisual();
        }

        private static void OnIsConnectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((StatusDot)d).OnIsConnectedChanged();
        }

        private void OnIsConnectedChanged()
        {
            if (dotBrush == null)
            {
                // Visual not created yet (before Loaded): EnsureVisual reads the
                // current IsConnected value, so the initial color is correct
                // without animating.
                return;
            }
            AnimateColor(IsConnected ? ConnectedColor : DisconnectedColor);
        }

        private void EnsureVisual()
        {
            if (dotVisual != null)
            {
                return;
            }
            Compositor compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

            // Rounded rectangle the size of the control (corner radius = half the
            // size → a circle for the default 10×10 dot).
            dotGeometry = compositor.CreateRoundedRectangleGeometry();
            dotGeometry.CornerRadius = new Vector2(5f, 5f);

            dotBrush = compositor.CreateColorBrush();
            dotBrush.Color = IsConnected ? ConnectedColor : DisconnectedColor;

            CompositionSpriteShape shape = compositor.CreateSpriteShape(dotGeometry);
            shape.FillBrush = dotBrush;

            dotVisual = compositor.CreateShapeVisual();
            dotVisual.Shapes.Add(shape);

            // "Last child of the element's visual tree" → drawn on top of the
            // (empty) host grid, which is exactly the dot.
            ElementCompositionPreview.SetElementChildVisual(DotHost, dotVisual);

            UpdateSize();
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        private void UpdateSize()
        {
            if (dotVisual == null)
            {
                return;
            }
            float width = Math.Max(1f, (float)ActualWidth);
            float height = Math.Max(1f, (float)ActualHeight);
            dotGeometry.Size = new Vector2(width, height);
            dotVisual.Size = new Vector2(width, height);
        }

        private void AnimateColor(Color target)
        {
            ColorKeyFrameAnimation animation = dotBrush.Compositor.CreateColorKeyFrameAnimation();
            animation.Target = "Color";
            animation.Duration = ColorAnimationDuration;
            animation.InsertKeyFrame(1f, target);
            dotBrush.StartAnimation("Color", animation);
        }
    }
}
