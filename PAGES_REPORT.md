# XOutput — Page content restructure (Prompt 4)

Date: 2026-08-20
Scope: **presentation only** — page layouts + reusable component restyling. All ViewModels/bindings remain the source of truth; no mapping-engine changes, no serialization-format changes, no theme-token changes (three new derived brushes were added, see §7).

---

## 1. Home page (`UI/Shell/Pages/HomePage.xaml`)

New two-column card layout per the mockup:

- **Top bar**: page title + right-aligned **Add Controller** primary button → opens the **wizard overlay** (new `MainWindowViewModel.OpenAddControllerWizard()` builds an unregistered `InputMapper` with default `MapperDataCollection`s — identical defaults to `Settings.CreateMapper` — and only calls `AddController(mapper)` when the wizard completes; cancelling discards the mapper instead of leaking an empty one into settings).
- **Left card "Input Devices"** (header + `DirectInput` tag): each detected device as a row — status dot (bound to `Model.Device.Connected` via the existing `BoolToBrushConverter`), display name, **VID/PID in TextDataMono** (new `SourceToVidPidConverter`, parses `VID_xxxx/PID_xxxx` from the hardware ID; empty for keyboard/mouse), and the existing **Configure** action (unchanged `OpenClick` → mapping page). Below the list: the existing **Show All Devices** checkbox and **Force Refresh** button (secondary style), bindings/events untouched.
- **Right card "Virtual Controllers"** (header + `ViGEm` tag): each controller as a restyled `ControllerView` row with a **status pill** ("Active - N Source(s)" in `BrushPrimary` when started, "Stopped" otherwise — new presentation properties `ActiveSourceCount`/`StatusText` on `ControllerModel`), start/stop, edit and remove icon+text actions (existing events). **Empty state** "+ / No additional virtual controllers active." shown via the existing `EnumerableCountToVisibilityConverter` (count == 0).
- **Status bar** below both cards: pill badges "Active Devices: N" and "Active Controllers: N" (new `MainWindowModel.InputCount`/`ControllerCount`, raised on collection changes) and a right-aligned "Backend: ViGEm" pill (`Model.BackendName`, set in `Initialize` from the existing ViGEm/SCP detection — no new detection logic).

## 2. Mapping page (`UI/Shell/Pages/MappingPage*`)

New presentation VM `MappingPageViewModel` (no engine changes) + full layout:

- **Tab row per connected device** (from `InputDevices.Instance`, each tab wraps the *existing* `InputSettingsViewModel` — the exact per-device live-test/FF/HidGuardian VM from before) **+ a "Virtual Gamepad" tab** (informational card; controller output testing lives in the Controller Test page).
- **Left card "Physical Inputs"**: the device's live axis/slider, button and dpad views (existing `AxisView`/`ButtonView`/`DPadView`, restyled in §5) with a **mapped-target label** per row (new read-only `SourceToMappedTargetConverter` — reverse lookup across the existing controllers' `MapperData`; "Unmapped" fallback). DPad rows deliberately have no label (their VM exposes no `InputSource`, avoiding dead bindings).
- **Right card "Configure: <selected input>"**:
  - input selector (ComboBox over the device's `Sources`),
  - **target dropdown** (all `XInputTypes`, translated; selecting one maps the input using the *same engine write path* as the existing editor — `MapperData.Source = source` via `InputMapper.GetMapping`),
  - **deadzone slider + %** (0–50, bound to the target's `MapperData.Deadzone`),
  - **invert checkbox** (swaps `MapperData.MinValue/MaxValue` — identical semantics to the existing `MappingViewModel.Invert()`),
  - **Live Preview** panel: raw value / mapped value in TextDataMono (updated by the page's existing 10 ms timer), a `BrushPrimary` fill bar (ScaleX = mapped value), and a **2D axis indicator** (crosshair dot, visible only for axis sources via `SourceIsAxisToVisibilityConverter`).
- **Bottom action row**: **Export Profile** (secondary — copies the controller mapper as JSON to the clipboard; disabled without a controller) + **Save Mapping** (primary — existing `SaveSettings`).
- The **Force Feedback test + HidGuardian** card is preserved below the configure card (same bindings/events, rewired to the selected tab's `InputSettingsViewModel`), so no functionality is lost.

**Explicit judgment calls:**
1. The configure panel edits the **first virtual controller's mapper** (mapping lives per-controller in this app, not per-device). Without any controller the panel shows "Create a virtual controller to configure mappings." instead of inventing a per-device mapper model.
2. **Anti-deadzone slider omitted**: `MapperData` has no anti-deadzone concept, `GetValue()` has no such parameter, and adding a non-functional control would violate §6 ("no dead bindings") and the Prompt-1 settings-format caution. Flagged for the mapping-engine work.
3. Left rows are display-only (their `DataContext` is owned by the component VMs); selection happens via the "Input" dropdown in the configure card.
4. "Remove" is shown on virtual-controller rows (the existing action) but not on physical-device rows — physical devices are auto-enumerated and cannot be removed (they re-appear on refresh).

## 3. Diagnostics page + `DiagnosticsItemView`

`DiagnosticsItemView` restyled as **cards**: device/system header (`TextTitleSm`) + per-result rows with translated name (existing `DynamicLanguageConverter`), **measured value in TextDataMono**, and **status pills** (Passed = primary-tinted pill, Warning = amber, Failed = red — the existing three-state pattern via `EqualsToVisibilityConverter`, now as pill badges with the new `BrushWarningSoft`/`BrushErrorSoft` tokens). `DiagnosticsItemViewModel` untouched; the page got a header.

## 4. Settings page

`SettingsWindow`'s five existing fields restyled as **two grouped cards** ("General": Language / Close to tray / Run at startup; "System": HidGuardian / disable auto refresh) with `TextTitleSm` section headers, label-left/control-right rows, wrapping labels for long German/Russian strings. **No settings added or removed**; bindings untouched. Also filled the pre-existing missing `DisableAutoRefresh` translations in 8 languages (it previously fell back to the raw key).

## 5. Reusable components (`UI/Component/*.xaml`)

All bindings/converters/events preserved; containers restyled:

- **ControllerView**: card row (rounded, tinted bg), name + **status pill**, icon+text remove/edit/start-stop buttons (existing `RemoveClick`/`OpenClick`/`ButtonClick` + `Model.ButtonText`).
- **InputView**: card row with status dot, name, VID/PID mono (new converter), Configure button (existing `OpenClick`); the input-flash `Model.Background` preserved.
- **AxisView**: label + **BrushPrimary fill bar proportional to live value** (ScaleX via new `RatioScaleConverter`) + mono percentage.
- **ButtonView**: label + rounded indicator that fills with `BrushPrimary` when pressed (`BoolToDoubleConverter` scale + existing bool binding).
- **DPadView / Axis2DView**: themed pads with crosshair guides, `BrushPrimary` indicator dots, themed progress bars kept in Axis2DView (nothing removed).
- **DiagnosticsItemView**: see §3.

## 6. New theme additions (tokens untouched)

- `Styles.xaml`: implicit **Slider** (primary fill + circular thumb), **TabControl/TabItem** (pill tabs, card body), **ListBox/ListBoxItem** (dark rows) styles, and a keyed **`ButtonSecondaryStyle`** — all built from existing Prompt-2 tokens.
- `Colors.xaml`: three new derived brushes — `BrushWarningSoft`, `BrushErrorSoft` (translucent status-pill backgrounds), `BrushNavActiveBackground` already existed.

## 7. New converters (presentation only)

`RatioScaleConverter`, `RatioToPositionConverter`, `SourceToMappedTargetConverter`, `SourceToVidPidConverter`, `BoolToDoubleConverter`, `SourceIsAxisToVisibilityConverter` — all read-only display helpers, registered in `App.xaml`.

## 8. Validation performed (static, in sandbox)

- All C# files parse clean (tree-sitter).
- All XAML well-formed; every `{StaticResource …}` referenced anywhere resolves (140+ keys, including theme-internal references); every `x:Class` has its code-behind.
- **Zero hardcoded colors** in any view/component/overlay XAML.
- All 9 language files parse; every XAML `ConverterParameter` key exists in English.json.
- German/Russian/CJK string widths measured against the real embedded fonts for the new fixed spots: status pills ≤ 155 px (auto-size), settings labels ≤ 226 px (wrap enabled), configure labels ≤ 86 px, empty states wrap — **no truncation**.
- **CJK/Cyrillic in the embedded fonts**: Inter and JetBrains Mono lack CJK glyphs; WPF performs automatic per-glyph font fallback (missing glyphs render via system fonts such as Microsoft YaHei), so "下 / 左" etc. still render — verified conceptually and mirrored in the browser preview (browsers do the same per-glyph fallback). This is a display-quality note, not a regression.

**Required on Windows (cannot run here — sandbox has no WPF/.NET):** build in Release; verify §6 of the task: pages render with 0/1/multiple devices & controllers (empty states), no binding errors in the debug output, German/Russian/CJK fit, and File→Save / Add Controller (wizard) / Force Refresh / tray behavior unchanged.

## 9. Files

**New:** `MappingPageViewModel.cs`, 6 converters, `preview-artifacts/pages-preview.html`.
**Modified:** `HomePage`, `MappingPage`, `SettingsPage`, `DiagnosticsPage` (+ `.cs`), `ShellViewModel` (Mapping wiring), `MainWindowViewModel` (`OpenAddControllerWizard` + `BackendName`), `MainWindowModel` (counts + backend), `ControllerModel` (status pill), `InputSettingsViewModel` (stop FF timer on dispose), components (`InputView`, `ControllerView`, `AxisView`, `ButtonView`, `DPadView`, `Axis2DView`, `DiagnosticsItemView`), `Styles.xaml`, `Colors.xaml`, `App.xaml`, 9 language files.
