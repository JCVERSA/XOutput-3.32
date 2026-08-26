using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;

namespace XOutput.WinUI.Animation
{
    /// <summary>
    /// Pointer-driven scale feedback for buttons ("buttery" Discord / Windows 11
    /// feel): the button grows to ~1.02 on hover and dips to ~0.98 while pressed,
    /// using <c>Vector3KeyFrameAnimation</c>s started on the element's composition
    /// <see cref="Visual"/> — i.e. executed on the compositor thread, decoupled
    /// from the UI thread (which is what the WPF approximation of this effect
    /// could not do).
    /// </summary>
    public static class MicroInteractions
    {
        // One feedback controller per button. Weak keys: buttons live and die
        // with their page, so navigating away never leaks an entry.
        private static readonly ConditionalWeakTable<Button, ButtonScaleFeedback> controllers =
            new ConditionalWeakTable<Button, ButtonScaleFeedback>();

        /// <summary>
        /// Attaches hover/press scale feedback to <paramref name="button"/>.
        /// Idempotent; safe to call once per button (e.g. in a page constructor).
        /// </summary>
        /// <param name="hoverScale">Target scale while the pointer is over the button.</param>
        /// <param name="pressedScale">Target scale while the button is pressed.</param>
        public static void AttachScaleFeedback(Button button, double hoverScale = 1.02, double pressedScale = 0.98)
        {
            if (!controllers.TryGetValue(button, out _))
            {
                controllers.Add(button, new ButtonScaleFeedback(button, hoverScale, pressedScale));
            }
        }

        private sealed class ButtonScaleFeedback
        {
            private readonly Button button;
            private readonly float hoverScale;
            private readonly float pressedScale;

            private Visual visual;
            private bool isPointerOver;
            private bool isPointerPressed;

            // One cached animation per target state, created lazily on first use.
            private Vector3KeyFrameAnimation hoverIn;
            private Vector3KeyFrameAnimation hoverOut;
            private Vector3KeyFrameAnimation press;
            private Vector3KeyFrameAnimation release;

            private static readonly TimeSpan HoverInDuration = TimeSpan.FromMilliseconds(120);
            private static readonly TimeSpan HoverOutDuration = TimeSpan.FromMilliseconds(150);
            private static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(80);
            private static readonly TimeSpan ReleaseDuration = TimeSpan.FromMilliseconds(120);

            public ButtonScaleFeedback(Button button, double hoverScale, double pressedScale)
            {
                this.button = button;
                this.hoverScale = (float)hoverScale;
                this.pressedScale = (float)pressedScale;

                button.PointerEntered += OnPointerEntered;
                button.PointerExited += OnPointerExited;
                button.PointerPressed += OnPointerPressed;
                button.PointerReleased += OnPointerReleased;
                button.PointerCanceled += OnPointerCanceled;
                button.SizeChanged += OnSizeChanged;
            }

            private Visual Visual
            {
                get
                {
                    if (visual == null)
                    {
                        visual = ElementCompositionPreview.GetElementVisual(button);
                    }
                    return visual;
                }
            }

            private Vector3KeyFrameAnimation CreateAnimation(float target, TimeSpan duration)
            {
                Vector3KeyFrameAnimation animation = Visual.Compositor.CreateVector3KeyFrameAnimation();
                animation.Target = "Scale";
                animation.Duration = duration;
                // Standard Fluent easing (ease-in-out cubic Bézier).
                animation.EasingFunction = Visual.Compositor.CreateCubicBezierEasingFunction(
                    new Vector2(0.33f, 0f), new Vector2(0.67f, 1f));
                animation.InsertKeyFrame(1f, new Vector3(target, target, 1f));
                return animation;
            }

            private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
            {
                isPointerOver = true;
                Start(isPointerPressed ? press : (hoverIn ??= CreateAnimation(hoverScale, HoverInDuration)));
            }

            private void OnPointerExited(object sender, PointerRoutedEventArgs e)
            {
                isPointerOver = false;
                if (!isPointerPressed)
                {
                    Start(hoverOut ??= CreateAnimation(1f, HoverOutDuration));
                }
            }

            private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
            {
                isPointerPressed = true;
                Start(press ??= CreateAnimation(pressedScale, PressDuration));
            }

            private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
            {
                isPointerPressed = false;
                Start(isPointerOver ? (hoverIn ??= CreateAnimation(hoverScale, HoverInDuration))
                                    : (release ??= CreateAnimation(1f, ReleaseDuration)));
            }

            private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
            {
                isPointerPressed = false;
                isPointerOver = false;
                Start(release ??= CreateAnimation(1f, ReleaseDuration));
            }

            private void OnSizeChanged(object sender, SizeChangedEventArgs e)
            {
                // Scale is centered on the visual's CenterPoint; keep it at the
                // button's center so scaling never looks anchored to a corner.
                float width = (float)button.ActualWidth;
                float height = (float)button.ActualHeight;
                if (width > 0 && height > 0)
                {
                    Visual.CenterPoint = new Vector3(width / 2f, height / 2f, 0f);
                }
            }

            private void Start(Vector3KeyFrameAnimation animation)
            {
                if (animation == null)
                {
                    return;
                }
                // Starting a new animation on "Scale" while one is still running
                // replaces it and continues from the current value — the
                // compositor handles the hand-off, no manual stop needed.
                Visual.StartAnimation("Scale", animation);
            }
        }
    }
}
