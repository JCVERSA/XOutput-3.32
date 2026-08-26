# XOutput — WinUI 3 port: core-library extraction + minimal shell (Prompt: WinUI 3)

Date: 2026-08-20
Scope: new WinUI 3 app + extraction of the UI-framework-agnostic business logic into class libraries. The existing WPF app (XOutput) is **kept and now references the shared libraries** — nothing was rewritten.

---

## 1. Resulting project structure

```
XOutput.sln          (unchanged)  → XOutput (WPF app) + XOutputTests
XOutput.WinUI.sln    (new)        → XOutput.Core + XOutput.WinUI

XOutput.Core/                       (new class library, AssemblyName XOutput.Core,
 RootNamespace XOutput — all original XOutput.* namespaces preserved)
 ├─ Devices/                        (DirectInput [Vortice], XInput/Vigem [ViGEm],
 │                                   XInput/SCPToolkit, Mapper, controllers/state)
 ├─ Diagnostics/
 ├─ Tools/                          (Settings, RegistryModifier, HidGuardianManager,
 │                                   SingleInstanceProvider, ArgumentParser, LanguageManager,
 │                                   ApplicationContext DI, Helper, IdHelper)
 ├─ UpdateChecker/
 ├─ Logging/                        (moved too — referenced by every other folder)
 ├─ Resources/Languages/*.json      (embedded; read by LanguageManager)
 ├─ LanguageModel.cs / UI/ModelBase.cs   (moved too — LanguageManager's dependency chain)
 └─ XOutput.Core.csproj             (net9.0-windows; Vortice 3.8.3, ViGEm 1.21.256, Newtonsoft 13.0.4)

XOutput.WinUI/                      (new WinUI 3 app, Windows App SDK 2.4.0)
 ├─ XOutput.WinUI.csproj            (unpackaged, self-contained, single-file)
 ├─ App.xaml / App.xaml.cs
 ├─ MainWindow.xaml / MainWindow.xaml.cs   (minimal core-plumbing probe)
 └─ app.manifest

XOutput/  (WPF app, unchanged TFM)  now references XOutput.Core
 └─ keeps ONLY WPF-specific code: UI/** (shell, pages, components, converters),
    App, Keyboard/Mouse input shims, Tools/DiagnosticsExporter.cs, fonts/icon
```

**Namespaces were not changed**: `RootNamespace=XOutput` on the Core project, so every moved file keeps its `XOutput.Devices.*` / `XOutput.Tools` / `XOutput.Diagnostics` / `XOutput.UpdateChecker` / `XOutput.Logging` namespace. Zero `using`/namespace edits were needed in the moved files themselves (the 4 adaptations below are the only non-wiring changes).

## 2. Packaging decision — verified, not guessed

Current stable Windows App SDK: **2.4.0** (2026-08-13; the 1.x line is in maintenance). Single-file EXE for unpackaged self-contained WinUI 3 is supported per the docs ("PublishSingleFile ... supported for unpackaged, self-contained .NET WinUI 3 apps, Windows App SDK 1.5 and later").

Properties verified against the MS Learn "Project properties and auto-initializers" page (`WindowsAppSDKSingleFileVerifyConfiguration` section — the build-time validation target **errors** if `EnableMsixTooling`, `WindowsPackageType=None` or `IncludeAllContentForSelfExtract` are missing, and **warns** if `WindowsAppSDKSelfContained`/`SelfContained` are absent):

```xml
<OutputType>WinExe</OutputType>
<TargetFramework>net9.0-windows10.0.19041.0</TargetFramework>
<TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
<Platforms>x86;x64;ARM64</Platforms>
<RuntimeIdentifiers>win-x86;win-x64;win-arm64</RuntimeIdentifiers>
<UseWinUI>true</UseWinUI>
<EnableMsixTooling>true</EnableMsixTooling>          <!-- required by the single-file validator -->
<WindowsPackageType>None</WindowsPackageType>          <!-- unpackaged -->
<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
<SelfContained>true</SelfContained>
<PublishSingleFile>true</PublishSingleFile>
<IncludeAllContentForSelfExtract>true</IncludeAllContentForSelfExtract>  <!-- required by validator -->
<IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
<WindowsAppSdkDeploymentManagerInitialize>false</WindowsAppSdkDeploymentManagerInitialize>
<PublishTrimmed>false</PublishTrimmed>    <!-- WinUI 3 does not support trimming -->
<PublishReadyToRun>false</PublishReadyToRun>
<PackageReference Include="Microsoft.WindowsAppSDK" Version="2.4.0" />
<PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.26100.7705" />
```

Publish command (produces one self-contained EXE; extract-to-temp on first launch):
```powershell
dotnet publish XOutput.WinUI/XOutput.WinUI.csproj -c Release -r win-x64
```
> TFM note: `net9.0-windows10.0.19041.0` is the canonical WinUI template TFM; the docs' versioning guidance also allows `net9.0-windows10.0.26100.0` (latest Windows SDK) with the same `TargetPlatformMinVersion`. Either builds the same single-file model.

## 3. Exactly which files needed adaptation (the high-uncertainty list)

Grep of the business folders for `System.Windows`/UI-framework references found **7 files + 1 indirect dependency**. Decisions:

| File | WPF dependency found | Resolution |
|---|---|---|
| `Devices/Input/DirectInput/DirectDevice.cs` | `System.Windows.Application.Current.MainWindow` + `System.Windows.Interop.WindowInteropHelper` (the HWND for `SetCooperativeLevel` exclusive acquisition) | **Adapted**: replaced with `DirectInputPlatform.GetWindowHandle()` (new `DirectInputPlatform.cs` in Core — a `Func<IntPtr>` provider each UI host sets at startup). Removed the two WPF usings. |
| `Devices/Input/Keyboard/Keyboard.cs` | `System.Windows.Input` (WPF input) | **Not extracted** — it is a WPF input shim, not UI-agnostic. Stays in the WPF app. |
| `Devices/Input/Keyboard/KeyboardSource.cs` | `System.Windows.Input.Keyboard.IsKeyDown` | Same — stays in the WPF app. |
| `Devices/Input/Mouse/Mouse.cs` | `System.Windows.Input.MouseButton` enum | Same — stays in the WPF app. |
| `Devices/Input/Mouse/MouseSource.cs` | `MouseButton`/`MouseButtonState` + resolves `MouseHook` | Same — stays in the WPF app. |
| `Devices/Input/Mouse/MouseHook.cs` | `System.Windows.Input` + Win32 hook | Same — stays in the WPF app (a WinUI mouse-input shim would use `Microsoft.UI.Input` and is a *different input model*; out of scope). |
| `Tools/DiagnosticsExporter.cs` | references `XOutput.UI.Windows.DiagnosticsViewModel` (WPF ViewModel) | **Not extracted** — stays in the WPF app (it is a UI-adjacent export helper). |
| `Tools/LanguageManager.cs` | (no WPF) but **assembly-name coupling**: resource lookup assumed the assembly is named `XOutput` (`StartsWith(AssemblyName + ".Resources.Languages.")` + `Split('.')[3]`) | **Adapted**: lookup now searches for the `.Resources.Languages.` marker and derives the key from the remainder — works for `XOutput.*` and `XOutput.Core.*` resource names. Behavior preserved. |
| `UI/LanguageModel.cs`, `UI/ModelBase.cs` | LanguageModel derives from `XOutput.UI.ModelBase` (INPC base) | **Extracted with Core** (both are UI-agnostic INPC/dictionary code) so the Core `LanguageManager` keeps working; namespaces unchanged. |
| `Logging/*` | (not in the requested list, but) referenced by every business folder | **Extracted with Core** (UI-agnostic). |

**Also required by the move** (project-file wiring, no logic):
- `XOutput/XOutput.csproj` — removed the moved package references (Vortice/ViGEm/Newtonsoft now in Core) and the `Resources\Languages\*.json` EmbeddedResource; added `<ProjectReference ..\XOutput.Core\XOutput.Core.csproj>`.
- `XOutput/UI/Windows/MainWindow.xaml.cs` — registers `DirectInputPlatform.HwndProvider` (2 lines) so the WPF app supplies its HWND to the shared DirectInput layer.
- `XOutput.WinUI/MainWindow.xaml.cs` — registers the same provider via `WinRT.Interop.WindowNative.GetWindowHandle(this)` and runs the core-plumbing probe.

## 4. What does NOT port as-is (prompt §3 inventory — verified by grep)

- **`System.Windows.*` direct references** — found only in the 6 WPF-shim files listed above (Keyboard/Mouse) and the old DirectDevice HWND call (now abstracted). No other `System.Windows` usage exists in the business folders. WinUI equivalents would be `Microsoft.UI.Xaml.*`; for this step the shims stay WPF-side.
- **`Dispatcher.Invoke/BeginInvoke`** — grep of the extracted folders: **zero occurrences** (the dispatcher marshaling lives in WPF view models that stay in the WPF app). For WinUI, the pattern to use later is `DispatcherQueue.TryEnqueue`.
- **WPF `Window` subclasses** — already eliminated in the earlier single-window redesign (Prompt 3); no `SettingsWindow`/`DiagnosticsWindow` remain to force-compile. `MainWindow` (WPF) stays WPF; the WinUI app has its own `Microsoft.UI.Xaml.Window`.
- **`MessageBox`** — none in the business folders (already replaced by overlays).

## 5. Per-file change report

| File | What / Why / Benefit / Risk |
|---|---|
| `XOutput.Core/XOutput.Core.csproj` (new) | Class library, `net9.0-windows`, `RootNamespace=XOutput`, `AssemblyName=XOutput.Core`; hosts Vortice/ViGEm/Newtonsoft + embedded language JSONs. **Benefit:** single source of logic for WPF and WinUI. **Risk:** none beyond the move itself. |
| 55 moved `.cs` files + 9 JSONs | `git mv` from `XOutput/*` to `XOutput.Core/*`. Namespaces unchanged → zero code edits in the moved files except the two adaptations in §3. **Risk:** the WPF app now depends on Core — validated statically (C# parses; Core has zero UI-framework refs). |
| `Devices/Input/DirectInput/DirectDevice.cs` | HWND source abstracted (see §3). **Benefit:** Core no longer touches WPF; WinUI can supply its own HWND. **Risk:** if a host forgets to register the provider, exclusive acquisition falls back to `IntPtr.Zero` → device skips exclusive cooperative level (same graceful path as before a window exists); WPF host registers it in `MainWindow` ctor. |
| `Devices/Input/DirectInput/DirectInputPlatform.cs` (new) | The single UI seam. |
| `Tools/LanguageManager.cs` | Assembly-agnostic resource lookup (see §3). |
| `XOutput/XOutput.csproj` | Points at Core; WPF-only deps remain (H.NotifyIcon, fonts). |
| `XOutput/UI/Windows/MainWindow.xaml.cs` | Registers the HWND provider (2 lines). |
| `XOutput.WinUI/*` (new) | csproj (verified single-file config), App, MainWindow probe, app.manifest. |

## 6. Validation status — honest

**Done in this sandbox (static):**
- Core compiles-clean-by-inspection: **zero** `System.Windows` / `Microsoft.UI` / `WinRT` references; zero references to the WPF Keyboard/Mouse shims; only self-contained `XOutput.UI` usage (ModelBase, now in Core).
- All C# parses clean (tree-sitter) across Core / WinUI / WPF / tests.
- WinUI XAML well-formed; csproj properties cross-checked against current MS Learn docs (versions verified on NuGet: WindowsAppSDK 2.4.0, BuildTools 10.0.26100.7705).
- The existing WPF app + tests are structurally intact (XOutputTests still references XOutput → transitively Core).

**Could NOT be done here (Linux sandbox, no .NET SDK / no Windows):**
- ❌ `dotnet build` / `dotnet publish` — cannot execute.
- ❌ Single-file EXE launch on a clean machine (the "no runtime pre-installed" check).
- ❌ Runtime DirectInput enumeration + ViGEm creation through the WinUI shell.
These must run on Windows:
```powershell
dotnet build XOutput.WinUI.sln -c Release
dotnet publish XOutput.WinUI/XOutput.WinUI.csproj -c Release -r win-x64   # -> bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\XOutput.WinUI.exe
dotnet build XOutput.sln -c Release   # confirm the WPF app still builds against XOutput.Core
```
First-launch caveat for the single-file publish: the EXE extracts dependencies to a temp dir at first run (per the docs), so it is "one file to distribute", not "zero-touch extraction-free".

## 7. Risks / flags

1. **WinAppSDK 2.4.0 is brand-new (2026-08-13)**. If the 2.x project system introduces template changes the docs haven't caught, the fallback is WindowsAppSDK `1.8.260804001` (maintenance line, same single-file properties). One-line version change.
2. **WPF app regression risk from the extraction**: the WPF app's build must be re-verified on Windows (it now compiles against Core). Static checks pass; a compile error would be mechanical to fix.
3. **Keyboard/Mouse shims stay WPF-only** — a future WinUI input model (Raw Input / `Microsoft.UI.Input`) is a separate feature, not part of this port.
4. **`EnableWindowsTargeting` kept on Core** so cross-platform CI can still restore/build the net9.0-windows TFM.
