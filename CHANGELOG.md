# Changelog

All notable changes to XOutput are documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [3.32] - 2026-08-20

### Added
- **Kinetic Console dark theme** — centralized palette/typography/shape tokens (`UI/Themes/*`), embedded Inter + JetBrains Mono fonts with runtime fallback, implicit control styles, unified vector icon set (`UI/Themes/Icons.xaml`).
- **Single-window shell** — persistent 240 px sidebar navigation (Home / Controller Test / Mapping / Diagnostics / Settings), collapsible console drawer with log cap, one reusable in-shell overlay host (wizard / about / messages). No OS-level dialogs for internal navigation.
- **Live-reactive controller visualization** — direction-precise stick guidance (X/Y arrows, center press), button/trigger/d-pad glow driven by the wizard's highlight blink; used in the mapping wizard and the Mapping page Live Preview.
- **Redesigned pages** — Home (two-column device/controller cards + status pills), Mapping (per-device tabs, target/deadzone/invert/live-preview), Diagnostics (cards + status pills), Settings (grouped cards).
- **Diagnostics export bundle** — button on the Diagnostics page writes `XOutput-diagnostics-<timestamp>.zip` (report + `XOutput.log` + `settings.json`).
- **Tray quick actions** — Start all / Stop all controllers from the tray context menu.
- **Settings auto-backup** — on app-version upgrade, `settings.json` is copied to `settings-<version>.bak.json` (sidecar `XOutput.version` marker; the settings format itself is unchanged).
- Unit tests for the new presentation converters and shell navigation model.
- GitHub Actions workflow: Release build, tests, publish, artifact upload, `workflow_dispatch`.

### Changed
- Migrated from .NET Framework 4.5.2 to **.NET 9.0 (net9.0-windows)**; SDK-style projects.
- SharpDX / SharpDX.DirectInput 4.2.0 → **Vortice.DirectInput 3.8.3** (DirectInput bindings).
- Nefarius.ViGEm.Client 1.16.148 → **1.21.256**; Newtonsoft.Json 12.0.2 → **13.0.4**.
- Hardcodet.NotifyIcon.Wpf 1.0.8 → **H.NotifyIcon.Wpf 2.3.2** (actively maintained, net9.0-windows compatible).
- Removed the legacy `DependencyEmbedder` embedded-resource single-exe mechanism (superseded by .NET single-file publish when desired).
- Update checker now queries this repository's GitHub Releases API (10 s timeout) instead of the upstream repo.
- Application version unified to **3.32**.
- Live-update refresh timers reduced from 10 ms to 33 ms.
- Atomic settings save (temp file + replace) to prevent corruption on crash.

### Fixed
- Wizard controller visualization stayed static: restored `RelativeSource` bindings so the configured input glows/blinks.
- Stick guidance could not distinguish X / Y / stick-press: split into per-direction arrows and a center glow.
- Mapping page navigated home when any (not just the selected) device disconnected; tabs are now removed in place.
- Stale mapper reference when controllers were added/removed while the Mapping page was open.
- Unhandled-exception handler now shows the themed overlay and keeps the session alive instead of dying silently.
- Various WPF XAML compile errors (Setter.Value, StrokeLineCap, x:Double, CharacterSpacing, IsItemsHost) surfaced by the CI Release build.
- Obsolete `[DataTestMethod]`/`Assert.ThrowsException` usages removed for MSTest 4.x.

### Removed
- `appveyor.yml` (GitHub Actions is canonical); dev `preview-artifacts/` untracked.

## [3.31] - (previous upstream release baseline)
