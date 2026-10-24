# Colormind palette generator

This feature is developed on the `colormind-palettes` branch. It is optional and
requires Internet access; screen picking and region analysis continue to run locally.

## Open the generator

- Select any palette row and click **Suggest palette** in the inspector. The selected
  RGB color is locked in the first position. Colormind automatically suggests the
  other four colors.
- Choose **Palette generator…** from the tray menu to generate all five colors
  without an initial selection.

The separate window follows ChromaTray's theme. Use **Locked** to preserve colors
and **Generate** to update only unlocked positions. Locked colors are restored
exactly after each response because Colormind may alter supplied RGB values slightly.
Unlock at least one position before generating again.

Each position has **Pick pixel**, which opens the existing screen picker. Confirm
with Enter or a click; Escape cancels. A confirmed pixel replaces and locks that
position. It does **not** start a request: press **Generate** when ready. The main
window and generator are hidden during capture, then restored. Dashboard capture
hotkeys are paused while the modal generator is open.

Copy individual HEX values or the complete palette as CSS variables. Existing
palette copying in the dashboard continues to work independently. A failed request
preserves existing colors; retry with **Generate**. Closing the window cancels any
pending request. Initial theme swatches are placeholders, not a generated result.

## Service access and terms

Requests use `https://colormind.io/api/` with model `ui` and five RGB/empty positions.
No account or API key is required by the published API. Only these colors and the
model are sent; screenshots and local settings are not uploaded. Requests time out
after 25 seconds. There is no fallback to unencrypted HTTP.

The [published Colormind terms](http://colormind.io/api-access/) allow free personal
and non-commercial API use. Commercial applications must contact jack@colormind.io.
ChromaTray's MIT license does not extend to the external service. There is no
availability guarantee; the provider documents brief downtime when models refresh.

HTTP contract tests use simulated responses. Live HTTPS requests were blocked by
this development environment's egress policy, so end-to-end service availability
must be verified on an allowed network. A blocked request here does not establish
that Colormind itself rejects requests or lacks HTTPS support.
