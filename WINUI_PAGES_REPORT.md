# XOutput — WinUI 3 page content (Prompt 3)

Date: 2026-08-20
Builds on: WinUI 3 shell (Prompt 2) + XOutput.Core extraction (Prompt 1).

## 1. Pages — re-implementation, not redesign

All five destinations are `Microsoft.UI.Xaml.Controls.Page`s navigated via the shell's `NavigationView`/`Frame`. The layout decisions from the validated WPF redesign are preserved (two-card Home, card-based panels, tabbed Mapping with live preview), implemented with WinUI-native controls.

| Page | Content | Binding style |
|---|---|---|
| HomePage | Two-card layout (Input Devices + Virtual Controllers), status bar (active device/controller counts), Add Controller button → wizard dialog | `{x:Bind}` for static page props; classic `{Binding}` inside the `ItemsControl` DataTemplate (items are runtime Core objects, not x:Bindable) |
| ControllerTestPage | Virtual-controller output card with the live `XboxControllerView` running a demo cycle | `{x:Bind}` (code-behind props) |
| MappingPage | Device tab strip (horizontal `ListView`) + two-column body: Physical Inputs list + Configure card with the live controller preview | `{x:Bind}` for page props; `{Binding}` for the Sources list |
| DiagnosticsPage | System card (ViGEm status) + per-device diagnostics cards; Export report button (placeholder) | code-behind + `{Binding}` |
| SettingsPage | General + System grouped cards with `ToggleSwitch`es bound to the shared Core `Settings` (persisted) | event-driven (code-behind) |

**Cards**: WinUI 3 has no built-in Card control → a themed `Border` + `CornerRadius=12` + theme-dictionary card brushes (`App.xaml` `CardBackgroundBrush`/`CardBorderBrush`, Light/Dark), consistent with Fluent practice.

## 2. Dialogs — ContentDialog (native)

- **AboutDialog** — `ContentDialog` showing the app version from `XOutput.Core/UpdateChecker/Version`. Opened from the NavigationView footer "About" item.
- **AddControllerDialog** — a single `ContentDialog` whose body is a `ContentControl` that swaps 4-step content (Select Device → Test Inputs → Confirm → Done), with a live `XboxControllerView` on the Test Inputs step. `PrimaryButtonClick` advances steps with `args.Cancel=true` until Finish. (Full InputMapper/ViGEm creation is the next prompt's wiring — the wizard shell + step flow is what this prompt delivers.)

## 3. Controller visualization — WinUI port

`XOutput.WinUI/Controls/XboxControllerView` — a `UserControl` with the corrected WPF behavior:
- **Continuous trigger opacity**: `Opacity = live value` (0 → invisible, 1 → full), via `ValueToOpacityConverter`.
- **Positional sticks**: cap + glow carry `TranslateTransform` bound to live X/Y via `StickOffsetConverter` (±6 px, Y inverted to match the app convention: value 1 = up).
- **Digital highlight**: face buttons / d-pad / shoulders glow on `Highlight` (wizard blink).
- Driven by `LiveTarget` / `LiveValue` / `Highlight` dependency properties feeding the shared Core `XBoxModel`. Same behavior as the WPF `XBox` component, WinUI XAML + code-behind syntax.

## 4. Bindings — {x:Bind} vs {Binding} (per-case decision)

Used **`{x:Bind}`** for: page-level properties that are stable per navigation (MappingPage's `Devices` list, `LiveTarget`/`LiveValue`/`PreviewHighlight` for the controller view; ControllerTestPage's `TestView` props; the wizard's `LiveTarget`/`LiveValue`/`Highlight`).
Used **classic `{Binding}`** for: `ItemsControl` item templates bound to runtime Core objects (`IInputDevice` in Home's device list, `DeviceDiagItem` in Diagnostics, `InputSource` in Mapping) — these are plain CLR objects without compile-time page types, where `{x:Bind}` would require generated accessor types. This matches the prompt's "pick per-case" guidance; `{x:Bind}` gives compile-time checking where the target is a page/code-behind type, `{Binding}` is the direct path for runtime item data.

## 5. Validation status — honest

**Done (static, sandbox):** all C# parses clean; all WinUI XAML well-formed; references between pages/dialogs/controls/converters resolve; package/API choices verified earlier (WinAppSDK 2.4.0, H.NotifyIcon.WinUI 2.3.2).

**Could NOT be done here (Linux sandbox, no .NET SDK / no Windows):**
- ❌ `dotnet build XOutput.WinUI.sln -c Release`
- ❌ Runtime render of every page, dialog flows, live controller animation, 9-language fit, binding resolution in the live app.
Must be verified on Windows (commands below). Static binding-correctness cannot be fully guaranteed without the WinUI XAML compiler; the `{x:Bind}` usages are to page/code-behind properties that exist, and `{Binding}` usages are to real Core properties.

```powershell
dotnet build XOutput.WinUI.sln -c Release
dotnet publish XOutput.WinUI/XOutput.WinUI.csproj -c Release -r win-x64
```

## 6. Files

New: `Controls/XboxControllerView.{xaml,cs}`, `Converters/XboxConverters.cs`, `Converters/BoolToColorConverter.cs`, `Dialogs/AboutDialog.{xaml,cs}`, `Dialogs/AddControllerDialog.{xaml,cs}`, `WINUI_PAGES_REPORT.md`.
Modified: `App.xaml` (card brushes + converter resources), `MainWindow.xaml` (About footer item), `MainWindow.xaml.cs` (About navigation), all 5 `Pages/*Page.{xaml,cs}` (real content).

## 7. Known follow-ups (next prompt, not this one)

- AddController wizard → actual `InputMapper` creation + ViGEm `Plugin` (per the validated WPF flow).
- Mapping page → real per-input configuration (target dropdown, deadzone, invert) writing to Core `InputMapper`.
- Diagnostics export (currently placeholder).
- Live input polling for the Mapping/ControllerTest previews (currently a demo cycle on ControllerTest).
