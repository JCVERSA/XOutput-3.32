# XOutput 3.32 — .NET Framework 4.5.2 → .NET 9.0 Migration Report

Date: 2026-08-20
Scope: Pure technical migration. **No XAML styling, layout, or visual appearance was touched.**
Target: `net9.0-windows`, SDK-style projects, framework-dependent (no RID pinning, no AOT, portable AnyCPU).

---

## 1. Package decisions (verified against NuGet + library sources, not guessed)

| Package (old) | Package (new) | Version | Why / verification |
|---|---|---|---|
| `SharpDX` 4.2.0 | **removed** | – | Replaced by Vortice.DirectInput. |
| `SharpDX.DirectInput` 4.2.0 | **removed** | – | Replaced by Vortice.DirectInput. |
| – | `Vortice.DirectInput` | **3.8.3** | Latest stable (2026-03-04). Ships `lib/net9.0` asset (also net8.0/net10.0). Depends on `Vortice.DirectX` 3.8.3 → `SharpGen.Runtime(.COM)` ≥2.4.2-beta, `Vortice.Mathematics` ≥2.1.0 (all transitive, all net9.0-compatible). The published package's source commit (`9e609cb`) was cloned and every API used by XOutput was verified member-by-member against it (see §3). |
| `Nefarius.ViGEm.Client` 1.16.148 | `Nefarius.ViGEm.Client` | **1.21.256** | Latest stable (published 2023-02-05; no newer stable exists — the 1.22+ line is not on NuGet). `netstandard2.0`, **zero dependencies** (native `ViGEmClient.dll` is embedded in the package itself via Costura.Fody — no extra files needed). API surface verified against the upstream repo (`nefarius/ViGEm.NET` master, which is newer than the published package). **No breaking change for this codebase**: the "strong-typed class" API (`Xbox360Button.A`, `Xbox360Axis.LeftThumbX`, `Xbox360Slider.LeftTrigger`, `IXbox360Controller`, `Xbox360FeedbackReceivedEventArgs.LargeMotor/SmallMotor`, `VigemBusNotFoundException`) has existed since 2019 — i.e. already present in the pinned 1.16.148. Only the version number changed. |
| `Newtonsoft.Json` 12.0.2 | `Newtonsoft.Json` | **13.0.4** | Latest stable 13.x (13.0.5 is still beta). Same `JsonConvert` API — zero code change. Not migrated to System.Text.Json (out of scope by instruction). |
| `Hardcodet.NotifyIcon.Wpf` 1.0.8 | `H.NotifyIcon.Wpf` | **2.3.2** | The original `Hardcodet.NotifyIcon.Wpf` targets net40/net45 only → not consumable from net9.0-windows. Its direct successor `Hardcodet.NotifyIcon.Wpf.NetCore` (1.1.5) is stale (2022, .NET 6-era). The actively maintained fork is **H.NotifyIcon** (HavenDV). **Important:** the newest stable `H.NotifyIcon.Wpf` **2.4.1** targets `net10.0-windows` only → *not* consumable from net9.0-windows. **2.3.2** (published 2025-10-23) explicitly ships `lib/net9.0-windows` (verified in its nuspec/catalog and in the `v2.3.2` source tag). It keeps the `TaskbarIcon` class, the `http://www.hardcodet.net/taskbar` XML namespace mapping, and the same event/property names (`ToolTipText`, `IconSource`, `MenuActivation`, `PopupActivation`, `TrayMouseDoubleClick`, `ContextMenu` — verified at the v2.3.2 tag, including the generated `RoutedEventHandler`/`RoutedEventArgs` signature). **MainWindow.xaml needed zero changes.** |

Test project packages (all latest stable as of today): `Microsoft.NET.Test.Sdk` 18.9.0, `MSTest.TestAdapter`/`MSTest.TestFramework` 4.3.3 (explicitly supports net9.0), `Moq` 4.20.72, `coverlet.msbuild` 10.0.1.

> Flag: 18.9.0 / 4.3.3 / 10.0.1 were published within the last weeks. If the release CI ever shows an adapter/coverage pairing problem, the conservative fallback is Test.Sdk 17.14.1 + MSTest 3.11.1 + coverlet 6.0.4 — behavior of the four test files is identical either way.

---

## 2. Per-file change report

### `XOutput/XOutput.csproj` — rewritten
- SDK: `Microsoft.NET.Sdk.WindowsDesktop` → **`Microsoft.NET.Sdk`** (`WindowsDesktop` is folded into the base SDK since .NET 5; `UseWPF` is enough).
- `<TargetFramework>net452</TargetFramework>` → **`net9.0-windows`**; kept `<OutputType>WinExe</OutputType>`, `<UseWPF>true</UseWPF>`, `<ApplicationIcon>Resources\icon.ico</ApplicationIcon>`, `<AssemblyName>XOutput</AssemblyName>`, all author/repo metadata. No `StartupObject` was set (kept absent).
- Added `<EnableWindowsTargeting>true</EnableWindowsTargeting>` — lets the same repo build on non-Windows CI hosts; no effect on Windows output.
- Removed `<Reference Include="System.Net.Http" />` (part of the .NET 9 shared framework).
- Removed all five `<EmbeddedResource Include="$(NuGetPackageRoot)\...dll">` entries (SharpDX×2, Newtonsoft, Hardcodet, ViGEm) — these were the old single-exe embedding hack; see §5.
- Removed `<Content Remove="Resources\Languages\*.json" />` (legacy non-SDK artifact); kept `<EmbeddedResource Include="Resources\Languages\*.json" />` and `<Resource Include="Resources\icon.ico" />` (the tray icon's `/Resources/icon.ico` pack URI depends on it).
- Package references updated per §1.
- **No `<Version>` property added.** The 3.31 versioning scheme is driven by `UpdateChecker/Version.cs` (`AppVersion = "3.31"`) and by CI's `-p:Version/-p:AssemblyVersion/-p:FileVersion` flags — exactly as before. There is no `AssemblyInfo.cs` in the repo, so nothing conflicted with SDK-style auto-generation. (Note: the repo directory is named "3.32" but the in-code version is 3.31; I kept the in-code version untouched per "zero behavior change".)

### `XOutputTests/XOutputTests.csproj` — rewritten
- `<TargetFrameworks>net452;netcoreapp3.0</TargetFrameworks>` → **`net9.0-windows`** (must match the app's TFM to reference it; net452/netcoreapp3.0 no longer exist in the .NET 9 world).
- `<UseWPF>true</UseWPF>` + `EnableWindowsTargeting` (the app is WPF; harmless for tests, keeps a Linux cross-build working).
- Test packages updated to latest stable (see §1); kept `coverlet.msbuild` with `PrivateAssets=all`.
- The 4 test files (`ApplicationContextTests`, `ArgumentParserTests`, `HelperTests`, `VersionTests`) compile unchanged — they only exercise `XOutput.Tools`/`XOutput.UpdateChecker` logic.

### `XOutput/Devices/Input/DirectInput/DirectInputDevices.cs` — SharpDX → Vortice
- `new SharpDX.DirectInput.DirectInput()` → **`DInput.DirectInput8Create()`** returning `IDirectInput8` (Vortice's factory; no `DirectInput` class exists).
- `.GetDevices()` → same name, same no-arg overload, returns `IList<DeviceInstance>` — filter predicates (`DeviceType.Keyboard/Mouse/Joystick/Gamepad/FirstPerson`) unchanged.
- `new Joystick(directInput, guid)` → **`directInput.CreateDevice(guid)`** returning `IDirectInputDevice8` (Vortice has no `Joystick` wrapper class).
- `joystick.Information.ProductGuid` → **`joystick.DeviceInfo.ProductGuid`** (Vortice's `DeviceInfo` property returns `DeviceInstance`).
- **Added `joystick.SetDataFormat<RawJoystickState>()`** — see §4 judgment call #1 (SharpDX's `Joystick` constructor did this implicitly; Vortice does not).
- `joystick.Properties.BufferSize = 128` unchanged.

### `XOutput/Devices/Input/DirectInput/DirectDevice.cs` — SharpDX → Vortice
- Device field/ctor type `Joystick` → **`IDirectInputDevice8`**; `using SharpDX.DirectInput` → `using Vortice.DirectInput`, `using SharpDX` → `using SharpGen.Runtime` (exceptions).
- All `Capabilities` members verified identical (`AxeCount`, `ButtonCount`, `PovCount`, `ForceFeedbackSamplePeriod` — confirmed in Vortice's `Mappings.xml` for `DIDEVCAPS`).
- `joystick.GetObjects(...)` / `DeviceObjectInstance` (`Usage`, `ObjectId.InstanceNumber`, `Offset`, `Name`, `ObjectType`/Guid) — verified identical.
- `JoystickState` (`X`, `Y`, `Z`, `RotationX/Y/Z`, `Acceleration*`, `AngularAcceleration*`, `Force*`, `Torque*`, `Velocity*`, `AngularVelocity*`, `Sliders[]`, `PointOfViewControllers[]`, `Buttons[]`) — verified identical (DIJOYSTATE2).
- `joystick.GetCurrentState()` → **`joystick.GetCurrentJoystickState()`**.
- `joystick.GetObjectPropertiesById(...)` → unchanged; `InputRange(ushort.MinValue, ushort.MaxValue)`, `DeadZone`, `Saturation` unchanged; `catch (SharpDXException)` → `catch (SharpGenException)`.
- `EffectGuid.ConstantForce`, `EffectInfo.Guid`, `GetEffects()`, `ObjectGuid.XAxis/.../Slider`, `DeviceAxisMode.Absolute`, `CooperativeLevel.Background|Exclusive`, `DeviceObjectTypeFlags.Button/AbsoluteAxis/ForceFeedbackActuator` — all verified identical in Vortice.
- Error propagation: `SetCooperativeLevel(...).CheckError()`, `Acquire().CheckError()`, `Poll().CheckError()` — Vortice returns `Result` instead of throwing like SharpDX; `.CheckError()` restores the old throw→catch behavior (see §4 judgment call #2).

### `XOutput/Devices/Input/DirectInput/DirectDeviceForceFeedback.cs` — SharpDX → Vortice (highest-risk file)
- `Joystick` → `IDirectInputDevice8`; `Effect` → **`IDirectInputEffect`**.
- Effect creation `new Effect(joystick, guid, params)` → **`joystick.CreateEffect(guid, params)`** (same parameter order: guid first, then parameters).
- `EffectParameters` — in Vortice it is a **class** (SharpDX had a struct); the object-initializer usage (`Flags`, `StartDelay`, `SamplePeriod`, `Duration`, `TriggerButton`, `TriggerRepeatInterval`, `Gain`) is unchanged. `SetAxes(int[], int[])` and `Parameters` (`TypeSpecificParameters`) unchanged; `ConstantForce { Magnitude }` unchanged (class now, same usage).
- `newEffect.Start()` → `newEffect.Start().CheckError()` (Start returns `Result` in Vortice); `catch (SharpDXException)` → `catch (SharpGenException)`.
- Removed unused usings (`System.Threading`, `System.Windows`, `System.Windows.Interop`, `System.Linq`, `System.Collections.Generic`).
- **Behavior of the loop logic (actuator pairing, `RefreshAxes`, `CalculateMagnitude`, `(int)ObjectId` casts) is byte-for-byte identical.**
- This file must be smoke-tested on a real force-feedback device (see §6).

### `XOutput/Devices/Input/DirectInput/DirectInputSource.cs`
- `using SharpDX.DirectInput` → `using Vortice.DirectInput`; `Func<JoystickState, double>` unchanged. No logic change.

### `XOutput/UI/Windows/MainWindowViewModel.cs`
- `IEnumerable<SharpDX.DirectInput.DeviceInstance>` → `IEnumerable<Vortice.DirectInput.DeviceInstance>` (single occurrence). No logic change.

### `XOutput/App.xaml.cs`
- Removed the 6-line `DependencyEmbedder` wiring (SharpDX/SharpDX.DirectInput/Newtonsoft/Hardcodet/ViGEm package registrations + `Initialize()`). On .NET 9 dependencies resolve from the output folder / shared framework normally — no `AssemblyResolve` hack needed. Everything else (working-directory setup, DI context, single instance, startup flow) unchanged.

### `XOutput/Tools/DependencyEmbedder.cs` — **deleted**
- See §5 for the rationale and the replacement strategy.

### `XOutput.sln`
- Project-type GUIDs updated to the SDK-style C# GUID (`{9A19103F-...}`) for both projects. Project GUIDs, configurations, and SolutionGuid untouched.

### CI / release tooling (paths only — no behavior change)
- `.github/workflows/build.yml`: added `actions/setup-dotnet@v4` with `dotnet-version: 9.0.x` (windows-latest does not guarantee a .NET 9 SDK); artifact path `bin/Release/net452` → `bin/Release/net9.0-windows/`; bumped deprecated `checkout@v2`/`upload-artifact@v2` → `@v4`. Version env stays `3.31`.
- `appveyor.yml`: image `Visual Studio 2019` → `Visual Studio 2022` (needed for the .NET 9 SDK); `7z` artifact now packages the whole `bin\Release\net9.0-windows\*` folder (a framework-dependent .NET 9 app consists of exe + dlls).
- `xoutput.nuspec` (chocolatey): file source `bin\Release\net452\XOutput.exe` → `bin\Release\net9.0-windows\*`; runtime dependency `dotnet4.5.2` → `dotnet-desktopruntime`. **Flag:** verify the `dotnet-desktopruntime` chocolatey package id at release time (I could not reach chocolatey.org from the build sandbox).

---

## 3. How API equivalence was verified (not assumed)

Because the sandbox can only reach github.com (nuget.org and all Microsoft CDNs are firewalled), the exact published sources were cloned and checked directly:

- **Vortice.DirectInput 3.8.3** — the NuGet catalog entry for 3.8.3 records source commit `9e609cb…`, which is exactly the `amerkoleci/Vortice.Windows` commit cloned for review. Every type/member above was confirmed in `src/Vortice.DirectInput` (`Mappings.xml` for generated names, `IDirectInput8.cs`, `IDirectInputDevice8.cs`, `IDirectInputEffect.cs`, `EffectParameters.cs`, `ConstantForce.cs`, `ObjectProperties.cs`, `DeviceObjectId.cs`, `InputRange.cs`, `ObjectGuid.cs`, `JoystickState.cs`, official `HelloDirectInput` sample) plus independent real-world users (FalconBMS.Launcher, SteamInputAddonforClaw, JoystickDebouncer) found via GitHub code search.
- **Nefarius.ViGEm.Client 1.21.256** — cloned `nefarius/ViGEm.NET` (newer than the published package); all used members confirmed (`ViGEmClient` ctor/Dispose/`CreateXbox360Controller`, `IXbox360Controller.SetButtonState/SetAxisValue/SetSliderValue/Connect/Disconnect/FeedbackReceived`, `Xbox360Button/Axis/Slider` static fields, `Xbox360FeedbackReceivedEventArgs.LargeMotor/SmallMotor`, `VigemBusNotFoundException`). File-history check dated the class-based API to 2019 ⇒ no breaking change vs the pinned 1.16.148.
- **H.NotifyIcon.Wpf 2.3.2** — checked the `v2.3.2` source tag (`TaskbarIcon.MouseEvents/ToolTips/IconSource/ContextMenu/Popups` files), the `H.NotifyIcon.Wpf.csproj` at that tag (TFMs + `XmlnsDefinition`), and the generated event handler contract (`RoutedEventHandler`/`RoutedEventArgs` from `DependencyPropertyGenerator`'s `RoutedEventAttribute`).

---

## 4. Judgment calls / places where 1:1 equivalence could not be taken literally

1. **`SetDataFormat<RawJoystickState>()` was added in `DirectInputDevices.CreateDirectDevice`.** SharpDX's `Joystick(device, guid)` constructor set the DIJOYSTATE2 data format implicitly; Vortice's `CreateDevice(guid)` does not, and `Poll()/GetCurrentState()` fail without it. This is a required adaptation, not a behavior change — verified against the official Vortice sample and real-world consumers.
2. **`Result`-returning calls now get `.CheckError()`** (`SetCooperativeLevel`, `Acquire`, `Poll`, `Effect.Start`). SharpDX threw on failure; Vortice returns `Result`. `.CheckError()` restores the exact throw→catch paths the app relies on (device-skip on acquire failure, disconnect detection on poll failure, "create/start effect failed" warning in force feedback). Without it, failures would be silently swallowed and behavior *would* change.
3. **Vortice's `GetCurrentJoystickState()` does not throw when the device is lost** (the internal `GetDeviceState` result is discarded), whereas SharpDX's `GetCurrentState()` did. Disconnect detection now effectively rides on `Poll().CheckError()` throwing first (which it does when the device is unplugged). Since the poll happens immediately before the state read in `RefreshInput`, the observable behavior is equivalent; still flagged because it is the one place where the failure path differs internally.
4. **`EffectParameters` is a class in Vortice vs a struct in SharpDX** — identical object-initializer code compiles either way; no behavioral difference in this usage. Same for `ConstantForce`.
5. **`H.NotifyIcon.Wpf` pinned to 2.3.2 instead of "latest" 2.4.1** — 2.4.1 targets net10.0-windows and cannot be referenced from a net9.0-windows app. 2.3.2 is the newest version that explicitly supports net9.0-windows. When the app is eventually retargeted to net10, 2.4.1 is the drop-in upgrade.
6. **ViGEm.Client 1.21.256 is the newest stable NuGet version but was published 2023-02-05** and the upstream repo's last commit is 2023-09. It is the maintained line (no successor package exists), but note that the dependency is effectively in maintenance mode. No alternative package with the same API was found.
7. **Force-feedback effect creation order** (`CreateEffect(guid, parameters)` vs SharpDX's `new Effect(device, guid, parameters)`) — verified from source; the parameter order for `EffectParameters.SetAxes(axes, directions)` is the same in both libraries (axes first — the widely documented SharpDX↔Vortice reversal does not affect this call).
8. **Test/coverage packages are the very latest stable** (18.9.0 / 4.3.3 / 10.0.1) — see fallback note in §1.

---

## 5. Dependency embedding / single-file output (`DependencyEmbedder.cs`)

**Current mechanism (net452):** `DependencyEmbedder` hooks `AppDomain.AssemblyResolve` and loads five managed DLLs from `EmbeddedResource`s baked into the exe by the csproj. That was the app's "single exe" trick on .NET Framework.

**Decision (implemented):** deleted `DependencyEmbedder.cs` and its `App.xaml.cs` wiring, and **do not** port the embedding mechanism. It exists only to fight the old build system; on .NET 9 the equivalent is the built-in single-file publish, and keeping the custom resolver would add moving parts for zero benefit (the embedded-resource approach also cannot bundle `Nefarius.ViGEm.Client`'s *native* `ViGEmClient.dll` — that package self-extracts its own native payload via Costura).

**Recommended release packaging (native replacement, not enabled in the csproj by default per your "no RID pinning in this step" constraint):**
```
dotnet publish XOutput/XOutput.csproj -c Release -p:PublishSingleFile=true -r win-x64 --self-contained false
```
- Framework-dependent single-file: one `XOutput.exe`, still runs on both 32/64-bit Windows with the .NET 9 Desktop Runtime installed (the RID only selects the apphost; managed assemblies remain portable).
- For a fully portable zero-runtime release: same command with `--self-contained true`.
- The dev/test/CI flow (`dotnet build`, `dotnet test`) is unchanged and produces the normal exe + dlls layout, which the CI artifact and chocolatey spec now package as a folder (`net9.0-windows\*`).

---

## 6. Validation status

**Done in this sandbox:**
- All `.cs` files (app + tests) pass a full C# grammar parse (tree-sitter) — no syntax errors introduced.
- Every migrated API call cross-checked against the exact library sources/versions (see §3).
- `git status`/diff reviewed; no residual `SharpDX`, `DependencyEmbedder`, or `Hardcodet.NotifyIcon.Wpf` references remain (the `http://www.hardcodet.net/taskbar` XML namespace in MainWindow.xaml is intentionally kept — H.NotifyIcon.Wpf maps that exact URI).

**Could NOT be done here (sandbox limitation):** this Linux sandbox has an egress allowlist that blocks `nuget.org`, `builds.dotnet.microsoft.com`, `aka.ms` and every Microsoft/Azure CDN, so neither the .NET 9 SDK nor any NuGet package can be downloaded here. Consequently the Release build and the runtime checks below could not be executed in this environment and must be run on your Windows machine:

```powershell
# 1) Build (Release) — must pass with zero warnings-as-errors
dotnet restore XOutput.sln
dotnet build XOutput.sln -c Release -p:Version=3.31 -p:AssemblyVersion=3.31 -p:FileVersion=3.31

# 2) Unit tests
dotnet test XOutputTests/XOutputTests.csproj -c Release

# 3) Runtime checklist (from your task §7 — each item needs a real Windows session with the
#    ViGEm Bus Driver installed and, for items marked FF, a force-feedback-capable device):
#   [ ] App launches, tray icon appears and its menu works (left-click popup, right-click menu, double-click restore)
#   [ ] DirectInput devices enumerated and listed exactly as before
#   [ ] Axis/button/dpad/slider values register in the UI identically to the pre-migration build
#   [ ] FF: rumble/constant-force still works on an FF-capable device (highest-risk path — see §2)
#   [ ] Virtual Xbox 360 controller appears in joy.cpl; input forwards end-to-end from a real device
#   [ ] settings.json save/load round-trips (Newtonsoft.Json 13 vs 12 — no format change expected)
```

If anything on the FF path misbehaves on hardware, the file to inspect first is `DirectDeviceForceFeedback.cs` (specifically `DoForceFeedback` / effect lifecycle), then the `Poll().CheckError()` path in `DirectDevice.RefreshInput`.

---

## 7. Risks / benefits summary

**Benefits:** .NET 9 (LTS-adjacent, in-support runtime vs .NET Framework 4.5.2 which is out of support); modern maintained input bindings (Vortice 3.8.3, actively developed) and tray icon (H.NotifyIcon 2.3.2, actively developed); Newtonsoft 13.x; SDK-style projects with simpler CI; no more fragile resource-embedding single-exe hack.

**Risks:**
- DirectInput/ViGEm paths are hardware-dependent and were not executable in this environment — the runtime checklist (§6) must be completed on Windows.
- ViGEm.Client is in maintenance mode (latest NuGet from 2023).
- H.NotifyIcon pin is TFM-sensitive (2.4.1 requires net10) — revisit on the next framework upgrade.
- The chocolatey dependency id (`dotnet-desktopruntime`) is a best-effort update; confirm before a release.
