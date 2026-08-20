# XOutput — Final pass: live controller visualization, icon set, full validation (Prompt 5)

Date: 2026-08-20
This is the final deliverable of the Kinetic Console redesign: .NET 9 (`net9.0-windows`), single-window shell, dark theme, themed pages, live-reactive controller visualization, one consistent icon set. Prompts 1–4 work is untouched.

---

## 1. Live-reactive controller visualization — `XOutput/UI/Component/XBox.xaml` (rebuilt)

**What changed:** the 1056-line / ~92 KB static SVG-style illustration was replaced with a 32 KB live-reactive WPF drawing.

**Why:** the old artwork was static — it could not express input activity beyond the pre-computed highlight logic embedded in 791 `Path` elements. The rebuild renders the same information (which XInput element is being configured) with dynamic `BrushPrimary` fills, and is 65 % smaller.

**How (rendering swap, no new data source):**
- **Same model, same component contract**: `XBoxModel` (`XInputType` + `Highlight`) and `XBoxViewModel` are untouched; `XBox.xaml.cs` (a `Viewbox` subclass with the `XInputType`/`Highlight` dependency properties) is byte-for-byte unchanged. The wizard's existing bindings (`XInputType="{Binding Model.XInput}"`, `Highlight="{Binding Model.Highlight}"`) keep working.
- **Controller outline**: a single `Path` silhouette (440×250 canvas) stroked **2 px `BrushOutlineVariant`**, plus two 1.4 px grip detail lines — the "outline only" resting state.
- **Dynamic elements** (all driven by `MultiDataTrigger`s on `Model.XInputType` + `Model.Highlight`):
  - face buttons A/B/X/Y (glow circle + letter label),
  - shoulders LB/RB and triggers LT/RT (inset rounded-rect fills),
  - d-pad UP/DOWN/LEFT/RIGHT (per-arm fills),
  - left stick (LX/LY/L3) and right stick (RX/RY/R3) (glow ring + cap),
  - Start/Back/Home (small pill/circle fills),
  - XBOX wordmark (static, `BrushOnSurfaceVariant`).
- **Opacity proportional to the live signal**: the model exposes `Highlight` (the wizard's 500 ms blink), not raw analog values, so the fill opacity is 0.9 while the active element blinks on and 0.35 while off; inactive elements are 0 (outline only). This is the faithful mapping of "opacity proportional to the live input value" given the existing model — full analog deflection would require a model change (out of scope per the "rendering swap, not a new data source" constraint).

**Usage:** kept in the wizard's "Test Inputs" step (`WizardOverlayView`) **and added to the Mapping page's Live Preview** (`MappingPage.xaml`): bound to the selected target (`XInputType` ← `SelectedTarget`, with `TargetNullValue` so no binding error occurs before a mapping exists) and `Highlight` ← `HasMapping` (steady fill while a mapping is selected; the control collapses when no mapping exists).

**Benefits:** live feedback of the configured input; smaller file; consistent with the theme (all colors from tokens, zero hex literals).

**Risks:** the silhouette was hand-authored geometry — visual proportions should be eyeballed on Windows once (see §5); the stick/trigger "position" is not data-driven (model has no analog values) — only activity highlighting, documented above.

## 2. Icon set — `XOutput/UI/Themes/Icons.xaml` (new)

**Chosen method: (a) vector Path-based icons** — vertical 2 px-stroke WPF `Geometry` resources in a shared 20×20 coordinate space.

**Why (documented per the task):** zero external dependency (no NuGet package, no font embedding, no license/CDN concerns — important for an offline utility); it matches the rendering approach the shell sidebar already used, so ONE consistent method applies app-wide instead of mixing Paths with an icon font; and it renders identically at any DPI/size via `Stretch=Uniform`.

**Icons defined (19):** `IconHome`, `IconControllerTest`, `IconMapping`, `IconDiagnostics`, `IconSettings` (sidebar), `IconEdit`, `IconRemove`, `IconStart`, `IconStop`, `IconConfigure`, `IconAdd`, `IconRefresh`, `IconExport`, `IconSave` (actions), `IconCopy`, `IconChevronDown`, `IconChevronUp` (console drawer), `IconWizard`, `IconInfo` (overlays).

**Applied everywhere (no mixing):**
- `ShellViewModel` nav items now resolve their geometry from `Icons.xaml` (via `TryFindResource`, with a placeholder fallback) instead of inline string literals.
- `MainShellView` drawer buttons (copy + collapse chevrons) → `StaticResource IconCopy/IconChevron*`.
- `ControllerView` remove/edit/start actions → `IconRemove/IconEdit/IconStart`, with a `DataTrigger` on `Model.ButtonText == "Stop"` switching the start button to `IconStop` (the trigger reads the literal, so it works in every language).
- `InputView` Configure → `IconConfigure`; `HomePage` Add Controller → `IconAdd`, Force Refresh → `IconRefresh`.
- `MappingPage` Export Profile → `IconExport`, Save Mapping → `IconSave`.
- `WizardOverlayView` stepper header → `IconWizard`; `AboutOverlayView` → `IconInfo`.

**Validation:** automated check confirms every `Icon*` referenced in XAML is defined and every defined icon is used (the 5 sidebar icons are consumed via code, verified separately). The only inline `Path Data="M ..."` left outside the theme are the controller silhouette in XBox.xaml (artwork, not an icon) and the crosshair gridlines in the D-pad/Axis2D/Live-Preview indicators (functional guides, not icons).

## 3. Full validation pass (Prompt 3 checklist)

| Check | Result |
|---|---|
| Release build net9.0-windows, no new warnings | ⚠️ **Cannot execute in this sandbox** (Linux, no .NET SDK — NuGet/Microsoft CDNs firewalled, same limitation as Prompts 1–4). All C# parses clean via tree-sitter; no analyzer-visible new patterns introduced (new code follows existing style). To run on Windows: `dotnet build XOutput.sln -c Release`. |
| Single OS window at runtime | ✅ Statically verified: grep finds exactly one `: Window` class (`MainWindow`); zero `ShowDialog()`; zero `new …Window`; zero `MessageBox.Show`. Runtime re-check on Windows (taskbar/Alt-Tab) still required. |
| Every binding from original ViewModels/Models resolves | ✅ Static: all XAML well-formed; every `{StaticResource}` resolves (170 resources incl. Icons.xaml); every `x:Class` has code-behind; every `ConverterParameter` key exists in English.json; XBox bindings verified against the unchanged `XInputType`/`Highlight` DPs. Runtime binding-error pass on Windows still required (Prompt 3 §5). |
| All 9 languages, full pass, no truncation | ✅ Measured every constrained string in all 9 languages with the real embedded fonts: all sidebar nav labels fit the 175 px text area; all long strings (`NoControllerForMapping`, `VirtualGamepadInfo`, `VigemNotInstalled`, …) render in `TextWrapping="Wrap"` blocks (multi-line, no clipping); CJK/Cyrillic render via WPF per-glyph font fallback. The old `Width=150` button overflow (`Aktualisierung erzwingen`) is gone — the redesigned buttons are auto-width. |
| Force feedback + virtual controller end-to-end | ⚠️ Requires Windows + ViGEm driver + FF-capable device (Prompt 1 §7 checklist); code path untouched by this pass (only XBox visuals + icons changed). |
| Tray icon / minimize-restore / exit / single-instance | ✅ Code untouched by this pass (`MainWindow.xaml.cs` lifecycle handlers unchanged); runtime re-check on Windows. |
| No hardcoded colors outside `UI/Themes/*.xaml` | ✅ **Final hex grep: zero hex color literals in any `.xaml` outside Themes.** The only named-color literal remaining is `Transparent` used as the WPF hit-testing idiom (clickable transparent areas on icon buttons and sidebar rows) and `{x:Null}` fills on pure outlines — neither is a visual color. |
| Change report per modified file | ✅ This document (§4). |

## 4. Per-file change report

| File | What / Why / Benefits / Risks |
|---|---|
| `XOutput/UI/Component/XBox.xaml` | Rebuilt as live-reactive controller (see §1). Benefits: dynamic activity fill, 65 % smaller, fully themed. Risks: hand-authored outline geometry → eyeball on Windows; analog values not represented (model unchanged). |
| `XOutput/UI/Themes/Icons.xaml` (new) | The single icon set (see §2). Benefit: one source of truth, no dependency. Risk: icons are hand-drawn paths — minor visual polish may be wanted later. |
| `XOutput/App.xaml` | Merges `Icons.xaml` into the application resources. |
| `XOutput/UI/Shell/ShellViewModel.cs` | Nav icons resolve from `Icons.xaml` (`TryFindResource`) instead of inline strings; placeholder fallback preserves behavior if a resource is missing. |
| `XOutput/UI/Shell/MainShellView.xaml` | Drawer copy/chevron icons → shared resources. No behavior change. |
| `XOutput/UI/Component/ControllerView.xaml` | Icons → shared set; start/stop button icon switches via `DataTrigger` on the literal `ButtonText`. No event/binding changes. |
| `XOutput/UI/Component/InputView.xaml` | Configure button icon → `IconConfigure`. |
| `XOutput/UI/Shell/Pages/HomePage.xaml` | Add Controller / Force Refresh buttons gain `IconAdd` / `IconRefresh` (auto-width, fixing the old German overflow). |
| `XOutput/UI/Shell/Pages/MappingPage.xaml` | Export/Save buttons gain icons; **Live Preview gains the new XBox** bound to the selected target (`TargetNullValue` avoids null-binding noise; collapsed without a mapping). |
| `XOutput/UI/Shell/Overlays/WizardOverlayView.xaml` | Stepper header gains `IconWizard`. Bindings to XBox unchanged. |
| `XOutput/UI/Shell/Overlays/AboutOverlayView.xaml` | About header gains `IconInfo`. |
| `preview-artifacts/xbox-preview.html` (new) | Interactive SVG mirror of the new controller for browser review (click an input to light it up, toggle the blink). |

Unchanged on purpose: `XBoxModel`, `XBoxViewModel`, `XBox.xaml.cs`, all ViewModels/Models from Prompts 1–4, `MainWindow` lifecycle, tray icon, single-instance logic.

## 5. Still required on Windows (cannot run in this Linux sandbox)

```powershell
dotnet build XOutput.sln -c Release -p:Version=3.31 -p:AssemblyVersion=3.31 -p:FileVersion=3.31
dotnet test XOutputTests/XOutputTests.csproj -c Release
```
Then run: confirm one taskbar entry; open wizard + Mapping page and verify the controller glow follows the configured input (blink in wizard, steady in Mapping); eyeball the new controller silhouette proportions; tray icon minimize/restore/exit; no binding errors in the debug output; FF + ViGEm end-to-end per Prompt 1 §7.
