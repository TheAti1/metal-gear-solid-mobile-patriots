# Changelog

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
