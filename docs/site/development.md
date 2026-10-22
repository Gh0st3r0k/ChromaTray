---
title: Development
---

# Development

## Follow the code

| Component | Responsibility |
| --- | --- |
| `Program.cs` | Initialize WinForms, enforce one running instance, open the dashboard. |
| `MainDashboardForm.cs` | Connect window lifetime, tray commands, settings, and hotkeys. |
| `MainDashboardForm.Layout.cs` | Build controls and adapt the layout to window size and DPI. |
| `MainDashboardForm.Capture.cs` | Hide the app, start a capture, and run cancellable palette analysis. |
| `MainDashboardForm.Palette.cs` | Update selection and inspector values, then copy or export. |
| `ScreenOverlayForm` | Own the frozen snapshot and handle point/region selection. Its `.Painting.cs` part draws the overlay. |
| `Forms/Controls` | Paint themed controls, virtual palette rows, and scrollbars. |
| `Services` | Capture preparation, histogram analysis, export, validation, and Windows hotkeys. |
| `Models` | Settings, hotkey bindings, and color representations. |

Capture uses physical screen coordinates, which can be negative on secondary monitors. Snapshot coordinates are relative to the virtual desktop's upper-left corner. Keep those coordinate systems separate when changing the picker.

The overlay owns its snapshot and cropped bitmap. The dashboard clones a captured region for later analysis; each background analysis receives another owned snapshot. A request version prevents an older analysis from overwriting a newer selection.

## Build and test

```powershell
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore
```

The full suite requires Windows. On Linux/macOS, run `ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj` to test the shared color, histogram, navigation, and export sources. Linux can also compile the Windows project with its existing `EnableWindowsTargeting` setting.

## Build this documentation

Run from the repository root with the .NET 8 SDK:

```powershell
dotnet tool restore
dotnet restore ChromaTray.csproj --locked-mode
dotnet docfx docs/site/docfx.json --warningsAsErrors
```

The static site is written to `artifacts/docs-site`. To preview it:

```powershell
dotnet docfx serve artifacts/docs-site --port 8080
```

Open `http://localhost:8080`. API metadata and HTML are generated outputs and are excluded from Git. DocFX's version is pinned in `.config/dotnet-tools.json`.

Update `/// <summary>`, `<param>`, and `<returns>` comments in the C# source to update API descriptions. Ordinary `//` implementation comments remain in the source. The API reference shows the project's public types and members; this page also explains the internal components.

## Automatic updates

The **Documentation** workflow builds the site on pushes and pull requests and uploads a `documentation-site` artifact. Pull requests are validated without publishing.

To publish automatically to GitHub Pages, a repository administrator must perform this one-time setup:

1. In **Settings → Pages**, set **Source** to **GitHub Actions**.
2. In **Settings → Secrets and variables → Actions → Variables**, add `DOCS_PUBLISH_ENABLED` with value `true`.
3. Run the **Documentation** workflow, or push a documentation/source change to `main`.

After setup, successful builds on `main` deploy the site. The workflow's deployment environment reports the public URL. Without that setup, the complete site is still available as a downloadable workflow artifact.
