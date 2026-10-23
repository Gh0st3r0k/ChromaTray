# Running ChromaTray locally

This guide covers Windows 10/11 x64 with a normal interactive desktop. The application interface is in English. Linux, macOS, and WSL can run the shared algorithm tests, but cannot run the Windows Forms interface or its screen picker.

## Choose how to run

| Option | Install first | Best for |
| --- | --- | --- |
| Standalone executable | Nothing beyond supported Windows | Using a published build. |
| Lightweight executable | x64 .NET 8 Desktop Runtime | Using a smaller published build. |
| Source with PowerShell | .NET 8 SDK, 8.0.4xx | Running and modifying the current code. |
| Source with Visual Studio | Visual Studio 2022 17.11+ and .NET desktop development | Editing and debugging in an IDE. |

## Run an executable

Tagged releases create `ChromaTray-win-x64-standalone.exe` and `ChromaTray-win-x64-lightweight.exe` on the [Releases page](https://github.com/Gh0st3r0k/ChromaTray/releases). If a release has not been published, use the source instructions below or publish your own executable.

1. Save the executable somewhere convenient, such as `C:\Tools\ChromaTray`.
2. Double click it. The dashboard opens and an icon appears in the notification area.
3. Press **Ctrl+Shift+X**, point at a color, adjust with **← ↑ ↓ →**, and press **Enter**.
4. Paste into Notepad with **Ctrl+V** to verify the copied value.
5. Press **Ctrl+Shift+Z** and drag a rectangle to extract a palette.

The standalone build includes .NET. The lightweight build specifically needs the **x64 .NET 8 Desktop Runtime**; the base .NET Runtime and ASP.NET Core Runtime do not include Windows Forms.

When `SHA256SUMS.txt` is provided, calculate the downloaded file's checksum in PowerShell:

```powershell
Get-FileHash .\ChromaTray-win-x64-standalone.exe -Algorithm SHA256
```

Compare `Hash` with the entry for that filename. Hexadecimal letter case does not matter.

## Run from source with PowerShell

### Install prerequisites

Install the [x64 .NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). The project requires the **8.0.4xx feature band**, at least 8.0.400, as configured in [global.json](../global.json). An SDK from a different major version alone does not satisfy that requirement.

Install [Git for Windows](https://git-scm.com/downloads/win) if you want to clone the repository. Git is unnecessary when using a downloaded source ZIP.

Open a new PowerShell window after installing the SDK:

```powershell
dotnet --list-sdks
```

Check that an `8.0.4xx` SDK is listed. Multiple SDK versions can be installed side by side.

### Get the source

```powershell
cd $env:USERPROFILE\Documents
git clone https://github.com/Gh0st3r0k/ChromaTray.git
cd ChromaTray
```

Alternatively, use **Code → Download ZIP** on GitHub, extract the archive, and open PowerShell in the directory containing `ChromaTray.csproj`, `Program.cs`, and `global.json`.

### Restore and launch

```powershell
dotnet restore ChromaTray.csproj --locked-mode
dotnet run --project ChromaTray.csproj -c Release --no-restore
```

The first restore downloads the required NuGet packages. No API keys, `.env` file, database, or external service setup is needed. PowerShell remains attached to the application process. Use **Exit** in the tray menu to end it.

## Run in Visual Studio

1. Install **Visual Studio 2022 17.11 or later** with **.NET desktop development** and the .NET 8.0.4xx SDK.
2. Choose **Open a project or solution** and open `ChromaTray.csproj`.
3. Wait for NuGet restore to finish.
4. Press **F5** to debug or **Ctrl+F5** to run without the debugger.

The interface is constructed in C#; no separate form-designer setup is needed.

## Use the picker

| Task | Control |
| --- | --- |
| Pick one color | **Pick Point** / **Ctrl+Shift+X**, then **Enter** or left click. |
| Adjust a point | Arrow keys move one physical pixel; holding a key repeats. |
| Extract a palette | **Scan Region** / **Ctrl+Shift+Z**, then drag. |
| Cancel | **Esc** or right click. |
| Inspect exact individual shades | Turn off **Group similar shades** below the palette heading or in Settings. |
| Copy one representation | **Copy** beside the value in the inspector. |
| Copy a palette | Select an export mode below the list → **Copy palette**. |
| Change shortcuts | **Settings** → click a shortcut field → press the combination → **Save Changes**. |
| Restore the dashboard | Click the tray icon, which may be under the notification area's `^` arrow. |
| Exit completely | Right click the tray icon → **Exit**. |

The screen is frozen when capture starts. Cancel and reopen the picker if the desktop changes. Arrow keys stop at screen edges and cannot enter gaps between monitors; adjacent display edges can be crossed. You can alternate between the mouse and keyboard.

The primary format is the first enabled entry in **HEX, RGB, HSL, HSV, CMYK** order. Disable HEX to use RGB for quick copy. At least one format must remain enabled. The palette shows the primary value, percentage, and pixel count; other enabled representations appear in the inspector. Narrow windows use separate Palette and Inspector tabs.

**Group similar shades** affects region analysis only and is off by default. Existing installations retain their saved setting. Grouping uses RGB steps of eight and displays an original sampled color for each group. With grouping off, every listed RGB value corresponds to an exact captured shade, including text antialiasing pixels. At most 500 entries are shown; percentages still refer to the full region.

## Settings and recovery

Preferences are stored in:

```text
%LocalAppData%\ChromaTray\settings.json
```

Saving settings updates the file after a complete temporary write. Invalid or unreadable JSON falls back to default preferences in memory. Shortcut fields reject invalid combinations and duplicates; F12 is reserved by Windows, while F1–F11 can be used without modifiers.

To reset preferences:

1. Choose **Exit** in the tray menu.
2. Rename `settings.json` to `settings.backup.json` in the directory above.
3. Start ChromaTray again. Defaults are used; saving settings creates a new file.

## Publish your own executable

Run from the repository root with the SDK installed. Publishing performs its own restore, including Windows runtime packages.

Standalone, with .NET included:

```powershell
dotnet publish ChromaTray.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/standalone
```

Output: `artifacts\standalone\ChromaTray.exe`.

Lightweight, requiring the x64 .NET 8 Desktop Runtime on the target machine:

```powershell
dotnet publish ChromaTray.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o artifacts/lightweight
```

Output: `artifacts\lightweight\ChromaTray.exe`.

These commands do not sign the executable. Tagged releases use the same two publishing modes and rename the files for distribution.

## Test your build

Run the full suite on Windows:

```powershell
dotnet restore ChromaTray.Tests/ChromaTray.Tests.csproj --locked-mode
dotnet test ChromaTray.Tests/ChromaTray.Tests.csproj -c Release --no-restore --logger "console;verbosity=normal"
```

Run the shared algorithms on Windows, Linux, or macOS:

```powershell
dotnet restore ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj --locked-mode
dotnet test ChromaTray.Core.Tests/ChromaTray.Core.Tests.csproj -c Release --no-restore
```

Use the [Windows checklist](WINDOWS-CHECKLIST.md) for real desktop capture and display checks. Automated test success does not by itself verify a particular monitor or graphics setup.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| `dotnet` is not recognized | Install the SDK and open a new PowerShell window. |
| `A compatible .NET SDK was not found` | Check `dotnet --list-sdks`; the repository requires SDK 8.0.4xx. |
| The app asks to install .NET | Install **Desktop Runtime 8 x64** for the lightweight build, or use standalone. |
| `ChromaTray.csproj` cannot be found | Open PowerShell in the extracted repository directory. |
| A shortcut does not work | Check the status message. Another application may own the combination; change it in Settings or use a capture button. |
| ChromaTray is already running | Find its tray icon, restore the existing instance, or choose Exit before restarting. |
| The window disappears after clicking X | Closing the dashboard leaves the application in the tray. |
| The clipboard is busy | Retry Copy after the other application finishes its clipboard operation. |
| Settings cannot be saved | Check write access to `%LocalAppData%\ChromaTray`. |
| NuGet restore fails | Check access to `api.nuget.org` and `globalcdn.nuget.org`, the system clock, and trusted certificates. |
| Text edges contain extra shades | Those are captured antialiasing pixels. Disable grouping for exact shades; capture uses no JPEG or resizing. |
| Protected video or the lock screen is missing | Windows capture restrictions apply; test on the normal desktop. |
| SmartScreen reports an unknown publisher | The build is unsigned. Verify its source and checksum; for a build you trust, the dialog may offer **More info → Run anyway**. |
| Windows Security reports a named threat | Record the exact detection and file hash so it can be investigated; an unknown publisher prompt is a different condition. |

When reporting a problem, include the commit or release, Windows version, monitor arrangement and scaling, build command, exact error, and steps to reproduce. Crop screenshots to the relevant area.

## Startup and tray restore timings

To investigate a slow window, exit the existing tray instance and enable optional
local diagnostics in Command Prompt before launching a fresh build:

```cmd
set "CHROMATRAY_PERF_LOG=%TEMP%\ChromaTray-performance.log"
bin\Release\net8.0-windows\ChromaTray.exe
```

Close the dashboard and reopen it from the tray several times, then inspect the
log. It records dashboard construction, synchronous restore, and elapsed time to
the next UI idle event. Startup timing starts in managed `Main`; it excludes
process creation and .NET runtime loading. Idle is a message-loop measurement,
not a guarantee that Windows has presented the frame. Logs contain timestamps,
process IDs and durations, without settings or captured screen contents.
Logging is disabled unless `CHROMATRAY_PERF_LOG` is set. To disable it for the
next launch, run `set CHROMATRAY_PERF_LOG=` and exit the current tray instance.

## Managed Linux workspace

In the prepared cloud workspace, the SDK is installed under `/workspace/.dotnet`:

```sh
bash scripts/setup-cloud.sh
bash scripts/check-cloud.sh
```

The setup script verifies the SDK archive with SHA-512 and restores locked dependencies. The check script compiles the Windows project and runs shared algorithm tests. It does not launch the Windows interface. The scripts allow overriding the SDK directory with `CHROMATRAY_DOTNET_ROOT`; see their headers for the remaining environment defaults.

For code structure, documentation builds, and release maintenance, continue with the [development guide](DEVELOPMENT.md).
