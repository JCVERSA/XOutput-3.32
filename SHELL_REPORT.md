# XOutput — Single-window shell (Prompt 3)

Date: 2026-08-20
Scope: window-architecture restructure only. **No theme tokens from Prompt 2 were changed** (one new derived brush was added, see §7). Nothing from Prompts 1–2 was reverted.

---

## 1. Architecture

```
MainWindow (the ONLY OS Window; 940x700; lifecycle + tray icon unchanged)
└─ MainShellView (UserControl, DataContext = ShellViewModel)
   ├─ Menu bar  (File / Tools / Help — File→Save, File→Game Controllers, File→Exit,
   │             Tools→Settings, Tools→Diagnostics, Help→About all preserved)
   ├─ Sidebar   (fixed 240 px, BrushSurfaceContainerLow, 1 px BrushOutlineVariant right border)
   │   └─ Nav items (44 px, 20x20 2 px-stroke icons + label):
   │        Home · Controller Test · Mapping · Diagnostics · Settings
   │        active = icon+label BrushPrimary, 3 px BrushPrimary left indicator, tinted bg
   ├─ Content area (ContentControl ← ShellViewModel.CurrentPage)
   │   └─ Console drawer (bottom): header row (Console title, copy button, collapse
   │        chevron) + GridSplitter + mono logBox; min height 120, remembers last
   │        expanded height, collapses to a thin header bar
   └─ Overlay host (full-bleed last child, ~60 % black backdrop + centered card,
        CornerRadius 16, BrushSurfaceContainer, max-width 700) ← ShellViewModel.ActiveOverlay
```

**One reusable overlay host** drives all three overlay types (wizard, about, message) — a single `ContentPresenter` bound to `ShellViewModel.ActiveOverlay`, visibility via `HasActiveOverlay` + the existing `BoolToVisibilityConverter`. No per-dialog backdrop/card duplication.

## 2. Page conversions (no OS windows remain)

| Former Window | New shell surface | Trigger (unchanged method names) |
|---|---|---|
| MainWindow body (devices + controllers + save bar) | `UI/Shell/Pages/HomePage` | sidebar Home (cached page, MainWindowViewModel) |
| `SettingsWindow` | `UI/Shell/Pages/SettingsPage` | sidebar Settings / Tools→Settings |
| `DiagnosticsWindow` | `UI/Shell/Pages/DiagnosticsPage` | sidebar Diagnostics / Tools→Diagnostics |
| `InputSettingsWindow` (device mapping/test) | `UI/Shell/Pages/MappingPage` | sidebar Mapping / device row "Edit" (`InputViewModel.Edit`) |
| `ControllerSettingsWindow` (controller mapping + XInput test) | `UI/Shell/Pages/ControllerTestPage` | sidebar Controller Test / controller row "Edit" (`ControllerViewModel.Edit`); shows an empty state when no controller exists |
| `AutoConfigureWindow` (mapping wizard) | `UI/Shell/Overlays/WizardOverlayView` (in-shell modal overlay) | "Configure All" (`ControllerSettingsViewModel.ConfigureAll`) and per-mapping "Configure" (`MappingViewModel.Configure`) |
| `MessageBox.Show` (about + error/info) | `UI/Shell/Overlays/AboutOverlayView` + `MessageOverlayView` (same overlay host) | `MainWindowViewModel.AboutPopupShow`, `ShowMessage(...)` for all former MessageBox sites |

Every previous `new XxxWindow(...).ShowDialog()` call was replaced by shell navigation/overlay — a repo-wide grep confirms **zero `ShowDialog()` and zero `new ...Window` (except `MainWindow` itself) remain**, and **no native `MessageBox` remains** (the two `App.xaml.cs` crash-path dialogs were converted to best-effort overlays with logging — see §6).

## 3. Judgment calls (explicitly flagged)

1. **`ControllerSettingsWindow` was not named in the prompt but is a `Window`, so it was converted too** (the goal is "zero separate OS windows"). It became **`ControllerTestPage`** (sidebar "Controller Test") because it is the per-controller page: live XInput test panel + mapping editor + Configure-All. `InputSettingsWindow` became **`MappingPage`** exactly as instructed ("InputSettingsWindow.xaml (mapping, currently instantiated in InputViewModel.cs) → MappingPage"). When the sidebar "Controller Test" is opened with no controllers configured, the page shows a themed empty state instead of crashing.
2. **Sidebar "Mapping" without a device context** opens the MappingPage for the first DirectInput device (falling back to the first input device — keyboard/mouse always exist), while device-row "Edit" opens it for that specific device. A device picker is deferred to Prompt 4.
3. **Wizard "Close" semantics**: the wizard auto-closes after the last step, on Save/Disable completion, or on Cancel — all now call `ShellViewModel.CloseOverlay()`. The `MappingViewModel.Configure` / `ControllerSettingsViewModel.ConfigureAll` refresh of the mapping views moved into an `onClosed` callback so views refresh when the wizard actually closes (previously after the blocking `ShowDialog` returned).
4. **Window width 700 → 940** so the content area keeps its exact pre-shell width (940 − 240 sidebar − 24 page margin ≈ 676, matching the old 700 − margins). No control metrics change.
5. **`MappingPage` / `ControllerTestPage` live-update timers** (10 ms) moved from window Loaded/Closed to page Loaded/Unloaded; both pages dispose their view model on Unloaded (navigation away), matching the old window-close lifecycle.
6. **Stepper header** added to the wizard (new `AutoConfigureModel.StepIndex/StepCount/StepText` + `WizardStep` translation key "Step {0} / {1}") — pure addition, no existing VM method renamed.
7. **New translation keys** added to all 9 language files: `HomeMenu`, `ControllerTestMenu`, `ConsoleTitle`, `CopyLog`, `Close`, `Ok`, `WizardStep` (verified all parse; missing keys fall back to the key itself as before).
8. **One new derived brush** added to Colors.xaml: `BrushNavActiveBackground` (#1A7CDB4D, sidebar active tint) and `BrushOverlayBackdrop` (#99000000, ~60 % black). Existing Prompt-2 tokens untouched.

## 4. Files

**New**
- `XOutput/UI/Shell/ShellPageType.cs`, `ShellNavItem.cs`, `ShellViewModel.cs`
- `XOutput/UI/Shell/MainShellView.xaml(.cs)`
- `XOutput/UI/Shell/Pages/HomePage.xaml(.cs)`, `SettingsPage.xaml(.cs)`, `DiagnosticsPage.xaml(.cs)`, `MappingPage.xaml(.cs)`, `ControllerTestPage.xaml(.cs)`
- `XOutput/UI/Shell/Overlays/WizardOverlayView.xaml(.cs)`, `AboutOverlayView.xaml(.cs)`, `MessageOverlayView.xaml(.cs)` (+ small overlay VMs)

**Modified**
- `MainWindow.xaml(.cs)` — hosts MainShellView + tray icon; only lifecycle handlers remain (WindowClosing/Closed, tray double-click, ForceShow, Exit, Log routed to the shell drawer); width 940
- `MainWindowViewModel.cs` — OpenSettings/OpenDiagnostics/AboutPopupShow delegate to the shell; all MessageBox.Show replaced by `ShellViewModel.ShowMessage`
- `InputViewModel.cs`, `ControllerViewModel.cs` — Edit buttons navigate the shell
- `MappingViewModel.cs`, `ControllerSettingsViewModel.cs` — Configure/ConfigureAll open the wizard overlay
- `AutoConfigureModel.cs`, `AutoConfigureViewModel.cs` — stepper state
- `UIConfiguration.cs` — SettingsWindow/DiagnosticsWindow resolvers removed (VM resolvers kept)
- `App.xaml.cs` — crash-path MessageBoxes → best-effort overlay
- `Colors.xaml` — two new derived brushes
- 9 language JSONs — new keys

**Deleted**
- `SettingsWindow`, `DiagnosticsWindow`, `InputSettingsWindow`, `ControllerSettingsWindow`, `AutoConfigureWindow` (xaml + cs)

## 5. Validation performed (static, in sandbox)

- All C# files parse clean (tree-sitter).
- All XAML well-formed; every `{StaticResource …}` referenced anywhere (views, shell, overlays, theme-internal) resolves; every `x:Class` has its `.xaml.cs`.
- Grep: no `ShowDialog()`, no `new …Window` (except `MainWindow`), no native `MessageBox` left.
- No hardcoded colors in any view/overlay XAML.
- All 9 language files re-parse with the new keys.

**Required on Windows (cannot run here — same sandbox limitation as Prompts 1–2):** build in Release; run; verify per the task's §5:
- [ ] Opening Settings / Diagnostics / Mapping / Controller Test / wizard / About creates **no second OS window** (taskbar / Alt-Tab shows exactly one XOutput entry)
- [ ] No binding errors in the debug/output window
- [ ] File→Save, Add Controller, Force Refresh, controller Edit/Start/Stop, tray icon (minimize to tray, restore, exit) all work
- [ ] Wizard completes all steps and updates the mapping views; About and error overlays open/close; console drawer collapses/expands and remembers its height; copy button copies the log

## 6. Known caveats

- The two `App.xaml.cs` crash/startup paths show the overlay best-effort: `DispatcherUnhandledException` still lets the process terminate (as before — it never set `e.Handled`), so on a hard crash the overlay may only flash before exit; the error is always logged. Startup failure with no window yet created can only be logged. This is the honest limit of replacing a modal crash dialog with an in-window overlay.
- `ControllerTestPage` preserves the original (quirky) 2-column + implicit-3rd-column grid from `ControllerSettingsWindow.xaml` verbatim so behavior matches the old window.
- Console drawer height is remembered in-memory (per session), not persisted to settings.json (avoiding any settings-format churn; Prompt 1's serialization caution still applies).
