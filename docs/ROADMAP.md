# Development roadmap

Ideas for future versions. This list may change and does not commit to delivery dates.

## Appearance themes

Add light and dark themes, then consider user-defined interface colors. Preserve
readable text, accessible contrast, and consistent styling across the dashboard,
settings, menus, and capture overlays.

## Windows, Linux, and macOS

Investigate replacing Windows Forms with a cross-platform interface, such as
Avalonia while retaining C#. Reuse color algorithms, exports, and suitable tests.
Screen capture, global shortcuts, tray integration, and operating-system
permissions need platform-specific implementations and validation.

## Possible language change

Evaluate other languages and technologies with a small prototype. Decide whether
to rewrite only after comparing startup and restore latency, memory consumption,
and development effort. A new language alone does not guarantee better performance
or support for multiple operating systems.

## Palettes for web design

Generate five coordinated colors from a selected color, with the ability to lock
individual colors and regenerate the rest. Preview the palette on a sample website,
check text and button contrast, and export CSS variables. These are full design
palettes, not just lighter or darker shades of one color. Evaluate online generators
and open-source alternatives before selecting an implementation.

### Palette providers and optional personal keys

Plan a **Palette source** setting. Colormind is the initial candidate: its published
API examples do not require an account or API key. Other providers may support a
user-supplied personal key, with requests billed or limited under that user's
account according to the provider's terms. Do not require a key for providers that
do not use one.

Send requests only after an explicit user action and transmit selected RGB colors,
not screenshots. Where personal keys are needed, use the operating system's secure
credential storage rather than plain `settings.json`; allow deletion and exclude
keys from logs and exports.

Colormind's published terms allow free personal and non-commercial API use;
commercial applications must contact the service owner. ChromaTray's MIT license
does not grant additional rights to external APIs. Document these separate terms.
Before integration, verify HTTPS support and current service conditions. Preserve
locked colors locally because Colormind may slightly alter supplied colors.

The `colormind-palettes` branch implements the [Colormind generator](https://github.com/Gh0st3r0k/ChromaTray/blob/colormind-palettes/docs/PALETTES.md).
Selection between multiple providers and personal-key storage remain future work.
