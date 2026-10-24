# Manual Windows verification

Use this checklist on Windows 10/11 x64 with a normal interactive desktop before a release. These unchecked items describe work to perform; they are not a claim that a particular build has already passed.

Record the commit or release, Windows version, monitor arrangement, display scaling, and build variant alongside your results. Start with [Running locally](RUNNING.md) if the application is not installed.

## Startup and tray lifetime

- [ ] Launch the application: the dashboard, tray icon, color formats, and initial `#98DED9` sample are visible.
- [ ] Close the dashboard: it hides, and clicking the tray icon restores it.
- [ ] Minimize and restore using the taskbar several times: the palette and selection persist, with no black frame or visibly staggered controls. Check normal and maximized windows separately.
- [ ] At each supported DPI, verify that complete color values fit and individual **Copy** buttons stay at the right edge, including after restoring the window.
- [ ] Start a second instance: it reports that ChromaTray is already running.
- [ ] Choose **Exit**: the icon disappears, the process stops, and a subsequent launch works.

## Point picking and keyboard input

- [ ] Press **Ctrl+Shift+X**: the dashboard is absent from the capture, and the magnifier shows the intended pixel.
- [ ] Move with the mouse, step once in each direction with the arrows, confirm with Enter, and verify the copied value in Notepad.
- [ ] Hold an arrow key: movement repeats and stops at a display edge.
- [ ] Alternate mouse and arrow input, then confirm: the magnifier, coordinates, and result all refer to the same point.
- [ ] Open the picker while the dashboard is hidden, then cancel with Esc and right click in separate attempts: cancellation preserves the previous dashboard visibility.
- [ ] Verify RGB/HEX and magnifier readability over black and white backgrounds near every monitor edge.
- [ ] Confirm that only one white crosshair is visible and the point-mode snapshot is untinted.

## Multiple monitors and DPI

- [ ] Test another monitor placed left of or above the primary display: negative coordinates and the sampled pixel agree.
- [ ] Move across a shared display edge and confirm that keyboard navigation cannot enter an empty gap between monitors.
- [ ] At Windows scaling of 100%, 125%, 150%, and 200%, verify the selection geometry, magnifier, and hint placement on the available displays.
- [ ] Move the dashboard between displays with different scaling: controls remain usable and no overlap appears.

## Region capture and analysis

- [ ] Press **Ctrl+Shift+Z** and drag in every direction: counts and percentages match the selected region.
- [ ] Capture across a display boundary and cancel an undersized selection.
- [ ] Toggle **Group similar shades** repeatedly: the last region is reanalyzed and the final result matches the final toggle state.
- [ ] With grouping off, compare a known solid-color region with its source; text antialiasing shades should remain present where they exist on screen.
- [ ] Start a new capture while the previous analysis is running: an older result must not replace the newer selection.
- [ ] Start capture using a dashboard button, hotkey, and tray menu over a solid background: neither the dashboard nor the closing menu remains in the snapshot.

## Settings, formats, and clipboard

- [ ] Set distinct shortcuts in Settings, save, restart, and verify both bindings.
- [ ] Assign a shortcut owned by another application: the dashboard reports the conflict and capture buttons still work.
- [ ] Cancel a settings edit: the active shortcuts and formats remain unchanged.
- [ ] Enable only RGB: point picking copies RGB and both the palette and inspector show RGB. The final enabled format cannot be disabled.
- [ ] Test each format's Copy button, palette-row double click, Ctrl+C, **Copy active formats**, and all four **Copy palette** modes.
- [ ] With HSV or CMYK primary, CSS export contains HEX while JSON retains enabled formats and numeric percentages.
- [ ] Check that preferences survive restart. Follow the [recovery instructions](RUNNING.md#settings-and-recovery) when testing a reset.

## Layout and appearance

- [ ] Resize to the minimum window size at high DPI: capture actions, Settings, Copy, inspector scrolling, and palette scrolling remain accessible.
- [ ] At 100%, 125%, 150%, and 200%, the header does not overlap capture cards, and buttons are fully visible.
- [ ] On narrow windows, switch between Palette and Inspector tabs and verify that selection details remain correct.
- [ ] Scroll a 500-color palette with the wheel, drag the thumb, and select the final row with End. No white system scrollbar appears.
- [ ] Open the export selector using the mouse and keyboard: menu colors, selection, and arrow match the theme, and changing the mode changes the copied output.
- [ ] Inspect rounded button corners in normal, hover, pressed, focused, and disabled states over the surrounding panel color.

## Distribution

- [ ] Run standalone on a machine without .NET installed.
- [ ] Run lightweight on a machine with the x64 .NET 8 Desktop Runtime installed.
- [ ] Verify the distributed executables against their entries in `SHA256SUMS.txt`.

## Report a failure

Include the exact build, Windows version, display configuration, action, expected behavior, actual behavior, and error text. For rendering issues, attach a cropped screenshot showing the relevant controls. For color accuracy, include the expected RGB, sampled RGB, coordinates, and grouping state. Avoid capturing unrelated private desktop content.

Automated Windows tests and preview PNGs complement this checklist. They do not replace testing real desktop composition, mixed-display scaling, runtime installation, or the release files themselves.

## Palette generator and website preview

- [ ] Open the generator from the tray and from **Suggest palette**. Pick a pixel for a slot; the generator must survive hiding, return with that exact color locked, and make no request until **Generate** is pressed.
- [ ] Cancel the picker with Escape: existing colors and lock states remain unchanged and the original windows return.
- [ ] Open **Website preview** from the generator and independently from the tray. Reassign each role and confirm that two roles swap colors immediately.
- [ ] Copy role CSS and confirm that named variables match the preview. Verify the sample at 100%, 125%, 150%, and 200% DPI.
