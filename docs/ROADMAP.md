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
