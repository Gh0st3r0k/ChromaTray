# Development guide

ChromaTray is a .NET 8 Windows Forms application with a small set of platform-independent algorithms. Start with [Running locally](RUNNING.md) to launch it, then use this guide to find the code, understand resource ownership, and validate changes.

## Prerequisites and repository layout

Use the **.NET 8 SDK, 8.0.4xx**, selected by [global.json](../global.json). Windows 10/11 x64 is required for the application and full test suite. Linux/macOS can run the core tests; the Windows project also supports cross-compilation through `EnableWindowsTargeting`.

| Location | Responsibility |
| --- | --- |
| `Program.cs` | Initialize WinForms, acquire the single-instance mutex, and start the dashboard. |
| `Forms/MainDashboardForm.cs` | Own application lifetime, tray commands, settings, and global hotkeys. |
| `Forms/MainDashboardForm.Layout.cs` | Construct the dashboard and adapt its cards, tabs, and inspector to size and DPI. |
| `Forms/MainDashboardForm.Capture.cs` | Prepare capture, own captured-region copies, cancel analysis, and apply results. |
| `Forms/MainDashboardForm.Palette.cs` | Handle palette selection, grouping, inspector updates, and clipboard commands. |
| `Forms/ScreenOverlayForm.cs` | Own the frozen desktop bitmap and process point/region input. |
| `Forms/ScreenOverlayForm.Painting.cs` | Paint the magnifier, pixel details, and region selection. |
| `Forms/SettingsForm*.cs` | Edit isolated preference copies, validate them, and save before applying. |
| `Forms/Controls/` | Draw themed buttons, palette rows, dropdowns, scrolling, and DPI-aware layouts. |
| `Models/` | Store preferences, shortcut bindings, capture modes, and formatted color data. |
| `Services/` | Prepare desktop capture, navigate pixels, analyze palettes, export text, register hotkeys, and validate settings. |
| `ChromaTray.Core.Tests/` | Test linked algorithm sources without a Windows desktop dependency. |
| `ChromaTray.Tests/` | Test the Windows application, bitmap handling, native shortcuts, settings, layout, and appearance. |
| `docs/site/` | DocFX configuration, English guides, translated guides, and the shared template. |
| `scripts/` | Set up the cloud SDK, run cloud checks, and build all documentation languages. |

## Application lifetime

`Program` uses a named mutex to prevent multiple instances from competing for hotkeys and tray icons. The dashboard initializes settings, controls, and the tray menu. Closing the dashboard hides it; choosing **Exit** shuts down the application and disposes its resources.

Global hotkeys are tied to the dashboard's native window handle. Keep registration and cleanup aligned with handle creation and destruction. WinForms may create a handle before all child controls have been initialized, so early event handlers must tolerate incomplete layout initialization.

## Capture and analysis

1. A dashboard button, tray command, or registered shortcut starts capture.
2. `DesktopCaptureScope` prepares the dashboard for capture, disables its transitions, hides it, and waits for desktop composition. Supported Windows versions also exclude the window from capture temporarily.
3. `ScreenOverlayForm` captures the virtual desktop into an uncompressed bitmap and owns that snapshot.
4. Point mode samples the original bitmap. Region mode returns a cropped original image after a drag.
5. The dashboard copies a confirmed region for later reanalysis. Each background analysis receives a further independent copy.
6. `PaletteAnalyzer` reads bitmap rows and passes RGB values to `PaletteHistogram`. The latest valid request updates the dashboard.

Cancellation alone does not prevent an older task from finishing. The request version check also prevents stale results from replacing a newer capture or grouping choice. Preserve both protections when changing background work.

### Coordinates and drawing

Screen coordinates are **physical pixels**, including negative coordinates on monitors placed left of or above the primary display. Bitmap coordinates are offsets from the virtual desktop's upper-left corner. Convert between them explicitly.

`PixelNavigator` restricts movement to actual monitor rectangles. The overlay intercepts arrow keys and Enter before WinForms treats them as focus-navigation commands. Each arrow step is one physical pixel, independent of display scaling.

Paint overlays and the magnifier without changing the snapshot. The magnifier draws a separate cell for each original pixel. Point mode leaves the image untinted; region mode dims outside the selection. The native picker cursor provides the crosshair.

### Bitmap ownership

The overlay disposes its snapshot and cropped image. Copy any image that must survive the overlay. A background task must own its bitmap for the entire analysis: a later capture can dispose the dashboard's previous region.

Always release `LockBits` in a `finally` path, including cancellation and exceptions. Row stride may be negative, and 32-bit ARGB bitmap bytes are accessed as BGRA in memory. Unsupported input formats are converted without changing the caller's bitmap.

## Palette and export rules

Exact RGB counting is the default. Optional grouping uses channel buckets with a step of eight. A bucket's displayed representative is its most frequent **original** RGB value, with lower RGB breaking frequency ties. Never display the bucket center as a sampled color.

Count percentages over the full region before applying the 500-entry display limit. Listed percentages can therefore sum to less than 100%. Preserve deterministic frequency/RGB ordering so scan direction does not affect the result.

`ColorDetails` formats the original color using invariant numeric formatting. HSL and HSV share a hue calculation; CMYK is a mathematical conversion without a printing profile. `PaletteExporter` returns text independently of the clipboard and UI. CSS export uses HEX for a primary HSV/CMYK format; JSON serialization escapes representation strings and writes numeric `sharePercent` values.

## Settings and layout

Preferences live at `%LocalAppData%\ChromaTray\settings.json`. Loading normalizes formats and shortcuts and falls back to defaults for unreadable or invalid JSON. Saving writes a temporary file beside the destination before replacing the existing file.

The settings dialog edits shortcut copies. It validates the candidate and saves it before updating live preferences, so Cancel or a failed save cannot partially change the active configuration.

`StudioTheme` centralizes colors, shared fonts, and drawing helpers. Shared fonts live for the application's lifetime. `DpiStackPanel` retains logical dimensions at 96 DPI and scales them for the current window. Preserve that baseline when creating controls after a DPI change. Rounded controls must paint their parent's background in the corners; treating them as opaque rectangles causes visible corner artifacts.

### Window restoration and painting

The dashboard builds its controls once and finishes initial sizing in `OnLoad`,
before the first visible frame. Closing with X hides the window; taskbar minimize
keeps it visible in the taskbar. Tray commands use `ShowMainWindow`, while native
taskbar restoration arrives through `WM_SIZE`. Preserve both paths when changing
window lifetime behavior.

Layout caches include their size, DPI, and selected-tab inputs. Minimized windows
skip responsive panel reconfiguration. Format controls are rebuilt only when the
active formats or DPI change. `StudioScrollViewport` owns the format table's
bounds; keep that table's `AutoSize` disabled so values and right-aligned Copy
buttons retain the available width.

Flat dashboard surfaces use solid theme backgrounds and local panel buffering.
Rounded controls still preserve the painted background outside their curves.
Avoid whole-window `WS_EX_COMPOSITED`: it produced a black frame during restore
on the user's Windows machine. Automated layout checks do not prove smooth
presentation; use the Windows checklist and the optional [timing diagnostics](RUNNING.md#startup-tray-and-taskbar-restore-timings)
for real desktop verification. `UiPerformance` is disabled unless its log path is
set before startup; idle timings exclude process creation and runtime loading.

## Build and test

From the repository root on Windows:

```powershell
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
dotnet build ChromaTray.csproj -c Release --no-restore
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore --logger "console;verbosity=normal"
```

The Windows project covers native and graphical behavior, including bitmap edge cases, shortcut dispatch, settings recovery, keyboard capture, and layout/appearance at multiple DPI values. Tests create synthetic captures where a real desktop would make results unstable.

For shared algorithms on any supported SDK host:

```powershell
dotnet restore ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj --locked-mode
dotnet test ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj -c Release --no-restore
```

The core project links the application's actual algorithm files. It does not maintain a second implementation. Changes to Windows input or painting also need the [manual Windows checklist](WINDOWS-CHECKLIST.md).

### Diagnostics and CI

[Build, test & release](../.github/workflows/release.yml) runs Linux core tests and the complete Windows suite on pushes and pull requests. It uploads TRX reports and Windows preview PNGs. Windows tests use a 90-second hang timeout and produce a small dump when that timeout is reached.

To save preview images during a local Windows test run:

```powershell
$env:CHROMATRAY_UI_PREVIEW_DIR = Join-Path $PWD 'artifacts/ui-preview'
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore
```

Inspect the failed test's input and diagnostics before adjusting an assertion. Pixel accuracy and layout constraints should be checked against the intended behavior.

## Documentation

Repository guides and source comments are written in English. DocFX guides additionally have Slovak, Czech, Russian, and Ukrainian translations. Keep those translated files in their respective languages.

| Source | What to edit |
| --- | --- |
| `README.md` | Project overview, quick start, controls, and links. |
| `docs/*.md` | Detailed setup, development, and manual verification. |
| C# `///` comments | English API summaries, parameters, returns, and remarks generated by DocFX. |
| C# `//` comments | Local implementation intent, ownership, coordinate rules, and non-obvious constraints. |
| `docs/site/*.md` and `docs/site/api/index.md` | English site guides and API introduction. |
| `docs/site/locales/<language>/` | Translated guides and navigation. |
| `docs/site/locales/<language>/template/token.json` | Translated DocFX interface labels. |
| `docs/site/template/public/` | Shared styling, language selector, and Unicode search worker. |

Install Python 3 alongside the SDK and run:

```powershell
dotnet tool restore
dotnet restore ChromaTray.csproj --locked-mode
python scripts/build-docs.py
dotnet docfx serve artifacts/docs-site --port 8080
```

On Linux/macOS, use `python3` if needed. Open `http://localhost:8080`. English stays at the root; translations use `/sk/`, `/cs/`, `/ru/`, and `/uk/`.

The builder extracts API metadata once and reuses it for every language. It creates a separate search index per version and places temporary configuration in the ignored `docs/site/obj` directory. HTML is written to the ignored `artifacts/docs-site` directory. Never edit generated API YAML or HTML to make a lasting documentation change.

The local tool manifest pins DocFX. Its API descriptions come from English XML comments; ordinary source comments do not appear in API pages. The shared search worker preserves Unicode terms and supports partial identifiers without needing separate search-language plugins.

### GitHub Pages

[Documentation](../.github/workflows/docs.yml) builds the site with warnings treated as errors and uploads the `documentation-site` artifact. Pull requests validate without deploying. Publishing from `main` requires these repository settings:

1. **Settings → Pages → Build and deployment → Source: GitHub Actions**.
2. **Settings → Secrets and variables → Actions → Variables → New repository variable**: name `DOCS_PUBLISH_ENABLED`, value `true`.
3. **Actions → Documentation → Run workflow**, using `main`.

Once configured, successful `main` builds deploy all languages together. The deployment environment reports the site URL. Without publishing enabled, the complete site is still available as a workflow artifact.

## Publishing a release

The [local setup guide](RUNNING.md#publish-your-own-executable) contains commands for producing standalone and lightweight builds without publishing a GitHub release.

For maintainers, the release job runs when a version tag matching `v*.*.*` is pushed. It waits for both test jobs, builds the two Windows x64 executables, creates SHA-256 checksums, and attaches them to a GitHub release. A normal push to `main` runs checks without creating a binary release.

Before creating a version tag, review the relevant source changes, run the Windows checklist, and confirm the intended release version. For example, to publish version `1.2.3` from the reviewed commit:

```sh
git tag -a v1.2.3 -m "ChromaTray 1.2.3"
git push origin v1.2.3
```

The release workflow uses the tag to set the executable version. It does not perform Authenticode signing. GitHub Pages deployment is handled by its separate Documentation workflow.

## Making changes

Follow [.editorconfig](../.editorconfig), keep platform-independent logic separate from Windows APIs, and comment the reasoning behind non-obvious behavior. Use XML comments for public API contracts. Keep examples and commands in the guides consistent with the code and update translations when their content changes.

Restore with `--locked-mode` for normal validation. If a dependency intentionally changes, regenerate and review the affected `packages.lock.json` files. Single-file publishing uses separate locks under `obj` so it does not alter the normal build/test dependency lock.

When submitting a change, describe its trigger, resulting behavior, and relevant validation. Include a screenshot for a visual change and identify any desktop checks that still need a real Windows machine.
