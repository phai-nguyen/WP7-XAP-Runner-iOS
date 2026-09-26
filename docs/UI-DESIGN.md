# UI Design — Approved Direction

**Status:** APPROVED  
**Date:** 2026-09-26

## Approved icon

Use the direction selected by the user from **concept image 2**:

- dark navy / deep blue base;
- cyan / electric-blue edge lighting;
- Windows Phone-style device silhouette with Metro tiles;
- visible XAP identity;
- technical / emulator appearance;
- modernized rather than a literal copy of the original Windows Phone UI.

The icon should remain legible at small iOS icon sizes and avoid excessive micro-detail.

## Approved app theme

Use the direction selected by the user from **concept image 4**:

- dark Metro-inspired UI;
- cyan / electric-blue accent;
- large typography;
- rounded dark cards;
- clear runtime status;
- high-contrast diagnostics/log display;
- native iPhone layout conventions while preserving Windows Phone visual character.

## Primary navigation

Target structure:

1. **Home**
   - Import XAP
   - Library
   - Diagnostics
   - Logs
   - Runtime status

2. **XAP Library**
   - app icon
   - title/publisher/version
   - Silverlight/XNA classification
   - compatibility status
   - Run / Details

3. **App Details**
   - overview
   - manifest
   - assemblies
   - logs
   - capabilities
   - XAPSCAN1 result

4. **Running / Launch**
   - startup pipeline
   - real-time compatibility stages
   - Failure Oracle markers
   - stop application

5. **Logs / Diagnostics**
   - APP / UI / ILRUN1 / BIND1 / XAML1 filters
   - copy/share/clear
   - monospace runtime log
   - persistent log integration

6. **Settings**
   - persistent logging
   - debug mode
   - auto-run after import
   - theme/runtime options

7. **Files / Persistent logs**
   - WP7Runner_TakeThis.log
   - WP7Runner_Persistent.log
   - WP7Runner_Persistent-prev.log

8. **Splash**
   - approved icon
   - WP7 XAP Runner
   - runtime initialization status

## Design principles

- User-facing UI and diagnostic UI are both first-class.
- Never hide a runtime failure behind a black screen.
- Every launch stage should expose a visible state and a persistent log marker.
- UI should remain responsive even when the runtime probe blocks.
- Prefer a modern interpretation of Metro rather than recreating Windows Phone 7 pixel-for-pixel.
- Keep iOS navigation and touch targets comfortable on current iPhones.

## Current implementation priority

The approved design is the target UI direction, but runtime debugging remains higher priority than visual polish.

Implementation order:

1. render-safe Home/Diagnostics shell;
2. persistent Logs screen;
3. runtime status / Failure Oracle visualization;
4. XAP import;
5. Library;
6. App Details;
7. Running screen;
8. Settings and polish.

## Canonical visual reference

The approved visual direction is based on the design concepts reviewed in the project conversation:

- **Icon:** concept image 2.
- **Theme:** concept image 4.
- The later multi-screen UI showcase is the working composition reference for implementation.
