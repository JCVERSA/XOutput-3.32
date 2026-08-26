# XOutput — WinUI 3 application shell (Prompt 2)

Date: 2026-08-20
Builds on: XOutput.Core extraction + minimal probe (Prompt 1). This prompt replaces the probe with the real application shell.

---

## 1. Main window — Mica + custom title bar

`XOutput.WinUI/MainWindow.xaml` + `.xaml.cs`:
- **Single top-level `Microsoft.UI.Xaml.Window`** (WinUI 3 is natively single-window; no second window is ever created — one process/window at runtime).
- **Mica backdrop** via the system-backdrop **controller** path (`Microsoft.UI.Composition.SystemBackdrops.MicaController` + `SystemBackdropConfiguration`), because the prompt requires **live** system theme/accent updates: the controller re-reads `ActualTheme` on `RootGrid.ActualThemeChanged` and activation state on `Activated`. This required the canonical `WindowsSystemDispatcherQueueHelper` (CoreMessaging.dll `CreateDispatcherQueueController`), copied from the official Windows App SDK sample.
  - Alternative: the one-liner `SystemBackdrop = new MicaBackdrop()` (docs' current recommended API) — simpler, but **not** the live-theme-driven controller path; kept the controller for the explicit "live" requirement.
- **Custom title bar**: `ExtendsContentIntoTitleBar = true` + `SetTitleBar(AppTitleBar)` + `appWindow.SetIcon(icon.ico)` + drag region (the title-bar Grid). App icon + "XOutput".

## 2. Tray icon — H.NotifyIcon.WinUI

- Package: **H.NotifyIcon.WinUI 2.3.2** (verified: 2.4.1 targets **net10.0-windows only** → not consumable from net9; 2.3.2 has a `net9.0-windows10.0.17763` asset — same decision as the WPF side).
- XAML: `xmlns:tb="using:H.NotifyIcon"`, `<tb:TaskbarIcon ... ContextFlyout>` (the context-menu DP is `ContextFlyout`, a `MenuFlyout`; `ContextMenuMode="SecondWindow"` renders the native Win32 popup menu; verified against the v2.3.2 source).
- Minimize/restore/exit: `this.Hide()` / `this.Show()` (H.NotifyIcon `WindowExtensions`), tray menu items **Show XOutput** / **Exit**.

## 3. "Minimize to tray on close" — exact technique (risk area)

WinUI 3 has **no cancelable `Window.Closing`** (the `Window.Closed`/`WindowEventArgs` event cannot cancel). The currently recommended approach is **`AppWindow.Closing`** (Windows App SDK 1.4+), which supports cancellation and works for unpackaged apps:

```csharp
var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
appWindow.Closing += (s, e) => { if (closeToTray) { e.Cancel = true; this.Hide(); } };
```

- The X-button and Alt+F4 route through `WM_CLOSE` → `AppWindow.Closing`, so this covers both.
- **Exit** sets `allowClose = true` before `this.Close()`, bypassing the cancel.
- Older alternatives (WM_CLOSE subclassing via `SetWindowSubclass`) are obsolete now that `AppWindow.Closing` exists (per the Windows App SDK discussion). This is the technique to flag: **`AppWindow.Closing` + `args.Cancel`** — verified from MS Learn docs (2026).
- Caveat (honest): with close-to-tray enabled, the process keeps running hidden; the tray Exit is the way out. `AppWindow.Closed` (non-cancelable) disposes Mica + the tray icon.

## 4. Navigation — NavigationView

Native `Microsoft.UI.Xaml.Controls.NavigationView`, 5 items (Home / Controller Test / Mapping / Diagnostics / Settings), each with a **Segoe Fluent Icon** `FontIcon` glyph (WinUI ships these natively — no icon package). `SelectionChanged` navigates a `Frame` to the matching placeholder page in `XOutput.WinUI/Pages/`.

## 5. Theme

`RequestedTheme` left as the default (`ElementTheme.Default`) → the app follows the **system** light/dark automatically; the Mica controller additionally reacts to `ActualThemeChanged` live. No custom palette (matches the prior WPF "system-driven" decision).

## 6. Validation status — honest

**Done (static, sandbox):** all C# parses clean (tree-sitter) across WinUI/Core/WPF/tests; WinUI XAML well-formed; package/TFM/API choices verified against NuGet metadata + H.NotifyIcon v2.3.2 source + Windows App SDK docs (Mica controller, AppWindow.Closing, dispatcher helper).

**Could NOT be done here (Linux sandbox, no .NET SDK / no Windows):**
- ❌ `dotnet build XOutput.WinUI.sln -c Release` / `dotnet publish -r win-x64`
- ❌ Runtime: Mica visible, custom title bar draggable, NavigationView navigation, tray minimize/restore/exit, **exactly one process/window**, live system theme/accent change.
Run on Windows (commands in the report). The highest-risk item to verify by hand: the `AppWindow.Closing` cancel + `WindowExtensions.Hide` path, and the Mica dispatcher-helper on first launch.

## 7. Files

- `XOutput.WinUI/XOutput.WinUI.csproj` — added `H.NotifyIcon.WinUI 2.3.2`.
- `XOutput.WinUI/MainWindow.xaml(.cs)` — the shell (replaced the probe window).
- `XOutput.WinUI/Pages/{Home,ControllerTest,Mapping,Diagnostics,Settings}Page.*` — 5 placeholder pages (Home re-runs the DirectInput+ViGEm probe so the end-to-end proof stays visible).
- `XOutput.WinUI/Assets/icon.ico` — app/tray icon.
- `XOutput.WinUI/app.manifest` — unchanged (DPI awareness).
