# XOutput — "Kinetic Console" dark theme (Prompt 2)

Date: 2026-08-20
Scope: color / typography / control styling only. **No layout restructuring** (that is Prompt 3). Nothing from the Prompt 1 migration was reverted.

---

## 1. What was added

| File | Purpose |
|---|---|
| `XOutput/UI/Themes/Colors.xaml` | All 20 palette colors + a matching `SolidColorBrush` for each, derived interactive brushes (button hover `#95E26F`, pressed `#63D42B`, translucent gloss, row "input active" flash), status accents (warning), the Xbox controller art accents (A/B/X/Y etc., formerly hardcoded in `ColorConverter`/`XBox.xaml`), shadow color, and the **shape tokens** (`CornerRadiusSmall` 8 / `Default` 12 / `Large` 16 / `Pill` 9999) and **spacing tokens** (8 px grid, page margin 24, card padding 16, stack gap 12, element gap 8). |
| `XOutput/UI/Themes/Typography.xaml` | `FontInter` / `FontJetBrainsMono` resources with **runtime fallback** (`…/#Inter, Segoe UI, Arial` and `…#JetBrains Mono, Consolas, Lucida Console`) and the 7 named TextBlock styles from the spec (`TextDisplayLg` 32 Bold, `TextHeadlineMd` 24 SemiBold, `TextTitleSm` 18 SemiBold, `TextBodyBase` 14 Regular, `TextBodySm` 12 Regular, `TextDataMono` 13 Medium mono, `TextLabelCaps` 11 Bold + CharacterSpacing 50). |
| `XOutput/UI/Themes/Styles.xaml` | Implicit (TargetType-only) styles: Window, Button, TextBox, PasswordBox, ComboBox + ComboBoxItem, CheckBox, RadioButton, ScrollBar (thin), ProgressBar (h+v), GridSplitter, Menu, MenuItem, ContextMenu, Separator, GroupBox (temporary bordered card per spec), ToolTip. |
| `XOutput/Resources/Fonts/Inter/` | `Inter-Regular.ttf`, `Inter-SemiBold.ttf`, `Inter-Bold.ttf`, `LICENSE.txt` (OFL). |
| `XOutput/Resources/Fonts/JetBrainsMono/` | `JetBrainsMono-Medium.ttf`, `OFL.txt`. |
| `XOutput/UI/ThemeHelper.cs` | Resolves theme brushes from application resources at runtime (used by converters/view models so no code hardcodes colors). |
| `XOutput/XOutput.csproj` | `<Resource Include>` entries embedding the font files + licenses. |

**Fonts note:** the Inter static weights were produced by instantiating the official `InterVariable.ttf` (from `rsms/inter`, commit-pinned via the GitHub API) with `fontTools.varLib.instancer` at wght 400/600/700 and normalizing the name/`OS/2` tables so the three files register as one "Inter" family with correct weights. JetBrains Mono Medium was taken from `JetBrains/JetBrainsMono` and its name table normalized so the family is exactly "JetBrains Mono" (the pack-URI fragment `#JetBrains Mono`). Verified with fontTools: family names + `usWeightClass` (400/600/700/500).

## 2. Views updated (colors → theme brushes, no layout edits)

- All 5 windows: `Background="{StaticResource BrushBackground}"` (MainWindow, SettingsWindow, DiagnosticsWindow, InputSettingsWindow, AutoConfigureWindow).
- `XBox.xaml` — every one of the 106 hardcoded hex colors replaced with theme brushes (attribute-aware: black fills → `BrushBackground`, black strokes → `BrushOutlineVariant`, body greys → surface tokens preserving the original light-to-dark shading order, translucent gloss `#73F3F3F3` → `BrushSurfaceBrightGloss`, analog-stick arrows → `BrushError`/`BrushErrorContainer`).
- `DPadView.xaml`, `Axis2DView.xaml` — black borders → `BrushOutlineVariant`, red indicators → `BrushError`.
- `DiagnosticsItemView.xaml` — LightGray separator → `BrushOutlineVariant`; Green/White/Yellow/Orange/Red status icons → `BrushSuccess` / `BrushOnPrimaryContainer` / `BrushWarning` / `BrushWarningDark` / `BrushError`.
- `BoolToBrushConverter.cs` — now returns `BrushPrimary` (true) / `BrushSurfaceContainerHighest` (false) via `ThemeHelper` (previously `Color.FromRgb` literals).
- `ColorConverter.cs` — the A/B/X/Y/bumper/trigger/stick/start/home/DPad button colors moved into `Colors.xaml` as `BrushXbox*` resources; the converter resolves them through `ThemeHelper` (same runtime behavior, zero hardcoded colors).
- `InputViewModel.cs` / `ControllerViewModel.cs` — idle row background → `BrushSurfaceContainerLow`, "recent input" flash → `BrushInputActive` (translucent primary tint) instead of `Brushes.White` / `Brushes.LightGreen`.
- `MainWindow.xaml` — the log `TextBox` now uses `FontJetBrainsMono` 13 Medium (the `TextDataMono` treatment) in addition to its implicit dark style.

Result of the automated sweep: **zero hardcoded colors remain in any view XAML** (the only hex values left in the project live in `Colors.xaml`, the token file).

## 3. Judgment calls (flagged explicitly)

1. **Button padding `16,6` instead of the spec's `16,10`.** Several existing buttons are fixed at `Height="30"` (InputSettingsWindow HidGuardian buttons). With 16,10 the vertical content area would be 10 px < the ~14.5 px line height, clipping localized labels vertically. 16,6 keeps the spec's horizontal padding and the card look while leaving 18 px for text. Horizontal padding stays exactly per spec.
2. **Global base font size kept at WPF default 12** (not 14). The named text styles implement the 14/16/18/24/32 scale; the implicit Window style keeps 12 so every existing fixed-width control keeps its pre-theme text metrics. With 14 px, several fixed-width buttons in German/Russian measurably overflow more (see §4); 12 px is the safe "no regression" choice until Prompt 3 widens/restructures those controls.
3. **Button content is not clipped by the rounded corner.** Long German/Russian labels on `Width=150` buttons (e.g. "Aktualisierung erzwingen", "Einstellungen speichern") never fit that width — pre-theme the text spilled past the button edges. A rounded Border would clip it (a readability regression). The template therefore renders the content as a sibling of the rounded border, preserving the pre-theme behavior (full text visible). Widening those buttons is a Prompt 3 layout fix.
4. **Top-level menu items use compact padding (12,1) and the menu bar border is 4,0** because MainWindow places the Menu in a fixed 20 px grid row — the default 12,6 padding would clip. Submenu items keep 12,6.
5. **`TextLabelCaps` "uppercase"** — WPF has no XAML-level uppercase transform; the style supplies size/weight/tracking and the convention (uppercase source strings) is documented in the style comment.
6. **GroupBox restyled as a bordered card exactly as specified** (SurfaceContainer, 1 px OutlineVariant, radius 12) — acknowledged as temporary until Prompt 3/4 cards.
7. **XBox controller artwork re-tinted to the theme** (zero-hardcoded-colors mandate). Shading *relationships* were preserved by mapping light→dark greys onto `SurfaceBright → … → SurfaceContainerHigh`; the classic A/B/X/Y accents were kept as explicit `BrushXbox*` tokens so the controller stays recognizable.

## 4. Font fallback

Implemented as **composite font families** (the explicit, documented WPF mechanism):
`FontInter = pack://application:,,,/Resources/Fonts/Inter/#Inter, Segoe UI, Arial` and
`FontJetBrainsMono = pack://application:,,,/Resources/Fonts/JetBrainsMono/#JetBrains Mono, Consolas, Lucida Console`.
If the embedded pack-URI font cannot be resolved, WPF uses the next family in the list instead of silently defaulting.

**Manual verification (required on Windows — cannot be executed in this Linux sandbox):** temporarily rename `XOutput\Resources\Fonts\Inter\Inter-Regular.ttf` (and/or the whole `Inter` folder), rebuild, launch, and confirm the UI renders in Segoe UI; repeat for `JetBrainsMono-Medium.ttf` → Consolas. This must be done on a Windows machine with the app running; I could not run WPF here (no Windows runtime, and the sandbox cannot download the .NET SDK/NuGet packages — same limitation as Prompt 1).

## 5. Readability verification — German & Russian (measured, not guessed)

All strings were measured with the **actual embedded fonts** (Inter SemiBold/Regular, JetBrains Mono) using their true glyph advances, against the real fixed-width constraints in the XAML:

- Settings window labels (col ≈ 310 px): "In die Taskleiste minimieren" 166 px, "Beim Windows-Start ausführen" 183 px, "HidGuardian setup during startup" 193 px, RU "Запускать при старте Windows" 219 px → **fit**.
- "Alle Eingabegeräte anzeigen" 170 px / RU 190 px vs ≈ 500 px card → **fit**.
- Buttons with auto width (Add controller, Disable/Save/Cancel, Start/Stop/Bearbeiten, HidGuardianAdd EN) → **fit**.
- GroupBox headers (SemiBold 16): "Spielkontroller" 115 px vs hundreds of px → **fit**.
- Pre-existing German/Russian overflows (unchanged behavior, text stays fully visible): "Aktualisierung erzwingen" 151 px and "Einstellungen speichern" 142 px vs 118 px content in the `Width=150` buttons; "Remove as affected device from HidGuardian" 271 px and RU "Добавить устройство для скрытия с помощью HidGuardian" 375 px vs ≈ 253 px column. These strings **never fit** those controls (they overflowed with Segoe UI at 12 px pre-migration too); the theme keeps the full text visible rather than clipping it (see judgment call 3). Widening these buttons is listed for Prompt 3.

## 6. Screenshots / previews — honest status

**This sandbox is Linux; WPF cannot run here** (and the egress allowlist blocks nuget.org/Microsoft CDNs, so no .NET SDK is installable). Real WPF screenshots therefore cannot be produced from this environment. Provided instead:

- `preview-artifacts/MainWindow-theme-preview.png` — pixel-accurate render (exact palette + the real embedded font files, German strings, MainWindow layout) built with PIL; the theme colors were verified by pixel sampling.
- `preview-artifacts/SettingsWindow-theme-preview.png` — same for the Settings modal.
- `preview-artifacts/theme-preview.html` — live browser preview of both windows (served at the sandbox preview URL; uses the exact `.ttf` files via `@font-face`), including button hover states.

**These are theme previews, not WPF screenshots.** To capture real screenshots on Windows: build (`dotnet build XOutput.sln -c Release`), run `XOutput.exe`, and screenshot MainWindow + e.g. SettingsWindow (or DiagnosticsWindow). The pixel colors/fonts in the previews are exactly what the WPF theme declares, so they are a faithful representation of the theme's palette and typography.

## 7. Validation performed

- All 130+ `.cs` files parse clean (tree-sitter C# grammar).
- Every XAML file is well-formed XML.
- StaticResource cross-check: every `{StaticResource …}` referenced by any view exists in the merged dictionaries (140 keys) — including references *inside* the theme files themselves.
- No hardcoded hex/named colors remain in any view XAML; no `Color.FromRgb`/`Brushes.*` remain in UI code.
- Font files verified with fontTools (family names, weights) before embedding.
- German/Russian overflow analysis per §5.

**Still required on Windows (cannot be done here):** Release build + runtime smoke test, visual review of all 5 windows, tray menu, and the font-fallback manual test (§4).
