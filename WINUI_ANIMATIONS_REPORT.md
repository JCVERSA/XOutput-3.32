# WinUI 3 Compositor Animations — Change Report (Prompt 4)

> **Scope of this prompt:** add the native compositor-thread animations that were the
> point of migrating XOutput from WPF to WinUI 3 — page transitions, implicit
> animations for list/card content, button/badge micro-interactions — plus a full
> validation pass and an honest per-file change report.
>
> **Branch:** `arena/01a01cdd-xoutput-3-32` (commits `c1d012f` + this prompt's work)
>
> **Validation honesty:** this sandbox is Linux with no .NET SDK and no Windows
> runtime, so **everything below that requires Windows is UNVERIFIED here**. What
> *was* verified: every composition API used was checked against the Windows App SDK
> 2.x WinRT reference (learn.microsoft.com) and/or Microsoft's own sample code
> (WinUI-Gallery, WindowsAppSDK-Samples); all C# parses cleanly with tree-sitter;
> all XAML is well-formed XML. Section 7 lists exactly what you must run on Windows.

---

## 1. Page transitions (Section 1 of the prompt)

**Status: confirmed + explicitly guaranteed, not replaced.**

- WinUI 3's `Frame` animates navigation with `NavigationThemeTransition` **by
  default** — the WinRT docs state: *"With Windows 10, version 1803, a Frame uses
  NavigationThemeTransition to animate navigation between Pages by default."*
- To guarantee the transition for every page (and to make the guarantee visible in
  code rather than implicit), `MainWindow.xaml` now declares it explicitly on the
  `Frame`:

  ```xml
  <Frame x:Name="ContentFrame">
      <Frame.ContentTransitions>
          <TransitionCollection>
              <NavigationThemeTransition/>
          </TransitionCollection>
      </Frame.ContentTransitions>
  </Frame>
  ```

- No duration/easing overrides: the default **is** the Fluent-correct behavior
  (slide-in + fade, ~300 ms system-driven). Nothing slower or cheaper was
  substituted.

---

## 2. Implicit animations for list/card content (Section 2)

### What we did

New helper `XOutput.WinUI/Animation/CompositionAnimations.cs` implements the
requested **`Visual.ImplicitAnimations`** pattern (raw `Microsoft.UI.Composition`,
no new NuGet dependencies):

- Each element gets an `ImplicitAnimationCollection` mapping `Opacity` → fade and
  `Offset` → slide, using `InsertExpressionKeyFrame(1f, "This.FinalValue")` so the
  animation always targets the value the property was changed to (verified against
  Microsoft's `ImplicitAnimationTransformer` sample).
- The element starts 10 px below its final position at opacity 0, and is revealed
  on the **next UI pass** (deferred via `DispatcherQueue.TryEnqueue`) so the
  compositor observes a real property change between commits — that is the trigger
  for implicit animations.
- Because the `Offset` animation is implicit, *any* later layout-driven offset
  change also animates smoothly — including the other items shifting when one is
  removed (Fluent "reposition" behavior), which covers the "subtle offset on
  remove" half of the prompt's ask even though we do not animate the removed
  container's fade-out (see §2.3 and §8).

Applied to:

| Element | Where | Effect |
|---|---|---|
| Input Devices item containers | `HomePage.xaml` `DeviceItem_Loaded` | fade + 10 px slide-in on appear |
| Virtual Controllers item containers | `HomePage.xaml` `ControllerItem_Loaded` | same (list populates once controller creation is wired — next prompt) |
| Home card + Controllers card + status bar | `HomePage.xaml.cs` `Page_Loaded` | subtle card entrance |

### 2.1 A research finding worth recording

The prompt said to "check current recommended approach". The classic
`ItemsRepeater` + `ElementAnimator`/`DefaultAnimator` API (WinUI 2.x) has been
**removed from current WinUI**: the WinUI source (`controls/dev/Repeater/`) now
defines `ItemCollectionTransitionProvider`/`ItemCollectionTransition` and
`ItemsRepeater.ItemTransitionProvider` instead, and only `LinedFlowLayout`
supplies a default provider (`StackLayout` returns `nullptr`). We deliberately did
**not** hand-roll a custom `ItemCollectionTransitionProvider` subclass — it is a
complex API (transition progress objects, animation batches, completion
handshakes) and, unlike `Visual.ImplicitAnimations`, it is *not* what the prompt
named. `Visual.ImplicitAnimations` is the more maintainable choice here and does
exactly the requested "fade + subtle offset" for the lists.

### 2.2 NavigationView selection indicator

**Confirmed native, not overridden.** The selection indicator pill is animated by
the NavigationView control itself (its default style drives the indicator's
movement with the standard Fluent timing). Overriding it would risk the
appearance/feel of a core Win11 control for zero benefit.

### 2.3 ContentDialog open/close

**Confirmed native, not overridden.** `ContentDialog` plays its own scale + fade
open/close animation (the control's built-in theme animation). The wizard steps
inside `AddControllerDialog` swap content without extra animation — also per the
prompt ("confirm rather than override").

---

## 3. Micro-interactions (Section 3)

### 3.1 Button press/hover scale — `XOutput.WinUI/Animation/MicroInteractions.cs` (new)

- `MicroInteractions.AttachScaleFeedback(button)` hooks `PointerEntered/Exited/
  Pressed/Released/Canceled` and `SizeChanged`, then starts cached
  `Vector3KeyFrameAnimation`s on the button's composition **Visual** (via
  `ElementCompositionPreview.GetElementVisual`).
- Targets: hover `1.02`, press `0.98`, release/unhover back to `1.0` — exactly the
  values requested. Durations: press **80 ms**, hover-in **120 ms**, release
  **120 ms**, hover-out **150 ms** (all within Fluent fast-and-fluid range), with
  the standard cubic-Bézier easing `(0.33, 0) → (0.67, 1)`.
- `CenterPoint` is kept at the button's center on every `SizeChanged`, so scaling
  is never anchored to a corner.
- Animations are cached per state; the state machine handles the hover-press-
  release sequencing (including press while hovering and pointer cancel).
- **Applied to:** Home → *Add Controller*, Diagnostics → *Export report*.
  NavigationView items and ContentDialog buttons keep their native
  hover/press feedback (overriding those would fight the control templates).

Why this is the real thing: these keyframes run on the **compositor's independent
thread**. In WPF, an equivalent effect was a `RenderTransform`/`Storyboard`
approximation driven from the Dispatcher — the animation itself stuttered whenever
the UI thread was busy polling DirectInput. Here the animation is scheduled and
executed by the compositor; UI-thread load (polling, layout, GC) cannot stall it.

### 3.2 Status pill/badge color — `XOutput.WinUI/Controls/StatusDot.xaml(.cs)` (new)

- The device status dot in the Input Devices list is now **rendered entirely by
  the compositor**: a `ShapeVisual` with a `CompositionSpriteShape` +
  `CompositionRoundedRectangleGeometry` + `CompositionColorBrush`, attached via
  `ElementCompositionPreview.SetElementChildVisual`.
- The Connected/Disconnected color change animates with a real
  **`ColorKeyFrameAnimation` on the brush's `Color`** property (250 ms) instead of
  an instant `SolidColorBrush` swap.
- `StatusDot.IsConnected` is a bindable dependency property, so the DataTemplate
  binding from Prompt 3 is unchanged: `IsConnected="{Binding Connected}"`.
- Colors kept identical to the previous converter values (green 90/200/90, gray
  160/160/160) so there is no visual regression.
- The old `BoolToColorConverter` is now unused by any binding (nothing else
  references it); it is **retained** as a plain-brush utility in case a later
  prompt needs a non-compositor color mapping — flagged here so it can be deleted
  if it stays unused.

### 3.3 Controller visualization (XboxControllerView)

**Kept as the direct data-binding from Prompt 3** — unchanged. Per the prompt, the
smoothing option is noted rather than built:

> *Option (not implemented):* wrap the stick movement/trigger fill in composition
> interpolation (e.g. animate the visual `Offset`/`Opacity` toward the polled
> value with a short keyframe or spring). There is a **real ceiling** here: the
> analog input itself updates at the polling rate (~50–100 ms per poll), so
> smoothing can only interpolate *between* polls, and at low poll rates it adds
> latency (the displayed value lags the physical stick). The correct fix is
> higher-rate polling of the underlying device — an input-pipeline change for the
> next prompt — not rendering-side smoothing. Recommendation: leave the binding
> direct; revisit only if live polling lands at ≥250 Hz and stick feel still
> reads as steppy.

---

## 4. What changed per file

| File | What | Why | Benefit | Risk |
|---|---|---|---|---|
| `XOutput.WinUI/MainWindow.xaml` | Added `Frame.ContentTransitions` with `NavigationThemeTransition` | Guarantee the Fluent page transition for every navigation (it is the Frame default since Win10 1803; now explicit) | Consistent, native page motion; zero custom code | None: default timing retained; a future custom `NavigationTransitionInfo` can still override per-call |
| `XOutput.WinUI/Animation/CompositionAnimations.cs` **(new)** | `AttachEntrance()` — implicit `Opacity`/`Offset` animations + deferred reveal | The prompt's requested `Visual.ImplicitAnimations` for list/card content | Compositor-thread fade + slide; reposition animations for free; no new packages | Implicit `Offset` animates *all* offset changes (reposition on resize too) — intended Fluent behavior, but note it if a future feature needs instant positioning |
| `XOutput.WinUI/Animation/MicroInteractions.cs` **(new)** | `AttachScaleFeedback()` — cached `Vector3KeyFrameAnimation` scale on pointer events | "Buttery" Discord/Win11 press/hover feel on the real compositor | Genuine independent-thread feedback; tiny (~2 px) scale so no layout shift | 1.02 hover can overlap a neighbor by ~1 px on tight layouts (none in current UI); pointer-only (keyboard focus uses native visuals) |
| `XOutput.WinUI/Controls/StatusDot.xaml(.cs)` **(new)** | Compositor-rendered dot + `ColorKeyFrameAnimation` on `CompositionColorBrush` | Animate badge color on the compositor thread instead of an instant brush swap (prompt §3) | Smooth Connected/Disconnected transitions; data-binding interface unchanged | Shape is not XAML hit-testable (irrelevant for a 10 px dot); colors are fixed constants (same as before) — they do not follow theme, matching the old converter |
| `XOutput.WinUI/Pages/HomePage.xaml` | StatusDot in device template; `Loaded` handlers on item templates; new Virtual Controllers `ItemsControl`; named cards/button/status bar | Wire the list/card entrance + badge animation; give the Virtual Controllers list real structure (animator-ready) for the next prompt | Fade + slide on the lists; badge color animation; empty-state text kept | Template event handlers are standard WinUI; item containers are recreated on each refresh, so entrance replays per page visit (intended, subtle) |
| `XOutput.WinUI/Pages/HomePage.xaml.cs` | `Page_Loaded` entrances; item `Loaded` handlers; scale feedback on Add Controller; controllers list refresh | Code-behind glue for the above | Everything above | `Controllers.Instance.GetControllers()` runs on the UI thread at page load (tiny list; fine) |
| `XOutput.WinUI/Pages/DiagnosticsPage.xaml` | Named the export button | Needed to attach scale feedback | Consistent micro-interaction | None |
| `XOutput.WinUI/Pages/DiagnosticsPage.xaml.cs` | Attach scale feedback to export button | Micro-interaction parity | Same as Home | None |
| `WINUI_ANIMATIONS_REPORT.md` **(this file)** | — | Deliverable | — | — |

No changes to `XOutput.WinUI.csproj` (no new NuGet packages — raw composition
APIs), no changes to `XOutput.Core`, and **no changes to the WPF app** (it remains
the working baseline; WPF feature parity is untouched).

---

## 5. API verification (done in sandbox)

| API used | Verified via |
|---|---|
| `ElementCompositionPreview.GetElementVisual` / `SetElementChildVisual` | Windows App SDK 2.x WinRT reference |
| `Compositor.CreateImplicitAnimationCollection`, `ImplicitAnimationCollection[...]` | Windows App SDK 2.x reference + Microsoft `ImplicitAnimationTransformer` sample |
| `KeyFrameAnimation.InsertExpressionKeyFrame(..., "This.FinalValue")` | Microsoft `ImplicitAnimationTransformer` sample (uses `"this.FinalValue"`; case-insensitive) |
| `Scalar/Vector3KeyFrameAnimation.Target`, `.Duration`, `.EasingFunction`, `Visual.StartAnimation` | Windows App SDK 2.x reference |
| `CreateCubicBezierEasingFunction(Vector2, Vector2)` | Windows App SDK 2.x reference |
| `CompositionRoundedRectangleGeometry` (`CornerRadius`, `Size`) | Windows App SDK 2.x reference |
| `Compositor.CreateShapeVisual` / `CreateSpriteShape(geometry)`, `CompositionSpriteShape.FillBrush`, `ShapeVisual.Shapes.Add` | Windows App SDK 2.x reference |
| `CompositionColorBrush.StartAnimation("Color", ColorKeyFrameAnimation)` | Windows App SDK 2.x reference (Color is an animatable brush property) |
| `NavigationThemeTransition` default on Frame | Windows App SDK 2.x reference (Frame uses it by default since Win10 1803) |
| `ItemsRepeater` animator status | WinUI source (`controls/dev/Repeater/*.idl`): old `ElementAnimator`/`DefaultAnimator` gone, replaced by `ItemTransitionProvider`; `StackLayout` default provider = `nullptr` |
| `UIElement.Scale/Translation/*Transition` (XamlTransitions) | WinUI Gallery `ImplicitTransitionPage` — noted as the *alternative* approach; not used because the prompt explicitly requested `Visual.ImplicitAnimations` + `ColorKeyFrameAnimation` |

Static checks in-sandbox: tree-sitter C# parse of every `XOutput.WinUI/**/*.cs`
(ALL OK), XML well-formedness of every `XOutput.WinUI/**/*.xaml` (ALL OK).

---

## 6. Validation checklist — honest status

**Verified here (static only):** ✓ API correctness per §5; ✓ C# syntax; ✓ XAML
well-formedness; ✓ no dependency changes; ✓ WPF baseline untouched (byte-level
`git diff` shows zero WPF files modified).

**Requires Windows — NOT verifiable from this sandbox; run these on a Windows
10/11 machine (they are the acceptance test):**

- [ ] `dotnet build XOutput.WinUI.sln -c Release` → 0 warnings / 0 errors.
- [ ] `dotnet publish XOutput.WinUI/XOutput.WinUI.csproj -c Release -r win-x64`
      → single-file self-contained exe; copy it (plus nothing else) to a machine
      **without the Windows App SDK installed** and confirm it launches (the
      `WindowsAppSDKSelfContained=true` + `WindowsPackageType=None` config from
      Prompt 1 is what makes this work).
- [ ] Single window/process at runtime; tray minimize/restore/exit (the
      `AppWindow.Closing` close-to-tray from Prompt 2).
- [ ] **The stress test that this migration was about:** with a gamepad plugged
      in and DirectInput actively forwarding (or the WPF app running the same
      controller), rapidly navigate between pages and hover/press buttons; confirm
      the transition, entrance, scale and badge animations do **not** drop frames.
      Because they run on the compositor thread, UI-thread load (polling, layout,
      GC) should not affect them — this is the property that was impossible in
      WPF. *Note:* the WinUI app's own continuous DirectInput polling is not wired
      yet (deferred to the next prompt, which also wires controller creation), so
      today's in-app stress path is the Controller Test demo loop + rapid
      navigation; the same procedure applies once live polling lands.
- [ ] Force feedback + virtual controller creation end-to-end. *Honest status:*
      this is still WPF-side parity today — WinUI's `AddControllerDialog` is a
      shell (steps + live preview) and controller creation is the next prompt's
      work. Nothing in this prompt touches that path, so WPF parity is unchanged.
- [ ] All 9 languages render correctly (animations do not alter text layout; the
      language JSONs are untouched in `XOutput.Core`).

---

## 7. Honest parity notes (WinUI 3 vs Discord/Chrome/native Win11)

**Reached:** true compositor-thread animation for every animated visual property
(scale, offset, opacity, color) — the same mechanism and same engine Windows 11's
shell and WinUI 3's native controls use. The architectural gap from WPF is closed
for these paths: WPF `Storyboard`s drive properties from the Dispatcher, so a busy
UI thread stuttered them; composition keyframes/implicit animations are scheduled
on the independent compositor thread.

**Not fully reached (and why):**

1. **Spring physics.** Discord/Win11 use springs for some motion (elastic hover,
   connected animations). The prompt asked for keyframes, so we use keyframes;
   `CompositionSpringVector3Animation`/`SpringScalarNaturalMotionAnimation` exist
   and could replace the scale keyframes later with ~30 lines if the feel ever
   needs it. Not claimed as parity today.
2. **List removal fade-out.** Items disappear without a fade-out because the
   lists are refreshed wholesale today (no incremental remove event to animate).
   The implicit `Offset` animation still smooths the *repositioning* of remaining
   items. A true fade-out needs either an incremental `ObservableCollection`
   update path or a custom `ItemCollectionTransitionProvider` — both are natural
   companions to the next prompt's live polling and are explicitly deferred.
3. **Shadow/elevation on hover** (Win11 cards lift with an accent shadow) — not
   implemented; a `DropShadow` on the button visuals is a possible later
   refinement, but the current flat design language (Mica cards, 12 px radius)
   does not use shadows anywhere, so adding them only on buttons would look
   inconsistent.
4. **Controller analog smoothing** — see §3.3; a rendering-side smoothing would
   add latency for no real gain while the input poll rate stays low.

---

## 8. Deferred to the next prompt (unchanged from before, now with animation hooks)

- **AddControllerDialog → actually create the controller** (InputMapper +
  ViGEm), which will populate the Virtual Controllers list — which already has its
  compositor entrance ready.
- **MappingPage real per-input configuration + live input polling** (the Mapping
  preview is still a demo cycle; ControllerTest likewise).
- **Diagnostics export** (button still a placeholder; button now has scale
  feedback, so it is pressable-feeling at least).
- **List removal fade-out** once device/controller lists become incremental.
- **`build.yml` `VERSION: '3.31' → '3.32'`** — still **local-only**: the GitHub
  App token lacks the `workflows` permission, so any commit touching
  `.github/workflows/*` is push-rejected. Please either (a) grant "Workflows:
  Read and write" to the GitHub App, or (b) apply this 3-line diff yourself on
  GitHub:
  ```diff
  -  VERSION: '3.31'
  +  VERSION: '3.32'   # must be kept in sync with XOutput/UpdateChecker/Version.cs AppVersion
  ```

---

## 9. Commands for the user

```powershell
# Build (Windows 10/11, requires Windows App SDK workload — normally automatic)
dotnet build XOutput.WinUI.sln -c Release

# Single-file self-contained publish
dotnet publish XOutput.WinUI/XOutput.WinUI.csproj -c Release -r win-x64

# WPF baseline still builds/tests (unchanged)
dotnet build XOutput.sln -c Release
dotnet test XOutputTests -c Release
```

Merge the `arena/01a01cdd-xoutput-3-32` branch into `main` via a PR when the CI
run is green (remember the workflows-permission note above if you also want the
VERSION fix in).
