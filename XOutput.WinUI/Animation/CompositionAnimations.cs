using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;

namespace XOutput.WinUI.Animation
{
    /// <summary>
    /// Small helpers for <c>Microsoft.UI.Composition</c> animations. These run on
    /// the compositor's independent thread (the same engine that animates the
    /// Windows 11 shell), so they never block or stall on the UI thread — the
    /// actual goal of the WPF → WinUI 3 migration.
    ///
    /// Every API used here was verified against the Windows App SDK 2.x WinRT
    /// reference (learn.microsoft.com) before being written; see
    /// WINUI_ANIMATIONS_REPORT.md for the verification table.
    /// </summary>
    public static class CompositionAnimations
    {
        /// <summary>
        /// Fluent "fast &amp; fluid" duration for subtle content entrances
        /// (within the 167–250 ms range the Windows motion system uses).
        /// </summary>
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(240);

        // Marks elements that already have an entrance attached, so a re-entered
        // element (rare with per-navigation page instances) is not re-hidden.
        // Weak keys: containers are garbage-collected with their page.
        private static readonly ConditionalWeakTable<FrameworkElement, object> entranceApplied =
            new ConditionalWeakTable<FrameworkElement, object>();

        /// <summary>
        /// Attaches implicit composition animations to <paramref name="element"/>
        /// (fade + slide on <c>Opacity</c>/<c>Offset</c> changes) and starts a
        /// one-shot fade + slide-in entrance. Because the <c>Offset</c> implicit
        /// animation reacts to *any* offset change, later layout-driven position
        /// changes — e.g. remaining list items shifting when a device is removed —
        /// also animate smoothly instead of jumping.
        /// </summary>
        /// <param name="element">The element to animate (e.g. a list item container or card).</param>
        /// <param name="slideOffset">DIPs the element starts below its final position.</param>
        public static void AttachEntrance(FrameworkElement element, double slideOffset = 10.0)
        {
            if (entranceApplied.TryGetValue(element, out _))
            {
                return;
            }
            entranceApplied.Add(element, null);

            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            Compositor compositor = visual.Compositor;

            // Apply the "from" state BEFORE the implicit animations are attached,
            // otherwise the 1 → 0 transition would itself trigger (and visibly
            // play) an implicit fade-out.
            Vector3 originalOffset = visual.Offset;
            visual.Opacity = 0f;
            visual.Offset = originalOffset + new Vector3(0f, (float)slideOffset, 0f);

            // Fade to the new value whenever Opacity changes. "This.FinalValue"
            // is the expression for "the value the property was changed to",
            // which is what implicit animations animate towards.
            ScalarKeyFrameAnimation fade = compositor.CreateScalarKeyFrameAnimation();
            fade.Target = "Opacity";
            fade.Duration = DefaultDuration;
            fade.InsertExpressionKeyFrame(1f, "This.FinalValue");

            // Slide to the new value whenever Offset changes (covers both the
            // entrance and later layout-driven repositioning).
            Vector3KeyFrameAnimation slide = compositor.CreateVector3KeyFrameAnimation();
            slide.Target = "Offset";
            slide.Duration = DefaultDuration;
            slide.InsertExpressionKeyFrame(1f, "This.FinalValue");

            ImplicitAnimationCollection implicitAnimations = compositor.CreateImplicitAnimationCollection();
            implicitAnimations["Opacity"] = fade;
            implicitAnimations["Offset"] = slide;
            visual.ImplicitAnimations = implicitAnimations;

            // Reveal on the next UI pass: implicit animations only fire when the
            // compositor observes a property change between commits, so the
            // hidden → final change must not happen in the same commit as the
            // initial hidden state.
            element.DispatcherQueue.TryEnqueue(() =>
            {
                visual.Opacity = 1f;
                visual.Offset = originalOffset;
            });
        }
    }
}
