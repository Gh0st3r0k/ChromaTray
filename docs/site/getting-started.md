---
title: Get started
---

# Get started

## Run the app

Use a Windows x64 build from a trusted source, or build the repository locally. A standalone build includes .NET; a lightweight build requires the x64 .NET 8 Desktop Runtime.

To run from source on Windows, install the .NET 8 SDK and use these commands from the repository root:

```powershell
dotnet restore ChromaTray.csproj --locked-mode
dotnet run --project ChromaTray.csproj
```

The SDK must be in the **8.0.4xx** feature band selected by `global.json`. For Visual Studio setup, executable publishing, settings recovery, and common errors, use the [complete local setup guide](https://github.com/Gh0st3r0k/ChromaTray/blob/main/docs/RUNNING.md).

## Capture colors

| Action | Control |
| --- | --- |
| Pick a point | `Ctrl+Shift+X` or **Pick Point** |
| Move the selection | Arrow keys, one physical pixel at a time |
| Confirm a point | `Enter` or left click |
| Scan a region | `Ctrl+Shift+Z` or **Scan Region**, then drag |
| Cancel | `Esc` or right click |
| Copy a selected color | Double click or `Ctrl+C` |
| Change shortcuts and formats | **Settings** |
| Exit completely | Tray menu → **Exit** |

The app hides its own window before taking a frozen desktop snapshot. Point picking always returns the original RGB pixel. ClearType and other antialiasing can produce colored edge pixels around text; those pixels already exist on the desktop and remain visible in the magnifier.

## Inspect and export

Choose a palette row to update the color inspector. On narrow windows, use the **Palette** and **Inspector** tabs. The export selector supports active formats, the primary format, CSS custom properties, and JSON.

**Group similar shades** is below the palette heading and also in Settings. It is off by default. When enabled, each RGB bucket is represented by its most frequent original color; its count combines all shades in that bucket. The list shows up to 500 entries, with percentages calculated from the entire captured region.

The primary format is the first enabled entry in HEX, RGB, HSL, HSV, CMYK order. Disable HEX in Settings to make quick copy use RGB. CSS export uses HEX when HSV or CMYK is primary; text and JSON preserve enabled representations.

## Settings and closing the app

Preferences are stored at `%LocalAppData%\ChromaTray\settings.json`. Closing the dashboard keeps the tray application running. Restore it with the tray icon, or choose **Exit** from its menu to stop the process. To reset preferences, exit first, rename the settings file, and restart.
