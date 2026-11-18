# Changelog

## Android 1.0.1

- Correct the Android system/installer application label.


## Android 1.0.0 · 2026-11-18

- Standalone Android 9+ universal APK for ARM64, ARMv7 and x86_64, with bundled game data and Turkish / English selection.
- Startup/manual release checks, ranged downloads with retry, SHA256/package/signature checks and Android installer handoff.
- Native game lifetime isolated from the launcher, with save data kept separate from immutable assets.


## 5.2.0 — 2026-11-18

- Addressed #1 with independent Crisp Interface sampling: nearest font/sprite atlas filtering and scoped 2D MSAA suppression, preserving high-resolution 3D rendering, original world filters and AA settings.
- Added the #2 requested inventory-only vertical navigation inversion. Keyboard, D-pad and left stick share the same guest-action mapping; gameplay, Codec, pause and other menus retain their controls.
- Exposed both settings in the launcher and native overlay, persisted them, and allowed live inventory-inversion changes.
- Refreshed launcher templates to 299 entries with five new settings texts; all previous IDs/sources and the 1,936-record game catalogue remain unchanged.
- Added a translation upgrade tool that carries completed text into the new full template, a non-importable changes report, old-pack compatibility checks and upgrade regression tests.
- Added real OpenGL state restoration checks and inventory input policy checks.

## 5.1.0 — 2026-11-18

- Added a Languages page with independent launcher/game JSON import, combined ZIP import and template export.
- Added discovered community locale IDs, English fallback for partial packs, component coverage and transactional installation.
- Separated 294 launcher strings and 1,936 game/reference/runtime records into translation catalogues with stable IDs and protected PC-button tokens.
- Preserved live keyboard/gamepad prompts and room/status Discord telemetry in imported languages; added per-language bitmap font overlays.
- Added a Brazilian Portuguese contributor kit, existing DAT-to-JSON converter and English/Turkish translation guides. Portuguese templates are untranslated; official support awaits completed and reviewed files.
- Added isolated importer tests and CI validation for templates, UTF-8 DAT rebuilding, combined packs, token protection, fallback and rollback.

## 5.0.0 — 2026-11-17

- Added persistent English / Turkish selection to the launcher.
- Localized launcher pages, dialogs, controller labels and messages.
- Added isolated game-language overlays for original English and the completed Turkish translation.
- Kept live input prompts and rebinding in both languages, including bare `Press *` and first-person `Press 1` instructions.
- Localized Discord room names, game status and edition text to match the running game.
- Preserved shared game saves, graphics preferences and automatic backups.

## 4.0.0 — 2026-11-17

- Added runtime keyboard and controller prompts, Turkish loading text and Discord presence.
- Added current-room tracking and live launcher binding updates.
