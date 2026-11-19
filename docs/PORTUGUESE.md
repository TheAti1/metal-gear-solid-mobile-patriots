# Brazilian Portuguese / Português (Brasil)

Official in **PC 5.3.0** and **Android 1.1.0**.

Translation: **[(Sanju Vinho e Fumaça)](https://steamcommunity.com/id/SanjiVinsmokeBRASIL/)**. Both submitted JSON files retain the contributor's text and author field. Patriots supplies the additional Android-only launcher/update messages.

## Coverage

| Component | Translated entries |
|---|---:|
| Windows launcher | 299 / 299 |
| Game menus, dialogue and tutorials | 1,814 / 1,814 |
| Runtime, button names and Discord room/status text | 48 / 48 |

The full game catalogue also includes 70 legacy N-Gage service/achievement metadata rows and four protected charset/native-reference rows, omitted from the contributor's submission. They are restored from the canonical template with empty translations/English fallback. They are not game-dialogue gaps and are not presented as translated N-Gage services.

## Files and rebuilding

- `localization/launcher/pt-BR.launcher.json` — reviewed launcher translation.
- `localization/game/pt-BR.game.json` — reviewed game/runtime translation plus unchanged reference rows.
- `localization/android/pt-BR.android.json` — Android-only strings.
- `Arayuz/Languages/pt-br/{launcher,game}.json` — installed PC catalogues.
- `Oyun/languages/pt-br/data/localisation/` — fourteen compiled DAT tables.
- `Oyun/languages/pt-br/runtime.bin` — runtime/Discord/button labels.
- `Oyun/languages/pt-br/data/common/ui/hud.group.bin` — Portuguese font overlay.

Build the launcher against a prepared local game package, then import the two JSONs with that launcher's `--import-language <path>` command (launcher first, game second). This uses the same validated DAT compiler as the interactive importer. Imports normalize the locale ID to `pt-br`. The English/Turkish built-in tables cannot be overwritten by imports; installed Portuguese can be updated with a new reviewed JSON.

Generate the Portuguese bitmap-font overlay from the prepared Patriots game data:

```powershell
python tools/build_portuguese_fonts.py --data 'package/Oyun/game/data' --game-json localization/game/pt-BR.game.json --out 'package/Oyun/languages/pt-br'
python tools/check_language_templates.py
python tools/check_portuguese.py
```

The font tool derives accent marks from the existing bitmap lettering, reparses its output and checks every original glyph pixel. It writes a preview and a verification report alongside the overlay. Shared font groups and all other languages remain unchanged.

Android packaging requires the prepared `languages/pt-br` folder; it bundles the same DAT/font/runtime files and generates its launcher catalogue from the reviewed PC JSON plus Android-only strings. Personal settings and savedata are excluded.

## New translations

Use the launcher's **Export translation templates** action or copy `launcher.en.json` / `game.en.json` and change the locale metadata. The `pt-BR.*.json` files are completed Portuguese translations, not blank templates. Keep `id`, `source`, metadata and protected tokens unchanged.
